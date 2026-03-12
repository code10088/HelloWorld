using System;
using System.Collections.Generic;

public class EventManager : Singleton<EventManager>
{
    private Dictionary<EventType, Delegate> eventDic = new Dictionary<EventType, Delegate>();

    private void _Register(EventType eventType, Delegate func)
    {
        if (eventDic.TryGetValue(eventType, out var del)) del = Delegate.Combine(del, func);
        else del = func;
        eventDic[eventType] = del;
    }
    private void _Unregister(EventType eventType, Delegate func)
    {
        if (eventDic.TryGetValue(eventType, out var del))
        {
            del = Delegate.Remove(del, func);
            if (del == null) eventDic.Remove(eventType);
            else eventDic[eventType] = del;
        }
    }
    public void Register(EventType eventType, Action func) => _Register(eventType, func);
    public void Register<T>(EventType eventType, Action<T> func) => _Register(eventType, func);
    public void Register<T1, T2>(EventType eventType, Action<T1, T2> func) => _Register(eventType, func);
    public void Register<T1, T2, T3>(EventType eventType, Action<T1, T2, T3> func) => _Register(eventType, func);
    public void Unregister(EventType eventType, Action func) => _Unregister(eventType, func);
    public void Unregister<T>(EventType eventType, Action<T> func) => _Unregister(eventType, func);
    public void Unregister<T1, T2>(EventType eventType, Action<T1, T2> func) => _Unregister(eventType, func);
    public void Unregister<T1, T2, T3>(EventType eventType, Action<T1, T2, T3> func) => _Unregister(eventType, func);
    public void Fire(EventType eventType)
    {
        if (eventDic.TryGetValue(eventType, out var del))
        {
            if (del is Action action) action.Invoke();
        }
    }
    public void Fire<T>(EventType eventType, T param)
    {
        if (eventDic.TryGetValue(eventType, out var del))
        {
            if (del is Action<T> action) action.Invoke(param);
        }
    }
    public void Fire<T1, T2>(EventType eventType, T1 p1, T2 p2)
    {
        if (eventDic.TryGetValue(eventType, out var del))
        {
            if (del is Action<T1, T2> action) action.Invoke(p1, p2);
        }
    }
    public void Fire<T1, T2, T3>(EventType eventType, T1 p1, T2 p2, T3 p3)
    {
        if (eventDic.TryGetValue(eventType, out var del))
        {
            if (del is Action<T1, T2, T3> action) action.Invoke(p1, p2, p3);
        }
    }
}