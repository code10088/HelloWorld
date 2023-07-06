package com.qs.HelloWorld;

import android.app.Activity;

import java.lang.reflect.InvocationTargetException;
import java.lang.reflect.Method;

public class UnityActivity {

    public static Activity unityActivity;

    public boolean Init() {
        try {
            Class<?> classtype = Class.forName("com.unity3d.player.UnityPlayer");
            Activity activity = (Activity) classtype.getDeclaredField("currentActivity").get(classtype);
            unityActivity = activity;
            CallUnity("Log", "SDKInitSuccess");
            return true;
        } catch (ClassNotFoundException e) {
            CallUnity("Log", e.toString());
            return false;
        } catch (IllegalAccessException e) {
            CallUnity("Log", e.toString());
            return false;
        } catch (NoSuchFieldException e) {
            CallUnity("Log", e.toString());
            return false;
        }
    }

    public static boolean CallUnity(String functionName, String args) {
        try {
            Class<?> classtype = Class.forName("com.unity3d.player.UnityPlayer");
            Method method = classtype.getMethod("UnitySendMessage", String.class, String.class, String.class);
            method.invoke(classtype, "SDK", functionName, args);
            return true;
        } catch (ClassNotFoundException e) {
            CallUnity("Log", e.toString());
        } catch (NoSuchMethodException e) {
            CallUnity("Log", e.toString());
        } catch (IllegalAccessException e) {
            CallUnity("Log", e.toString());
        } catch (InvocationTargetException e) {
            CallUnity("Log", e.toString());
        }
        return false;
    }

    //微信登录
    public boolean WeChatLogin() {
        return WeChatSDK.WechatLogin();
    }
}
