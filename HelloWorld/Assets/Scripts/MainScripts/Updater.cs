using System;
using System.Collections.Generic;

public class Updater : Singletion<Updater>
{
    private static Queue<UpdateItem> cache = new();

    public int StartUpdate(Action<float> action, bool ignoreFrameTime = false)
    {
        UpdateItem temp = cache.Count > 0 ? cache.Dequeue() : new();
        temp.Init(action);
        AsyncManager.Instance.Add(temp, ignoreFrameTime);
        return temp.ItemID;
    }
    public void StopUpdate(int id)
    {
        if (id < 0) return;
        AsyncManager.Instance.Remove(id, false);
    }


    private class UpdateItem : AsyncItem
    {
        private Action<float> action;
        public void Init(Action<float> action)
        {
            base.Init(null);
            this.action = action;
        }
        public override void Update(float t)
        {
            if (endMark) return;
            action?.Invoke(t);
        }
        public override void Reset()
        {
            base.Reset();
            action = null;

            cache.Enqueue(this);
        }
    }
}