using UnityEngine;

namespace HotAssembly
{
    /// <summary>
    /// 移动多样性使Entity难以继承
    /// </summary>
    public class PieceMove_Normal : PieceMove
    {
        private MoveMode moveMode;
        private PieceEntity target;

        private bool stop = true;
        private float w;
        private float v;
        private Vector3 curPos;
        private Vector3 curDir;
        private Curve wcurve;
        private Curve vcurve;

        public void MoveDir(Vector3 startPos, Vector3 startDir)
        {
            moveMode = MoveMode.Dir;
            curPos = startPos;
            curDir = startDir;
            stop = false;
        }
        public void MoveTrack(PieceEntity target, Vector3 startPos, Vector3 startDir)
        {
            moveMode = MoveMode.Target;
            this.target = target;
            curPos = startPos;
            curDir = startDir;
            stop = false;
        }
        public void Stop()
        {
            stop = true;
        }

        public void SetW(float w)
        {
            this.w = w;
        }
        public void SetWCurve(float min, float max, float time, CurveType curve)
        {
            wcurve = new Curve(min, max, time, curve);
        }
        public void SetV(float v)
        {
            this.v = v;
        }
        public void SetVCurve(float min, float max, float time, CurveType curve)
        {
            vcurve = new Curve(min, max, time, curve);
        }
        public void SetPos(Vector3 pos)
        {
            curPos = pos;
        }
        public void SetDir(Vector3 dir)
        {
            curDir = dir;
        }
        public void Update(float t)
        {
            if (stop) return;
            if (wcurve != null) w = wcurve.Update(t);
            if (vcurve != null) v = vcurve.Update(t);

            float angle = w * t;
            bool b = true;
            if (moveMode == MoveMode.Target)
            {
                Vector2 dir = target.PieceModel.Pos - curPos;
                Vector3 tempV = Vector3.Cross(curDir, dir);
                if (tempV.y >= 0) angle = -w * t;
                float tempAngle = Vector3.Angle(curDir, dir);
                b = Mathf.Abs(angle) < tempAngle;
                if (!b) curDir = dir;
            }
            if (b)
            {
                float radian = angle * Mathf.Deg2Rad;
                float cos = Mathf.Cos(radian);
                float sin = Mathf.Sin(radian);
                float tempx = curDir.x * cos - curDir.y * sin;
                float tempy = curDir.y * cos + curDir.x * sin;
                curDir.x = tempx;
                curDir.y = tempy;
            }
            curDir = curDir.normalized;
            curPos += curDir * v * t;
            piece.PieceModel.SetPos(curPos);
        }
    }
}