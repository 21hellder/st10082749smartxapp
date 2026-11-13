using SmartX.Api.DataStructures;
using SmartX.Api.Models;

namespace SmartX.Api.Services;

/// <summary>
/// Wraps the AVL tree so the rest of the application can ask
/// "give me all readings for device X between time A and B" in O(log n + k).
///
/// One AVL tree is maintained PER DEVICE, keyed by the reading's Timestamp.
/// This keeps queries independent: querying ESP32-001 never touches the
/// ESP32-002 tree, which is what makes per-device range queries fast even
/// with tens of thousands of total readings.
///
/// The service is a singleton so the tree survives across requests.
/// </summary>
public class TelemetryRangeService
{
    // DeviceId -> AVL tree keyed by DateTime (reading timestamp) -> TelemetryReading
    private readonly Dictionary<string, AvlTree<DateTime, TelemetryReading>> _treesByDevice =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly object _lock = new();

    /// <summary>
    /// Called from TelemetryProcessingService whenever a reading is stored,
    /// so the AVL index stays in sync with the TelemetryStore.
    /// </summary>
    public void Add(TelemetryReading reading)
    {
        if (string.IsNullOrWhiteSpace(reading.DeviceId)) return;

        lock (_lock)
        {
            if (!_treesByDevice.TryGetValue(reading.DeviceId, out var tree))
            {
                tree = new AvlTree<DateTime, TelemetryReading>();
                _treesByDevice[reading.DeviceId] = tree;
            }
            tree.Insert(reading.Timestamp, reading);
        }
    }

    /// <summary>
    /// Range query for a single device.
    /// Both bounds are inclusive.
    /// </summary>
    public List<TelemetryReading> GetRange(string deviceId, DateTime from, DateTime to)
    {
        lock (_lock)
        {
            if (!_treesByDevice.TryGetValue(deviceId, out var tree))
                return new List<TelemetryReading>();

            return tree.RangeQuery(from, to);
        }
    }

    /// <summary>
    /// Range query for a single device, filtered by metric (e.g. only Moisture).
    /// This matches the Part 3 brief example: "all moisture readings between 14:00 and 16:00".
    /// </summary>
    public List<TelemetryReading> GetRange(string deviceId, string? metric, DateTime from, DateTime to)
    {
        var raw = GetRange(deviceId, from, to);

        if (string.IsNullOrWhiteSpace(metric))
            return raw;

        return raw
            .Where(r => r.Metric.Equals(metric, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    /// <summary>Diagnostics: current AVL height per device (proves it stays balanced).</summary>
    public int GetTreeHeight(string deviceId)
    {
        lock (_lock)
        {
            return _treesByDevice.TryGetValue(deviceId, out var tree)
                ? tree.Height
                : 0;
        }
    }

    /// <summary>Diagnostics: total values stored across all device trees.</summary>
    public int GetTotalValueCount()
    {
        lock (_lock)
        {
            return _treesByDevice.Values.Sum(t => t.ValueCount);
        }
    }

    /// <summary>List of device IDs currently tracked by the AVL forest.</summary>
    public List<string> GetTrackedDevices()
    {
        lock (_lock)
        {
            return _treesByDevice.Keys.ToList();
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _treesByDevice.Clear();
        }
    }
}