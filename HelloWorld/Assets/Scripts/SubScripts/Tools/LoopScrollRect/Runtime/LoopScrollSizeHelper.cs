using UnityEngine;
using System.Collections;

namespace HotAssembly
{
    // optional class for better scroll support
    public interface LoopScrollSizeHelper
    {
        Vector2 GetItemsSize(int itemsCount);
    }
}
