using cfg;
using UnityEngine;
using UnityEngine.UI;

public class CommonItem : MonoSingletion<CommonItem>, SingletionInterface
{
    private AssetObjectPool<CommonItem_Normal> normalPool;
    private AssetObjectPool<CommonItem_Equip> equipPool;

    public void Init()
    {
        gameObject.SetActive(false);
    }

    public CommonItem_Normal Get(int id)
    {
        var cfg = ConfigManager.Instance.TbItems[id];
        CommonItem_Normal target = null;
        switch (cfg.ItemType)
        {
            case ItemType.Normal:
                if (normalPool == null)
                {
                    normalPool = new AssetObjectPool<CommonItem_Normal>();
                    normalPool.Init($"{ZResConst.ResUIPrefabPath}Common/CommonItem_Normal.prefab", 30);
                }
                target = normalPool.Dequeue();
                break;
            case ItemType.Equip:
                if (equipPool == null)
                {
                    equipPool = new AssetObjectPool<CommonItem_Equip>();
                    equipPool.Init($"{ZResConst.ResUIPrefabPath}Common/CommonItem_Equip.prefab", 30);
                }
                target = equipPool.Dequeue();
                break;
        }
        target.Init(cfg);
        return target;
    }
    public void Recycle(CommonItem_Normal item)
    {
        switch (item.ItemType)
        {
            case ItemType.Normal:
                normalPool.Enqueue(item.ItemID);
                break;
            case ItemType.Equip:
                equipPool.Enqueue(item.ItemID);
                break;
        }
    }
}
public class CommonItem_Normal : ObjectPoolItem
{
    private CommonItem_NormalComponent component;
    protected Transform parent;

    private static int atlasId = -1;
    protected Items cfg;
    private ItemType itemType;
    protected int count;

    public ItemType ItemType => itemType;

    public void Init(Items cfg)
    {
        this.cfg = cfg;
        itemType = cfg.ItemType;
    }
    public void SetData(Transform parent, int count)
    {
        this.count = count;
        this.parent = parent;
        Refresh();
    }
    public virtual void Refresh()
    {
        if (obj == null)
        {
            return;
        }
        if (component == null)
        {
            component = new CommonItem_NormalComponent();
            component.Init(obj);
        }
        obj.transform.parent = parent ? parent : CommonItem.Instance.transform;
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;
        obj.transform.localScale = Vector3.one;
        SetSprite(component.qualityImage, ZResConst.ResUIAtlasItemPath, cfg.Quality.ToString());
        SetSprite(component.iconImage, ZResConst.ResUIAtlasItemPath, cfg.Icon);
        component.numTextMeshProUGUI.text = count.ToString();
        component.nameTextMeshProUGUI.text = cfg.ItemNameKey;
        if (DataManager.Instance.PlayerData.Lv > cfg.UseLevel)
        {
            component.lockObj.SetActive(true);
            component.lockLvTextMeshProUGUI.text = "Lv." + cfg.UseLevel;
        }
        else
        {
            component.lockObj.SetActive(false);
        }
    }
    protected override void Finish(GameObject obj)
    {
        base.Finish(obj);
        if (obj) Refresh();
    }
    public override void Disable()
    {
        base.Disable();
        obj?.transform.SetParent(CommonItem.Instance.transform);
    }
    protected void SetSprite(Image image, string atlas, string name)
    {
        AtlasManager.Instance.LoadSprite(ref atlasId, atlas, name, sprite => image.sprite = sprite);
    }
}
public class CommonItem_Equip : CommonItem_Normal
{

}