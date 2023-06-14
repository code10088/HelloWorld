using System;
using System.Collections.Generic;
using UnityEngine;

public class AsyncManager : Singletion<AsyncManager>
{
    private List<AsyncItem> list = new(1000);
    private float t = 0;

    public void Add(AsyncItem item)
    {
        list.Add(item);
    }
    public void Remove(int id, bool execMark)
    {
        AsyncItem temp;
        for (int i = 0; i < list.Count; i++)
        {
            temp = list[i];
            if (temp == null)
            {
                continue;
            }
            if (temp.ItemID == id)
            {
                temp.endMark = true;
                temp.execMark = execMark;
                break;
            }
        }
    }
    public void Update()
    {
        AsyncItem temp;
        t = Time.realtimeSinceStartup;
        for (int i = 0; i < list.Count; i++)
        {
            temp = list[i];
            if (temp == null)
            {
                continue;
            }
            temp.Update();
            if (temp.endMark)
            {
                temp.Finish();
                list.RemoveAt(i);
                temp.Reset();
                i--;
            }
            if (Time.realtimeSinceStartup - t > GameSetting.updateTimeSliceS) break;
        }
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