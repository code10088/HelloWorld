using UnityEngine;

public class UIGuideCollect : MonoBehaviour
{
    public int[] guideID;

    private void OnEnable()
    {
        for (int i = 0; i < guideID.Length; i++)
        {
            DataManager.Instance.GuideData.AddGuideT(guideID[i], transform);
        }
    }
    private void OnDisable()
    {
        for (int i = 0; i < guideID.Length; i++)
        {
            DataManager.Instance.GuideData.RemoveGuideT(guideID[i]);
        }
    }
}