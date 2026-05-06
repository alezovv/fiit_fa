using System.Collections;
using TreeDataStructures.Interfaces;

namespace TreeDataStructures.Core;

public abstract class BinarySearchTreeBase<TKey, TValue, TNode>(IComparer<TKey>? comparer = null) 
    : ITree<TKey, TValue>
    where TNode : Node<TKey, TValue, TNode>
{
    protected TNode? Root;
    public IComparer<TKey> Comparer { get; protected set; } = comparer ?? Comparer<TKey>.Default; // use it to compare Keys

    public int Count { get; protected set; }
    
    public bool IsReadOnly => false;

    public ICollection<TKey> Keys
    {
        get
        {
            List<TKey> keys = new();
            foreach (var entry in InOrder())
                keys.Add(entry.Key);
            return keys;
        }
    }

    public ICollection<TValue> Values
    {
        get
        {
            List<TValue> values = new();
            foreach (var entry in InOrder())
                values.Add(entry.Value);
            return values;
        }
    }
    
    public virtual void Add(TKey key, TValue value)
    {
        TNode newNode = CreateNode(key, value);

        if (Root == null)
        {
            Root = newNode;
            Count++;
            OnNodeAdded(newNode);
            return;
        }

        TNode? curr = Root;
        TNode? par = null;

        int cmp;

        while (curr != null)
        {
            par = curr;
            cmp = Comparer.Compare(key, curr.Key);

            if (cmp == 0)
                throw new ArgumentException($"This key is already there: {key}");

            curr = cmp < 0 ? curr.Left : curr.Right;
        }

        if (Comparer.Compare(key, par!.Key) < 0)
            par.Left = newNode;
        else
            par.Right = newNode;

        newNode.Parent = par;

        Count++;
        OnNodeAdded(newNode);
    }

    
    public virtual bool Remove(TKey key)
    {
        TNode? node = FindNode(key);
        if (node == null) { return false; }

        RemoveNode(node);
        this.Count--;
        return true;
    }
    
    
    protected virtual void RemoveNode(TNode node)
    {
        TNode physicalRemovedNode = (node.Left == null || node.Right == null) 
            ? node 
            : FindSuccessor(node);

        TNode deletedNode = physicalRemovedNode;

        TNode? parent;
        TNode? child;

        if (node.Left == null && node.Right == null)
        {
            parent = node.Parent;
            child = null;

            if (node.Parent == null)
                Root = null;
            else if (node.IsLeftChild)
                node.Parent.Left = null;
            else
                node.Parent.Right = null;
        }
        else if (node.Left == null || node.Right == null)
        {
            child = node.Left ?? node.Right;
            Transplant(node, child);
            parent = child?.Parent;
        }
        else
        {
            TNode tmp = physicalRemovedNode; 

            if (tmp.Parent != node)
            {
                parent = tmp.Parent;
                child = tmp.Right;
                Transplant(tmp, tmp.Right);
                tmp.Right = node.Right;
                if (tmp.Right != null) tmp.Right.Parent = tmp;
            }
            else
            {
                parent = tmp;
                child = tmp.Right;
            }

            Transplant(node, tmp);
            tmp.Left = node.Left;
            if (tmp.Left != null) tmp.Left.Parent = tmp;
        }

        OnNodeRemoved(parent, child, deletedNode);
    }

    private TNode FindSuccessor(TNode node)
    {
        TNode tmp = node.Right!;
        while (tmp.Left != null) tmp = tmp.Left;
        return tmp;
    }

    public virtual bool ContainsKey(TKey key) => FindNode(key) != null;
    
    public virtual bool TryGetValue(TKey key, out TValue value)
    {
        TNode? node = FindNode(key);
        
        if (node != null)
        {
            value = node.Value;
            return true;
        }
        
        value = default!; 
        return false;
    }

    public TValue this[TKey key]
    {
        get => TryGetValue(key, out TValue? val) ? val : throw new KeyNotFoundException();
        set
        {
            TNode? node = FindNode(key);
            if (node != null)
            {
                node.Value = value;
                return;
            }
            Add(key, value);
        }
    }
    
    
    #region Hooks
    
    /// <summary>
    /// Вызывается после успешной вставки
    /// </summary>
    /// <param name="newNode">Узел, который встал на место</param>
    protected virtual void OnNodeAdded(TNode newNode) { }
    
    /// <summary>
    /// Вызывается после удаления. 
    /// </summary>
    /// <param name="parent">Узел, чей ребенок изменился</param>
    /// <param name="child">Узел, который встал на место удаленного</param>
    protected virtual void OnNodeRemoved(TNode? parent, TNode? child, TNode deletedNode) { }
    
    #endregion
    
    
    #region Helpers
    protected abstract TNode CreateNode(TKey key, TValue value);
    
    
    protected TNode? FindNode(TKey key)
    {
        TNode? current = Root;
        while (current != null)
        {
            int cmp = Comparer.Compare(key, current.Key);
            if (cmp == 0) { return current; }
            current = cmp < 0 ? current.Left : current.Right;
        }
        return null;
    }

    protected void RotateLeft(TNode x)
    {
        if (x.Right == null) return;
        
        TNode y = x.Right;
        x.Right = y.Left;

        if (y.Left != null)
        {
            y.Left.Parent = x;
        }

        y.Parent = x.Parent;
        
        if (x.Parent == null)
        {
            Root = y;
        } 
        else if (x == x.Parent.Left)
        {
            x.Parent.Left = y;
        } 
        else
        {
            x.Parent.Right = y;
        }

        y.Left = x;
        x.Parent = y;

        // throw new NotImplementedException();
    }
    
    protected void RotateRight(TNode y)
    {
        if (y.Left == null) return;

        TNode x = y.Left;
        y.Left = x.Right;
        
        if (x.Right != null)
        {
            x.Right.Parent = y;
        }
        
        x.Parent = y.Parent;
   
        if (y.Parent == null)
        {
            Root = x;
        }
        else if (y == y.Parent.Left)
        {
            y.Parent.Left = x;
        }
        else
        {
            y.Parent.Right = x;
        }
       
        x.Right = y;
        y.Parent = x;

        // throw new NotImplementedException();
    }
    
    protected void RotateBigLeft(TNode x)
    {
        if (x.Right == null || x.Right.Left == null) return;
        RotateRight(x.Right);
        RotateLeft(x);

        //throw new NotImplementedException();
    }
    
    protected void RotateBigRight(TNode x)
    {
        if (x.Left == null || x.Left.Right == null) return;
        RotateLeft(x.Left);
        RotateRight(x);

        // throw new NotImplementedException();
    }

    protected void RotateDoubleLeft(TNode x)
    {
        if (x.Left == null) return;
        
        RotateLeft(x.Left);
        RotateRight(x);

        // throw new NotImplementedException();
    }
    
    protected void RotateDoubleRight(TNode y)
    {
        if (y.Right == null) return;
    
        RotateRight(y.Right);
        RotateLeft(y);

        // throw new NotImplementedException();
    }
    
    protected void Transplant(TNode u, TNode? v)
    {
        if (u.Parent == null)
        {
            Root = v;
        }
        else if (u.IsLeftChild)
        {
            u.Parent.Left = v;
        }
        else
        {
            u.Parent.Right = v;
        }
        v?.Parent = u.Parent;
    }
    #endregion

    public IEnumerable<TreeEntry<TKey, TValue>> InOrder() 
        => new TreeIterator(Root, TraversalStrategy.InOrder);

    public IEnumerable<TreeEntry<TKey, TValue>> PreOrder() 
        => new TreeIterator(Root, TraversalStrategy.PreOrder);

    public IEnumerable<TreeEntry<TKey, TValue>> PostOrder() 
        => new TreeIterator(Root, TraversalStrategy.PostOrder);

    public IEnumerable<TreeEntry<TKey, TValue>> InOrderReverse() 
        => new TreeIterator(Root, TraversalStrategy.InOrderReverse);

    public IEnumerable<TreeEntry<TKey, TValue>> PreOrderReverse() 
        => new TreeIterator(Root, TraversalStrategy.PreOrderReverse);

    public IEnumerable<TreeEntry<TKey, TValue>> PostOrderReverse() 
        => new TreeIterator(Root, TraversalStrategy.PostOrderReverse);
    
    /// <summary>
    /// Внутренний класс-итератор. 
    /// Реализует паттерн Iterator вручную, без yield return (ban).
    /// </summary>
    private class TreeIterator : 
        IEnumerable<TreeEntry<TKey, TValue>>,
        IEnumerator<TreeEntry<TKey, TValue>>
    {
        private readonly TraversalStrategy _strategy;
        private readonly TNode? _root;
        private readonly bool _reverse;
        
        private TNode? _curr;
        private int _curDepth;
        private bool _started;
        private TreeEntry<TKey, TValue> _currentEntry;

        public TreeIterator(TNode? root, TraversalStrategy strategy)
        {
            _root = root;
            _strategy = strategy;
            _reverse = strategy is
                TraversalStrategy.InOrderReverse or
                TraversalStrategy.PreOrderReverse or
                TraversalStrategy.PostOrderReverse;
            Reset();
        }

        public IEnumerator<TreeEntry<TKey, TValue>> GetEnumerator() => new TreeIterator(_root, _strategy);
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        
        public TreeEntry<TKey, TValue> Current => _currentEntry;
        object IEnumerator.Current => Current;
        
        public bool MoveNext() => _strategy switch
        {
            TraversalStrategy.InOrder => MoveNextInOrder(),
            TraversalStrategy.PreOrder => MoveNextPreOrder(),
            TraversalStrategy.PostOrder => MoveNextPostOrder(),
            TraversalStrategy.InOrderReverse => MoveNextInOrder(),
            TraversalStrategy.PreOrderReverse => MoveNextPreOrder(),
            TraversalStrategy.PostOrderReverse => MoveNextPostOrder(),
            _ => throw new NotSupportedException($"Стратегия {_strategy} не поддерживается")
        };
        
        private void SetCurrent(TNode node, int depth)
        {
            _curr = node;
            _curDepth = depth;
            _currentEntry = new TreeEntry<TKey, TValue>(node.Key, node.Value, depth);
        }
        
        private TNode? FirstChild(TNode node) => _reverse ? node.Right : node.Left;
        private TNode? SecondChild(TNode node) => _reverse ? node.Left : node.Right;

        #region InOrder (Центрированный обход)
        
        private bool MoveNextInOrder()
        {
            if (_root == null) return false;

            if (!_started)
            {
                _started = true;
                _curr = _root;
                _curDepth = 0;
                
                // Спускаемся к самому левому (или правому при reverse) узлу
                while (FirstChild(_curr!) != null)
                {
                    _curr = FirstChild(_curr!);
                    _curDepth++;
                }

                if (_curr != null)
                {
                    SetCurrent(_curr, _curDepth);
                    return true;
                }
                return false;
            }

            if (_curr == null) return false;
            
            // Если есть правый потомо идем туда и максимально влево
            if (SecondChild(_curr) != null)
            {
                _curr = SecondChild(_curr);
                _curDepth++;

                while (FirstChild(_curr!) != null)
                {
                    _curr = FirstChild(_curr!);
                    _curDepth++;
                }

                SetCurrent(_curr!, _curDepth);
                return true;
            }

            // Поднимаемся вверх пока не найдем узел где мы были левым потомком
            while (_curr.Parent != null)
            {
                _curDepth--;
                if (_curr == FirstChild(_curr.Parent))
                {
                    _curr = _curr.Parent;
                    SetCurrent(_curr, _curDepth);
                    return true;
                }
                _curr = _curr.Parent;
            }

            _curr = null;
            return false;
        }

        #endregion

        #region PreOrder (Прямой обход)

        private bool MoveNextPreOrder()
        {
            if (_root == null) return false;

            if (!_started)
            {
                _curr = _root;
                _curDepth = 0;
                _started = true;
                SetCurrent(_curr, _curDepth);
                return true;
            }

            if (_curr == null) return false;

            if (FirstChild(_curr) != null)
            {
                _curr = FirstChild(_curr);
                _curDepth++;
                SetCurrent(_curr!, _curDepth);
                return true;
            }
            else if (SecondChild(_curr) != null)
            {
                _curr = SecondChild(_curr);
                _curDepth++;
                SetCurrent(_curr!, _curDepth);
                return true;
            }
            else
            {
                while (_curr.Parent != null)
                {
                    if (_curr == FirstChild(_curr.Parent) && SecondChild(_curr.Parent) != null)
                    {
                        _curr = SecondChild(_curr.Parent);
                        SetCurrent(_curr!, _curDepth);
                        return true;
                    }

                    _curr = _curr.Parent;
                    _curDepth--;
                }

                _curr = null;
                return false;
            }
        }

        #endregion

        #region PostOrder (Обратный обход)

        private bool MoveNextPostOrder()
        {
            if (_root == null) return false;

            if (!_started)
            {
                _started = true;
                _curr = _root;
                _curDepth = 0;
                
                // Спускаемся к самому левому узлу
                while (FirstChild(_curr!) != null || SecondChild(_curr!) != null)
                {
                    if (FirstChild(_curr!) != null)
                    {
                        _curr = FirstChild(_curr!);
                    }
                    else
                    {
                        _curr = SecondChild(_curr!);
                    }
                    _curDepth++;
                }

                SetCurrent(_curr!, _curDepth);
                return true;
            }

            if (_curr == null || _curr.Parent == null)
            {
                _curr = null;
                return false;
            }

            // Если мы были левым потомком и есть правый потомок
            if (_curr == FirstChild(_curr.Parent) && SecondChild(_curr.Parent) != null)
            {
                _curr = SecondChild(_curr.Parent);
                
                // Спускаемся максимально влево от правого потомка
                while (FirstChild(_curr!) != null || SecondChild(_curr!) != null)
                {
                    if (FirstChild(_curr!) != null)
                    {
                        _curr = FirstChild(_curr!);
                    }
                    else
                    {
                        _curr = SecondChild(_curr!);
                    }
                    _curDepth++;
                }

                SetCurrent(_curr!, _curDepth);
                return true;
            }

            // Иначе поднимаемся к родителю
            _curDepth--;
            _curr = _curr.Parent;
            SetCurrent(_curr, _curDepth);
            return true;
        }

        #endregion

        public void Reset()
        {
            _curr = null;
            _curDepth = 0;
            _started = false;
            _currentEntry = default!;
        }

        public void Dispose()
        {
            Reset();
        }
    }
    
    
    private enum TraversalStrategy { InOrder, PreOrder, PostOrder, InOrderReverse, PreOrderReverse, PostOrderReverse }
    
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        return new KeyValuePairIterator(this);
    }
    
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private class KeyValuePairIterator : IEnumerator<KeyValuePair<TKey, TValue>>
    {
        private readonly TreeIterator _iterator;
        
        public KeyValuePairIterator(BinarySearchTreeBase<TKey, TValue, TNode> tree)
        {
            _iterator = new TreeIterator(tree.Root, TraversalStrategy.InOrder);
        }
        
        public KeyValuePair<TKey, TValue> Current => 
            new KeyValuePair<TKey, TValue>(_iterator.Current.Key, _iterator.Current.Value);
        
        object IEnumerator.Current => Current;
        
        public bool MoveNext() => _iterator.MoveNext();
        public void Reset() => _iterator.Reset();
        public void Dispose() => _iterator.Dispose();
    }

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        using var iterator = new TreeIterator(Root, TraversalStrategy.InOrder);
        while (iterator.MoveNext())
        {
            array[arrayIndex++] = new KeyValuePair<TKey, TValue>(
                iterator.Current.Key, 
                iterator.Current.Value
            );
        }
    }

    public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);
    public void Clear() { Root = null; Count = 0; }
    public bool Contains(KeyValuePair<TKey, TValue> item) => ContainsKey(item.Key);
    public bool Remove(KeyValuePair<TKey, TValue> item) => Remove(item.Key);
}