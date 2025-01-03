#if UNITY_EDITOR
using System.Reflection;

namespace YooAsset
{
    public static class EditorSimulateModeHelper
    {
        /// <summary>
        /// 编辑器下模拟构建清单
        /// </summary>
        public static EditorSimulateBuildResult SimulateBuild(EditorSimulateBuildParam buildParam)
        {
            var assemblyName = buildParam.InvokeAssmeblyName;
            var className = buildParam.InvokeClassFullName;
            var methodName = buildParam.InvokeMethodName;
            var classType = Assembly.Load(assemblyName).GetType(className);
            return (EditorSimulateBuildResult)InvokePublicStaticMethod(classType, methodName, buildParam);
        }

        private static object InvokePublicStaticMethod(System.Type type, string method, params object[] parameters)
        {
            var methodInfo = type.GetMethod(method, BindingFlags.Public | BindingFlags.Static);
            if (methodInfo == null)
            {
                UnityEngine.Debug.LogError($"{type.FullName} not found method : {method}");
                return null;
            }
            return methodInfo.Invoke(null, parameters);
        }
    }
}
#else
namespace YooAsset
{ 
    public static class EditorSimulateModeHelper
    {
        public static EditorSimulateBuildResult SimulateBuild(EditorSimulateBuildParam buildParam) 
        {
            throw new System.Exception("Only support in unity editor !");
        }
    }
}
#endif