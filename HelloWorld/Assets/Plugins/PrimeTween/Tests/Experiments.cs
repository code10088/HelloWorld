/*// ReSharper disable UnusedParameter.Global
using System;
using JetBrains.Annotations;
using PrimeTween;
using UnityEngine;
using Object = UnityEngine.Object;

[Serializable, PublicAPI]
public class TweenData<T> where T : struct {
    [SerializeField] TweenType tweenType;
    [SerializeField] Object target;
    [SerializeField] TweenSettings<T> settings;
    Tween tween;
    readonly Action<T> customSetter;
    bool toEndValue;

    /*public TweenData(TweenType tweenType, Object target, TweenSettings<T> data) {
        this.tweenType = tweenType;
        this.target = target;
        this.data = data;
    }

    public TweenData(TweenSettings<T> data, Action<T> customSetter) {
        tweenType = TweenType.Custom;
        this.data = data;
        this.customSetter = customSetter;
    }#1#

    public void Play(bool _toEndValue = true) {
        if (tween._isAlive) {
            if (isPaused && toEndValue == _toEndValue) {
                isPaused = false;
                return;
            }
            tween.Stop();
        }
        toEndValue = _toEndValue;
        // tween = ...
    }

    public void Stop() {
        tween.Stop();
    }

    public void Complete() {
        tween.Complete();
    }

    public void OnComplete(Action callback) {
        callback();
        throw new NotImplementedException();
    }
    
    public bool isPaused {
        get => tween.isPaused;
        set => tween.isPaused = value;
    }
}*/