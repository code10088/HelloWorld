package com.qs.helloworld.sdk;

import android.app.Activity;
import android.content.Context;
import android.content.Intent;

import com.tencent.tauth.Tencent;

public class QQSDK {

    public static final String APP_ID = "1906400845";

    private static Tencent mTencent;

    public static Tencent getTencent(Context context) {
        if (mTencent == null) {
            Tencent.setIsPermissionGranted(true);
            mTencent = Tencent.createInstance(APP_ID, context.getApplicationContext());
        }
        return mTencent;
    }

    public static void Login(Activity activity) {
        activity.startActivity(new Intent(activity, QQLoginActivity.class));
    }
}