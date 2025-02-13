mergeInto(LibraryManager.library, {
    StarkAbfsCheckReady: function() {
        if (!window.StarkSDK || !window.StarkSDK.abfs)
            return false;
        return window.StarkSDK.abfs.checkReady();
    },
    StarkAbfsRegisterAssetBundleUrl: function(ptr) {
        if (!window.StarkSDK)
            return;

        var url = UTF8ToString(ptr);
        window.StarkSDK.abfs.registerAssetBundleUrl(url);
    },
    StarkAbfsUnregisterAssetBundleUrl: function(ptr) {
        if (!window.StarkSDK)
            return;

        var url = UTF8ToString(ptr);
        window.StarkSDK.abfs.unregisterAssetBundleUrl(url);
    },
    StarkAbfsFetchBundleFromXHR: function(url, id, callback, needRetry) {
        if (!window.StarkSDK)
            return false;

        var _url = UTF8ToString(url);
        var _id = UTF8ToString(id);
        var _callback = function(code, message) {

            var len = lengthBytesUTF8(_id) + 1;
            var idPtr = _malloc(len);
            stringToUTF8(_id, idPtr, len);
            
            dynCall("viii", callback, [idPtr, code, 0]);
        }
        window.StarkSDK.abfs.fetchBundleFromXHR(_url, _id, _callback, needRetry);
    }
});