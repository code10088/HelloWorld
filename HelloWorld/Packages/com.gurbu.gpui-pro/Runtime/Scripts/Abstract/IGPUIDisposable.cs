// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;

namespace GPUInstancerPro
{
    public interface IGPUIDisposable : IDisposable
    {
        void ReleaseBuffers();
    }
}
