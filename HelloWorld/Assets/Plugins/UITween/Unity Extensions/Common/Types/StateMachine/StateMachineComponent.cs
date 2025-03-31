using System;
using UnityEngine;

namespace UnityExtensions
{
    /// <summary>
    /// StateMachine
    /// </summary>
    public class StateMachineComponent<T> : BaseStateComponent where T : class, IState
    {
        T _currentState;
        double _currentStateTime;


        /// <summary>
        /// Total time since entering the current state.
        /// </summary>
        public float currentStateTime => (float)_currentStateTime;


        /// <summary>
        /// Total time since entering the current state.
        /// </summary>
        public double currentStateTimeDouble => _currentStateTime;


#if DEBUG
        bool _duringSetting = false;
#endif


        /// <summary>
        /// Get or set current state.
        /// </summary>
        public T currentState
        {
            get => _currentState;
            set
            {
#if DEBUG
                if (_duringSetting)
                {
                    throw new Exception("Can not change state inside OnExit or OnEnter!");
                }
                _duringSetting = true;
#endif

                _currentState?.OnExit();

                T previousState = _currentState;
                _currentState = value;
                _currentStateTime = 0;

                _currentState?.OnEnter();

#if DEBUG
                _duringSetting = false;
#endif

                StateChanged(previousState, _currentState);
            }
        }


        protected virtual void StateChanged(T lastState, T currentState)
        {
        }


        /// <summary>
        /// Update current state.
        /// Note: top level state machine need call this.
        /// </summary>
        public override void OnUpdate(float deltaTime)
        {
            _currentStateTime += deltaTime;
            _currentState?.OnUpdate(deltaTime);
        }

    } // class StateMachineComponent<T>


    [AddComponentMenu("State Machines/State Machine Component")]
    [DisallowMultipleComponent]
    public class StateMachineComponent : StateMachineComponent<BaseStateComponent>
    {
        public TimeMode timeMode = TimeMode.Normal;

        void Update()
        {
            OnUpdate(timeMode == TimeMode.Normal ? Time.deltaTime : Time.unscaledDeltaTime);
        }
    }

} // namespace UnityExtensions
