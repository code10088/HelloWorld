#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

namespace UnityExtensions.Editor
{
    /// <summary>
    /// BaseDecoratorDrawer<T>
    /// </summary>
    public class BaseDecoratorDrawer<T> : DecoratorDrawer where T : PropertyAttribute
    {
        protected new T attribute => (T)base.attribute;

    } // class BaseDecoratorDrawer<T>

} // namespace UnityExtensions.Editor

#endif // UNITY_EDITOR