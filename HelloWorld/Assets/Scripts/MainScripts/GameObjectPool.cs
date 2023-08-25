using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public class GameObjectPool<T> : LoadAssetItem where T : PoolItem, new()
{
    private List<T> wait = new();
    private List<T> cache = new();
    private int frameId;

    public void Enqueue(T item)
    {
        int index = wait.FindIndex(a => a.ItemID == item.ItemID);
        if (index >= 0) wait.RemoveAt(index);
        item.SetActive(false);
        cache.Add(item);
    }
    public T Dequeue(params object[] param)
    {
        T temp = null;
        if (cache.Count > 0) temp = cache[0];
        if (temp != null) cache.RemoveAt(0);
        else temp = new T();
        temp.Init(Delete, param);
        temp.SetActive(true);
        wait.Add(temp);
        Load<GameObject>();
        return temp;
    }
    public override void Release()
    {
        for (int i = 0; i < cache.Count; i++) cache[i].Release();
        if (frameId > 0) FrameManager.Instance.StopFrame(frameId);
        wait.Clear();
        cache.Clear();
        frameId = -1;
        base.Release();
    }
    private void Delete(int itemId)
    {
        int index = wait.FindIndex(a => a.ItemID == itemId);
        if (index >= 0) wait.RemoveAt(index);
        index = cache.FindIndex(a => a.ItemID == itemId);
        if (index >= 0) cache.RemoveAt(index);
        if (wait.Count == 0 && cache.Count == 0) Delay();
    }
    protected override void Finish(Object asset)
    {
        if (asset == null) return;
        frameId = FrameManager.Instance.StartFrame(0, 1, Instantiate);
    }
    private void Instantiate(int frame)
    {
        if (wait.Count == 0)
        {
            FrameManager.Instance.StopFrame(frameId);
            frameId = -1;
        }
        else
        {
            wait[0].Instantiate(asset);
            wait.RemoveAt(0);
        }
    }
}
public class PoolItem
{
    private static int uniqueId = 0;
    private int itemId = -1;
    protected GameObject obj;
    private int timerId = -1;
    private bool active = false;

    private Action<int> release;
    protected object[] param;

    public int ItemID => itemId;

    /// <summary>
    /// 请使用LoadGameObjectPool
    /// </summary>
    public void Init(Action<int> release, params object[] param)
    {
        itemId = ++uniqueId;
        this.release = release;
        this.param = param;
    }
    /// <summary>
    /// 请使用LoadGameObjectPool
    /// </summary>
    public void SetActive(bool b)
    {
        if (b && !active) Recycle();
        else if (!b && active) Delay();
    }
    private void Delay()
    {
        active = false;
        obj?.SetActive(false);
        if (timerId < 0) timerId = TimeManager.Instance.StartTimer(GameSetting.recycleTime, finish: Release);
        param = null;
    }
    /// <summary>
    /// 请使用LoadGameObjectPool
    /// </summary>
    public virtual void Release()
    {
        if (timerId < 0) TimeManager.Instance.StopTimer(timerId, false);
        if (obj != null) GameObject.Destroy(obj);
        itemId = -1;
        obj = null;
        timerId = -1;
        active = false;
        release?.Invoke(itemId);
        param = null;
    }
    private void Recycle()
    {
        if (timerId < 0) TimeManager.Instance.StopTimer(timerId, false);
        timerId = -1;
        active = true;
        obj?.SetActive(true);
    }
    /// <summary>
    /// 请使用LoadGameObjectPool
    /// </summary>
    public void Instantiate(Object asset)
    {
        if (obj == null) obj = Object.Instantiate(asset, Vector3.zero, Quaternion.identity) as GameObject;
        obj.SetActive(active);
        LoadFinish();
    }
    protected virtual void LoadFinish()
    {

    }
}