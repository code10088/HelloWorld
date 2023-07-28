using Cysharp.Threading.Tasks;
using System;

namespace HotAssembly
{
    public class UIMessageBox : UIBase
    {
        private UIMessageBoxComponent component = new UIMessageBoxComponent();
        private UIMessageBoxParam messageBoxParam;

        protected override void Init()
        {
            base.Init();
            component.Init(UIObj);
            component.sure1Button.onClick.AddListener(OnClickSure1);
            component.sure2Button.onClick.AddListener(OnClickSure2);
            component.cancelButton.onClick.AddListener(OnClickCancel);
        }
        public override async UniTask OnEnable(params object[] param)
        {
            await base.OnEnable(param);
            messageBoxParam = param[0] as UIMessageBoxParam;
            component.titleTextMeshProUGUI.text = messageBoxParam.title;
            component.contentTextMeshProUGUI.text = messageBoxParam.content;
            if (messageBoxParam.type == UIMessageBoxType.Sure)
            {
                component.sure1Button.gameObject.SetActive(true);
                component.sure2Button.gameObject.SetActive(false);
                component.cancelButton.gameObject.SetActive(false);
            }
            else if (messageBoxParam.type == UIMessageBoxType.SureAndCancel)
            {
                component.sure1Button.gameObject.SetActive(false);
                component.sure2Button.gameObject.SetActive(true);
                component.cancelButton.gameObject.SetActive(true);
            }
        }
        private void OnClickSure1()
        {
            messageBoxParam.sure?.Invoke(messageBoxParam.sureParam);
            CheckMessageBox();
        }
        private void OnClickSure2()
        {
            messageBoxParam.sure?.Invoke(messageBoxParam.sureParam);
            CheckMessageBox();
        }
        private void OnClickCancel()
        {
            messageBoxParam.cancel?.Invoke(messageBoxParam.cancelParam);
            CheckMessageBox();
        }
        private void CheckMessageBox()
        {
            OnClose();
            UIManager.Instance.CheckMessageBox();
        }
    }
    public enum UIMessageBoxType
    {
        Sure,
        SureAndCancel,
    }
    public class UIMessageBoxParam
    {
        public UIMessageBoxType type;
        public string title;
        public string content;
        public Action<object> sure;
        public object sureParam;
        public Action<object> cancel;
        public object cancelParam;
    }
}