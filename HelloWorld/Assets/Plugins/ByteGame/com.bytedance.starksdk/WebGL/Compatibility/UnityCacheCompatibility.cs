#if !UNITY_WEBGL
using UnityEngine;
#endif

namespace TTSDK
{

    public class UnityCacheStorageStatus
    {
        public long maximumAvailableStorageSpace;
        public long spaceOccupied;
        public long spaceFree;
    }
    
    public static class UnityCacheCompatibility {
    
        public static bool CheckAndSetMaxStorageSpace(long cacheSizeInBytes)
        {
#if !UNITY_WEBGL
            Cache cache = Caching.currentCacheForWriting;
            if (null != cache && cache.maximumAvailableStorageSpace > cacheSizeInBytes)
            {
                cache.maximumAvailableStorageSpace = cacheSizeInBytes;
                return true;
            }
#endif
            return false;
        }
        
        public static UnityCacheStorageStatus GetStorageStatus()
        {
#if !UNITY_WEBGL
            Cache cache = Caching.currentCacheForWriting;
            if (null != cache)
            {
                return new UnityCacheStorageStatus
                {
                    maximumAvailableStorageSpace = cache.maximumAvailableStorageSpace,
                    spaceOccupied = cache.spaceOccupied,
                    spaceFree = cache.spaceFree
                };
            }
#endif
            return null;
        }
        
    }
    
}