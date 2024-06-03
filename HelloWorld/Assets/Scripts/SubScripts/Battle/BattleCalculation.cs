using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HotAssembly
{
    public class BattleCalculation : Singletion<BattleCalculation>
    {
        public void Attack(PieceEntity origin, PieceEntity target)
        {
            var atk = origin.pieceAttr.GetAttr(MonsterAttrEnum.Atk);
            var def = origin.pieceAttr.GetAttr(MonsterAttrEnum.Def);
            if (atk <= def) return;
            target.pieceAttr.SetAttr(MonsterAttrEnum.Hp, atk - def);
        }
        public void AddEffect(PieceEntity target, MonsterAttrEnum k, float v)
        {
            target.pieceAttr.SetAttr(k, v);
        }
    }
}