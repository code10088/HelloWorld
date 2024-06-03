using System.Collections.Generic;
using UnityEngine;

namespace HotAssembly
{
    public enum MonsterAttrEnum
    {
        Hp,
        Mp,
        Atk,
        Def,
    }
    public class PieceAttr
    {
        private Dictionary<MonsterAttrEnum, float> attrs = new Dictionary<MonsterAttrEnum, float>();

        public float GetAttr(MonsterAttrEnum k)
        {
            float f = 0;
            attrs.TryGetValue(k, out f);
            switch (k)
            {
                case MonsterAttrEnum.Hp:
                    return f;
                default:
                    return f;
            }
        }
        public void SetAttr(MonsterAttrEnum k, float v)
        {
            float f = 0;
            attrs.TryGetValue(k, out f);
            float result = f + v;
            attrs[k] = result;
            switch (k)
            {
                case MonsterAttrEnum.Hp:

                    break;
            }
        }
    }
}