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
        private Action<bool> open = null;
        private object[] param = null;

        private int state = 0;//7：二进制111：分别表示release init load
        private float releaseTime = 10f;
        private int timerId = -1;

        public UISubItem(string path)
        {
            prefabPath = path;
        }
        private void Load(Transform parent, Action<bool> open = null, params object[] param)
        {
            this.parent = parent;
            this.open = open;
            this.param = param;
            if (state > 3)
            {
                Recycle();
            }
            if (state > 0)
            {
                LoadFinish(loadId, UIObj);
            }
            else
            {
                AssetManager.Instance.Load<GameObject>(ref loadId, prefabPath, LoadFinish);
            }
        }
        private void LoadFinish(int id, Object asset)
        {
            if (asset == null)
            {
                Release(true);
                open?.Invoke(false);
            }
            else if (state == 0)
            {
                state = 3;
                UIObj = Object.Instantiate(asset, Vector3.zero, Quaternion.identity, parent) as GameObject;
                RectTransform rt = UIObj.GetComponent<RectTransform>();
                rt.anchoredPosition3D = Vector3.zero;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.sizeDelta = Vector2.zero;
                Init();
                OnEnable(param);
                open?.Invoke(true);
            }
            else if (state == 3)
            {
                UIObj.SetActive(true);
                OnEnable(param);
                open?.Invoke(true);
            }
            releaseTime = Mathf.Lerp(releaseTime, GameSetting.recycleTimeMax, 0.2f);
        }
        private void Release(bool immediate = false)
        {
            UIObj?.SetActive(false);
            OnDisable();
            if (immediate) _Release();
            else if (timerId < 0) timerId = TimeManager.Instance.StartTimer(releaseTime, finish: _Release);
            state |= 4;
        }
        private void _Release()
        {
            TimeManager.Instance.StopTimer(timerId);
            OnDestroy();
            if (UIObj != null) GameObject.Destroy(UIObj);
            AssetManager.Instance.Unload(loadId);
            parent = null;
            loadId = -1;
            UIObj = null;
            open = null;
            param = null;
            timerId = -1;
            state = 0;
        }
        private void Recycle()
        {
            TimeManager.Instance.StopTimer(timerId);
            timerId = -1;
            state &= 3;
        }
        #endregion

        private bool subactive = false;
        public bool Active => subactive;

        public void SetActive(Transform parent, bool state, Action<bool> open = null, params object[] param)
        {
            if (state && !subactive) Open(parent, open, param);
            else if (!state && subactive) Close();
        }
        public void Open(Transform parent, Action<bool> open = null, params object[] param)
        {
            subactive = true;
            Load(parent, open, param);
        }
        public void Close(bool immediate = false)
        {
            subactive = false;
            Release(immediate);
        }
        protected override void RefreshUILayer()
        {
            Canvas canvas = parent.GetComponentInParent<Canvas>();
            for (int i = 0; i < layerRecord1.Length; i++)
            {
                if (layerRecord1[i] != null)
                {
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