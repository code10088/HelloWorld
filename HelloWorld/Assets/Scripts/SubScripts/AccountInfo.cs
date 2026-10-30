using cfg;
using System;
using UnityEngine;

public enum LoginPlatform
{
    QQ,
}
public enum LoginCode
{
    Unknown = -1,
    Success,
    Maintenance = 10010,
    Banned = 10011,
}
[Serializable]
public class LoginRequest
{
    public int platform;
    public string authCode;
    public string deviceId;
    public string clientVersion;
}
[Serializable]
public class LoginResponse
{
    public int code;
    public string accessToken;
    public string refreshToken;
    public string userId;
    public string serverId;
}
[Serializable]
public class RefreshTokenRequest
{
    public string refreshToken;
}
public class AccountInfo : Singleton<AccountInfo>
{
    private const int AccountDataVersion = 1;
    private Action loginCallback;

    public string AccessToken { get; private set; }
    public string RefreshToken { get; private set; }
    public string UserId { get; private set; }
    public string ServerId { get; private set; }

    public void Login(Action loginCallback)
    {
        this.loginCallback = loginCallback;
        FileUtils.Read(FileUtils.PlatformPath + "account.data", Login);
    }
    private void Login(UnsafeByteBuffer buffer)
    {
        AccessToken = string.Empty;
        RefreshToken = string.Empty;
        UserId = string.Empty;
        ServerId = string.Empty;
        if (buffer == null || buffer.WPos == 0)
        {
            UIManager.Instance.OpenUI(UIType.UILogin);
            return;
        }
        if (buffer.ReadInt() == AccountDataVersion)
        {
            AccessToken = buffer.ReadString();
            RefreshToken = buffer.ReadString();
            UserId = buffer.ReadString();
            ServerId = buffer.ReadString();
        }
        if (string.IsNullOrEmpty(RefreshToken))
        {
            UIManager.Instance.OpenUI(UIType.UILogin);
            return;
        }
        var request = new RefreshTokenRequest { refreshToken = RefreshToken };
        var bytes = System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(request));
        HttpManager.Instance.Post($"{GameSetting.LoginUrl}auth/refresh", bytes, LoginHandle, GameSetting.timeoutS);
    }
    public void LoginWithCode(LoginPlatform platform, string authCode)
    {
        var request = new LoginRequest
        {
            platform = (int)platform,
            authCode = authCode,
            deviceId = SystemInfo.deviceUniqueIdentifier,
            clientVersion = Application.version
        };
        var bytes = System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(request));
        HttpManager.Instance.Post($"{GameSetting.LoginUrl}auth/login", bytes, LoginHandle, GameSetting.timeoutS);
    }
    private void LoginHandle(byte[] data)
    {
        if (data == null || data.Length == 0)
        {
            UICommonTips.ShowTips(LanguageManager.Instance.Get(10008));
            UIManager.Instance.OpenUI(UIType.UILogin);
            return;
        }
        LoginResponse response;
        try
        {
            response = JsonUtility.FromJson<LoginResponse>(System.Text.Encoding.UTF8.GetString(data));
        }
        catch (Exception exception)
        {
            UICommonTips.ShowTips(exception.Message);
            UIManager.Instance.OpenUI(UIType.UILogin);
            return;
        }
        if (response == null || response.code != 1)
        {
            UICommonTips.ShowTips(LanguageManager.Instance.Get(response.code));
            UIManager.Instance.OpenUI(UIType.UILogin);
            return;
        }
        switch ((LoginCode)response.code)
        {
            case LoginCode.Unknown:
                UICommonTips.ShowTips(LanguageManager.Instance.Get(response.code));
                UIManager.Instance.OpenUI(UIType.UILogin);
                break;
            case LoginCode.Success:
                AccessToken = response.accessToken;
                RefreshToken = response.refreshToken;
                UserId = response.userId;
                ServerId = response.serverId;
                SaveAccount();
                loginCallback?.Invoke();
                break;
            case LoginCode.Maintenance:
                UICommonBoxParam param = new UICommonBoxParam();
                param.type = UICommonBoxType.Sure;
                param.title = "Tips";
                param.content = LanguageManager.Instance.Get(response.code);
                param.sure = a => UIManager.Instance.OpenUI(UIType.UILogin);
                UICommonBox.OpenCommonBox(param);
                break;
            case LoginCode.Banned:
                UICommonTips.ShowTips(LanguageManager.Instance.Get(response.code));
                UIManager.Instance.OpenUI(UIType.UILogin);
                break;
        }
    }
    private void SaveAccount()
    {
        UnsafeByteBuffer buffer = UnsafeByteBuffer.Rent(256);
        buffer.WriteInt(AccountDataVersion);
        buffer.WriteString(AccessToken);
        buffer.WriteString(RefreshToken);
        buffer.WriteString(UserId);
        buffer.WriteString(ServerId);
        FileUtils.Write(FileUtils.PlatformPath + "account.data", buffer);
    }
}
