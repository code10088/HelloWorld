using UnityEngine;
using System.Collections.Generic;

namespace HotAssembly
{
    public class CustomLoopScrollSource<T> : LoopScrollPrefabSource, LoopScrollDataSource where T : UIItemBase, new()
    {
        private LoopScrollRect lsr;
        private GameObject item;
        private Stack<T> pool = new Stack<T>();

        public void Init(LoopScrollRect _lsr, GameObject _item, int totalCount = -1)
        {
            lsr = _lsr;
            item = _item;
            lsr.prefabSource = this;
            lsr.dataSource = this;
            RefillCells(totalCount);
        }
        public void Clear()
        {
            lsr.ClearCells();
            while (pool.Count > 0)
            {
                var temp = pool.Pop();
                GameObject.Destroy(temp.obj);
            }
        }
        public void RefreshCells()
        {
            lsr.RefreshCells();
        }
        public void RefillCells(int count)
        {
            lsr.totalCount = count;
            lsr.RefillCells();
        }

        public UIItemBase GetObject()
        {
            T target;
            if (pool.Count > 0)
            {
                target = pool.Pop();
                target.obj.SetActive(true);
            }
            else
            {
                target = new T();
                var tempObj = GameObject.Instantiate(item);
                target.Init(tempObj);
            }
            target.SetActive(true);
            return target;
        }
        public void ReturnObject(UIItemBase item)
        {
            item.SetActive(false);
            item.SetParent(lsr.transform);
            pool.Push(item as T);
        }
        public void ProvideData(UIItemBase item, int idx)
        {
            item.SetData(idx);
        }
    }
}