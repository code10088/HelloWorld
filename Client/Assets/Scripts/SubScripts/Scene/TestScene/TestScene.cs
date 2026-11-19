using UnityEngine;

public class TestScene : SceneBase
{
    private TestSceneComponent comp;
    private ObjectPool<ObjectPoolItem> pool = new ObjectPool<ObjectPoolItem>();
    private ObjectPoolItem testItem;
    private EffectItem testEffect;

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

        testEffect = EffectManager.Instance.Get($"{ZResConst.ResSceneEffectPath}Fire/Fire.prefab", comp.fireRootTransform);
    }
    public override void OnDisable()
    {
        base.OnDisable();
        GameDebug.Log("TestScene OnDisable");

        EffectManager.Instance.Return(testEffect);
    }
    public override void OnDestroy()
    {
        base.OnDestroy();
        GameDebug.Log("TestScene OnDestroy");
        pool.Clear();
    }

    public void LoadBulletFromPool()
    {
        testItem = pool.Get((a, b) =>
        {
            a.transform.SetParent(comp.transform);
            a.transform.localScale = Vector3.one * Random.Range(0, 10);
        });
    }
    public void DelectBullet()
    {
        pool.Return(testItem);
    }
}