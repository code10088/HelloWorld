using UnityEngine;
using static UnityEngine.GraphicsBuffer;

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
        protected MonsterState monsterState = MonsterState.None;
        private MonsterAni pieceAni;

        public override void Init()
        {
            base.Init();
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
                    target = PieceManager.Instance.FindNearArmyTarget(pos, allyId);
                    if (target == null) ChangeState(MonsterState.Patrol);
                    else ChangeState(MonsterState.Attack);
                    break;
                case MonsterState.Patrol:
                    target = PieceManager.Instance.FindNearArmyTarget(pos, allyId);
                    if (target != null) ChangeState(MonsterState.Attack);
                    break;
                case MonsterState.Attack:
                    if (pieceSkill.AtkDis < Vector3.Distance(pos, target.Pos))
                    {
                        float f = pieceAttr.GetAttr(MonsterAttrEnum.MoveSpeed);
                        var dir = Vector3.Normalize(target.Pos - pos);
                        pos += dir * f;
                    }
                    else
                    {
                        pieceSkill.AutoPlaySkill();
                    }
                    break;
                    
            }
        }
        public void ChangeState(MonsterState state)
        {
            switch (monsterState)
            {
                //ÍË³ö
                case MonsterState.Enter:
                    break;
            }
            var record = monsterState;
            monsterState = state;
            switch (monsterState)
            {
                //½øÈë
                case MonsterState.Enter:
                    pieceAni.PlayAni(MonsterAniEnum.Enter, 0, () => ChangeState(MonsterState.Idle));
                    break;
                case MonsterState.Attack:
                    break;
            }
        }
    }
}