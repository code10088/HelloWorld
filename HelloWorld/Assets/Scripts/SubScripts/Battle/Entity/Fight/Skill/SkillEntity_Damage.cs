using UnityEngine;

public class SkillEntity_Damage : SkillEntity
{
    private int count = 0;

    public override void Clear()
    {
        base.Clear();
        count = 0;
    }
    public override void PlaySkill(float t)
    {
        //ÉËº¦´ÎÊı
        int count1 = 1;
        if (skill.Config.Internal > 0)
        {
            int count2 = 1 + Mathf.FloorToInt((t - skill.Config.Delay) / skill.Config.Internal);
            count1 = count2 - count;
            count = count2;
        }
        if (count1 == 0) return;
        //ÉËº¦ÃüÖĞ
        for (int i = 0; i < count1; i++)
        {
            BattleCalculation.Instance.Attack(this, target);
        }
    }
}