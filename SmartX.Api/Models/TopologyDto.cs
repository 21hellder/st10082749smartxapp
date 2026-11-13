namespace SmartX.Api.Models;

/// <summary>
/// Data Transfer Objects for the Network Topology feature.
/// The client consumes these shapes directly, so field names match
/// what NetworkTopology.razor expects.
/// </summary>
public class TopologyNodeDto
{
    public string Id { get; set; } = string.Empty;
    public string MacAddress { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool IsOnline { get; set; } = true;
    public double X { get; set; }
    public double Y { get; set; }
    public bool IsGateway { get; set; }
}

public class TopologyEdgeDto
{
    public string FromId { get; set; } = string.Empty;
    public string ToId { get; set; } = string.Empty;
    public double Weight { get; set; }
    public double SignalStrengthDbm { get; set; }
}

public class TopologyGraphDto
{
    public List<TopologyNodeDto> Nodes { get; set; } = new();
    public List<TopologyEdgeDto> Edges { get; set; } = new();
    public int NodeCount { get; set; }
    public int EdgeCount { get; set; }
}

public class TraversalResultDto
{
    public string StartNode { get; set; } = string.Empty;
    public List<string> VisitOrder { get; set; } = new();
    public Dictionary<string, int> HopsFromStart { get; set; } = new();
    public List<string> Unreachable { get; set; } = new();
    public string Algorithm { get; set; } = string.Empty;
}

public class PathResultDto
{
    public string FromId { get; set; } = string.Empty;
    public string ToId { get; set; } = string.Empty;
    public List<string> Path { get; set; } = new();
    public int HopCount { get; set; }
    public bool Found { get; set; }
}

public class MstEdgeDto
{
    public string FromId { get; set; } = string.Empty;
    public string ToId { get; set; } = string.Empty;
    public double Weight { get; set; }
}

public class MstResultDto
{
    public string Algorithm { get; set; } = string.Empty;
    public List<MstEdgeDto> Edges { get; set; } = new();
    public double TotalWeight { get; set; }
    public int NodeCount { get; set; }
    public int EdgeCount { get; set; }
}

public class TelemetryRangeRequest
{
    public string DeviceId { get; set; } = string.Empty;
    public string? Metric { get; set; }
    public DateTime From { get; set; }
    public DateTime To { get; set; }
}

public class TelemetryRangeResultDto
{
    public string DeviceId { get; set; } = string.Empty;
    public string? Metric { get; set; }
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public List<TelemetryReading> Readings { get; set; } = new();
    public int Count { get; set; }
    public int TreeHeight { get; set; }
    public int TreeValueCount { get; set; }
}