using System;
using UnityEngine;

public class SDK : MonoSingleton<SDK>
{
    private AndroidJavaObject androidActivity;
    private bool initResult = false;

    public void InitSDK()
    {
#if UNITY_ANDROID
        androidActivity = new AndroidJavaObject("com.qs.HelloWorld.UnityActivity");
        initResult = androidActivity.Call<bool>("Init");
#endif
    }

    #region 微信登录
    private Action<string> wechatLoginCallback;
    public void OnOpenWechatLogin(Action<string> callBack)
    {
        if (!initResult)
        {
            InitSDK();
        }
        if (!initResult)
        {
            //content="-1" sdk初始化失败
            callBack("-1");
            return;
        }
        wechatLoginCallback = callBack;
#if UNITY_ANDROID
        bool result = androidActivity.Call<bool>("WeChatLogin");
        //content="-2" 微信初始化失败
        if (!result) callBack("-2");
#endif
    }
    public void WXLoginCallback_Android(string content)
    {
        //content="-3" 用户拒绝
        //content="-4" 用户取消
        wechatLoginCallback?.Invoke(content);
    }
    #endregion

    public void Log(string s)
    {
        GameDebug.LogError(s);
    }
}
