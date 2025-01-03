
namespace YooAsset
{
    public class EditorSimulateBuildParam
    {
        /// <summary>
        /// 包裹名称
        /// </summary>
        public readonly string PackageName;

        /// <summary>
        /// 模拟构建管线名称
        /// </summary>
        public string BuildPipelineName = "EditorSimulateBuildPipeline";

        /// <summary>
        /// 模拟构建类所属程序集名称
        /// </summary>
        public string InvokeAssmeblyName = "YooAsset.Editor";

        /// <summary>
        /// 模拟构建执行的类名全称
        /// 注意：类名必须包含命名空间！
        /// </summary>
        public string InvokeClassFullName = "YooAsset.Editor.AssetBundleSimulateBuilder";

        /// <summary>
        /// 模拟构建执行的方法名称
        /// 注意：执行方法必须满足 BindingFlags.Public | BindingFlags.Static
        /// </summary>
        public string InvokeMethodName = "SimulateBuild";

        public EditorSimulateBuildParam(string packageName)
        {
            PackageName = packageName;
        }
    }
}