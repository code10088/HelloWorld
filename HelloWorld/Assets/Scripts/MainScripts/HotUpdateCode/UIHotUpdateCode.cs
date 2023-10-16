using System;
using UnityEngine;

namespace MainAssembly
{
    public class UIHotUpdateCode : MonoBehaviour
    {
        public static UIHotUpdateCode Instance;
        public UIHotUpdateCodeComponent component = new UIHotUpdateCodeComponent();
        private int loadId = -1;
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
        public void OpenMessageBox(string title, string content, Action retry)
        {
            this.retry = retry;
            component.titleTextMeshProUGUI.text = title;
            component.contentTextMeshProUGUI.text = content;
            component.messageBoxObj.SetActive(true);
        }
        private void OnClickRetry()
        {
            component.messageBoxObj.SetActive(false);
            retry?.Invoke();
        }
        private void OnClickQuit()
        {
            Application.Quit();
        }
        public void Finish()
        {
            AssetManager.Instance.Unload(loadId);
        }
        public void Destroy()
        {
            Destroy(gameObject);
        }
    }
}