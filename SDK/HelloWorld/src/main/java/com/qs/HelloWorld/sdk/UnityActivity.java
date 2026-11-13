package com.qs.helloworld.sdk;

import android.app.Activity;

public class UnityActivity {

    public static Activity CurrentActivity() {
        try {
            Class<?> cls = Class.forName("com.unity3d.player.UnityPlayer");
            return (Activity) cls.getDeclaredField("currentActivity").get(cls);
        } catch (Exception e) {
            e.printStackTrace();
            return null;
        }
    }

    public static void CallUnity(String functionName, String args) {
        try {
            Class<?> cls = Class.forName("com.unity3d.player.UnityPlayer");
            cls.getMethod("UnitySendMessage", String.class, String.class, String.class)
                    .invoke(cls, "SDK", functionName, args);
        } catch (Exception e) {
            e.printStackTrace();
        }
    }

    //QQ登录
    public static void QQLogin() {
        QQSDK.Login(CurrentActivity());
    }
}