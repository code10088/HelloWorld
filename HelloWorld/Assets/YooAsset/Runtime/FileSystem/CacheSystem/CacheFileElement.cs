using System.IO;

namespace YooAsset
{
    internal class CacheFileElement
    {
        public string PackageName { private set; get; }
        public string BundleGUID { private set; get; }
        public string FileRootPath { private set; get; }
        public string DataFilePath { private set; get; }
        public string InfoFilePath { private set; get; }

        public EFileVerifyResult Result;
        public string DataFileCRC;
        public long DataFileSize;

        public CacheFileElement(string packageName, string bundleGUID, string fileRootPath, string dataFilePath, string infoFilePath)
        {
            PackageName = packageName;
            BundleGUID = bundleGUID;
            FileRootPath = fileRootPath;
            DataFilePath = dataFilePath;
            InfoFilePath = infoFilePath;
        }

        public void DeleteFiles()
        {
            try
            {
                Directory.Delete(FileRootPath, true);
            }
            catch (System.Exception e)
            {
                YooLogger.Warning($"Failed to delete cache bundle folder : {e}");
            }
        }
    }
}