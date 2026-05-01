using System.Diagnostics.CodeAnalysis;
using TreeDataStructures.Implementations.BST;

namespace TreeDataStructures.Implementations.Splay;

public class SplayTree<TKey, TValue> : BinarySearchTree<TKey, TValue>
    where TKey : IComparable<TKey>
{
    protected override BstNode<TKey, TValue> CreateNode(TKey key, TValue value)
        => new(key, value);
    
    protected override void OnNodeAdded(BstNode<TKey, TValue> newNode)
    {
        Splay(newNode);
    }
 
    private void Splay(BstNode<TKey, TValue> x)
    {
        while (x.Parent != null)
        {
            var p = x.Parent;
            var g = p.Parent;

            if (g == null)
            {
                if (x == p.Left) RotateRight(p);
                else RotateLeft(p);
            }
            else
            {
                bool isPLeft = (g.Left == p);
                bool isXLeft = (p.Left == x);

                if (isPLeft == isXLeft)
                {
                    if (isPLeft)
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
                else
                {
                    if (isPLeft) // LR
                    {
                        RotateLeft(p);
                        RotateRight(g);
                    }
                    else // RL
                    {
                        RotateRight(p);
                        RotateLeft(g);
                    }
                }
            }
        }
        this.Root = x;
    }
    protected override void OnNodeRemoved(BstNode<TKey, TValue>? parent, BstNode<TKey, TValue>? child)
    {
        if (parent != null) Splay(parent);
    }
    
    public override bool TryGetValue(TKey key, out TValue value)
    {
        var node = FindNode(key);
        if (node == null)
        {
            value = default!;
            return false;
        }
        
        Splay(node);
        value = node.Value;
        return true;
    }
    
    }
