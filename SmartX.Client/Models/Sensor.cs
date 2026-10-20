namespace SmartX.Client.Models;

public class Sensor
{
    public string Id { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public string MacAddress { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool IsOnline { get; set; } = true;
    public DateTime LastTelemetryReceived { get; set; } = DateTime.UtcNow;
}
