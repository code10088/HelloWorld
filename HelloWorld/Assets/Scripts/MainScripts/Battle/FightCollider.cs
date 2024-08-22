using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class FightCollider : MonoBehaviour
{
    private static Dictionary<int, FightCollider> dic = new Dictionary<int, FightCollider>();
    public static int Find(int code)
    {
        if (dic.TryGetValue(code, out FightCollider collider)) return collider.id;
        else return 0;
    }
    public static void Clear()
    {
        dic.Clear();
    }

    private int id;
    private void Awake()
    {
        var collider = GetComponent<Collider2D>();
        int code = collider.GetHashCode();
        dic.Add(code, this);
    }
    public void Init(int id)
    {
        this.id = id;
    }
}
