namespace HotAssembly
{
    public class MonsterEntity_Rvo : MonsterEntity
    {
        protected PieceMove_Rvo monsterMove;

        protected override void InitMove()
        {
            if (monsterMove == null) monsterMove = new PieceMove_Rvo();
            pieceMove = monsterMove;
            monsterMove.Init(this);
        }
        public override void Clear()
        {
            base.Clear();
            monsterMove.Clear();
        }
        protected override void CheckState()
        {
            switch (monsterState)
            {
                case MonsterState.Idle:
                    monsterMove.RefreshTarget(BattleManager.Instance.InputWorldPos);
                    target = FightManager.Instance.FindNearTarget(pieceModel.Pos, allyId);
                    if (target != null) ChangeState(MonsterState.Attack);
                    break;
                case MonsterState.Attack:
                    var result = pieceSkill.AutoPlaySkill();
                    if (result == PieceSkillState.NoTarget) ChangeState(MonsterState.Idle);
                    else if (result == PieceSkillState.NoDistance) monsterMove.RefreshTarget(target.PieceModel.Pos);
                    else monsterMove.Stop();
                    break;
            }
        }
    }
}