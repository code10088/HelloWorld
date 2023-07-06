using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine.UI
{
    public abstract class LoopScrollRectMulti : LoopScrollRectBase
    {
        [HideInInspector]
        [NonSerialized]
        public LoopScrollDataSource dataSource = null;
        
        protected override void ProvideData(CustomLoopItem transform, int index)
        {
            dataSource.ProvideData(transform, index);
        }
        
        // Multi Data Source cannot support TempPool
        protected override CustomLoopItem GetFromTempPool(int itemIdx)
        {
            CustomLoopItem nextItem = prefabSource.GetObject();
            nextItem.SetParent(m_Content);
            nextItem.SetActive(true);

            ProvideData(nextItem, itemIdx);
            return nextItem;
        }

        protected override void ReturnToTempPool(bool fromStart, int count)
        {
            Debug.Assert(items.Count >= count);
            if (fromStart)
            {
                for (int i = count - 1; i >= 0; i--)
                {
                    prefabSource.ReturnObject(items[i]);
                    items.RemoveAt(i);
                }
            }
            else
            {
                int t = items.Count - count;
                for (int i = items.Count - 1; i >= t; i--)
                {
                    prefabSource.ReturnObject(items[i]);
                    items.RemoveAt(i);
                }
            }
        }

        protected override void ClearTempPool()
        {
        }
    }
}