using cfg;

public class SkillEntity : FightEntity, SkillSystemInterface
{
    protected FightEntity entity;
    protected SkillComponent skill;

    public virtual void Init(SkillConfig config, FightEntity entity)
    {
        this.entity = entity;
        if (attr == null) attr = new AttrComponent();
        attr = entity.Attr.CopyAttr();
        if (skill == null) skill = new SkillComponent();
        skill.Init(this, config);
        SystemManager.Instance.SkillSystem.AddEntity(this);
        target = entity.Target;
    }
    public override void Clear()
    {
        base.Clear();
        attr.Clear();
        skill.Clear();
        entity = null;
        target = null;
        SystemManager.Instance.SkillSystem.RemoveEntity(this);
        EntityCacheManager.Instance.SkillCache.Remove(this);
    }
    public void UpdateSkill(float t)
    {
        skill.Update(t);
    }
    public virtual void PlaySkill(float t)
    {

    }
}