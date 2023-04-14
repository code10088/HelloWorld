using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using MainAssembly;
using Object = UnityEngine.Object;

namespace HotAssembly
{
    public class UIManager : Singletion<UIManager>
    {
        public GameObject UIRoot;
        public Transform tUIRoot;
        public Camera UICamera;
        private EventSystem eventSystem;
        public static int layer = 0;
        private List<UIItem> loadUI = new List<UIItem>();
        private List<UIItem> curUI = new List<UIItem>();
        private List<UIItem> cacheUI = new List<UIItem>();

        public void Init()
        {
            UIRoot = GameObject.FindWithTag("UIRoot");
            tUIRoot = UIRoot.transform;
            var temp = GameObject.FindWithTag("UICamera");
            UICamera = temp.GetComponent<Camera>();
            temp = GameObject.FindWithTag("EventSystem");
            eventSystem = temp.GetComponent<EventSystem>();
        }
        public void OpenUI(UIType type, Action open = null, params object[] param)
        {
            UIType from = GetFromUI();
            if (type == UIType.Max)
            {
                for (int i = 0; i < loadUI.Count; i++) loadUI[i].Release();
                cacheUI.AddRange(loadUI);
                loadUI.Clear();
                for (int i = 0; i < curUI.Count; i++) curUI[i].Release();
                cacheUI.AddRange(loadUI);
                curUI.Clear();
            }

            int tempIndex = loadUI.FindIndex(a => a.Type == type);
            if (tempIndex >= 0)
            {
                UIItem item = loadUI[tempIndex];
                item.SetParam(from, open, param);
                item.Load();
                return;
            }

            tempIndex = curUI.FindIndex(a => a.Type == type);
            if (tempIndex >= 0)
            {
                UIItem item = curUI[tempIndex];
                item.SetParam(from, open, param);
                curUI.RemoveAt(tempIndex);
                loadUI.Add(item);
                item.Load();
                return;
            }

            tempIndex = cacheUI.FindIndex(a => a.Type == type);
            if (tempIndex >= 0)
            {
                UIItem item = cacheUI[tempIndex];
                item.SetParam(from, open, param);
                cacheUI.RemoveAt(tempIndex);
                loadUI.Add(item);
                item.Load();
                return;
            }

            UIItem _item = new UIItem(type);
            _item.SetParam(from, open, param);
            loadUI.Add(_item);
            _item.Load();
        }
        private void InitUI(UIItem item)
        {
            while (loadUI.Count > 0 && loadUI[0].State1)
            {
                loadUI[0].Init();
                if (loadUI[0].State2) curUI.Add(loadUI[0]);
                loadUI.RemoveAt(0);
            }
        }
        public void CloseUI(UIType type)
        {
            int tempIndex = loadUI.FindIndex(a => a.Type == type);
            if (tempIndex >= 0)
            {
                UIItem item = loadUI[tempIndex];
                item.Release();
                loadUI.RemoveAt(tempIndex);
                cacheUI.Add(item);
                return;
            }

            tempIndex = curUI.FindIndex(a => a.Type == type);
            if (tempIndex >= 0)
            {
                UIItem item = curUI[tempIndex];
                item.Release();
                curUI.RemoveAt(tempIndex);
                cacheUI.Add(item);
                return;
            }
        }
        public UIBase GetUI(UIType type)
        {
            for (int i = 0; i < curUI.Count; i++)
            {
                if (curUI[i].Type == type)
                {
                    return curUI[i].BaseUI;
                }
            }
            return null;
        }
        private UIType GetFromUI()
        {
            //TODO：获取当前非本身、非提示UI
            return UIType.None;
        }

        public void SetEventSystemState(bool state)
        {
            bool cur = eventSystem.enabled;
            if (cur != state) eventSystem.enabled = state;
        }

        private class UIItem
        {
            private UIType type;
            private UIType from;
            private int loaderID;
            private Data_UIConfig config;
            private UIBase baseUI;
            private GameObject baseObj;
            private Action open = null;
            private object[] param = null;

            private int state = 0;//7：二进制111：分别表示release init load
            private float releaseTime = 10f;
            private int timerId = -1;

            public UIType Type => type;
            public UIBase BaseUI => baseUI;
            public bool State1 => state > 0;
            public bool State2 => state < 4;

            public UIItem(UIType type)
            {
                this.type = type;
                config = ConfigManager.Instance.GameConfigs.Data_UIConfig.GetDataByID((int)type);
            }
            public void SetParam(UIType from, Action open = null, params object[] param)
            {
                this.from = from;
                this.open = open;
                this.param = param;
            }
            public void Load()
            {
                if (state > 3)
                {
                    Recycle();
                }
                if (state > 0)
                {
                    LoadFinish(loaderID, baseObj);
                }
                else
                {
                    Instance.SetEventSystemState(false);
                    if (loaderID <= 0) loaderID = AssetManager.Instance.Load<GameObject>(config.prefabName, LoadFinish);
                }
            }
            private void LoadFinish(int id, Object asset)
            {
                if (asset == null)
                {
                    Release(true);
                }
                else if ((state & 1) == 0)
                {
                    state |= 1;
                    baseObj = Object.Instantiate(asset) as GameObject;
                    baseObj.transform.SetParent(Instance.tUIRoot);
                    baseObj.transform.localPosition = Vector3.zero;
                    baseObj.transform.localRotation = Quaternion.identity;
                    baseObj.transform.localScale = Vector3.one;
                    RectTransform rt = baseObj.GetComponent<RectTransform>();
                    rt.anchoredPosition3D = Vector3.zero;
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.sizeDelta = Vector2.zero;
                }
                releaseTime = Mathf.Lerp(releaseTime, 120f, 0.2f);
                Instance.SetEventSystemState(true);
                Instance.InitUI(this);
            }
            public void Init()
            {
                if (state == 1)
                {
                    Type t = System.Type.GetType("HotAssembly." + type);
                    baseUI = Activator.CreateInstance(t) as UIBase;
                    baseUI.InitUI(baseObj, type, from, config, param);
                    open?.Invoke();
                    state = 3;
                }
                else if (state == 3)
                {
                    baseUI.Refresh(param);
                    open?.Invoke();
                }
            }
            public void Release(bool immediate = false)
            {
                if (immediate) _Release();
                else if (timerId < 0) timerId = TimeManager.Instance.StartTimer(releaseTime, finish: _Release);
                state |= 4;
            }
            private void _Release()
            {
                if (baseUI != null) baseUI.OnDestroy();
                if (baseObj != null) GameObject.Destroy(baseObj);
                AssetManager.Instance.Unload(loaderID);
                config = null;
                baseUI = null;
                baseObj = null;
                timerId = -1;
                state = 0;
            }
            private void Recycle()
            {
                TimeManager.Instance.StopTimer(timerId);
                timerId = -1;
                state &= 3;
            }
        }
    }
}