using cfg;
using System.Collections.Generic;
using UnityEngine;

public enum SkillState
{
    None,
    Success,
    NoInterrupt,
    NoCooldown,
    NoDistance,
    NoTarget,
}
public class PlaySkillComponent : ECS_Component
{
    private FightEntity entity;
    private List<SkillItem> skills = new List<SkillItem>();
    private SkillItem skill;

    public List<SkillItem> Skills => skills;

    public void Init(FightEntity entity, List<int> list)
    {
        this.entity = entity;
        for (int i = 0; i < list.Count; i++)
        {
            SkillItem skillItem = new SkillItem();
            skillItem.Init(entity, list[i]);
            skills.Add(skillItem);
        }
    }
    public SkillState PlaySkill(int skillId)
    {
        var item = skills.Find(a => a.SkillId == skillId);
        if (item == null) return SkillState.None;
        if (item.CooldownTimer > 0) return SkillState.NoCooldown;
        if (item.NeedTarget && (entity.Target == null || !entity.Target.Active)) return SkillState.NoTarget;
        if (item.AtkDis < Vector3.Distance(entity.Transform.Pos, entity.Target.Transform.Pos)) return SkillState.NoDistance;
        if (skill != null && item.InterruptPriority < skill.BeInterruptPriority) return SkillState.NoInterrupt;
        if (skill != null) skill.Reset();
        SkillState result = item.PlaySkill();
        if (result == SkillState.Success) skill = item;
        return result;
    }
    public SkillState AutoPlaySkill()
    {
        SkillState result = SkillState.None;
        for (int i = 0; i < skills.Count; i++)
        {
            var state = PlaySkill(skills[i].SkillId);
            if (state == SkillState.Success) return SkillState.Success;
            else if (state > result) result = state;
        }
        return result;
    }
    public void Update(float t)
    {
        for (int i = 0; i < skills.Count; i++) skills[i].Update(t);
    }
    public void Clear()
    {
        for (int i = 0; i < skills.Count; i++) skills[i].Reset();
        skills.Clear();
    }
}

public class SkillItem
{
    protected FightEntity entity;
    protected SkillConfig config;
    protected float cooldownTimer;
    private int timerId;

    public int SkillId => config.ID;
    public bool NeedTarget => config.NeedTarget;
    public float CooldownTimer => cooldownTimer;
    public float AtkDis => config.AtkDis;
    public float InterruptPriority => config.InterruptPriority;
    public float BeInterruptPriority => config.BeInterruptPriority;

    public void Init(FightEntity entity, int skillId)
    {
        this.entity = entity;
        config = ConfigManager.Instance.GameConfigs.TbSkillConfig[skillId];
        cooldownTimer = 0;
    }
    public SkillState PlaySkill()
    {
        entity.Ani.PlayAni("Skill");
        if (config.Anticipation == 0) _PlaySkill();
        else timerId = TimeManager.Instance.StartTimer(config.Anticipation, finish: _PlaySkill);
        return SkillState.Success;
    }
    private void _PlaySkill()
    {
        EntityCacheManager.Instance.SkillCache.AddSkill(SkillId, entity.AllyId, entity.Transform.Pos, entity);
    }
    public void Update(float t)
    {
        cooldownTimer -= t;
        cooldownTimer = Mathf.Max(0, cooldownTimer);
    }
    public void Reset()
    {
        cooldownTimer = config.Cooldown;
        TimeManager.Instance.StopTimer(timerId);
        timerId = -1;
    }
}