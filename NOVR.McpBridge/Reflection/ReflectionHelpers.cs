using System;
using System.Linq;
using System.Reflection;

namespace NOVR.McpBridge.Reflection;

internal static class ReflectionHelpers
{
    public static Type? ResolveType(string typeName)
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            var t = asm.GetType(typeName, false, true);
            if (t != null) return t;
        }

        var matches = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => SafeGetTypes(a))
            .Where(t => t.Name == typeName)
            .ToList();

        return matches.Count == 1 ? matches[0] : null;
    }

    public static MemberInfo? ResolveMember(Type type, string name)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        return (MemberInfo?)type.GetProperty(name, flags) ?? type.GetField(name, flags);
    }

    private static Type[] SafeGetTypes(Assembly a)
    {
        try { return a.GetTypes(); }
        catch (ReflectionTypeLoadException ex) { return ex.Types.Where(t => t != null).Cast<Type>().ToArray(); }
    }
}
