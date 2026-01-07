using Message;
using SuperScrollView;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIMail : UIBase
{
    private UIMailComponent component = new UIMailComponent();
    private List<CommonItem_Normal> rewardItems = new List<CommonItem_Normal>();
    private uint curMailId = 0;
    private int totalCount = 0;

    protected override void Init()
    {
        base.Init();
        component.Init(UIObj);
        component.bgRectTransform.anchorMin = UIManager.Instance.anchorMinFull;
        component.closeBtnUIButton.onClick.AddListener(OnClose);
        component.readAllBtnUIButton.onClick.AddListener(OnClickReadAll);
        component.deleteAllBtnUIButton.onClick.AddListener(OnClickDeleteAll);
        component.getBtnUIButton.onClick.AddListener(OnClickGetReward);
        component.deleteBtnUIButton.onClick.AddListener(OnClickDelete);
        totalCount = DataManager.Instance.MailData.All.Count;
        component.loopLoopListView2.InitListView(totalCount, OnGetItemByIndex);
    }
    public override void OnEnable(params object[] param)
    {
        base.OnEnable(param);
        EventManager.Instance.RegisterEvent(EventType.RefreshMail, Refresh);

        Refresh(null);
    }
    public override void OnDisable()
    {
        base.OnDisable();
        EventManager.Instance.UnRegisterEvent(EventType.RefreshMail, Refresh);
    }
    public override void OnDestroy()
    {
        for (int i = 0; i < rewardItems.Count; i++)
        {
            CommonItem.Instance.Recycle(rewardItems[i]);
        }
        base.OnDestroy();
    }
    protected override void OnClose()
    {
        base.OnClose();
        DataManager.Instance.MailData.CSReadMail();
    }

    private void Refresh(object o)
    {
        var hasRewards = DataManager.Instance.MailData.HasRewards();
        component.readAllTextTextMeshProUGUI.text = LanguageManager.Instance.Get(hasRewards ? 10006 : 10007);
        if (totalCount == DataManager.Instance.MailData.All.Count)
        {
            component.loopLoopListView2.RefreshAllShownItem();
        }
        else
        {
            totalCount = DataManager.Instance.MailData.All.Count;
            component.loopLoopListView2.SetListItemCount(totalCount, false);
            component.loopLoopListView2.RefreshAllShownItem();
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
            component.contentRootObj.SetActive(false);
            return;
        }
        var item = component.loopLoopListView2.GetShownItemByItemId((int)curMailId);
        if (item) ((UIMailItem)item.ItemData).SetSelect(false);
        curMailId = id;
        item = component.loopLoopListView2.GetShownItemByItemId((int)curMailId);
        if (item) ((UIMailItem)item.ItemData).SetSelect(true);

        var data = DataManager.Instance.MailData.All[index];
        component.contentRootObj.SetActive(true);
        component.titleTextMeshProUGUI.text = data.Title;
        component.contentTextMeshProUGUI.text = data.Content;
        for (int i = 0; i < data.Rewards.Count; i++)
        {
            var reward = data.Rewards[i];
            CommonItem_Normal rewardItem;
            if (i < rewardItems.Count)
            {
                rewardItem = rewardItems[i];
            }
            else
            {
                rewardItem = CommonItem.Instance.Get((int)reward.itemId);
                rewardItems.Add(rewardItem);
            }
            rewardItem.SetParent(component.rewardContentRectTransform);
            rewardItem.SetCount((int)reward.Count);
            rewardItem.SetReceived(data.Status == 2);
            rewardItem.SetActive(true);
        }
        for (int i = data.Rewards.Count; i < rewardItems.Count; i++)
        {
            rewardItems[i].SetActive(false);
        }
        LayoutRebuilder.MarkLayoutForRebuild(component.rewardContentRectTransform);
        if (data.Rewards.Count > 0 && data.Status != 2)
        {
            component.getBtnObj.SetActive(true);
            component.deleteBtnObj.SetActive(false);
        }
        else
        {
            component.getBtnObj.SetActive(false);
            component.deleteBtnObj.SetActive(true);
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
    private UIMailItemComponent component = new UIMailItemComponent();
    private MailDetail data;
    private Action<uint> action;
    public void Init(GameObject obj)
    {
        component.Init(obj);
        component.mailItemButton.onClick.AddListener(OnClick);
    }
    public void SetData(MailDetail data, bool select, Action<uint> action)
    {
        this.data = data;
        this.action = action;
        component.titleTextMeshProUGUI.text = data.Title;
        component.timeTextMeshProUGUI.text = TimeUtils.FormatTime(data.Time);
        component.redPointObj.SetActive(data.Rewards.Count == 0 && data.Status == 0 || data.Rewards.Count > 0 && data.Status != 2);
        SetSelect(select);
    }
    private void OnClick()
    {
        DataManager.Instance.MailData.SetRead(data.mailId);
        component.redPointObj.SetActive(data.Rewards.Count > 0 && data.Status != 2);
        action.Invoke(data.mailId);
    }
    public void SetSelect(bool select)
    {
        component.mailItemImage.color = select ? Color.green : Color.white;
    }
}