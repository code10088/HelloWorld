using cfg;
using UnityEngine;
using UnityExtensions.Tween;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine.UI;
using System;

namespace HotAssembly
{
    public class UIBase
    {
        protected GameObject UIObj;
        protected UIType type;
        protected UIType from;
        protected UIConfig config;
        protected bool active = false;
        protected Canvas[] layerRecord1;
        protected int[] layerRecord2;
        protected UIParticle[] layerRecord3;
        private List<int> loaders = new();
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
            for (int i = 0; i < loaders.Count; i++) AssetManager.Instance.Unload(loaders[i]);
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

        #region 扩展方法
        protected void SetSprite(Image image, string name)
        {
            int id = AssetManager.Instance.Load<Sprite>(name, (a, b) => image.sprite = (Sprite)b);
            loaders.Add(id);
        }
        protected void SetSprite(RawImage image, string name)
        {
            int id = AssetManager.Instance.Load<Texture>(name, (a, b) => image.texture = (Texture)b);
            loaders.Add(id);
        }
        protected void LoadPrefab(string name, Action<GameObject> finish)
        {
            int id = AssetManager.Instance.Load<GameObject>(name, (a, b) => finish?.Invoke((GameObject)b));
            loaders.Add(id);
        }
        protected GameObject Instantiate(GameObject obj, Transform parent = null)
        {
            var result = GameObject.Instantiate(obj);
            if (parent) result.transform.SetParent(parent);
            result.transform.localPosition = Vector3.zero;
            result.transform.localRotation = Quaternion.identity;
            result.transform.localScale = Vector3.one;
            return result;
        }
        #endregion
    }
}