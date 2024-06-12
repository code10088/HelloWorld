using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HotAssembly
{
    public enum MoveType
    {
        None,
        Line,
    }
    public class SkillEntity : PieceEntity
    {
        
        public void Init(Dictionary<PieceAttrEnum, float> attrs)
        {

        }
        public override bool Update(float t)
        {
            Trigger();
            return base.Update(t);
        }
        protected virtual void Trigger()
        {
            //持续时间、伤害间隔

            CheckCollision();

            //造成伤害(-1伤害后结束，0每帧伤害，1一秒伤害一次)
            //override释放技能，发射一个实体
        }
        protected virtual void CheckCollision()
        {
            //碰撞用collider简单通用

        }
    }
}