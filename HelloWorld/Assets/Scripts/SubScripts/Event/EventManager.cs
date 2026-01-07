using System;
using System.Collections.Generic;

public class EventManager : Singletion<EventManager>
{
    private Dictionary<EventType, EventItem> eventDic = new Dictionary<EventType, EventItem>();

    public void RegisterEvent(EventType eventType, Action<object> function)
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
    public void UnRegisterEvent(EventType eventType, Action<object> function)
    {
        if (eventDic.ContainsKey(eventType))
        {
            EventItem item = eventDic[eventType];
            item.Remove(function);
        }
    }
    public void FireEvent(EventType eventType, params object[] obj)
    {
        if (eventDic.ContainsKey(eventType))
        {
            eventDic[eventType].Handle(obj);
        }
    }
    private class EventItem
    {
        private event Action<object> eventBindFunction;
        public void Add(Action<object> function)
        {
            eventBindFunction -= function;
            eventBindFunction += function;
        }
        public void Remove(Action<object> function)
        {
            eventBindFunction -= function;
        }
        public void Handle(object[] obj)
        {
            eventBindFunction?.Invoke(obj);
        }
    }
}