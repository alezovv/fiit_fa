using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.RedBlackTree;
/* 
h <= 2 * log(n + 1)
1 - Каждый узел либо красный, либо черный.

2 - Корень всегда черный.

3 - Листья всегда черные.

4 - Красный узел не может иметь красного сына.

5 - Черная высота одинакова: любой путь от узла до любого его листа содержит одинаковое 
количество черных узлов. */

public class RedBlackTree<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, RbNode<TKey, TValue>>
{   
    public RbColor GetColor(RbNode<TKey, TValue>? node) 
    => node?.Color ?? RbColor.Black;

    protected override RbNode<TKey, TValue> CreateNode(TKey key, TValue value)
    {
        return new RbNode<TKey, TValue>(key, value);
    }
    
    protected override void OnNodeAdded(RbNode<TKey, TValue> newNode)
    {
        FixInsert(newNode);
    }
    protected override void OnNodeRemoved(RbNode<TKey, TValue>? parent, RbNode<TKey, TValue>? child, 
        RbNode<TKey, TValue>? deletedNode)
    {
        if (deletedNode == null) return;

        RbColor colorOfDeleted = deletedNode.Color;
    
        if (colorOfDeleted == RbColor.Black && GetColor(child) == RbColor.Red)
        {
            child!.Color = RbColor.Black;
            return;
        }

        if (colorOfDeleted == RbColor.Black)
        {
            FixDelete(child, parent);
        }
    }

#region Insert
    protected void FixInsert(RbNode<TKey, TValue> node)
    {
        // пока папа красный есть косяки
        while (GetColor(node.Parent) == RbColor.Red)
        {
            // папа - левый ребенок
            if (node.Parent == node.Grandparent!.Left)
            {
                node = HandleLeftParent(node);
            }
            else
            {
                node = HandleRightParent(node);
            }
        }
        Root!.Color = RbColor.Black;
    }
    
    // если узел и его отец красные, и отец левый ребенок
    private RbNode<TKey, TValue> HandleLeftParent(RbNode<TKey, TValue> node)
    {
        var uncle = node.Uncle; // справа от отца

        // дядя красный
        if (GetColor(uncle) == RbColor.Red)
        {
            // папа и дядя становятся черными
            node.Parent!.Color = RbColor.Black;
            uncle!.Color = RbColor.Black;

            // дед становится красным
            node.Grandparent!.Color = RbColor.Red;

            return node.Grandparent; 
        }

        // дядя черный или null и правый ребенок
        if (node == node.Parent!.Right)
        {
            // вращаем батю
            node = node.Parent;
            RotateLeft(node);
        }

        // дядя черный и левый ребенок
        node.Parent!.Color = RbColor.Black;

        node.Grandparent!.Color = RbColor.Red;
        // крутим деда
        RotateRight(node.Grandparent);

        return node;
    }

    // зеркальное все
    private RbNode<TKey, TValue> HandleRightParent(RbNode<TKey, TValue> node)
    {
        var uncle = node.Uncle;

        if (GetColor(uncle) == RbColor.Red)
        {
            node.Parent!.Color = RbColor.Black;
            uncle!.Color = RbColor.Black; 
            node.Grandparent!.Color = RbColor.Red;
            return node.Grandparent;
        }

        if (node == node.Parent!.Left)
        {
            node = node.Parent;
            RotateRight(node);
        }

        node.Parent!.Color = RbColor.Black;
        node.Grandparent!.Color = RbColor.Red;

        RotateLeft(node.Grandparent!);
        return node;
    }

    #endregion 

    #region delete

    // удаленный был черным
    protected void FixDelete(RbNode<TKey, TValue>? node, RbNode<TKey, TValue>? parent)
    {
        // если не корень и не вернули должок по черным
        while (node != Root && GetColor(node) == RbColor.Black)
        {
            // отец черный и узел черный
            if (node == parent?.Left)
            {
                var sibling = parent!.Right;
                // брат красный (1)
                if (GetColor(sibling) == RbColor.Red)   
                {
                    sibling!.Color = RbColor.Black;
                    parent.Color = RbColor.Red;
                    RotateLeft(parent);
                    sibling = parent.Right; 
                }
                // дети брата оба черные (2)
                if (GetColor(sibling?.Left) == RbColor.Black && 
                    GetColor(sibling?.Right) == RbColor.Black)
                {
                    if (sibling != null) sibling.Color = RbColor.Red;
                    node = parent;
                    parent = node?.Parent;
                }
                else
                {
                    // дальний черный, ближний красный (3)
                    if (GetColor(sibling?.Right) == RbColor.Black)
                    {
                        if (sibling?.Left != null) sibling.Left.Color = RbColor.Black;
                        if (sibling != null) sibling.Color = RbColor.Red;
                        RotateRight(sibling!);
                        sibling = parent.Right;
                    }
                    // дальний стал или был красным (4)
                    if (sibling != null && parent != null)
                    {
                        sibling.Color = parent.Color;
                        parent.Color = RbColor.Black;
                        if (sibling.Right != null) sibling.Right.Color = RbColor.Black;

                        RotateLeft(parent);
                    }

                    node = Root;
                }
            }
            else
            {
                var sibling = parent?.Left;

                // Брат красный
                if (GetColor(sibling) == RbColor.Red)
                {
                    sibling!.Color = RbColor.Black;
                    parent!.Color = RbColor.Red;
                    RotateRight(parent);
                    sibling = parent.Left;
                }

                // Оба племянника черные
                if (GetColor(sibling?.Left) == RbColor.Black && 
                    GetColor(sibling?.Right) == RbColor.Black)
                {
                    if (sibling != null) sibling.Color = RbColor.Red;
                    node = parent;
                    parent = node?.Parent;
                }
                else
                {
                    // Дальний (Left) черный
                    if (GetColor(sibling?.Left) == RbColor.Black)
                    {
                        if (sibling?.Right != null) sibling.Right.Color = RbColor.Black;
                        if (sibling != null) sibling.Color = RbColor.Red;
                        RotateLeft(sibling!);
                        sibling = parent?.Left;
                    }

                    // Дальний (Left) красный
                    if (sibling != null)
                    {
                        sibling.Color = parent!.Color;
                        parent.Color = RbColor.Black;
                        if (sibling.Left != null) sibling.Left.Color = RbColor.Black;
                        RotateRight(parent);
                    }
                    node = Root;
                }
            }
        }
        
        if (node != null) node.Color = RbColor.Black;
    }

    #endregion
}
