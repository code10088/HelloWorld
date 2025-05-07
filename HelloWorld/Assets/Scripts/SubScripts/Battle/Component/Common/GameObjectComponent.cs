using System;
using UnityEngine;

public class GameObjectComponent : ECS_Component
{
    private int itemId;
    private string path;
    private Transform parent;
    private GameObject obj;
    private Action finish;

    public GameObject Obj => obj;

    public void Init(string path, Transform parent, Action finish)
    {
        if (itemId > 0) Clear();
        if (string.IsNullOrEmpty(path)) return;
        this.path = path;
        this.parent = parent;
        this.finish = finish;
        itemId = BattleManager.Instance.Pool.Dequeue(path, LoadFinish);
    }
    protected void LoadFinish(int itemId, GameObject obj, object[] param)
    {
        this.obj = obj;
        obj.transform.SetParent(parent);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;
        obj.transform.localScale = Vector3.one;
        finish?.Invoke();
    }
    public void Clear()
    {
        if (string.IsNullOrEmpty(path)) return;
        BattleManager.Instance.Pool.Enqueue(path, itemId);
        obj = null;
    }
}