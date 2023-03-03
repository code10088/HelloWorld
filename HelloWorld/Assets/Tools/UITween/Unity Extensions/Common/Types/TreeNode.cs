using System;
using UnityEngine;

#if !UNITY_2019_3_OR_NEWER
using SerializeReference = System.NonSerializedAttribute;
#endif

namespace UnityExtensions
{
    /// <summary>
    /// TreeNode
    /// </summary>
    [Serializable]
    public class TreeNode<Node> where Node : TreeNode<Node>
    {
        [SerializeReference] Node _parent;
        [SerializeReference] Node _next;
        [SerializeReference] Node _previous;        // the previous of first child references the last child
        [SerializeReference] Node _firstChild;


#region Enumerable & Enumerator

        public struct ChildrenEnumerable
        {
            Node _node;

            internal ChildrenEnumerable(Node node)
            {
                _node = node;
            }

            public ChildrenEnumerator GetEnumerator()
            {
                return new ChildrenEnumerator(_node);
            }
        }


        public struct ChildrenEnumerator
        {
            Node _root;

            internal ChildrenEnumerator(Node node)
            {
                _root = node;
                Current = null;
            }

            public Node Current { get; private set; }

            public bool MoveNext()
            {
                if (Current != null)
                {
                    if (Current._firstChild != null)
                    {
                        Current = Current._firstChild;
                        return true;
                    }
                    else
                    {
                        while (Current != _root)
                        {
                            if (Current.next != null)
                            {
                                Current = Current._next;
                                return true;
                            }
                            else
                            {
                                Current = Current._parent;
                            }
                        }
                        Current = null;
                        return false;
                    }
                }
                else
                {
                    Current = _root;
                    return true;
                }
            }

            public void Reset()
            {
                Current = null;
            }
        }


        public struct ParentsEnumerable
        {
            Node _node;

            internal ParentsEnumerable(Node node)
            {
                _node = node;
            }

            public ParentsEnumerator GetEnumerator()
            {
                return new ParentsEnumerator(_node);
            }
        }


        public struct ParentsEnumerator
        {
            Node _node;

            internal ParentsEnumerator(Node node)
            {
                _node = node;
                Current = null;
            }

            public Node Current { get; private set; }

            public bool MoveNext()
            {
                if (Current != null)
                {
                    Current = Current._parent;
                    return Current != null;
                }
                else
                {
                    Current = _node;
                    return true;
                }
            }

            public void Reset()
            {
                Current = null;
            }
        }


        public struct DirectChildrenEnumerable
        {
            Node _node;

            internal DirectChildrenEnumerable(Node node)
            {
                _node = node;
            }

            public DirectChildrenEnumerator GetEnumerator()
            {
                return new DirectChildrenEnumerator(_node);
            }
        }


        public struct DirectChildrenEnumerator
        {
            Node _node;

            internal DirectChildrenEnumerator(Node node)
            {
                _node = node;
                Current = null;
            }

            public Node Current { get; private set; }

            public bool MoveNext()
            {
                if (Current != null)
                {
                    Current = Current.next;
                }
                else
                {
                    Current = _node._firstChild;
                }

                return Current != null;
            }

            public void Reset()
            {
                Current = null;
            }
        }

#endregion


        /// <summary>
        /// Parent node, return null if it does not exist.
        /// </summary>
        public Node parent => _parent;


        /// <summary>
        /// Next node in the same hierarchy, return null if this node is the last one.
        /// </summary>
        public Node next => _next;


        /// <summary>
        /// Previous node in the same hierarchy, return null if this node is the first one.
        /// </summary>
        public Node previous => (_parent != null && _parent._firstChild == this) ? null : _previous;


        /// <summary>
        /// First child node, return null if no child.
        /// </summary>
        public Node firstChild => _firstChild;


        /// <summary>
        /// Last child node, return null if no child.
        /// </summary>
        public Node lastChild => _firstChild?._previous;


        /// <summary>
        /// Is this node a root node?
        /// </summary>
        public bool isRoot => _parent == null;


        /// <summary>
        /// Is this node a leaf node?
        /// </summary>
        public bool isLeaf => _firstChild == null;


        /// <summary>
        /// Number of direct children. Time complexity: O(n) - n is number of direct children.
        /// </summary>
        public int directChildCount
        {
            get
            {
                int n = 0;
                var node = _firstChild;
                while (node != null)
                {
                    n++;
                    node = node._next;
                }
                return n;
            }
        }


        /// <summary>
        /// Depth of this node. Depth of a root node is zero. Time complexity: O(n) - n is depth of this node.
        /// </summary>
        public int depth
        {
            get
            {
                int n = 0;
                var node = _parent;
                while (node != null)
                {
                    n++;
                    node = node._parent;
                }
                return n;
            }
        }


        /// <summary>
        /// Root node of this tree. Time complexity: O(n) - n is depth of this node.
        /// </summary>
        public Node root
        {
            get
            {
                var node = this as Node;
                while (node._parent != null)
                {
                    node = node._parent;
                }
                return node;
            }
        }


        /// <summary>
        /// Get a enumerable instance to foreach all children (include this node).
        /// Note: can not change the structure of this tree inside the foreach.
        /// </summary>
        public ChildrenEnumerable children => new ChildrenEnumerable(this as Node);


        /// <summary>
        /// Get a enumerable instance to foreach all parents (include this node).
        /// Note: can not change the structure of this tree inside the foreach.
        /// </summary>
        public ParentsEnumerable parents => new ParentsEnumerable(this as Node);


        /// <summary>
        /// Get a enumerable instance to foreach all direct children.
        /// Note: can not change the structure of this tree inside the foreach.
        /// </summary>
        public DirectChildrenEnumerable directChildren => new DirectChildrenEnumerable(this as Node);


        /// <summary>
        /// Attach to a specified node as the first child.
        /// Note: Use TREE_NODE_STRICT to check if the specified node is a child of this node.
        /// </summary>
        public void AttachAsFirst(Node parent)
        {
            InternalValidateAttaching(parent);

            _parent = parent;
            var self = this as Node;

            if (parent._firstChild != null)
            {
                _previous = parent._firstChild._previous;
                _next = parent._firstChild;
                parent._firstChild._previous = self;
            }
            else
            {
                _previous = self;
            }

            parent._firstChild = self;
        }


        /// <summary>
        /// Attach to a specified node as the last child.
        /// Note: Use TREE_NODE_STRICT to check if the specified node is a child of this node.
        /// </summary>
        public void AttachAsLast(Node parent)
        {
            InternalValidateAttaching(parent);

            _parent = parent;
            var self = this as Node;

            if (parent._firstChild != null)
            {
                _previous = parent._firstChild._previous;
                _previous._next = self;
                parent._firstChild._previous = self;
            }
            else
            {
                _previous = self;
                parent._firstChild = self;
            }
        }


        /// <summary>
        /// Attach to a specified node before a child of it.
        /// Note: Use TREE_NODE_STRICT to check if the specified node is a child of this node.
        /// </summary>
        public void AttachBefore(Node parent, Node next)
        {
            InternalValidateAttaching(parent);
            parent.InternalValidateChild(next);

            _parent = parent;
            var self = this as Node;

            _previous = next._previous;
            _next = next;
            next._previous = self;

            if (parent._firstChild == next)
            {
                parent._firstChild = self;
            }
        }


        /// <summary>
        /// Attach to a specified node after a child of it.
        /// Note: Use TREE_NODE_STRICT to check if the specified node is a child of this node.
        /// </summary>
        public void AttachAfter(Node parent, Node previous)
        {
            InternalValidateAttaching(parent);
            parent.InternalValidateChild(previous);

            _parent = parent;
            var self = this as Node;

            _previous = previous;
            _next = previous._next;
            previous._next = self;
        }


        /// <summary>
        /// Detach from parent node.
        /// </summary>
        public void DetachParent()
        {
            if (_parent != null)
            {
                if (_parent._firstChild == this)
                {
                    _parent._firstChild = _next;
                }
                else
                {
                    _previous._next = _next;
                }

                if (_next != null) _next._previous = _previous;

                _parent = null;
                _next = null;
                _previous = null;
            }
        }


        /// <summary>
        /// Detach from all direct children.
        /// </summary>
        public void DetachChildren()
        {
            Node child;

            child = _firstChild;
            while (child != null)
            {
                child._parent = null;
                child._next = null;
                child._previous = null;

                child = child._next;
            }

            _firstChild = null;
        }


        /// <summary>
        /// Is this node a child of a specified node?
        /// </summary>
        public bool IsChildOf(Node parent)
        {
            if (parent == null)
            {
                throw new ArgumentNullException("parent");
            }

            var node = this;
            do
            {
                if (node == parent) return true;
                node = node._parent;
            }
            while (node != null);

            return false;
        }


#region Internal

        void InternalValidateAttaching(Node parent)
        {
            if (_parent != null)
            {
                DetachParent();
            }
            if (parent == null)
            {
                throw new ArgumentNullException("parent");
            }
#if TREE_NODE_STRICT
            if (parent.IsChildOf(this))
            {
                throw new InvalidOperationException("new parent is a child of this node");
            }
#endif
        }


        void InternalValidateChild(Node node)
        {
            if (node == null)
            {
                throw new ArgumentNullException("node");
            }
            if (node._parent != this)
            {
                throw new InvalidOperationException("node is not a child of parent");
            }
        }

#endregion

    } // class TreeNode<Node>

} // namespace UnityExtensions