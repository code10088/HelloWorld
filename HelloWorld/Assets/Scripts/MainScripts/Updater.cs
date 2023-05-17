using System;

public class Updater : Singletion<Updater>
{
    private static UpdateItem cache = new UpdateItem();

    public int StartUpdate(Action action)
    {
        UpdateItem temp = (UpdateItem)cache.next;
        if (temp == null) temp = new UpdateItem();
        else cache.next = temp.next;
        temp.Init(action);
        AsyncManager.Instance.Add(temp);
        return temp.ItemID;
    }
    public void StopUpdate(int id, bool execMark = true)
    {
        AsyncManager.Instance.Remove(id, execMark);
    }


    private class UpdateItem : AsyncItem
    {
        private Action action;

        public new void Init(Action action)
        {
            base.Init(null);
            this.action = action;
        }
        public override bool Update()
        {
            if (endMark) return true;
            action();
            return false;
        }
        public override void Reset()
        {
            base.Reset();
            action = null;

            next = cache.next;
            cache.next = this;
        }
    }
}