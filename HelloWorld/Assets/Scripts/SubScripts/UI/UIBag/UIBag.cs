using SuperScrollView;
using System.Collections.Generic;
using UnityEngine;

public enum ItemType
{
    Item = 1,
    Equip = 2,
}
public class UIBag : UIBase
{
    public struct Entry
    {
        public ItemType ItemType;
        public uint Id;
        public uint Count;
        public long Uid;
    }

    private UIBagComponent comp;
    private List<Entry> entries = new List<Entry>();
    private ItemType tab = ItemType.Item;

    protected override void Init()
    {
        base.Init();
        comp = component as UIBagComponent;
        comp.bgRectTransform.anchorMin = UIManager.Instance.anchorMinFull;
        comp.closeBtnUIButton.onClick.AddListener(OnClose);
        comp.itemTabBtnUIButton.onClick.AddListener(SwitchItem);
        comp.equipTabBtnUIButton.onClick.AddListener(SwitchEquip);
        comp.itemTabUIText.SetText(10101);
        comp.equipTabUIText.SetText(10102);
        comp.scrollViewLoopGridView.InitGridView(0, OnGetItemByIndex);
    }

    public override void OnEnable(params object[] param)
    {
        base.OnEnable(param);
        EventManager.Instance.Register(EventType.RefreshBag, Refresh);
        Refresh();
    }
    public override void OnDisable()
    {
        base.OnDisable();
        EventManager.Instance.Unregister(EventType.RefreshBag, Refresh);
    }
    public override void OnDestroy()
    {
        UIBagItem.ReturnAll();
        base.OnDestroy();
    }

    private void SwitchItem()
    {
        if (tab == ItemType.Item) return;
        tab = ItemType.Item;
        Refresh();
    }
    private void SwitchEquip()
    {
        if (tab == ItemType.Equip) return;
        tab = ItemType.Equip;
        Refresh();
    }
    private void Refresh()
    {
        entries.Clear();
        var data = DataManager.Instance.BagData;
        switch (tab)
        {
            case ItemType.Item:
                for (int i = 0; i < data.Items.Count; i++)
                {
                    var item = data.Items[i];
                    entries.Add(new Entry { ItemType = ItemType.Item, Id = item.itemId, Count = item.count });
                }
                break;
            case ItemType.Equip:
                for (int i = 0; i < data.Equips.Count; i++)
                {
                    var equip = data.Equips[i];
                    entries.Add(new Entry { ItemType = ItemType.Equip, Id = equip.equipId, Count = 1, Uid = equip.uid });
                }
                break;
        }
        comp.scrollViewLoopGridView.SetListItemCount(entries.Count, false);
        comp.scrollViewLoopGridView.RefreshAllShownItem();
    }
    private LoopGridViewItem OnGetItemByIndex(LoopGridView gridView, int index, int row, int column)
    {
        if (index < 0 || index >= entries.Count) return null;
        var item = gridView.NewListViewItem<UIBagItem>("UIBagItem");
        if (item == null) return null;
        var bagItem = item.ItemData as UIBagItem;
        bagItem.SetData(entries[index], index);
        return item;
    }
}

public class UIBagItem : LoopItemData
{
    private static List<UIBagItem> items = new List<UIBagItem>();
    private GameObject slot;
    private CommonItem item;
    private CommonEquipItem equip;
    private int index;

    public void Init(GameObject obj)
    {
        slot = obj;
        items.Add(this);
    }
    public void SetData(UIBag.Entry entry, int index)
    {
        this.index = index;
        switch (entry.ItemType)
        {
            case ItemType.Item:
                if (item == null)
                {
                    item = CommonItemPool.Instance.Get();
                    item.SetParent(slot.transform);
                }
                if (equip != null)
                {
                    CommonEquipItemPool.Instance.Return(equip);
                    equip = null;
                }
                item.SetActive(true);
                item.Refresh((int)entry.Id);
                item.SetCount((int)entry.Count);
                break;
            case ItemType.Equip:
                if (item != null)
                {
                    CommonItemPool.Instance.Return(item);
                    item = null;
                }
                if (equip == null)
                {
                    equip = CommonEquipItemPool.Instance.Get();
                    equip.SetParent(slot.transform);
                }
                equip.SetActive(true);
                equip.Refresh((int)entry.Id);
                break;
        }
    }
    public static void ReturnAll()
    {
        foreach (var item in items)
        {
            if (item.item != null) CommonItemPool.Instance.Return(item.item);
            if (item.equip != null) CommonEquipItemPool.Instance.Return(item.equip);
        }
        items.Clear();
    }
}