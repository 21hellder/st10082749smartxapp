
namespace SmartX.Api.Models;

public class DeviceCommand
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string DeviceId { get; set; } = string.Empty;

    // Examples: Pump or Relay.
    public string Component { get; set; } = string.Empty;

    // Examples: ON or OFF.
    public string Action { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public string IssuedBy { get; set; } = "Dashboard";

    public bool IsUndone { get; set; }
}
