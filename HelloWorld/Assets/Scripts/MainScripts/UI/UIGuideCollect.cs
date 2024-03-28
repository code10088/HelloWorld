using UnityEngine;

public class UIGuideCollect : MonoBehaviour
{
    public delegate void AddGuideT(int id, Transform t);
    public delegate void RemoveGuideT(int id);
    public static AddGuideT Add;
    public static RemoveGuideT Remove;
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