using System;
using System.Collections.Generic;

public class FrameManager : Singletion<FrameManager>
{
    private static Queue<FrameItem> cache = new();

    /// <summary>
    /// loop>0
    ///     loop：循环帧数
    ///     frame：>0表示总帧数，<=0表示无限循环
    ///     action：循环回调
    ///     finish：总帧数结束回调
    /// loop<=0
    ///     loop：没有意义
    ///     frame：总帧数
    ///     action：没有意义
    ///     finish：总帧数结束回调
    /// </summary>
    public int StartFrame(int frame, int loop = 0, Action<int> action = null, Action finish = null, bool ignoreFrameTime = false)
    {
        if (loop <= 0 && frame <= 0) return -1;
        FrameItem temp = cache.Count > 0 ? cache.Dequeue(): new();
        temp.Init(frame, loop, action, finish);
        AsyncManager.Instance.Add(temp, ignoreFrameTime);
        return temp.ItemID;
    }
    public void StopFrame(int id, bool execMark = true)
    {
        AsyncManager.Instance.Remove(id, execMark);
    }


    private class FrameItem : AsyncItem
    {
        private int frame;
        private int loop;
        private Action<int> action;
        private int _frame = 0;
        private int _loop = 0;

        public void Init(int frame, int loop = 0, Action<int> action = null, Action finish = null)
        {
            base.Init(finish);
            this.frame = frame;
            this.loop = loop;
            this.action = action;
        }
        public override void Update()
        {
            if (endMark) return;
            _frame++;
            if (loop > 0 && _frame > _loop)
            {
                _loop += loop;
                action(_frame);
            }
            endMark = frame > 0 && _frame > frame;
        }
        public override void Reset()
        {
            base.Reset();
            frame = 0;
            loop = 0;
            action = null;
            _frame = 0;
            _loop = 0;

            cache.Enqueue(this);
        }
    }
}