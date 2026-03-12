using Message;
using SuperScrollView;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIMail : UIBase
{
    private UIMailComponent comp;
    private List<CommonItem> rewardItems = new List<CommonItem>();
    private uint curMailId = 0;
    private int totalCount = 0;

    protected override void Init()
    {
        base.Init();
        comp = component as UIMailComponent;
        comp.bgRectTransform.anchorMin = UIManager.Instance.anchorMinFull;
        comp.closeBtnUIButton.onClick.AddListener(OnClose);
        comp.readAllBtnUIButton.onClick.AddListener(OnClickReadAll);
        comp.deleteAllBtnUIButton.onClick.AddListener(OnClickDeleteAll);
        comp.getBtnUIButton.onClick.AddListener(OnClickGetReward);
        comp.deleteBtnUIButton.onClick.AddListener(OnClickDelete);
        totalCount = DataManager.Instance.MailData.All.Count;
        comp.loopLoopListView2.InitListView(totalCount, OnGetItemByIndex);
    }
    public override void OnEnable(params object[] param)
    {
        base.OnEnable(param);
        EventManager.Instance.Register(EventType.RefreshMail, Refresh);

        Refresh();
    }
    public override void OnDisable()
    {
        base.OnDisable();
        EventManager.Instance.Unregister(EventType.RefreshMail, Refresh);
    }
    public override void OnDestroy()
    {
        for (int i = 0; i < rewardItems.Count; i++)
        {
            CommonItemPool.Instance.Recycle(rewardItems[i]);
        }
        base.OnDestroy();
    }
    protected override void OnClose()
    {
        base.OnClose();
        DataManager.Instance.MailData.CSReadMail();
    }

    private void Refresh()
    {
        var hasRewards = DataManager.Instance.MailData.HasRewards();
        comp.readAllTextTextMeshProUGUI.text = LanguageManager.Instance.Get(hasRewards ? 10006 : 10007);
        if (totalCount == DataManager.Instance.MailData.All.Count)
        {
            comp.loopLoopListView2.RefreshAllShownItem();
        }
        else
        {
            totalCount = DataManager.Instance.MailData.All.Count;
            comp.loopLoopListView2.SetListItemCount(totalCount, false);
            comp.loopLoopListView2.RefreshAllShownItem();
        }
        RefreshContent(curMailId);
    }
    LoopListViewItem2 OnGetItemByIndex(LoopListView2 listView, int index)
    {
        if (index < 0 || index >= DataManager.Instance.MailData.All.Count)
        {
            return null;
        }
        LoopListViewItem2 item = listView.NewListViewItem<UIMailItem>("MailItem");
        var mail = item.ItemData as UIMailItem;
        var data = DataManager.Instance.MailData.All[index];
        mail.SetData(data, data.mailId == curMailId, RefreshContent);
        item.ItemId = (int)data.mailId;
        return item;
    }
    private void RefreshContent(uint id)
    {
        var index = DataManager.Instance.MailData.All.FindIndex(a => a.mailId == id);
        if (index < 0)
        {
            comp.contentRootGameObject.SetActive(false);
            return;
        }
        var item = comp.loopLoopListView2.GetShownItemByItemId((int)curMailId);
        if (item) ((UIMailItem)item.ItemData).SetSelect(false);
        curMailId = id;
        item = comp.loopLoopListView2.GetShownItemByItemId((int)curMailId);
        if (item) ((UIMailItem)item.ItemData).SetSelect(true);

        var data = DataManager.Instance.MailData.All[index];
        comp.contentRootGameObject.SetActive(true);
        comp.titleTextMeshProUGUI.text = data.Title;
        comp.contentTextMeshProUGUI.text = data.Content;
        for (int i = 0; i < data.Rewards.Count; i++)
        {
            var reward = data.Rewards[i];
            CommonItem rewardItem;
            if (i < rewardItems.Count)
            {
                rewardItem = rewardItems[i];
            }
            else
            {
                rewardItem = CommonItemPool.Instance.Get();
                rewardItem.SetParent(comp.rewardContentRectTransform);
                rewardItems.Add(rewardItem);
            }
            rewardItem.Refresh((int)reward.itemId);
            rewardItem.SetCount((int)reward.Count);
            rewardItem.SetReceived(data.Status == 2);
            rewardItem.SetActive(true);
        }
        for (int i = data.Rewards.Count; i < rewardItems.Count; i++)
        {
            rewardItems[i].SetActive(false);
        }
        LayoutRebuilder.MarkLayoutForRebuild(comp.rewardContentRectTransform);
        if (data.Rewards.Count > 0 && data.Status != 2)
        {
            comp.getBtnGameObject.SetActive(true);
            comp.deleteBtnGameObject.SetActive(false);
        }
        else
        {
            comp.getBtnGameObject.SetActive(false);
            comp.deleteBtnGameObject.SetActive(true);
        }
    }
    private void OnClickReadAll()
    {
        if (DataManager.Instance.MailData.HasRewards())
        {
            DataManager.Instance.MailData.CSGetMailAllReward();
        }
        else
        {
            DataManager.Instance.MailData.SetReadAll();
        }
    }
    private void OnClickGetReward()
    {
        DataManager.Instance.MailData.CSGetMailReward(curMailId);
    }
    private void OnClickDelete()
    {
        DataManager.Instance.MailData.CSDeleteMail(curMailId);
    }
    private void OnClickDeleteAll()
    {
        DataManager.Instance.MailData.CSDeleteAllMail();
    }
}

public class UIMailItem : LoopItemData
{
    private UIMailItemComponent comp;
    private MailDetail data;
    private Action<uint> action;
    public void Init(GameObject obj)
    {
        comp = obj.GetComponent<UIMailItemComponent>();
        comp.mailItemButton.onClick.AddListener(OnClick);
    }
    public void SetData(MailDetail data, bool select, Action<uint> action)
    {
        this.data = data;
        this.action = action;
        comp.titleTextMeshProUGUI.text = data.Title;
        comp.timeTextMeshProUGUI.text = TimeUtils.FormatTime(data.Time);
        comp.redPointGameObject.SetActive(data.Rewards.Count == 0 && data.Status == 0 || data.Rewards.Count > 0 && data.Status != 2);
        SetSelect(select);
    }
    private void OnClick()
    {
        DataManager.Instance.MailData.SetRead(data.mailId);
        comp.redPointGameObject.SetActive(data.Rewards.Count > 0 && data.Status != 2);
        action.Invoke(data.mailId);
    }
    public void SetSelect(bool select)
    {
        comp.mailItemImage.color = select ? Color.green : Color.white;
    }
}