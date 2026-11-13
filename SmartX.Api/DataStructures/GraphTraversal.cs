namespace SmartX.Api.DataStructures;

/// <summary>
/// BFS and DFS traversal over the MeshGraph.
///
/// BFS (Breadth-First Search) uses a Queue (FIFO), so it visits nodes
/// level by level. This is the correct choice for answering "which devices
/// are reachable from this gateway, and how many hops away are they?" — which
/// is exactly what the Part 3 brief calls "identify all devices currently
/// connected to a specific subnet or gateway".
///
/// DFS (Depth-First Search) uses recursion (which is a Stack under the hood),
/// so it dives deep before backtracking. This is the correct choice for
/// "which nodes are in a separate subnet / island" — if DFS from a node
/// misses some nodes, those nodes are on a different subnet.
///
/// Both algorithms use a HashSet "visited" set to prevent infinite loops
/// because a mesh network has cycles (a node may be reachable via multiple
/// paths), and revisiting a node would loop forever.
/// </summary>
public static class GraphTraversal
{
    public class TraversalResult
    {
        public string StartNode { get; set; } = string.Empty;
        public List<string> VisitOrder { get; set; } = new();
        public Dictionary<string, int> HopsFromStart { get; set; } = new();
        public HashSet<string> Visited { get; set; } = new();
        public List<string> Unreachable { get; set; } = new();
    }

    /// <summary>
    /// Breadth-First Search from the given start node.
    /// Uses a Queue&lt;string&gt; to enforce FIFO order.
    /// </summary>
    public static TraversalResult Bfs(MeshGraph graph, string startId)
    {
        var result = new TraversalResult { StartNode = startId };

        if (!graph.ContainsNode(startId))
            return result;

        var queue = new Queue<string>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        queue.Enqueue(startId);
        visited.Add(startId);
        result.HopsFromStart[startId] = 0;

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            result.VisitOrder.Add(current);

            foreach (var neighbour in graph.GetNeighbors(current))
            {
                if (visited.Contains(neighbour.ToId)) continue;

                visited.Add(neighbour.ToId);
                result.HopsFromStart[neighbour.ToId] =
                    result.HopsFromStart[current] + 1;

                queue.Enqueue(neighbour.ToId);
            }
        }

        result.Visited = visited;
        result.Unreachable = graph.GetAllNodes()
            .Where(n => !visited.Contains(n.Id))
            .Select(n => n.Id)
            .ToList();

        return result;
    }

    /// <summary>
    /// Depth-First Search from the given start node (recursive).
    /// Uses a HashSet&lt;string&gt; to prevent revisiting — a mesh graph has cycles.
    /// </summary>
    public static TraversalResult Dfs(MeshGraph graph, string startId)
    {
        var result = new TraversalResult { StartNode = startId };

        if (!graph.ContainsNode(startId))
            return result;

        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var hops = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var order = new List<string>();

        DfsRecursive(graph, startId, visited, hops, order, 0);

        result.VisitOrder = order;
        result.HopsFromStart = hops;
        result.Visited = visited;
        result.Unreachable = graph.GetAllNodes()
            .Where(n => !visited.Contains(n.Id))
            .Select(n => n.Id)
            .ToList();

        return result;
    }

    private static void DfsRecursive(
        MeshGraph graph,
        string current,
        HashSet<string> visited,
        Dictionary<string, int> hops,
        List<string> order,
        int depth)
    {
        visited.Add(current);
        hops[current] = depth;
        order.Add(current);

        foreach (var neighbour in graph.GetNeighbors(current))
        {
            if (visited.Contains(neighbour.ToId)) continue;
            DfsRecursive(graph, neighbour.ToId, visited, hops, order, depth + 1);
        }
    }

    /// <summary>
    /// Find the shortest hop path between two nodes using BFS
    /// (BFS on an unweighted graph gives the shortest path).
    /// Returns null if no path exists.
    /// </summary>
    public static List<string>? FindShortestPath(MeshGraph graph, string fromId, string toId)
    {
        if (!graph.ContainsNode(fromId) || !graph.ContainsNode(toId))
            return null;

        if (fromId.Equals(toId, StringComparison.OrdinalIgnoreCase))
            return new List<string> { fromId };

        var queue = new Queue<string>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var parent = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        queue.Enqueue(fromId);
        visited.Add(fromId);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current.Equals(toId, StringComparison.OrdinalIgnoreCase))
                break;

            foreach (var neighbour in graph.GetNeighbors(current))
            {
                if (visited.Contains(neighbour.ToId)) continue;

                visited.Add(neighbour.ToId);
                parent[neighbour.ToId] = current;
                queue.Enqueue(neighbour.ToId);
            }
        }

        if (!visited.Contains(toId))
            return null;

        // Walk the parent chain backwards.
        var path = new List<string>();
        var cursor = toId;
        while (!cursor.Equals(fromId, StringComparison.OrdinalIgnoreCase))
        {
            path.Add(cursor);
            if (!parent.TryGetValue(cursor, out var next)) return null;
            cursor = next;
        }
        path.Add(fromId);
        path.Reverse();
        return path;
    }

    /// <summary>
    /// Return all nodes that are NOT reachable from the given start node.
    /// These represent devices that are down or cut off from the gateway.
    /// </summary>
    public static List<string> FindUnreachable(MeshGraph graph, string startId)
    {
        var bfs = Bfs(graph, startId);
        return bfs.Unreachable;
    }
}