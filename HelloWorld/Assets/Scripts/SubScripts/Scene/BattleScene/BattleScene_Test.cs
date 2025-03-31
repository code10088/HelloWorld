using System.Collections.Generic;
using UnityEngine;

public class BattleScene_Test : BattleScene
{
    private BattleScene_TestComponent component = new BattleScene_TestComponent();
    private int coroutineId = -1;

    protected override void Init()
    {
        base.Init();
        component.Init(SceneObj);
    }

    public override void OnEnable(params object[] param)
    {
        base.OnEnable(param);

        //此时调用SceneManager.Instance.GetScene取不到当前scene的解决办法
        var enumerator = Start();
        coroutineId = CoroutineManager.Instance.StartCoroutine(enumerator);
    }
    public override void OnDisable()
    {
        base.OnDisable();
        CoroutineManager.Instance.Stop(coroutineId);
    }

    private IEnumerator<Coroutine> Start()
    {
        yield return new WaitForFrame(1);
        EntityCacheManager.Instance.FightCache.AddMonster(1, 0, Vector3.right * 2);
        EntityCacheManager.Instance.FightCache.AddMonster(1, 1, Vector3.left * 2);
    }
}