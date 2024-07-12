using cfg;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HotAssembly
{
    public class BattleCalculation : Singletion<BattleCalculation>
    {
        public void Attack(PieceEntity origin, PieceEntity target)
        {
            var atk = origin.PieceAttr.GetAttr(PieceAttrEnum.Atk);
            var def = target.PieceAttr.GetAttr(PieceAttrEnum.Def);
            if (atk <= def) return;
            target.PieceAttr.SetAttr(PieceAttrEnum.Hp, atk - def);
        }
        public void AddEffect(PieceEntity target, PieceAttrEnum k, float v)
        {
            target.PieceAttr.SetAttr(k, v);
        }
    }
}