using System;
using UnityEngine;

namespace MainAssembly
{
    public class TimeManager : Singletion<TimeManager>
    {
        private static TimeItem cache = new TimeItem();
        /// <summary>
        /// loop>0
        ///     loop：循环时间
        ///     time：>0表示总时间，<=0表示无限循环
        ///     action：循环回调
        ///     finish：总时间结束回调
        /// loop<=0
        ///     loop：没有意义
        ///     time：总时间
        ///     action：没有意义
        ///     finish：总时间结束回调
        /// </summary>
        public int StartTimer(float time, float loop = 0f, Action<float> action = null, Action finish = null)
        {
            TimeItem temp = (TimeItem)cache.next;
            if (temp == null) temp = new TimeItem();
            temp.Init(time, loop, action, finish);
            AsyncManager.Instance.Add(temp);
            return temp.ItemID;
        }
        public void StopTimer(int id, bool execMark = true)
        {
            AsyncManager.Instance.Remove(id, execMark);
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
            public override bool Update()
            {
                if (endMark) return true;
                _time += Time.deltaTime;
                if (loop > 0 && _time > _loop)
                {
                    _loop += loop;
                    action(_time);
                }
                return time > 0 && _time > time;
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
}