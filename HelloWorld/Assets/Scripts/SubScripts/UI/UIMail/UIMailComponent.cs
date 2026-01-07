using UnityEngine;
public partial class UIMailComponent
{
    public GameObject obj;
    public UnityEngine.RectTransform bgRectTransform = null;
    public SuperScrollView.LoopListView2 loopLoopListView2 = null;
    public SuperScrollView.LoopListViewItem2 mailItemLoopListViewItem2 = null;
    public UnityEngine.UI.Button mailItemButton = null;
    public UnityEngine.UI.Image mailItemImage = null;
    public UIButton readAllBtnUIButton = null;
    public TMPro.TextMeshProUGUI readAllTextTextMeshProUGUI = null;
    public UIButton deleteAllBtnUIButton = null;
    public GameObject contentRootObj = null;
    public TMPro.TextMeshProUGUI titleTextMeshProUGUI = null;
    public TMPro.TextMeshProUGUI contentTextMeshProUGUI = null;
    public UnityEngine.RectTransform rewardContentRectTransform = null;
    public GameObject getBtnObj = null;
    public UIButton getBtnUIButton = null;
    public GameObject deleteBtnObj = null;
    public UIButton deleteBtnUIButton = null;
    public UIButton closeBtnUIButton = null;
    public void Init(GameObject obj)
    {
        this.obj = obj;
        ExportComponent[] allData = obj.GetComponentsInChildren<ExportComponent>(true);
        bgRectTransform = allData[0].exportComponent[0] as UnityEngine.RectTransform;
        loopLoopListView2 = allData[1].exportComponent[0] as SuperScrollView.LoopListView2;
        mailItemLoopListViewItem2 = allData[2].exportComponent[0] as SuperScrollView.LoopListViewItem2;
        mailItemButton = allData[2].exportComponent[1] as UnityEngine.UI.Button;
        mailItemImage = allData[2].exportComponent[2] as UnityEngine.UI.Image;
        readAllBtnUIButton = allData[6].exportComponent[0] as UIButton;
        readAllTextTextMeshProUGUI = allData[7].exportComponent[0] as TMPro.TextMeshProUGUI;
        deleteAllBtnUIButton = allData[8].exportComponent[0] as UIButton;
        contentRootObj = allData[9].gameObject;
        titleTextMeshProUGUI = allData[10].exportComponent[0] as TMPro.TextMeshProUGUI;
        contentTextMeshProUGUI = allData[11].exportComponent[0] as TMPro.TextMeshProUGUI;
        rewardContentRectTransform = allData[12].exportComponent[0] as UnityEngine.RectTransform;
        getBtnObj = allData[13].gameObject;
        getBtnUIButton = allData[13].exportComponent[1] as UIButton;
        deleteBtnObj = allData[14].gameObject;
        deleteBtnUIButton = allData[14].exportComponent[1] as UIButton;
        closeBtnUIButton = allData[15].exportComponent[0] as UIButton;
    }
}
public partial class UIMailItemComponent
{
    public GameObject obj;
    public SuperScrollView.LoopListViewItem2 mailItemLoopListViewItem2 = null;
    public UnityEngine.UI.Button mailItemButton = null;
    public UnityEngine.UI.Image mailItemImage = null;
    public TMPro.TextMeshProUGUI titleTextMeshProUGUI = null;
    public TMPro.TextMeshProUGUI timeTextMeshProUGUI = null;
    public GameObject redPointObj = null;
    public void Init(GameObject obj)
    {
        this.obj = obj;
        ExportComponent[] allData = obj.GetComponentsInChildren<ExportComponent>(true);
        mailItemLoopListViewItem2 = allData[0].exportComponent[0] as SuperScrollView.LoopListViewItem2;
        mailItemButton = allData[0].exportComponent[1] as UnityEngine.UI.Button;
        mailItemImage = allData[0].exportComponent[2] as UnityEngine.UI.Image;
        titleTextMeshProUGUI = allData[1].exportComponent[0] as TMPro.TextMeshProUGUI;
        timeTextMeshProUGUI = allData[2].exportComponent[0] as TMPro.TextMeshProUGUI;
        redPointObj = allData[3].gameObject;
    }
}
