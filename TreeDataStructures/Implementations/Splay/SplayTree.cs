using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.Splay;

public class SplayTree<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, SplayNode<TKey, TValue>>
{
    public SplayTree() : base(null) { }
    public SplayTree(IComparer<TKey>? comparer = null) : base(comparer) { }

    protected override SplayNode<TKey, TValue> CreateNode(TKey key, TValue value) 
        => new SplayNode<TKey, TValue>(key, value);

    protected override void OnNodeAdded(SplayNode<TKey, TValue> newNode)
    {
        Splay(newNode);
    }

    protected override void OnNodeRemoved(SplayNode<TKey, TValue>? parent, SplayNode<TKey, TValue>? child)
    {
        if (parent != null)
        {
            Splay(parent);
        }
    }
    public override bool ContainsKey(TKey key)
    {
        var node = FindNode(key);
        if (node != null)
        {
            Splay(node);
            return true;
        }
        return false;
    }

    public override bool TryGetValue(TKey key, out TValue value)
    {
        var node = FindNode(key);
        if (node != null)
        {
            Splay(node);
            value = node.Value;
            return true;
        }
        value = default!;
        return false;
    }

    private void Splay(SplayNode<TKey, TValue> x)
    {
        while (x.Parent != null)
        {
            var p = x.Parent;
            var g = p.Parent;

            if (g == null) // Zig
            {
                if (x.IsLeftChild) RotateRight(p);
                else RotateLeft(p);
            }
            else if (x.IsLeftChild == p.IsLeftChild) // Zig-Zig
            {
                if (x.IsLeftChild)
                {
                    RotateRight(g);
                    RotateRight(p);
                }
                else
                {
                    RotateLeft(g);
                    RotateLeft(p);
                }
            }
            else // Zig-Zag
            {
                if (x.IsLeftChild)
                {
                    RotateRight(p);
                    RotateLeft(g);
                }
                else
                {
                    RotateLeft(p);
                    RotateRight(g);
                }
            }
        }
        this.Root = x;
    }
}