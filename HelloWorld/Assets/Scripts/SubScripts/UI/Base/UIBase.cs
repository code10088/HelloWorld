using System.Collections.Generic;
using UnityEngine;
using UnityExtensions.Tween;

namespace HotAssembly
{
    public class UIBase
    {
        protected GameObject UIObj;
        protected UIType type;
        protected UIType from;
        protected Data_UIConfig config;
        private Dictionary<int, int> layerRecord = new Dictionary<int, int>();
        public virtual void InitUI(GameObject UIObj, UIType type, UIType from, Data_UIConfig config, params object[] param)
        {
            this.UIObj = UIObj;
            this.type = type;
            this.from = from;
            this.config = config;
            ResetUILayer();
        }
        public virtual void Refresh(params object[] param)
        {
            ResetUILayer();
            PlayInitAni();
        }
        private void ResetUILayer()
        {
            Canvas[] canvas = UIObj.GetComponentsInChildren<Canvas>(true);
            for (int i = 0; i < canvas.Length; i++)
            {
                int tempKey = canvas[i].GetInstanceID();
                if (layerRecord.TryGetValue(tempKey, out int tempLayer))
                {
                    UIManager.layer += tempLayer;
                    canvas[i].sortingOrder = UIManager.layer;
                }
                else
                {
                    tempLayer = canvas[i].sortingOrder;
                    layerRecord[tempKey] = tempLayer;
                    UIManager.layer += tempLayer;
                    canvas[i].sortingOrder = UIManager.layer;
                }
            }
            UIParticle[] particles = UIObj.GetComponentsInChildren<UIParticle>(true);
            for (int i = 0; i < particles.Length; i++)
            {
                particles[i].Refresh();
            }
        }
        public virtual void PlayInitAni()
        {
            TweenPlayer tp = UIObj.GetComponent<TweenPlayer>();
            if (tp) tp.SetForwardDirectionAndEnabled();
        }
        public virtual void OnDestroy()
        {
            layerRecord.Clear();
        }
        protected virtual void OnClose()
        {
            UIManager.Instance.CloseUI(type);
        }
        protected virtual void OnReture()
        {
            OnClose();
            UIManager.Instance.OpenUI(from);
        }
    }
}