using System;
using System.Text;
using NOVR.McpBridge.Logs;

namespace NOVR.McpBridge.Tools;

public static class LogTools
{
    [McpTool("get_logs", "Returns recent Unity + BepInEx log entries. Pass sinceId to get only new entries, minLevel to filter (log|warning|error|exception).")]
    public static string GetLogs(
        [McpParam(Name = "sinceId", Description = "Return only entries with Id > sinceId (default 0)", Required = false)]
        int sinceId = 0,
        [McpParam(Name = "minLevel", Description = "Minimum level: log|warning|error|exception (default log)", Required = false)]
        string minLevel = "log")
    {
        LogBuffer.Instance.EnsureHooked();

        LogLevel? threshold = minLevel?.ToLowerInvariant() switch
        {
            null or "" or "log" => null,
            "warning" => LogLevel.Warning,
            "error" => LogLevel.Error,
            "exception" => LogLevel.Exception,
            _ => throw new ArgumentException($"Unknown minLevel '{minLevel}'. Use log|warning|error|exception."),
        };

        var entries = LogBuffer.Instance.Snapshot(sinceId, threshold);
        if (entries.Count == 0)
            return $"No log entries since id={sinceId} (minLevel={minLevel ?? "log"}).";

        var sb = new StringBuilder();
        sb.AppendLine($"Returned {entries.Count} entries (sinceId={sinceId}, minLevel={minLevel ?? "log"}):");
        foreach (var e in entries)
        {
            sb.AppendLine($"[{e.Id}] {e.TimestampUtc:O} {e.Level}: {e.Message}");
            if (!string.IsNullOrEmpty(e.StackTrace))
                sb.AppendLine($"    {e.StackTrace.Replace("\n", "\n    ")}");
        }
        return sb.ToString();
    }
}