using System.Collections.Generic;
using RVO;
using UnityEngine;
using Vector2 = RVO.Vector2;

public class RVOManager : Singletion<RVOManager>
{
    private int updateId = -1;

    private List<Agent> agents = new List<Agent>();
    private Queue<Agent> cache = new Queue<Agent>();

    public void Init()
    {
        Simulator.Instance.setTimeStep(0.25f);
        Simulator.Instance.setAgentDefaults(10.0f, 10, 5.0f, 5.0f, 1.0f, 0.5f, new Vector2(0.0f, 0.0f));
        Simulator.Instance.SetNumWorkers(1);
    }

    public void Start()
    {
        if (updateId < 0) updateId = Updater.Instance.StartUpdate(Update);
    }

    private void Update()
    {
        Simulator.Instance.doStep();
        for (int i = 0; i < agents.Count; i++) agents[i].Update();
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
    public Agent AddAgent(Vector3 pos, Transform transform, float radius)
    {
        int agentId = Simulator.Instance.addAgent(new Vector2(pos.x, pos.y));
        if (agentId < 0) return null;
        Agent agent = null;
        if (cache.Count > 0) agent = cache.Dequeue();
        else agent = new Agent();
        agent.Init(agentId, transform, radius);
        agents.Add(agent);
        return agent;
    }
    public void RemoveAgent(int agentId)
    {
        int index = agents.FindIndex(a => a.agentId == agentId);
        var agent = agents[index];
        agent.Clear();
        cache.Enqueue(agent);
        agents.RemoveAt(index);
    }
}