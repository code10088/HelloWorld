mergeInto(LibraryManager.library, {
	StarkPointerStringify: function (str) {
		if (typeof UTF8ToString !== "undefined") {
			return UTF8ToString(str)
		}
		return Pointer_stringify(str)
	},
	StarkMigratingData: function () {
		if (!window.StarkSDK) return false;
		return window.StarkSDK.MigratingData();
	},
	StarkIsDataMigrated: function () {
		if (!window.StarkSDK) return false;
		return window.StarkSDK.IsDataMigrated();
	},
	StarkCanUseLocalStorage: function () {
		if (!window.StarkSDK) return false;
		return window.StarkSDK.CanUseLocalStorage();
	},
	StarkStorageSetIntSync: function (key, value) {
		window.StarkSDK && window.StarkSDK.SetStorageSync(_StarkPointerStringify(key), value);
	},
	StarkStorageGetIntSync: function (key, defaultValue) {
		if (!window.StarkSDK) return defaultValue;
		return window.StarkSDK.GetStorageSync(_StarkPointerStringify(key), defaultValue);
	},
	StarkStorageSetFloatSync: function (key, value) {
		window.StarkSDK && window.StarkSDK.SetStorageSync(_StarkPointerStringify(key), value);
	},
	StarkStorageGetFloatSync: function (key, defaultValue) {
		if (!window.StarkSDK) return defaultValue;
		return window.StarkSDK.GetStorageSync(_StarkPointerStringify(key), defaultValue);
	},
	StarkStorageSetStringSync: function (key, value) {
		window.StarkSDK && window.StarkSDK.SetStorageSync(_StarkPointerStringify(key), _StarkPointerStringify(value));
	},
	StarkStorageGetStringSync: function (key, defaultValue) {
		if (!window.StarkSDK) return defaultValue;
		var returnStr = window.StarkSDK.GetStorageSync(_StarkPointerStringify(key), _StarkPointerStringify(defaultValue));
		var bufferSize = lengthBytesUTF8(returnStr) + 1;
		var buffer = _malloc(bufferSize);
		stringToUTF8(returnStr, buffer, bufferSize);
		return buffer;
	},
	StarkStorageDeleteAllSync: function () {
		window.StarkSDK && window.StarkSDK.ClearStorageSync();
	},
	StarkStorageDeleteKeySync: function (key) {
		window.StarkSDK && window.StarkSDK.RemoveStorageSync(_StarkPointerStringify(key));
	},
	StarkStorageHasKeySync: function (key) {
		if (!window.StarkSDK) return;
		return window.StarkSDK.StorageHasKeySync(_StarkPointerStringify(key));
	},
	StarkWriteBinFile: function (filePath, data, dataLength, s, f) {
		window.StarkSDK && window.StarkSDK.WriteFile(
			_StarkPointerStringify(filePath),
			HEAPU8.slice(data, dataLength + data),
			"binary",
			_StarkPointerStringify(s),
			_StarkPointerStringify(f)
		)
	},
	StarkWriteBinFileSync: function (filePath, data, dataLength) {
		if (!window.StarkSDK) return;
		var returnStr = window.StarkSDK.WriteFileSync(
			_StarkPointerStringify(filePath),
			HEAPU8.slice(data, dataLength + data),
			"binary"
		)
		if (!returnStr) {
			return;
		}
		var bufferSize = lengthBytesUTF8(returnStr) + 1;
		var buffer = _malloc(bufferSize);
		stringToUTF8(returnStr, buffer, bufferSize);
		return buffer;
	},
	StarkWriteStringFile: function (filePath, data, encoding, s, f) {
		window.StarkSDK && window.StarkSDK.WriteFile(
			_StarkPointerStringify(filePath),
			_StarkPointerStringify(data),
			_StarkPointerStringify(encoding),
			_StarkPointerStringify(s),
			_StarkPointerStringify(f)
		)
	},
	StarkWriteStringFileSync: function (filePath, data, encoding) {
		if (!window.StarkSDK) return;
		var returnStr = window.StarkSDK.WriteFileSync(_StarkPointerStringify(filePath), _StarkPointerStringify(data), _StarkPointerStringify(encoding));
		if (!returnStr) {
			return;
		}
		var bufferSize = lengthBytesUTF8(returnStr) + 1;
		var buffer = _malloc(bufferSize);
		stringToUTF8(returnStr, buffer, bufferSize);
		return buffer;
	},
	StarkReadFile: function (filePath, encoding, callbackId) {
		window.StarkSDK && window.StarkSDK.ReadFile(
			_StarkPointerStringify(filePath),
			_StarkPointerStringify(encoding),
			_StarkPointerStringify(callbackId)
		);
	},
	StarkReadStringFileSync: function (filePath, encoding) {
		if (!window.StarkSDK) return;
		var returnStr = window.StarkSDK.ReadStringFileSync(_StarkPointerStringify(filePath), _StarkPointerStringify(encoding));
		if (!returnStr) {
			return;
		}
		var bufferSize = lengthBytesUTF8(returnStr) + 1;
		var buffer = _malloc(bufferSize);
		stringToUTF8(returnStr, buffer, bufferSize);
		return buffer;
	},
	StarkReadBinFileSync: function (filePath) {
		if (!window.StarkSDK) return;
		return window.StarkSDK.ReadBinFileSync(
			_StarkPointerStringify(filePath)
		);
	},
	StarkShareFileBuffer: function (offset, callbackId) {
		window.StarkSDK && window.StarkSDK.ShareFileBuffer(
			HEAPU8,
			offset,
			_StarkPointerStringify(callbackId)
		)
	},
	StarkAccessFileSync: function (path) {
		if (!window.StarkSDK) return false;
		return window.StarkSDK.AccessFileSync(_StarkPointerStringify(path));
	},
	StarkAccessFile: function (path, s, f) {
		window.StarkSDK && window.StarkSDK.AccessFile(
			_StarkPointerStringify(path),
			_StarkPointerStringify(s),
			_StarkPointerStringify(f));
	},
	StarkCopyFileSync: function (srcPath, destPath) {
		if (!window.StarkSDK) return;
		var returnStr = window.StarkSDK.CopyFileSync(_StarkPointerStringify(srcPath), _StarkPointerStringify(destPath));
		if (!returnStr) {
			return;
		}
		var bufferSize = lengthBytesUTF8(returnStr) + 1;
		var buffer = _malloc(bufferSize);
		stringToUTF8(returnStr, buffer, bufferSize);
		return buffer;
	},
	StarkCopyFile: function (srcPath, destPath, s, f) {
		window.StarkSDK && window.StarkSDK.CopyFile(
			_StarkPointerStringify(srcPath),
			_StarkPointerStringify(destPath),
			_StarkPointerStringify(s),
			_StarkPointerStringify(f));
	},
	StarkRenameFileSync: function (srcPath, destPath) {
		if (!window.StarkSDK) return;
		var returnStr = window.StarkSDK.RenameFileSync(_StarkPointerStringify(srcPath), _StarkPointerStringify(destPath));
		if (!returnStr) {
			return;
		}
		var bufferSize = lengthBytesUTF8(returnStr) + 1;
		var buffer = _malloc(bufferSize);
		stringToUTF8(returnStr, buffer, bufferSize);
		return buffer;
	},
	StarkRenameFile: function (srcPath, destPath, s, f) {
		window.StarkSDK && window.StarkSDK.RenameFile(
			_StarkPointerStringify(srcPath),
			_StarkPointerStringify(destPath),
			_StarkPointerStringify(s),
			_StarkPointerStringify(f));
	},
	StarkUnlinkSync: function (filePath) {
		if (!window.StarkSDK) return;
		var returnStr = window.StarkSDK.UnlinkSync(_StarkPointerStringify(filePath));
		if (!returnStr) {
			return;
		}
		var bufferSize = lengthBytesUTF8(returnStr) + 1;
		var buffer = _malloc(bufferSize);
		stringToUTF8(returnStr, buffer, bufferSize);
		return buffer;
	},
	StarkUnlink: function (filePath, s, f) {
		window.StarkSDK && window.StarkSDK.Unlink(
			_StarkPointerStringify(filePath),
			_StarkPointerStringify(s),
			_StarkPointerStringify(f));
	},
	StarkMkdir: function (dirPath, recursive, s, f) {
		window.StarkSDK && window.StarkSDK.Mkdir(
			_StarkPointerStringify(dirPath),
			recursive,
			_StarkPointerStringify(s),
			_StarkPointerStringify(f));
	},
	StarkMkdirSync: function (dirPath, recursive) {
		if (!window.StarkSDK) return;
		var returnStr = window.StarkSDK.MkdirSync(_StarkPointerStringify(dirPath), recursive);
		if (!returnStr) {
			return;
		}
		var bufferSize = lengthBytesUTF8(returnStr) + 1;
		var buffer = _malloc(bufferSize);
		stringToUTF8(returnStr, buffer, bufferSize);
		return buffer;
	},
	StarkRmdir: function (dirPath, recursive, s, f) {
		window.StarkSDK && window.StarkSDK.Rmdir(
			_StarkPointerStringify(dirPath),
			recursive,
			_StarkPointerStringify(s),
			_StarkPointerStringify(f));
	},
	StarkRmdirSync: function (dirPath, recursive) {
		if (!window.StarkSDK) return;
		var returnStr = window.StarkSDK.RmdirSync(_StarkPointerStringify(dirPath), recursive);
		if (!returnStr) {
			return;
		}
		var bufferSize = lengthBytesUTF8(returnStr) + 1;
		var buffer = _malloc(bufferSize);
		stringToUTF8(returnStr, buffer, bufferSize);
		return buffer;
	},
	StarkStatSync: function (path) {
		if (!window.StarkSDK) return;
		var returnStr = window.StarkSDK.StatSync(_StarkPointerStringify(path));
		if (!returnStr) {
			return;
		}
		var bufferSize = lengthBytesUTF8(returnStr) + 1;
		var buffer = _malloc(bufferSize);
		stringToUTF8(returnStr, buffer, bufferSize);
		return buffer;
	},
	StarkStat: function (path, callbackId) {
		window.StarkSDK && window.StarkSDK.Stat(
			_StarkPointerStringify(path),
			_StarkPointerStringify(callbackId));
	},
	StarkGetSavedFileList: function (callbackId) {
		window.StarkSDK && window.StarkSDK.GetSavedFileList(
			_StarkPointerStringify(callbackId));
	},
	StarkAppendBinFile: function(filePath, data, dataLength, s, f) {
        window.StarkSDK && window.StarkSDK.AppendFile(
            _StarkPointerStringify(filePath),
            HEAPU8.slice(data, dataLength + data),
            "binary",
            _StarkPointerStringify(s),
            _StarkPointerStringify(f)
        )
    },
    StarkAppendBinFileSync: function(filePath, data, dataLength) {
        if (!window.StarkSDK) return;
        var returnStr = window.StarkSDK.AppendFileSync(
            _StarkPointerStringify(filePath),
            HEAPU8.slice(data, dataLength + data),
            "binary"
        )
        if (!returnStr) {
            return;
        }
        var bufferSize = lengthBytesUTF8(returnStr) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(returnStr, buffer, bufferSize);
        return buffer;
    },
    StarkAppendStringFile: function(filePath, data, encoding, s, f) {
        window.StarkSDK && window.StarkSDK.AppendFile(
            _StarkPointerStringify(filePath),
            _StarkPointerStringify(data),
            _StarkPointerStringify(encoding),
            _StarkPointerStringify(s),
            _StarkPointerStringify(f));
    },
    StarkAppendStringFileSync: function(filePath, data, encoding) {
        if (!window.StarkSDK) return;
        var returnStr = window.StarkSDK.AppendFileSync(
            _StarkPointerStringify(filePath),
            _StarkPointerStringify(data),
            _StarkPointerStringify(encoding));
        if (!returnStr) {
            return;
        }
        var bufferSize = lengthBytesUTF8(returnStr) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(returnStr, buffer, bufferSize);
        return buffer;      
    },
    StarkRemoveSavedFile: function(filePath, s, f) {
        if (!window.StarkSDK) return;   
        window.StarkSDK && window.StarkSDK.RemoveSavedFile(
            _StarkPointerStringify(filePath),
            _StarkPointerStringify(s),
            _StarkPointerStringify(f));
    },
    StarkReadDir: function(filePath, s, f) {
        if (!window.StarkSDK) return;
        window.StarkSDK && window.StarkSDK.ReadDir(
            _StarkPointerStringify(filePath),
            _StarkPointerStringify(s),
            _StarkPointerStringify(f));
    },
    StarkReadDirSync: function(filePath) {
        if (!window.StarkSDK) return;
        var returnStr = window.StarkSDK.ReadDirSync(
            _StarkPointerStringify(filePath));
        if (!returnStr) {
            return;
        }
        var bufferSize = lengthBytesUTF8(returnStr) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(returnStr, buffer, bufferSize);
        return buffer;
    },
    StarkTruncate: function(filePath, length, s, f) {
        if (!window.StarkSDK) return;
        window.StarkSDK && window.StarkSDK.Truncate(
            _StarkPointerStringify(filePath),
            length,
            _StarkPointerStringify(s),
            _StarkPointerStringify(f));
    },
    StarkTruncateSync: function(filePath, length) {
        if (!window.StarkSDK) return;
        var returnStr = window.StarkSDK.TruncateSync(
            _StarkPointerStringify(filePath),
            length);
        if (!returnStr) {
            return;
        }
        var bufferSize = lengthBytesUTF8(returnStr) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(returnStr, buffer, bufferSize);
        return buffer;
    },
	StarkGetCachedPathForUrl: function (url) {
		if (!window.StarkSDK) return;
		var returnStr = window.StarkSDK.getCachedPathForUrl(_StarkPointerStringify(url));
		if (!returnStr) {
			return;
		}
		var bufferSize = lengthBytesUTF8(returnStr) + 1;
		var buffer = _malloc(bufferSize);
		stringToUTF8(returnStr, buffer, bufferSize);
		return buffer;
	},
	StarkCreateUDPSocket: function () {
		if (!window.StarkSDK) return;
		var res = window.StarkSDK.CreateUDPSocket();
		var bufferSize = lengthBytesUTF8(res || '') + 1;
		var buffer = _malloc(bufferSize);
		stringToUTF8(res, buffer, bufferSize);
		return buffer;
	},
	StarkUDPSocketClose: function (id) {
		if (!window.StarkSDK) return;
		window.StarkSDK.UDPSocketClose(_StarkPointerStringify(id));
	},
	StarkUDPSocketConnect: function (id, option) {
		if (!window.StarkSDK) return;
		window.StarkSDK.UDPSocketConnect(_StarkPointerStringify(id), _StarkPointerStringify(option));
	},
	StarkUDPSocketOffClose: function (id) {
		if (!window.StarkSDK) return;
		window.StarkSDK.UDPSocketOffClose(_StarkPointerStringify(id));
	},
	StarkUDPSocketOffError: function (id) {
		if (!window.StarkSDK) return;
		window.StarkSDK.UDPSocketOffError(_StarkPointerStringify(id));
	},
	StarkUDPSocketOffListening: function (id) {
		if (!window.StarkSDK) return;
		window.StarkSDK.UDPSocketOffListening(_StarkPointerStringify(id));
	},
	StarkUDPSocketOffMessage: function (id) {
		if (!window.StarkSDK) return;
		window.StarkSDK.UDPSocketOffMessage(_StarkPointerStringify(id));
	},
	StarkUDPSocketOnClose: function (id) {
		if (!window.StarkSDK) return;
		window.StarkSDK.UDPSocketOnClose(_StarkPointerStringify(id));
	},
	StarkUDPSocketOnError: function (id) {
		if (!window.StarkSDK) return;
		window.StarkSDK.UDPSocketOnError(_StarkPointerStringify(id));
	},
	StarkUDPSocketOnListening: function (id) {
		if (!window.StarkSDK) return;
		window.StarkSDK.UDPSocketOnListening(_StarkPointerStringify(id));
	},
	StarkUDPSocketOnMessage: function (id, needInfo) {
		if (!window.StarkSDK) return;
		window.StarkSDK.UDPSocketOnMessage(_StarkPointerStringify(id), needInfo);
	},
	StarkUDPSocketSendString: function (id, data, param) {
		if (!window.StarkSDK) return;
		window.StarkSDK.UDPSocketSendString(_StarkPointerStringify(id), _StarkPointerStringify(data), _StarkPointerStringify(param));
	},
	StarkUDPSocketSendBuffer: function (id, dataPtr, dataLength, param) {
		if (!window.StarkSDK) return;
		window.StarkSDK.UDPSocketSendBuffer(_StarkPointerStringify(id), dataPtr, dataLength, _StarkPointerStringify(param));
	},
	StarkUDPSocketSetTTL: function (id, ttl) {
		if (!window.StarkSDK) return;
		window.StarkSDK.UDPSocketSetTTL(_StarkPointerStringify(id), ttl);
	},
	StarkUDPSocketBind: function (id, port) {
		if (!window.StarkSDK) return;
		var res = window.StarkSDK.UDPSocketBind(_StarkPointerStringify(id), _StarkPointerStringify(port));
		return res;
	},
	StarkUDPSocketDestroy: function (id) {
		if (!window.StarkSDK) return;
		window.StarkSDK.UDPSocketDestroy(_StarkPointerStringify(id));
	},
	StarkRegisterUDPSocketOnMessageCallback: function (callback) {
		if (!window.StarkSDK) return;
		window.StarkSDK.RegisterUDPSocketOnMessageCallback(callback);
	},
	StarkGetSystemFont: function (onGetFontData) {
		window.StarkSDK && window.StarkSDK.GetSystemFont((data, length) => {
			if (!length || !data) {
			    Runtime.dynCall('vii', onGetFontData, [null, 0]);
			} else {
				var dataSize = data.byteLength;
				var dataPtr;
				try {
					dataPtr = _malloc(dataSize);
					if (!dataPtr) {
						throw new Error("Failed to allocate memory for font");
					}
					var dataHeap = new Uint8Array(HEAPU8.buffer, dataPtr, dataSize);
					dataHeap.set(new Uint8Array(data));
					Runtime.dynCall('vii', onGetFontData, [dataPtr, dataSize]);
				} catch (e) {
					console.error("An error occurred while passing fontData", error);
				} finally {
					if (dataPtr) {
						_free(dataPtr);
					}
					Runtime.dynCall('vii', onGetFontData, [null, 0]);
				}				
			}

		})
	},
	TT_RegisterKeyDownCallback: function (callback) {
		if (!window.StarkSDK || !window.StarkSDK.RegisterKeyDownCallback) return;
		window.StarkSDK.RegisterKeyDownCallback(callback);
	},
	TT_RegisterKeyUpCallback: function (callback) {
		if (!window.StarkSDK || !window.StarkSDK.RegisterKeyUpCallback) return;
		window.StarkSDK.RegisterKeyUpCallback(callback);
	},
	TT_RegisterMouseDownCallback: function (callback) {
		if (!window.StarkSDK || !window.StarkSDK.RegisterMouseDownCallback) return;
		window.StarkSDK.RegisterMouseDownCallback(callback);
	},
	TT_RegisterMouseUpCallback: function (callback) {
		if (!window.StarkSDK || !window.StarkSDK.RegisterMouseUpCallback) return;
		window.StarkSDK.RegisterMouseUpCallback(callback);
	},
	TT_RegisterMouseMoveCallback: function (callback) {
		if (!window.StarkSDK || !window.StarkSDK.RegisterMouseMoveCallback) return;
		window.StarkSDK.RegisterMouseMoveCallback(callback);
	},
	TT_RegisterWheelCallback: function (callback) {
		if (!window.StarkSDK || !window.StarkSDK.RegisterWheelCallback) return;
		window.StarkSDK.RegisterWheelCallback(callback);
	},
	TT_SetCursor: function (path, x, y) {
		if (!window.StarkSDK || !window.StarkSDK.SetCursor) return;
		var res = window.StarkSDK.SetCursor(_StarkPointerStringify(path), x, y);
		return res;
	},
	TT_RequestPointerLock: function () {
		if (!window.StarkSDK || !window.StarkSDK.RequestPointerLock) return;
		window.StarkSDK.RequestPointerLock();
	},
	TT_IsPointerLocked: function () {
		if (!window.StarkSDK || !window.StarkSDK.IsPointerLocked) return;
		var res = window.StarkSDK.IsPointerLocked();
		return res;
	},
	TT_ExitPointerLock: function () {
		if (!window.StarkSDK || !window.StarkSDK.ExitPointerLock) return;
		window.StarkSDK.ExitPointerLock();
	},
	TT_SetPreferredDevicePixelRatioPercent: function(dprPct) {
		Module.devicePixelRatio = dprPct * 0.01;
	}
});