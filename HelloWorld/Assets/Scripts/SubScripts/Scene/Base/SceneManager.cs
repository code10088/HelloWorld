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
        private int from = -1;
        private List<SceneItem> loadScene = new List<SceneItem>();
        private List<SceneItem> curScene = new List<SceneItem>();
        private List<SceneItem> cacheScene = new List<SceneItem>();

        public void Init()
        {
            SceneRoot = GameObject.FindWithTag("SceneRoot");
            tSceneRoot = SceneRoot.transform;
            var temp = GameObject.FindWithTag("MainCamera");
            SceneCamera = temp.GetComponent<Camera>();
            TimeManager.Instance.StartTimer(-1, 1, UpdateProgress);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id">场景配置表里的id，可重复加载</param>
        /// <param name="open"></param>
        /// <param name="progress"></param>
        /// <param name="param"></param>
        /// <returns>唯一id</returns>
        public int OpenScene(int id, Action<bool> open = null, Action<float> progress = null, params object[] param)
        {
            SceneItem item;
            int tempIndex = cacheScene.FindIndex(a => a.ConfigID == id);
            if (tempIndex < 0) item = new SceneItem(id);
            else item = cacheScene[tempIndex];
            item.SetParam(from, open, progress, param);
            if (tempIndex >= 0) cacheScene.RemoveAt(tempIndex);
            loadScene.Add(item);
            item.Load();
            return item.ID;
        }
        public void CloseScene(int id)
        {
            int tempIndex = loadScene.FindIndex(a => a.ID == id);
            if (tempIndex >= 0)
            {
                SceneItem item = loadScene[tempIndex];
                item.Release();
                item.OpenActionInvoke(false);
                loadScene.RemoveAt(tempIndex);
                cacheScene.Add(item);
                return;
            }

            tempIndex = curScene.FindIndex(a => a.ID == id);
            if (tempIndex >= 0)
            {
                SceneItem item = curScene[tempIndex];
                item.Release();
                curScene.RemoveAt(tempIndex);
                cacheScene.Add(item);
                return;
            }
        }
        private void InitScene()
        {
            while (loadScene.Count > 0 && loadScene[0].State)
            {
                loadScene[0].Init();
                loadScene.RemoveAt(0);
            }
        }
        public void UpdateProgress(float f)
        {
            for (int i = 0; i < loadScene.Count; i++)
            {
                loadScene[i].ProgressActionInvoke();
            }
        }


        private class SceneItem
        {
            private static int uniqueId = 0;
            private int id;
            private int from;
            private int loaderID;
            private Data_SceneConfig config;
            private SceneBase baseScene;
            private GameObject baseObj;
            private Action<bool> open = null;
            private Action<float> progress = null;
            private object[] param = null;

            private int state = 0;//7：二进制111：分别表示release init load
            private float releaseTime = 5f;
            private int timerId = -1;

            public int ID => id;
            public int ConfigID => config.ID;
            public SceneBase BaseScene => baseScene;
            public bool State => state > 0;

            public SceneItem(int _id)
            {
                id = ++uniqueId;
                config = ConfigManager.Instance.GameConfigs.Data_SceneConfig.GetDataByID(_id);
            }
            public void SetParam(int _from, Action<bool> _open = null, Action<float> _progress = null, params object[] _param)
            {
                from = _from;
                open = _open;
                param = _param;
                progress = _progress;
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
                    AssetManager.Instance.Unload(loaderID);
                    loaderID = AssetManager.Instance.Load<GameObject>(config.prefabName, LoadFinish);
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
                    state |= 1;
                    baseObj = Object.Instantiate(asset, Vector3.zero, Quaternion.identity, Instance.tSceneRoot) as GameObject;
                    baseObj.SetActive(false);
                }
                releaseTime = Mathf.Lerp(releaseTime, 60f, 0.1f);
                Instance.InitScene();
            }
            public void Init()
            {
                if (state == 1)
                {
                    baseObj.SetActive(true);
                    Type t = System.Type.GetType("HotAssembly." + config.type);
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
                open?.Invoke(success);
            }
            public void ProgressActionInvoke()
            {
                if (progress != null) progress(AssetManager.Instance.GetProgerss(loaderID));
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
                TimeManager.Instance.StopTimer(timerId, false);
                if (baseScene != null) baseScene?.OnDestroy();
                if (baseObj != null) GameObject.Destroy(baseObj);
                AssetManager.Instance.Unload(loaderID);
                loaderID = -1;
                baseScene = null;
                baseObj = null;
                timerId = -1;
                state = 0;
            }
            private void Recycle()
            {
                TimeManager.Instance.StopTimer(timerId, false);
                timerId = -1;
                state &= 3;
            }
        }
    }
}