using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityExtensions.Tween;

namespace MainAssembly
{
    public class UIBase
    {
        protected GameObject UIObj;
        protected int type;
        protected int from;
        private Dictionary<int, int> layerRecord = new Dictionary<int, int>();
        public virtual void InitUI(GameObject UIObj, int type, int from, params object[] param)
        {
            this.UIObj = UIObj;
            this.type = type;
            this.from = from;
            ResetUILayer();
            PlayInitAni();
        }
        public virtual void Refresh(params object[] param)
        {
            ResetUILayer();
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
            UIManager.Instance.OpenUI(from);
        }
    }
}