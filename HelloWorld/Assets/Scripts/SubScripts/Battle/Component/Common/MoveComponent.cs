using UnityEngine;

namespace HotAssembly
{
    public enum MoveMode
    {
        Dir = 1,
        Target = 2,
    }
    public class MoveComponent : ECS_Component
    {
        public MoveMode moveMode;
        public TransformComponent self;
        public TransformComponent target;

        public bool stop = true;
        public float w;
        public float v;
        public Vector3 curPos;
        public Vector3 curDir;
        public Curve wcurve;
        public Curve vcurve;

        public void Init(TransformComponent self)
        {
            this.self = self;
        }
        public void MoveDir(Vector3 startPos, Vector3 startDir)
        {
            moveMode = MoveMode.Dir;
            curPos = startPos;
            curDir = startDir;
            stop = false;
        }
        public void MoveTrack(TransformComponent target, Vector3 startPos, Vector3 startDir)
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
                Vector2 dir = target.Pos - curPos;
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
            self.SetPos(curPos);
        }
    }
}