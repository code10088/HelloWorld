using System;
using UnityEngine;

public class AsyncManager : Singletion<AsyncManager>
{
    private AsyncItem first = new AsyncItem();
    private AsyncItem add = new AsyncItem();
    private float realtimeSinceStartup = 0;

    public void Add(AsyncItem item)
    {
        item.next = add.next;
        add.next = item;
    }
    public void Remove(int id, bool execMark)
    {
        AsyncItem item = first;
        while (item != null)
        {
            AsyncItem temp = item.next;
            if (temp == null) break;
            if (temp.ItemID == id) temp.endMark = true;
            if (temp.endMark) temp.execMark = execMark;
            if (temp.endMark) break;
            else item = temp;
        }
        item = add;
        while (item != null)
        {
            AsyncItem temp = item.next;
            if (temp == null) break;
            if (temp.ItemID == id) temp.endMark = true;
            if (temp.endMark) temp.execMark = execMark;
            if (temp.endMark) break;
            else item = temp;
        }
    }
    public void Update()
    {
        realtimeSinceStartup = Time.realtimeSinceStartup;
        AsyncItem item = first;
        while (item != null)
        {
            AsyncItem temp = item.next;
            if (temp == null) break;
            if (temp.Update()) temp.Finish();
            if (temp.endMark) item.next = temp.next;
            else item = temp;
            if (temp.endMark) temp.Reset();
            if (Time.realtimeSinceStartup - realtimeSinceStartup > GameSetting.updateTimeSliceS) break;
        }
        item = add;
        while (item != null)
        {
            AsyncItem temp = item.next;
            if (temp == null)
            {
                if (add.next != null)
                {
                    item.next = first.next;
                    first.next = add.next;
                    add.next = null;
                }
                break;
            }
            item = temp;
        }
    }
}
public class AsyncItem
{
    private static int uniqueId = 0;
    private int itemId = -1;
    public AsyncItem next;
    public bool endMark = false;
    public bool execMark = true;
    private Action finish;

    public int ItemID => itemId;

    public virtual void Init(Action finish)
    {
        itemId = ++uniqueId;
        this.finish = finish;
    }

    public virtual bool Update()
    {
        return true;
    }

    public virtual void Finish()
    {
        endMark = true;
        if (execMark) finish?.Invoke();
    }

    public virtual void Reset()
    {
        itemId = -1;
        next = null;
        endMark = false;
        execMark = true;
        finish = null;
    }
}