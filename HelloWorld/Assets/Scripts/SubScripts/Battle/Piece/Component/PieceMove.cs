using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HotAssembly
{
    public enum MoveMode
    {
        Dir,
        Target,
    }
    /// <summary>
    /// 移动多样性使Entity难以继承
    /// </summary>
    public class PieceMove
    {
        protected PieceEntity piece;
        protected MoveMode moveMode;
        protected Vector3 dir;
        protected PieceEntity target;
        protected float speed;

        public void Init(PieceEntity piece)
        {
            this.piece = piece;
        }
        public void MoveDir(Vector3 dir, float speed)
        {
            moveMode = MoveMode.Dir;
            this.dir = dir;
            this.speed = speed;
        }
        public void MoveTrack(PieceEntity target, float speed)
        {
            moveMode = MoveMode.Target;
            this.target = target;
            this.speed = speed;
        }
        /// <summary>
        /// 速度曲线无法枚举，运动过程中减速buff难以处理，所以使用此接口自定义
        /// </summary>
        /// <param name="speed"></param>
        public void SetSpeed(float speed)
        {
            this.speed = speed;
        }
        public virtual void Update(float t)
        {

        }
    }
    public class PieceMoveLine : PieceMove
    {
        public override void Update(float t)
        {
            base.Update(t);
            Vector3 pos = piece.PieceModel.Pos;
            if (moveMode == MoveMode.Dir)
            {
                pos += dir * speed * t;
            }
            else if (moveMode == MoveMode.Target)
            {
                dir = target.PieceModel.Pos - piece.PieceModel.Pos;
                pos += dir * speed * t;
            }
            piece.PieceModel.SetPos(pos);
        }
    }
    public class PieceMoveSpiral : PieceMove
    {
        private float radius;
        public void SetRadius(float f)
        {
            radius = f;
        }
        public override void Update(float t)
        {
            Vector3 pos = piece.PieceModel.Pos;
            if (moveMode == MoveMode.Dir)
            {
                Vector3 temp = pos - dir;
                float radian = t * speed / radius;
                float cos = Mathf.Cos(radian);
                float sin = Mathf.Sin(radian);
                pos.x = temp.x * cos - temp.y * sin;
                pos.y = temp.y * cos + temp.x * sin;
                piece.PieceModel.SetPos(pos);
            }
            else if (moveMode == MoveMode.Target)
            {
                Vector3 temp = pos - target.PieceModel.Pos;
                float radian = t * speed / radius;
                float cos = Mathf.Cos(radian);
                float sin = Mathf.Sin(radian);
                pos.x = temp.x * cos - temp.y * sin;
                pos.y = temp.y * cos + temp.x * sin;
                piece.PieceModel.SetPos(pos);
            }
        }
    }
}