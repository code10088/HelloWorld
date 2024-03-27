using cfg;
using System.Collections.Generic;
using UnityEngine;

namespace HotAssembly
{
    public class GuideData : Database
    {
        private int guideId;
        private Guide guideCfg;
        private Dictionary<int, Transform> guideT = new Dictionary<int, Transform>();

        public Guide GuideCfg => guideCfg;

        public void AddGuideT(int key, Transform t)
        {
            guideT[key] = t;
        }
        public void RemoveGuideT(int key)
        {
            guideT.Remove(key);
        }
        public Transform GetGuideT()
        {
            Transform temp = null;
            guideT.TryGetValue(guideId, out temp);
            return temp;
        }
        public void StartGuide(int id)
        {
            guideId = id;
            if (guideId <= 0) return;
            guideCfg = ConfigManager.Instance.GameConfigs.TbGuide[id];
            if (guideCfg == null) return;
            UIManager.Instance.OpenUI(UIType.UIGuide);
        }
        public void Next()
        {
            guideId = guideCfg.Next;
            if (guideId <= 0)
            {
                End();
                return;
            }
            guideCfg = ConfigManager.Instance.GameConfigs.TbGuide[guideId];
            if (guideCfg == null)
            {
                End();
                return;
            }
            EventManager.Instance.FireEvent(EventType.RefreshGuide);
        }
        public void Skip()
        {
            if (guideCfg.CanSkip == 1) Next();
            else End();
        }
        public void End()
        {
            UIManager.Instance.CloseUI(UIType.UIGuide);
        }

        public void Clear()
        {

        }
    }
}
