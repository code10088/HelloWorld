using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PieceCollider : MonoBehaviour
{
    private static Dictionary<int, PieceCollider> dic = new Dictionary<int, PieceCollider>();
    public static int Find(int code)
    {
        if (dic.TryGetValue(code, out PieceCollider collider)) return collider.id;
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
