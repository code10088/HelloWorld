using UnityEngine;
using System;
using Object = UnityEngine.Object;

namespace HotAssembly
{
    public class UISubItem : UIBase
    {
        #region 加载
        private string prefabPath;
        private Transform parent;
        private int loadId;
        private Object asset;
        private Action<bool> open = null;
        private object[] param = null;

        private LoadState state = LoadState.Release;
        private float releaseTime = 10f;
        private int timerId = -1;

        /// <summary>
        /// 子界面
        /// </summary>
        /// <param name="path">相对于Assets/ZRes/UI/Prefab的相对路径</param>
        public UISubItem(string path)
        {
            prefabPath = path;
        }
        private void Load(Transform parent, Action<bool> open = null, params object[] param)
        {
            this.parent = parent;
            this.open = open;
            this.param = param;
            if (state.HasFlag(LoadState.Release))
            {
                Recycle();
            }
            switch (state)
            {
                case LoadState.None:
                    state = LoadState.Loading;
                    AssetManager.Instance.Load<GameObject>(ref loadId, $"{ZResConst.ResUIPrefabPath}{prefabPath}.prefab", LoadFinish);
                    break;
                case LoadState.Loading:
                    break;
                case LoadState.LoadFinish:
                    LoadFinish(loadId, asset);
                    break;
                case LoadState.Instantiate:
                    UIObj.SetActive(true);
                    OnEnable(param);
                    open?.Invoke(true);
                    break;
            }
        }
        private void LoadFinish(int id, Object _asset)
        {
            if (_asset == null)
            {
                Release(true);
                open?.Invoke(false);
            }
            else if (state.HasFlag(LoadState.Release))
            {
                asset = _asset;
                state = LoadState.LoadFinish | LoadState.Release;
                open?.Invoke(false);
            }
            else
            {
                state = LoadState.Instantiate;
                UIObj = Object.Instantiate(_asset, parent) as GameObject;
                UIObj.transform.localPosition = Vector3.zero;
                UIObj.transform.localRotation = Quaternion.identity;
                UIObj.transform.localScale = Vector3.one;
                RectTransform rt = UIObj.GetComponent<RectTransform>();
                rt.anchoredPosition3D = Vector3.zero;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.sizeDelta = Vector2.zero;
                Init();
                OnEnable(param);
                open?.Invoke(true);
            }
        }
        private void Release(bool immediate = false)
        {
            UIObj?.SetActive(false);
            if (state.HasFlag(LoadState.Instantiate)) OnDisable();
            if (immediate) _Release();
            else if (timerId < 0) timerId = TimeManager.Instance.StartTimer(releaseTime, finish: _Release);
            state |= LoadState.Release;
        }
        private void _Release()
        {
            OnDestroy();
            asset = null;
            if (UIObj != null) GameObject.Destroy(UIObj);
            UIObj = null;
            AssetManager.Instance.Unload(ref loadId);
            parent = null;
            open = null;
            param = null;
            state = LoadState.Release;
            releaseTime = Mathf.Lerp(releaseTime, GameSetting.recycleTimeMinS, 0.2f);
            TimeManager.Instance.StopTimer(timerId);
            timerId = -1;
        }
        private void Recycle()
        {
            state &= LoadState.Instantiate | LoadState.LoadFinish | LoadState.Loading;
            releaseTime = Mathf.Lerp(releaseTime, GameSetting.recycleTimeMaxS, 0.2f);
            TimeManager.Instance.StopTimer(timerId);
            timerId = -1;
        }
        #endregion

        private bool active = false;
        public bool Active => active;

        public void SetActive(Transform parent, bool state, Action<bool> open = null, params object[] param)
        {
            if (state && !active) Open(parent, open, param);
            else if (!state && active) Close();
        }
        public void Open(Transform parent, Action<bool> open = null, params object[] param)
        {
            active = true;
            Load(parent, open, param);
        }
        public void Close(bool immediate = false)
        {
            active = false;
            Release(immediate);
        }
        protected override void RefreshUILayer()
        {
            Canvas canvas = parent.GetComponentInParent<Canvas>();
            for (int i = 0; i < layerRecord1.Length; i++)
            {
                if (layerRecord1[i] != null)
                {
                    layerRecord1[i].sortingLayerName = canvas.sortingLayerName;
                    layerRecord1[i].sortingOrder = canvas.sortingOrder + layerRecord2[i];
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
    }
}