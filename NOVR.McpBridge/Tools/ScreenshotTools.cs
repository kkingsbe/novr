using System;
using UnityEngine;

namespace NOVR.McpBridge.Tools;

public static class ScreenshotTools
{
    [McpTool("screenshot", "Captures the current game view as a PNG and returns it as image content. Optionally downscale via maxWidth.")]
    public static McpToolResult Screenshot(
        [McpParam(Name = "maxWidth", Description = "Downscale to this width (preserves aspect ratio). 0 = no scaling.", Required = false)]
        int maxWidth = 0,
        [McpParam(Name = "caption", Description = "Optional caption text returned alongside the image", Required = false)]
        string? caption = null)
    {
        var src = ScreenCapture.CaptureScreenshotAsTexture();
        Texture2D final = null!;
        try
        {
            final = (maxWidth > 0 && src.width > maxWidth) ? ScaleTexture(src, maxWidth) : src;
            var bytes = final.EncodeToPNG();
            var b64 = Convert.ToBase64String(bytes);
            var summary = caption ?? $"Captured {final.width}x{final.height} ({bytes.Length / 1024} KB PNG).";
            return McpToolResult.FromImage(b64, summary);
        }
        finally
        {
            if (!ReferenceEquals(src, final)) UnityEngine.Object.Destroy(final);
            UnityEngine.Object.Destroy(src);
        }
    }

    private static Texture2D ScaleTexture(Texture2D source, int maxWidth)
    {
        var ratio = (float)maxWidth / source.width;
        var w = maxWidth;
        var h = Mathf.RoundToInt(source.height * ratio);
        var rt = RenderTexture.GetTemporary(w, h, 0);
        Graphics.Blit(source, rt);
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        var result = new Texture2D(w, h, TextureFormat.RGB24, false);
        result.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        result.Apply();
        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);
        UnityEngine.Object.Destroy(source);
        return result;
    }
}
