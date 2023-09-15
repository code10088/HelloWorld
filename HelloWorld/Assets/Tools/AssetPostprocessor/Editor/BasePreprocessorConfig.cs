using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace AssetPreprocessor.Scripts.Editor
{
    public class BasePreprocessorConfig : ScriptableObject
    {
        [Header("设置")]
        public bool IsEnabled = true;
        public bool ForcePreprocess;
        [Tooltip("值小优先")]
        public int ConfigSortOrder = 10;

        [Header("正则匹配")]
        public List<string> MatchList = new List<string>();
        public List<string> IgnoreList = new List<string>();

        public bool Check(AssetImporter assetImporter)
        {
            if (!IsEnabled) return false;
            if (!Match(MatchList, assetImporter.assetPath)) return false;
            if (Match(IgnoreList, assetImporter.assetPath)) return false;
            return true;
        }
        public bool Match(List<string> list, string target)
        {
            for (int i = 0; i < list.Count; i++)
            {
                string s = Regex.Escape(list[i]).Replace("\\*", ".*?");
                bool b = new Regex(s).IsMatch(target);
                if (b) return true;
            }
            return false;
        }
    }
}
