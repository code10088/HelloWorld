using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityExtensions
{
    /// <summary>
    /// StackStateMachineComponent
    /// </summary>
    public class StackStateMachineComponent<T> : BaseStackStateComponent where T : class, IStackState
    {
        List<T> _states = new List<T>(4);
        double _currentStateTime;


        /// <summary>
        /// Number of states in the stack.
        /// </summary>
        public int stateCount => _states.Count;


        /// <summary>
        /// Total time since entering the current state.
        /// </summary>
        public float currentStateTime => (float)_currentStateTime;


        /// <summary>
        /// Total time since entering the current state.
        /// </summary>
        public double currentStateTimeDouble => _currentStateTime;


        /// <summary>
        /// Current state (unusable if stateCount is zero)
        /// </summary>
        public T currentState => _states[_states.Count - 1];


        /// <summary>
        /// The state under current state (unusable if stateCount is zero or one)
        /// </summary>
        public T underState => _states[_states.Count - 2];


        /// <summary>
        /// Get the state in the stack by specified index.
        /// </summary>
        public T GetState(int index)
        {
            return _states[index];
        }


#if DEBUG
        bool _duringSetting = false;
#endif


        /// <summary>
        /// Push a state to the stack (state can be null).
        /// </summary>
        public void PushState(T newState)
        {
#if DEBUG
            if (_duringSetting)
            {
                throw new Exception("Can not change state inside OnExit or OnEnter!");
            }
            _duringSetting = true;
#endif

            if (stateCount > 0) currentState?.OnSuspend();

            _currentStateTime = 0;
            _states.Add(newState);

            newState?.OnPush();

#if DEBUG
            _duringSetting = false;
#endif

            StatePushed(newState);
        }


        /// <summary>
        /// Pop current state from the stack (unusable if stateCount is zero).
        /// </summary>
        public void PopState()
        {
#if DEBUG
            if (_duringSetting)
            {
                throw new Exception("Can not change state inside OnExit or OnEnter!");
            }
            _duringSetting = true;
#endif

            T originalState = currentState;

            originalState?.OnPop();

            _states.RemoveAt(_states.Count - 1);
            _currentStateTime = 0;

            if (stateCount > 0) currentState?.OnResume();

#if DEBUG
            _duringSetting = false;
#endif

            StatePopped(originalState);
        }


        /// <summary>
        /// Pop multi-states from the stack.
        /// </summary>
        public void PopStates(int count)
        {
            while (count > 0)
            {
                PopState();
                count--;
            }
        }


        /// <summary>
        /// Pop all states from the stack.
        /// </summary>
        public void PopAllStates()
        {
            PopStates(_states.Count);
        }


        /// <summary>
        /// Reset the stack.
        /// </summary>
        public void ResetStack()
        {
            while (_states.Count > 0)
            {
                int index = _states.Count - 1;
                _states[index]?.OnReset();
                _states.RemoveAt(index);
            }
            _currentStateTime = 0;
        }


        protected virtual void StatePopped(T poppedState)
        {
        }


        protected virtual void StatePushed(T pushedState)
        {
        }


        /// <summary>
        /// Update current state.
        /// Note: top level state machine need call this.
        /// </summary>
        public override void OnUpdate(float deltaTime)
        {
            _currentStateTime += deltaTime;
            if (stateCount > 0) currentState?.OnUpdate(deltaTime);
        }


        public override void OnReset()
        {
            ResetStack();
        }

    } // class StackStateMachineComponent<T>


    [AddComponentMenu("State Machines/Stack State Machine Component")]
    [DisallowMultipleComponent]
    public class StackStateMachineComponent : StackStateMachineComponent<BaseStackStateComponent>
    {
        public TimeMode timeMode = TimeMode.Unscaled;

        void Update()
        {
            OnUpdate(timeMode == TimeMode.Normal ? Time.deltaTime : Time.unscaledDeltaTime);
        }
    }

} // namespace UnityExtensions
