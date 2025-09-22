namespace YooAsset.Editor
{
    [DisplayName("收集UserData所有资源")]
    public class CollectUserData : IFilterRule
    {
        public string FindAssetType => EAssetSearchType.All.ToString();

        public bool IsCollectAsset(FilterRuleData data)
        {
            string[] strs = data.UserData.Split(';', System.StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < strs.Length; i++)
            {
                if (data.AssetPath.Contains(strs[i]))
                    return true;
            }
            return false;
        }
    }
}