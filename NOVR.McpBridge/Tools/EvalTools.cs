using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace NOVR.McpBridge.Tools;

public static class EvalTools
{
    private static readonly object _evalLock = new();
    private static object? _evaluator;
    private static bool _initTried;

    [McpTool("eval_csharp", "Evaluates a C# snippet against the live game using Mono.CSharp.Evaluator. Runs on the main thread. Captures Debug.Log output. The snippet must end with an expression statement to produce a return value. NOTE: Requires Mono.CSharp.dll to be available in the runtime.")]
    public static string EvalCSharp(
        [McpParam(Name = "snippet", Description = "C# code to evaluate. Last expression's value is returned as text.")]
        string snippet,
        [McpParam(Name = "captureLogs", Description = "Capture Debug.Log output during evaluation (default true)", Required = false)]
        bool captureLogs = true)
    {
        if (!TryInitEvaluator(out var initError))
            return $"<eval unavailable: {initError}>";

        EnsureMainThreadDispatcher();

        var logs = captureLogs ? new ConcurrentQueue<string>() : null;
        Application.LogCallback? handler = null;
        if (logs != null)
        {
            handler = (cond, st, type) => logs.Enqueue($"[{type}] {cond}");
            Application.logMessageReceivedThreaded += handler;
        }

        try
        {
            return MainThreadDispatcher.Instance.RunAsync(() =>
            {
                object? result = null;
                lock (_evalLock)
                {
                    var evalType = _evaluator!.GetType();
                    var evaluateMethod = evalType.GetMethod("Evaluate", new[] { typeof(string), typeof(object).MakeByRefType(), typeof(Type).MakeByRefType() });
                    if (evaluateMethod == null)
                        return "<eval error: Mono.CSharp.Evaluator.Evaluate(string, out object, out Type) method not found>";
                    var args = new object?[] { snippet, null, null };
                    var ok = (bool)evaluateMethod.Invoke(_evaluator, args)!;
                    if (!ok) return "<compile error>";
                    result = args[1];
                }

                if (logs != null && logs.Count > 0)
                {
                    var joined = string.Join("\n", logs);
                    return $"--- Debug.Log during eval ---\n{joined}\n--- return ---\n{result?.ToString() ?? "null"}";
                }
                return result?.ToString() ?? "(void)";
            }).Result;
        }
        catch (Exception ex)
        {
            return $"<eval threw {ex.GetType().Name}: {ex.Message}>";
        }
        finally
        {
            if (handler != null) Application.logMessageReceivedThreaded -= handler;
        }
    }

    private static bool TryInitEvaluator(out string error)
    {
        lock (_evalLock)
        {
            if (_evaluator != null) { error = ""; return true; }
            if (_initTried) { error = "Mono.CSharp.Evaluator not available (init already attempted)."; return false; }
            _initTried = true;

            try
            {
                var t = Type.GetType("Mono.CSharp.Evaluator, Mono.CSharp");
                if (t == null)
                {
                    t = AppDomain.CurrentDomain.GetAssemblies()
                        .Select(a => a.GetType("Mono.CSharp.Evaluator", false))
                        .FirstOrDefault(x => x != null);
                }

                if (t == null) { error = "Type Mono.CSharp.Evaluator not found in any loaded assembly. Drop Mono.CSharp.dll into /lib to enable."; return false; }

                _evaluator = Activator.CreateInstance(t, new object[] { 0 });
                error = "";
                return true;
            }
            catch (Exception ex)
            {
                error = $"Failed to instantiate Mono.CSharp.Evaluator: {ex.Message}";
                return false;
            }
        }
    }

    private static bool _dispatcherEnsured;
    private static void EnsureMainThreadDispatcher()
    {
        if (_dispatcherEnsured) return;
        _dispatcherEnsured = true;
        _ = MainThreadDispatcher.Instance;
    }
}
