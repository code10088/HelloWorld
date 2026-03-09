using cfg;
using UnityEngine;

public class CommonItemPool : MonoSingletion<CommonItemPool>, SingletionInterface
{
    private AssetObjectPool<CommonItem> pool;

    public void Init()
    {
        gameObject.SetActive(false);
        pool = new AssetObjectPool<CommonItem>();
        pool.Init($"{ZResConst.ResUIPrefabPath}Common/CommonItem.prefab", 30);
    }

    public CommonItem Get()
    {
        return pool.Dequeue();
    }
    public void Recycle(CommonItem item)
    {
        pool.Enqueue(item.ItemID);
    }
}
public class CommonItem : ObjectPoolItem
{
    private CommonItemComponent comp;
    private Transform parent;

    private static int atlasId = -1;
    private Items cfg;
    private int count;
    private bool active = true;
    private bool received = false;

    protected override void Finish(GameObject obj)
    {
        base.Finish(obj);
        comp ??= obj?.GetComponent<CommonItemComponent>();
        Refresh();
    }
    public override void Disable()
    {
        base.Disable();
        obj?.transform.SetParent(CommonItemPool.Instance.transform);
        parent = null;
        cfg = null;
        count = 0;
        active = true;
        received = false;
    }

    /// <summary>
    /// 用于直接添加到UI上的Item，不用Pool管理
    /// </summary>
    public void Refresh(GameObject obj)
    {
        this.obj = obj;
        parent = obj.transform.parent;
        comp ??= obj?.GetComponent<CommonItemComponent>();
    }
    /// <summary>
    /// 刷新数据
    /// </summary>
    public void Refresh(int id)
    {
        cfg = ConfigManager.Instance.TbItems[id];
        Refresh();
    }
    private void Refresh()
    {
        if (obj == null || cfg == null)
        {
            return;
        }
        obj.SetActive(active);
        obj.transform.parent = parent ? parent : CommonItemPool.Instance.transform;
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;
        obj.transform.localScale = Vector3.one;
        AtlasManager.Instance.LoadSprite(ref atlasId, ZResConst.ResUIAtlasItemPath, cfg.Quality.ToString(), sprite => comp.qualityImage.sprite = sprite);
        AtlasManager.Instance.LoadSprite(ref atlasId, ZResConst.ResUIAtlasItemPath, cfg.Icon, sprite => comp.iconImage.sprite = sprite);
        comp.numTextMeshProUGUI.text = count.ToString();
        comp.nameTextMeshProUGUI.text = cfg.ItemNameKey;
        if (DataManager.Instance.PlayerData.Lv > cfg.UseLevel)
        {
            comp.lockGameObject.SetActive(true);
            comp.lockLvTextMeshProUGUI.text = "Lv." + cfg.UseLevel;
        }
        else
        {
            comp.lockGameObject.SetActive(false);
        }
        comp.receivedGameObject.SetActive(received);
    }
    public void SetParent(Transform parent)
    {
        this.parent = parent;
        if (obj)
        {
            obj.transform.parent = parent;
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale = Vector3.one;
        }
    }
    public void SetActive(bool state)
    {
        active = state;
        if (obj) obj.SetActive(state);
    }
    public void SetCount(int count)
    {
        this.count = count;
        if (obj) comp.numTextMeshProUGUI.text = count.ToString();
    }
    public void SetReceived(bool state)
    {
        received = state;
        if (obj) comp.receivedGameObject.SetActive(state);
    }
}