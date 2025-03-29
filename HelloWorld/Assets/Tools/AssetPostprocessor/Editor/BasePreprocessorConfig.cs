using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace AssetPreprocessor.Scripts.Editor
{
    public class BasePreprocessorConfig : ScriptableObject
    {
        [Header("正则匹配")]
        public List<string> MatchList = new List<string>();
        public List<string> IgnoreList = new List<string>();

        public bool Check(AssetImporter assetImporter)
        {
            bool match = false;
            for (int i = 0; i < IgnoreList.Count; i++)
            {
                string s = Regex.Escape(IgnoreList[i]).Replace("\\*", ".*?");
                match = new Regex(s).IsMatch(assetImporter.assetPath);
                if (match) return false;
            }
            for (int i = 0; i < MatchList.Count; i++)
            {
                string s = Regex.Escape(MatchList[i]).Replace("\\*", ".*?");
                match = new Regex(s).IsMatch(assetImporter.assetPath);
                if (match) return true;
            }
            return false;
        }
    }
}
