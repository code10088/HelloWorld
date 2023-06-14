using System;
using System.Collections.Generic;
using xasset;
using Object = UnityEngine.Object;

public class AssetManager : Singletion<AssetManager>
{
    private static Dictionary<int, AssetItemGroup> group = new Dictionary<int, AssetItemGroup>();
    private static Dictionary<int, AssetItem> total = new Dictionary<int, AssetItem>();
    private static AssetItem cache = new AssetItem();
    private Action initFinish;

    public void Init(Action action)
    {
        initFinish = action;
        Assets.InitializeAsync(InitFinish);
    }
    private void InitFinish(Request completed)
    {
        initFinish?.Invoke();
    }

    public int Load<T>(string path, Action<int, Object> action = null) where T : Object
    {
        AssetItem temp = (AssetItem)cache.next;
        if (temp == null) temp = new AssetItem();
        else cache.next = temp.next;
        temp.Init<T>(path, action);
        total[temp.ItemID] = temp;
        return temp.ItemID;
    }
    public int Load(string[] path, Action<string[], Object[]> action = null)
    {
        AssetItemGroup temp = new AssetItemGroup();
        temp.Init(path, action);
        return temp.ItemID;
    }
    public void Unload(int id)
    {
        if (group.TryGetValue(id, out AssetItemGroup a))
        {
            a.Unload();
            group.Remove(id);
        }
        else if (total.TryGetValue(id, out AssetItem b))
        {
            b.Unload();
            total.Remove(id);
        }
    }
    public float GetProgerss(int id)
    {
        float progress = 0;
        if (group.TryGetValue(id, out AssetItemGroup a)) progress = a.Progress;
        else if (total.TryGetValue(id, out AssetItem b)) progress = b.Progress;
        return progress;
    }


    private class AssetItemGroup : AsyncItem
    {
        private Action<string[], Object[]> action;
        private string[] path;
        private int[] ids;
        private Object[] assets;
        private int complete;
        public float Progress => (float)complete / ids.Length;

        public void Init(string[] path, Action<string[], Object[]> action)
        {
            base.Init(null);
            this.path = path;
            this.action = action;
            ids = new int[path.Length];
            assets = new Object[path.Length];
            for (int i = 0; i < path.Length; i++) ids[i] = Instance.Load<Object>(path[i], Finish);
        }
        private void Finish(int id, Object asset)
        {
            int index = Array.FindIndex(ids, a => a == id);
            assets[index] = asset;
            if (++complete == ids.Length) action?.Invoke(path, assets);
        }
        public void Unload()
        {
            base.Reset();
            for (int i = 0; i < ids.Length; i++) Instance.Unload(ids[i]);
            action = null;
            path = null;
            ids = null;
            assets = null;
        }
    }
    private class AssetItem : AsyncItem
    {
        private Action<int, Object> action;
        private AssetRequest ar;
        public float Progress => ar == null ? 0 : ar.progress;

        public void Init<T>(string path, Action<int, Object> action) where T : Object
        {
            base.Init(null);
            this.action = action;
            ar = Asset.LoadAsync(path, typeof(T));
            if (ar == null) Finish();
            else ar.completed += Finish;
        }
        private void Finish(Request request)
        {
            Object asset = ar == null ? null : ar.asset;
            action?.Invoke(ItemID, asset);
        }
        public void Unload()
        {
            base.Reset();
            ar?.Release();
            ar = null;
            action = null;
            next = cache.next;
            cache.next = this;
        }
    }
}