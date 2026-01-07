using Message;
using ProtoBuf;
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
        NetMsgDispatch.Instance.Register(NetMsgId.Message_SCDeleteMail, SCDeleteMail);

        //测试
        all = new()
        {
            new MailDetail() { mailId = 1, Title = "1", Time = 1767710843 },
            new MailDetail() { mailId = 2, Title = "2", Time = 1767710843 },
            new MailDetail() { mailId = 3, Title = "3", Time = 1767710843 },
            new MailDetail() { mailId = 4, Title = "4", Time = 1767710843 },
            new MailDetail() { mailId = 5, Title = "5", Time = 1767710843 },
            new MailDetail() { mailId = 6, Title = "6", Time = 1767710843 },
            new MailDetail() { mailId = 7, Title = "7", Time = 1767710843 },
            new MailDetail() { mailId = 8, Title = "8", Time = 1767710843 },
            new MailDetail() { mailId = 9, Title = "9", Time = 1767710843 },
            new MailDetail() { mailId = 10, Title = "10", Time = 1767710843 },
        };
        foreach (var item in all)
        {
            item.Rewards.Add(new RewardInfo { itemId = 1, Count = 10 });
        }
        EventManager.Instance.FireEvent(EventType.RefreshMail);
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
    private void SCMail(IExtensible msg)
    {
        var mail = (SCMail)msg;
        all = mail.Details;
        EventManager.Instance.FireEvent(EventType.RefreshMail);
    }
    /// <summary>
    /// 关闭界面时调用，批量标记已读
    /// </summary>
    public void CSReadMail()
    {
        SocketManager.Instance.Send(NetMsgId.Message_CSReadMail, new CSReadMail { Lists = readList.ToArray() });

        //测试
        foreach (var mail in readList)
        {
            var data = all.Find(a => a.mailId == mail);
            if (data != null && data.Status == 0) data.Status = 1;
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
        data.Status = 2;
        EventManager.Instance.FireEvent(EventType.RefreshMail);
    }
    /// <summary>
    /// 单个领取奖励
    /// </summary>
    /// <param name="msg"></param>
    private void SCGetMailReward(IExtensible msg)
    {
        var mail = (SCGetMailReward)msg;
        if (mail.Status == 0)
        {
            var data = all.Find(a => a.mailId == mail.mailId);
            if (data != null)
            {
                data.Status = 2;
                EventManager.Instance.FireEvent(EventType.RefreshMail);
            }
        }
    }
    /// <summary>
    /// 一键领取所有奖励
    /// </summary>
    public void CSGetMailAllReward()
    {
        SocketManager.Instance.Send(NetMsgId.Message_CSGetMailAllReward, new CSGetMailAllReward());

        //测试
        foreach (var item in all)
        {
            if (item.Rewards.Count > 0) item.Status = 2;
        }
        EventManager.Instance.FireEvent(EventType.RefreshMail);
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
        EventManager.Instance.FireEvent(EventType.RefreshMail);
    }
    /// <summary>
    /// 单个删除邮件
    /// </summary>
    private void SCDeleteMail(IExtensible msg)
    {
        var mail = (SCDeleteMail)msg;
        if (mail.Status == 0)
        {
            var index = all.FindIndex(a => a.mailId == mail.mailId);
            if (index >= 0)
            {
                all.RemoveAt(index);
                EventManager.Instance.FireEvent(EventType.RefreshMail);
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
        EventManager.Instance.FireEvent(EventType.RefreshMail);
    }


    public void SetRead(uint id)
    {
        var data = all.Find(a => a.mailId == id);
        if (data != null && data.Status == 0)
        {
            data.Status = 1;
            readList.Add(id);
        }
    }
    public void SetReadAll()
    {
        for (int i = 0; i < all.Count; i++)
        {
            if (all[i].Status == 0)
            {
                all[i].Status = 1;
                readList.Add(all[i].mailId);
            }
        }
    }
    public bool HasRewards()
    {
        for (int i = 0; i < all.Count; i++)
        {
            if (all[i].Rewards.Count > 0) return true;
        }
        return false;
    }
}