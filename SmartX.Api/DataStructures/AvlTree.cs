namespace SmartX.Api.DataStructures;

/// <summary>
/// Self-balancing AVL Tree used by Smart-X Part 3 to store timestamped
/// telemetry readings for efficient range queries.
///
/// Why AVL and not a plain list?
/// - A list requires O(n) to scan for "readings between 14:00 and 16:00".
/// - The AVL tree self-balances on every insert (via rotations), so its
///   height stays at O(log n), and a range query becomes O(log n + k)
///   where k is the number of readings that fall inside the range.
///
/// Why AVL and not a plain BST?
/// - A plain BST degenerates into a linked list if keys are inserted in
///   sorted order (which is exactly what timestamped telemetry does).
///   The AVL tree prevents this with rotations.
///
/// Why AVL and not a Red-Black tree?
/// - Both are valid. AVL is chosen here because it produces a slightly
///   faster lookup and the balancing logic is easier to demonstrate.
/// </summary>
public class AvlTree<TKey, TValue> where TKey : IComparable<TKey>
{
    private class Node
    {
        public TKey Key = default!;
        public List<TValue> Values = new();
        public Node? Left;
        public Node? Right;
        public int Height = 1;
    }

    private Node? _root;
    private readonly object _lock = new();

    /// <summary>Number of distinct keys in the tree.</summary>
    public int KeyCount { get; private set; }

    /// <summary>Total number of values stored (across all keys).</summary>
    public int ValueCount { get; private set; }

    /// <summary>Current height of the tree — useful to prove it stays balanced.</summary>
    public int Height
    {
        get { lock (_lock) return GetHeight(_root); }
    }

    public void Insert(TKey key, TValue value)
    {
        lock (_lock)
        {
            _root = InsertRecursive(_root, key, value);
        }
    }

    private Node InsertRecursive(Node? node, TKey key, TValue value)
    {
        if (node is null)
        {
            KeyCount++;
            ValueCount++;
            return new Node { Key = key, Values = new List<TValue> { value } };
        }

        int cmp = key.CompareTo(node.Key);

        if (cmp < 0)
            node.Left = InsertRecursive(node.Left, key, value);
        else if (cmp > 0)
            node.Right = InsertRecursive(node.Right, key, value);
        else
        {
            // Same key (same timestamp) — store multiple values in one node.
            node.Values.Add(value);
            ValueCount++;
            return node;
        }

        node.Height = 1 + Math.Max(GetHeight(node.Left), GetHeight(node.Right));

        int balance = GetBalance(node);

        // Left-Left: single right rotation
        if (balance > 1 && key.CompareTo(node.Left!.Key) < 0)
            return RotateRight(node);

        // Right-Right: single left rotation
        if (balance < -1 && key.CompareTo(node.Right!.Key) > 0)
            return RotateLeft(node);

        // Left-Right: left rotation on left child, then right rotation on node
        if (balance > 1 && key.CompareTo(node.Left!.Key) > 0)
        {
            node.Left = RotateLeft(node.Left!);
            return RotateRight(node);
        }

        // Right-Left: right rotation on right child, then left rotation on node
        if (balance < -1 && key.CompareTo(node.Right!.Key) < 0)
        {
            node.Right = RotateRight(node.Right!);
            return RotateLeft(node);
        }

        return node;
    }

    /// <summary>
    /// Return all values whose key lies within [from, to] (inclusive),
    /// in ascending key order.
    ///
    /// This is the operation the Part 3 brief requires:
    /// "all moisture readings between 14:00 and 16:00 for Node A".
    /// </summary>
    public List<TValue> RangeQuery(TKey from, TKey to)
    {
        var results = new List<TValue>();
        lock (_lock)
        {
            RangeQueryRecursive(_root, from, to, results);
        }
        return results;
    }

    private void RangeQueryRecursive(Node? node, TKey from, TKey to, List<TValue> results)
    {
        if (node is null) return;

        // Only descend left if the left subtree could contain keys >= from.
        if (from.CompareTo(node.Key) < 0)
            RangeQueryRecursive(node.Left, from, to, results);

        // If current key is inside the range, add all of its values.
        if (from.CompareTo(node.Key) <= 0 && to.CompareTo(node.Key) >= 0)
            results.AddRange(node.Values);

        // Only descend right if the right subtree could contain keys <= to.
        if (to.CompareTo(node.Key) > 0)
            RangeQueryRecursive(node.Right, from, to, results);
    }

    /// <summary>Return all values in ascending key order.</summary>
    public List<TValue> InOrder()
    {
        var results = new List<TValue>();
        lock (_lock)
        {
            InOrderRecursive(_root, results);
        }
        return results;
    }

    private void InOrderRecursive(Node? node, List<TValue> results)
    {
        if (node is null) return;
        InOrderRecursive(node.Left, results);
        results.AddRange(node.Values);
        InOrderRecursive(node.Right, results);
    }

    public void Clear()
    {
        lock (_lock)
        {
            _root = null;
            KeyCount = 0;
            ValueCount = 0;
        }
    }

    // ?????????????????????????????????????????????????????????????
    // AVL balancing helpers
    // ?????????????????????????????????????????????????????????????

    private static int GetHeight(Node? node) => node?.Height ?? 0;

    private static int GetBalance(Node? node) =>
        node is null ? 0 : GetHeight(node.Left) - GetHeight(node.Right);

    private static Node RotateRight(Node y)
    {
        var x = y.Left!;
        var t2 = x.Right;
        x.Right = y;
        y.Left = t2;
        y.Height = 1 + Math.Max(GetHeight(y.Left), GetHeight(y.Right));
        x.Height = 1 + Math.Max(GetHeight(x.Left), GetHeight(x.Right));
        return x;
    }

    private static Node RotateLeft(Node x)
    {
        var y = x.Right!;
        var t2 = y.Left;
        y.Left = x;
        x.Right = t2;
        x.Height = 1 + Math.Max(GetHeight(x.Left), GetHeight(x.Right));
        y.Height = 1 + Math.Max(GetHeight(y.Left), GetHeight(y.Right));
        return y;
    }
}