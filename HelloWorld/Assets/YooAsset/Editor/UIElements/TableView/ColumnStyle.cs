#if UNITY_2019_4_OR_NEWER
using System;
using UnityEditor;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace YooAsset.Editor
{
    public class ColumnStyle
    {
        public const float MaxValue = 8388608f;

        /// <summary>
        /// 单元列宽度
        /// </summary>
        public Length Width = 100f;

        /// <summary>
        /// 单元列最小宽度
        /// </summary>
        public Length MinWidth = 30f;

        /// <summary>
        /// 单元列最大宽度
        /// </summary>
        public Length MaxWidth = MaxValue;
        
        /// <summary>
        /// 可伸缩
        /// </summary>
        public bool Stretchable = false;

        /// <summary>
        /// 可搜索
        /// </summary>
        public bool Searchable = false;

        /// <summary>
        /// 可排序
        /// </summary>
        public bool Sortable = false;
    }
}
#endif