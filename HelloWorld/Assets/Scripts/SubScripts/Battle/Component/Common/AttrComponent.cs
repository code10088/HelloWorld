using cfg;
using System.Collections.Generic;

public class AttrComponent : ECS_Component
{
    private Dictionary<int, float> attrs = new Dictionary<int, float>();

    public float GetAttr(AttrEnum k)
    {
        return GetAttr((int)k);
    }
    public float GetAttr(int k)
    {
        float f = 0;
        attrs.TryGetValue(k, out f);
        return f;
    }
    public void SetAttr(AttrEnum k, float v)
    {
        SetAttr((int)k, v);
    }
    public void SetAttr(int k, float v)
    {
        float f = 0;
        attrs.TryGetValue(k, out f);
        float result = f + v;
        attrs[k] = result;
    }
    public AttrComponent CopyAttr()
    {
        AttrComponent result = new AttrComponent();
        foreach (var item in attrs) result.SetAttr(item.Key, item.Value);
        return result;
    }
    public void Clear()
    {
        attrs.Clear();
    }
}