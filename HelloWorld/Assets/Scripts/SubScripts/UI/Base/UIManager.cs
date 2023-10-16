using cfg;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Object = UnityEngine.Object;

namespace HotAssembly
{
    public partial class UIManager : Singletion<UIManager>, SingletionInterface
    {
        public GameObject UIRoot;
        public Transform tUIRoot;
        public Camera UICamera;
        private EventSystem eventSystem;
        private Vector2 anchorMin = Vector2.zero;
        public static Vector2 anchorMinFull = Vector2.zero;

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
            //适配
            anchorMin.x = Screen.safeArea.x / Screen.width;
            anchorMinFull.x = anchorMin.x == 0 ? 0 : 1 / anchorMin.x;
        }
        public void OpenUI(UIType type, Action<bool> open = null, params object[] param)
        {
            UIType from = GetFromUI(type);

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
        private void InitUI()
        {
            while (loadUI.Count > 0 && loadUI[0].State)
            {
                loadUI[0].Init();
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
                item.OpenActionInvoke(false);
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
        private UIType GetFromUI(UIType type)
        {
            for (int i = curUI.Count - 1; i >= 0; i--)
            {
                if(curUI[i].Type != type && curUI[i].Config.UIWindowType != UIWindowType.Tips)
                {
                    return curUI[i].Type;
                }
            }
            return UIType.UIMain;
        }
        public bool HasOpen(UIType type)
        {
            for (int i = 0; i < loadUI.Count; i++)
            {
                if (loadUI[i].Type == type)
                {
                    return true;
                }
            }
            for (int i = 0; i < curUI.Count; i++)
            {
                if (curUI[i].Type == type)
                {
                    return true;
                }
            }
            return false;
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
            private int loadId;
            private UIConfig config;
            private UIBase baseUI;
            private GameObject baseObj;
            private Action<bool> open = null;
            private object[] param = null;

            private int state = 0;//7：二进制111：分别表示release init load
            private float releaseTime = 10f;
            private int timerId = -1;

            public UIType Type => type;
            public UIBase BaseUI => baseUI;
            public UIConfig Config => config;
            public bool State => state > 0;

            public UIItem(UIType type)
            {
                this.type = type;
                config = ConfigManager.Instance.GameConfigs.TbUIConfig[type];
            }
            public void SetParam(UIType from, Action<bool> open = null, params object[] param)
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
                    LoadFinish(loadId, baseObj);
                }
                else
                {
                    Instance.SetEventSystemState(false);
                    AssetManager.Instance.Load<GameObject>(ref loadId, config.PrefabPath, LoadFinish);
                }
            }
            private void LoadFinish(int id, Object asset)
            {
                if (asset == null)
                {
                    Release(true);
                }
                else if (state == 0)
                {
                    state = 1;
                    baseObj = Object.Instantiate(asset, Vector3.zero, Quaternion.identity, Instance.tUIRoot) as GameObject;
                    RectTransform rt = baseObj.GetComponent<RectTransform>();
                    rt.anchoredPosition3D = Vector3.zero;
                    rt.anchorMin = Instance.anchorMin;
                    rt.anchorMax = Vector2.one;
                    rt.sizeDelta = Vector2.zero;
                    baseObj.SetActive(false);
                }
                releaseTime = Mathf.Lerp(releaseTime, GameSetting.recycleTimeMax, 0.2f);
                Instance.SetEventSystemState(true);
                Instance.InitUI();
            }
            public void Init()
            {
                if (state == 1)
                {
                    baseObj.SetActive(true);
                    Type t = System.Type.GetType("HotAssembly." + type);
                    baseUI = Activator.CreateInstance(t) as UIBase;
                    baseUI.InitUI(baseObj, type, from, config, param);
                    Instance.curUI.Add(this);
                    OpenActionInvoke(true);
                    state = 3;
                }
                else if (state == 3)
                {
                    baseObj.SetActive(true);
                    baseUI.OnEnable(param);
                    Instance.curUI.Add(this);
                    OpenActionInvoke(true);
                }
                else
                {
                    OpenActionInvoke(false);
                }
            }
            public void OpenActionInvoke(bool success)
            {
                open?.Invoke(success);
            }
            public void Release(bool immediate = false)
            {
                baseObj?.SetActive(false);
                baseUI?.OnDisable();
                if (immediate) _Release();
                else if (timerId < 0) timerId = TimeManager.Instance.StartTimer(releaseTime, finish: _Release);
                state |= 4;
            }
            private void _Release()
            {
                TimeManager.Instance.StopTimer(timerId);
                if (baseUI != null) baseUI.OnDestroy();
                if (baseObj != null) GameObject.Destroy(baseObj);
                AssetManager.Instance.Unload(loadId);
                loadId = -1;
                baseUI = null;
                baseObj = null;
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
        }
    }
}