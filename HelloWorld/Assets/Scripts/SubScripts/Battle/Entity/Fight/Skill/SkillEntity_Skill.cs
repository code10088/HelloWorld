using cfg;

public class SkillEntity_Skill : SkillEntity, PlaySkillSystemInterface
{
    private PlaySkillComponent play;

    public override void Init(SkillConfig config, FightEntity entity)
    {
        base.Init(config, entity);
        if (play == null) play = new PlaySkillComponent();
        play.Init(this, config.FightConfig.Skills);
        SystemManager.Instance.PlaySkillSystem.AddEntity(this);
    }
    public override void Clear()
    {
        base.Clear();
        play.Clear();
        SystemManager.Instance.PlaySkillSystem.RemoveEntity(this);
    }
    public void UpdatePlaySkill(float t)
    {
        play.Update(t);
    }
    public override void PlaySkill(float t)
    {
        for (int i = 0; i < play.Skills.Count; i++)
        {
            play.PlaySkill(play.Skills[i].SkillId);
        }
    }
}