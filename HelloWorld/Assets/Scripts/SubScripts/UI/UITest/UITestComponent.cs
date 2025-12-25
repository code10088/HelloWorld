using UnityEngine;
public partial class UITestComponent
{
    public GameObject obj;
    public UnityEngine.UI.CanvasScaler uITestCanvasScaler = null;
    public UnityEngine.RectTransform bgRectTransform = null;
    public UIButton closeBtnUIButton = null;
    public GameObject totalObj = null;
    public UIButton openSceneBtnUIButton = null;
    public UIButton openUIBtnUIButton = null;
    public UIButton openFunctionBtnUIButton = null;
    public UIButton openUISettingUIButton = null;
    public UIButton openMiniGameBtnUIButton = null;
    public GameObject sceneObj = null;
    public UIButton openTestSceneUIButton = null;
    public UIButton closeTestSceneUIButton = null;
    public UIButton poolEnqueueUIButton = null;
    public UIButton poolDequeueUIButton = null;
    public UIButton openBattleSceneUIButton = null;
    public UIButton closeBattleSceneUIButton = null;
    public GameObject uIObj = null;
    public UIButton openSubBtnUIButton = null;
    public UIButton openMsgBtnUIButton = null;
    public UIButton openTipsBtnUIButton = null;
    public UIButton loadSpriteUIButton = null;
    public UIButton uIProcessBtnUIButton = null;
    public UIButton commonItemBtnUIButton = null;
    public UIButton languageBtnUIButton = null;
    public GameObject subRootObj = null;
    public SuperScrollView.LoopListView2 loopLoopListView2 = null;
    public SuperScrollView.LoopListViewItem2 itemLoopListViewItem2 = null;
    public UnityEngine.UI.Image imageImage = null;
    public GameObject itemRootObj = null;
    public UIText languageUIText = null;
    public GameObject functionObj = null;
    public UIButton openSDKBtnUIButton = null;
    public UIButton coroutineBtnUIButton = null;
    public UIButton addTriggerBtnUIButton = null;
    public UIButton excuteTriggerBtnUIButton = null;
    public UIButton guideUIButton = null;
    public UIButton serializeUIButton = null;
    public UIButton deserializeUIButton = null;
    public GameObject miniGameObj = null;
    public UIButton createAdBtnUIButton = null;
    public UIButton showAdBtnUIButton = null;
    public UIButton showAdVideoBtnUIButton = null;
    public UIButton uploadRankDataBtnUIButton = null;
    public UIButton openRankBtnUIButton = null;
    public UIButton openMenuBtnUIButton = null;
    public GameObject rankObj = null;
    public UnityEngine.UI.RawImage rankRawImage = null;
    public UnityEngine.RectTransform rankRectTransform = null;
    public void Init(GameObject obj)
    {
        this.obj = obj;
        ExportComponent[] allData = obj.GetComponentsInChildren<ExportComponent>(true);
        uITestCanvasScaler = allData[0].exportComponent[0] as UnityEngine.UI.CanvasScaler;
        bgRectTransform = allData[1].exportComponent[0] as UnityEngine.RectTransform;
        closeBtnUIButton = allData[2].exportComponent[0] as UIButton;
        totalObj = allData[3].gameObject;
        openSceneBtnUIButton = allData[4].exportComponent[0] as UIButton;
        openUIBtnUIButton = allData[5].exportComponent[0] as UIButton;
        openFunctionBtnUIButton = allData[6].exportComponent[0] as UIButton;
        openUISettingUIButton = allData[7].exportComponent[0] as UIButton;
        openMiniGameBtnUIButton = allData[8].exportComponent[0] as UIButton;
        sceneObj = allData[9].gameObject;
        openTestSceneUIButton = allData[10].exportComponent[0] as UIButton;
        closeTestSceneUIButton = allData[11].exportComponent[0] as UIButton;
        poolEnqueueUIButton = allData[12].exportComponent[0] as UIButton;
        poolDequeueUIButton = allData[13].exportComponent[0] as UIButton;
        openBattleSceneUIButton = allData[14].exportComponent[0] as UIButton;
        closeBattleSceneUIButton = allData[15].exportComponent[0] as UIButton;
        uIObj = allData[16].gameObject;
        openSubBtnUIButton = allData[17].exportComponent[0] as UIButton;
        openMsgBtnUIButton = allData[18].exportComponent[0] as UIButton;
        openTipsBtnUIButton = allData[19].exportComponent[0] as UIButton;
        loadSpriteUIButton = allData[20].exportComponent[0] as UIButton;
        uIProcessBtnUIButton = allData[21].exportComponent[0] as UIButton;
        commonItemBtnUIButton = allData[22].exportComponent[0] as UIButton;
        languageBtnUIButton = allData[23].exportComponent[0] as UIButton;
        subRootObj = allData[24].gameObject;
        loopLoopListView2 = allData[25].exportComponent[0] as SuperScrollView.LoopListView2;
        itemLoopListViewItem2 = allData[26].exportComponent[0] as SuperScrollView.LoopListViewItem2;
        imageImage = allData[28].exportComponent[0] as UnityEngine.UI.Image;
        itemRootObj = allData[29].gameObject;
        languageUIText = allData[30].exportComponent[0] as UIText;
        functionObj = allData[31].gameObject;
        openSDKBtnUIButton = allData[32].exportComponent[0] as UIButton;
        coroutineBtnUIButton = allData[33].exportComponent[0] as UIButton;
        addTriggerBtnUIButton = allData[34].exportComponent[0] as UIButton;
        excuteTriggerBtnUIButton = allData[35].exportComponent[0] as UIButton;
        guideUIButton = allData[36].exportComponent[0] as UIButton;
        serializeUIButton = allData[37].exportComponent[0] as UIButton;
        deserializeUIButton = allData[38].exportComponent[0] as UIButton;
        miniGameObj = allData[39].gameObject;
        createAdBtnUIButton = allData[40].exportComponent[0] as UIButton;
        showAdBtnUIButton = allData[41].exportComponent[0] as UIButton;
        showAdVideoBtnUIButton = allData[42].exportComponent[0] as UIButton;
        uploadRankDataBtnUIButton = allData[43].exportComponent[0] as UIButton;
        openRankBtnUIButton = allData[44].exportComponent[0] as UIButton;
        openMenuBtnUIButton = allData[45].exportComponent[0] as UIButton;
        rankObj = allData[46].gameObject;
        rankRawImage = allData[46].exportComponent[1] as UnityEngine.UI.RawImage;
        rankRectTransform = allData[46].exportComponent[2] as UnityEngine.RectTransform;
    }
}
public partial class UITestItem : LoopItemData
{
    public GameObject obj;
    public SuperScrollView.LoopListViewItem2 itemLoopListViewItem2 = null;
    public TMPro.TextMeshProUGUI itemTextTextMeshProUGUI = null;
    public void Init(GameObject obj)
    {
        this.obj = obj;
        ExportComponent[] allData = obj.GetComponentsInChildren<ExportComponent>(true);
        itemLoopListViewItem2 = allData[0].exportComponent[0] as SuperScrollView.LoopListViewItem2;
        itemTextTextMeshProUGUI = allData[1].exportComponent[0] as TMPro.TextMeshProUGUI;
    }
}
