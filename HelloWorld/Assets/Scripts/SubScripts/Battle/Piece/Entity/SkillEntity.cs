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
            //����ʱ�䡢�˺����

            CheckCollision();

            //����˺�(-1�˺��������0ÿ֡�˺���1һ���˺�һ��)
            //override�ͷż��ܣ�����һ��ʵ��
        }
        protected virtual void CheckCollision()
        {
            //��ײ��collider��ͨ��

        }
    }
}