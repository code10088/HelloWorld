namespace YooAsset.Editor
{
    [DisplayName("�ռ�UserData������Դ")]
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