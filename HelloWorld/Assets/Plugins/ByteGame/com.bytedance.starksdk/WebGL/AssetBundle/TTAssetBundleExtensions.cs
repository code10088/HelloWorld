using UnityEngine;

namespace TTSDK
{
    public static class TTAssetBundleExtensions
    {
        public static void TTUnload(this AssetBundle ab, bool unloadAllLoadedObjects)
        {
            ab.Unload(unloadAllLoadedObjects);
            
            if (!TTAssetBundle.isAbfsReady)
                return;
            
            if (TTAssetBundle.bundle2path.TryGetValue(ab, out var path))
            {
                TTAssetBundle.UnregisterAssetBundleUrl(path);
                TTAssetBundle.bundle2path.Remove(ab);
            }
            else
            {
                Debug.LogError("AssetBundle.TTUnload() Failure: Unregistered Asset Bundle. Loaded without TTAssetBundle methods?");
            }
        }
    }
}