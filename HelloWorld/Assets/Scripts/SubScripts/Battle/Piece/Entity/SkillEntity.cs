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
        private BoxCollider2D collider;
        private ContactFilter2D contactFilter;
        private Collider2D[] results = new Collider2D[50];
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
            int count = Physics2D.OverlapCollider(collider, contactFilter, results);
            for (int i = 0; i < count; i++)
            {

            }
        }
    }
}