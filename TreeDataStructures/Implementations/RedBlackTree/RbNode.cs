using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.RedBlackTree;

public enum RbColor : byte
{
    Red   = 0,
    Black = 1
}

public class RbNode<TKey, TValue>(TKey key, TValue value)
    : Node<TKey, TValue, RbNode<TKey, TValue>>(key, value)
{   
    public RbNode<TKey, TValue>? Grandparent => Parent?.Parent;

    public RbNode<TKey, TValue>? Uncle
    {
        get
        {
            var gp = Grandparent;
            if (gp == null) return null;
            return (Parent == gp.Left) ? gp.Right : gp.Left;
        }
    }

    public RbNode<TKey, TValue>? Sibling
    {
        get
        {
            if (Parent == null) return null;
            return (this == Parent.Left) ? Parent.Right : Parent.Left;
        }
    }

    public RbColor Color { get; set; } = RbColor.Red;
}