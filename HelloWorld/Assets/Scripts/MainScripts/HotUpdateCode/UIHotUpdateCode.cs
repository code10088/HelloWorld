using System;
using UnityEngine;

public class UIHotUpdateCode : MonoBehaviour
{
    public static UIHotUpdateCode Instance;
    public UIHotUpdateCodeComponent component = new UIHotUpdateCodeComponent();
    private Action retry;

    private void Awake()
    {
        Instance = this;
        component.Init(gameObject);
        component.sureButton.onClick.AddListener(OnClickRetry);
        component.cancelButton.onClick.AddListener(OnClickQuit);
    }
    public void SetText(string str)
    {
        component.tipsTextMeshProUGUI.text = str;
    }
    public void SetSlider(float progress)
    {
        progress = float.IsNaN(progress) ? 0 : progress;
        component.sliderSlider.value = progress;
    }
    public void OpenCommonBox(string title, string content, Action retry)
    {
        this.retry = retry;
        component.titleTextMeshProUGUI.text = title;
        component.contentTextMeshProUGUI.text = content;
        component.commonBoxObj.SetActive(true);
    }
    private void OnClickRetry()
    {
        component.commonBoxObj.SetActive(false);
        retry?.Invoke();
    }
    private void OnClickQuit()
    {
        Application.Quit();
    }
    public void Destroy()
    {
        Destroy(gameObject);
    }
}