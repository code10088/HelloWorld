using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace UnityExtensions.Tween
{
    public enum WrapMode
    {
        Clamp,
        Loop,
        PingPong
    }


    public enum ArrivedAction
    {
        KeepPlaying = 0,
        StopOnForwardArrived = 1,
        StopOnBackArrived = 2,
        AlwaysStopOnArrived = 3
    }


    public enum PlayDirection
    {
        Forward,
        Back
    }


    /// <summary>
    /// TweenPlayer
    /// </summary>
    [AddComponentMenu("Miscellaneous/Tween Player")]
    public partial class TweenPlayer : ConfigurableUpdateComponent
    {
        const float _minDuration = 0.0001f;

        [SerializeField, Min(_minDuration)]
        float _duration = 1f;

        /// <summary>
        /// Use unscaled delta time or normal delta time?
        /// </summary>
        public TimeMode timeMode = TimeMode.Unscaled;

        /// <summary>
        /// The wrap mode for playing.
        /// </summary>
        public WrapMode wrapMode = WrapMode.Clamp;

        /// <summary>
        /// Controls whether playback stops when the animation ends.
        /// </summary>
        public ArrivedAction arrivedAction = ArrivedAction.AlwaysStopOnArrived;

        /// <summary>
        /// Samples all animation states when this TweenPLayer awakes, it can avoid flashing caused by error initial states.
        /// </summary>
        public bool sampleOnAwake = true;

        [SerializeField] UnityEvent _onForwardArrived = default;
        [SerializeField] UnityEvent _onBackArrived = default;

        [SerializeField, SerializeReference] List<TweenAnimation> _animations = default;

        /// <summary>
        /// The direction of the playback.
        /// </summary>
        [NonSerialized] public PlayDirection direction;

        float _normalizedTime = 0f;
        int _state = 0;    // -1: BackArrived, 0: Playing, +1: ForwardArrived

        /// <summary>
        /// The total duration time
        /// </summary>
        public float duration
        {
            get => _duration;
            set => _duration = value > _minDuration ? value : _minDuration;
        }

        /// <summary>
        /// Add or remove callbacks when it's over.
        /// </summary>
        public event UnityAction onForwardArrived
        {
            add => (_onForwardArrived ?? (_onForwardArrived = new UnityEvent())).AddListener(value);
            remove => _onForwardArrived?.RemoveListener(value);
        }

        /// <summary>
        /// Add or remove callbacks when it gets to the starting point.
        /// </summary>
        public event UnityAction onBackArrived
        {
            add => (_onBackArrived ?? (_onBackArrived = new UnityEvent())).AddListener(value);
            remove => _onBackArrived?.RemoveListener(value);
        }

        /// <summary>
        /// Current time in range [0, 1]
        /// </summary>
        public float normalizedTime
        {
            get => _normalizedTime;
            set
            {
                _normalizedTime = Mathf.Clamp01(value);
                Sample(_normalizedTime);
            }
        }

        /// <summary>
        /// animationCount
        /// </summary>
        public int animationCount => _animations == null ? 0 : _animations.Count;

        /// <summary>
        /// Reverse playback direction.
        /// </summary>
        public void ReverseDirection() => direction = (direction == PlayDirection.Forward ? PlayDirection.Back : PlayDirection.Forward);


        public void SetForwardDirection() => direction = PlayDirection.Forward;


        public void SetBackDirection() => direction = PlayDirection.Back;


        public void SetForwardDirectionAndEnabled()
        {
            direction = PlayDirection.Forward;
            normalizedTime = 0f;
            enabled = true;
        }


        public void SetBackDirectionAndEnabled()
        {
            direction = PlayDirection.Back;
            normalizedTime = 0f;
            enabled = true;
        }

        /// <summary>
        /// Sample all animation states at a specified time.
        /// </summary>
        public void Sample(float normalizedTime)
        {
            if (_animations != null)
            {
                for (int i = 0; i < _animations.Count; i++)
                {
                    var item = _animations[i];
                    if (item.enabled) item.Sample(normalizedTime);
                }
            }
        }

        /// <summary>
        /// Add an animation by a type parameter.
        /// </summary>
        public T AddAnimation<T>() where T : TweenAnimation, new()
        {
            var anim = new T();
            (_animations ?? (_animations = new List<TweenAnimation>(4))).Add(anim);
            return anim;
        }

        /// <summary>
        /// Add an animation by a type parameter.
        /// </summary>
        public TweenAnimation AddAnimation(Type type)
        {
            var anim = (TweenAnimation)Activator.CreateInstance(type);
            (_animations ?? (_animations = new List<TweenAnimation>(4))).Add(anim);
            return anim;
        }

        /// <summary>
        /// Get the animation at the index.
        /// </summary>
        public TweenAnimation GetAnimation(int index) => _animations[index];

        /// <summary>
        /// Get an animation by the specified type parameter.
        /// </summary>
        public T GetAnimation<T>() where T : TweenAnimation
        {
            if (_animations != null)
            {
                foreach (var item in _animations)
                {
                    if (item is T result) return result;
                }
            }
            return null;
        }

        /// <summary>
        /// Remove the animation at the index.
        /// </summary>
        public void RemoveAnimation(int index) => _animations.RemoveAt(index);

        /// <summary>
        /// Remove the specified animation.
        /// </summary>
        public bool RemoveAnimation(TweenAnimation animation) => _animations != null ? _animations.Remove(animation) : false;

        /// <summary>
        /// Remove an animation by the specified type parameter.
        /// </summary>
        public bool RemoveAnimation<T>() where T : TweenAnimation
        {
            if (_animations != null)
            {
                for (int i = 0; i < _animations.Count; i++)
                {
                    if (_animations[i] is T)
                    {
                        _animations.RemoveAt(i);
                        return true;
                    }
                }
            }
            return false;
        }


        private void Awake()
        {
            TweenAnimation.tweenAniRoot = transform;
            if (config) BytesDecode.Deserialize(this, config.bytes);
            itemDurationRecord = duration;
            itemAnimationsRecord = new List<TweenAnimation>(_animations);
			if (tweenGrid) RefreshTweenGrid();
            if (sampleOnAwake) Sample(_normalizedTime);
        }


        protected override void OnUpdate()
        {
#if UNITY_EDITOR
            if (_dragging) return;

            // Avoid rare error of prefab mode
            if (!this)
            {
                playing = false;
                return;
            }
#endif

            float deltaTime = RuntimeUtilities.GetUnitedDeltaTime(timeMode);

            while (this && isActiveAndEnabled && deltaTime > Mathf.Epsilon)
            {
                if (direction == PlayDirection.Forward)
                {
                    if (_normalizedTime < 1f)
                    {
                        _state = 0;
                    }
                    else if (wrapMode == WrapMode.Loop)
                    {
                        _normalizedTime = 0f;
                        _state = 0;
                    }

                    float time = _normalizedTime * _duration + deltaTime;

                    // playing
                    if (time < _duration)
                    {
                        normalizedTime = time / _duration;
                        return;
                    }

                    // arrived
                    normalizedTime = 1f;
                    if (_state != +1)
                    {
                        _state = +1;

                        if ((arrivedAction & ArrivedAction.StopOnForwardArrived) != 0)
                            enabled = false;

                        _onForwardArrived?.Invoke();
                    }

                    // wrap
                    switch (wrapMode)
                    {
                        case WrapMode.Clamp:
                            return;

                        case WrapMode.PingPong:
                            direction = PlayDirection.Back;
                            break;
                    }

                    deltaTime = time - _duration;
                }
                else
                {
                    if (_normalizedTime > 0f)
                    {
                        _state = 0;
                    }
                    else if (wrapMode == WrapMode.Loop)
                    {
                        _normalizedTime = 1f;
                        _state = 0;
                    }

                    float time = _normalizedTime * _duration - deltaTime;

                    // playing
                    if (time > 0f)
                    {
                        normalizedTime = time / _duration;
                        return;
                    }

                    // arrived
                    normalizedTime = 0f;
                    if (_state != -1)
                    {
                        _state = -1;

                        if ((arrivedAction & ArrivedAction.StopOnBackArrived) != 0)
                            enabled = false;

                        _onBackArrived?.Invoke();
                    }

                    // wrap
                    switch (wrapMode)
                    {
                        case WrapMode.Clamp:
                            return;

                        case WrapMode.PingPong:
                            direction = PlayDirection.Forward;
                            break;
                    }

                    deltaTime = -time;
                }
            }
        }

    } // class TweenPlayer

} // UnityExtensions.Tween
