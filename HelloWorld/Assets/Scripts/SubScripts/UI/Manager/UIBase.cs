using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityExtensions.Tween;

public class UIBase
{
    public GameObject UIObj;
    public virtual void InitUI(GameObject UIObj, params object[] param)
    {
        this.UIObj = UIObj;
        OnPlayUIAnimation();
    }
    public virtual void Refresh(params object[] param)
    {

    }
    public virtual void OnPlayUIAnimation()
    {
        TweenPlayer tp = UIObj.GetComponent<TweenPlayer>();
        if (tp) tp.SetForwardDirectionAndEnabled();
    }
	public virtual void OnDestroy()
    {

    }
}
