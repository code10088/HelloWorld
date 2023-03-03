using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UnityExtensions.Tween
{
    public partial class TweenPlayer : BytesDecodeInterface
    {
        public TextAsset config;
        public bool tweenGrid = false; 
        public float tweenGridInterval = 0;
        public bool tweenGridDir = true;

        [SerializeField] float itemDurationRecord;
        [SerializeField] List<TweenAnimation> itemAnimationsRecord;

        public void RefreshTweenGrid()
        {
            int childCount = transform.childCount;
            duration = childCount * tweenGridInterval - tweenGridInterval + itemDurationRecord;
            _animations = new List<TweenAnimation>();
            for (int i = 0; i < childCount; i++)
            {
                int index = tweenGridDir ? i : childCount - i - 1;
                Transform childT = transform.GetChild(index);
                float start = i * tweenGridInterval;
                float end = i * tweenGridInterval;
                for (int j = 0; j < itemAnimationsRecord.Count; j++)
                {
                    TweenAnimation tempDefault = itemAnimationsRecord[j];
                    TweenAnimation tempNew = tempDefault.Clone();
                    float _start = start + tempDefault.minNormalizedTime * itemDurationRecord;
                    float _end = end + tempDefault.maxNormalizedTime * itemDurationRecord;
                    tempNew.minNormalizedTime = _start / duration;
                    tempNew.maxNormalizedTime = _end / duration;
                    if (tempDefault is ITweenFromToWithTarget td)
                    {
                        var tn = tempNew as ITweenFromToWithTarget;
                        Type type = td.target.GetType();
                        Transform t = childT.GetChild(0);
                        tn.target = t.GetComponent(type);
                    }
                    _animations.Add(tempNew);
                }
            }
        }

        public void Serialize(BytesDecode bd)
        {
            bd.ToBytes(duration);
            bd.ToBytes((int)updateMode);
            bd.ToBytes((int)timeMode);
            bd.ToBytes((int)wrapMode);
            bd.ToBytes((int)arrivedAction);
            bd.ToBytes(sampleOnAwake);
            bd.ToBytes(tweenGrid);
            bd.ToBytes(tweenGridInterval);
            bd.ToBytes(tweenGridDir);
            bd.ToBytes(animationCount);
            for (int i = 0; i < animationCount; i++)
            {
                var temp = _animations[i];
                Type t = temp.GetType();
                bd.ToBytes(t.ToString());
                temp.Serialize(bd);
            }
        }

        public void Deserialize(BytesDecode bd)
        {
            duration = bd.ToFloat();
            updateMode = (UpdateMode)bd.ToInt();
            timeMode = (TimeMode)bd.ToInt();
            wrapMode = (WrapMode)bd.ToInt();
            arrivedAction = (ArrivedAction)bd.ToInt();
            sampleOnAwake = bd.ToBool();
            tweenGrid = bd.ToBool();
            tweenGridInterval = bd.ToFloat();
            tweenGridDir = bd.ToBool();
            int count = bd.ToInt();
            _animations.Clear();
            for (int i = 0; i < count; i++)
            {
                string typeStr = bd.ToStr();
                Type t = Type.GetType(typeStr);
                object obj = Activator.CreateInstance(t);
                var ta = obj as TweenAnimation;
                ta.Deserialize(bd);
                _animations.Add(ta);
            }
        }
    }
}
