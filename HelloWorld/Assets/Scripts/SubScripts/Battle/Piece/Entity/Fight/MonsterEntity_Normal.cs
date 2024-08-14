namespace HotAssembly
{
    public class MonsterEntity_Normal : MonsterEntity
    {
        protected PieceMove_Normal monsterMove;

        protected override void InitMove()
        {
            if (monsterMove == null) monsterMove = new PieceMove_Normal();
            pieceMove = monsterMove;
            monsterMove.Init(this);
            monsterMove.SetV(config.PieceConfig.Speed);
            monsterMove.SetW(config.PieceConfig.AngleSpeed);
        }
        public override bool Update(float t)
        {
            monsterMove.Update(t);
            return base.Update(t);
        }
        protected override void CheckState()
        {
            switch (monsterState)
            {
                case MonsterState.Idle:
                    monsterMove.Stop();
                    target = FightManager.Instance.FindNearTarget(pieceModel.Pos, allyId);
                    if (target != null) ChangeState(MonsterState.Attack);
                    break;
                case MonsterState.Attack:
                    var result = pieceSkill.AutoPlaySkill();
                    if (result == PieceSkillState.NoTarget) ChangeState(MonsterState.Idle);
                    else if (result == PieceSkillState.NoDistance) monsterMove.MoveDir(pieceModel.Pos, target.PieceModel.Pos - pieceModel.Pos);
                    else monsterMove.Stop();
                    break;
            }
        }
    }
}