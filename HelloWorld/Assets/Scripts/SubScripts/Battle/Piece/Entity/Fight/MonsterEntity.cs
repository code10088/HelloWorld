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
        Moderate,
        Frozen,
        Vertigo,
    }
    public class MonsterEntity : FightEntity
    {
        protected MonsterConfig config;
        protected MonsterState monsterState = MonsterState.None;
        protected PieceMove_Normal monsterMove;

        public void Init(MonsterConfig config, Vector3 pos)
        {
            this.config = config;
            if (pieceModel == null) pieceModel = new PieceModel();
            var parent = BattleManager.Instance.BattleScene.GetTransform(config.PieceConfig.PieceType.ToString());
            pieceModel.Init(config.PieceConfig.ModelPath, parent, pos);
            if (pieceSkill == null) pieceSkill = new PieceSkill();
            pieceSkill.Init(this, config.PieceConfig.Skills);
            if (pieceAttr == null) pieceAttr = new PieceAttr();
            foreach (var item in config.PieceConfig.Attrs) pieceAttr.SetAttr(item.Key, item.Value);
            if (monsterMove == null) monsterMove = new PieceMove_Normal();
            pieceMove = monsterMove;
            monsterMove.Init(this);
            monsterMove.SetV(config.PieceConfig.Speed);
            monsterMove.SetW(config.PieceConfig.AngleSpeed);
            ChangeState(MonsterState.Enter);
        }
        public override void Clear()
        {
            base.Clear();
            pieceModel.Clear();
            pieceSkill.Clear();
            pieceAttr.Clear();
            config = null;
            target = null;
        }
        public override bool Update(float t)
        {
            base.Update(t);
            pieceSkill.Update(t);
            monsterMove.Update(t);
            CheckState();
            float hp = pieceAttr.GetAttr(PieceAttrEnum.Hp);
            return hp <= 0;
        }
        public void CheckState()
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