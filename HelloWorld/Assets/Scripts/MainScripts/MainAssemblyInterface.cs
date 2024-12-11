using System;
using WeChatWASM;

public static class MainAssemblyInterface
{
    #region н╒пе
    public static void PostMessage(string str)
    {
        WX.GetOpenDataContext().PostMessage(str);
    }

    private static Action<string, int> weChatAdError;
    private static Action<string, string> weChatAdLoad;
    private static Action<string, bool> weChatAdClose;
    public static void InitWeChatAd(Action<string, int> _weChatAdError, Action<string, string> _weChatAdLoad, Action<string, bool> _weChatAdClose)
    {
        weChatAdError = _weChatAdError;
        weChatAdLoad = _weChatAdLoad;
        weChatAdClose = _weChatAdClose;
    }
    public static string InitWeChatAd(string adUnitId)
    {
        var param = new WXCreateRewardedVideoAdParam();
        param.adUnitId = adUnitId;
        param.multiton = true;
        var ad = WX.CreateRewardedVideoAd(param);
        ad.onErrorAction = OnError;
        ad.onLoadActon = OnLoad;
        ad.onCloseAction = OnClose;
        ad.Load();
        return ad.instanceId;
    }
    private static void OnError(WXADErrorResponse response)
    {
        weChatAdError?.Invoke(response.callbackId, response.errCode);
    }
    private static void OnLoad(WXADLoadResponse response)
    {
        weChatAdLoad?.Invoke(response.callbackId, response.errMsg);
    }
    private static void OnClose(WXRewardedVideoAdOnCloseResponse response)
    {
        weChatAdClose?.Invoke(response.callbackId, response.isEnded);
    }
    public static void LoadWeChatAd(string instanceId)
    {
        if (WXBaseAd.Dict.TryGetValue(instanceId, out WXBaseAd ad)) ((WXRewardedVideoAd)ad).Load();
        else weChatAdLoad?.Invoke(instanceId, "init fail");
    }
    public static void ShowWeChatAd(string instanceId)
    {
        if (WXBaseAd.Dict.TryGetValue(instanceId, out WXBaseAd ad)) ad.Show();
        else weChatAdError?.Invoke(instanceId, -100);
    }
    public static void DestroyWeChatAd(string instanceId)
    {
        if (WXBaseAd.Dict.TryGetValue(instanceId, out WXBaseAd ad)) ad.Destroy();
    }
    #endregion
}
