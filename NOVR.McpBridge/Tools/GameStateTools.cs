using System;
using System.Reflection;
using UnityEngine;

namespace NOVR.McpBridge.Tools;

public static class GameStateTools
{
    [McpTool("set_timescale", "Sets UnityEngine.Time.timeScale (1 = real time, 0 = paused, 0.1 = slow-mo).")]
    public static string SetTimescale(
        [McpParam(Name = "scale", Description = "New timeScale value (clamped 0..10)")]
        float scale)
    {
        var clamped = Mathf.Clamp(scale, 0f, 10f);
        Time.timeScale = clamped;
        return $"Time.timeScale = {Time.timeScale} (requested {scale})";
    }

    [McpTool("teleport_player", "Teleports the NOVR-tracked aircraft to a position. Use 'x,y,z' or omit to print current position only.")]
    public static string TeleportPlayer(
        [McpParam(Name = "position", Description = "Comma-separated 'x,y,z' (Unity world space). Omit to read current position.", Required = false)]
        string? position = null,
        [McpParam(Name = "rotationY", Description = "Optional Y rotation in degrees", Required = false)]
        float? rotationY = null)
    {
        var coreType = FindType("NOVR.Core");
        if (coreType == null) return "NOVR.Core not found.";

        var instance = coreType.GetField("Instance", BindingFlags.Public | BindingFlags.Static)?.GetValue(null)
                    ?? coreType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
        if (instance == null) return "NOVR.Core.Instance is null.";

        var aircraftProp = coreType.GetProperty("TrackedAircraft", BindingFlags.Public | BindingFlags.Instance);
        var aircraft = aircraftProp?.GetValue(instance);
        if (aircraft == null) return "No TrackedAircraft.";

        var aircraftType = aircraft.GetType();
        var transformProp = aircraftType.GetProperty("Transform");
        var transform = transformProp?.GetValue(aircraft) as Transform;
        if (transform == null) return "TrackedAircraft has no Transform property.";

        if (position == null)
            return $"Current position: {transform.position}, rotation: {transform.eulerAngles}";

        var parts = position.Split(',');
        if (parts.Length != 3)
            return "position must be 'x,y,z'.";

        var pos = new Vector3(
            float.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture),
            float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture),
            float.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture));

        transform.position = pos;
        if (rotationY.HasValue)
        {
            var e = transform.eulerAngles;
            e.y = rotationY.Value;
            transform.eulerAngles = e;
        }

        return $"Teleported to {transform.position}, rotation {transform.eulerAngles}.";
    }

    private static Type? FindType(string fullName)
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            var t = asm.GetType(fullName, false, true);
            if (t != null) return t;
        }
        return null;
    }
}
