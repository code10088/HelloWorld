using UnityEngine;
public partial class UITestComponent
{
    public GameObject obj;
    public UnityEngine.RectTransform bgRectTransform = null;
    public UIButton closeBtnUIButton = null;
    public GameObject totalObj = null;
    public UIButton openSceneBtnUIButton = null;
    public UIButton openUIBtnUIButton = null;
    public UIButton openFunctionBtnUIButton = null;
    public UIButton openUISettingUIButton = null;
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
    public GameObject subRootObj = null;
    public SuperScrollView.LoopListView2 loopLoopListView2 = null;
    public SuperScrollView.LoopListViewItem2 itemLoopListViewItem2 = null;
    public UIButton openSubBtnUIButton = null;
    public UIButton openMsgBtnUIButton = null;
    public UIButton openTipsBtnUIButton = null;
    public UIButton loadSpriteUIButton = null;
    public UIImage imageUIImage = null;
    public GameObject functionObj = null;
    public UIButton openSDKBtnUIButton = null;
    public UIButton coroutineBtnUIButton = null;
    public UIButton addTriggerBtnUIButton = null;
    public UIButton excuteTriggerBtnUIButton = null;
    public UIButton guideUIButton = null;
    public void Init(GameObject obj)
    {
        this.obj = obj;
        ExportComponent[] allData = obj.GetComponentsInChildren<ExportComponent>(true);
        bgRectTransform = allData[0].exportComponent[0] as UnityEngine.RectTransform;
        closeBtnUIButton = allData[1].exportComponent[0] as UIButton;
        totalObj = allData[2].gameObject;
        openSceneBtnUIButton = allData[3].exportComponent[0] as UIButton;
        openUIBtnUIButton = allData[4].exportComponent[0] as UIButton;
        openFunctionBtnUIButton = allData[5].exportComponent[0] as UIButton;
        openUISettingUIButton = allData[6].exportComponent[0] as UIButton;
        sceneObj = allData[7].gameObject;
        openTestSceneUIButton = allData[8].exportComponent[0] as UIButton;
        closeTestSceneUIButton = allData[9].exportComponent[0] as UIButton;
        poolEnqueueUIButton = allData[10].exportComponent[0] as UIButton;
        poolDequeueUIButton = allData[11].exportComponent[0] as UIButton;
        openBattleSceneUIButton = allData[12].exportComponent[0] as UIButton;
        closeBattleSceneUIButton = allData[13].exportComponent[0] as UIButton;
        openRvoSceneUIButton = allData[14].exportComponent[0] as UIButton;
        closeRvoSceneUIButton = allData[15].exportComponent[0] as UIButton;
        openInfiniteTerrainSceneUIButton = allData[16].exportComponent[0] as UIButton;
        closeInfiniteTerrainSceneUIButton = allData[17].exportComponent[0] as UIButton;
        uIObj = allData[18].gameObject;
        subRootObj = allData[19].gameObject;
        loopLoopListView2 = allData[20].exportComponent[0] as SuperScrollView.LoopListView2;
        itemLoopListViewItem2 = allData[21].exportComponent[0] as SuperScrollView.LoopListViewItem2;
        openSubBtnUIButton = allData[23].exportComponent[0] as UIButton;
        openMsgBtnUIButton = allData[24].exportComponent[0] as UIButton;
        openTipsBtnUIButton = allData[25].exportComponent[0] as UIButton;
        loadSpriteUIButton = allData[26].exportComponent[0] as UIButton;
        imageUIImage = allData[27].exportComponent[0] as UIImage;
        functionObj = allData[28].gameObject;
        openSDKBtnUIButton = allData[29].exportComponent[0] as UIButton;
        coroutineBtnUIButton = allData[30].exportComponent[0] as UIButton;
        addTriggerBtnUIButton = allData[31].exportComponent[0] as UIButton;
        excuteTriggerBtnUIButton = allData[32].exportComponent[0] as UIButton;
        guideUIButton = allData[33].exportComponent[0] as UIButton;
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
