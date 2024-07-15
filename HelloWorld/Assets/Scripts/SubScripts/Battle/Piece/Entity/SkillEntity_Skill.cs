namespace HotAssembly
{
    public class SkillEntity_Skill : SkillEntity
    {
        protected override void PlaySkill()
        {
            for (int i = 0; i < pieceSkill.Skills.Count; i++)
            {
                pieceSkill.PlaySkill(pieceSkill.Skills[i].SkillId);
            }
        }
    }
}