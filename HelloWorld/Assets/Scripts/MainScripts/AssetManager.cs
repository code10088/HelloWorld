using System;
using System.Collections.Generic;
using xasset;
using Object = UnityEngine.Object;

namespace MainAssembly
{
    public class AssetManager : Singletion<AssetManager>
    {
        private static Dictionary<int, AssetItem> total = new Dictionary<int, AssetItem>();
        private static AssetItem cache = new AssetItem();

        public void Init(Action<object> action)
        {
            Assets.InitializeAsync(action);
        }

        public int Load<T>(string path, Action<int, Object, object> action = null, object param = null) where T : Object
        {
            AssetItem temp = (AssetItem)cache.next;
            if (temp == null) temp = new AssetItem();
            else cache.next = temp.next;
            temp.Init<T>(path, action, param);
            total[temp.ItemID] = temp;
            return temp.ItemID;
        }
        public void Unload(int id)
        {
            if (total.TryGetValue(id, out AssetItem item))
            {
                item.Unload();
                total.Remove(id);
            }
        }


        private class AssetItem : AsyncItem
        {
            private Action<int, Object, object> action;
            private object param;
            private AssetRequest ar;

            public void Init<T>(string path, Action<int, Object, object> action, object param) where T : Object
            {
                base.Init(null);
                this.action = action;
                this.param = param;
                ar = Asset.LoadAsync(path, typeof(T));
                if (ar == null) Finish();
                else ar.completed += Finish;
            }
            private void Finish(Request request)
            {
                Object asset = ar == null ? null : ar.asset;
                action?.Invoke(ItemID, asset, param);
            }
            public void Unload()
            {
                base.Reset();
                ar?.Release();
                ar = null;
                action = null;
                param = null;
                next = cache.next;
                cache.next = this;
            }
        }
    }
}