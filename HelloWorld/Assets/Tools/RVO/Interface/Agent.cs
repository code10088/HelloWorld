using System;
using RVO;
using UnityEngine;
using Random = System.Random;
using Vector2 = RVO.Vector2;

public class Agent
{
    public int agentId = -1;
    public Transform transform;
    public Vector3 target;

    private Random random = new Random();

    public void Init(int agentId, Transform transform, float radius)
    {
        this.agentId = agentId;
        this.transform = transform;
        Simulator.Instance.setAgentRadius(agentId, radius);
    }
    public void Clear()
    {
        Simulator.Instance.RemoveAgent(agentId);
        agentId = -1;
        transform = null;
    }
    public void RefreshTarget(Vector3 target)
    {
        this.target = target;
    }
    public void Update()
    {
        Vector2 pos = Simulator.Instance.getAgentPosition(agentId);
        transform.position = new Vector3(pos.x(), pos.y());
        Vector3 dir = target - transform.position;
        transform.forward = dir;

        Vector2 vector = new Vector2(dir.x, dir.y);
        if (RVOMath.absSq(vector) > 1.0f) vector = RVOMath.normalize(vector);
        Simulator.Instance.setAgentPrefVelocity(agentId, vector);
        float angle = (float)random.NextDouble() * 2.0f * (float)Math.PI;
        float dis = (float)random.NextDouble() * 0.0001f;
        Simulator.Instance.setAgentPrefVelocity(agentId, vector + dis * new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)));
    }
}