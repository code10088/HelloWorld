using System;
using System.Collections.Generic;

public class TimeManager : Singletion<TimeManager>
{
    private static Queue<TimeItem> cache = new();

    /// <summary>
    /// loop>0
    ///     loop：循环时间
    ///     time：>0表示总时间，<=0表示无限循环
    ///     action：循环回调(immediately立刻执行一次)
    ///     finish：总时间结束回调
    /// loop<=0
    ///     loop：没有意义
    ///     time：总时间
    ///     action：没有意义
    ///     finish：总时间结束回调
    /// </summary>
    public int StartTimer(float time, float loop = 0f, Action<float> action = null, bool immediately = true, Action finish = null, bool ignoreFrameTime = false)
    {
        if (loop <= 0 && time <= 0) return -1;
        TimeItem temp = cache.Count > 0 ? cache.Dequeue(): new();
        temp.Init(time, loop, action, immediately, finish);
        AsyncManager.Instance.Add(temp, ignoreFrameTime);
        return temp.ItemID;
    }
    public void StopTimer(int id, bool execMark = false)
    {
        if (id < 0) return;
        AsyncManager.Instance.Remove(id, execMark);
    }


    private class TimeItem : AsyncItem
    {
        private float time;
        private float loop;
        private Action<float> action;
        private float _time = 0;
        private float _loop = 0;

        public void Init(float time, float loop = 0f, Action<float> action = null, bool immediately = true, Action finish = null)
        {
            base.Init(finish);
            this.time = time;
            this.loop = loop;
            this.action = action;
            _time = 0;
            _loop = immediately ? 0 : loop;
        }
        public override void Update(float t)
        {
            if (endMark) return;
            _time += t;
            if (loop > 0 && _time > _loop)
            {
                _loop += loop;
                action(_time);
            }
            if (endMark) return;
            endMark = time > 0 && _time > time;
        }
        public override void Reset()
        {
            base.Reset();
            action = null;

            cache.Enqueue(this);
        }
    }
}