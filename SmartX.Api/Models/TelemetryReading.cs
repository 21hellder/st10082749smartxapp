namespace SmartX.Api.Models;

public class TelemetryReading
{
    public string DeviceId { get; set; } = string.Empty;

    public string Metric { get; set; } = string.Empty;

    public double Value { get; set; }

    public string Unit { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static TelemetryReading operator +(TelemetryReading first, TelemetryReading second)
    {
        return new TelemetryReading
        {
            DeviceId = first.DeviceId,
            Metric = first.Metric,
            Value = first.Value + second.Value,
            Unit = first.Unit,
            Timestamp = DateTime.UtcNow
        };
    }

    public static TelemetryReading operator -(TelemetryReading first, TelemetryReading second)
    {
        return new TelemetryReading
        {
            DeviceId = first.DeviceId,
            Metric = first.Metric,
            Value = first.Value - second.Value,
            Unit = first.Unit,
            Timestamp = DateTime.UtcNow
        };
    }
}