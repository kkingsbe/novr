using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Valve.Newtonsoft.Json.Linq;

namespace NOVR.McpBridge;

public sealed class McpHttpServer : IDisposable
{
    private readonly HttpListener _listener = new();
    private readonly int _port;
    private bool _running;

    public McpHttpServer(int port)
    {
        _port = port;
    }

    public void Start()
    {
        if (_running) return;
        _running = true;
        _listener.Prefixes.Add($"http://localhost:{_port}/");
        _listener.Start();
        _ = ListenLoop();
    }

    public void Stop()
    {
        _running = false;
        try { _listener.Stop(); } catch { }
    }

    public void Dispose() => Stop();

    private async Task ListenLoop()
    {
        while (_running)
        {
            try
            {
                var ctx = await _listener.GetContextAsync();
                _ = HandleRequest(ctx);
            }
            catch when (!_running) { break; }
        }
    }

    private async Task HandleRequest(HttpListenerContext ctx)
    {
        try
        {
            var rawUrl = ctx.Request.Url?.AbsolutePath?.Trim('/') ?? "";
            ctx.Response.ContentType = "application/json";

            switch (rawUrl)
            {
                case "health":
                    await JsonResponse(ctx, 200, "{\"status\":\"ok\"}");
                    break;

                case "tools":
                    await HandleGetTools(ctx);
                    break;

                case "invoke":
                    await HandleInvoke(ctx);
                    break;

                default:
                    await JsonResponse(ctx, 404, "{\"error\":\"Unknown endpoint\"}");
                    break;
            }
        }
        catch (Exception ex)
        {
            try
            {
                await JsonResponse(ctx, 500, new JObject { ["error"] = ex.Message }.ToString(Valve.Newtonsoft.Json.Formatting.None));
            }
            catch { }
        }
    }

    private async Task HandleGetTools(HttpListenerContext ctx)
    {
        var tools = new JArray();
        foreach (var (name, descriptor) in ToolRegistry.Tools)
        {
            tools.Add(new JObject
            {
                ["name"] = name,
                ["description"] = descriptor.Description,
                ["inputSchema"] = JToken.Parse(descriptor.InputSchemaJson),
            });
        }

        await JsonResponse(ctx, 200, tools.ToString(Valve.Newtonsoft.Json.Formatting.None));
    }

    private async Task HandleInvoke(HttpListenerContext ctx)
    {
        string body;
        using (var reader = new StreamReader(ctx.Request.InputStream, ctx.Request.ContentEncoding))
        {
            body = await reader.ReadToEndAsync();
        }

        var (toolName, args) = ParseInvokeRequest(body);

        if (!ToolRegistry.Tools.TryGetValue(toolName, out var descriptor))
        {
            await JsonResponse(ctx, 404, new JObject { ["error"] = $"Unknown tool: {toolName}" }.ToString(Valve.Newtonsoft.Json.Formatting.None));
            return;
        }

        var result = await MainThreadDispatcher.Instance.RunAsync(() =>
        {
            try { return ToolRegistry.Invoke(descriptor, args); }
            catch (Exception ex) { return $"<error>{ex.Message}"; }
        });

        var envelope = BuildResultEnvelope(result);
        var json = envelope.ToString(Valve.Newtonsoft.Json.Formatting.None);
        await JsonResponse(ctx, 200, $"{{\"result\":{json}}}");
    }

    private static (string name, Dictionary<string, object?> args) ParseInvokeRequest(string json)
    {
        var token = JToken.Parse(json);
        var name = token.Value<string>("tool") ?? "";
        var argsToken = token["args"] as JObject;
        var args = new Dictionary<string, object?>();
        if (argsToken != null)
        {
            foreach (var prop in argsToken.Properties())
            {
                args[prop.Name] = prop.Value.Type == JTokenType.Null
                    ? null
                    : prop.Value.ToObject<object>();
            }
        }
        return (name, args);
    }

    private static JToken BuildResultEnvelope(object? result)
    {
        if (result is McpToolResult mcp)
        {
            var blocks = new JArray();
            if (mcp.Text != null)
                blocks.Add(new JObject { ["type"] = "text", ["text"] = mcp.Text });
            if (mcp.ImageBase64 != null)
                blocks.Add(new JObject
                {
                    ["type"] = "image",
                    ["mimeType"] = mcp.ImageMimeType,
                    ["data"] = mcp.ImageBase64,
                });
            return blocks;
        }

        if (result == null) return JValue.CreateNull();
        if (result is string s) return new JValue(s);
        if (result is bool b) return new JValue(b);
        if (result is int || result is long || result is short || result is byte ||
            result is float || result is double || result is decimal)
            return new JValue(Convert.ToDouble(result, System.Globalization.CultureInfo.InvariantCulture));
        return JToken.FromObject(result);
    }

    private static Task JsonResponse(HttpListenerContext ctx, int status, string json)
    {
        ctx.Response.StatusCode = status;
        var buffer = Encoding.UTF8.GetBytes(json);
        ctx.Response.ContentLength64 = buffer.Length;
        return ctx.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length)
            .ContinueWith(_ =>
            {
                try { ctx.Response.OutputStream.Close(); } catch { }
            });
    }
}