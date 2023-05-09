using System;
using System.Collections.Generic;

public class EventManager : Singletion<EventManager>
{
    private Dictionary<int, EventItem> eventDic = new Dictionary<int, EventItem>();

    public void RegisterEvent(int id, Action<object> function)
    {
        if (eventDic.ContainsKey(id))
        {
            eventDic[id].Add(function);
        }
        else
        {
            EventItem item = new EventItem();
            item.Add(function);
            eventDic.Add(id, item);
        }
    }
    public void UnRegisterEvent(int id, Action<object> function)
    {
        if (eventDic.ContainsKey(id))
        {
            EventItem item = eventDic[id];
            item.Remove(function);
        }
        else
        {
            GameDebug.LogError("卸载不存在事件类型");
        }
    }
    public void FireEvent(int id, params object[] obj)
    {
        if (eventDic.ContainsKey(id))
        {
            eventDic[id].Parameter = obj;
            eventDic[id].Handle();
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
            eventBindFunction?.Invoke(parameter);
        }
    }
}