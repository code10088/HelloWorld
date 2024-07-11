using cfg;
using UnityEngine;

namespace HotAssembly
{
    public class SkillEntity_Damage : SkillEntity
    {
        private int count = 0;
        private BoxCollider2D collider;
        private ContactFilter2D contactFilter;
        private Collider2D[] results = new Collider2D[50];

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
                target[i] = PieceManager.Instance.GetPiece(id);
            }
            for (int i = 0; i < count3; i++)
            {
                BattleCalculation.Instance.Attack(this, target[i]);
            }
        }
    }
}