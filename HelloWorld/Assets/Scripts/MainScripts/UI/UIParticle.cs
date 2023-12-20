using UnityEngine;
using UnityEngine.UI;

public class UIParticle : MonoBehaviour
{
    private ParticleSystemRenderer[] ps;
    private int[] layerRecord;
    private string sortingLayerName;
    private int sortingOrder;

    //²Ã¼ôÌØÐ§RectTransform.Scale.z=0
    private RectTransform rt;
    private MaterialPropertyBlock mpb;
    private void Awake()
    {
        Init();
    }
    private void OnEnable()
    {
        Refresh();
    }
    private void Init()
    {
        ps = GetComponentsInChildren<ParticleSystemRenderer>(true);
        layerRecord = new int[ps.Length];
        for (int i = 0; i < ps.Length; i++)
        {
            if (ps[i].sortingOrder < 10) layerRecord[i] = ps[i].sortingOrder;
        }
        var sr = GetComponentInParent<ScrollRect>();
        if (sr)
        {
            rt = sr.GetComponent<RectTransform>();
            mpb = new MaterialPropertyBlock();
        }
    }
    public void Refresh()
    {
        Canvas c = GetComponentInParent<Canvas>();
        if (c.sortingLayerName == sortingLayerName && c.sortingOrder == sortingOrder) return;
        sortingLayerName = c.sortingLayerName;
        sortingOrder = c.sortingOrder;
        for (int i = 0; i < ps.Length; i++)
        {
            ps[i].sortingLayerName = sortingLayerName;
            ps[i].sortingOrder = sortingOrder + layerRecord[i];
        }
        if (rt)
        {
            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            float minX = corners[0].x;
            float minY = corners[0].y;
            float maxX = corners[2].x;
            float maxY = corners[2].y;
            for (int i = 0; i < ps.Length; i++)
            {
                mpb.Clear();
                ps[i].GetPropertyBlock(mpb);
                mpb.SetFloat("_MinX", minX);
                mpb.SetFloat("_MinY", minY);
                mpb.SetFloat("_MaxX", maxX);
                mpb.SetFloat("_MaxY", maxY);
                ps[i].SetPropertyBlock(mpb);
            }
        }
    }
}
