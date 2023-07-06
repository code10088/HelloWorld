using UnityEngine;
using System.Collections;

namespace UnityEngine.UI
{
    public interface LoopScrollPrefabSource
    {
        CustomLoopItem GetObject();

        void ReturnObject(CustomLoopItem trans);
    }
}
