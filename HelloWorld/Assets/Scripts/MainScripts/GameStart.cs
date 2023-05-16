using System;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MainAssembly
{
    public class GameStart : MonoBehaviour
    {
        private void Start()
        {
            Application.runInBackground = true;
            AssetManager.Instance.Init(HotUpdate);
        }
        private void HotUpdate()
        {
            HotUpdateCode.Instance.StartUpdate(StartHotAssembly);
        }
        private void StartHotAssembly()
        {
#if UNITY_EDITOR
            HotAssembly.GameStart.Instance.Init();
#else
            AssetManager.Instance.Load<TextAsset>("HotAssembly", StartHotAssembly);
#endif
        }
        private void StartHotAssembly(int id, Object asset)
        {
            AssetManager.Instance.Unload(id);
            TextAsset ta = asset as TextAsset;
            var hotAssembly = Assembly.Load(ta.bytes);
            Type t = hotAssembly.GetType("HotAssembly.GameStart");
            PropertyInfo p = t.BaseType.GetProperty("Instance");
            object o = p.GetMethod.Invoke(null, null);
            MethodInfo m = t.GetMethod("Init");
            m.Invoke(o, null);
        }
        private void Update()
        {
            AsyncManager.Instance.Update();
        }
    }
}