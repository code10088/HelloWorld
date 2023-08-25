using System;
using System.Collections.Generic;

public class Updater : Singletion<Updater>
{
    private static Queue<UpdateItem> cache = new();

    public int StartUpdate(Action action, bool ignoreFrameTime = false)
    {
        UpdateItem temp = cache.Count > 0 ? cache.Dequeue() : new();
        temp.Init(action);
        AsyncManager.Instance.Add(temp, ignoreFrameTime);
        return temp.ItemID;
    }
    public void StopUpdate(int id, bool execMark = true)
    {
        if (id < 0) return;
        AsyncManager.Instance.Remove(id, execMark);
    }


    private class UpdateItem : AsyncItem
    {
        public override void Update()
        {
            if (endMark) return;
            Finish();
        }
        public override void Finish()
        {
            finish?.Invoke();
        }
        public override void Reset()
        {
            base.Reset();

            cache.Enqueue(this);
        }
    }
}