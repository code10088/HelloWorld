using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoroutineManager : MonoSingleton<CoroutineManager>
{
    private int uniqueId = 0;
    private Dictionary<int, Coroutine> coroutines = new Dictionary<int, Coroutine>();
    public int StartCoroutinea(IEnumerator a)
    {
        coroutines[++uniqueId] = StartCoroutine(a);
        return uniqueId;
    }
    public void StopCoroutinea(int id)
    {
        Coroutine c;
        if (coroutines.TryGetValue(id, out c))
        {
            StopCoroutine(c);
            coroutines.Remove(id);
        }
    }
}