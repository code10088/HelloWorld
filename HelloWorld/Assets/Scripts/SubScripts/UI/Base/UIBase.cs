using cfg;
using UnityEngine;
using UnityExtensions.Tween;
using System.Collections.Generic;
using UnityEngine.UI;
using System;

namespace HotAssembly
{
    public class UIBase
    {
        protected GameObject UIObj;
        protected UIType from;
        protected UIConfig config;
        protected Canvas[] layerRecord1;
        protected int[] layerRecord2;
        protected UIParticle[] layerRecord3;
        private List<int> loader1 = new List<int>();
        private List<LoadGameObjectItem> loader2 = new List<LoadGameObjectItem>();
        public void InitUI(GameObject UIObj, UIType from, UIConfig config, params object[] param)
        {
            this.UIObj = UIObj;
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
        public virtual void OnEnable(params object[] param)
        {
            RefreshUILayer();
            PlayInitAni();
        }
        protected virtual void RefreshUILayer()
        {
            for (int i = 0; i < layerRecord1.Length; i++)
            {
                if (layerRecord1[i] != null)
                {
                    UIManager.layer += Math.Max(layerRecord2[i], 1);
                    layerRecord1[i].sortingLayerName = config.UIWindowType.ToString();
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
            for (int i = 0; i < loader1.Count; i++)
            {
                int loadId = loader1[i];
                AssetManager.Instance.Unload(ref loadId);
            }
            loader1.Clear();
            for (int i = 0; i < loader2.Count; i++)
            {
                loader2[i].SetActive(false);
            }
        }
        public virtual void OnDestroy()
        {
            layerRecord1 = null;
            layerRecord2 = null;
            layerRecord3 = null;
            for (int i = 0; i < loader2.Count; i++) loader2[i].Release();
            loader1 = null;
            loader2 = null;
        }
        protected virtual void OnClose()
        {
            UIManager.Instance.CloseUI(config.UIType);
        }

        #region 扩展方法
        protected void SetSprite(Image image, string atlas, string name)
        {
            int loadId = -1;
            AssetManager.Instance.Load<Sprite>(ref loadId, $"{atlas}{name}.png", (a, b) => image.sprite = (Sprite)b);
            loader1.Add(loadId);
        }
        /// <summary>
        /// 背景图
        /// </summary>
        /// <param name="path">相对于Assets/ZRes/UI/Texture的相对路径</param>
        protected void SetSprite(RawImage image, string path)
        {
            int loadId = -1;
            AssetManager.Instance.Load<Texture>(ref loadId, $"{ZResConst.ResUITexturePath}{path}.png", (a, b) => image.texture = (Texture)b);
            loader1.Add(loadId);
        }
        /// <summary>
        /// 加载Prefab
        /// </summary>
        /// <param name="path">相对于Assets/ZRes/UI/Prefab的相对路径</param>
        protected void LoadPrefab(string path, Action<GameObject> finish)
        {
            int loadId = -1;
            AssetManager.Instance.Load<GameObject>(ref loadId, $"{ZResConst.ResUIPrefabPath}{path}.prefab", (a, b) => finish?.Invoke((GameObject)b));
            loader1.Add(loadId);
        }
        protected void AddEffect(string path, Transform parent)
        {
            path = $"{ZResConst.ResUIEffectPath}{path}.prefab";
            var item = new LoadGameObjectItem();
            item.Init(path, parent);
            item.SetActive(true);
            loader2.Add(item);
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