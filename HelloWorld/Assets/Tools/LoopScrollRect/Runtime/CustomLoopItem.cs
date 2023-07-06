using UnityEngine;

public class CustomLoopItem
{
    public GameObject obj;
    public RectTransform rt;
    public virtual void Init(GameObject _obj)
    {
        obj = _obj;
        rt = obj.GetComponent<RectTransform>();
    }
    public virtual void SetData(int idx)
    {

    }
    public void SetActive(bool state)
    {
        if (obj.activeSelf != state) obj.SetActive(state);
    }
    public void SetParent(Transform parent)
    {
        obj.transform.SetParent(parent, false);
    }
    public void SetSiblingIndex(int idx)
    {
        obj.transform.SetSiblingIndex(idx);
    }
}