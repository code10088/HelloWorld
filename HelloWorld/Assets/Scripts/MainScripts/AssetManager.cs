using System;
using System.Collections.Generic;
using xasset;
using Object = UnityEngine.Object;

public class AssetManager : Singletion<AssetManager>
{
    private static Dictionary<int, AssetItem> total = new Dictionary<int, AssetItem>();
    private static AssetItem cache = new AssetItem();
    public int Load<T>(string path, Action<int, dynamic, dynamic> action = null, dynamic param = null) where T : Object
    {
        AssetItem temp = (AssetItem)cache.next;
        if (temp == null) temp = new AssetItem();
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
        private dynamic action;
        private dynamic param;
        private AssetRequest ar;

        public void Init<T>(string path, Action<int, dynamic, dynamic> action, dynamic param) where T : Object
        {
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
            action = null;
            param = null;
        }
        public void Unload()
        {
            if (ar != null) ar.Release();
            ar = null;
            next = cache.next;
            cache.next = this;
        }
    }
}
