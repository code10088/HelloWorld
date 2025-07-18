using UnityEngine;
using UnityEngine.UI;

public class UIImage : Image
{
    private int loadId = -1;
    public int LoadId => loadId;
    public void SetImage(string path)
    {
        AssetManager.Instance.Load<Sprite>(ref loadId, path, LoadFinish);
    }
    private void LoadFinish(int loadId, Object asset)
    {
        if (asset) sprite = (Sprite)asset;
    }
}
