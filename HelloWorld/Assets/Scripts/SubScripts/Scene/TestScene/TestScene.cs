﻿using UnityEngine;

public class TestScene : SceneBase
{
    private TestSceneComponent component = new TestSceneComponent();
    private AssetObjectPool<ObjectPoolItem> pool = new AssetObjectPool<ObjectPoolItem>();

    private int testEffectId = -1;

    protected override void Init()
    {
        base.Init();
        component.Init(SceneObj);
        pool.Init($"{ZResConst.ResScenePrefabPath}TestScene/TestBullet.prefab");
    }
    public override void OnEnable(params object[] param)
    {
        base.OnEnable(param);
        GameDebug.Log("TestScene OnEnable");

        testEffectId = EffectManager.Instance.AddEffect($"{ZResConst.ResSceneEffectPath}Fire/Fire.prefab", component.fireRootTransform);
    }
    public override void OnDisable()
    {
        base.OnDisable();
        GameDebug.Log("TestScene OnDisable");

        EffectManager.Instance.Remove(testEffectId);
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
            b.transform.SetParent(component.obj.transform);
            b.transform.localScale = Vector3.one * Random.Range(0, 10);
        });
        pool.Dequeue((a, b, c) =>
        {
            b.transform.SetParent(component.obj.transform);
            b.transform.localScale = Vector3.one * Random.Range(0, 10);
        });
        pool.Enqueue(pool.Use[0].ItemID);
        pool.Dequeue((a, b, c) =>
        {
            b.transform.SetParent(component.obj.transform);
            b.transform.localScale = Vector3.one * Random.Range(0, 10);
        });
    }
    public void DelectBullet()
    {
        pool.Enqueue(pool.Use[0].ItemID);
    }
}