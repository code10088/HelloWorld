using UnityEngine;

public class SkillEntity_Bullet : SkillEntity, MoveSystemInterface
{
    private int count = 0;
    private MoveComponent move;

    protected BoxCollider2D collider;
    protected ContactFilter2D contactFilter;
    protected Collider2D[] results = new Collider2D[100];

    public void Init(Vector3 pos)
    {
        if (obj == null) obj = new GameObjectComponent();
        var parent = BattleManager.Instance.BattleScene.GetTransform(skill.Config.FightConfig.FightType.ToString());
        obj.Init(skill.Config.FightConfig.ModelPath, parent, LoadFinish);
        if (transform == null) transform = new TransformComponent();
        transform.Init(obj);
        transform.SetPos(pos);
        if (move == null) move = new MoveComponent();
        move.Init(transform);
        move.SetV(skill.Config.FightConfig.Speed);
        move.SetW(skill.Config.FightConfig.AngleSpeed);
        SystemManager.Instance.MoveSystem.AddEntity(this);
    }
    private void LoadFinish()
    {
        transform.SetPos();
    }
    public override void Clear()
    {
        base.Clear();
        obj.Clear();
        transform.Clear();
        count = 0;
        SystemManager.Instance.MoveSystem.RemoveEntity(this);
    }
    public void UpdateMove(float t)
    {
        move.Update(t);
    }
    public override void PlaySkill(float t)
    {
        //…À∫¶¥Œ ˝
        int count1 = 1;
        if (skill.Config.Internal > 0)
        {
            int count2 = 1 + Mathf.FloorToInt((t - skill.Config.Delay) / skill.Config.Internal);
            count1 = count2 - count;
            count = count2;
        }
        if (count1 == 0) return;
        //…À∫¶√¸÷–
        int count3 = Physics2D.OverlapCollider(collider, contactFilter, results);
        FightEntity[] target = new FightEntity[count3];
        for (int i = 0; i < count3; i++)
        {
            int code = results[i].GetHashCode();
            int id = SkillCollider.Find(code);
            target[i] = EntityCacheManager.Instance.FightCache.GetEntity<FightEntity>(id);
        }
        for (int i = 0; i < count1; i++)
        {
            for (int j = 0; j < count3; j++)
            {
                BattleCalculation.Instance.Attack(this, target[j]);
            }
        }
    }
}