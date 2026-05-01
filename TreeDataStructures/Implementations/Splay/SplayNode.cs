using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.Splay;

public class SplayNode<TKey, TValue> : Node<TKey, TValue, SplayNode<TKey, TValue>>
{
    public SplayNode(TKey key, TValue value) : base(key, value)
    {
    }
}