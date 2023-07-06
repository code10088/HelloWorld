package com.qs.HelloWorld.wxapi;

import android.app.Activity;
import android.content.Intent;
import android.os.Bundle;

import com.qs.HelloWorld.UnityActivity;
import com.qs.HelloWorld.WeChatSDK;
import com.tencent.mm.opensdk.constants.ConstantsAPI;
import com.tencent.mm.opensdk.modelbase.BaseReq;
import com.tencent.mm.opensdk.modelbase.BaseResp;
import com.tencent.mm.opensdk.modelmsg.SendAuth;
import com.tencent.mm.opensdk.openapi.IWXAPI;
import com.tencent.mm.opensdk.openapi.IWXAPIEventHandler;
import com.tencent.mm.opensdk.openapi.WXAPIFactory;

public class WXEntryActivity extends Activity implements IWXAPIEventHandler {

    private IWXAPI api;

    public void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);

        api = WXAPIFactory.createWXAPI(this, WeChatSDK.WX_APP_ID);
        api.handleIntent(getIntent(), this);
    }

    @Override
    protected void onNewIntent(Intent intent) {
        super.onNewIntent(intent);
        setIntent(intent);
        api.handleIntent(getIntent(), this);
    }

    //微信发送请求到第三方应用时，会回调到该方法
    @Override
    public void onReq(BaseReq req) {
        switch (req.getType()) {
            case ConstantsAPI.COMMAND_GETMESSAGE_FROM_WX:
                break;
            case ConstantsAPI.COMMAND_SHOWMESSAGE_FROM_WX:
                break;
            case ConstantsAPI.COMMAND_LAUNCH_BY_WX:
                break;
            default:
                break;
        }
    }

    //第三方应用发送到微信的请求处理后的响应结果，会回调到该方法
    @Override
    public void onResp(BaseResp resp) {
        switch (resp.getType()) {
            case ConstantsAPI.COMMAND_LAUNCH_BY_WX:
                break;
            case ConstantsAPI.COMMAND_SENDAUTH:
                onSendAuthResp(resp);
                break;
            case ConstantsAPI.COMMAND_PAY_BY_WX:
                break;
            default:
                break;
        }
        finish();
    }

    public void onSendAuthResp(BaseResp resp) {
        int errorCode = resp.errCode;
        switch (errorCode) {
            case BaseResp.ErrCode.ERR_OK:
                //用户同意
                String code = ((SendAuth.Resp) resp).code;
                UnityActivity.CallUnity("WXLoginCallback_Android", code);
                break;
            case BaseResp.ErrCode.ERR_AUTH_DENIED:
                //用户拒绝
                UnityActivity.CallUnity("WXLoginCallback_Android", "-3");
                break;
            case BaseResp.ErrCode.ERR_USER_CANCEL:
                //用户取消
                UnityActivity.CallUnity("WXLoginCallback_Android", "-4");
                break;
        }
    }
}