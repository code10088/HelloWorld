using System.Collections.Generic;
using UnityEngine;

namespace HotAssembly
{
    public enum PieceAttrEnum
    {
        Hp,
        Mp,
        Atk,
        Def,
        MoveSpeed,
    }
    public class PieceAttr
    {
        private Dictionary<PieceAttrEnum, float> attrs = new Dictionary<PieceAttrEnum, float>();

        public float GetAttr(PieceAttrEnum k)
        {
            float f = 0;
            attrs.TryGetValue(k, out f);
            switch (k)
            {
                case PieceAttrEnum.Hp:
                    return f;
                default:
                    return f;
            }
        }
        public void SetAttr(PieceAttrEnum k, float v)
        {
            float f = 0;
            attrs.TryGetValue(k, out f);
            float result = f + v;
            attrs[k] = result;
            switch (k)
            {
                case PieceAttrEnum.Hp:

                    break;
            }
        }
        public PieceAttr CopyAttr()
        {
            PieceAttr result = new PieceAttr();
            foreach (var item in attrs) result.SetAttr(item.Key, item.Value);
            return result;
        }
    }
}