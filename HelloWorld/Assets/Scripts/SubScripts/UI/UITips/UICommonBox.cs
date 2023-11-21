using cfg;
using System;
using System.Collections.Generic;

namespace HotAssembly
{
    public class UICommonBox : UIBase
    {
        private UICommonBoxComponent component = new UICommonBoxComponent();
        private static Queue<UICommonBoxParam> commonBoxQueue = new Queue<UICommonBoxParam>();
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
        public override void OnEnable(params object[] param)
        {
            base.OnEnable(param);
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
        protected override void OnClose()
        {
            base.OnClose();
            Check();
        }

        private void OnClickSure1()
        {
            commonBoxParam.sure?.Invoke(commonBoxParam.sureParam);
            OnClose();
        }
        private void OnClickSure2()
        {
            commonBoxParam.sure?.Invoke(commonBoxParam.sureParam);
            OnClose();
        }
        private void OnClickCancel()
        {
            commonBoxParam.cancel?.Invoke(commonBoxParam.cancelParam);
            OnClose();
        }

        public static void OpenCommonBox(UICommonBoxParam param)
        {
            commonBoxQueue.Enqueue(param);
            Check();
        }
        private static void Check()
        {
            if (commonBoxQueue.Count == 0) return;
            if (UIManager.Instance.HasOpen(UIType.UICommonBox)) return;
            var param = commonBoxQueue.Dequeue();
            UIManager.Instance.OpenUI(UIType.UICommonBox, param: param);
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