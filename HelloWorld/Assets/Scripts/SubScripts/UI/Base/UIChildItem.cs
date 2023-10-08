using UnityEngine;

namespace HotAssembly
{
    public interface UIChildItem
    {
        void Init(GameObject obj);
        void SetData(int index);
    }
}