using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using NOVR.McpBridge.Reflection;
using UnityEngine;

namespace NOVR.McpBridge.Tools;

public static class ReflectionTools
{
    [McpTool("get_member", "Reads a field or property on a component. path format: 'GameObject/Path:TypeName.memberName'. Set includePrivate=true to see non-public members.")]
    public static string GetMember(
        [McpParam(Name = "path", Description = "Hierarchy path + ':' + type + '.' + member")]
        string path,
        [McpParam(Name = "includePrivate", Description = "Include non-public members (default true)", Required = false)]
        bool includePrivate = true)
    {
        var mp = MemberPath.Parse(path);
        var go = ResolveGameObject(mp.GameObjectPath);
        if (go == null) return $"GameObject '{mp.GameObjectPath}' not found.";

        var type = ReflectionHelpers.ResolveType(mp.TypeName);
        if (type == null) return $"Type '{mp.TypeName}' not found.";

        var component = go.GetComponent(type);
        if (component == null) return $"Component of type '{mp.TypeName}' not on '{mp.GameObjectPath}'.";

        var member = ReflectionHelpers.ResolveMember(type, mp.MemberName);
        if (member == null) return $"Member '{mp.MemberName}' not found on {type.FullName}.";

        if (!includePrivate && member is FieldInfo f && !f.IsPublic)
            return $"Member '{mp.MemberName}' is non-public; pass includePrivate=true.";

        try
        {
            var value = member switch
            {
                PropertyInfo p => p.GetValue(component),
                FieldInfo fld => fld.GetValue(component),
                _ => null,
            };
            return $"{member.Name} = {FormatValue(value)}";
        }
        catch (Exception ex)
        {
            return $"<error reading {member.Name}: {ex.GetType().Name}: {ex.Message}>";
        }
    }

    [McpTool("set_member", "Writes a field or property on a component. path format: 'GameObject/Path:TypeName.memberName'. value is a string converted to the target type.")]
    public static string SetMember(
        [McpParam(Name = "path", Description = "Hierarchy path + ':' + type + '.' + member")]
        string path,
        [McpParam(Name = "value", Description = "New value (string; converted to target type)")]
        string value)
    {
        var mp = MemberPath.Parse(path);
        var go = ResolveGameObject(mp.GameObjectPath);
        if (go == null) return $"GameObject '{mp.GameObjectPath}' not found.";

        var type = ReflectionHelpers.ResolveType(mp.TypeName);
        if (type == null) return $"Type '{mp.TypeName}' not found.";

        var component = go.GetComponent(type);
        if (component == null) return $"Component of type '{mp.TypeName}' not on '{mp.GameObjectPath}'.";

        var member = ReflectionHelpers.ResolveMember(type, mp.MemberName);
        if (member == null) return $"Member '{mp.MemberName}' not found on {type.FullName}.";

        try
        {
            switch (member)
            {
                case PropertyInfo p:
                    if (!p.CanWrite) return $"Property '{mp.MemberName}' is read-only.";
                    var convertedProp = ConvertString(value, p.PropertyType);
                    p.SetValue(component, convertedProp);
                    return $"{p.Name} = {FormatValue(p.GetValue(component))}";
                case FieldInfo fld:
                    var convertedField = ConvertString(value, fld.FieldType);
                    fld.SetValue(component, convertedField);
                    return $"{fld.Name} = {FormatValue(fld.GetValue(component))}";
                default:
                    return $"Member '{mp.MemberName}' is not a field or property.";
            }
        }
        catch (Exception ex)
        {
            return $"<error writing {member.Name}: {ex.GetType().Name}: {ex.Message}>";
        }
    }

    [McpTool("invoke_method", "Invokes a method on a component. path format: 'GameObject/Path:TypeName.methodName'. Pass args as a JSON array of strings.")]
    public static string InvokeMethod(
        [McpParam(Name = "path", Description = "Hierarchy path + ':' + type + '.' + method")]
        string path,
        [McpParam(Name = "args", Description = "JSON array of string arguments (converted to declared parameter types)", Required = false)]
        string[]? args = null)
    {
        var mp = MemberPath.Parse(path);
        var go = ResolveGameObject(mp.GameObjectPath);
        if (go == null) return $"GameObject '{mp.GameObjectPath}' not found.";

        var type = ReflectionHelpers.ResolveType(mp.TypeName);
        if (type == null) return $"Type '{mp.TypeName}' not found.";

        var component = go.GetComponent(type);
        if (component == null) return $"Component of type '{mp.TypeName}' not on '{mp.GameObjectPath}'.";

        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        var method = type.GetMethod(mp.MemberName, flags);
        if (method == null) return $"Method '{mp.MemberName}' not found on {type.FullName}.";

        var parameters = method.GetParameters();
        var supplied = args ?? Array.Empty<string>();
        if (supplied.Length != parameters.Length)
            return $"Method takes {parameters.Length} args, got {supplied.Length}.";

        var callArgs = new object?[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
        {
            try { callArgs[i] = ConvertString(supplied[i], parameters[i].ParameterType); }
            catch (Exception ex) { return $"<error converting arg {i} to {parameters[i].ParameterType.Name}: {ex.Message}>"; }
        }

        try
        {
            var result = method.Invoke(component, callArgs);
            return method.ReturnType == typeof(void)
                ? $"{method.Name}() = void"
                : $"{method.Name}() = {FormatValue(result)}";
        }
        catch (TargetInvocationException tie) when (tie.InnerException != null)
        {
            return $"<{method.Name} threw {tie.InnerException.GetType().Name}: {tie.InnerException.Message}>";
        }
    }

    private static GameObject? ResolveGameObject(string path)
    {
        var matches = SceneToolsFindAll(path);
        return matches.Count > 0 ? matches[0] : null;
    }

    private static List<GameObject> SceneToolsFindAll(string nameOrPath)
    {
        if (nameOrPath.Contains('/'))
        {
            var results = new List<GameObject>();
            for (var i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;
                foreach (var root in scene.GetRootGameObjects())
                {
                    var t = FindByPath(root.transform, nameOrPath);
                    if (t != null) results.Add(t.gameObject);
                }
            }
            return results;
        }

        var subs = new List<GameObject>();
        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (!go.scene.isLoaded) continue;
            if (go.name.IndexOf(nameOrPath, StringComparison.OrdinalIgnoreCase) >= 0) subs.Add(go);
        }
        return subs;
    }

    private static Transform? FindByPath(Transform root, string path)
    {
        var parts = path.Split('/');
        var current = root;
        for (var i = 0; i < parts.Length; i++)
        {
            var wanted = parts[i];
            if (i == 0 && wanted == current.name) continue;
            Transform? next = null;
            for (var c = 0; c < current.childCount; c++)
            {
                var child = current.GetChild(c);
                if (child.name == wanted) { next = child; break; }
            }
            if (next == null) return null;
            current = next;
        }
        return current;
    }

    private static object? ConvertString(string raw, Type target)
    {
        if (target == typeof(string)) return raw;
        if (target == typeof(bool)) return bool.Parse(raw);
        if (target.IsEnum) return Enum.Parse(target, raw, ignoreCase: true);
        if (target == typeof(int)) return int.Parse(raw, CultureInfo.InvariantCulture);
        if (target == typeof(long)) return long.Parse(raw, CultureInfo.InvariantCulture);
        if (target == typeof(float)) return float.Parse(raw, CultureInfo.InvariantCulture);
        if (target == typeof(double)) return double.Parse(raw, CultureInfo.InvariantCulture);
        if (target == typeof(Vector3))
        {
            var parts = raw.Split(',');
            return new Vector3(float.Parse(parts[0], CultureInfo.InvariantCulture),
                               float.Parse(parts[1], CultureInfo.InvariantCulture),
                               float.Parse(parts[2], CultureInfo.InvariantCulture));
        }
        if (target == typeof(Vector2))
        {
            var parts = raw.Split(',');
            return new Vector2(float.Parse(parts[0], CultureInfo.InvariantCulture),
                               float.Parse(parts[1], CultureInfo.InvariantCulture));
        }
        if (target == typeof(Color))
        {
            var parts = raw.Split(',');
            return new Color(float.Parse(parts[0], CultureInfo.InvariantCulture),
                             float.Parse(parts[1], CultureInfo.InvariantCulture),
                             float.Parse(parts[2], CultureInfo.InvariantCulture),
                             parts.Length > 3 ? float.Parse(parts[3], CultureInfo.InvariantCulture) : 1f);
        }
        return Convert.ChangeType(raw, target, CultureInfo.InvariantCulture);
    }

    private static string FormatValue(object? value) => value?.ToString() ?? "null";
}
