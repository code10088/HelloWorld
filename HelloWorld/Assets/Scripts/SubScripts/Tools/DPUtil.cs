using System;
using UnityEngine;

namespace HotAssembly
{
    public enum DPLevel
    {
        Null = 0,
        Low,
        Mid,
        High
    }
    public class DPUtil
    {
        public static DPLevel dpl;
        public static int DeviceScreenWidth;
        public static int DeviceScreenHeight;

        /// <summary>
        /// 游戏初始化
        /// </summary>
        public static void Init()
        {
            DeviceScreenWidth = Screen.width;
            DeviceScreenHeight = Screen.height;
            int qualityLv = PlayerPrefs.GetInt("Quality_Level", -1);
            if (qualityLv < 0) Recommend();
            else SetQualitySettings(qualityLv);
        }
        /// <summary>
        /// 推荐配置/恢复默认
        /// </summary>
        public static void Recommend()
        {
            DPLevel deviceLv = GetDeviceLevel();
            switch (deviceLv)
            {
                case DPLevel.Low:
                    SetQualitySettings(-1, "Low");
                    SetFrameRate(30);
                    SetScreenResolution(1);
                    SetAntiLv(0);
                    SetPostProcess(false);
                    SetGraphicsQualityLv(0);
                    SetShadowLv(0);
                    SetMasterTextureLimit(0);
                    break;
                case DPLevel.Mid:
                    SetQualitySettings(-1, "Medium");
                    SetFrameRate(30);
                    SetScreenResolution(1);
                    SetAntiLv(0);
                    SetPostProcess(true);
                    SetGraphicsQualityLv(1);
                    SetShadowLv(1);
                    SetMasterTextureLimit(0);
                    break;
                case DPLevel.High:
                    SetQualitySettings(-1, "High");
                    SetFrameRate(30);
                    SetScreenResolution(1);
                    SetAntiLv(4);
                    SetPostProcess(true);
                    SetGraphicsQualityLv(2);
                    SetShadowLv(2);
                    SetMasterTextureLimit(0);
                    break;
            }
        }
        /// <summary>
        /// 推荐分级
        /// </summary>
        /// <returns></returns>
        public static DPLevel GetDeviceLevel()
        {
            DPLevel deviceLv = DPLevel.Null;
            if (deviceLv == DPLevel.Null) deviceLv = GetDeviceModelLevel();
            if (deviceLv == DPLevel.Null) deviceLv = GetDeviceGPULevel();
            if (deviceLv == DPLevel.Null) deviceLv = GetDeviceCPULevel();
            return deviceLv;
        }
        /// <summary>
        /// CPU
        /// </summary>
        /// <returns></returns>
        private static DPLevel GetDeviceCPULevel()
        {
            DPLevel tempLv = DPLevel.High;
#if UNITY_EDITOR
            tempLv = DPLevel.High;
#else
            if (SystemInfo.graphicsDeviceVendorID == 32902)
            {
                tempLv = DevicePerformanceLevel.Low;
            }
            else if (SystemInfo.processorCount <= 4)
            {
                tempLv = DevicePerformanceLevel.Low;
            }
            else
            {
#if UNITY_IPHONE
            int systemMemorySize = SystemInfo.systemMemorySize;
            if (systemMemorySize >= 4000)
                tempLv = DevicePerformanceLevel.High;
            else if (systemMemorySize >= 2000)
                tempLv = DevicePerformanceLevel.Mid;
            else
                tempLv = DevicePerformanceLevel.Low;
#elif UNITY_ANDROID
            int graphicsMemorySize = SystemInfo.graphicsMemorySize;
            int systemMemorySize = SystemInfo.systemMemorySize;
            if (graphicsMemorySize >= 2000 && systemMemorySize >= 6000)
                tempLv = DevicePerformanceLevel.High;
            else if (graphicsMemorySize >= 1000 && systemMemorySize >= 4000)
                tempLv = DevicePerformanceLevel.Mid;
            else
                tempLv = DevicePerformanceLevel.Low;
#endif
            }
#endif
            return tempLv;
        }
        /// <summary>
        /// GPU
        /// </summary>
        /// <param name="lv"></param>
        /// <returns></returns>
        private static DPLevel GetDeviceGPULevel()
        {
            DPLevel tempLv = DPLevel.Null;
#if UNITY_EDITOR
            tempLv = DPLevel.High;
#else
            var deviceInfo = ConfigManager.Instance.GameConfigs.TbDeviceInfo.DataList;
            var graphicsDevice = SystemInfo.graphicsDeviceName.ToLower();
            for (int i = 0; i < deviceInfo.Count; i++)
            {
                if (graphicsDevice.Contains(deviceInfo[i].Name))
                {
                    tempLv = (DevicePerformanceLevel)deviceInfo[i].Lv;
                    break;
                }
            }
#endif
            return tempLv;
        }
        /// <summary>
        /// 设置具体型号
        /// </summary>
        /// <param name="lv"></param>
        /// <returns></returns>
        private static DPLevel GetDeviceModelLevel()
        {
            DPLevel tempLv = DPLevel.Null;
#if UNITY_EDITOR
            tempLv = DPLevel.High;
#else
            var deviceInfo = ConfigManager.Instance.GameConfigs.TbDeviceInfo.DataList;
            var deviceModel = SystemInfo.deviceModel.ToLower();
            for (int i = 0; i < deviceInfo.Count; i++)
            {
                if (deviceModel.Contains(deviceInfo[i].Name))
                {
                    tempLv = (DevicePerformanceLevel)deviceInfo[i].Lv;
                    break;
                }
            }
#endif
            return tempLv;
        }
        public static void SetQualitySettings(int qualityLv = -1, string qualityName = "")
        {
            if (qualityLv < 0) qualityLv = Array.FindIndex(QualitySettings.names, (a) => { return a == qualityName; });
            else if (string.IsNullOrEmpty(qualityName)) qualityName = QualitySettings.names[qualityLv];
            QualitySettings.SetQualityLevel(qualityLv);
            PlayerPrefs.SetInt("Quality_Level", qualityLv);
            dpl = (DPLevel)qualityLv;

            int frameRate = PlayerPrefs.GetInt("Quality_FrameRate", -1);
            if (frameRate >= 0) SetFrameRate(frameRate);
            int resolution = PlayerPrefs.GetInt("Quality_ScreenResolution", -1);
            if (resolution >= 0) SetScreenResolution(resolution);
            int anti = PlayerPrefs.GetInt("Quality_Anti", -1);
            if (anti >= 0) SetAntiLv(anti);
            int postProcess = PlayerPrefs.GetInt("Quality_PostProcess", -1);
            if (postProcess >= 0) SetPostProcess(postProcess > 0);
            int graphics = PlayerPrefs.GetInt("Quality_GraphicsQuality", -1);
            if (graphics >= 0) SetGraphicsQualityLv(graphics);
            int shadow = PlayerPrefs.GetInt("Quality_Shadow", -1);
            if (shadow >= 0) SetShadowLv(shadow);
            int textureLimit = PlayerPrefs.GetInt("Quality_TextureLimit", -1);
            if (textureLimit >= 0) SetMasterTextureLimit(textureLimit);
            ////像素灯的最大数量
            //QualitySettings.pixelLightCount = 2;
            ////反射探针
            //QualitySettings.realtimeReflectionProbes = false;
            ////特效软粒子
            //QualitySettings.softParticles = false;
            ////各向异性
            //QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
            ////高低模切换缓冲
            //QualitySettings.lodBias = 2;
            //QualitySettings.particleRaycastBudget = 0;
            //QualitySettings.skinWeights = SkinWeights.FourBones;
        }

        /// <summary>
        /// 帧率
        /// </summary>
        /// <param name="targetFrameRate"></param>
        public static void SetFrameRate(int targetFrameRate)
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = targetFrameRate;
            PlayerPrefs.SetInt("Quality_FrameRate", targetFrameRate);
            GameSetting.updateTimeSliceS = 1.0f / targetFrameRate;
            xasset.Scheduler.AutoslicingTimestep = GameSetting.updateTimeSliceS;
        }
        /// <summary>
        /// 分辨率
        /// </summary>
        /// <param name="resolution">0.5,0.8,1</param>
        public static void SetScreenResolution(float rate)
        {
            //string activity = string.Format("com.{0}.{1}.MainActivity", Application.companyName, Application.productName);
            //AndroidJavaClass jc = new AndroidJavaClass(activity);
            //AndroidJavaObject jo = jc.CallStatic<AndroidJavaObject>("Instance");
            //int resolution = jo.Call<int>("GetScreenWidthHeight");
            int width = Mathf.CeilToInt(DeviceScreenWidth * rate);
            int height = Mathf.CeilToInt(DeviceScreenHeight * rate);
            Screen.SetResolution(width, height, true);
            PlayerPrefs.SetFloat("Quality_ScreenResolution", rate);
        }
        /// <summary>
        /// 抗锯齿
        /// </summary>
        /// <param name="lv">0,2,4</param>
        public static void SetAntiLv(int lv)
        {
            if (lv == 2) QualitySettings.antiAliasing = 2;
            else if (lv == 4) QualitySettings.antiAliasing = 4;
            else QualitySettings.antiAliasing = 0;
            PlayerPrefs.SetInt("Quality_Anti", lv);
        }
        /// <summary>
        /// 后处理
        /// </summary>
        /// <param name="state"></param>
        public static void SetPostProcess(bool state)
        {
#if PostProcess
            UnityEngine.Rendering.PostProcessing.PostProcessManager.instance.SetActive(state);
#endif
            PlayerPrefs.SetInt("Quality_PostProcess", state ? 1 : 0);
        }
        /// <summary>
        /// 画质：低、中、高
        /// </summary>
        /// <param name="quality">0,1,2</param>
        public static void SetGraphicsQualityLv(int quality)
        {
            switch (quality)
            {
                case 1:
                    Shader.DisableKeyword("LowQuality");
                    Shader.EnableKeyword("MediumQuality");
                    Shader.DisableKeyword("HighQuality");
                    break;
                case 2:
                    Shader.DisableKeyword("LowQuality");
                    Shader.DisableKeyword("MediumQuality");
                    Shader.EnableKeyword("HighQuality");
                    break;
                default:
                    Shader.EnableKeyword("LowQuality");
                    Shader.DisableKeyword("MediumQuality");
                    Shader.DisableKeyword("HighQuality");
                    break;
            }
            PlayerPrefs.SetInt("Quality_GraphicsQuality", quality);
        }
        /// <summary>
        /// 阴影
        /// </summary>
        public static void SetShadowLv(int quality)
        {
            QualitySettings.shadows = (ShadowQuality)quality;
            PlayerPrefs.SetInt("Quality_Shadow", quality);
        }
        /// <summary>
        /// 0_完整分辨率，1_1/2分辨率，2_1/4分辨率，3_1/8分辨率
        /// </summary>
        public static void SetMasterTextureLimit(int lv)
        {
            QualitySettings.masterTextureLimit = lv;
            PlayerPrefs.SetInt("Quality_TextureLimit", lv);
        }
    }
}