using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using UnityEngine;

namespace NOVR.McpBridge;

public sealed class MainThreadDispatcher : MonoBehaviour
{
    private static MainThreadDispatcher? _instance;
    private readonly ConcurrentQueue<Action> _queue = new();

    public static MainThreadDispatcher Instance
    {
        get
        {
            if (_instance != null) return _instance;
            var go = new GameObject("NOVR.McpBridge.MainThreadDispatcher")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<MainThreadDispatcher>();
            return _instance;
        }
    }

    public const int DefaultTimeoutMs = 5000;

    public Task<T> RunAsync<T>(Func<T> func) => RunAsync(func, DefaultTimeoutMs);

    public async Task<T> RunAsync<T>(Func<T> func, int timeoutMs)
    {
        var tcs = new TaskCompletionSource<T>();
        _queue.Enqueue(() =>
        {
            try { tcs.SetResult(func()); }
            catch (Exception ex) { tcs.SetException(ex); }
        });

        var winner = await Task.WhenAny(tcs.Task, Task.Delay(timeoutMs));
        if (winner != tcs.Task)
            throw new TimeoutException(
                $"Main thread did not pump the queued action within {timeoutMs}ms " +
                "(game paused, unfocused, or runInBackground off?)");
        return await tcs.Task;
    }

    public Task RunAsync(Action action) => RunAsync<object?>(() => { action(); return null; }, DefaultTimeoutMs);

    public Task RunAsync(Action action, int timeoutMs) =>
        RunAsync<object?>(() => { action(); return null; }, timeoutMs);

    private void Update()
    {
        while (_queue.TryDequeue(out var action))
        {
            try { action(); }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[NOVR.McpBridge] Main-thread action threw: {ex}");
            }
        }
    }

    private void OnDestroy()
    {
        _instance = null;
    }
}