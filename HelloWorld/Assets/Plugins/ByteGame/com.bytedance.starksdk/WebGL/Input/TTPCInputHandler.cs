#if UNITY_WEBGL && !UNITY_EDITOR
  #define TT_PC_INPUT
#endif

using System;
using UnityEngine;

#if TT_PC_INPUT
using System.Runtime.InteropServices;
using AOT;
#endif

namespace TTSDK
{
    public class TTPCInputHandler
    {
#if UNITY_EDITOR
        public const string NotImplMessage = "TT Desktop Input API not supported on Unity Editor. Test it with Douyin App.";
#elif UNITY_ANDROID
        public const string NotImplMessage = "TT Desktop Input API not supported on Android Native.";
#else
        public const string NotImplMessage = "TT Desktop Input API supported on current platform.";
#endif
        
        #region APIs
        
#if TT_PC_INPUT
        [DllImport("__Internal")]
        public static extern bool TT_SetCursor(string path, double x, double y);
#else
        public static bool TT_SetCursor(string path, double x, double y)
        {
            throw new NotSupportedException(NotImplMessage);
        }
#endif
        
#if TT_PC_INPUT
        [DllImport("__Internal")]
        public static extern void TT_RequestPointerLock();
#else
        public static void TT_RequestPointerLock()
        {
            throw new NotSupportedException(NotImplMessage);
        }
#endif
        
#if TT_PC_INPUT
        [DllImport("__Internal")]
        public static extern bool TT_IsPointerLocked();
#else
        public static bool TT_IsPointerLocked()
        {
            throw new NotSupportedException(NotImplMessage);
        }
#endif
        
#if TT_PC_INPUT
        [DllImport("__Internal")]
        public static extern void TT_ExitPointerLock();
#else
        public static void TT_ExitPointerLock()
        { 
            throw new NotSupportedException(NotImplMessage);
        }
#endif
        #endregion
      
        #region Callback Delegate
        
        internal delegate void TTIntPtrCallback(IntPtr ptr);
        
        #endregion
        
        #region Callback OnKeyDown
#if TT_PC_INPUT
        [DllImport("__Internal")]
        private static extern void TT_RegisterKeyDownCallback(TTIntPtrCallback callback);
#else
        private static void TT_RegisterKeyDownCallback(TTIntPtrCallback callback)
        {
            throw new NotSupportedException(NotImplMessage);
        }
#endif

#if TT_PC_INPUT
        [MonoPInvokeCallback(typeof(TTIntPtrCallback))]
#endif
        private static void _OnKeyDownCallback(IntPtr ptr)
        {
            _onKeyDownCallback.Invoke(ptr);
        }

        private static Action<IntPtr> _onKeyDownCallback;
        public static void SetOnKeyDownCallback(Action<IntPtr> callback)
        {
            _onKeyDownCallback = callback;
            TT_RegisterKeyDownCallback(_onKeyDownCallback != null ? _OnKeyDownCallback : null);
        }
        
        #endregion
        
        #region Callback OnKeyUp
#if TT_PC_INPUT
        [DllImport("__Internal")]
        private static extern void TT_RegisterKeyUpCallback(TTIntPtrCallback callback);
#else
        private static void TT_RegisterKeyUpCallback(TTIntPtrCallback callback)
        {
            throw new NotSupportedException(NotImplMessage);
        }
#endif

#if TT_PC_INPUT
        [MonoPInvokeCallback(typeof(TTIntPtrCallback))]
#endif
        private static void _OnKeyUpCallback(IntPtr ptr)
        {
            _onKeyUpCallback.Invoke(ptr);
        }

        private static Action<IntPtr> _onKeyUpCallback;
        public static void SetOnKeyUpCallback(Action<IntPtr> callback)
        {
            _onKeyUpCallback = callback;
            TT_RegisterKeyUpCallback(_onKeyUpCallback != null ? _OnKeyUpCallback : null);
        }
        
        #endregion
        
        #region Callback OnMouseDown
#if TT_PC_INPUT
        [DllImport("__Internal")]
        private static extern void TT_RegisterMouseDownCallback(TTIntPtrCallback callback);
#else
        private static void TT_RegisterMouseDownCallback(TTIntPtrCallback callback)
        {
            throw new NotSupportedException(NotImplMessage);
        }
#endif

#if TT_PC_INPUT
        [MonoPInvokeCallback(typeof(TTIntPtrCallback))]
#endif
        private static void _OnMouseDownCallback(IntPtr ptr)
        {
            _onMouseDownCallback.Invoke(ptr);
        }

        private static Action<IntPtr> _onMouseDownCallback;
        public static void SetOnMouseDownCallback(Action<IntPtr> callback)
        {
            _onMouseDownCallback = callback;
            TT_RegisterMouseDownCallback(_onMouseDownCallback != null ? _OnMouseDownCallback : null);
        }
        
        #endregion
        
        #region Callback OnMouseUp
#if TT_PC_INPUT
        [DllImport("__Internal")]
        private static extern void TT_RegisterMouseUpCallback(TTIntPtrCallback callback);
#else
        private static void TT_RegisterMouseUpCallback(TTIntPtrCallback callback)
        {
            throw new NotSupportedException(NotImplMessage);
        }
#endif

#if TT_PC_INPUT
        [MonoPInvokeCallback(typeof(TTIntPtrCallback))]
#endif
        private static void _OnMouseUpCallback(IntPtr ptr)
        {
            _onMouseUpCallback.Invoke(ptr);
        }

        private static Action<IntPtr> _onMouseUpCallback;
        public static void SetOnMouseUpCallback(Action<IntPtr> callback)
        {
            _onMouseUpCallback = callback;
            TT_RegisterMouseUpCallback(_onMouseUpCallback != null ? _OnMouseUpCallback : null);
        }
        
        #endregion
        
        #region Callback OnMouseMove
#if TT_PC_INPUT
        [DllImport("__Internal")]
        private static extern void TT_RegisterMouseMoveCallback(TTIntPtrCallback callback);
#else
        private static void TT_RegisterMouseMoveCallback(TTIntPtrCallback callback)
        {
            throw new NotSupportedException(NotImplMessage);
        }
#endif

#if TT_PC_INPUT
        [MonoPInvokeCallback(typeof(TTIntPtrCallback))]
#endif
        private static void _OnMouseMoveCallback(IntPtr ptr)
        {
            _onMouseMoveCallback.Invoke(ptr);
        }

        private static Action<IntPtr> _onMouseMoveCallback;
        public static void SetOnMouseMoveCallback(Action<IntPtr> callback)
        {
            _onMouseMoveCallback = callback;
            TT_RegisterMouseMoveCallback(_onMouseMoveCallback != null ? _OnMouseMoveCallback : null);
        }
        
        #endregion
        
        #region Callback OnWheel
#if TT_PC_INPUT
        [DllImport("__Internal")]
        private static extern void TT_RegisterWheelCallback(TTIntPtrCallback callback);
#else
        private static void TT_RegisterWheelCallback(TTIntPtrCallback callback)
        {
            throw new NotSupportedException(NotImplMessage);
        }
#endif

#if TT_PC_INPUT
        [MonoPInvokeCallback(typeof(TTIntPtrCallback))]
#endif
        private static void _OnWheelCallback(IntPtr ptr)
        {
            _onWheelCallback.Invoke(ptr);
        }

        private static Action<IntPtr> _onWheelCallback;
        public static void SetOnWheelCallback(Action<IntPtr> callback)
        {
            _onWheelCallback = callback;
            TT_RegisterWheelCallback(_onWheelCallback != null ? _OnWheelCallback : null);
        }
        
        #endregion

    }
}