using Message;
using ProtoBuf;
using System.Collections.Generic;

public class MailData : DataBase
{
    private List<MailDetail> all;

    public void Init()
    {
        NetMsgDispatch.Instance.Register(NetMsgId.Message_SCMail, SCMail);
    }
    public void Clear()
    {
        NetMsgDispatch.Instance.UnRegister(NetMsgId.Message_SCMail);
    }

    private void SCMail(IExtensible msg)
    {
        var mail = (SCMail)msg;
        all = mail.Details;
    }
}