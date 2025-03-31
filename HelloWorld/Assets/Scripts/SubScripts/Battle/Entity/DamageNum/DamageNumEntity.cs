using cfg;
using TMPro;
using UnityEngine;
using UnityExtensions.Tween;

public class DamageNumEntity : ECS_Entity
{
    private GameObjectComponent obj;
    private TransformComponent transform;
    private string content;

    private TextMeshPro tmp;
    private TweenPlayer tp;

    public void Init(DamageNumConfig config, string content, Vector3 pos)
    {
        if (obj == null) obj = new GameObjectComponent();
        var parent = BattleManager.Instance.BattleScene.GetTransform("DamageNum");
        obj.Init(config.PrefabPath, parent, LoadFinish);
        if (transform == null) transform = new TransformComponent();
        transform.Init(obj);
        transform.SetPos(pos);
        this.content = content;
        Enable();
    }
    public override void Clear()
    {
        base.Clear();
        content = null;
        EntityCacheManager.Instance.DamageNumCache.Remove(this);
    }

    private void LoadFinish()
    {
        tmp = obj.Obj.transform.GetChild(0).GetComponent<TextMeshPro>();
        tp = obj.Obj.GetComponent<TweenPlayer>();
        tp.OnForwardArrived = Disable;
        Enable();
    }
    private void Enable()
    {
        transform.SetPos();
        tmp.SetText(content);
        tp.SetForwardDirectionAndEnabled();
    }
    private void Disable()
    {
        Remove();
    }
}