using System.Collections.Generic;

public class BattleScene_Test : BattleScene
{
    private int coroutineId = -1;

    protected override void Init()
    {
        base.Init();
    }

    public override void OnEnable(params object[] param)
    {
        base.OnEnable(param);

        //此时调用SceneManager.Instance.GetScene取不到当前scene的解决办法
        var enumerator = Start();
        coroutineId = Driver.Instance.StartCoroutine(enumerator);
    }
    public override void OnDisable()
    {
        base.OnDisable();
        Driver.Instance.Remove(coroutineId);
    }

    private IEnumerator<ICoroutine> Start()
    {
        yield return new WaitFrame(1);
    }
}