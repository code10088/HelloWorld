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
    private UITestComponent component = new UITestComponent();
    private UISubTest subUI = new UISubTest("UITest/UISubTest");
    private int updateId = -1;

    protected override void Init()
    {
        base.Init();
        component.Init(UIObj);
        component.bgRectTransform.anchorMin = UIManager.Instance.anchorMinFull;
        component.closeBtnUIButton.onClick.AddListener(OnClickClose);
        component.openSceneBtnUIButton.onClick.AddListener(OnOpenScene);
        component.openUIBtnUIButton.onClick.AddListener(OnOpenUI);
        component.openFunctionBtnUIButton.onClick.AddListener(OnOpenFunction);
        component.openUISettingUIButton.onClick.AddListener(OnOpenUISetting);
        component.openMiniGameBtnUIButton.onClick.AddListener(OnOpenMiniGame);

        //Scene
        component.openTestSceneUIButton.onClick.AddListener(OpenTestScene);
        component.closeTestSceneUIButton.onClick.AddListener(CloseTestScene);
        component.poolEnqueueUIButton.onClick.AddListener(LoadBulletFromPool);
        component.poolDequeueUIButton.onClick.AddListener(DelectBullet);
        component.openBattleSceneUIButton.onClick.AddListener(OpenBattleScene);
        component.closeBattleSceneUIButton.onClick.AddListener(CloseBattleScene);

        //UI
        component.openSubBtnUIButton.onClick.AddListener(OnOpenSub);
        component.openMsgBtnUIButton.onClick.AddListener(OnOpenMessage);
        component.openTipsBtnUIButton.onClick.AddListener(OnOpenTips);
        component.loadSpriteUIButton.onClick.AddListener(LoadSprite);
        component.uIProcessBtnUIButton.onClick.AddListener(OpenUIProcess);
        component.commonItemBtnUIButton.onClick.AddListener(ShowCommonItem);

        //Function
        component.openSDKBtnUIButton.onClick.AddListener(SDKInit);
        component.coroutineBtnUIButton.onClick.AddListener(TestCoroutine);
        component.addTriggerBtnUIButton.onClick.AddListener(AddTrigger);
        component.excuteTriggerBtnUIButton.onClick.AddListener(ExcuteTrigger);
        component.guideUIButton.onClick.AddListener(StartGuide);
        component.serializeUIButton.onClick.AddListener(MemoryPackSerialize);
        component.deserializeUIButton.onClick.AddListener(MemoryPackDeserialize);

        //小游戏
        component.createAdBtnUIButton.onClick.AddListener(CreateAd);
        component.showAdBtnUIButton.onClick.AddListener(ShowAd);
        component.showAdVideoBtnUIButton.onClick.AddListener(ShowAdVideo);
        component.uploadRankDataBtnUIButton.onClick.AddListener(UploadRankData);
        component.openRankBtnUIButton.onClick.AddListener(OnOpenRank);
        component.openMenuBtnUIButton.onClick.AddListener(OnOpenMenu);

        component.loopLoopListView2.InitListView(DataManager.Instance.TestData.testItemDatas.Count, OnGetItemByIndex);
    }
    public override void OnEnable(params object[] param)
    {
        base.OnEnable(param);
        EventManager.Instance.RegisterEvent(EventType.CloseUI, NextUIProcess);

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
        EventManager.Instance.UnRegisterEvent(EventType.CloseUI, NextUIProcess);

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
    private void OnOpenUISetting()
    {
        UIManager.Instance.OpenUI(UIType.UISetting);
    }
    private void OnOpenMiniGame()
    {
        component.totalObj.SetActive(false);
        component.miniGameObj.SetActive(true);
    }
    private void OnClickClose()
    {
        component.totalObj.SetActive(true);
        component.sceneObj.SetActive(false);
        component.uIObj.SetActive(false);
        component.functionObj.SetActive(false);
        component.miniGameObj.SetActive(false);
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
        if (subUI.Active) subUI.Close();
        else subUI.Open(component.subRootObj.transform);
    }
    private void LoadSprite()
    {
        SetSprite(component.imageUIImage, ZResConst.ResUIAtlasTestPath, "TestIcon");
        SetSprite(component.imageUIImage, ZResConst.ResUIAtlasTestPath, "TestIcon2");
        GameDebug.Log("SetSprite");
    }
    private ProcessControl<UIProcessItem> UIProcess = new ProcessControl<UIProcessItem>();
    private void OpenUIProcess()
    {
        UIProcess = new ProcessControl<UIProcessItem>();
        UIProcess.Add((int)UIType.UISetting, single: false);
        UIProcess.Add((int)UIType.UISetting, single: false);
        UIProcess.Start();
    }
    private void NextUIProcess(object o)
    {
        if ((int)((object[])o)[0] == UIProcess.CurId) UIProcess.Next();
    }
    private void ShowCommonItem()
    {
        var item = CommonItem.Instance.Get(1);
        item.SetData(component.itemRootObj.transform, 10);
        CommonItem.Instance.Recycle(item);
        item = CommonItem.Instance.Get(1);
        item.SetData(component.itemRootObj.transform, 10);
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
        component.rankObj.SetActive(true);
        var x = Mathf.FloorToInt(-component.rankRectTransform.offsetMax.x);
        var y = Mathf.FloorToInt(150 - component.rankRectTransform.offsetMax.y);
        var offset = Screen.width / component.uITestCanvasScaler.referenceResolution.x;
        var width = Mathf.FloorToInt(component.rankRectTransform.rect.width * offset);
        var height = Mathf.FloorToInt(component.rankRectTransform.rect.height * offset);
        WX.ShowOpenData(component.rankRawImage.texture, x, y, width, height);
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
public partial class UITestItem
{
    public void SetData(int index)
    {
        var data = DataManager.Instance.TestData.testItemDatas[index];
        itemTextTextMeshProUGUI.text = data.name;
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