using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace HotAssembly
{
    public class UITest : UIBase
    {
        private UITestComponent component = new UITestComponent();
        private CustomLoopScroll<TestItem> clss = new CustomLoopScroll<TestItem>();

        protected override void InitComponent()
        {
            component.Init(UIObj);
            component.buttonButton.onClick.AddListener(OnClickClose);
        }
        public override async UniTask OnEnable(params object[] param)
        {
            await base.OnEnable(param);
            GameDebug.Log("UITest OnEnable");
            InitLoopScrollRect();
        }
        protected override void PlayInitAni()
        {
            base.PlayInitAni();
            GameDebug.Log("UITest OnPlayUIAnimation");
        }
        public override void OnDisable()
        {
            base.OnDisable();
            GameDebug.Log("UITest OnDisable");
        }
        public override void OnDestroy()
        {
            base.OnDestroy();
            GameDebug.Log("UITest OnDestroy");
        }

        private void OnClickClose()
        {
            SDKManager.Instance.InitSDK();
            GameDebug.Log("UITest Click");
            UIMessageBoxParam param = new UIMessageBoxParam();
            param.type = UIMessageBoxType.SureAndCancel;
            param.title = "Tips";
            param.content = "Content";
            param.sure = a => OnClickClose();
            UIManager.Instance.OpenMessageBox(param);
        }

        private void InitLoopScrollRect()
        {
            clss.Init(component.loopLoopVerticalScrollRect, component.itemObj, DataManager.Instance.TestData.testItemDatas.Count);
        }
        private class TestItem : CustomLoopItem
        {
            public TestData.TestItemData data;
            public UITestItem component = new UITestItem();
            public override void Init(GameObject _obj)
            {
                base.Init(_obj);
                component.Init(obj);
            }
            public override void SetData(int idx)
            {
                data = DataManager.Instance.TestData.testItemDatas[idx];
                component.itemTextTextMeshProUGUI.text = data.name;
            }
        }
    }
}