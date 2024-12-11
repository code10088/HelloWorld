using System;
using System.Collections.Generic;
using UnityEngine;

namespace HotAssembly
{
    public class WeChatAd : Singletion<WeChatAd>, SingletionInterface
    {
        public const string WeChatAdUnitId1 = "adunit-19a8b14fbb55ce9e";

        class WeChatAdUnit
        {
            public string name;
            public string id;
            public bool load;
            public int retry;
        }

        private List<WeChatAdUnit> list = new List<WeChatAdUnit>();
        private Action<bool> onClose;

        public void Init()
        {
            MainAssemblyInterface.InitWeChatAd(OnError, OnLoad, OnClose);
        }
        private void OnError(string id, int err)
        {
            Debug.LogError($"错误：{id} {err}");
            var unit = list.Find(a => a.id == id);
            if (unit == null) return;
            if (unit.retry < 3)
            {
                MainAssemblyInterface.LoadWeChatAd(id);
                unit.retry++;
            }
            else
            {
                MainAssemblyInterface.DestroyWeChatAd(id);
                list.Remove(unit);
            }
        }
        private void OnLoad(string id, string err)
        {
            var unit = list.Find(a => a.id == id);
            if (unit == null) return;
            if (string.IsNullOrEmpty(err))
            {
                unit.load = true;
                unit.retry = 0;
            }
            else if (unit.retry < 3)
            {
                MainAssemblyInterface.LoadWeChatAd(id);
                unit.retry++;
            }
            else
            {
                MainAssemblyInterface.DestroyWeChatAd(id);
                list.Remove(unit);
            }
        }
        private void OnClose(string id, bool end)
        {
            onClose?.Invoke(end);
        }
        public void Init(string adUnitId)
        {
            var index = list.FindIndex(a => a.name == adUnitId);
            if (index >= 0) return;
            var unit = new WeChatAdUnit();
            unit.name = adUnitId;
            unit.id = MainAssemblyInterface.InitWeChatAd(adUnitId);
            unit.load = false;
            unit.retry = 0;
            list.Add(unit);
        }
        public void Show(string adUnitId, Action<bool> onClose)
        {
            var unit = list.Find(a => a.name == adUnitId);
            if (unit == null)
            {
                UICommonTips.ShowTips("广告准备失败，请重新登录重试");
                return;
            }
            if (!unit.load)
            {
                UICommonTips.ShowTips("广告准备中，请稍后重试");
                return;
            }
            this.onClose = onClose;
            MainAssemblyInterface.ShowWeChatAd(unit.id);
            unit.load = false;
            unit.retry = 0;
        }
    }
}