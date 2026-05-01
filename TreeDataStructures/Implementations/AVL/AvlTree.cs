using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.AVL;

public class AvlTree<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, AvlNode<TKey, TValue>>
    where TKey : IComparable<TKey>
{
    protected override AvlNode<TKey, TValue> CreateNode(TKey key, TValue value)
        => new(key, value);

    protected override void OnNodeAdded(AvlNode<TKey, TValue> newNode)
    {
        RebalanceFrom(newNode.Parent);
    }

    protected override void OnNodeRemoved(AvlNode<TKey, TValue>? parent, AvlNode<TKey, TValue>? child)
    {
        RebalanceFrom(parent ?? child);
    }

    private static int Height(AvlNode<TKey, TValue>? node) => node?.Height ?? 0;

    private static int Balance(AvlNode<TKey, TValue>? node)
        => node == null ? 0 : Height(node.Left) - Height(node.Right);

    private static void UpdateHeight(AvlNode<TKey, TValue> node)
    {
        node.Height = 1 + Math.Max(Height(node.Left), Height(node.Right));
    }

    private void RebalanceFrom(AvlNode<TKey, TValue>? start)
    {
        AvlNode<TKey, TValue>? current = start;

        while (current != null)
        {
            UpdateHeight(current);
            int balance = Balance(current);

            if (balance > 1)
            {
                if (Balance(current.Left) < 0)
                {
                    AvlNode<TKey, TValue> left = current.Left!;
                    RotateLeft(left);
                    UpdateHeight(left);
                    if (left.Parent != null)
                        UpdateHeight(left.Parent);
                }

                AvlNode<TKey, TValue> oldRoot = current;
                RotateRight(oldRoot);
                UpdateHeight(oldRoot);
                if (oldRoot.Parent != null)
                    UpdateHeight(oldRoot.Parent);

                current = oldRoot.Parent?.Parent;
                continue;
            }

            if (balance < -1)
            {
                if (Balance(current.Right) > 0)
                {
                    AvlNode<TKey, TValue> right = current.Right!;
                    RotateRight(right);
                    UpdateHeight(right);
                    if (right.Parent != null)
                        UpdateHeight(right.Parent);
                }

                AvlNode<TKey, TValue> oldRoot = current;
                RotateLeft(oldRoot);
                UpdateHeight(oldRoot);
                if (oldRoot.Parent != null)
                    UpdateHeight(oldRoot.Parent);

                current = oldRoot.Parent?.Parent;
                continue;
            }

            current = current.Parent;
        }
    }
}