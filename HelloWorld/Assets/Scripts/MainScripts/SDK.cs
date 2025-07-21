using System;
using System.Collections.Generic;

#if WEIXINMINIGAME
using WeChatWASM;
#elif DOUYINMINIGAME
using TTSDK;
using TTSDK.UNBridgeLib.LitJson;
#endif

public enum ShowAdResult
{
    Fail,
    Loading,
    Success,
}
public class SDK : MonoSingletion<SDK>
{
#if UNITY_ANDROID
    private AndroidJavaObject androidActivity;
    private bool initResult = false;
    public void InitSDK()
    {
        androidActivity = new AndroidJavaObject("com.qs.HelloWorld.UnityActivity");
        initResult = androidActivity.Call<bool>("Init");
    }

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
        bool result = androidActivity.Call<bool>("WeChatLogin");
        //content="-2" 微信初始化失败
        if (!result) callBack("-2");
    }
    public void WXLoginCallback_Android(string content)
    {
        //content="-3" 用户拒绝
        //content="-4" 用户取消
        wechatLoginCallback?.Invoke(content);
    }

    public void Log(string s)
    {
        GameDebug.LogError(s);
    }
#endif

#if WEIXINMINIGAME
    /// <summary>
    /// 开放域数据
    /// </summary>
    public void PostMessage(string str)
    {
        WX.GetOpenDataContext().PostMessage(str);
    }

    public int WXCreateRewardedVideoAd(string adUnitId)
    {
        var item = new WXRewardedVideoAdItem();
        item.Init(++uniqueId, adUnitId);
        ads.Add(uniqueId, item);
        return uniqueId;
    }
    /// <summary>
    /// 微信PC端分辨率是Screen的1/2
    /// 原生模板广告宽是高的3倍
    /// </summary>
    public int WXCreateCustomAd(string adUnitId, int adIntervals, int left, int top, int width)
    {
        var item = new WXCustomAdItem();
        item.Init(++uniqueId, adUnitId, adIntervals, left, top, width);
        ads.Add(uniqueId, item);
        return uniqueId;
    }
    class WXRewardedVideoAdItem : AdItem
    {
        private WXRewardedVideoAd video;
        public void Init(int id, string adUnitId)
        {
            this.id = id;
            this.adUnitId = adUnitId;
            var param = new WXCreateRewardedVideoAdParam();
            param.adUnitId = adUnitId;
            param.multiton = true;
            video = WX.CreateRewardedVideoAd(param);
            video.onErrorAction = OnError;
            video.onLoadActon = OnLoad;
            video.onCloseAction = OnClose;
        }
        public override ShowAdResult Show(Action<bool> onClose = null)
        {
            if (load)
            {
                this.onClose = onClose;
                video.Show(null, OnError);
                load = false;
                retry = 0;
                return ShowAdResult.Success;
            }
            else
            {
                return ShowAdResult.Loading;
            }
        }
        private void OnError(WXBaseResponse response)
        {
            GameDebug.LogError($"错误：{adUnitId} {response.errMsg}");
            OnClose(null);
            if (retry < 3)
            {
                video.Load(null, OnError);
                retry++;
            }
            else
            {
                Destroy();
            }
        }
        private void OnLoad(WXADLoadResponse response)
        {
            if (string.IsNullOrEmpty(response.errMsg))
            {
                load = true;
                retry = 0;
            }
            else
            {
                OnError(response);
            }
        }
        private void OnClose(WXRewardedVideoAdOnCloseResponse response)
        {
            onClose?.Invoke(response != null && response.isEnded);
            onClose = null;
        }
        public override void Destroy()
        {
            base.Destroy();
            video.Destroy();
            video = null;
        }
    }
    class WXCustomAdItem : AdItem
    {
        private WXCustomAd custom;
        public void Init(int id, string adUnitId, int adIntervals, int left, int top, int width)
        {
            this.id = id;
            this.adUnitId = adUnitId;
            var param = new WXCreateCustomAdParam();
            param.adUnitId = adUnitId;
            param.adIntervals = adIntervals;
            param.style = new CustomStyle() { left = left, top = top, width = width };
            custom = WX.CreateCustomAd(param);
            custom.onErrorAction = OnError;
            custom.onLoadActon = OnLoad;
            custom.onCloseAction = OnClose;
        }
        public override ShowAdResult Show(Action<bool> onClose = null)
        {
            if (load)
            {
                this.onClose = onClose;
                custom.Show(null, OnError);
                return ShowAdResult.Success;
            }
            else
            {
                return ShowAdResult.Loading;
            }
        }
        public override void Hide()
        {
            custom.Hide();
        }
        private void OnError(WXBaseResponse response)
        {
            GameDebug.LogError($"错误：{adUnitId} {response.errMsg}");
            OnClose();
            Destroy();
        }
        private void OnLoad(WXADLoadResponse response)
        {
            if (string.IsNullOrEmpty(response.errMsg))
            {
                load = true;
            }
            else
            {
                OnError(response);
            }
        }
        private void OnClose()
        {
            onClose?.Invoke(true);
            onClose = null;
        }
        public override void Destroy()
        {
            base.Destroy();
            custom.Destroy();
            custom = null;
        }
    }
#endif

#if DOUYINMINIGAME
    public void InitSDK()
    {
        TT.InitSDK();
    }

    private bool loginState = false;
    public bool LoginState => loginState;
    public void Login(Action<bool> callback)
    {
        TT.Login((code, anonymousCode, isLogin) =>
        {
            loginState = true;
            callback?.Invoke(true);
        },
        err =>
        {
            loginState = false;
            callback?.Invoke(false);
        });
    }

    public void TTNavigateToScene()
    {
        var data = JsonMapper.ToObject("{\"scene\":\"sidebar\"}");
        TT.NavigateToScene(data, null, null, null);
    }

    public int TTCreateRewardedVideoAd(string adUnitId)
    {
        var item = new TTRewardedVideoAdItem();
        item.Init(++uniqueId, adUnitId);
        ads.Add(uniqueId, item);
        return uniqueId;
    }
    public int TTCreateBannerAd(string adUnitId, int adIntervals, int left, int top, int width)
    {
        var item = new TTBannerAdItem();
        item.Init(++uniqueId, adUnitId, adIntervals, left, top, width);
        ads.Add(uniqueId, item);
        return uniqueId;
    }
    class TTRewardedVideoAdItem : AdItem
    {
        private TTRewardedVideoAd video;
        public void Init(int id, string adUnitId)
        {
            this.id = id;
            this.adUnitId = adUnitId;
            var param = new CreateRewardedVideoAdParam();
            param.AdUnitId = adUnitId;
            param.Multiton = false;
            param.MultitonRewardMsg = null;
            param.MultitonRewardTimes = 0;
            param.ProgressTip = false;
            video = TT.CreateRewardedVideoAd(param);
            video.OnError += OnError;
            video.OnLoad += OnLoad;
            video.OnClose += OnClose;
        }
        public override ShowAdResult Show(Action<bool> onClose = null)
        {
            if (load)
            {
                this.onClose = onClose;
                video.Show();
                load = false;
                retry = 0;
                return ShowAdResult.Success;
            }
            else
            {
                return ShowAdResult.Loading;
            }
        }
        private void OnError(int code, string message)
        {
            GameDebug.LogError($"错误：{adUnitId} {code} {message}");
            OnClose(false, 0);
            if (retry < 3)
            {
                video.Load();
                retry++;
            }
            else
            {
                Destroy();
            }
        }
        private void OnLoad()
        {
            load = true;
            retry = 0;
        }
        private void OnClose(bool isEnded, int count)
        {
            onClose?.Invoke(isEnded);
            onClose = null;
        }
        public override void Destroy()
        {
            base.Destroy();
            video.Destroy();
            video = null;
        }
    }
    class TTBannerAdItem : AdItem
    {
        private TTBannerAd banner;
        public void Init(int id, string adUnitId, int adIntervals, int left, int top, int width)
        {
            this.id = id;
            this.adUnitId = adUnitId;
            var param = new CreateBannerAdParam();
            param.BannerAdId = adUnitId;
            param.AdIntervals = adIntervals;
            param.Style = new TTBannerStyle() { left = left, top = top, width = width };
            banner = TT.CreateBannerAd(param);
            banner.OnError += OnError;
            banner.OnLoad += OnLoad;
            banner.OnClose += OnClose;
        }
        public override ShowAdResult Show(Action<bool> onClose = null)
        {
            if (load)
            {
                this.onClose = onClose;
                banner.Show();
                return ShowAdResult.Success;
            }
            else
            {
                return ShowAdResult.Loading;
            }
        }
        public override void Hide()
        {
            banner.Hide();
        }
        private void OnError(int code, string message)
        {
            GameDebug.LogError($"错误：{adUnitId} {code} {message}");
            OnClose();
            Destroy();
        }
        private void OnLoad()
        {
            load = true;
        }
        private void OnClose()
        {
            onClose?.Invoke(true);
            onClose = null;
        }
        public override void Destroy()
        {
            base.Destroy();
            banner.Destroy();
            banner = null;
        }
    }
#endif

    #region 广告
    private int uniqueId = 0;
    private Dictionary<int, AdItem> ads = new Dictionary<int, AdItem>();

    public ShowAdResult Show(int id, Action<bool> onClose = null)
    {
        if (ads.TryGetValue(id, out var item)) return item.Show(onClose);
        else return ShowAdResult.Fail;
    }
    public void Hide(int id)
    {
        if (ads.TryGetValue(id, out var item)) item.Hide();
    }
    public void Destroy(int id)
    {
        if (ads.TryGetValue(id, out var item)) item.Destroy();
    }
    class AdItem
    {
        protected int id;
        protected string adUnitId;
        protected bool load = true;
        protected int retry = 0;
        protected Action<bool> onClose;

        public virtual ShowAdResult Show(Action<bool> onClose = null)
        {
            return ShowAdResult.Success;
        }
        public virtual void Hide()
        {

        }
        public virtual void Destroy()
        {
            onClose = null;
            if (Instance.ads.ContainsKey(id)) Instance.ads.Remove(id);
        }
    }
    #endregion
}
