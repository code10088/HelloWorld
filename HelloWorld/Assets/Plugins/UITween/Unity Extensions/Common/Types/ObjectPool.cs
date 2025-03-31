using System;
using System.Collections.Generic;

namespace UnityExtensions
{
    public abstract class BaseObjectPool<T> where T : class
    {
        Stack<T> _objects;


        public int count => _objects.Count;


        public BaseObjectPool(int initialQuantity = 0)
        {
            _objects = new Stack<T>(initialQuantity > 16 ? initialQuantity : 16);
            AddObjects(initialQuantity);
        }


        protected abstract T CreateInstance();


        public void AddObjects(int quantity)
        {
            while (quantity > 0)
            {
                _objects.Push(CreateInstance());
                quantity--;
            }
        }


        public T Spawn()
        {
            return _objects.Count > 0 ? _objects.Pop() : CreateInstance();
        }


        public void Despawn(T target)
        {
            _objects.Push(target);
        }


        public TempObject GetTemp()
        {
            return new TempObject(this);
        }


        public struct TempObject : IDisposable
        {
            public T item { get; private set; }
            BaseObjectPool<T> _pool;

            public TempObject(BaseObjectPool<T> objectPool)
            {
                item = objectPool.Spawn();
                _pool = objectPool;
            }

            public static implicit operator T(TempObject temp) => temp.item;

            void IDisposable.Dispose()
            {
                _pool.Despawn(item);
                item = null;
                _pool = null;
            }
        }

    } // class BaseObjectPool

    public class ObjectPool<T> : BaseObjectPool<T> where T : class, new()
    {
        protected override T CreateInstance() => new T();

        public ObjectPool(int initialQuantity = 0) : base(initialQuantity) { }

    } // class ObjectPool

} // namespace UnityExtensions