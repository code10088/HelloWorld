using cfg;
using UnityEngine;
using UnityExtensions.Tween;
using Cysharp.Threading.Tasks;

namespace HotAssembly
{
    public class UIBase
    {
        protected GameObject UIObj;
        protected UIType type;
        protected UIType from;
        protected UIConfig config;
        protected bool active = false;
        private Canvas[] layerRecord1;
        private int[] layerRecord2;
        private UIParticle[] layerRecord3;
        public void InitUI(GameObject UIObj, UIType type, UIType from, UIConfig config, params object[] param)
        {
            this.UIObj = UIObj;
            this.type = type;
            this.from = from;
            this.config = config;
            Init();
            OnEnable(param);
        }
        protected virtual void Init()
        {
            layerRecord1 = UIObj.GetComponentsInChildren<Canvas>(true);
            layerRecord2 = new int[layerRecord1.Length];
            for (int i = 0; i < layerRecord1.Length; i++) layerRecord2[i] = layerRecord1[i].sortingOrder;
            layerRecord3 = UIObj.GetComponentsInChildren<UIParticle>(true);
        }
        public virtual async UniTask OnEnable(params object[] param)
        {
            active = true;
            RefreshUILayer();
            PlayInitAni();
            await UniTask.Yield();
        }
        protected virtual void RefreshUILayer()
        {
            for (int i = 0; i < layerRecord1.Length; i++)
            {
                if (layerRecord1[i] != null)
                {
                    UIManager.layer += layerRecord2[i];
                    layerRecord1[i].sortingOrder = UIManager.layer;
                }
            }
            for (int i = 0; i < layerRecord3.Length; i++)
            {
                if (layerRecord3[i] != null)
                {
                    layerRecord3[i].Refresh();
                }
            }
        }
        protected virtual void PlayInitAni()
        {
            TweenPlayer tp = UIObj.GetComponent<TweenPlayer>();
            if (tp) tp.SetForwardDirectionAndEnabled();
        }
        public virtual void OnDisable()
        {
            active = false;
        }
        public virtual void OnDestroy()
        {
            layerRecord1 = null;
            layerRecord2 = null;
            layerRecord3 = null;
        }
        protected void OnClose()
        {
            UIManager.Instance.CloseUI(type);
        }
        protected void OnReture()
        {
            OnClose();
            UIManager.Instance.OpenUI(from);
        }
    }
}