// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System.Collections.Generic;
using UnityEngine;

namespace GPUInstancerPro
{
    /// <summary>
    /// Provides property description for Material Property Blocks
    /// </summary>
    public abstract class GPUIMPBDescription : ScriptableObject
    {
        public Shader shader;
        public abstract void SetMPBValues(GPUIRenderSourceGroup rsg, GPUIManager manager, int prototypeIndex);
    }
}
