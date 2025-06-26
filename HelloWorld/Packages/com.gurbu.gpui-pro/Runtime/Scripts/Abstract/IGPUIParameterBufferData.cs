// GPU Instancer Pro
// Copyright (c) GurBu Technologies

namespace GPUInstancerPro
{
    public interface IGPUIParameterBufferData
    {
        public void SetParameterBufferData();
        public bool TryGetParameterBufferIndex(out int index);
    }
}
