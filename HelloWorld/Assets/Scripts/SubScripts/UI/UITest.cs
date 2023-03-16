using UnityEngine;
using MainAssembly;

namespace HotAssembly
{
    public class UITest : UIBase
    {
        public override void InitUI(GameObject UIObj, Data_UIConfig config, UIType from, params object[] param)
        {
            GameDebug.Log("UITest InitUI");
            base.InitUI(UIObj, config, from, param);
        }
        public override void Refresh(params object[] param)
        {
            GameDebug.Log("UITest Refresh");
            base.Refresh(param);
        }
        public override void PlayInitAni()
        {
            GameDebug.Log("UITest OnPlayUIAnimation");
            base.PlayInitAni();
        }
        public override void OnDestroy()
        {
            GameDebug.Log("UITest OnDestroy");
            base.OnDestroy();
        }
    }
}