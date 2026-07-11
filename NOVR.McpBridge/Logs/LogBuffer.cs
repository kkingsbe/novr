using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NOVR.McpBridge.Logs;

public enum LogLevel { Log, Warning, Error, Exception, Assert }

public readonly struct LogEntry
{
    public int Id { get; init; }
    public LogLevel Level { get; init; }
    public string Message { get; init; }
    public string StackTrace { get; init; }
    public DateTime TimestampUtc { get; init; }
}

public sealed class LogBuffer
{
    private const int Capacity = 500;
    private static LogBuffer? _instance;
    public static LogBuffer Instance => _instance ??= new LogBuffer();

    private readonly ConcurrentQueue<LogEntry> _entries = new();
    private int _nextId = 1;
    private readonly object _trimLock = new();
    private bool _hooked;

    public void EnsureHooked()
    {
        if (_hooked) return;
        _hooked = true;
        Application.logMessageReceivedThreaded += OnUnityLog;
        try { HarmonyInterop.AttachBepInExListener(OnBepInExLog); }
        catch (Exception ex) { Debug.LogWarning($"[NOVR.McpBridge] Could not attach BepInEx listener: {ex.Message}"); }
    }

    public void Capture(LogLevel level, string message, string stackTrace)
    {
        var entry = new LogEntry
        {
            Id = System.Threading.Interlocked.Increment(ref _nextId) - 1,
            Level = level,
            Message = message ?? "",
            StackTrace = stackTrace ?? "",
            TimestampUtc = DateTime.UtcNow,
        };
        _entries.Enqueue(entry);
        lock (_trimLock)
        {
            while (_entries.Count > Capacity && _entries.TryDequeue(out _)) { }
        }
    }

    public List<LogEntry> Snapshot(int sinceId, LogLevel? minLevel)
    {
        var snapshot = _entries.ToArray();
        var results = new List<LogEntry>();
        foreach (var e in snapshot)
        {
            if (e.Id <= sinceId) continue;
            if (minLevel.HasValue && !AtOrAbove(e.Level, minLevel.Value)) continue;
            results.Add(e);
        }
        return results;
    }

    private static bool AtOrAbove(LogLevel level, LogLevel min) =>
        (int)level >= (int)min;

    private void OnUnityLog(string condition, string stackTrace, LogType type)
    {
        var level = type switch
        {
            LogType.Error => LogLevel.Error,
            LogType.Exception => LogLevel.Exception,
            LogType.Warning => LogLevel.Warning,
            LogType.Assert => LogLevel.Assert,
            _ => LogLevel.Log,
        };
        Capture(level, condition, stackTrace);
    }

    private void OnBepInExLog(BepInEx.Logging.LogLevel level, string message)
    {
        var mapped = level == BepInEx.Logging.LogLevel.Error ? LogLevel.Error
                   : level == BepInEx.Logging.LogLevel.Warning ? LogLevel.Warning
                   : level == BepInEx.Logging.LogLevel.Message ? LogLevel.Log
                   : LogLevel.Log;
        Capture(mapped, message, "");
    }
}