using Luban;
using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public enum LanguageType
{
    CN = 0,
    EN,

    Max
}
public class LanguageManager : Singletion<LanguageManager>
{
    private LanguageType languageType;
    private Dictionary<int, string> languageDic = new Dictionary<int, string>();
    private Action finish;

    public LanguageType LanguageType => languageType;

    public void Init(Action finish)
    {
        this.finish = finish;
        int type = GamePlayerPrefs.GetInt(PlayerPrefsConst.Language, -1);
        if (type < 0)
        {
            switch (Application.systemLanguage)
            {
                case SystemLanguage.Chinese: languageType = LanguageType.CN; break;
                case SystemLanguage.English: languageType = LanguageType.EN; break;
                default: languageType = LanguageType.EN; break;
            }
        }
        else
        {
            languageType = (LanguageType)type;
        }
        int loadId = -1;
        string tempPath = $"{ZResConst.ResDataConfigPath}tblanguage{languageType.ToString().ToLower()}.bytes";
        AssetManager.Instance.Load<TextAsset>(ref loadId, tempPath, Deserialize);
    }
    private void Deserialize(int loadId, Object asset)
    {
        byte[] bytes = ((TextAsset)asset).bytes;
        Driver.Instance.StartTask(Deserialize, finish, bytes);
        AssetManager.Instance.Unload(ref loadId);
    }
    private void Deserialize(object o)
    {
        ByteBuf buf = new ByteBuf((byte[])o);
        int n = buf.ReadSize();
        for (int i = n; i > 0; --i)
        {
            int key = buf.ReadInt();
            string value = buf.ReadString();
            languageDic[key] = value;
        }
    }

    public void ChangeLanguage(LanguageType type)
    {
        languageType = type;
        GamePlayerPrefs.SetInt(PlayerPrefsConst.Language, (int)type);
    }
    public string Get(int key, params object[] args)
    {
        string result = languageDic[key];
        if (args == null || args.Length == 0)
        {
            return result;
        }
        try
        {
            return string.Format(result, args);
        }
        catch (FormatException)
        {
            return result; 
        }
    }
}
