using UnityEngine;

public class TransformComponent : ECS_Component
{
    private GameObjectComponent obj;
    private Vector3 pos;
    private Vector3 euler;

    public Vector3 Pos => pos;

    public void Init(GameObjectComponent obj)
    {
        this.obj = obj;
        SetPos(pos);
        SetEulerAngles(euler);
    }
    /// <summary>
    /// ÊÀ½ç×ø±ê
    /// </summary>
    public void SetPos(Vector3 pos)
    {
        this.pos = pos;
        SetPos();
    }
    public void SetPos()
    {
        if (obj.Obj == null) return;
        obj.Obj.transform.position = pos;
    }
    public void SetEulerAngles(Vector3 angle)
    {
        euler = angle;
        SetEulerAngles();
    }
    public void SetEulerAngles()
    {
        if (obj.Obj == null) return;
        obj.Obj.transform.eulerAngles = euler;
    }
    public void Clear()
    {
        obj = null;
    }
}