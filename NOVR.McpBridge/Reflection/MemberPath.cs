using System;

namespace NOVR.McpBridge.Reflection;

public readonly struct MemberPath
{
    public string GameObjectPath { get; }
    public string TypeName { get; }
    public string MemberName { get; }
    public string? ChildPath { get; }

    private MemberPath(string goPath, string typeName, string memberName, string? childPath)
    {
        GameObjectPath = goPath;
        TypeName = typeName;
        MemberName = memberName;
        ChildPath = childPath;
    }

    public static MemberPath Parse(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("path is empty");

        var lastDot = input.LastIndexOf('.');
        var head = lastDot < 0 ? input : input.Substring(0, lastDot);
        var tail = lastDot < 0 ? "" : input.Substring(lastDot + 1);

        var colonIdx = head.LastIndexOf(':');
        if (colonIdx < 0)
            throw new ArgumentException($"path must contain ':' between GameObject path and type. Got: {input}");

        var goPath = head.Substring(0, colonIdx);
        var typePart = head.Substring(colonIdx + 1);

        string memberName;
        string? childPath;
        if (string.IsNullOrEmpty(tail))
        {
            memberName = "";
            childPath = null;
        }
        else
        {
            var firstDot = tail.IndexOf('.');
            if (firstDot < 0)
            {
                memberName = tail;
                childPath = null;
            }
            else
            {
                memberName = tail.Substring(0, firstDot);
                childPath = tail.Substring(firstDot + 1);
            }
        }

        return new MemberPath(goPath, typePart, memberName, childPath);
    }
}
