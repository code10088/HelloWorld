using System;
using UnityEngine;
using UnityEngine.Events;

namespace UnityExtensions
{
    /// <summary>
    /// BaseStackState
    /// </summary>
    public abstract class BaseStackState : IStackState
    {
        public virtual void OnReset() { }
        public virtual void OnPush() { }
        public virtual void OnPop() { }
        public virtual void OnSuspend() { }
        public virtual void OnResume() { }
        public virtual void OnUpdate(float deltaTime) { }
    }

    /// <summary>
    /// StackState. OnPush, OnPop, OnSuspend, OnResume can be serialized.
    /// </summary>
    [Serializable]
    public class StackState : BaseStackState
    {
        [SerializeField]
        UnityEvent _onPush = default;

        [SerializeField]
        UnityEvent _onPop = default;

        [SerializeField]
        UnityEvent _onSuspend = default;

        [SerializeField]
        UnityEvent _onResume = default;

        public event UnityAction onPush
        {
            add
            {
                if (_onPush == null) _onPush = new UnityEvent();
                _onPush.AddListener(value);
            }
            remove { _onPush?.RemoveListener(value); }
        }

        public event UnityAction onPop
        {
            add
            {
                if (_onPop == null) _onPop = new UnityEvent();
                _onPop.AddListener(value);
            }
            remove { _onPop?.RemoveListener(value); }
        }

        public event UnityAction onSuspend
        {
            add
            {
                if (_onSuspend == null) _onSuspend = new UnityEvent();
                _onSuspend.AddListener(value);
            }
            remove { _onSuspend?.RemoveListener(value); }
        }

        public event UnityAction onResume
        {
            add
            {
                if (_onResume == null) _onResume = new UnityEvent();
                _onResume.AddListener(value);
            }
            remove { _onResume?.RemoveListener(value); }
        }

        public override void OnPush()
        {
            _onPush?.Invoke();
        }

        public override void OnPop()
        {
            _onPop?.Invoke();
        }

        public override void OnSuspend()
        {
            _onSuspend?.Invoke();
        }

        public override void OnResume()
        {
            _onResume?.Invoke();
        }

    } // class StackState

} // namespace UnityExtensions
