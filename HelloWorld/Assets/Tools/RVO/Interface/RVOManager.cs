using System;
using System.Collections.Generic;
using RVO;
using UnityEngine;
using Random = System.Random;
using Vector2 = RVO.Vector2;

public class RVOManager : Singletion<RVOManager>, SingletionInterface
{
    private int updateId = -1;

    private Dictionary<int, Agent> agents = new();
    private Queue<Agent> cache = new();

    public void Init()
    {
        Simulator.Instance.setTimeStep(0.25f);
        Simulator.Instance.setAgentDefaults(10.0f, 10, 5.0f, 5.0f, 1.0f, 1f, new Vector2(0.0f, 0.0f));
#if UNITY_WEBGL
        Simulator.Instance.SetNumWorkers(1);
#else
        Simulator.Instance.SetNumWorkers(8);
#endif
    }

    public void Start()
    {
        if (updateId < 0) updateId = Updater.Instance.StartUpdate(Update);
    }
    private void Update()
    {
        Simulator.Instance.doStep();
        foreach (var item in agents) item.Value.Update();
    }
    public void Stop()
    {
        if (updateId > 0) Updater.Instance.StopUpdate(updateId);
    }
    public void Clear()
    {
        Simulator.Instance.Clear();
        agents.Clear();
    }

    public void AddObstacle(GameObject obj)
    {
        BoxCollider2D[] colliders = obj.GetComponentsInChildren<BoxCollider2D>();
        for (int i = 0; i < colliders.Length; i++)
        {
            var pos = colliders[i].transform.position;
            var scale = colliders[i].transform.localScale;
            var size = colliders[i].size;
            float minX = pos.x - size.x * scale.x * 0.5f;
            float minY = pos.y - size.y * scale.y * 0.5f;
            float maxX = pos.x + size.x * scale.x * 0.5f;
            float maxY = pos.y + size.y * scale.y * 0.5f;
            IList<Vector2> obstacle = new List<Vector2>()
            {
                new Vector2(minX, minY),
                new Vector2(maxX, minY),
                new Vector2(maxX, maxY),
                new Vector2(minX, maxY),
            };
            Simulator.Instance.addObstacle(obstacle);
        }
        Simulator.Instance.processObstacles();
    }
    public int AddAgent(Vector3 pos, Action<Vector3> change)
    {
        Agent agent = cache.Count > 0 ? cache.Dequeue() : new Agent();
        int id = agent.Init(pos, change);
        agents.Add(id, agent);
        return id;
    }
    public void RemoveAgent(int agentId)
    {
        if (agents.TryGetValue(agentId, out Agent agent))
        {
            agent.Clear();
            cache.Enqueue(agent);
            agents.Remove(agentId);
        }
    }
    public void RefreshTarget(int agentId, Vector3 target)
    {
        if (agents.TryGetValue(agentId, out Agent agent)) agent.RefreshTarget(target);
    }
    public void SetAgentRadius(int agentId, float radius)
    {
        if (agents.TryGetValue(agentId, out Agent agent)) agent.SetAgentRadius(radius);
    }
    public void SetAgentMaxSpeed(int agentId, float speed)
    {
        if (agents.TryGetValue(agentId, out Agent agent)) agent.SetAgentMaxSpeed(speed);
    }
}

public class Agent
{
    private static Random random = new Random();
    private int agentId = -1;
    private Vector3 target;
    private Action<Vector3> change;

    public int Init(Vector3 start, Action<Vector3> change)
    {
        agentId = Simulator.Instance.addAgent(new Vector2(start.x, start.y));
        this.change = change;
        return agentId;
    }
    public void Clear()
    {
        Simulator.Instance.RemoveAgent(agentId);
        agentId = -1;
        change = null;
    }
    public void RefreshTarget(Vector3 target)
    {
        this.target = target;
    }
    public void SetAgentRadius(float radius)
    {
        Simulator.Instance.setAgentRadius(agentId, radius);
    }
    public void SetAgentMaxSpeed(float speed)
    {
        Simulator.Instance.setAgentMaxSpeed(agentId, speed);
    }
    public void Update()
    {
        Vector2 pos1 = Simulator.Instance.getAgentPosition(agentId);
        Vector3 pos2 = new Vector3(pos1.x(), pos1.y());
        change?.Invoke(pos2);

        Vector3 dir = target - pos2;
        Vector2 vector = new Vector2(dir.x, dir.y);
        if (RVOMath.absSq(vector) > 1.0f) vector = RVOMath.normalize(vector);
        Simulator.Instance.setAgentPrefVelocity(agentId, vector);
        float angle = (float)random.NextDouble() * 2.0f * (float)Math.PI;
        float dis = (float)random.NextDouble() * 0.0001f;
        Simulator.Instance.setAgentPrefVelocity(agentId, vector + dis * new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)));
    }
}