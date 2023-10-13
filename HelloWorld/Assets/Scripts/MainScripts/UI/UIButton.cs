using System;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIButton : Button
{
    public override void OnPointerClick(PointerEventData eventData)
    {
        AudioManager.Instance.PlaySound("Frequence/Button3");
        base.OnPointerClick(eventData);
    }
}
