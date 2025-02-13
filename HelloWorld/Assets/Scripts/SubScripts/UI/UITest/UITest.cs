using cfg;
using SuperScrollView;
using System.Collections.Generic;

namespace HotAssembly
{
    public class UITest : UIBase
    {
        private UITestComponent component = new UITestComponent();
        private UISubTest subUI = new UISubTest("UISubTest");
        private int updateId = -1;

        protected override void Init()
        {
            base.Init();
            component.Init(UIObj);
            component.bgRectTransform.anchorMin = UIManager.Instance.anchorMinFull;
            component.openSceneBtnUIButton.onClick.AddListener(OnOpenScene);
            component.openUIBtnUIButton.onClick.AddListener(OnOpenUI);
            component.openFunctionBtnUIButton.onClick.AddListener(OnOpenFunction);
            component.openUISettingUIButton.onClick.AddListener(OnOpenUISetting);
            component.closeBtnUIButton.onClick.AddListener(OnClickClose);
            component.openSubBtnUIButton.onClick.AddListener(OnOpenSub);
            component.openMsgBtnUIButton.onClick.AddListener(OnOpenMessage);
            component.openTipsBtnUIButton.onClick.AddListener(OnOpenTips);
            component.openSDKBtnUIButton.onClick.AddListener(SDKInit);
            component.loadSpriteUIButton.onClick.AddListener(LoadSprite);
            component.poolEnqueueUIButton.onClick.AddListener(LoadBulletFromPool);
            component.poolDequeueUIButton.onClick.AddListener(DelectBullet);
            component.openTestSceneUIButton.onClick.AddListener(OpenTestScene);
            component.closeTestSceneUIButton.onClick.AddListener(CloseTestScene);
            component.openBattleSceneUIButton.onClick.AddListener(OpenBattleScene);
            component.closeBattleSceneUIButton.onClick.AddListener(CloseBattleScene);
            component.openRvoSceneUIButton.onClick.AddListener(OpenRvoScene);
            component.closeRvoSceneUIButton.onClick.AddListener(CloseRvoScene);
            component.coroutineBtnUIButton.onClick.AddListener(TestCoroutine);
            component.addTriggerBtnUIButton.onClick.AddListener(AddTrigger);
            component.excuteTriggerBtnUIButton.onClick.AddListener(ExcuteTrigger);
            component.guideUIButton.onClick.AddListener(StartGuide);
            component.loopLoopListView2.InitListView(DataManager.Instance.TestData.testItemDatas.Count, OnGetItemByIndex);
        }
        public override void OnEnable(params object[] param)
        {
            base.OnEnable(param);
            updateId = Updater.Instance.StartUpdate(UpdateTrigger);
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
            Updater.Instance.StopUpdate(updateId);
            GameDebug.Log("UITest OnDisable");
        }
        public override void OnDestroy()
        {
            base.OnDestroy();
            subUI.Close(true);
            GameDebug.Log("UITest OnDestroy");
        }

        private void OnOpenScene()
        {
            component.totalObj.SetActive(false);
            component.sceneObj.SetActive(true);
        }
        private void OnOpenUI()
        {
            component.totalObj.SetActive(false);
            component.uIObj.SetActive(true);
        }
        private void OnOpenFunction()
        {
            component.totalObj.SetActive(false);
            component.functionObj.SetActive(true);
        }
        private void OnClickClose()
        {
            component.totalObj.SetActive(true);
            component.sceneObj.SetActive(false);
            component.uIObj.SetActive(false);
            component.functionObj.SetActive(false);
        }

        #region Scene
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
        private void OpenTestScene()
        {
            SceneManager.Instance.OpenScene(SceneType.TestScene);
        }
        private void CloseTestScene()
        {
            SceneManager.Instance.CloseScene(SceneType.TestScene);
        }
        private void OpenBattleScene()
        {
            BattleManager.Instance.Init(SceneType.BattleScene_Test);
        }
        private void CloseBattleScene()
        {
            BattleManager.Instance.Exit();
        }
        private void OpenRvoScene()
        {
            BattleManager.Instance.Init(SceneType.BattleScene_Rvo);
        }
        private void CloseRvoScene()
        {
            BattleManager.Instance.Exit();
        }
        #endregion

        #region UI
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
            UICommonTips.ShowTips(TimeUtils.ServerTime.ToString());
        }
        private void OnOpenSub()
        {
            if (subUI.Active) subUI.SetActive(component.subRootObj.transform, false);
            else subUI.Open(component.subRootObj.transform);
        }
        private void LoadSprite()
        {
            SetSprite(component.imageUIImage, ZResConst.ResUIAtlasTestPath, "TestIcon");
            SetSprite(component.imageUIImage, ZResConst.ResUIAtlasTestPath, "TestIcon2");
            GameDebug.Log("SetSprite");
        }
        #endregion

        #region Function
        private void SDKInit()
        {
#if UNITY_ANDROID
            SDK.Instance.InitSDK();
#endif
        }
        private void TestCoroutine()
        {
            var a = _TestCoroutine();
            CoroutineManager.Instance.StartCoroutine(a);
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
        public BuffManager buffManager = new BuffManager();
        private void UpdateTrigger(float t)
        {
            triggerManager.Update(t);
            buffManager.Update(t);
        }
        private void AddTrigger()
        {
            triggerManager.AddTrigger(1, action1: delegate { GameDebug.Log("T"); }, action2: delegate { GameDebug.Log("F"); });
            triggerManager.AddTrigger(3);
        }
        private void ExcuteTrigger()
        {
            triggerManager.ExcuteTrigger(TriggerMode.Attack);
            triggerManager.ExcuteTrigger(TriggerMode.AddBuff, buffManager);
        }
        private void StartGuide()
        {
            DataManager.Instance.GuideData.StartGuide(1);
        }
        #endregion

        private void OnOpenUISetting()
        {
            UIManager.Instance.OpenUI(UIType.UISetting);
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