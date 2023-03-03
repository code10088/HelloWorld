using System;
using System.Collections.Generic;

namespace UnityExtensions
{
    /// <summary>
    /// Quick linked list, no extra node reference.
    /// </summary>
    public class LinkedList<T>
    {
        /// <summary>
        /// internal node
        /// </summary>
        public struct Node
        {
            public int previous;
            public int next;
            public T value;

            internal bool isAlone => previous == next;
        }


        Stack<int> _emptyIds;
        Node[] _array;
        int _arrayCount;
        int _first;
        int _last;


        /// <summary>
        /// The id of first node
        /// </summary>
        public int first => _first;


        /// <summary>
        /// The id of last node
        /// </summary>
        public int last => _last;


        /// <summary>
        /// Number of nodes
        /// </summary>
        public int count => _arrayCount - _emptyIds.Count;


        /// <summary>
        /// Visit node value by id
        /// </summary>
        public T this[int id]
        {
            get
            {
                if (_array[id].isAlone && _first != id)
                {
                    throw new Exception("invalid id");
                }

                return _array[id].value;
            }
            set
            {
                if (_array[id].isAlone && _first != id)
                {
                    throw new Exception("invalid id");
                }

                _array[id].value = value;
            }
        }


        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }


        public LinkedList(int capacity = 16)
        {
            _emptyIds = new Stack<int>(4);
            _array = new Node[capacity < 4 ? 4 : capacity];
            _arrayCount = 0;
            _first = -1;
            _last = -1;
        }


        /// <summary>
        /// 
        /// </summary>
        public Node GetNode(int id)
        {
            return _array[id];
        }


        /// <summary>
        /// 
        /// </summary>
        public int GetPrevious(int id)
        {
            return _array[id].previous;
        }


        /// <summary>
        /// 
        /// </summary>
        public int GetNext(int id)
        {
            return _array[id].next;
        }


        int InternalAdd(ref Node node)
        {
            int index;

            if (_emptyIds.Count > 0)
            {
                index = _emptyIds.Pop();
                _array[index] = node;
            }
            else
            {
                if (_arrayCount == _array.Length)
                {
                    Array.Resize(ref _array, _array.Length * 2);
                }
                index = _arrayCount;
                _array[_arrayCount++] = node;
            }

            return index;
        }


        void InternalRemove(int id)
        {
            var node = _array[id];

            if (_first == id) _first = node.next;
            if (_last == id) _last = node.previous;

            if (node.previous != -1)
            {
                _array[node.previous].next = node.next;
            }
            if (node.next != -1)
            {
                _array[node.next].previous = node.previous;
            }

            node.previous = -1;
            node.next = -1;
            node.value = default;
            _array[id] = node;
            _emptyIds.Push(id);
        }


        public int AddFirst(T value)
        {
            var node = new Node { previous = -1, next = _first, value = value };
            int newId = InternalAdd(ref node);

            if (_first == -1) _last = newId;
            else _array[_first].previous = newId;
            _first = newId;

            return newId;
        }


        public int AddLast(T value)
        {
            var node = new Node { previous = _last, next = -1, value = value };
            int newId = InternalAdd(ref node);

            if (_first == -1) _first = newId;
            else _array[_last].next = newId;
            _last = newId;

            return _last;
        }


        public int AddAfter(int id, T value)
        {
            if (_array[id].isAlone && _first != id)
            {
                throw new Exception("invalid id");
            }

            var prevNodeNext = _array[id].next;
            var node = new Node { previous = id, next = prevNodeNext, value = value };
            int newId = InternalAdd(ref node);

            if (prevNodeNext == -1) _last = newId;
            else _array[prevNodeNext].previous = newId;

            _array[id].next = newId;

            return newId;
        }


        public int AddBefore(int id, T value)
        {
            if (_array[id].isAlone && _first != id)
            {
                throw new Exception("invalid id");
            }

            var nextNodePrevious = _array[id].previous;
            var node = new Node { previous = nextNodePrevious, next = id, value = value };
            int newId = InternalAdd(ref node);

            if (nextNodePrevious == -1) _first = newId;
            else _array[nextNodePrevious].next = newId;
            
            _array[id].previous = newId;

            return newId;
        }


        public void Clear()
        {
            _emptyIds.Clear();
            Array.Clear(_array, 0, _arrayCount);
            _arrayCount = 0;
            _first = -1;
            _last = -1;
        }


        public void Remove(int id)
        {
            if (_array[id].isAlone && _first != id)
            {
                throw new Exception("invalid id");
            }
            InternalRemove(id);
        }


        public void RemoveFirst()
        {
            if (_first == -1)
            {
                throw new Exception("empty list");
            }
            InternalRemove(_first);
        }


        public void RemoveLast()
        {
            if (_last == -1)
            {
                throw new Exception("empty list");
            }
            InternalRemove(_last);
        }


        public struct Enumerator
        {
            Node[] _array;
            int _first;
            int _current;

            internal Enumerator(LinkedList<T> linkedList)
            {
                _array = linkedList._array;
                _first = linkedList._first;
                _current = -1;
            }

            public T Current => _array[_current].value;

            public bool MoveNext()
            {
                if (_current >= 0)
                {
                    _current = _array[_current].next;
                }
                else
                {
                    _current = _first;
                }

                return _current >= 0;
            }

            public void Reset()
            {
                throw new NotSupportedException();
            }
        }

    } // class QuickLinkedList<T>

} // namespace UnityExtensions