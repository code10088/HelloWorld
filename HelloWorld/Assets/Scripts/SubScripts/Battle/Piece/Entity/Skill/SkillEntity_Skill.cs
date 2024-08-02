using cfg;

namespace HotAssembly
{
    public class SkillEntity_Skill : SkillEntity
    {
        public override void Init(SkillConfig config, PieceEntity piece)
        {
            base.Init(config, piece);
            if (pieceSkill == null) pieceSkill = new PieceSkill();
            pieceSkill.Init(this, config.PieceConfig.Skills);
        }
        public override void Clear()
        {
            base.Clear();
            pieceSkill.Clear();
        }
        protected override void PlaySkill()
        {
            for (int i = 0; i < pieceSkill.Skills.Count; i++)
            {
                pieceSkill.PlaySkill(pieceSkill.Skills[i].SkillId);
            }
        }
        public override bool Update(float t)
        {
            pieceSkill.Update(t);
            return base.Update(t);
        }
    }
}