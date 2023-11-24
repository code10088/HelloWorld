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

    #region ΢�ŵ�¼
    private Action<string> wechatLoginCallback;
    public void OnOpenWechatLogin(Action<string> callBack)
    {
        if (!initResult)
        {
            InitSDK();
        }
        if (!initResult)
        {
            //content="-1" sdk��ʼ��ʧ��
            callBack("-1");
            return;
        }
        wechatLoginCallback = callBack;
#if UNITY_ANDROID
        bool result = androidActivity.Call<bool>("WeChatLogin");
        //content="-2" ΢�ų�ʼ��ʧ��
        if (!result) callBack("-2");
#endif
    }
    public void WXLoginCallback_Android(string content)
    {
        //content="-3" �û��ܾ�
        //content="-4" �û�ȡ��
        wechatLoginCallback?.Invoke(content);
    }
    #endregion

    public void Log(string s)
    {
        GameDebug.LogError(s);
    }
}
