using System;
using Random = System.Random;

public class BattleScene_Rvo : BattleScene
{
    private BattleScene_RvoComponent component = new BattleScene_RvoComponent();
    private Random random = new Random();
    private int timerId = -1;

    protected override void Init()
    {
        base.Init();
        component.Init(SceneObj);

        RVOManager.Instance.Init();
    }
    public override void OnEnable(params object[] param)
    {
        base.OnEnable(param);

        RVOManager.Instance.AddObstacle(component.obstacleObj);
        RVOManager.Instance.Start();
        timerId = TimeManager.Instance.StartTimer(10, 0.1f, CreateTest, false);
    }
    public override void OnDisable()
    {
        base.OnDisable();

        RVOManager.Instance.Stop();
        TimeManager.Instance.StopTimer(timerId);
    }
    public override void OnDestroy()
    {
        base.OnDestroy();

        RVOManager.Instance.Clear();
    }

    private void CreateTest(float t)
    {
        var angle = random.NextDouble() * 2.0f * Math.PI;
        var pos = component.startTransform.position;
        pos.x += (float)Math.Cos(angle);
        pos.y += (float)Math.Sin(angle);
        EntityCacheManager.Instance.FightCache.AddMonster(2, 1, pos);
    }
}