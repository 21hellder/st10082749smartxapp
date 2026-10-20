namespace SmartX.Api.Models;

public class TelemetryPacket<T>
{
    public string DeviceId { get; set; } = string.Empty;

    public string SensorId { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public string Metric { get; set; } = string.Empty;

    public T Value { get; set; } = default!;

    public string Unit { get; set; } = string.Empty;
}