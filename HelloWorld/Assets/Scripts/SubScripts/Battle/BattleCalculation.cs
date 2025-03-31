using cfg;

public class BattleCalculation : Singletion<BattleCalculation>
{
    public void Attack(SkillEntity origin, FightEntity target)
    {
        if (target == null) return;
        if (!target.Active) return;
        var atk = origin.Attr.GetAttr(AttrEnum.Atk);
        var def = target.Attr.GetAttr(AttrEnum.Def);
        if (atk <= def) return;
        target.Attr.SetAttr(AttrEnum.Hp, def - atk);
    }
}