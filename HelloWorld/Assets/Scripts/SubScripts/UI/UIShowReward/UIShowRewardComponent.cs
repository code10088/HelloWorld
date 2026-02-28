using UnityEngine;
public partial class UIShowRewardComponent
{
    public GameObject obj;
    public UnityEngine.RectTransform bgRectTransform = null;
    public SuperScrollView.LoopGridView loopLoopGridView = null;
    public SuperScrollView.LoopGridViewItem rewardItemLoopGridViewItem = null;
    public UnityEngine.UI.Button rewardItemButton = null;
    public UnityEngine.UI.Image rewardItemImage = null;
    public UIButton getBtnUIButton = null;
    public void Init(GameObject obj)
    {
        this.obj = obj;
        ExportComponent[] allData = obj.GetComponentsInChildren<ExportComponent>(true);
        bgRectTransform = allData[0].exportComponent[0] as UnityEngine.RectTransform;
        loopLoopGridView = allData[1].exportComponent[0] as SuperScrollView.LoopGridView;
        rewardItemLoopGridViewItem = allData[2].exportComponent[0] as SuperScrollView.LoopGridViewItem;
        rewardItemButton = allData[2].exportComponent[1] as UnityEngine.UI.Button;
        rewardItemImage = allData[2].exportComponent[2] as UnityEngine.UI.Image;
        getBtnUIButton = allData[4].exportComponent[0] as UIButton;
    }
}
public partial class UIShowRewardItemComponent
{
    public GameObject obj;
    public SuperScrollView.LoopGridViewItem rewardItemLoopGridViewItem = null;
    public UnityEngine.UI.Button rewardItemButton = null;
    public UnityEngine.UI.Image rewardItemImage = null;
    public UnityEngine.RectTransform rewardRectTransform = null;
    public void Init(GameObject obj)
    {
        this.obj = obj;
        ExportComponent[] allData = obj.GetComponentsInChildren<ExportComponent>(true);
        rewardItemLoopGridViewItem = allData[0].exportComponent[0] as SuperScrollView.LoopGridViewItem;
        rewardItemButton = allData[0].exportComponent[1] as UnityEngine.UI.Button;
        rewardItemImage = allData[0].exportComponent[2] as UnityEngine.UI.Image;
        rewardRectTransform = allData[1].exportComponent[0] as UnityEngine.RectTransform;
    }
}
