using UnityEngine;
using System.Collections;

namespace HotAssembly
{
    public interface LoopScrollDataSource
    {
        void ProvideData(UIItemBase item, int idx);
    }
}