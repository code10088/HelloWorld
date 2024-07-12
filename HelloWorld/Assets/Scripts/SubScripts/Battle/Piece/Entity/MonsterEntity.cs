using cfg;
using UnityEngine;

namespace HotAssembly
{
    public enum MonsterState
    {
        None,
        Enter,
        Idle,
        Attack,
        Patrol,
        Moderate,
        Frozen,
        Vertigo,
    }
    public class MonsterEntity : PieceEntity
    {
        protected MonsterConfig config;
        protected MonsterState monsterState = MonsterState.None;

        public void Init(MonsterConfig config)
        {
            this.config = config;
            ChangeState(MonsterState.Enter);
        }
        public override bool Update(float t)
        {
            CheckState();
            return false;
        }
        public void CheckState()
        {
            switch (monsterState)
            {
                case MonsterState.Idle:
                    target = PieceManager.Instance.FindNearArmyTarget(pieceModel.Pos, allyId);
                    if (target == null) ChangeState(MonsterState.Patrol);
                    else ChangeState(MonsterState.Attack);
                    break;
                case MonsterState.Patrol:
                    target = PieceManager.Instance.FindNearArmyTarget(pieceModel.Pos, allyId);
                    if (target != null) ChangeState(MonsterState.Attack);
                    break;
                case MonsterState.Attack:
                    var result = pieceSkill.AutoPlaySkill();
                    if (result == PieceSkillState.NoDistance)
                    {
                        float f = pieceAttr.GetAttr(PieceAttrEnum.MoveSpeed);
                        var dir = Vector3.Normalize(target.PieceModel.Pos - pieceModel.Pos);
                        //通过piecemove移动
                        //pieceModel.Pos += dir * f;
                    }
                    break;
                    
            }
        }
        public void ChangeState(MonsterState state)
        {
            switch (monsterState)
            {
                //退出
                case MonsterState.Enter:
                    break;
            }
            var record = monsterState;
            monsterState = state;
            switch (monsterState)
            {
                //进入
                case MonsterState.Enter:
                    PieceModel.PlayAni(PieceAniEnum.Enter.ToString(), 0, () => ChangeState(MonsterState.Idle));
                    break;
                case MonsterState.Attack:
                    break;
            }
        }
    }
}