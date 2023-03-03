using System;
using UnityEngine;

namespace UnityExtensions
{
    public struct GUIContentColorScope : IDisposable
    {
        Color _orginal;

        public static GUIContentColorScope New(Color value)
        {
            var scope = new GUIContentColorScope { _orginal = GUI.contentColor };
            GUI.contentColor = value;
            return scope;
        }

        void IDisposable.Dispose()
        {
            GUI.contentColor = _orginal;
        }
    }


    public struct GUIBackgroundColorScope : IDisposable
    {
        Color _orginal;

        public static GUIBackgroundColorScope New(Color value)
        {
            var scope = new GUIBackgroundColorScope { _orginal = GUI.backgroundColor };
            GUI.backgroundColor = value;
            return scope;
        }

        void IDisposable.Dispose()
        {
            GUI.backgroundColor = _orginal;
        }
    }


    public struct GUIColorScope : IDisposable
    {
        Color _orginal;

        public static GUIColorScope New(Color value)
        {
            var scope = new GUIColorScope { _orginal = GUI.color };
            GUI.color = value;
            return scope;
        }

        void IDisposable.Dispose()
        {
            GUI.color = _orginal;
        }
    }


    public struct GUIGroupScope : IDisposable
    {
        public static GUIGroupScope New(Rect rect)
        {
            GUI.BeginGroup(rect);
            return new GUIGroupScope();
        }

        public static GUIGroupScope New(Rect rect, Texture image)
        {
            GUI.BeginGroup(rect, image);
            return new GUIGroupScope();
        }

        void IDisposable.Dispose()
        {
            GUI.EndGroup();
        }
    }


    public struct GUILayoutAreaScope : IDisposable
    {
        public static GUILayoutAreaScope New(Rect rect)
        {
            GUILayout.BeginArea(rect);
            return new GUILayoutAreaScope();
        }

        public static GUILayoutAreaScope New(Rect rect, Texture image)
        {
            GUILayout.BeginArea(rect, image);
            return new GUILayoutAreaScope();
        }

        void IDisposable.Dispose()
        {
            GUILayout.EndArea();
        }
    }


    public struct GUILayoutHorizontalScope : IDisposable
    {
        public static GUILayoutHorizontalScope New(params GUILayoutOption[] options)
        {
            GUILayout.BeginHorizontal(options);
            return new GUILayoutHorizontalScope();
        }

        public static GUILayoutHorizontalScope New(GUIStyle style, params GUILayoutOption[] options)
        {
            GUILayout.BeginHorizontal(style, options);
            return new GUILayoutHorizontalScope();
        }

        void IDisposable.Dispose()
        {
            GUILayout.EndHorizontal();
        }
    }


    public struct GUILayoutVerticalScope : IDisposable
    {
        public static GUILayoutVerticalScope New(params GUILayoutOption[] options)
        {
            GUILayout.BeginVertical(options);
            return new GUILayoutVerticalScope();
        }

        public static GUILayoutVerticalScope New(GUIStyle style, params GUILayoutOption[] options)
        {
            GUILayout.BeginVertical(style, options);
            return new GUILayoutVerticalScope();
        }

        void IDisposable.Dispose()
        {
            GUILayout.EndVertical();
        }
    }


    public struct GUILayoutScrollViewScope : IDisposable
    {
        public static GUILayoutScrollViewScope New(ref Vector2 scrollPosition, params GUILayoutOption[] options)
        {
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, options);
            return new GUILayoutScrollViewScope();
        }

        public static GUILayoutScrollViewScope New(ref Vector2 scrollPosition, bool alwaysShowHorizontal, bool alwaysShowVertical, params GUILayoutOption[] options)
        {
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, alwaysShowHorizontal, alwaysShowVertical, options);
            return new GUILayoutScrollViewScope();
        }

        void IDisposable.Dispose()
        {
            GUI.EndScrollView();
        }
    }

} // namespace UnityExtensions