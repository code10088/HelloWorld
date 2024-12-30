
namespace YooAsset.Editor
{
    public class CollectCommand
    {
        /// <summary>
        /// 模拟构建模式
        /// </summary>
        public bool SimulateBuild { private set; get; }

        /// <summary>
        /// 使用资源依赖数据库
        /// </summary>
        public bool UseAssetDependencyDB { private set; get; }
        
        /// <summary>
        /// 包裹名称
        /// </summary>
        public string PackageName { private set; get; }

        /// <summary>
        /// 启用可寻址资源定位
        /// </summary>
        public bool EnableAddressable { private set; get; }

        /// <summary>
        /// 资源定位地址大小写不敏感
        /// </summary>
        public bool LocationToLower { private set; get; }

        /// <summary>
        /// 包含资源GUID数据
        /// </summary>
        public bool IncludeAssetGUID { private set; get; }

        /// <summary>
        /// 自动收集所有着色器
        /// </summary>
        public bool AutoCollectShaders { private set; get; }

        /// <summary>
        /// 资源包名唯一化
        /// </summary>
        public bool UniqueBundleName { private set; get; }

        /// <summary>
        /// 着色器统一全名称
        /// </summary>
        public string ShadersBundleName { private set; get; }

        /// <summary>
        /// 忽略规则实例
        /// </summary>
        public IIgnoreRule IgnoreRule { private set; get; }


        public CollectCommand(bool simulateBuild, bool useAssetDependencyDB, string packageName,
            bool enableAddressable, bool locationToLower, bool includeAssetGUID,
            bool autoCollectShaders, bool uniqueBundleName, IIgnoreRule ignoreRule)
        {
            SimulateBuild = simulateBuild;
            UseAssetDependencyDB = useAssetDependencyDB;
            PackageName = packageName;
            EnableAddressable = enableAddressable;
            LocationToLower = locationToLower;
            IncludeAssetGUID = includeAssetGUID;
            AutoCollectShaders = autoCollectShaders;
            UniqueBundleName = uniqueBundleName;
            IgnoreRule = ignoreRule;

            // 着色器统一全名称
            var packRuleResult = DefaultPackRule.CreateShadersPackRuleResult();
            ShadersBundleName = packRuleResult.GetBundleName(packageName, uniqueBundleName);
        }

        private AssetDependencyCache _assetDependency;
        public AssetDependencyCache AssetDependency
        {
            get
            {
                if (_assetDependency == null)
                    _assetDependency = new AssetDependencyCache(UseAssetDependencyDB);
                return _assetDependency;
            }
        }
    }
}