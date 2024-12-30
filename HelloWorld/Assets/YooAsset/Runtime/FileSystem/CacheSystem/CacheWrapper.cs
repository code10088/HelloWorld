
namespace YooAsset
{
    internal class CacheWrapper
    {
        public string InfoFilePath { private set; get; }
        public string DataFilePath { private set; get; }
        public string DataFileCRC { private set; get; }
        public long DataFileSize { private set; get; }
        
        public CacheWrapper(string infoFilePath, string dataFilePath, string dataFileCRC, long dataFileSize)
        {
            InfoFilePath = infoFilePath;
            DataFilePath = dataFilePath;
            DataFileCRC = dataFileCRC;
            DataFileSize = dataFileSize;
        }
    }
}