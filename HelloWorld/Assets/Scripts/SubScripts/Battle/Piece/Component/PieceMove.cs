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
        private PieceEntity piece;
        private MoveMode moveMode;
        private PieceEntity target;
        private float w;
        private float v;
        private Vector3 curPos;
        private Vector3 curDir;

        private Curve wcurve;
        private Curve vcurve;


        public void Init(PieceEntity piece)
        {
            this.piece = piece;
        }
        public void MoveDir(Vector3 startPos, Vector3 startDir)
        {
            moveMode = MoveMode.Dir;
            curPos = startPos;
            curDir = startDir;
        }
        public void MoveTrack(PieceEntity target, Vector3 startPos, Vector3 startDir)
        {
            moveMode = MoveMode.Target;
            this.target = target;
            curPos = startPos;
            curDir = startDir;
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
    public enum CurveType
    {
        None,
        Line,
        InSine,
        OutSine,
    }
    public class Curve
    {
        private float min;
        private float max;
        private float time;
        private float timer;
        private CurveType curve;

        public Curve(float min, float max, float time, CurveType curve)
        {
            this.min = min;
            this.max = max;
            this.time = time;
            this.curve = curve;
        }
        public float Update(float t)
        {
            timer += t;
            float y = 0;
            float x = timer / time;
            switch (curve)
            {
                case CurveType.Line:
                    y = x;
                    break;
                case CurveType.InSine:
                    y = 1 - Mathf.Cos(x * Mathf.PI / 2);
                    break;
                case CurveType.OutSine:
                    y = Mathf.Sin(x * Mathf.PI / 2);
                    break;
            }
            return min + (max - min) * y;
        }
    }
}