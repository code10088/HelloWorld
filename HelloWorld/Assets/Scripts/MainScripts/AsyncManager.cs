using System;
using System.Collections.Generic;
using UnityEngine;
using YooAsset;

public class AsyncManager : Singletion<AsyncManager>
{
    private List<AsyncItem> list1 = new(50);//不切片列表
    private List<AsyncItem> list2 = new(1000);//切片列表
    private float gcTimer = 0;
    private int idxMark = 0;

    public void Add(AsyncItem item, bool ignoreFrameTime)
    {
        (ignoreFrameTime ? list1 : list2).Add(item);
    }
    public void Remove(int id, bool execMark)
    {
        AsyncItem temp;
        for (int i = 0; i < list1.Count; i++)
        {
            temp = list1[i];
            if (temp.ItemID == id)
            {
                temp.endMark = true;
                temp.execMark = execMark;
                return;
            }
        }
        for (int i = 0; i < list2.Count; i++)
        {
            temp = list2[i];
            if (temp.ItemID == id)
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
        YooAssets.SetOperationSystemFrameTime(t);
        AsyncItem temp;
        for (int i = 0; i < list1.Count; i++)
        {
            temp = list1[i];
            temp.Update();
            if (temp.endMark)
            {
                temp.Finish();
                list1.RemoveAt(i);
                temp.Reset();
                i--;
            }
        }
        for (int i = 0; i < list2.Count; i++)
        {
            int idx = (i + idxMark) % list2.Count;
            temp = list2[idx];
            temp.Update();
            if (temp.endMark)
            {
                temp.Finish();
                list2.RemoveAt(idx);
                temp.Reset();
                idx--;
                i--;
            }
            if (Time.realtimeSinceStartup - t > GameSetting.updateTimeSliceS)
            {
                //list2.Count=9 ids最大为8
                //ids=0
                //  无删除(0+1)%9=1
                //  有删除(0-1+1)%8=0
                //ids=7
                //  无删除(7+1)%9=8
                //  有删除(7-1+1)%8=7
                //ids=8
                //  无删除(8+1)%9=0
                //  有删除(8-1+1)%8=0
                idxMark = (idx + 1) % Math.Max(1, list2.Count);
                return;
            }
        }
        if (Time.realtimeSinceStartup - gcTimer > GameSetting.gcTimeIntervalS)
        {
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

    public virtual void Update()
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