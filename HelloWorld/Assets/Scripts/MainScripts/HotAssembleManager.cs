using System;
using UnityEngine;
using System.Reflection;

namespace MainAssembly
{
    public class HotAssembleManager : Singletion<HotAssembleManager>
    {
        private Assembly hotAssembly;
        private Action finish;

        public void Init(Action finish)
        {
            this.finish = finish;
            AssetManager.Instance.Load<TextAsset>("HotAssembly", StartHotAssembly);
        }
        private void StartHotAssembly(int id, dynamic asset, dynamic param = null)
        {
            AssetManager.Instance.Unload(id);
            hotAssembly = Assembly.Load(asset.bytes);
            Type t = hotAssembly.GetType("HotAssembly.GameStart");
            PropertyInfo p = t.BaseType.GetProperty("Instance");
            dynamic o = p.GetMethod.Invoke(null, null);
            o.Init();
            finish?.Invoke();
        }
        public dynamic Invoke(string type, string method, dynamic param)
        {
            if (hotAssembly == null) return null;
            Type t = hotAssembly.GetType("HotAssembly." + type);
            PropertyInfo p = t.BaseType.GetProperty("Instance");
            object o = p.GetMethod.Invoke(null, null);
            return t.GetMethod(method).Invoke(o, param);
        }
    }
}