using System;
using UnityEngine;

public class UIGuideCollect : MonoBehaviour
{
    public static Action<int, Transform> Add;
    public static Action<int> Remove;
    public int[] guideID;

    private void OnEnable()
    {
        for (int i = 0; i < guideID.Length; i++)
        {
            Add?.Invoke(guideID[i], transform);
        }
    }
    private void OnDisable()
    {
        for (int i = 0; i < guideID.Length; i++)
        {
            Remove?.Invoke(guideID[i]);
        }
    }
}