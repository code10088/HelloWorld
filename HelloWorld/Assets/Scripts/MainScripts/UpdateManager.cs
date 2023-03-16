using System;
using UnityEngine;

namespace MainAssembly
{
    public class UpdateManager : Singletion<UpdateManager>
    {
        private static UpdateItem cache = new UpdateItem();
        public int StartUpdate(Action action)
        {
            UpdateItem temp = (UpdateItem)cache.next;
            if (temp == null) temp = new UpdateItem();
            temp.Init(action);
            AsyncManager.Instance.Add(temp);
            return temp.ItemID;
        }
        public void StopUpdate(int id)
        {
            AsyncManager.Instance.Remove(id);
        }


        private class UpdateItem : AsyncItem
        {
            private Action action;

            public new void Init(Action action)
            {
                base.Init(null);
                this.action = action;
            }
            public override void Update()
            {
                action();
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
}