
using SmartX.Api.Models;

namespace SmartX.Api.Services;

public class TelemetryStore
{
    private readonly SortedDictionary<DateTime, List<TelemetryReading>>
        _readingsByTimestamp = new();

    private readonly object _lock = new();

    public void Add(TelemetryReading reading)
    {
        lock (_lock)
        {
            if (!_readingsByTimestamp.TryGetValue(
                reading.Timestamp, out var readings))
            {
                readings = new List<TelemetryReading>();
                _readingsByTimestamp.Add(reading.Timestamp, readings);
            }

            // Multiple readings can have the same timestamp.
            readings.Add(reading);
        }
    }

    public List<TelemetryReading> GetAll()
    {
        lock (_lock)
        {
            // SortedDictionary returns timestamps in ascending order.
            return _readingsByTimestamp.Values
                .SelectMany(readings => readings)
                .ToList();
        }
    }

    public List<TelemetryReading> GetLatest(int count)
    {
        if (count <= 0)
        {
            return new List<TelemetryReading>();
        }

        lock (_lock)
        {
            return _readingsByTimestamp
                .Reverse()
                .SelectMany(entry => entry.Value.AsEnumerable().Reverse())
                .Take(count)
                .ToList();
        }
    }

    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _readingsByTimestamp.Values
                    .Sum(readings => readings.Count);
            }
        }
    }
}
