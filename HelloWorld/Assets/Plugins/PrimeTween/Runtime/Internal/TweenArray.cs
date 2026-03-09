using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace PrimeTween {
    internal unsafe class TweenArray {
        TweenData[] _tweens;
        NativeArray<UnmanagedTweenData> _data;
        internal UnmanagedTweenData* _dataPtr { get; private set; }
        int _capacity;
        int numLocks;
        internal readonly string _name;
        internal int Count { get; private set; }


        public TweenArray(int capacity, string name) {
            CreateBuffers(capacity);
            _name = name;
        }

        internal bool isLocked => numLocks > 0;

        void CreateBuffers(int capacity) {
            Assert.IsFalse(isLocked);
            Assert.IsTrue(capacity >= 0);
            _tweens = new TweenData[capacity];
            _data = new NativeArray<UnmanagedTweenData>(capacity, Allocator.Persistent);
            _dataPtr = (UnmanagedTweenData*)_data.GetUnsafePtr();
            _capacity = capacity;
        }

        internal ref TweenData this[int index] {
            get {
                Assert.IsTrue(index >= 0 && index < _capacity);
                return ref _tweens[index];
            }
        }

        internal void MoveAndClearOld(TweenData tween, int oldIndex, int newIndex) {
            Assert.IsTrue(newIndex < oldIndex);
            Assert.IsTrue(oldIndex >= 0 && oldIndex < _capacity);
            Assert.IsTrue(newIndex >= 0 && newIndex < _capacity);
            Assert.AreEqual(this[oldIndex], tween);

            _data[newIndex] = _data[oldIndex];
            Assert.IsNotNull(tween.cold);
            tween.cold._index = newIndex;
            this[newIndex] = tween;
            Assert.AreEqual(tween.id, _data[newIndex].id);

            this[oldIndex] = default; // setting to null is important because ProcessAll filters nulls
            _data[oldIndex] = default;
        }

        internal void RemoveLast(ColdData cold) {
            Assert.IsFalse(isLocked);
            Assert.IsTrue(Count > 0);
            Assert.IsNotNull(cold);
            Assert.IsNotNull(cold._tweenArray);
            Assert.AreNotEqual(-1, cold._index);

            int i = Count - 1;
            Assert.AreEqual(this[i].cold, cold);
            cold._tweenArray = null;
            cold._index = -1;
            this[i] = default;
            _data[i] = default;
            Count--;
        }

        internal void TrimEndNulls(int numRemoved) {
            Assert.IsFalse(isLocked);
            for (int i = Count - numRemoved; i < Count; i++) {
                Assert.IsNull(this[i].cold);
                Assert.AreEqual(0, _data[i].id);
            }
            Count -= numRemoved;
        }

        internal void Clear() {
            Assert.IsFalse(isLocked);
            for (int i = 0; i < Count; i++) {
                this[i] = default;
                _data[i] = default;
            }
            Count = 0;
        }

        public void Add(ColdData tween) {
            Assert.IsFalse(isLocked, _name);
            Assert.IsNotNull(tween);
            int i = Count;
            Assert.IsTrue(i <= Capacity);
            if (i == Capacity) {
                Assert.AreEqual(i, _tweens.Length);
                Assert.AreEqual(i, _data.Length);
                Capacity = Capacity == 0 ? 4 : Capacity * 2;
            }
            Assert.AreEqual(0, _data[i].id);
            Assert.AreNotEqual(0, tween.id);

            tween._tweenArray = this;
            tween._index = i;
            Assert.IsTrue(tween.data.IsDefault());

            Assert.AreNotEqual(0, tween.id);
            tween.data.id = tween.id;
            this[i] = new TweenData { cold = tween };
            Count++;
            Assert.AreEqual(tween.id, tween.data.id);
        }

        public int Capacity {
            get => _capacity;
            set {
                Assert.IsFalse(isLocked);
                Assert.IsTrue(value >= 0);
                Assert.IsTrue(value >= Count);
                Assert.IsTrue(_data.IsCreated);
                if (_capacity != value) {
                    // Debug.Log($"set capacity to {value}");
                    TweenData[] oldTweens = _tweens;
                    NativeArray<UnmanagedTweenData> oldData = _data;

                    CreateBuffers(value);

                    Array.Copy(oldTweens, _tweens, Count);
                    NativeArray<UnmanagedTweenData>.Copy(oldData, _data, Count);
                    oldData.Dispose();

                    foreach (var el in this) {
                        TweenData tween = el.tween;
                        UnmanagedTweenData data = el.data;

                        Assert.IsNotNull(tween.cold);
                        Assert.AreEqual(data.id, tween.cold.data.id);
                        Assert.AreEqual(tween.id, _data[el.index].id);
                        Assert.AreEqual(tween.id, tween.cold.id);
                        Assert.AreEqual(el.index, tween.cold._index);
                    }
                }
            }
        }

        public void Dispose() {
            Assert.IsFalse(isLocked);
            Assert.IsTrue(_data.IsCreated);
            _data.Dispose();
            _data = default;
            _capacity = 0;
            Count = 0;
        }

        #if TEST_FRAMEWORK_INSTALLED
        internal ColdData Single() {
            Assert.AreEqual(1, Count);
            return this[0].cold;
        }
        #endif

        internal NativeArray<UnmanagedTweenData> GetData() => _data;
        internal ref UnmanagedTweenData GetDataAt(int index) {
            Assert.IsTrue(isLocked);
            #if UNITY_2020_1_OR_NEWER
            return ref UnsafeUtility.AsRef<UnmanagedTweenData>(_dataPtr + index);
            #else
            return ref *(_dataPtr + index);
            #endif
        }

        internal readonly struct Lock : IDisposable {
            readonly TweenArray array;

            internal Lock(TweenArray array) {
                this.array = array;
                Assert.IsTrue(array.numLocks >= 0);
                array.numLocks++;
            }

            public void Dispose() {
                array.numLocks--;
                Assert.IsTrue(array.numLocks >= 0);
            }
        }

        public Enumerator GetEnumerator() => new Enumerator(this);
        public struct Enumerator : IDisposable {
            readonly TweenArray _array;
            int _index;
            readonly Lock _lock;

            internal Enumerator(TweenArray array) {
                Assert.IsTrue(array._data.IsCreated || array._capacity == 0);
                _array = array;
                _index = -1;
                _lock = new Lock(array);
            }

            public EnumeratorElement Current => new EnumeratorElement(_array, _index);

            public bool MoveNext() {
                _index++;
                return _index < _array.Count;
            }

            void IDisposable.Dispose() => _lock.Dispose();
        }
        public struct EnumeratorElement {
            readonly TweenArray _array;
            internal readonly int index;

            internal EnumeratorElement(TweenArray array, int index) {
                _array = array;
                this.index = index;
            }

            public ref TweenData tween => ref _array[index];
            public ref UnmanagedTweenData data => ref _array.GetDataAt(index);
        }
    }
}
