using System;
using UnityEngine;

public class TimeManager : Singletion<TimeManager>
{
    private static TimeItem cache = new TimeItem();
    public int StartTimer(float time, float loop = 0f, Action<float> action = null, Action finish = null)
    {
        TimeItem temp = (TimeItem)cache.next;
        if (temp == null) temp = new TimeItem();
        temp.Init(time, loop, action, finish);
        AsyncManager.Instance.Add(temp);
        return temp.ItemID;
    }
    public void StopTimer(int id)
    {
        AsyncManager.Instance.Remove(id);
    }


    private class TimeItem : AsyncItem
    {
        private float time;
        private float loop;
        private Action<float> action;
        private float _time = 0;
        private float _loop = 0;

        public void Init(float time, float loop = 0f, Action<float> action = null, Action finish = null)
        {
            if (loop <= 0 && time <= 0) return;
            base.Init(finish);
            this.time = time;
            this.loop = loop;
            this.action = action;
        }
        public override void Update()
        {
            if (mark) return;
            _time += Time.deltaTime;
            if (loop > 0 && _time > _loop)
            {
                _loop += loop;
                action(_time);
            }
            if (time > 0 && _time > time)
            {
                Finish();
            }
        }
        public override void Reset()
        {
            base.Reset();
            time = 0;
            loop = 0;
            action = null;
            _time = 0;
            _loop = 0;

            next = cache.next;
            cache.next = this;
        }
    }
}