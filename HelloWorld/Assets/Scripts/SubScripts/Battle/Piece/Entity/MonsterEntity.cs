using cfg;

namespace HotAssembly
{
    public enum MonsterState
    {
        None,
        Enter,
        Idle,
        Attack,
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
        public override void Clear()
        {
            base.Clear();
            config = null;
        }
        public override bool Update(float t)
        {
            base.Update(t);
            CheckState();
            float hp = pieceAttr.GetAttr(PieceAttrEnum.Hp);
            return hp <= 0;
        }
        public void CheckState()
        {
            switch (monsterState)
            {
                case MonsterState.Idle:
                    pieceMove.Stop();
                    target = PieceManager.Instance.FindNearArmyTarget(pieceModel.Pos, allyId);
                    if (target != null) ChangeState(MonsterState.Attack);
                    break;
                case MonsterState.Attack:
                    var result = pieceSkill.AutoPlaySkill();
                    if (result == PieceSkillState.NoTarget) ChangeState(MonsterState.Idle);
                    else if (result == PieceSkillState.NoDistance) pieceMove.MoveDir(pieceModel.Pos, target.PieceModel.Pos - pieceModel.Pos);
                    else pieceMove.Stop();
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
                    ChangeState(MonsterState.Idle);
                    break;
                case MonsterState.Attack:
                    break;
            }
        }
    }
}