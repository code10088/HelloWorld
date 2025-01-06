using System;
using UnityEngine;

public class CustomerPreferenceData : ScriptableObject
{
    public string BuildPlayerPath;
    public string MSBuildPath;
    public string LubanPath;
    public CosBucketConfig CosBucketConfig = new CosBucketConfig();
}
[Serializable]
public class CosBucketConfig
{
    public string Name;
    public string SecretId;
    public string SecretKey;
}