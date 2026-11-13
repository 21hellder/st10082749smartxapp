# Smart-X Part 3 — Network Topology and Mesh Routing

## 1. Overview

Part 3 adds a Network Topology and Mesh Routing pillar to Smart-X. The `CommandStream` and `Dashboard` pages from Parts 1 and 2 are unchanged. Part 3 delivers an AVL tree for historical telemetry range queries, a mesh graph with BFS and DFS traversal, and Prim's and Kruskal's Minimum Spanning Tree algorithms. All three are exposed through the new `/network-topology` page and a set of REST endpoints under `/api/topology`. The Home page pillar and the sidebar navigation have both been activated.

## 2. Starting Part 3

Two processes must run, same as Parts 1 and 2.

Window 1 — API:

    dotnet run --project ".\SmartX.Api\SmartX.Api.csproj"

Window 2 — Client:

    dotnet run --project ".\SmartX.Client\SmartX.Client.csproj"

Open `http://localhost:5241/network-topology`. After any client rebuild, hard refresh with Ctrl + Shift + R.

## 3. New Files

`SmartX.Api/DataStructures/` contains `AvlTree.cs`, `MeshGraph.cs`, `GraphTraversal.cs`, and `MinimumSpanningTree.cs`. Supporting files are `TopologyDto.cs`, `TelemetryRangeService.cs`, `TopologyStore.cs`, and `TopologyEndpoints.cs`. The client page is `NetworkTopology.razor`. `TelemetryProcessingService.cs` and `Program.cs` were modified to wire the AVL tree and the endpoints into the pipeline.

## 4. AVL Tree

`AvlTree.cs` is a self-balancing BST keyed by reading timestamp. `TelemetryRangeService` keeps one tree per device. `TelemetryProcessingService` calls `rangeService.Add(reading)` on every incoming reading so the trees stay current. AVL was chosen over a plain BST because timestamps arrive in increasing order, which would degenerate a plain BST into a linked list and push range queries to O(n). AVL balances on every insert and keeps the height at O(log n), making range queries O(log n + k). The four rotation cases (LL, RR, LR, RL) are in `InsertRecursive`. Endpoint: `GET /api/topology/telemetry-range?deviceId=ID&metric=M&from=ISO&to=ISO`. Diagnostics: `GET /api/topology/avl-stats`.

## 5. Mesh Graph

`MeshGraph.cs` models the mesh as an undirected weighted graph. Vertices are ESP32 sensors and the gateway. Edges are wireless links. Edge weight is hop cost. The adjacency list uses `Dictionary<string, MeshNode>` for O(1) node lookup and `Dictionary<string, List<MeshEdge>>` for O(degree) neighbour enumeration. `TopologyStore` seeds a demo mesh with 7 nodes (gateway + 5 sensors + 1 intentionally isolated sensor) and 9 edges. Endpoint: `GET /api/topology`.

## 6. BFS and DFS

`GraphTraversal.cs` provides BFS using `Queue<string>` and DFS using recursion. Both use a `HashSet<string>` visited set because a mesh has cycles. BFS is used for shortest-path routing and for identifying unreachable nodes. Endpoints: `GET /api/topology/traverse/bfs?start=ID`, `GET /api/topology/traverse/dfs?start=ID`, `GET /api/topology/path?from=ID&to=ID`, `GET /api/topology/unreachable?start=ID`.

## 7. Prim's and Kruskal's MST

`MinimumSpanningTree.cs` implements both. Prim's uses `PriorityQueue<MeshEdge, double>` to grow from a start node. Kruskal's sorts edges by weight and uses Union-Find with path compression and union by rank to detect cycles. Both are implemented so the marker can confirm they produce identical total weight on the same graph. Endpoints: `GET /api/topology/mst/prim?start=ID`, `GET /api/topology/mst/kruskal`.

## 8. Network Topology Page

`NetworkTopology.razor` renders the graph as SVG with no external JavaScript libraries. Nodes are circles (gateway larger, unreachable nodes red). Edges are lines (MST green, shortest path yellow, normal grey). Clicking a node selects it. Four control panels sit alongside the graph: BFS/DFS traversal, Prim/Kruskal MST, shortest-hop path finder, and an AVL range query panel with a results table. The page defines local DTO classes matching the API's `TopologyDto` shapes so the client does not need a project reference.

## 9. Testing Part 3

Send telemetry to populate the AVL trees:

    Invoke-RestMethod -Uri "http://localhost:5187/api/telemetry/double" -Method POST -ContentType "application/json" -Body '{"sensorId":"ESP32-001","deviceId":"ESP32-001","metric":"Moisture","value":45.0,"unit":"%"}'

Verify the graph:

    Invoke-RestMethod -Uri "http://localhost:5187/api/topology"

Verify BFS, DFS, and path:

    Invoke-RestMethod -Uri "http://localhost:5187/api/topology/traverse/bfs?start=GATEWAY-01"
    Invoke-RestMethod -Uri "http://localhost:5187/api/topology/traverse/dfs?start=GATEWAY-01"
    Invoke-RestMethod -Uri "http://localhost:5187/api/topology/path?from=ESP32-001&to=ESP32-004"

Verify Prim and Kruskal produce identical total weight:

    Invoke-RestMethod -Uri "http://localhost:5187/api/topology/mst/prim"
    Invoke-RestMethod -Uri "http://localhost:5187/api/topology/mst/kruskal"

Verify the AVL range query:

    Invoke-RestMethod -Uri "http://localhost:5187/api/topology/avl-stats"

## 10. Walkthrough

Open `/network-topology`. Confirm 7 nodes, 9 edges, 1 unreachable. Click Run BFS — visit order starts with GATEWAY-01, unreachable lists ESP32-006. Click Run DFS — order differs from BFS. Click Prim's — MST edges turn green, edge list excludes ESP32-006. Click Kruskal's — total weight matches Prim's. Set path From ESP32-001 To ESP32-004, click Find Path — yellow highlight appears. Choose a device and metric, set the date range, click Query — readings return with the AVL tree height in the header.

## 11. Summary

Part 3 completes the Smart-X project. The AVL tree makes historical range queries fast. The mesh graph with BFS, DFS, and shortest path provides full traversal coverage. Prim's and Kruskal's MST deliver the most power-efficient routing tree for the mesh. The SVG-rendered topology page makes every algorithm visible and interactive in the browser.
