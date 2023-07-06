using UnityEngine;
using System.Collections;

namespace UnityEngine.UI
{
    public interface LoopScrollDataSource
    {
        void ProvideData(CustomLoopItem item, int idx);
    }
}