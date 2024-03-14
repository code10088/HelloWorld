using cfg;
using SuperScrollView;
using System.Collections.Generic;
using UnityEngine;

namespace HotAssembly
{
    public class UITest : UIBase
    {
        private UITestComponent component = new UITestComponent();
        private UISubTest subUI = new UISubTest("UISubTest");

        protected override void Init()
        {
            base.Init();
            component.Init(UIObj);
            component.bgRectTransform.anchorMin = UIManager.anchorMinFull;
            component.closeBtnUIButton.onClick.AddListener(OnClose);
            component.openSubBtnUIButton.onClick.AddListener(OnOpenSub);
            component.openMsgBtnUIButton.onClick.AddListener(OnOpenMessage);
            component.openTipsBtnUIButton.onClick.AddListener(OnOpenTips);
            component.openSDKBtnUIButton.onClick.AddListener(SDKInit);
            component.loadSpriteUIButton.onClick.AddListener(LoadSprite);
            component.poolEnqueueUIButton.onClick.AddListener(LoadBulletFromPool);
            component.poolDequeueUIButton.onClick.AddListener(DelectBullet);
            component.openSceneUIButton.onClick.AddListener(OpenScene);
            component.closeSceneUIButton.onClick.AddListener(CloseScene);
            component.coroutineBtnUIButton.onClick.AddListener(TestCoroutine);
            component.addTriggerBtnUIButton.onClick.AddListener(AddTrigger);
            component.excuteTriggerBtnUIButton.onClick.AddListener(ExcuteTrigger);
            component.loopLoopListView2.InitListView(DataManager.Instance.TestData.testItemDatas.Count, OnGetItemByIndex);
        }
        public override void OnEnable(params object[] param)
        {
            base.OnEnable(param);
            GameDebug.Log("UITest OnEnable");
        }
        protected override void PlayInitAni()
        {
            base.PlayInitAni();
            GameDebug.Log("UITest OnPlayUIAnimation");
        }
        protected override void RefreshUILayer()
        {
            base.RefreshUILayer();
            GameDebug.Log("UITest RefreshUILayer");
        }
        public override void OnDisable()
        {
            base.OnDisable();
            subUI.Close();
            GameDebug.Log("UITest OnDisable");
        }
        public override void OnDestroy()
        {
            base.OnDestroy();
            subUI.Close(true);
            GameDebug.Log("UITest OnDestroy");
        }

        private void OnOpenMessage()
        {
            UICommonBoxParam param = new UICommonBoxParam();
            param.type = UICommonBoxType.SureAndCancel;
            param.title = "Tips";
            param.content = "Content";
            param.sure = a => OnOpenMessage();
            UICommonBox.OpenCommonBox(param);
        }
        private void OnOpenTips()
        {
            UICommonTips.ShowTips(Time.realtimeSinceStartup.ToString());
        }
        private void OnOpenSub()
        {
            if (subUI.Active) subUI.SetActive(component.subRootObj.transform, false);
            else subUI.Open(component.subRootObj.transform);
        }
        private void SDKInit()
        {
            SDK.Instance.InitSDK();
        }
        private void LoadSprite()
        {
            SetSprite(component.imageImage, ZResConst.ResUIAtlasTestPath, "TestIcon");
            SetSprite(component.imageImage, ZResConst.ResUIAtlasTestPath, "TestIcon2");
            SetSprite(component.imageImage, ZResConst.ResUIAtlasTestPath, "TestIcon3");
        }
        private void LoadBulletFromPool()
        {
            TestScene ts = SceneManager.Instance.GetScene(SceneType.TestScene) as TestScene;
            ts.LoadBulletFromPool();
        }
        private void DelectBullet()
        {
            TestScene ts = SceneManager.Instance.GetScene(SceneType.TestScene) as TestScene;
            ts.DelectBullet();
        }
        private void OpenScene()
        {
            SceneManager.Instance.OpenScene(SceneType.TestScene);
        }
        private void CloseScene()
        {
            SceneManager.Instance.CloseScene(SceneType.TestScene);
        }
        private void TestCoroutine()
        {
            var a = _TestCoroutine();
            CoroutineManager.Instance.Start(a);
            GameDebug.Log(1);
        }
        private IEnumerator<Coroutine> _TestCoroutine()
        {
            GameDebug.Log(0);
            yield return new WaitForFrame(1);
            GameDebug.Log(2);
            yield return new WaitForSeconds(1);
            GameDebug.Log(3);
        }
        public TriggerManager triggerManager = new TriggerManager();
        private void AddTrigger()
        {
            triggerManager.AddTrigger(1, action1: delegate { GameDebug.Log("T"); }, action2: delegate { GameDebug.Log("F"); });
        }
        private void ExcuteTrigger()
        {
            triggerManager.ExcuteTrigger(TriggerMode.Attack);
        }

        LoopListViewItem2 OnGetItemByIndex(LoopListView2 listView, int index)
        {
            if (index < 0 || index >= DataManager.Instance.TestData.testItemDatas.Count)
            {
                return null;
            }
            LoopListViewItem2 item = listView.NewListViewItem("Item");
            if (item.IsInitHandlerCalled == false)
            {
                item.IsInitHandlerCalled = true;
                item.ItemData = new UITestItem();
                item.ItemData.Init(item.gameObject);
            }
            item.ItemData.SetData(index);
            return item;
        }
    }
    public partial class UITestItem
    {
        public void SetData(int index)
        {
            var data = DataManager.Instance.TestData.testItemDatas[index];
            itemTextTextMeshProUGUI.text = data.name;
        }
    }
}