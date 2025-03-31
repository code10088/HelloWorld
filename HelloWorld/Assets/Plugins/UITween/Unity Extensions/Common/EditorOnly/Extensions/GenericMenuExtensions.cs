#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

namespace UnityExtensions.Editor
{
    /// <summary>
    /// Extensions for GenericMenu.
    /// </summary>
    public static partial class Extensions
    {
        public static void AddItem(this GenericMenu menu, GUIContent content, GenericMenu.MenuFunction func, bool disabled = false, bool on = false)
        {
            if (disabled)
            {
                menu.AddDisabledItem(content, on);
            }
            else
            {
                menu.AddItem(content, on, func);
            }
        }

        public static void AddItem(this GenericMenu menu, GUIContent content, GenericMenu.MenuFunction2 func, object userData, bool disabled = false, bool on = false)
        {
            if (disabled)
            {
                menu.AddDisabledItem(content, on);
            }
            else
            {
                menu.AddItem(content, on, func, userData);
            }
        }

    } // class Extensions

} // namespace UnityExtensions.Editor

#endif // UNITY_EDITOR