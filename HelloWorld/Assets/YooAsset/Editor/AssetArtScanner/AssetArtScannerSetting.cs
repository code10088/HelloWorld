using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;

namespace YooAsset.Editor
{
    public class AssetArtScannerSetting : ScriptableObject
    {
        /// <summary>
        /// 扫描器列表
        /// </summary>
        public List<AssetArtScanner> Scanners = new List<AssetArtScanner>();
    }
}