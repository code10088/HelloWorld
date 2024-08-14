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
            InitMove();
            ChangeState(MonsterState.Enter);
        }
        protected virtual void InitMove()
        {

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
            CheckState();
            float hp = pieceAttr.GetAttr(PieceAttrEnum.Hp);
            return hp <= 0;
        }
        protected virtual void CheckState()
        {
            
        }
        protected void ChangeState(MonsterState state)
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