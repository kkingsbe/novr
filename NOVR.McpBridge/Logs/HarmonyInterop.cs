using System;
using BepInEx.Logging;
using BepInExLogLevel = BepInEx.Logging.LogLevel;

namespace NOVR.McpBridge.Logs;

internal static class HarmonyInterop
{
    public static void AttachBepInExListener(Action<BepInExLogLevel, string> onMessage)
    {
        Logger.Listeners.Add(new DelegateListener(onMessage));
    }

    private sealed class DelegateListener : ILogListener
    {
        private readonly Action<BepInExLogLevel, string> _onMessage;
        public DelegateListener(Action<BepInExLogLevel, string> onMessage) { _onMessage = onMessage; }
        public void LogEvent(object sender, LogEventArgs eventArgs) => _onMessage(eventArgs.Level, eventArgs.Data?.ToString() ?? "");
        public void Dispose() { }
    }
}