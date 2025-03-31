using cfg;
using UnityEngine;

public class MonsterMoveEntity : MonsterEntity, MoveSystemInterface
{
    private MoveComponent move;

    public override void Init(MonsterConfig config, Vector3 pos)
    {
        base.Init(config, pos);
        if (move == null) move = new MoveComponent();
        move.Init(transform);
        move.SetV(config.FightConfig.Speed);
        move.SetW(config.FightConfig.AngleSpeed);
        SystemManager.Instance.MoveSystem.AddEntity(this);
    }
    public override void Clear()
    {
        base.Clear();
        SystemManager.Instance.MoveSystem.RemoveEntity(this);
    }
    public void UpdateMove(float t)
    {
        move.Update(t);
    }
    public override void UpdateState()
    {
        base.UpdateState();
        if ((monsterState & MonsterState.Idle) == MonsterState.Idle)
        {
            move.Stop();
            target = EntityCacheManager.Instance.FightCache.FindNearTarget(transform.Pos, allyId);
            if (target != null)
            {
                monsterState &= ~MonsterState.Idle;
                monsterState |= MonsterState.Attack;
            }
        }
        if ((monsterState & MonsterState.Attack) == MonsterState.Attack)
        {
            var result = play.AutoPlaySkill();
            if (result == SkillState.NoTarget)
            {
                monsterState &= ~MonsterState.Attack;
                monsterState |= MonsterState.Idle;
            }
            else if (result == SkillState.NoDistance) move.MoveDir(transform.Pos, target.Transform.Pos - transform.Pos);
            else move.Stop();
        }
    }
}