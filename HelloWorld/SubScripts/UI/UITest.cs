using UnityEngine;
using MainAssembly;

namespace SubAssembly
{
    public class UITest : UIBase
    {
        public override void InitUI(GameObject UIObj, int type, int from, params object[] param)
        {
            GameDebug.Log("UITest InitUI");
            base.InitUI(UIObj, type, from, param);
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