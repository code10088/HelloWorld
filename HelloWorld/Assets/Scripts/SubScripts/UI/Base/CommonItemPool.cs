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
    private CommonItemComponent component;
    private Transform parent;

    private static int atlasId = -1;
    private Items cfg;
    private int count;
    private bool active = true;
    private bool received = false;

    protected override void Finish(GameObject obj)
    {
        base.Finish(obj);
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
        if (component == null)
        {
            component = new CommonItemComponent();
            component.Init(obj);
        }
        obj.SetActive(active);
        obj.transform.parent = parent ? parent : CommonItemPool.Instance.transform;
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;
        obj.transform.localScale = Vector3.one;
        AtlasManager.Instance.LoadSprite(ref atlasId, ZResConst.ResUIAtlasItemPath, cfg.Quality.ToString(), sprite => component.qualityImage.sprite = sprite);
        AtlasManager.Instance.LoadSprite(ref atlasId, ZResConst.ResUIAtlasItemPath, cfg.Icon, sprite => component.iconImage.sprite = sprite);
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
        component.receivedObj.SetActive(received);
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
        if (obj) component.numTextMeshProUGUI.text = count.ToString();
    }
    public void SetReceived(bool state)
    {
        received = state;
        if (obj) component.receivedObj.SetActive(state);
    }
}