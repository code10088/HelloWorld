using UnityEngine;

namespace HotAssembly
{
    public class SkillEntity_Bullet : SkillEntity
    {
        private PieceMove_Normal bulletMove;

        private int count = 0;

        protected BoxCollider2D collider;
        protected ContactFilter2D contactFilter;
        protected Collider2D[] results = new Collider2D[100];

        public void Init(Vector3 pos)
        {
            if (pieceModel == null) pieceModel = new PieceModel();
            var parent = BattleManager.Instance.BattleScene.GetTransform(config.PieceConfig.PieceType.ToString());
            pieceModel.Init(config.PieceConfig.ModelPath, parent, pos);
            if (bulletMove == null) bulletMove = new PieceMove_Normal();
            pieceMove = bulletMove;
            bulletMove.Init(this);
            bulletMove.SetV(config.PieceConfig.Speed);
            bulletMove.SetW(config.PieceConfig.AngleSpeed);
        }
        public override void Clear()
        {
            base.Clear();
            pieceModel.Clear();
            count = 0;
        }
        public override bool Update(float t)
        {
            bulletMove.Update(t);
            return base.Update(t);
        }
        protected override void PlaySkill()
        {
            //…À∫¶¥Œ ˝
            int count1 = 1;
            if (config.Internal > 0)
            {
                int count2 = 1 + Mathf.FloorToInt((timer - config.Delay) / config.Internal);
                count1 = count2 - count;
                count = count2;
            }
            if (count1 == 0) return;
            //…À∫¶√¸÷–
            int count3 = Physics2D.OverlapCollider(collider, contactFilter, results);
            PieceEntity[] target = new PieceEntity[count3];
            for (int i = 0; i < count3; i++)
            {
                int code = results[i].GetHashCode();
                int id = PieceCollider.Find(code);
                target[i] = FightManager.Instance.GetFight(id);
            }
            for (int i = 0; i < count1; i++)
            {
                for (int j = 0; j < count3; j++)
                {
                    BattleCalculation.Instance.Attack(this, target[j]);
                }
            }
        }
    }
}