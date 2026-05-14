using System.Collections.Concurrent;
using SmartMacroAI.Models;

namespace SmartMacroAI.Core;

/// <summary>
/// Manages concurrent macro execution across multiple target windows.
/// Each instance runs independently with isolated state.
/// </summary>
public sealed class ParallelExecutor : IDisposable
{
    private readonly ConcurrentDictionary<string, ParallelInstance> _instances = new();
    private int _maxConcurrent = 8;
    private bool _disposed;

    /// <summary>Maximum concurrent execution instances (default: 8).</summary>
    public int MaxConcurrent
    {
        get => _maxConcurrent;
        set => _maxConcurrent = Math.Clamp(value, 1, 32);
    }

    /// <summary>Fires when an instance's status changes.</summary>
    public event Action<string, ParallelInstanceStatus>? StatusChanged;

    /// <summary>Fires when an instance logs a message.</summary>
    public event Action<string, string>? InstanceLog;

    /// <summary>Gets all current instances and their statuses.</summary>
    public IReadOnlyList<ParallelInstanceInfo> GetInstances()
    {
        return _instances.Values
            .Select(i => new ParallelInstanceInfo
            {
                Id = i.Id,
                ScriptName = i.ScriptName,
                TargetWindowTitle = i.TargetWindowTitle,
                TargetHwnd = i.TargetHwnd,
                Status = i.Status,
                StartTime = i.StartTime,
                ErrorMessage = i.ErrorMessage,
            })
            .OrderBy(i => i.StartTime)
            .ToList();
    }

    /// <summary>
    /// Launches macro execution on multiple target windows concurrently.
    /// Returns the number of instances actually started.
    /// </summary>
    public int RunAll(MacroScript script, List<IntPtr> targetWindows, bool hardwareMode = false)
    {
        int started = 0;

        foreach (IntPtr hwnd in targetWindows)
        {
            if (_instances.Count >= MaxConcurrent)
            {
                break;
            }

            string windowTitle = Win32Api.GetWindowTitle(hwnd);
            if (string.IsNullOrWhiteSpace(windowTitle))
                windowTitle = $"Window 0x{hwnd:X}";

            string id = $"{script.Name}_{hwnd:X}_{DateTime.Now.Ticks}";

            var instance = new ParallelInstance
            {
                Id = id,
                ScriptName = script.Name,
                TargetWindowTitle = windowTitle,
                TargetHwnd = hwnd,
                Status = ParallelInstanceStatus.Running,
                StartTime = DateTime.Now,
                Cts = new CancellationTokenSource(),
            };

            if (!_instances.TryAdd(id, instance)) continue;

            // Launch execution on a background task
            instance.Task = Task.Run(async () =>
            {
                try
                {
                    var engine = new MacroEngine { HardwareMode = hardwareMode };
                    engine.Log += msg => InstanceLog?.Invoke(id, msg);

                    await engine.ExecuteScriptAsync(script, hwnd, instance.Cts.Token);

                    instance.Status = ParallelInstanceStatus.Completed;
                    StatusChanged?.Invoke(id, ParallelInstanceStatus.Completed);
                }
                catch (OperationCanceledException)
                {
                    instance.Status = ParallelInstanceStatus.Stopped;
                    StatusChanged?.Invoke(id, ParallelInstanceStatus.Stopped);
                }
                catch (Exception ex)
                {
                    instance.Status = ParallelInstanceStatus.Error;
                    instance.ErrorMessage = ex.Message;
                    StatusChanged?.Invoke(id, ParallelInstanceStatus.Error);
                    InstanceLog?.Invoke(id, $"[ERROR] {ex.Message}");
                }
            });

            started++;
            StatusChanged?.Invoke(id, ParallelInstanceStatus.Running);
        }

        return started;
    }

    /// <summary>
    /// Stops a specific instance by ID.
    /// </summary>
    public void StopInstance(string id)
    {
        if (_instances.TryGetValue(id, out var instance))
        {
            instance.Cts.Cancel();
        }
    }

    /// <summary>
    /// Stops all running instances.
    /// </summary>
    public async Task StopAllAsync()
    {
        foreach (var instance in _instances.Values)
        {
            instance.Cts.Cancel();
        }

        // Wait up to 2 seconds for all to terminate
        var tasks = _instances.Values
            .Where(i => i.Task is not null)
            .Select(i => i.Task!)
            .ToArray();

        if (tasks.Length > 0)
        {
            await Task.WhenAny(Task.WhenAll(tasks), Task.Delay(2000));
        }
    }

    /// <summary>
    /// Removes completed/stopped/error instances from the list.
    /// </summary>
    public void ClearFinished()
    {
        var toRemove = _instances
            .Where(kv => kv.Value.Status is ParallelInstanceStatus.Completed
                or ParallelInstanceStatus.Stopped
                or ParallelInstanceStatus.Error)
            .Select(kv => kv.Key)
            .ToList();

        foreach (string key in toRemove)
        {
            if (_instances.TryRemove(key, out var instance))
            {
                instance.Cts.Dispose();
            }
        }
    }

    /// <summary>
    /// Gets the count of currently running instances.
    /// </summary>
    public int RunningCount => _instances.Values.Count(i => i.Status == ParallelInstanceStatus.Running);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var instance in _instances.Values)
        {
            instance.Cts.Cancel();
            instance.Cts.Dispose();
        }
        _instances.Clear();
    }

    private class ParallelInstance
    {
        public string Id { get; set; } = "";
        public string ScriptName { get; set; } = "";
        public string TargetWindowTitle { get; set; } = "";
        public IntPtr TargetHwnd { get; set; }
        public ParallelInstanceStatus Status { get; set; }
        public DateTime StartTime { get; set; }
        public string? ErrorMessage { get; set; }
        public CancellationTokenSource Cts { get; set; } = new();
        public Task? Task { get; set; }
    }
}

/// <summary>Status of a parallel execution instance.</summary>
public enum ParallelInstanceStatus
{
    Running,
    Paused,
    Completed,
    Stopped,
    Error,
}

/// <summary>Read-only info about a parallel instance for UI display.</summary>
public class ParallelInstanceInfo
{
    public string Id { get; set; } = "";
    public string ScriptName { get; set; } = "";
    public string TargetWindowTitle { get; set; } = "";
    public IntPtr TargetHwnd { get; set; }
    public ParallelInstanceStatus Status { get; set; }
    public DateTime StartTime { get; set; }
    public string? ErrorMessage { get; set; }

    public string StatusDisplay => Status switch
    {
        ParallelInstanceStatus.Running => "🟢 Running",
        ParallelInstanceStatus.Paused => "🟡 Paused",
        ParallelInstanceStatus.Completed => "✅ Completed",
        ParallelInstanceStatus.Stopped => "⏹ Stopped",
        ParallelInstanceStatus.Error => "❌ Error",
        _ => "Unknown",
    };
}
