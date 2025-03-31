using cfg;
using UnityEngine;

public class MonsterRvoEntity : MonsterEntity
{
    private MoveRvoComponent move;

    public override void Init(MonsterConfig config, Vector3 pos)
    {
        base.Init(config, pos);
        if (move == null) move = new MoveRvoComponent();
        move.Init(transform);
    }
    public override void Clear()
    {
        base.Clear();
        move.Clear();
    }
    public override void UpdateState()
    {
        base.UpdateState();
        if ((monsterState & MonsterState.Idle) == MonsterState.Idle)
        {
            move.RefreshTarget(BattleManager.Instance.InputWorldPos);
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
            else if (result == SkillState.NoDistance) move.RefreshTarget(target.Transform.Pos);
            else move.Stop();
        }
    }
}