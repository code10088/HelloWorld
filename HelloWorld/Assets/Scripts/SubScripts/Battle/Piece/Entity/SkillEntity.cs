using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HotAssembly
{
    public class SkillEntity : PieceEntity
    {
        //运动、命中判断、命中类型、持续时间、伤害间隔
        //直接造成伤害
        //持续性伤害
        //释放技能，发射一个实体
        public void Init(Dictionary<PieceAttrEnum, float> attrs)
        {

        }
        public override bool Update(float t)
        {
            return base.Update(t);
        }
    }
}