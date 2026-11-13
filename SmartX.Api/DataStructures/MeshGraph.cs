namespace SmartX.Api.DataStructures;

/// <summary>
/// Undirected weighted graph representing the physical IoT mesh network.
/// Vertices = ESP32 sensors and the gateway // Edges = wireless links.
/// Edge weight = hop cost (lower = better; a strong-signal link has low weight).
///
/// Backed by an adjacency list:
///   - Dictionary&lt;string, MeshNode&gt; for O(1) node lookup by ID/MAC
///   - Dictionary&lt;string, List&lt;MeshEdge&gt;&gt; for O(degree) neighbour lookup
///
/// This is the graph the BFS / DFS / Prim / Kruskal algorithms operate on.
/// </summary>
public class MeshGraph
{
    public class MeshNode
    {
        public string Id { get; set; } = string.Empty;
        public string MacAddress { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public bool IsOnline { get; set; } = true;

        // Optional visual coordinates (0..1 normalised) for the client SVG rendering.
        public double X { get; set; }
        public double Y { get; set; }
    }

    public class MeshEdge
    {
        public string FromId { get; set; } = string.Empty;
        public string ToId { get; set; } = string.Empty;
        public double Weight { get; set; }          // hop cost — lower is better
        public double SignalStrengthDbm { get; set; } // e.g. -45 dBm
    }

    private readonly Dictionary<string, MeshNode> _nodes =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, List<MeshEdge>> _adjacency =
        new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, MeshNode> Nodes => _nodes;

    public int NodeCount => _nodes.Count;

    public int EdgeCount { get; private set; }

    public void AddNode(MeshNode node)
    {
        if (string.IsNullOrWhiteSpace(node.Id)) return;
        if (_nodes.ContainsKey(node.Id)) return;

        _nodes[node.Id] = node;
        _adjacency[node.Id] = new List<MeshEdge>();
    }

    public void AddEdge(string fromId, string toId, double weight, double rssi = 0)
    {
        if (!_nodes.ContainsKey(fromId) || !_nodes.ContainsKey(toId)) return;
        if (fromId.Equals(toId, StringComparison.OrdinalIgnoreCase)) return;

        // Undirected graph — do not add a duplicate edge in either direction.
        var existing = _adjacency[fromId].FirstOrDefault(e =>
            e.ToId.Equals(toId, StringComparison.OrdinalIgnoreCase));
        if (existing is not null) return;

        _adjacency[fromId].Add(new MeshEdge
        {
            FromId = fromId,
            ToId = toId,
            Weight = weight,
            SignalStrengthDbm = rssi
        });

        _adjacency[toId].Add(new MeshEdge
        {
            FromId = toId,
            ToId = fromId,
            Weight = weight,
            SignalStrengthDbm = rssi
        });

        EdgeCount++;
    }

    public MeshNode? GetNode(string id) =>
        _nodes.TryGetValue(id, out var node) ? node : null;

    public List<MeshEdge> GetNeighbors(string id) =>
        _adjacency.TryGetValue(id, out var edges) ? edges : new List<MeshEdge>();

    public IEnumerable<MeshNode> GetAllNodes() => _nodes.Values;

    /// <summary>
    /// Return each undirected edge exactly once (deduped by lower ID first),
    /// so a client rendering loop does not draw two lines per connection.
    /// </summary>
    public IEnumerable<MeshEdge> GetAllEdges()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var edges in _adjacency.Values)
        {
            foreach (var edge in edges)
            {
                var key = string.Compare(edge.FromId, edge.ToId,
                    StringComparison.OrdinalIgnoreCase) < 0
                    ? $"{edge.FromId}|{edge.ToId}"
                    : $"{edge.ToId}|{edge.FromId}";

                if (seen.Add(key))
                    yield return edge;
            }
        }
    }

    public bool ContainsNode(string id) => _nodes.ContainsKey(id);

    public void Clear()
    {
        _nodes.Clear();
        _adjacency.Clear();
        EdgeCount = 0;
    }
}