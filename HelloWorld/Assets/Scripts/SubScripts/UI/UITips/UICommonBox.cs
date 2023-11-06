using Cysharp.Threading.Tasks;
using System;

namespace HotAssembly
{
    public class UICommonBox : UIBase
    {
        private UICommonBoxComponent component = new UICommonBoxComponent();
        private UICommonBoxParam commonBoxParam;

        protected override void Init()
        {
            base.Init();
            component.Init(UIObj);
            component.bgRectTransform.anchorMin = UIManager.anchorMinFull;
            component.sure1UIButton.onClick.AddListener(OnClickSure1);
            component.sure2UIButton.onClick.AddListener(OnClickSure2);
            component.cancelUIButton.onClick.AddListener(OnClickCancel);
        }
        public override async UniTask OnEnable(params object[] param)
        {
            await base.OnEnable(param);
            commonBoxParam = param[0] as UICommonBoxParam;
            component.titleTextMeshProUGUI.text = commonBoxParam.title;
            component.contentTextMeshProUGUI.text = commonBoxParam.content;
            if (commonBoxParam.type == UICommonBoxType.Sure)
            {
                component.sure1UIButton.gameObject.SetActive(true);
                component.sure2UIButton.gameObject.SetActive(false);
                component.cancelUIButton.gameObject.SetActive(false);
            }
            else if (commonBoxParam.type == UICommonBoxType.SureAndCancel)
            {
                component.sure1UIButton.gameObject.SetActive(false);
                component.sure2UIButton.gameObject.SetActive(true);
                component.cancelUIButton.gameObject.SetActive(true);
            }
        }
        private void OnClickSure1()
        {
            commonBoxParam.sure?.Invoke(commonBoxParam.sureParam);
            CheckCommonBox();
        }
        private void OnClickSure2()
        {
            commonBoxParam.sure?.Invoke(commonBoxParam.sureParam);
            CheckCommonBox();
        }
        private void OnClickCancel()
        {
            commonBoxParam.cancel?.Invoke(commonBoxParam.cancelParam);
            CheckCommonBox();
        }
        private void CheckCommonBox()
        {
            OnClose();
            UIManager.Instance.CheckCommonBox();
        }
    }
    public enum UICommonBoxType
    {
        Sure,
        SureAndCancel,
    }
    public class UICommonBoxParam
    {
        public UICommonBoxType type;
        public string title;
        public string content;
        public Action<object> sure;
        public object sureParam;
        public Action<object> cancel;
        public object cancelParam;
    }
}