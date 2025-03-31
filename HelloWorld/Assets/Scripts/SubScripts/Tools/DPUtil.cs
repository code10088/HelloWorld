using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using YooAsset;

public enum DPLevel
{
    Null = -1,
    Low,
    Mid,
    High
}
/// <summary>
/// build-in管线：
///     所有设置都在QualitySetting
/// urp管线：
///     所有设置都在UniversalRendererData上但不支持运行时修改
///     部分设置暴露在Camera属性上
///     部分设置通过反射方式修改
/// 当前urp方案：
///     QualitySetting保留高中低三挡设置相同
///     UniversalRendererData保留高中低三挡设置相同
///     设置通过Camera或反射修改
/// </summary>
public class DPUtil
{
    public static DPLevel dpl => (DPLevel)QualityLv;
    public static int QualityLv => quality & 0x0003;
    public static int FrameRate => (quality & 0x000C) >> 2;
    public static int ScreenResolution => (quality & 0x0030) >> 4;
    public static int MasterTextureLimit => (quality & 0x0040) >> 6;
    public static int AntiLv => (quality & 0x0080) >> 7;
    public static int Shadow => (quality & 0x0100) >> 8;
    public static int SoftShadow => (quality & 0x0200) >> 9;
    public static int ShadowLv => (quality & 0x0C00) >> 10;
    public static int HDR => (quality & 0x1000) >> 12;
    public static int PostProcess => (quality & 0x2000) >> 13;
    public static int GraphicsQualityLv => (quality & 0xC000) >> 14;

    public static int DeviceScreenWidth;
    public static int DeviceScreenHeight;

    private static int quality = 0;

    #region 推荐分级
    /// <summary>
    /// 推荐分级
    /// </summary>
    /// <returns></returns>
    private static DPLevel GetDeviceLevel()
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
                tempLv = DPLevel.Low;
            }
            else if (SystemInfo.processorCount <= 4)
            {
                tempLv = DPLevel.Low;
            }
            else
            {
#if UNITY_IPHONE
            int systemMemorySize = SystemInfo.systemMemorySize;
            if (systemMemorySize >= 4000)
                tempLv = DPLevel.High;
            else if (systemMemorySize >= 2000)
                tempLv = DPLevel.Mid;
            else
                tempLv = DPLevel.Low;
#elif UNITY_ANDROID
            int graphicsMemorySize = SystemInfo.graphicsMemorySize;
            int systemMemorySize = SystemInfo.systemMemorySize;
            if (graphicsMemorySize >= 2000 && systemMemorySize >= 6000)
                tempLv = DPLevel.High;
            else if (graphicsMemorySize >= 1000 && systemMemorySize >= 4000)
                tempLv = DPLevel.Mid;
            else
                tempLv = DPLevel.Low;
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
                    tempLv = (DPLevel)deviceInfo[i].Lv;
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
                    tempLv = (DPLevel)deviceInfo[i].Lv;
                    break;
                }
            }
#endif
        return tempLv;
    }
    #endregion

    /// <summary>
    /// 游戏初始化
    /// </summary>
    public static void Init()
    {
        DeviceScreenWidth = Screen.width;
        DeviceScreenHeight = Screen.height;
        quality = GamePlayerPrefs.GetInt(PlayerPrefsConst.DPQuality, 0);
        if (quality > 0)
        {
            SetQualitySettings(QualityLv);
            SetFrameRate(FrameRate);
            SetScreenResolution(ScreenResolution);
            SetMasterTextureLimit(MasterTextureLimit);
            SetAntiLv(AntiLv);
            SetShadow(Shadow);
            SetSoftShadow(SoftShadow);
            SetShadowLv(ShadowLv);
            SetHDR(HDR);
            SetPostProcess(PostProcess);
            SetGraphicsQualityLv(GraphicsQualityLv);
        }
        else
        {
            DPLevel deviceLv = GetDeviceLevel();
            switch (deviceLv)
            {
                case DPLevel.Low:
                    SetQualitySettings(0);
                    SetFrameRate(0);
                    SetScreenResolution(0);
                    SetMasterTextureLimit(0);
                    SetAntiLv(0);
                    SetShadow(0);
                    SetSoftShadow(0);
                    SetShadowLv(0);
                    SetHDR(0);
                    SetPostProcess(0);
                    SetGraphicsQualityLv(0);
                    break;
                case DPLevel.Mid:
                    SetQualitySettings(1);
                    SetFrameRate(1);
                    SetScreenResolution(0);
                    SetMasterTextureLimit(0);
                    SetAntiLv(1);
                    SetShadow(1);
                    SetSoftShadow(0);
                    SetShadowLv(1);
                    SetHDR(0);
                    SetPostProcess(0);
                    SetGraphicsQualityLv(1);
                    break;
                case DPLevel.High:
                    SetQualitySettings(2);
                    SetFrameRate(2);
                    SetScreenResolution(0);
                    SetMasterTextureLimit(0);
                    SetAntiLv(1);
                    SetShadow(1);
                    SetSoftShadow(1);
                    SetShadowLv(2);
                    SetHDR(1);
                    SetPostProcess(1);
                    SetGraphicsQualityLv(2);
                    break;
            }
        }
    }
    /// <summary>
    /// 恢复默认
    /// 恢复默认
    /// </summary>
    public static void Recommand()
    {
        GamePlayerPrefs.SetInt(PlayerPrefsConst.DPQuality, 0);
        Init();
    }

    /// <summary>
    /// 画面质量 0,1,2
    /// </summary>
    public static void SetQualitySettings(int lv)
    {
        QualitySettings.SetQualityLevel(lv);
        quality = quality & 0xFFFC | Mathf.Clamp(lv, 0, 2);
        GamePlayerPrefs.SetInt(PlayerPrefsConst.DPQuality, quality);
        SetMasterTextureLimit(MasterTextureLimit);
        SetAntiLv(AntiLv);
        SetSoftShadow(SoftShadow);
        SetShadowLv(ShadowLv);
    }
    /// <summary>
    /// 帧率 0,1,2
    /// </summary>
    public static void SetFrameRate(int lv)
    {
        QualitySettings.vSyncCount = 0;
        if (lv == 0) Application.targetFrameRate = 30;
        else if (lv == 1) Application.targetFrameRate = 45;
        else if (lv == 2) Application.targetFrameRate = 60;
        quality = quality & 0xFFF3 | Mathf.Clamp(lv, 0, 2) << 2;
        GamePlayerPrefs.SetInt(PlayerPrefsConst.DPQuality, quality);
        GameSetting.updateTimeSliceS = 1.0f / Application.targetFrameRate;
        GameSetting.updateTimeSliceMS = 1000 / Application.targetFrameRate;
        YooAssets.SetOperationSystemMaxTimeSlice(GameSetting.updateTimeSliceMS);
    }
    /// <summary>
    /// 分辨率 0,1,2
    /// </summary>
    public static void SetScreenResolution(int lv)
    {
        float r = 1;
        if (lv == 0) r = 1f;
        else if (lv == 1) r = 0.8f;
        else if (lv == 2) r = 0.5f;
        //int width = Mathf.CeilToInt(DeviceScreenWidth * r);
        //int height = Mathf.CeilToInt(DeviceScreenHeight * r);
        //Screen.SetResolution(width, height, true);
        ScalableBufferManager.ResizeBuffers(r, r);
        quality = quality & 0xFFCF | Mathf.Clamp(lv, 0, 2) << 4;
        GamePlayerPrefs.SetInt(PlayerPrefsConst.DPQuality, quality);
    }
    /// <summary>
    /// 贴图分辨率 0,1
    /// </summary>
    public static void SetMasterTextureLimit(int lv)
    {
        //0:贴图1/1大小 1:贴图1/2大小 2:贴图1/4大小 3:贴图1/8大小
        QualitySettings.globalTextureMipmapLimit = lv;
        quality = quality & 0xFFBF | Mathf.Clamp(lv, 0, 1) << 6;
        GamePlayerPrefs.SetInt(PlayerPrefsConst.DPQuality, quality);
    }
    /// <summary>
    /// 抗锯齿 0,1
    /// </summary>
    public static void SetAntiLv(int lv)
    {
        Camera.main.allowMSAA = lv == 1;
        quality = quality & 0xFF7F | Mathf.Clamp(lv, 0, 1) << 7;
        GamePlayerPrefs.SetInt(PlayerPrefsConst.DPQuality, quality);
    }
    /// <summary>
    /// 阴影开关 0,1
    /// </summary>
    public static void SetShadow(int lv)
    {
        var uac = Camera.main.GetComponent<UniversalAdditionalCameraData>();
        uac.renderShadows = lv == 1;
        quality = quality & 0xFEFF | Mathf.Clamp(lv, 0, 1) << 8;
        GamePlayerPrefs.SetInt(PlayerPrefsConst.DPQuality, quality);
    }
    /// <summary>
    /// 软阴影 0,1
    /// </summary>
    public static void SetSoftShadow(int lv)
    {
        var softShadow = typeof(UniversalRenderPipelineAsset).GetField("m_SoftShadowsSupported", BindingFlags.Instance | BindingFlags.NonPublic);
        softShadow.SetValue(GraphicsSettings.currentRenderPipeline, lv == 1);
        quality = quality & 0xFDFF | Mathf.Clamp(lv, 0, 1) << 9;
        GamePlayerPrefs.SetInt(PlayerPrefsConst.DPQuality, quality);
    }
    /// <summary>
    /// 阴影质量 0,1,2
    /// </summary>
    public static void SetShadowLv(int lv)
    {
        var resolution = typeof(UniversalRenderPipelineAsset).GetField("m_MainLightShadowmapResolution", BindingFlags.Instance | BindingFlags.NonPublic);
        var cascade = typeof(UniversalRenderPipelineAsset).GetField("m_ShadowCascadeCount", BindingFlags.Instance | BindingFlags.NonPublic);
        if (lv == 0)
        {
            cascade.SetValue(GraphicsSettings.currentRenderPipeline, 2);
            resolution.SetValue(GraphicsSettings.currentRenderPipeline, UnityEngine.Rendering.Universal.ShadowResolution._1024);
        }
        else if (lv == 1)
        {
            cascade.SetValue(GraphicsSettings.currentRenderPipeline, 3);
            resolution.SetValue(GraphicsSettings.currentRenderPipeline, UnityEngine.Rendering.Universal.ShadowResolution._1024);
        }
        else if (lv == 2)
        {
            cascade.SetValue(GraphicsSettings.currentRenderPipeline, 4);
            resolution.SetValue(GraphicsSettings.currentRenderPipeline, UnityEngine.Rendering.Universal.ShadowResolution._1024);
        }
        quality = quality & 0xF3FF | Mathf.Clamp(lv, 0, 2) << 10;
        GamePlayerPrefs.SetInt(PlayerPrefsConst.DPQuality, quality);
    }
    /// <summary>
    /// HDR 0,1
    /// </summary>
    public static void SetHDR(int lv)
    {
        Camera.main.allowHDR = lv == 1;
        quality = quality & 0xEFFF | Mathf.Clamp(lv, 0, 1) << 12;
        GamePlayerPrefs.SetInt(PlayerPrefsConst.DPQuality, quality);
    }
    /// <summary>
    /// 后处理 0,1
    /// </summary>
    public static void SetPostProcess(int lv)
    {
        var uac = Camera.main.GetComponent<UniversalAdditionalCameraData>();
        uac.renderPostProcessing = lv == 1;
        quality = quality & 0xDFFF | Mathf.Clamp(lv, 0, 1) << 13;
        GamePlayerPrefs.SetInt(PlayerPrefsConst.DPQuality, quality);
    }
    /// <summary>
    /// shader计算 0,1,2
    /// </summary>
    public static void SetGraphicsQualityLv(int lv)
    {
        switch (lv)
        {
            case 0:
                Shader.EnableKeyword("LowQuality");
                Shader.DisableKeyword("MediumQuality");
                Shader.DisableKeyword("HighQuality");
                break;
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
        }
        quality = quality & 0x3FFF | Mathf.Clamp(lv, 0, 2) << 14;
        GamePlayerPrefs.SetInt(PlayerPrefsConst.DPQuality, quality);
    }
}