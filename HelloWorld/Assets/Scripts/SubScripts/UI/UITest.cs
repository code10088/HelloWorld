using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UITest : UIBase
{
    public override void InitUI(GameObject UIObj, params object[] param)
    {
        GameDebug.Log("UITest InitUI");
        base.InitUI(UIObj, param);
    }
    public override void Refresh(params object[] param)
    {
        GameDebug.Log("UITest Refresh");
        base.Refresh(param);
    }
    public override void OnPlayUIAnimation()
    {
        GameDebug.Log("UITest OnPlayUIAnimation");
        base.OnPlayUIAnimation();
    }
    public override void OnDestroy()
    {
        GameDebug.Log("UITest OnDestroy");
        base.OnDestroy();
    }
}
