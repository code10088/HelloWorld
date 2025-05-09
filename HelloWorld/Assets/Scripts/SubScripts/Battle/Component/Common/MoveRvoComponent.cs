using System;
using UnityEngine;
using Random = System.Random;

public class MoveRvoComponent : ECS_Component
{
    private static Random random = new Random();
    private TransformComponent transform;
    private int agentId = -1;
    private Vector3 targetPos;
    private float radius = 1f;
    private float speed = 1f;

    public void Init(TransformComponent transform)
    {
        this.transform = transform;
        var pos = transform.Pos;
        var angle = random.NextDouble() * 2.0f * Math.PI;
        pos.x += (float)Math.Cos(angle);
        pos.y += (float)Math.Sin(angle);
        transform.SetPos(pos);
        agentId = RVOManager.Instance.AddAgent(pos, Change);
        RefreshTarget(targetPos);
        SetAgentRadius(radius);
        SetAgentMaxSpeed(speed);
    }
    public void Clear()
    {
        RVOManager.Instance.RemoveAgent(agentId);
        agentId = -1;
    }
    public void Stop()
    {
        RVOManager.Instance.SetAgentMaxSpeed(agentId, 0);
    }

    private void Change(Vector3 pos)
    {
        transform.SetPos(pos);
        var dir = targetPos - pos;
        float angle = Vector2.SignedAngle(Vector2.right, dir);
        transform.SetEulerAngles(Vector3.forward * angle);
    }
    public void RefreshTarget(Vector3 target)
    {
        targetPos = target;
        RVOManager.Instance.RefreshTarget(agentId, target);
        RVOManager.Instance.SetAgentMaxSpeed(agentId, speed);
    }
    public void SetAgentRadius(float radius)
    {
        this.radius = radius;
        RVOManager.Instance.SetAgentRadius(agentId, radius);
    }
    public void SetAgentMaxSpeed(float speed)
    {
        this.speed = speed;
        RVOManager.Instance.SetAgentMaxSpeed(agentId, speed);
    }
}