// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static GPUInstancerPro.GPUIProPackageImporterData;

namespace GPUInstancerPro
{
    [Serializable]
    public class GPUIProPackageImporterImportedData : ScriptableObject
    {
        public List<ImportedPackageInfo> importedPackageInfos;
    }
}