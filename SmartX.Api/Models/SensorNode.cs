namespace SmartX.Api.Models;

public class SensorNode
{
    public string DeviceId { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public List<SensorNode> Children { get; set; } = new();
}