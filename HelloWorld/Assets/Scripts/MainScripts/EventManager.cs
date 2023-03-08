using System;
using System.Collections.Generic;
using UnityEngine;


public class EventManager : Singletion<EventManager>
{
    private Dictionary<GameEventType, EventItem> eventDic = new Dictionary<GameEventType, EventItem>();

    public void RegisterEvent(GameEventType eventType, Action<object> function)
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
    public void UnRegisterEvent(GameEventType eventType, Action<object> function)
    {
        if (eventDic.ContainsKey(eventType))
        {
            EventItem item = eventDic[eventType];
            item.Remove(function);
        }
        else
        {
            GameDebug.LogError("卸载不存在事件类型");
        }
    }
    public void FireEvent(GameEventType eventType, params object[] obj)
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
    private class EventItem
    {
        private event Action<object> eventBindFunction;
        private object[] parameter;
        public object[] Parameter
        {
            set { parameter = value; }
            get { return parameter; }
        }
        public void Add(Action<object> function)
        {
            eventBindFunction += function;
        }
        public void Remove(Action<object> function)
        {
            try
            {
                eventBindFunction -= function;
            }
            catch (Exception ex)
            {
                GameDebug.LogError(ex + "：卸载不存在事件");
            }
        }
        public void Handle()
        {
            if (eventBindFunction != null) eventBindFunction(parameter);
        }
    }
}