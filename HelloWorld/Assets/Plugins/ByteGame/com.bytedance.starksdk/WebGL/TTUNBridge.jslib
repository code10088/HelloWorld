mergeInto(LibraryManager.library, {
    unityCallJs: function(msg) {
        if (typeof(UNBridgeCore) === "undefined") {
            return;
        }
        UNBridgeCore.handleMsgFromUnity(_StarkPointerStringify(msg));
    },
    unityCallJsSync: function(msg) {
        if (typeof(UNBridgeCore) === "undefined") {
            return;
        }
        var result = UNBridgeCore.handleMsgFromUnitySync(_StarkPointerStringify(msg));
        var bufferSize = lengthBytesUTF8(result) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(result, buffer, bufferSize);
        return buffer;
    },
    h5HasAPI: function(apiName) {
        if (typeof(UNBridgeCore) === "undefined") {
            return;
        }
        return UNBridge.h5HasAPI(_StarkPointerStringify(apiName));
    },
    unityMixCallJs: function(msg) {
        if (typeof(UNBridgeCore) === "undefined") {
            return;
        }
        var result = UNBridgeCore.onUnityMixCall(_StarkPointerStringify(msg));
        var bufferSize = lengthBytesUTF8(result) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(result, buffer, bufferSize);
        return buffer;
    }
});