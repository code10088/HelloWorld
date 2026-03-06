using UnityEngine;

public class TestScene : SceneBase
{
    private TestSceneComponent comp;
    private AssetObjectPool<ObjectPoolItem> pool = new AssetObjectPool<ObjectPoolItem>();

    private int testEffectId = -1;

    protected override void Init()
    {
        base.Init();
        comp = component as TestSceneComponent;
        pool.Init($"{ZResConst.ResScenePrefabPath}TestScene/TestBullet.prefab");
    }
    public override void OnEnable(params object[] param)
    {
        base.OnEnable(param);
        GameDebug.Log("TestScene OnEnable");

        testEffectId = EffectManager.Instance.Get($"{ZResConst.ResSceneEffectPath}Fire/Fire.prefab", comp.fireRootTransform);
    }
    public override void OnDisable()
    {
        base.OnDisable();
        GameDebug.Log("TestScene OnDisable");

        EffectManager.Instance.Recycle(testEffectId);
    }
    public override void OnDestroy()
    {
        base.OnDestroy();
        GameDebug.Log("TestScene OnDestroy");
        pool.Destroy();
    }

    public void LoadBulletFromPool()
    {
        pool.Dequeue((a, b, c) =>
        {
            b.transform.SetParent(comp.transform);
            b.transform.localScale = Vector3.one * Random.Range(0, 10);
        });
        pool.Dequeue((a, b, c) =>
        {
            b.transform.SetParent(comp.transform);
            b.transform.localScale = Vector3.one * Random.Range(0, 10);
        });
        pool.Enqueue(pool.Use[0].ItemID);
        pool.Dequeue((a, b, c) =>
        {
            b.transform.SetParent(comp.transform);
            b.transform.localScale = Vector3.one * Random.Range(0, 10);
        });
    }
    public void DelectBullet()
    {
        pool.Enqueue(pool.Use[0].ItemID);
    }
}