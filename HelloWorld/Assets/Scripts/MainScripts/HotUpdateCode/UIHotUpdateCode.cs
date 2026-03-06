using System;
using UnityEngine;

public class UIHotUpdateCode : MonoBehaviour
{
    public static UIHotUpdateCode Instance;
    public UIHotUpdateCodeComponent comp;
    private Action retry;

    private void Awake()
    {
        Instance = this;
        comp = GetComponent<UIHotUpdateCodeComponent>();
        comp.sureButton.onClick.AddListener(OnClickRetry);
        comp.cancelButton.onClick.AddListener(OnClickQuit);
    }
    public void SetText(string str)
    {
        comp.tipsTextMeshProUGUI.text = str;
    }
    public void SetSlider(float progress)
    {
        progress = float.IsNaN(progress) ? 0 : progress;
        comp.sliderSlider.value = progress;
    }
    public void OpenCommonBox(string title, string content, Action retry)
    {
        this.retry = retry;
        comp.titleTextMeshProUGUI.text = title;
        comp.contentTextMeshProUGUI.text = content;
        comp.commonBoxGameObject.SetActive(true);
    }
    private void OnClickRetry()
    {
        comp.commonBoxGameObject.SetActive(false);
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