using System;
using UnityEngine;

public class CustomerPreferenceData : ScriptableObject
{
    public string BuildPlayerPath;
    public string LubanPath;
    public string MsgPath;
    public CosBucketConfig CosBucketConfig = new CosBucketConfig();
}
[Serializable]
public class CosBucketConfig
{
    public string Name;
    public string SecretId;
    public string SecretKey;
}