using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System;
using System.Collections;
using System.Collections.Generic;

namespace HotAssembly
{
    public abstract class LoopScrollRect : LoopScrollRectBase
    {
        [HideInInspector]
        [NonSerialized]
        public LoopScrollDataSource dataSource = null;
        
        protected override void ProvideData(UIItemBase item, int index)
        {
            dataSource.ProvideData(item, index);
        }
        
        protected override UIItemBase GetFromTempPool(int itemIdx)
        {
            UIItemBase nextItem = null;
            if (deletedItemTypeStart > 0)
            {
                deletedItemTypeStart--;
                nextItem = items[0];
                SetSiblingIndex(0, itemIdx - itemTypeStart + deletedItemTypeStart);
            }
            else if (deletedItemTypeEnd > 0)
            {
                deletedItemTypeEnd--;
                nextItem = items[items.Count - 1];
                SetSiblingIndex(items.Count - 1, itemIdx - itemTypeStart + deletedItemTypeStart);
            }
            else
            {
                nextItem = prefabSource.GetObject();
                nextItem.SetParent(m_Content);
                nextItem.SetActive(true);
            }
            ProvideData(nextItem, itemIdx);
            return nextItem;
        }

        protected override void ReturnToTempPool(bool fromStart, int count)
        {
            if (fromStart)
                deletedItemTypeStart += count;
            else
                deletedItemTypeEnd += count;
        }

        protected override void ClearTempPool()
        {
            Debug.Assert(items.Count >= deletedItemTypeStart + deletedItemTypeEnd);
            if (deletedItemTypeStart > 0)
            {
                for (int i = deletedItemTypeStart - 1; i >= 0; i--)
                {
                    prefabSource.ReturnObject(items[i]);
                    items.RemoveAt(i);
                }
                deletedItemTypeStart = 0;
            }
            if (deletedItemTypeEnd > 0)
            {
                int t = items.Count - deletedItemTypeEnd;
                for (int i = items.Count - 1; i >= t; i--)
                {
                    prefabSource.ReturnObject(items[i]);
                    items.RemoveAt(i);
                }
                deletedItemTypeEnd = 0;
            }
        }
    }
}