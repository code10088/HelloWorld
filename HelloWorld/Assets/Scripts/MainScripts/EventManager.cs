using System;
using System.Collections.Generic;
using UnityEngine;

public delegate void EventBindFunction(object obj);
public class EventItem
{
    private event EventBindFunction eventBindFunction;
    private object parameter;
    private float time;
    private float timer;
    private int count;
    public object Parameter
    {
        set { parameter = value; }
        get { return parameter; }
    }
    public float Time
    {
        set { time = value; }
        get { return time; }
    }
    public float Timer
    {
        set { timer = value; }
        get { return timer; }
    }
    public int Count
    {
        set { count = value; }
        get { return count; }
    }
    public void Add(EventBindFunction function)
    {
        eventBindFunction += function;
    }
    public void Remove(EventBindFunction function)
    {
        try
        {
            eventBindFunction -= function;
        }
        catch(Exception ex)
        {
            GameDebug.LogError(ex + "：卸载不存在事件");
        }
    }
    public void Handle()
    {
        if (eventBindFunction != null)
            eventBindFunction(parameter);
    }
}
public class EventManager
{
    private Dictionary<GameEventType, EventItem> eventDic = new Dictionary<GameEventType, EventItem>();
    private List<EventItem> eventList = new List<EventItem>();

    private EventManager() { }
    private static EventManager instance;
    public static EventManager Instance
    {
        get
        {
            if (instance == null)
                instance = new EventManager();
            return instance;
        }
    }

    public void RegisterEvent(GameEventType eventType, EventBindFunction function)
    {
        if (eventDic.ContainsKey(eventType))
        {
            eventDic[eventType].Add(function);
        }
        else
        {
            EventItem item = new EventItem();
            item.Add(function);
            eventDic.Add(eventType, item);
        }
    }
    public void UnRegisterEvent(GameEventType eventType, EventBindFunction function)
    {
        if (eventDic.ContainsKey(eventType))
        {
            EventItem item = eventDic[eventType];
            item.Remove(function);
            if (eventList.Contains(item))
                eventList.Remove(item);
        }
        else
        {
            GameDebug.LogError("卸载不存在事件类型");
        }
    }
    public void FireEvent(GameEventType eventType, object obj = null)
    {
        if (eventDic.ContainsKey(eventType))
        {
            eventDic[eventType].Parameter = obj;
            eventDic[eventType].Handle();
        }
        else
        {
            GameDebug.LogError("执行不存在事件类型");
        }
    }
    public void FireEventDelay(GameEventType eventType, float timer, object obj = null, int count = 1)
    {
        if (eventDic.ContainsKey(eventType))
        {
            EventItem item = eventDic[eventType];
            item.Parameter = obj;
            item.Timer = timer;
            item.Count = count;
            eventList.Add(item);
        }
        else
        {
            GameDebug.LogError("执行不存在事件类型");
        }
    }
    public void Update()
    {
        for (int i = 0; i < eventList.Count;)
        {
            EventItem item = eventList[i];
            item.Time += Time.deltaTime;
            if (item.Time > item.Timer)
            {
                item.Handle();
                item.Time = 0;
                item.Count--;
                if (item.Count <= 0)
                {
                    eventList.Remove(item);
                    continue;
                }
            }
            i++;
        }
    }
}