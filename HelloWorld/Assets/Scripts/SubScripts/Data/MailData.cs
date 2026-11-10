using Message;
using System.Collections.Generic;

public class MailData : DataBase
{
    private List<MailDetail> all = new();
    private List<uint> readList = new();

    public List<MailDetail> All => all;

    public void Init()
    {
        NetClient.Instance.Register(NetMsgId.Message_SCMail, SCMail);
        NetClient.Instance.Register(NetMsgId.Message_SCGetMailReward, SCGetMailReward);
        NetClient.Instance.Register(NetMsgId.Message_SCGetMailAllReward, SCGetMailAllReward);
        NetClient.Instance.Register(NetMsgId.Message_SCDeleteMail, SCDeleteMail);
    }
    public void Clear()
    {
        NetClient.Instance.Unregister(NetMsgId.Message_SCMail);
        NetClient.Instance.Unregister(NetMsgId.Message_SCGetMailReward);
        NetClient.Instance.Unregister(NetMsgId.Message_SCGetMailAllReward);
        NetClient.Instance.Unregister(NetMsgId.Message_SCDeleteMail);
    }

    public void CSMail()
    {
        NetClient.Instance.Send(NetMsgId.Message_CSMail, new CSMail());
    }
    private void SCMail(IDeserialize msg)
    {
        var mail = (SCMail)msg;
        all = mail.details;
        EventManager.Instance.Fire(EventType.RefreshMail);
    }
    /// <summary>
    /// 关闭界面时调用，发送已读消息
    /// </summary>
    public void CSReadMail()
    {
        NetClient.Instance.Send(NetMsgId.Message_CSReadMail, new CSReadMail { lists = readList.ToArray() });
    }
    /// <summary>
    /// 领取单封邮件奖励
    /// </summary>
    public void CSGetMailReward(uint id)
    {
        NetClient.Instance.Send(NetMsgId.Message_CSGetMailReward, new CSGetMailReward { mailId = id });
    }
    private void SCGetMailReward(IDeserialize msg)
    {
        var mail = (SCGetMailReward)msg;
        var data = all.Find(a => a.mailId == mail.mailId);
        if (data == null) return;
        if (mail.status == 0)
        {
            data.status = 2;
            EventManager.Instance.Fire(EventType.RefreshMail);
        }
        DataManager.Instance.ShowRewardData.ShowRewards(data.rewards);
    }
    /// <summary>
    /// 一键领取所有奖励
    /// </summary>
    public void CSGetMailAllReward()
    {
        NetClient.Instance.Send(NetMsgId.Message_CSGetMailAllReward, new CSGetMailAllReward());
    }
    private void SCGetMailAllReward(IDeserialize msg)
    {
        List<RewardInfo> rewards = new();
        foreach (var item in all)
        {
            if (item.status < 2 && item.rewards.Count > 0)
            {
                item.status = 2;
                rewards.AddRange(item.rewards);
            }
        }
        EventManager.Instance.Fire(EventType.RefreshMail);
        DataManager.Instance.ShowRewardData.ShowRewards(rewards);
    }
    /// <summary>
    /// 删除单封邮件
    /// </summary>
    public void CSDeleteMail(uint id)
    {
        NetClient.Instance.Send(NetMsgId.Message_CSDeleteMail, new CSDeleteMail { mailId = id });
    }
    private void SCDeleteMail(IDeserialize msg)
    {
        var mail = (SCDeleteMail)msg;
        if (mail.status == 0)
        {
            var index = all.FindIndex(a => a.mailId == mail.mailId);
            if (index >= 0)
            {
                all.RemoveAt(index);
                EventManager.Instance.Fire(EventType.RefreshMail);
            }
        }
    }
    /// <summary>
    /// 删除已读邮件
    /// </summary>
    public void CSDeleteAllMail()
    {
        NetClient.Instance.Send(NetMsgId.Message_CSDeleteAllMail, new CSDeleteAllMail());
    }

    public void SetRead(uint id)
    {
        var data = all.Find(a => a.mailId == id);
        if (data != null && data.status == 0)
        {
            data.status = 1;
            readList.Add(id);
        }
    }
    public void SetReadAll()
    {
        for (int i = 0; i < all.Count; i++)
        {
            if (all[i].status == 0)
            {
                all[i].status = 1;
                readList.Add(all[i].mailId);
            }
        }
    }
    public bool HasRewards()
    {
        for (int i = 0; i < all.Count; i++)
        {
            if (all[i].rewards.Count > 0) return true;
        }
        return false;
    }
}