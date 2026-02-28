using SuperScrollView;
using System.Collections.Generic;
using UnityEngine;

public class UIShowReward : UIBase
{
    private UIShowRewardComponent component = new UIShowRewardComponent();
    private int totalCount = 0;
    private HashSet<UIShowRewardItem> rewardItems = new HashSet<UIShowRewardItem>();

    protected override void Init()
    {
        base.Init();
        component.Init(UIObj);
        component.bgRectTransform.anchorMin = UIManager.Instance.anchorMinFull;
        component.getBtnUIButton.onClick.AddListener(OnClickGetReward);
        totalCount = DataManager.Instance.ShowRewardData.Current.Length;
        component.loopLoopGridView.InitGridView(totalCount, OnGetItemByIndex);
    }
    public override void OnEnable(params object[] param)
    {
        base.OnEnable(param);

        if (totalCount == DataManager.Instance.ShowRewardData.Current.Length)
        {
            component.loopLoopGridView.RefreshAllShownItem();
        }
        else
        {
            totalCount = DataManager.Instance.ShowRewardData.Current.Length;
            component.loopLoopGridView.SetListItemCount(totalCount, false);
            component.loopLoopGridView.RefreshAllShownItem();
        }
    }
    public override void OnDestroy()
    {
        foreach (var item in rewardItems) item.Recycle();
        base.OnDestroy();
    }

    LoopGridViewItem OnGetItemByIndex(LoopGridView gridView, int index, int row, int column)
    {
        if (index < 0 || index >= DataManager.Instance.ShowRewardData.Current.Length)
        {
            return null;
        }
        LoopGridViewItem item = gridView.NewListViewItem<UIShowRewardItem>("RewardItem");
        var reward = item.ItemData as UIShowRewardItem;
        var data = DataManager.Instance.ShowRewardData.Current[index];
        reward.SetData(data);
        rewardItems.Add(reward);
        return item;
    }
    private void OnClickGetReward()
    {
        DataManager.Instance.ShowRewardData.Next();
    }
}

public class UIShowRewardItem : LoopItemData
{
    private UIShowRewardItemComponent component = new UIShowRewardItemComponent();
    private CommonItem rewardItem;
    public void Init(GameObject obj)
    {
        component.Init(obj);
        rewardItem = CommonItemPool.Instance.Get();
        rewardItem.SetParent(component.rewardRectTransform);
    }
    public void SetData(ShowRewardStruct data)
    {
        rewardItem.Refresh(data.Id);
        rewardItem.SetCount(data.Count);
    }
    public void Recycle()
    {
        CommonItemPool.Instance.Recycle(rewardItem);
    }
}