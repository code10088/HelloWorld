using cfg;
using UnityEngine;

public class CommonEquipItemPool : MonoSingleton<CommonEquipItemPool>, SingletonInterface
{
    private ObjectPool<CommonEquipItem> pool;

    public void Init()
    {
        gameObject.SetActive(false);
        pool = new ObjectPool<CommonEquipItem>();
        pool.Init($"{ZResConst.ResUIPrefabPath}Common/CommonEquipItem.prefab", 30);
    }

    public CommonEquipItem Get()
    {
        return pool.Get();
    }
    public void Return(CommonEquipItem item)
    {
        pool.Return(item);
    }
}

public class CommonEquipItem : ObjectPoolItem
{
    private CommonEquipItemComponent comp;
    private RectTransform rt;
    private Transform parent;

    private static int atlasId = -1;
    private EquipItems cfg;
    private bool active = true;
    private bool received = false;

    protected override void Finish(GameObject obj)
    {
        base.Finish(obj);
        comp ??= obj?.GetComponent<CommonEquipItemComponent>();
        rt ??= obj?.GetComponent<RectTransform>();
        Refresh();
    }
    public override void Disable()
    {
        base.Disable();
        obj?.transform.SetParent(CommonEquipItemPool.Instance.transform);
        parent = null;
        cfg = null;
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
        comp ??= obj?.GetComponent<CommonEquipItemComponent>();
        rt ??= obj?.GetComponent<RectTransform>();
    }
    /// <summary>
    /// 刷新装备数据
    /// </summary>
    public void Refresh(int equipId)
    {
        cfg = ConfigManager.Instance.TbEquipItems[equipId];
        Refresh();
    }
    private void Refresh()
    {
        if (obj == null || cfg == null) return;
        obj.SetActive(active);
        obj.transform.parent = parent ? parent : CommonEquipItemPool.Instance.transform;
        rt.anchoredPosition3D = Vector2.zero;
        obj.transform.localRotation = Quaternion.identity;
        obj.transform.localScale = Vector3.one;
        AtlasManager.Instance.LoadSprite(ref atlasId, ZResConst.ResUIAtlasItemPath, cfg.Quality.ToString(), sprite => comp.qualityImage.sprite = sprite);
        AtlasManager.Instance.LoadSprite(ref atlasId, ZResConst.ResUIAtlasItemPath, cfg.Icon, sprite => comp.iconImage.sprite = sprite);
        comp.nameTextMeshProUGUI.text = LanguageManager.Instance.Get(cfg.NameKey);
        if (DataManager.Instance.PlayerData.Lv < cfg.Level)
        {
            comp.lockGameObject.SetActive(true);
            comp.lockLvTextMeshProUGUI.text = "Lv." + cfg.Level;
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
            rt.anchoredPosition3D = Vector2.zero;
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale = Vector3.one;
        }
    }
    public void SetActive(bool state)
    {
        active = state;
        if (obj) obj.SetActive(state);
    }
    public void SetReceived(bool state)
    {
        received = state;
        if (obj) comp.receivedGameObject.SetActive(state);
    }
}