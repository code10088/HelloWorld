using cfg;
using System.Collections.Generic;

namespace HotAssembly
{
    public class PieceAttr
    {
        private Dictionary<int, float> attrs = new Dictionary<int, float>();

        public float GetAttr(PieceAttrEnum k)
        {
            return GetAttr((int)k);
        }
        public float GetAttr(int k)
        {
            float f = 0;
            attrs.TryGetValue(k, out f);
            return f;
        }
        public void SetAttr(PieceAttrEnum k, float v)
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
        public PieceAttr CopyAttr()
        {
            PieceAttr result = new PieceAttr();
            foreach (var item in attrs) result.SetAttr(item.Key, item.Value);
            return result;
        }
        public void Clear()
        {
            attrs.Clear();
        }
    }
}