using System;
using UnityEngine;
using YooAsset;

public class AsyncManager : Singletion<AsyncManager>
{
    private AsyncItem[] array1 = new AsyncItem[100];//不切片列表
    private AsyncItem[] array2 = new AsyncItem[10000];//切片列表
    private byte[] mark1 = new byte[100];
    private byte[] mark2 = new byte[10000];
    private int count1 = 0;
    private int count2 = 0;
    private float gcTimer = 0;
    private int idxMark = 0;

    public void Add(AsyncItem item, bool ignoreFrameTime)
    {
        if (ignoreFrameTime)
        {
            for (int i = 0; i < count1; i++)
            {
                if (mark1[i] == 0)
                {
                    array1[i] = item;
                    mark1[i] = 1;
                    return;
                }
            }
            array1[count1] = item;
            mark1[count1] = 1;
            count1++;
        }
        else
        {
            for (int i = 0; i < count2; i++)
            {
                if (mark2[i] == 0)
                {
                    array2[i] = item;
                    mark2[i] = 1;
                    return;
                }
            }
            array2[count2] = item;
            mark2[count2] = 1;
            count2++;
        }
    }
    public void Remove(int id, bool execMark)
    {
        AsyncItem temp;
        for (int i = 0; i < count1; i++)
        {
            if (mark1[i] > 0)
            {
                temp = array1[i];
                if (temp.ItemID == id)
                {
                    temp.endMark = true;
                    temp.execMark = execMark;
                    return;
                }
            }
        }
        for (int i = 0; i < count2; i++)
        {
            if (mark2[i] > 0)
            {
                temp = array2[i];
                if (temp.ItemID == id)
                {
                    temp.endMark = true;
                    temp.execMark = execMark;
                    return;
                }
            }
        }
    }
    public void Update()
    {
        float t = Time.realtimeSinceStartup;
        YooAssets.SetOperationSystemFrameTime(t);
        AsyncItem temp;
        for (int i = 0; i < count1; i++)
        {
            if (mark1[i] > 0)
            {
                temp = array1[i];
                temp.Update();
                if (temp.endMark)
                {
                    temp.Finish();
                    temp.Reset();
                    mark1[i] = 0;
                }
            }
        }
        for (int i = 0; i < count2; i++)
        {
            int idx = (i + idxMark) % count2;
            if (mark2[idx] > 0)
            {
                temp = array2[idx];
                temp.Update();
                if (temp.endMark)
                {
                    temp.Finish();
                    temp.Reset();
                    mark2[idx] = 0;
                }
            }
            if (Time.realtimeSinceStartup - t > GameSetting.updateTimeSliceS)
            {
                idxMark = (idx + 1) % Math.Max(1, count2);
                return;
            }
        }
        if (Time.realtimeSinceStartup - gcTimer > GameSetting.gcTimeIntervalS)
        {
            int offset = 0;
            for (int i = 0; i < count1; i++)
            {
                int idx = i - offset;
                if (mark1[i] == 0) offset++;
                array1[idx] = array1[i];
                mark1[idx] = 1;
            }
            for (int i = 0; i < offset; i++)
            {
                int idx = count1 - 1 - i;
                array1[idx] = null;
                mark1[idx] = 0;
            }
            count1 -= offset;

            offset = 0;
            for (int i = 0; i < count2; i++)
            {
                int idx = i - offset;
                if (mark2[i] == 0) offset++;
                array2[idx] = array2[i];
                mark2[idx] = 1;
            }
            for (int i = 0; i < offset; i++)
            {
                int idx = count2 - 1 - i;
                array2[idx] = null;
                mark2[idx] = 0;
            }
            count2 -= offset;

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