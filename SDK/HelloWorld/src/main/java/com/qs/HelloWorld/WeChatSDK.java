package com.qs.HelloWorld;

import com.tencent.mm.opensdk.modelmsg.SendAuth;
import com.tencent.mm.opensdk.openapi.IWXAPI;
import com.tencent.mm.opensdk.openapi.WXAPIFactory;

public class WeChatSDK {

    public static String WX_APP_ID = "***************";//从微信开放平台申请得到的APPID
    private static IWXAPI mWxApi;
    private static boolean register = false;

    private static boolean Check() {
        if (register) return true;
        if (mWxApi == null) mWxApi = WXAPIFactory.createWXAPI(UnityActivity.unityActivity, WX_APP_ID);
        if (mWxApi == null) return false;
        if (!mWxApi.isWXAppInstalled()) return false;
        mWxApi.registerApp(WX_APP_ID);
        register = true;
        return true;
    }

    //调用微信登录
    public static boolean WechatLogin() {
        if (Check()) {
            SendAuth.Req req = new SendAuth.Req();
            req.scope = "snsapi_userinfo";
            req.state = "wechat_sdk";
            mWxApi.sendReq(req);
            UnityActivity.CallUnity("Log", "微信发起登录申请成功");
            return true;
        } else {
            UnityActivity.CallUnity("Log", "微信初始化失败");
            return false;
        }
    }
}
