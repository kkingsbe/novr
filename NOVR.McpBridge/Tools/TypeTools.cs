using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace NOVR.McpBridge.Tools;

public static class TypeTools
{
    [McpTool("search_types", "Search loaded assemblies for types whose name contains the given substring (case-insensitive).")]
    public static string SearchTypes(
        [McpParam(Name = "substring", Description = "Substring to search for in type names")]
        string substring,
        [McpParam(Name = "maxResults", Description = "Cap the number of results (default 50)", Required = false)]
        int maxResults = 50)
    {
        if (string.IsNullOrEmpty(substring)) return "substring is required.";

        var matches = new List<Type>();
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type[] types;
            try { types = asm.GetTypes(); }
            catch (ReflectionTypeLoadException ex) { types = ex.Types.Where(t => t != null).Cast<Type>().ToArray(); }

            foreach (var t in types)
            {
                if (t.Name.IndexOf(substring, StringComparison.OrdinalIgnoreCase) >= 0)
                    matches.Add(t);
                if (matches.Count >= maxResults) break;
            }
            if (matches.Count >= maxResults) break;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"Found {matches.Count} type(s) matching '{substring}':");
        foreach (var t in matches.OrderBy(t => t.FullName))
            sb.AppendLine($"  - {t.FullName} ({t.Assembly.GetName().Name})");
        return sb.ToString();
    }

    [McpTool("get_type_members", "Lists the public methods, properties, and fields of a type.")]
    public static string GetTypeMembers(
        [McpParam(Name = "typeName", Description = "Fully-qualified or unique bare type name")]
        string typeName)
    {
        var type = ResolveType(typeName);
        if (type == null) return $"Type '{typeName}' not found.";

        var sb = new StringBuilder();
        sb.AppendLine($"Type: {type.FullName} ({type.Assembly.GetName().Name})");
        sb.AppendLine($"  Base: {type.BaseType?.FullName ?? "(none)"}");
        sb.AppendLine();

        const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        sb.AppendLine("  Methods:");
        foreach (var m in type.GetMethods(flags).Where(m => !m.IsSpecialName))
            sb.AppendLine($"    {m.ReturnType.Name} {m.Name}({string.Join(", ", m.GetParameters().Select(p => p.ParameterType.Name + " " + p.Name))})");

        sb.AppendLine("  Properties:");
        foreach (var p in type.GetProperties(flags).Where(p => p.GetIndexParameters().Length == 0))
            sb.AppendLine($"    {p.PropertyType.Name} {p.Name} {{ {(p.CanRead ? "get;" : "")} {(p.CanWrite ? "set;" : "")} }}");

        sb.AppendLine("  Fields:");
        foreach (var f in type.GetFields(flags))
            sb.AppendLine($"    {f.FieldType.Name} {f.Name}");

        return sb.ToString();
    }

    private static Type? ResolveType(string typeName)
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            var t = asm.GetType(typeName, false, true);
            if (t != null) return t;
        }
        var matches = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => { try { return a.GetTypes(); } catch (ReflectionTypeLoadException e) { return e.Types.Where(x => x != null).Cast<Type>(); } })
            .Where(t => t.Name == typeName).ToList();
        return matches.Count == 1 ? matches[0] : null;
    }
}
