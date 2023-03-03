using UnityEngine;
using UnityEngine.Events;

namespace UnityExtensions
{
    /// <summary>
    /// BaseStateComponent
    /// </summary>
    public abstract class BaseStateComponent : ScriptableComponent, IState
    {
        public virtual void OnEnter() { }
        public virtual void OnExit() { }
        public virtual void OnUpdate(float deltaTime) { }
}


    /// <summary>
    /// StateComponent. OnEnter & OnExit can be serialized.
    /// </summary>
    [AddComponentMenu("State Machines/State Component")]
    [DisallowMultipleComponent]
    public class StateComponent : BaseStateComponent
    {
        [SerializeField]
        UnityEvent _onEnter = default;

        [SerializeField]
        UnityEvent _onExit = default;

        public event UnityAction onEnter
        {
            add
            {
                if (_onEnter == null) _onEnter = new UnityEvent();
                _onEnter.AddListener(value);
            }
            remove { _onEnter?.RemoveListener(value); }
        }

        public event UnityAction onExit
        {
            add
            {
                if (_onExit == null) _onExit = new UnityEvent();
                _onExit.AddListener(value);
            }
            remove { _onExit?.RemoveListener(value); }
        }


        public override void OnEnter()
        {
            _onEnter?.Invoke();
        }


        public override void OnExit()
        {
            _onExit?.Invoke();
        }

    } // class StateComponent

} // namespace UnityExtensions
