using Message;
using System.Collections.Generic;

public class MailData : DataBase
{
    private List<MailDetail> all = new();
    private List<uint> readList = new();

    public List<MailDetail> All => all;

    public void Init()
    {
        NetMsgDispatch.Instance.Register(NetMsgId.Message_SCMail, SCMail);
        NetMsgDispatch.Instance.Register(NetMsgId.Message_SCGetMailReward, SCGetMailReward);
        NetMsgDispatch.Instance.Register(NetMsgId.Message_SCGetMailAllReward, SCGetMailAllReward);
        NetMsgDispatch.Instance.Register(NetMsgId.Message_SCDeleteMail, SCDeleteMail);

        //测试
        all = new()
        {
            new MailDetail() { mailId = 1, title = "1", time = 1767710843 },
            new MailDetail() { mailId = 2, title = "2", time = 1767710843 },
            new MailDetail() { mailId = 3, title = "3", time = 1767710843 },
            new MailDetail() { mailId = 4, title = "4", time = 1767710843 },
            new MailDetail() { mailId = 5, title = "5", time = 1767710843 },
            new MailDetail() { mailId = 6, title = "6", time = 1767710843 },
            new MailDetail() { mailId = 7, title = "7", time = 1767710843 },
            new MailDetail() { mailId = 8, title = "8", time = 1767710843 },
            new MailDetail() { mailId = 9, title = "9", time = 1767710843 },
            new MailDetail() { mailId = 10, title = "10", time = 1767710843 },
        };
        foreach (var item in all)
        {
            item.rewards.Add(new RewardInfo { itemId = 1, count = 10 });
        }
        EventManager.Instance.Fire(EventType.RefreshMail);
    }
    public void Clear()
    {
        NetMsgDispatch.Instance.UnRegister(NetMsgId.Message_SCMail);
        NetMsgDispatch.Instance.UnRegister(NetMsgId.Message_SCGetMailReward);
        NetMsgDispatch.Instance.UnRegister(NetMsgId.Message_SCDeleteMail);
    }

    public void CSMail()
    {
        SocketManager.Instance.Send(NetMsgId.Message_CSMail, new CSMail());
    }
    private void SCMail(IDeserialize msg)
    {
        var mail = (SCMail)msg;
        all = mail.details;
        EventManager.Instance.Fire(EventType.RefreshMail);
    }
    /// <summary>
    /// 关闭界面时调用，批量标记已读
    /// </summary>
    public void CSReadMail()
    {
        SocketManager.Instance.Send(NetMsgId.Message_CSReadMail, new CSReadMail { lists = readList.ToArray() });

        //测试
        foreach (var mail in readList)
        {
            var data = all.Find(a => a.mailId == mail);
            if (data != null && data.status == 0) data.status = 1;
        }
    }
    /// <summary>
    /// 单个领取奖励
    /// </summary>
    /// <param name="id"></param>
    public void CSGetMailReward(uint id)
    {
        SocketManager.Instance.Send(NetMsgId.Message_CSGetMailReward, new CSGetMailReward { mailId = id });

        //测试
        var data = all.Find(a => a.mailId == id);
        data.status = 2;
        EventManager.Instance.Fire(EventType.RefreshMail);
        DataManager.Instance.ShowRewardData.ShowRewards(data.rewards);
    }
    /// <summary>
    /// 单个领取奖励
    /// </summary>
    /// <param name="msg"></param>
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
        SocketManager.Instance.Send(NetMsgId.Message_CSGetMailAllReward, new CSGetMailAllReward());

        //测试
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
    /// 一键领取所有奖励
    /// </summary>
    private void SCGetMailAllReward(IDeserialize msg)
    {
        var mail = (SCGetMailAllReward)msg;
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
    /// 单个删除邮件
    /// </summary>
    /// <param name="id"></param>
    public void CSDeleteMail(uint id)
    {
        SocketManager.Instance.Send(NetMsgId.Message_CSDeleteMail, new CSDeleteMail { mailId = id });

        //测试
        var index = all.FindIndex(a => a.mailId == id);
        all.RemoveAt(index);
        EventManager.Instance.Fire(EventType.RefreshMail);
    }
    /// <summary>
    /// 单个删除邮件
    /// </summary>
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
    /// <param name="msg"></param>
    public void CSDeleteAllMail()
    {
        SocketManager.Instance.Send(NetMsgId.Message_CSDeleteAllMail, new CSDeleteAllMail());

        //测试
        all.Clear();
        EventManager.Instance.Fire(EventType.RefreshMail);
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