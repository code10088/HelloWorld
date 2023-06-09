using UnityEngine;
using System.Collections;

namespace HotAssembly
{
    public interface LoopScrollPrefabSource
    {
        UIItemBase GetObject();

        void ReturnObject(UIItemBase trans);
    }
}
