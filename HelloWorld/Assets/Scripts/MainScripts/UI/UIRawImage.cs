using UnityEngine;
using UnityEngine.UI;

public class UIRawImage : RawImage
{
    private int loadId = -1;
    public int LoadId => loadId;
    public void SetImage(string path)
    {
        AssetManager.Instance.Load<Texture>(ref loadId, path, LoadFinish);
    }
    private void LoadFinish(int loadId, Object asset)
    {
        if (asset) texture = (Texture)asset;
    }
    public void Clear()
    {
        AssetManager.Instance.Unload(ref loadId);
        texture = null;
    }
}
