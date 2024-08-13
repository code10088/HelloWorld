using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIButton : Button
{
    public string soundPath = "Frequence/Button.wav";
    public override void OnPointerClick(PointerEventData eventData)
    {
        if (!string.IsNullOrEmpty(soundPath)) AudioManager.Instance.PlaySound(soundPath);
        base.OnPointerClick(eventData);
    }
}
