using System;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;

namespace NOVR.McpBridge.Tools;

public static class HarmonyTools
{
    [McpTool("list_harmony_patches", "Lists every method patched by Harmony, grouped by method, with owner HarmonyIds.")]
    public static string ListHarmonyPatches(
        [McpParam(Name = "filter", Description = "Substring filter on method or owner name (optional)", Required = false)]
        string filter = "")
    {
        var methods = Harmony.GetAllPatchedMethods().ToList();
        var sb = new StringBuilder();
        var count = 0;

        foreach (var m in methods.OrderBy(m => m.DeclaringType?.FullName).ThenBy(m => m.Name))
        {
            var info = Harmony.GetPatchInfo(m);
            if (info == null) continue;

            var owners = string.Join(", ", info.Owners);
            var summary = $"{m.DeclaringType?.FullName}.{m.Name}";

            if (!string.IsNullOrEmpty(filter) &&
                summary.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0 &&
                owners.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
                continue;

            sb.AppendLine($"- {summary}");
            sb.AppendLine($"    owners: [{owners}]");
            foreach (var p in info.Prefixes)     sb.AppendLine($"    prefix:    {p.PatchMethod.FullDescription()}");
            foreach (var p in info.Postfixes)    sb.AppendLine($"    postfix:   {p.PatchMethod.FullDescription()}");
            foreach (var p in info.Transpilers)   sb.AppendLine($"    transpile: {p.PatchMethod.FullDescription()}");
            foreach (var p in info.Finalizers)   sb.AppendLine($"    finalize:  {p.PatchMethod.FullDescription()}");
            count++;
        }

        return $"Found {count} patched method(s):\n{sb}";
    }

    private static string FullDescription(this MethodInfo m) =>
        $"{m.DeclaringType?.FullName}.{m.Name}";
}
