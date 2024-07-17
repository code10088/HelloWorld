using cfg;

namespace HotAssembly
{
    public class BattleCalculation : Singletion<BattleCalculation>
    {
        public void Attack(PieceEntity origin, PieceEntity target)
        {
            if (target == null) return;
            if (!target.Active) return;
            var atk = origin.PieceAttr.GetAttr(PieceAttrEnum.Atk);
            var def = target.PieceAttr.GetAttr(PieceAttrEnum.Def);
            if (atk <= def) return;
            target.PieceAttr.SetAttr(PieceAttrEnum.Hp, def - atk);
        }
    }
}