using SmartX.Api.DataStructures;

namespace SmartX.Api.Services;

/// <summary>
/// Singleton holder for the IoT mesh graph.
///
/// In a real deployment, the topology would be built from:
/// - Sensor registration events (each sensor reports its neighbours)
/// - RSSI measurements from telemetry (strong RSSI = short edge)
/// - A gateway heartbeat that confirms it can reach each node
///
/// For this project we seed a demo topology on startup so the client
/// has a stable, meaningful graph to visualise, traverse, and MST-ify.
///
/// The demo mesh has:
/// - 1 gateway node (GATEWAY-01) — the sink for all telemetry
/// - 6 sensor nodes (ESP32-001 through ESP32-006)
/// - Multiple redundant paths (cycles) so BFS/DFS/MST have something to do
/// - One intentionally isolated node (ESP32-006) so the client can
///   demonstrate "find unreachable devices"
/// </summary>
public class TopologyStore
{
    private readonly MeshGraph _graph = new();
    private readonly object _lock = new();

    public const string GatewayId = "GATEWAY-01";

    public TopologyStore()
    {
        SeedDemoTopology();
    }

    public MeshGraph Graph => _graph;

    private void SeedDemoTopology()
    {
        lock (_lock)
        {
            _graph.Clear();

            // Gateway (sink)
            _graph.AddNode(new MeshGraph.MeshNode
            {
                Id = GatewayId,
                MacAddress = "GW:00:00:00:00:01",
                Label = "Central Gateway",
                Category = "Gateway",
                IsOnline = true,
                X = 0.5,
                Y = 0.5
            });

            // Sensors — positioned in a rough circle around the gateway
            _graph.AddNode(new MeshGraph.MeshNode
            {
                Id = "ESP32-001",
                MacAddress = "AA:BB:CC:DD:EE:01",
                Label = "ESP32-001 (Temp)",
                Category = "Environmental",
                IsOnline = true,
                X = 0.20,
                Y = 0.20
            });

            _graph.AddNode(new MeshGraph.MeshNode
            {
                Id = "ESP32-002",
                MacAddress = "AA:BB:CC:DD:EE:02",
                Label = "ESP32-002 (Moisture)",
                Category = "Environmental",
                IsOnline = true,
                X = 0.80,
                Y = 0.20
            });

            _graph.AddNode(new MeshGraph.MeshNode
            {
                Id = "ESP32-003",
                MacAddress = "AA:BB:CC:DD:EE:03",
                Label = "ESP32-003 (Power)",
                Category = "Power Consumption",
                IsOnline = true,
                X = 0.15,
                Y = 0.70
            });

            _graph.AddNode(new MeshGraph.MeshNode
            {
                Id = "ESP32-004",
                MacAddress = "AA:BB:CC:DD:EE:04",
                Label = "ESP32-004 (Power)",
                Category = "Power Consumption",
                IsOnline = true,
                X = 0.85,
                Y = 0.70
            });

            _graph.AddNode(new MeshGraph.MeshNode
            {
                Id = "ESP32-005",
                MacAddress = "AA:BB:CC:DD:EE:05",
                Label = "ESP32-005 (Pump)",
                Category = "Actuator",
                IsOnline = true,
                X = 0.50,
                Y = 0.90
            });

            // Intentionally isolated node — demonstrates "unreachable" detection.
            // It has NO edges, so BFS from the gateway will never reach it.
            _graph.AddNode(new MeshGraph.MeshNode
            {
                Id = "ESP32-006",
                MacAddress = "AA:BB:CC:DD:EE:06",
                Label = "ESP32-006 (Isolated)",
                Category = "Environmental",
                IsOnline = false,
                X = 0.50,
                Y = 0.05
            });

            // Edges — weight = hop cost, lower is better (stronger signal).
            // Signal strength in dBm is stored for the client visualisation.

            // Gateway <-> sensors
            _graph.AddEdge(GatewayId, "ESP32-001", 1.0, -55);
            _graph.AddEdge(GatewayId, "ESP32-002", 1.5, -62);
            _graph.AddEdge(GatewayId, "ESP32-003", 2.0, -68);
            _graph.AddEdge(GatewayId, "ESP32-004", 1.8, -65);
            _graph.AddEdge(GatewayId, "ESP32-005", 2.2, -70);

            // Mesh redundancy (creates cycles so MST has something to prune)
            _graph.AddEdge("ESP32-001", "ESP32-002", 1.2, -58);
            _graph.AddEdge("ESP32-003", "ESP32-005", 1.4, -60);
            _graph.AddEdge("ESP32-004", "ESP32-005", 1.6, -63);
            _graph.AddEdge("ESP32-001", "ESP32-003", 2.5, -75);

            // ESP32-006 has no edges — it's isolated by design.
        }
    }

    /// <summary>
    /// Add a node on the fly (e.g. when a new sensor registers).
    /// </summary>
    public void AddNode(MeshGraph.MeshNode node)
    {
        lock (_lock)
        {
            _graph.AddNode(node);
        }
    }

    /// <summary>
    /// Add an edge (e.g. when two nodes report a strong RSSI between them).
    /// </summary>
    public void AddEdge(string fromId, string toId, double weight, double rssi)
    {
        lock (_lock)
        {
            _graph.AddEdge(fromId, toId, weight, rssi);
        }
    }

    /// <summary>
    /// Mark a node offline (e.g. when it misses a heartbeat window).
    /// </summary>
    public void MarkOffline(string nodeId)
    {
        lock (_lock)
        {
            var node = _graph.GetNode(nodeId);
            if (node is not null)
                node.IsOnline = false;
        }
    }
}