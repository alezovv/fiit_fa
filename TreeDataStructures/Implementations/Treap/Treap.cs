using System.Diagnostics.CodeAnalysis;
using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.Treap;

public class Treap<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, TreapNode<TKey, TValue>>
    where TKey : IComparable<TKey>
{
    private int _nextPriority = int.MaxValue;

    /// <summary>
    /// Разрезает дерево с корнем <paramref name="root"/> на два поддерева:
    /// Left: все ключи <= <paramref name="key"/>
    /// Right: все ключи > <paramref name="key"/>
    /// </summary>
    protected virtual (TreapNode<TKey, TValue>? Left, TreapNode<TKey, TValue>? Right) Split(TreapNode<TKey, TValue>? root, TKey key)
    {
        if (root == null) return (null, null);

        int cmp = Comparer.Compare(root.Key, key);
        if (cmp <= 0)
        {
            var (leftOfRight, right) = Split(root.Right, key);
            root.Right = leftOfRight;
            if (leftOfRight != null) leftOfRight.Parent = root;

            root.Parent = null;
            if (right != null) right.Parent = null;
            return (root, right);
        }
        else
        {
            var (left, rightOfLeft) = Split(root.Left, key);
            root.Left = rightOfLeft;
            if (rightOfLeft != null) rightOfLeft.Parent = root;

            if (left != null) left.Parent = null;
            root.Parent = null;
            return (left, root);
        }
    }

    /// <summary>
    /// Сливает два дерева в одно.
    /// Важное условие: все ключи в <paramref name="left"/> должны быть меньше ключей в <paramref name="right"/>.
    /// Слияние происходит на основе Priority (куча).
    /// </summary>
    protected virtual TreapNode<TKey, TValue>? Merge(TreapNode<TKey, TValue>? left, TreapNode<TKey, TValue>? right)
    {
        if (left == null)
        {
            if (right != null) right.Parent = null;
            return right;
        }
        if (right == null)
        {
            left.Parent = null;
            return left;
        }

        if (left.Priority > right.Priority)
        {
            var mergedRight = Merge(left.Right, right);
            left.Right = mergedRight;
            if (mergedRight != null) mergedRight.Parent = left;
            left.Parent = null;
            return left;
        }
        else
        {
            var mergedLeft = Merge(left, right.Left);
            right.Left = mergedLeft;
            if (mergedLeft != null) mergedLeft.Parent = right;
            right.Parent = null;
            return right;
        }
    }
    
    /// <summary>
    /// Разрез:
    /// Left: все ключи &lt; <paramref name="key"/>
    /// Right: все ключи &gt;= <paramref name="key"/>
    /// </summary>
    private (TreapNode<TKey, TValue>? Left, TreapNode<TKey, TValue>? Right) SplitLess(TreapNode<TKey, TValue>? root, TKey key)
    {
        if (root == null) return (null, null);

        int cmp = Comparer.Compare(root.Key, key);
        if (cmp < 0)
        {
            var (leftOfRight, right) = SplitLess(root.Right, key);
            root.Right = leftOfRight;
            if (leftOfRight != null) leftOfRight.Parent = root;

            root.Parent = null;
            if (right != null) right.Parent = null;
            return (root, right);
        }
        else
        {
            var (left, rightOfLeft) = SplitLess(root.Left, key);
            root.Left = rightOfLeft;
            if (rightOfLeft != null) rightOfLeft.Parent = root;

            if (left != null) left.Parent = null;
            root.Parent = null;
            return (left, root);
        }
    }

    public override void Add(TKey key, TValue value)
    {
        if (FindNode(key) != null)
            throw new ArgumentException($"This key is already there: {key}");

        TreapNode<TKey, TValue> node = CreateNode(key, value);
        var (left, right) = Split(Root, key);
        Root = Merge(Merge(left, node), right);
        if (Root != null) Root.Parent = null;

        Count++;
        OnNodeAdded(node);
    }

    public override bool Remove(TKey key)
    {
        if (Root == null) return false;

        // (<key) | (>=key)
        var (left, greaterOrEqual) = SplitLess(Root, key);
        // (==key) | (>key)
        var (equal, right) = Split(greaterOrEqual, key);

        bool removed = equal != null;
        Root = Merge(left, right);
        if (Root != null) Root.Parent = null;

        if (!removed) return false;

        Count--;
        OnNodeRemoved(null, null);
        return true;
    }

    protected override TreapNode<TKey, TValue> CreateNode(TKey key, TValue value)
    {
        var node = new TreapNode<TKey, TValue>(key, value)
        {
            Priority = _nextPriority--
        };
        return node;
    }
    protected override void OnNodeAdded(TreapNode<TKey, TValue> newNode)
    {
    }
    
    protected override void OnNodeRemoved(TreapNode<TKey, TValue>? parent, TreapNode<TKey, TValue>? child)
    {
    }
    
}