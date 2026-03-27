using cfg;
using MemoryPack;
using Newtonsoft.Json;
using SuperScrollView;
using System.Collections.Generic;
using UnityEngine;

#if WEIXINMINIGAME
using WeChatWASM;
#elif DOUYINMINIGAME
using TTSDK;
using TTSDK.UNBridgeLib.LitJson;
#endif

public class UITest : UIBase
{
    private UITestComponent comp;
    private UISubTest subUI = new UISubTest("UITest/UISubTest");
    private int updateId = -1;

    protected override void Init()
    {
        base.Init();
        comp = component as UITestComponent;
        comp.bgRectTransform.anchorMin = UIManager.Instance.anchorMinFull;
        comp.closeBtnUIButton.onClick.AddListener(OnClickClose);
        comp.openSceneBtnUIButton.onClick.AddListener(OnOpenScene);
        comp.openUIBtnUIButton.onClick.AddListener(OnOpenUI);
        comp.openFunctionBtnUIButton.onClick.AddListener(OnOpenFunction);
        comp.openUISettingUIButton.onClick.AddListener(OnOpenUISetting);
        comp.openMiniGameBtnUIButton.onClick.AddListener(OnOpenMiniGame);

        //Scene
        comp.openTestSceneUIButton.onClick.AddListener(OpenTestScene);
        comp.closeTestSceneUIButton.onClick.AddListener(CloseTestScene);
        comp.poolEnqueueUIButton.onClick.AddListener(LoadBulletFromPool);
        comp.poolDequeueUIButton.onClick.AddListener(DelectBullet);
        comp.openBattleSceneUIButton.onClick.AddListener(OpenBattleScene);
        comp.closeBattleSceneUIButton.onClick.AddListener(CloseBattleScene);
        comp.cameraTRSUIButton.onClick.AddListener(SetCameraTRS);

        //UI
        comp.openSubBtnUIButton.onClick.AddListener(OnOpenSub);
        comp.openMsgBtnUIButton.onClick.AddListener(OnOpenMessage);
        comp.openTipsBtnUIButton.onClick.AddListener(OnOpenTips);
        comp.loadSpriteUIButton.onClick.AddListener(LoadSprite);
        comp.uIProcessBtnUIButton.onClick.AddListener(OpenUIProcess);
        comp.commonItemBtnUIButton.onClick.AddListener(ShowCommonItem);
        comp.languageBtnUIButton.onClick.AddListener(SetText);

        //Function
        comp.openSDKBtnUIButton.onClick.AddListener(SDKInit);
        comp.coroutineBtnUIButton.onClick.AddListener(TestCoroutine);
        comp.addTriggerBtnUIButton.onClick.AddListener(AddTrigger);
        comp.excuteTriggerBtnUIButton.onClick.AddListener(ExcuteTrigger);
        comp.guideUIButton.onClick.AddListener(StartGuide);
        comp.serializeUIButton.onClick.AddListener(MemoryPackSerialize);
        comp.deserializeUIButton.onClick.AddListener(MemoryPackDeserialize);

        //小游戏
        comp.createAdBtnUIButton.onClick.AddListener(CreateAd);
        comp.showAdBtnUIButton.onClick.AddListener(ShowAd);
        comp.showAdVideoBtnUIButton.onClick.AddListener(ShowAdVideo);
        comp.uploadRankDataBtnUIButton.onClick.AddListener(UploadRankData);
        comp.openRankBtnUIButton.onClick.AddListener(OnOpenRank);
        comp.openMenuBtnUIButton.onClick.AddListener(OnOpenMenu);

        comp.loopLoopListView2.InitListView(DataManager.Instance.TestData.testItemDatas.Count, OnGetItemByIndex);
    }
    public override void OnEnable(params object[] param)
    {
        base.OnEnable(param);
        EventManager.Instance.Register<UIType>(EventType.CloseUI, NextUIProcess);

        updateId = Driver.Instance.StartUpdate(UpdateTrigger);
        GameDebug.Log("UITest OnEnable");
    }
    protected override void PlayEnableAni()
    {
        base.PlayEnableAni();
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
        EventManager.Instance.Unregister<UIType>(EventType.CloseUI, NextUIProcess);

        subUI.Close();
        Driver.Instance.Remove(updateId);
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
        comp.totalGameObject.SetActive(false);
        comp.sceneGameObject.SetActive(true);
    }
    private void OnOpenUI()
    {
        comp.totalGameObject.SetActive(false);
        comp.uIGameObject.SetActive(true);
    }
    private void OnOpenFunction()
    {
        comp.totalGameObject.SetActive(false);
        comp.functionGameObject.SetActive(true);
    }
    private void OnOpenUISetting()
    {
        UIManager.Instance.OpenUI(UIType.UISetting);
    }
    private void OnOpenMiniGame()
    {
        comp.totalGameObject.SetActive(false);
        comp.miniGameGameObject.SetActive(true);
    }
    private void OnClickClose()
    {
        if (comp.totalGameObject.activeSelf)
        {
            OnClose();
        }
        else
        {
            comp.totalGameObject.SetActive(true);
            comp.sceneGameObject.SetActive(false);
            comp.uIGameObject.SetActive(false);
            comp.functionGameObject.SetActive(false);
            comp.miniGameGameObject.SetActive(false);
        }
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
    private void SetCameraTRS()
    {
        SceneManager.Instance.CameraController.SetTarget(1, Vector3.right, 50, Vector2.right);
    }
    #endregion

    #region UI
    LoopListViewItem2 OnGetItemByIndex(LoopListView2 listView, int index)
    {
        if (index < 0 || index >= DataManager.Instance.TestData.testItemDatas.Count)
        {
            return null;
        }
        LoopListViewItem2 item = listView.NewListViewItem<UITestItem>("Item");
        var test = item.ItemData as UITestItem;
        var data = DataManager.Instance.TestData.testItemDatas[index];
        test.SetData(data);
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
        if (subUI.Active) subUI.Close();
        else subUI.Open(comp.subRootGameObject.transform);
    }
    private void LoadSprite()
    {
        SetSprite(comp.imageImage, ZResConst.ResUIAtlasTestPath, "TestIcon");
        SetSprite(comp.imageImage, ZResConst.ResUIAtlasTestPath, "TestIcon2");
        GameDebug.Log("SetSprite");
    }
    private ProcessControl UIProcess = new ProcessControl();
    private void OpenUIProcess()
    {
        UIProcess = new ProcessControl();
        UIProcess.Add(new UIProcessItem(UIType.UISetting));
        UIProcess.Add(new UIProcessItem(UIType.UISetting));
        UIProcess.Start();
    }
    private void NextUIProcess(UIType type)
    {
        if (type == ((UIProcessItem)UIProcess.Cur).type) UIProcess.Next();
    }
    private void ShowCommonItem()
    {
        var item = CommonItemPool.Instance.Get();
        item.SetParent(comp.itemRootGameObject.transform);
        item.Refresh(1);
        item.SetActive(true);
        item.SetCount(10);
        CommonItemPool.Instance.Return(item);
        item = CommonItemPool.Instance.Get();
        item.SetParent(comp.itemRootGameObject.transform);
        item.Refresh(1);
        item.SetActive(true);
        item.SetCount(10);
    }
    private void SetText()
    {
        comp.languageUIText.SetText(10001, "World!!");
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
        Driver.Instance.StartCoroutine(a);
        GameDebug.Log(1);
    }
    private IEnumerator<ICoroutine> _TestCoroutine()
    {
        GameDebug.Log(0);
        yield return new WaitFrame(1);
        GameDebug.Log(2);
        yield return new WaitSeconds(1);
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
    private byte[] testBytes;
    private MemoryPackTest memoryPackTest = new MemoryPackTest();
    private void MemoryPackSerialize()
    {
        memoryPackTest.a = 123;
        memoryPackTest.b = "Hello, MemoryPack!";
        memoryPackTest.c = 1.0f;
        testBytes = MemoryPackSerializer.Serialize(memoryPackTest);
        GameDebug.Log(testBytes.Length);
    }
    private void MemoryPackDeserialize()
    {
        MemoryPackSerializer.Deserialize(testBytes, ref memoryPackTest);
        GameDebug.Log(memoryPackTest.c);
    }
    #endregion

    #region 小游戏
    private int adId = -1;
    /// <summary>
    /// banner广告初始化
    /// </summary>
    private void CreateAd()
    {
#if WEIXINMINIGAME
        AdConst.TempId1 = SDK.Instance.WXCreateRewardedVideoAd(AdConst.WXAdUnitId1);
        adId = SDK.Instance.WXCreateCustomAd(AdConst.WXAdUnitId2, 30, 0, -200, Screen.width);
#elif DOUYINMINIGAME
        AdConst.TempId1 = SDK.Instance.TTCreateRewardedVideoAd(AdConst.TTAdUnitId1);
        adId = SDK.Instance.TTCreateBannerAd(AdConst.TTAdUnitId2, 30, 0, -200, Screen.width);
#endif
    }
    /// <summary>
    /// banner广告
    /// </summary>
    private void ShowAd()
    {
        SDK.Instance.Show(adId, success =>
        {
            if (success)
            {
            }
            else
            {
#if WEIXINMINIGAME
                adId = SDK.Instance.WXCreateCustomAd(AdConst.WXAdUnitId2, 30, 0, -200, Screen.width);
#elif DOUYINMINIGAME
                adId = SDK.Instance.TTCreateBannerAd(AdConst.TTAdUnitId2, 30, 0, -200, Screen.width);
#endif
                ShowAd();
            }
        });
    }
    /// <summary>
    /// 视频广告
    /// </summary>
    private void ShowAdVideo()
    {
        SDK.Instance.Show(AdConst.TempId1, OnShowAdVideoCallback);
    }
    private void OnShowAdVideoCallback(bool isEnded)
    {
        GameDebug.Log(isEnded);
    }
    int record = 0;
    /// <summary>
    /// 上传排行榜数据
    /// </summary>
    private void UploadRankData()
    {
#if WEIXINMINIGAME
        var message = new OpenDataMessage();
        message.type = "setUserRecord";
        message.score = record++;
        string str = JsonConvert.SerializeObject(message);
        SDK.Instance.PostMessage(str);
#elif DOUYINMINIGAME
        void SetImRankData()
        {
            var message = new JsonData
            {
                ["dataType"] = 0,
                ["value"] = record++.ToString(),
                ["priority"] = 0,
                ["extra"] = "extra",
                ["zoneId"] = "default",
            };
            TT.SetImRankData(message);
        }
        if (SDK.Instance.LoginState) SetImRankData();
        else SDK.Instance.Login(a => { if (a) SetImRankData(); });
#endif
    }
    /// <summary>
    /// 打开排行榜
    /// </summary>
    private void OnOpenRank()
    {
#if WEIXINMINIGAME
        comp.rankGameObject.SetActive(true);
        var x = Mathf.FloorToInt(-comp.rankRectTransform.offsetMax.x);
        var y = Mathf.FloorToInt(150 - comp.rankRectTransform.offsetMax.y);
        var offset = Screen.width / comp.uITestCanvasScaler.referenceResolution.x;
        var width = Mathf.FloorToInt(comp.rankRectTransform.rect.width * offset);
        var height = Mathf.FloorToInt(comp.rankRectTransform.rect.height * offset);
        WX.ShowOpenData(comp.rankRawImage.texture, x, y, width, height);
        var message = new OpenDataMessage();
        message.type = "showFriendsRank";
        var str = JsonConvert.SerializeObject(message);
        SDK.Instance.PostMessage(str);
#elif DOUYINMINIGAME
        void GetImRankList()
        {
            var data = new JsonData
            {
                ["relationType"] = "default",
                ["dataType"] = 0,
                ["rankType"] = "week",
                ["rankTitle"] = "排行榜",
                ["zoneId"] = "default",
                ["suffix"] = "",
            };
            TT.GetImRankList(data);
        }
        if (SDK.Instance.LoginState) GetImRankList();
        else SDK.Instance.Login(a => { if (a) GetImRankList(); });
#endif
    }
    /// <summary>
    /// 跳转到小游戏菜单界面
    /// </summary>
    private void OnOpenMenu()
    {
#if DOUYINMINIGAME
        SDK.Instance.TTNavigateToScene();
#endif
    }
    #endregion

}
public class UITestItem : LoopItemData
{
    private UITestItemComponent comp;
    public void Init(GameObject obj)
    {
        comp = obj.GetComponent<UITestItemComponent>();
    }
    public void SetData(TestData.TestItemData data)
    {
        comp.itemTextTextMeshProUGUI.text = data.name;
    }
}
[MemoryPackable]
public partial class MemoryPackTest
{
    public int a;
    public string b;
    public float c;
}
#if WEIXINMINIGAME
[System.Serializable]
public class OpenDataMessage
{
    public string type;
    public int score;
}
#endif