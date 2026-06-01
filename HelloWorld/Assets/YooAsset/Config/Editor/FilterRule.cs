namespace YooAsset.Editor
{
    [DisplayName("收集UserData所有资源")]
    public class CollectUserData : IAssetFilterRule
    {
        public string FindAssetType => EAssetFilterType.All.ToString();

        public bool IsCollectAsset(AssetFilterRuleData data)
        {
            string[] strs = data.UserData.Split(';', System.StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < strs.Length; i++)
            {
                if (data.AssetPath.Contains(strs[i])) return true;
            }
            return false;
        }
    }
}