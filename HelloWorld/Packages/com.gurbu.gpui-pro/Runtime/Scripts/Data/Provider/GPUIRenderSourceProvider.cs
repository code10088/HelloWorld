// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections.Generic;
using UnityEngine;

namespace GPUInstancerPro
{
    public class GPUIRenderSourceProvider : GPUIDataProvider<int, GPUIRenderSource>
    {
        public override void Dispose()
        {
            if (_dataDict != null)
            {
                foreach (var rs in Values)
                {
                    if (rs != null)
                        rs.Dispose();
                }
            }

            base.Dispose();
        }

        internal bool TryCreateRenderSource(UnityEngine.Object source, GPUIRenderSourceGroup renderSourceGroup, out GPUIRenderSource renderSource)
        {
            renderSource = new GPUIRenderSource(source, renderSourceGroup);
            if (renderSourceGroup.AddRenderSource(renderSource))
            {
                AddOrSet(renderSource.Key, renderSource);
                GPUIRenderingSystem.Instance.OnCreatedRenderSource(renderSource);
                return true;
            }
            return false;
        }

        internal void DisposeRenderer(int renderKey)
        {
            if (TryGetData(renderKey, out GPUIRenderSource renderSource))
            {
                Remove(renderKey);
                renderSource.Dispose();
                GPUIRenderingSystem.Instance.OnRemovedRenderSource(renderKey);
            }
        }
    }
}