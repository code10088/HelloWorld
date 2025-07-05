#if TUANJIE_1_5_OR_NEWER
#define TT_MINIGAME_BUILD_SUPPORTED
#endif

using UnityEditor;
using UnityEngine.Rendering;
#if !TT_MINIGAME_BUILD_SUPPORTED
using UnityEngine;
#endif

namespace TTSDK.Tool
{
    public class TTMiniGameSupportProvider : ITTMiniGameSupportProvider
    {
        
        private const string NotSupportedTips = "当前环境不支持 MiniGame 构建";

        public bool SupportMiniGame()
        {
#if TT_MINIGAME_BUILD_SUPPORTED
            return true;
#else
            return false;
#endif
        }

        public int GetBuildTargetGroup(WasmSubFramework target)
        {
            if (target == WasmSubFramework.MiniGame)
            {
#if TT_MINIGAME_BUILD_SUPPORTED
                return (int)BuildTargetGroup.MiniGame;
#else
                Debug.LogError(NotSupportedTips);
                return (int)BuildTargetGroup.Unknown;
#endif
            }
            return (int)BuildTargetGroup.WebGL;
        }

        public int GetBuildTarget(WasmSubFramework target)
        {
            if (target == WasmSubFramework.MiniGame)
            {
#if TT_MINIGAME_BUILD_SUPPORTED
                return (int)BuildTarget.MiniGame;
#else
                Debug.LogError(NotSupportedTips);
                return (int)BuildTargetGroup.Unknown;
#endif
            }
            return (int)BuildTarget.WebGL;
        }
        
        public void SetPlayerSettingsMemorySize(WasmSubFramework target, int memorySize, bool profiling, PlayerSettings playerSettings = null)
        {
            if (memorySize <= 0)
                return;
            
            var emscriptenArgs = profiling ? $" -s TOTAL_MEMORY={memorySize}MB --memoryprofiler" : $" -s TOTAL_MEMORY={memorySize}MB";
            if (target == WasmSubFramework.MiniGame)
            {
#if TT_MINIGAME_BUILD_SUPPORTED
                if (playerSettings != null)
                {
                    PlayerSettings.MiniGame.SetMemorySize_Internal(playerSettings, (MiniGameDebugSymbolMode)memorySize);
                    PlayerSettings.MiniGame.SetEmscriptenArgs_Internal(playerSettings, emscriptenArgs);
                }
                else
                {
                    PlayerSettings.MiniGame.memorySize = memorySize;
                    PlayerSettings.MiniGame.emscriptenArgs = emscriptenArgs;
                }
#else
                Debug.LogError(NotSupportedTips);
#endif
            }
            else
            {
                PlayerSettings.WebGL.memorySize = memorySize;
                PlayerSettings.WebGL.emscriptenArgs = emscriptenArgs;
            }
        }
        
        public void SetPlayerSettingsDebugSymbolMode(WasmSubFramework target, int mode, PlayerSettings playerSettings = null)
        {
            if (target == WasmSubFramework.MiniGame)
            {
#if TT_MINIGAME_BUILD_SUPPORTED
                if (playerSettings != null)
                {
                    PlayerSettings.MiniGame.SetDebugSymbolMode_Internal(playerSettings, (MiniGameDebugSymbolMode)mode);
                }
                else
                {
                    PlayerSettings.MiniGame.debugSymbolMode = (MiniGameDebugSymbolMode)mode;
                }
#else
                Debug.LogError(NotSupportedTips);
#endif
            }
            else
            {
                PlayerSettings.WebGL.debugSymbolMode = (WebGLDebugSymbolMode)mode;
            }
        }
        
        public void SetPlayerSettingsRequiredModification(WasmSubFramework target, PlayerSettings playerSettings = null)
        {
            if (target == WasmSubFramework.MiniGame)
            {
#if TT_MINIGAME_BUILD_SUPPORTED
                if (playerSettings != null)
                {
                    PlayerSettings.MiniGame.SetDataCaching_Internal(playerSettings, false);
                    PlayerSettings.MiniGame.SetNameFilesAsHashes_Internal(playerSettings, false);
                    PlayerSettings.MiniGame.SetCompressionFormat_Internal(playerSettings, MiniGameCompressionFormat.Disabled);
                }
                else
                {
                    PlayerSettings.MiniGame.dataCaching = false;
                    PlayerSettings.MiniGame.nameFilesAsHashes = false;
                    PlayerSettings.MiniGame.compressionFormat = MiniGameCompressionFormat.Disabled;
                }
#else
                Debug.LogError(NotSupportedTips);
#endif
            }
            else
            {
                PlayerSettings.WebGL.dataCaching = false;
                PlayerSettings.WebGL.nameFilesAsHashes = false;
                PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            }
        }

        public bool DetectWebGL2Required(WasmSubFramework target, PlayerSettings playerSettings = null)
        {
#if TT_MINIGAME_BUILD_SUPPORTED
            var targets = PlayerSettings.GetGraphicsAPIs_Internal(playerSettings, (BuildTarget)GetBuildTarget(target));
            return targets.Length > 0 && targets[0] == GraphicsDeviceType.OpenGLES3;
#else
            return false;
#endif
        }
        
    }
}