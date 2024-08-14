using cfg;
using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HotAssembly
{
    public partial class SceneManager : Singletion<SceneManager>, SingletionInterface
    {
        public GameObject SceneRoot;
        public Transform tSceneRoot;
        public Camera SceneCamera;
        private List<SceneItem> loadScene = new List<SceneItem>();
        private List<SceneItem> curScene = new List<SceneItem>();
        private List<SceneItem> cacheScene = new List<SceneItem>();
        private int timerId = -1;

        public void Init()
        {
            SceneRoot = GameObject.FindWithTag("SceneRoot");
            tSceneRoot = SceneRoot.transform;
            var temp = GameObject.FindWithTag("MainCamera");
            SceneCamera = temp.GetComponent<Camera>();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id">场景配置表里的id，可重复加载</param>
        /// <param name="open"></param>
        /// <param name="progress"></param>
        /// <param name="param"></param>
        /// <returns>唯一id</returns>
        public int OpenScene(SceneType type, Action<int, bool> open = null, Action<float> progress = null, params object[] param)
        {
            SceneItem item;
            int tempIndex = cacheScene.FindIndex(a => a.Type == type);
            if (tempIndex < 0) item = new SceneItem(type);
            else item = cacheScene[tempIndex];
            SceneType from = SceneType.SceneBase;
            if (curScene.Count > 0) from = curScene[curScene.Count - 1].Type;
            item.SetParam(from, open, progress, param);
            if (tempIndex >= 0) cacheScene.RemoveAt(tempIndex);
            loadScene.Add(item);
            item.Load();
            if (timerId < 0) timerId = TimeManager.Instance.StartTimer(-1, 1, UpdateProgress);
            return item.ID;
        }
        private void InitScene()
        {
            while (loadScene.Count > 0 && loadScene[0].State)
            {
                loadScene[0].Init();
                loadScene.RemoveAt(0);
            }
        }
        public void CloseScene(int id)
        {
            int tempIndex = loadScene.FindLastIndex(a => a.ID == id);
            if (tempIndex >= 0)
            {
                SceneItem item = loadScene[tempIndex];
                item.Release();
                item.OpenActionInvoke(false);
                loadScene.RemoveAt(tempIndex);
                cacheScene.Add(item);
                return;
            }

            tempIndex = curScene.FindLastIndex(a => a.ID == id);
            if (tempIndex >= 0)
            {
                SceneItem item = curScene[tempIndex];
                item.Release();
                curScene.RemoveAt(tempIndex);
                cacheScene.Add(item);
                return;
            }
        }
        public void CloseScene(SceneType type)
        {
            int tempIndex = loadScene.FindLastIndex(a => a.Type == type);
            if (tempIndex >= 0)
            {
                SceneItem item = loadScene[tempIndex];
                item.Release();
                item.OpenActionInvoke(false);
                loadScene.RemoveAt(tempIndex);
                cacheScene.Add(item);
                return;
            }

            tempIndex = curScene.FindLastIndex(a => a.Type == type);
            if (tempIndex >= 0)
            {
                SceneItem item = curScene[tempIndex];
                item.Release();
                curScene.RemoveAt(tempIndex);
                cacheScene.Add(item);
                return;
            }
        }
        public SceneBase GetScene(int id)
        {
            var result = curScene.FindLast(a => a.ID == id);
            return result == null ? null : result.BaseScene;
        }
        public SceneBase GetScene(SceneType type)
        {
            var result = curScene.FindLast(a => a.Type == type);
            return result == null ? null : result.BaseScene;
        }
        public bool HasOpen(int id)
        {
            int tempIndex = loadScene.FindLastIndex(a => a.ID == id);
            if (tempIndex >= 0) return true;
            tempIndex = curScene.FindLastIndex(a => a.ID == id);
            if (tempIndex >= 0) return true;
            return false;
        }
        public bool HasOpen(SceneType type)
        {
            int tempIndex = loadScene.FindLastIndex(a => a.Type == type);
            if (tempIndex >= 0) return true;
            tempIndex = curScene.FindLastIndex(a => a.Type == type);
            if (tempIndex >= 0) return true;
            return false;
        }

        public void UpdateProgress(float f)
        {
            if (loadScene.Count > 0) for (int i = 0; i < loadScene.Count; i++) loadScene[i].ProgressActionInvoke();
            else TimeManager.Instance.StopTimer(timerId);
        }

        private class SceneItem
        {
            private static int uniqueId = 0;
            private int id;
            private SceneType from;
            private int loadId;
            private SceneConfig config;
            private SceneBase baseScene;
            private GameObject baseObj;
            private Action<int, bool> open = null;
            private Action<float> progress = null;
            private object[] param = null;

            private int state = 0;//7：二进制111：分别表示release init load
            private float releaseTime = 5f;
            private int timerId = -1;

            public int ID => id;
            public SceneType Type => config.SceneType;
            public SceneBase BaseScene => baseScene;
            public bool State => state > 0;

            public SceneItem(SceneType type)
            {
                id = ++uniqueId;
                config = ConfigManager.Instance.GameConfigs.TbSceneConfig[type];
            }
            public void SetParam(SceneType _from, Action<int, bool> _open = null, Action<float> _progress = null, params object[] _param)
            {
                from = _from;
                open = _open;
                param = _param;
                progress = _progress;
                UIManager.Instance.OpenUI(UIType.UISceneLoading);
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
                    baseObj = Object.Instantiate(asset, Instance.tSceneRoot) as GameObject;
                    baseObj.transform.localPosition = Vector3.zero;
                    baseObj.transform.localRotation = Quaternion.identity;
                    baseObj.transform.localScale = Vector3.one;
                    baseObj.SetActive(false);
                }
                releaseTime = Mathf.Lerp(releaseTime, GameSetting.recycleTimeMaxS, 0.1f);
                Instance.InitScene();
            }
            public void Init()
            {
                if (state == 1)
                {
                    baseObj.SetActive(true);
                    Type t = System.Type.GetType("HotAssembly." + config.Name);
                    baseScene = Activator.CreateInstance(t) as SceneBase;
                    baseScene.InitScene(baseObj, id, from, config, param);
                    Instance.curScene.Add(this);
                    OpenActionInvoke(true);
                    state = 3;
                }
                else if (state == 3)
                {
                    baseObj.SetActive(true);
                    baseScene.OnEnable(param);
                    Instance.curScene.Add(this);
                    OpenActionInvoke(true);
                }
                else
                {
                    OpenActionInvoke(false);
                }
            }
            public void OpenActionInvoke(bool success)
            {
                UIManager.Instance.CloseUI(UIType.UISceneLoading);
                open?.Invoke(id, success);
            }
            public void ProgressActionInvoke()
            {
                float f = AssetManager.Instance.GetProgerss(loadId);
                EventManager.Instance.FireEvent(EventType.SetSceneLoadingProgress, "Loading Scene", f);
                if (progress != null) progress(f);
            }
            public void Release(bool immediate = false)
            {
                baseObj?.SetActive(false);
                baseScene?.OnDisable();
                if (immediate) _Release();
                else if (timerId < 0) timerId = TimeManager.Instance.StartTimer(releaseTime, finish: _Release);
                state |= 4;
            }
            private void _Release()
            {
                Instance.cacheScene.Remove(this);
                TimeManager.Instance.StopTimer(timerId);
                if (baseScene != null) baseScene?.OnDestroy();
                if (baseObj != null) GameObject.Destroy(baseObj);
                AssetManager.Instance.Unload(ref loadId);
                baseScene = null;
                baseObj = null;
                open = null;
                progress = null;
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