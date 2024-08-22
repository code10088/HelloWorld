using System;
using UnityEngine;
using YooAsset;

public class AsyncManager : Singletion<AsyncManager>
{
    private ArrayEx<AsyncItem> array1 = new ArrayEx<AsyncItem>(100);//不切片列表
    private ArrayEx<AsyncItem> array2 = new ArrayEx<AsyncItem>(10000);//切片列表
    private float gcTimer = 0;
    private int idxMark = 0;
    private float timeMark = 0;

    public void Add(AsyncItem item, bool ignoreFrameTime)
    {
        (ignoreFrameTime ? array1 : array2).Add(item);
    }
    public void Remove(int id, bool execMark)
    {
        AsyncItem temp;
        for (int i = 0; i < array1.Count; i++)
        {
            temp = array1[i];
            if (temp != null && temp.ItemID == id)
            {
                temp.endMark = true;
                temp.execMark = execMark;
                return;
            }
        }
        for (int i = 0; i < array2.Count; i++)
        {
            temp = array2[i];
            if (temp != null && temp.ItemID == id)
            {
                temp.endMark = true;
                temp.execMark = execMark;
                return;
            }
        }
    }
    public void Update()
    {
        float t = Time.realtimeSinceStartup;
        float delta = t - timeMark;
        timeMark = t;
        YooAssets.SetOperationSystemFrameTime(t);
        AsyncItem temp;
        for (int i = 0; i < array1.Count; i++)
        {
            temp = array1[i];
            if (temp != null)
            {
                temp.Update(delta);
                if (temp.endMark)
                {
                    temp.Finish();
                    temp.Reset();
                    array1[i] = null;
                }
            }
        }
        for (int i = 0; i < array2.Count; i++)
        {
            int idx = (i + idxMark) % array2.Count;
            temp = array2[idx];
            if (temp != null)
            {
                temp.Update(delta);
                if (temp.endMark)
                {
                    temp.Finish();
                    temp.Reset();
                    array2[idx] = null;
                }
            }
            if (Time.realtimeSinceStartup - t > GameSetting.updateTimeSliceS)
            {
                idxMark = (idx + 1) % Math.Max(1, array2.Count);
                return;
            }
        }
        if (Time.realtimeSinceStartup - gcTimer > GameSetting.gcTimeIntervalS)
        {
            array1.Trim();
            array2.Trim();

            gcTimer = Time.realtimeSinceStartup;
            GC.Collect();
        }
    }
    public void GCCollect()
    {
        gcTimer = -GameSetting.gcTimeIntervalS;
    }
}
public class AsyncItem
{
    private static int uniqueId = 0;
    private int itemId = -1;
    public bool endMark = false;
    public bool execMark = true;
    protected Action finish;

    public int ItemID => itemId;

    public virtual void Init(Action finish)
    {
        itemId = ++uniqueId;
        this.finish = finish;
    }

    public virtual void Update(float t)
    {

    }

    public virtual void Finish()
    {
        endMark = true;
        if (execMark) finish?.Invoke();
    }

    public virtual void Reset()
    {
        endMark = false;
        execMark = true;
        finish = null;
    }
}