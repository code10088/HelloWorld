package com.qs.helloworld.sdk;

import android.app.Activity;
import android.content.Intent;
import android.os.Bundle;

import com.tencent.tauth.IUiListener;
import com.tencent.tauth.Tencent;
import com.tencent.tauth.UiError;

import org.json.JSONObject;

public class QQLoginActivity extends Activity {

    private final IUiListener mListener = new IUiListener() {
        @Override
        public void onComplete(Object response) {
            JSONObject json = (JSONObject) response;
            Callback(json.optInt("ret") + "|" + json.optString("openid") + "|"
                    + json.optString("access_token") + "|" + json.optString("expires_in"));
        }

        @Override
        public void onError(UiError error) {
            Callback("-4|" + error.errorCode + "|" + error.errorMessage);
        }

        @Override
        public void onCancel() {
            Callback("-3");
        }

        @Override
        public void onWarning(int code) {
        }
    };

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        QQSDK.getTencent(this).login(this, "get_user_info", mListener);
    }

    @Override
    protected void onActivityResult(int requestCode, int resultCode, Intent data) {
        Tencent.onActivityResultData(requestCode, resultCode, data, mListener);
    }

    private void Callback(String result) {
        UnityActivity.CallUnity("QQLoginCallback", result);
        finish();
    }
}