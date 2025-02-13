using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class TTAPIUpdate
{
    private static string noteString = "";
    public static void UpdateAPI(string path)
    {
        noteString = "";
        Debug.Log("path " + path);
        
        string[] files = Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories);

        for (int i = 0; i < files.Length; ++i)
        {
            NoteInFile(files[i]);
            Console.WriteLine(files[i]);
            
            ReplaceInFile(files[i], @"using\sStarkSDKSpace", "using TTSDK");
            ReplaceInFile(files[i], @"using\sStarkSDKSpace", "using TTSDK");
            ReplaceInFile(files[i], @"namespace\sStarkSDKSpace", "namespace TTSDK");
            ReplaceInFile(files[i], @"StarkSDKSpace\.", "TTSDK.");
            ReplaceInFile(files[i], "EnableStarkSDKDebugToast", "EnableTTSDKDebugToast");
            ReplaceInFile(files[i], @"StarkSDK\.API.Init\(\)", "TT.InitSDK()");
            ReplaceInFile(files[i], @"StarkSDK\.API.", "TT.");
            ReplaceInFile(files[i], @"StarkSDK\.", @"TT.");

            ReplaceInFile(files[i], "StarkAudioManager", "TTAudioManager");
            ReplaceInFile(files[i], "StarkAdManager", "TTAdManager");
            ReplaceInFile(files[i], @"\.GetStarkAdManager\(\).", ".");

            ReplaceInFile(files[i], @"TTAdManager\.BannerAd", "TTBannerAd");
            ReplaceInFile(files[i], @"TTAdManager\.BannerStyle", "TTBannerStyle");
            ReplaceInFile(files[i], @"\.GetStarkDeviceMotion\(\)", "");
            ReplaceInFile(files[i], "StarkDeviceMotion", "TTDeviceMotion");

            ReplaceInFile(files[i], "GetStarkDouyinCloudManager", "CreateCloud");
            ReplaceInFile(files[i], "StarkDouyinCloud", "DouyinCloud");
            ReplaceInFile(files[i], "GetScUserInfo", "GetUserInfo");
            ReplaceInFile(files[i], "ScUserInfo ", "TTUserInfo ");
            ReplaceInFile(files[i], @"\.GetAccountManager\(\)", "");
            ReplaceInFile(files[i], "GetStarkFileSystemManager", "GetFileSystemManager");
            ReplaceInFile(files[i], "StarkUIManager", "TTUIManager");
            ReplaceInFile(files[i], @"\sStarkGameRecorder\s", @" TTGameRecorder ");
            ReplaceInFile(files[i], @"\sStarkGameRecorder\.", " TTGameRecorder.");
            ReplaceInFile(files[i], @"<StarkGameRecorder\.", "<TTGameRecorder.");
            ReplaceInFile(files[i], @"\.StarkGameRecorder\.", ".TTGameRecorder.");
            ReplaceInFile(files[i], @"GetStarkGameRecorder\(\)", "GetGameRecorder()");
            ReplaceInFile(files[i], "EnableStarkSDKDebugToast", "EnableTTSDKDebugToast");
            ReplaceInFile(files[i], @"\.GetStarkShare\(\)", "");
            ReplaceInFile(files[i], @"\.StopRecord", ".Stop");
            ReplaceInFile(files[i], @"\.StartRecord", ".Start");

            ReplaceInFile(files[i], @"\.GetStarkGoldCoinGameManager\(\)", "");
            ReplaceInFile(files[i], @"\.GetStarkRequest\(\)", "");
            ReplaceInFile(files[i], @"GetStarkRequest", "GetTTRequest");
            ReplaceInFile(files[i], "StarkRequest", "TTRequest");
            ReplaceInFile(files[i], @"\.InnerRequest", ".Request");

            ReplaceInFile(files[i], @"\.GetStarkPayService\(\)", "");
            ReplaceInFile(files[i], "GetStarkPayService", "GetTTPayService");
            
            ReplaceInFile(files[i], @"CanIUse\.", "TTSDK.CanIUse.");
            ReplaceInFile(files[i], @"CanIUse\.StarkPayService", "CanIUse.TTPayService");
            ReplaceInFile(files[i], @"CanIUse\.TTGameRecorder\.StartRecord", "CanIUse.TTGameRecorder.Start");
            ReplaceInFile(files[i], @"CanIUse\.TTGameRecorder\.StopRecord", "CanIUse.TTGameRecorder.Stop");
            ReplaceInFile(files[i], @"CanIUse\.TTAccount\.GetScUserInfo", "CanIUse.TTAccount.GetUserInfo");
            ReplaceInFile(files[i], @"CanIUse\.StarkUtils", "CanIUse.TTUtils");
            ReplaceInFile(files[i], @"CanIUse\.StarkGyroscope", "CanIUse.TTGyroscope");

            ReplaceInFile(files[i], @"\.GetStarkRank\(\)", "");
            ReplaceInFile(files[i], @"\.SetImRankDataV2", ".SetImRankData");
            ReplaceInFile(files[i], @"\.GetImRankListV2", ".GetImRankList");
            ReplaceInFile(files[i], @"StarkRank\.", "TTRank.");
            ReplaceInFile(files[i], @"\.GetImRankDataV2", ".GetImRankData");
            ReplaceInFile(files[i], @"\.GetStarkScreenManager\(\)", "");
            ReplaceInFile(files[i], @"StarkShare\.", "TTShare.");

            ReplaceInFile(files[i], @"\.GetStarkSideBarManager\(\)", "");
            ReplaceInFile(files[i], @"StarkSideBar\.", "TTSideBar.");
            ReplaceInFile(files[i], "StarkInputOverrideBypass", "TTInputOverrideBypass");
            ReplaceInFile(files[i], "StarkUDPSocket", "TTUDPSocket");
            ReplaceInFile(files[i], @"\.StarkAccount\.", ".TTAccount.");


            ReplaceInFile(files[i], @"\.GetStarkAppLifeCycle\(\)", ".GetAppLifeCycle()");
            ReplaceInFile(files[i], @"GetAppLifeCycle\(\)\.onAppShow", "GetAppLifeCycle().OnShow");
            ReplaceInFile(files[i], @"GetAppLifeCycle\(\)\.OnShowWithDict ", "GetAppLifeCycle().OnShow");
            ReplaceInFile(files[i], @"GetAppLifeCycle\(\)\.onAppHide", "GetAppLifeCycle().OnHide");
            ReplaceInFile(files[i], @"\.StarkSendToTAQ", ".SendToTAQ");
            ReplaceInFile(files[i], @"StarkPlatform\.", "TTPlatform.");
            ReplaceInFile(files[i], @"\.GetStarkInput\(\)", "");
            ReplaceInFile(files[i], @"StarkInput\.", "TTInput.");
            ReplaceInFile(files[i], @"\.GetStarkClipboard\(\)", "");
            ReplaceInFile(files[i], @"StarkShare\.ShareParam", "TTShare.ShareParam");
            ReplaceInFile(files[i], @"\.GetStarkInvite\(\)", "");
            ReplaceInFile(files[i], @"\.onInviteStateChanged", ".OnInviteStateChanged");
            ReplaceInFile(files[i], @"\.ExitApp", ".ExitMiniProgram");
            ReplaceInFile(files[i], @"\.RestartApp", ".RestartMiniProgramSync");
            ReplaceInFile(files[i], @".GetStarkGyroscope\(\)", ".GetTTGyroscope()");
            ReplaceInFile(files[i], @"TTAdManager\.VideoAdCallback", "TTVideoAdCallback");
            ReplaceInFile(files[i], @".GetStarkHostEvent\(\)", "");
            ReplaceInFile(files[i], @"\.ShowVideoAdWithId", ".CreateRewardedVideoAd");

            ReplaceInFile(files[i], @".GetStarkContainerVersion\(\)", ".GetContainerVersion()");
            ReplaceInFile(files[i], @"TT\.SDKVersion", "TT.TTSDKVersion");
            ReplaceInFile(files[i], @"\.GetStarkFavorite\(\)", "");
            ReplaceInFile(files[i], @"\.GetStarkKeyboard\(\)", "");

            ReplaceInFile(files[i], @"CanIUse\.StarkFavorite\.", "CanIUse.TTFavorite.");
            ReplaceInFile(files[i], @"StarkFavorite\.Style", "TTFavorite.Style");
            ReplaceInFile(files[i], @"IsFollowDouyin", "CheckFollowAwemeState");
            ReplaceInFile(files[i], @"HasBoundDouyin", "CheckBoundAweme");
            ReplaceInFile(files[i], @"\.FollowDouYinUserProfile", ".OpenAwemeUserProfile");

            ReplaceInFile(files[i], @"\.IsShortcutExist", ".CheckShortcut");
            ReplaceInFile(files[i], @"\.CreateShortcut", ".AddShortcut");
            ReplaceInFile(files[i], @"TTAdManager\.InterstitialAd", "TTInterstitialAd");
            ReplaceInFile(files[i], @"\.GetTTAdManager\(\)", "");
            ReplaceInFile(files[i], @"StarkFileSystemManager", "TTFileSystemManager");
            
            
            ReplaceInFile(files[i], @"\.onKeyboardInputEvent", ".OnKeyboardInput");
            ReplaceInFile(files[i], @"\.onKeyboardConfirmEvent", ".OnKeyboardConfirm");
            ReplaceInFile(files[i], @"\.onKeyboardCompleteEvent", ".OnKeyboardComplete");
            
            ReplaceInFile(files[i], @"StarkKeyboard\.", "TTKeyboard.");
            
            ReplaceInFile(files[i], @"\.GetStarkGroup\(\)", "");
            ReplaceInFile(files[i], @"StarkGroup\.", "TTGroup.");
            
            ReplaceInFile(files[i], @"\.GetStarkGridGamePanelManager", ".GetGridGamePanelManager");
            ReplaceInFile(files[i], @"StarkGridGamePanelManager", "TTGridGamePanelManager");
            ReplaceInFile(files[i], @"StarkGridGamePanel", "TTGridGamePanel");
            
            ReplaceInFile(files[i], @"\.NavigateToSceneV2", ".NavigateToScene");
            
            ReplaceInFile(files[i], @"TTRequest\.Options", "TTRequest.InnerOptions");
            
        }
        string notePath = path + "/note.txt";
        noteString += "其他处理请参考官方文档站的 API 接口说明\n";
        File.WriteAllText(notePath, noteString);
        
        string str = "处理替换完成，剩下内容还需手动检查，可参考文件 " + notePath;
        Debug.Log(str);
        
        EditorUtility.DisplayDialog(str,  "ok","cancel");
        

    }
    
    static void ReplaceInFile(string filePath, string oldString, string newString)
    {
        // 读取文件内容
        string fileContents = File.ReadAllText(filePath);

        // 检查文件内容中是否包含指定字符串
        // if (!fileContents.Contains(oldString))
        // {
        //     return;
        // }
        
        // 替换指定字符串
        fileContents = Regex.Replace(fileContents, oldString, newString);
        
        // 将修改后的内容写回文件
        File.WriteAllText(filePath, fileContents);
    }
    

    static void NoteInFile(string filePath)
    {
        string fileContents = File.ReadAllText(filePath);
        if (fileContents.Contains(".SetImRankData"))
        {
            string str = "文件 " + filePath + " 中 SetImRankData 需要调整参数，参考 TT.SetImRankData 接口\n";
            noteString += str;
            Debug.Log(str);
        }

        if (fileContents.Contains("GetStarkShare()"))
        {
            string str = "文件 " + filePath + " 中 share 需要调整参数，参考 TT.Sharexxx 接口\n";
            noteString += str;
            Debug.Log(str);
        }
        
        if (fileContents.Contains(".ShowVideoAdWithId"))
        {
            string str = "文件 " + filePath + " 中 ShowVideoAdWithId 需要修改，参考 TT.CreateRewardedVideoAd 接口\n";
            noteString += str;
            Debug.Log(str);
        }

        if (fileContents.Contains(".SetContainerInitCallback"))
        {
            string str = "文件 " + filePath + " 中初始化回调接口需要调整，参考 TT.InitSDK 接口\n";
            noteString += str;
            Debug.Log(str);
        }
        
        
        if (fileContents.Contains("GetAppLifeCycle().onAppShow") || fileContents.Contains("GetAppLifeCycle().OnShowWithDict"))
        {
            string str = "文件 " + filePath + " onAppShow 监听接口需要添加参数 Dictionary<string, object> param\n";
            noteString += str;
            Debug.Log(str);
        }
        
        
        
    }
    

}
