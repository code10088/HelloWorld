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
    public UIButton openRvoSceneUIButton = null;
    public UIButton closeRvoSceneUIButton = null;
    public UIButton openInfiniteTerrainSceneUIButton = null;
    public UIButton closeInfiniteTerrainSceneUIButton = null;
    public GameObject uIObj = null;
    public UIButton openSubBtnUIButton = null;
    public UIButton openMsgBtnUIButton = null;
    public UIButton openTipsBtnUIButton = null;
    public UIButton loadSpriteUIButton = null;
    public UIImage imageUIImage = null;
    public GameObject subRootObj = null;
    public SuperScrollView.LoopListView2 loopLoopListView2 = null;
    public SuperScrollView.LoopListViewItem2 itemLoopListViewItem2 = null;
    public GameObject functionObj = null;
    public UIButton openSDKBtnUIButton = null;
    public UIButton coroutineBtnUIButton = null;
    public UIButton addTriggerBtnUIButton = null;
    public UIButton excuteTriggerBtnUIButton = null;
    public UIButton guideUIButton = null;
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
        openRvoSceneUIButton = allData[16].exportComponent[0] as UIButton;
        closeRvoSceneUIButton = allData[17].exportComponent[0] as UIButton;
        openInfiniteTerrainSceneUIButton = allData[18].exportComponent[0] as UIButton;
        closeInfiniteTerrainSceneUIButton = allData[19].exportComponent[0] as UIButton;
        uIObj = allData[20].gameObject;
        openSubBtnUIButton = allData[21].exportComponent[0] as UIButton;
        openMsgBtnUIButton = allData[22].exportComponent[0] as UIButton;
        openTipsBtnUIButton = allData[23].exportComponent[0] as UIButton;
        loadSpriteUIButton = allData[24].exportComponent[0] as UIButton;
        imageUIImage = allData[25].exportComponent[0] as UIImage;
        subRootObj = allData[26].gameObject;
        loopLoopListView2 = allData[27].exportComponent[0] as SuperScrollView.LoopListView2;
        itemLoopListViewItem2 = allData[28].exportComponent[0] as SuperScrollView.LoopListViewItem2;
        functionObj = allData[30].gameObject;
        openSDKBtnUIButton = allData[31].exportComponent[0] as UIButton;
        coroutineBtnUIButton = allData[32].exportComponent[0] as UIButton;
        addTriggerBtnUIButton = allData[33].exportComponent[0] as UIButton;
        excuteTriggerBtnUIButton = allData[34].exportComponent[0] as UIButton;
        guideUIButton = allData[35].exportComponent[0] as UIButton;
        miniGameObj = allData[36].gameObject;
        createAdBtnUIButton = allData[37].exportComponent[0] as UIButton;
        showAdBtnUIButton = allData[38].exportComponent[0] as UIButton;
        showAdVideoBtnUIButton = allData[39].exportComponent[0] as UIButton;
        uploadRankDataBtnUIButton = allData[40].exportComponent[0] as UIButton;
        openRankBtnUIButton = allData[41].exportComponent[0] as UIButton;
        openMenuBtnUIButton = allData[42].exportComponent[0] as UIButton;
        rankObj = allData[43].gameObject;
        rankRawImage = allData[43].exportComponent[1] as UnityEngine.UI.RawImage;
        rankRectTransform = allData[43].exportComponent[2] as UnityEngine.RectTransform;
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
