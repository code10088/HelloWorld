using Message;
using System.Collections.Generic;

public class BagData : DataBase
{
    private List<Message.BagData> items = new List<Message.BagData>();
    private List<EquipData> equips = new List<EquipData>();

    public IReadOnlyList<Message.BagData> Items => items;
    public IReadOnlyList<EquipData> Equips => equips;

    public void Init()
    {
        NetClient.Instance.Register(NetMsgId.Message_SCBag, SCBag);
        NetClient.Instance.Register(NetMsgId.Message_SCUseItem, SCUseItem);
    }
    public void Clear()
    {
        NetClient.Instance.Unregister(NetMsgId.Message_SCBag);
        NetClient.Instance.Unregister(NetMsgId.Message_SCUseItem);
        items.Clear();
        equips.Clear();
    }

    public Message.BagData GetItem(uint itemId)
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].itemId == itemId) return items[i];
        }
        return null;
    }
    public int GetItemCount(uint itemId)
    {
        int count = 0;
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].itemId == itemId) count += (int)items[i].count;
        }
        return count;
    }
    public EquipData GetEquip(long uid)
    {
        for (int i = 0; i < equips.Count; i++)
        {
            if (equips[i].uid == uid) return equips[i];
        }
        return null;
    }
    private void SCBag(IDeserialize msg)
    {
        var bag = (SCBag)msg;
        SetData(bag.items, bag.equips);
    }
    public void SetData(List<Message.BagData> newItems, List<EquipData> newEquips)
    {
        items = newItems;
        equips = newEquips;
        EventManager.Instance.Fire(EventType.RefreshBag);
    }
    public void UseItem(uint itemId, uint count = 1)
    {
        if (count == 0) return;
        var msg = new CSUseItem();
        msg.itemId = itemId;
        msg.count = count;
        NetClient.Instance.Send(NetMsgId.Message_CSUseItem, msg);
    }
    private void SCUseItem(IDeserialize msg)
    {
        var result = (SCUseItem)msg;
        if (result.code != 1) return;
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].itemId == result.itemId)
            {
                if (result.count == 0) items.RemoveAt(i);
                else items[i].count = result.count;
                break;
            }
        }
        EventManager.Instance.Fire(EventType.RefreshBag);
    }
}