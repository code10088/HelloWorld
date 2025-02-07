using System;

namespace YooAsset.Editor
{
    [Serializable]
    public class ReportHeader
    {
        public const int MaxValue = 8388608;

        /// <summary>
        /// 标题
        /// </summary>
        public string HeaderTitle;

        /// <summary>
        /// 标题宽度
        /// </summary>
        public int Width;

        /// <summary>
        /// 单元列最小宽度
        /// </summary>
        public int MinWidth = 50;

        /// <summary>
        /// 单元列最大宽度
        /// </summary>
        public int MaxWidth = MaxValue;

        /// <summary>
        /// 可伸缩选项
        /// </summary>
        public bool Stretchable = false;

        /// <summary>
        /// 可搜索选项
        /// </summary>
        public bool Searchable = false;

        /// <summary>
        /// 可排序选项
        /// </summary>
        public bool Sortable = false;

        /// <summary>
        /// 数值类型
        /// </summary>
        public EHeaderType HeaderType = EHeaderType.StringValue;


        public ReportHeader(string headerTitle, int width)
        {
            HeaderTitle = headerTitle;
            Width = width;
            MinWidth = width;
            MaxWidth = width;
        }
        public ReportHeader(string headerTitle, int width, int minWidth, int maxWidth)
        {
            HeaderTitle = headerTitle;
            Width = width;
            MinWidth = minWidth;
            MaxWidth = maxWidth;
        }

        public ReportHeader SetMinWidth(int value)
        {
            MinWidth = value;
            return this;
        }
        public ReportHeader SetMaxWidth(int value)
        {
            MaxWidth = value;
            return this;
        }
        public ReportHeader SetStretchable()
        {
            Stretchable = true;
            return this;
        }
        public ReportHeader SetSearchable()
        {
            Searchable = true;
            return this;
        }
        public ReportHeader SetSortable()
        {
            Sortable = true;
            return this;
        }
        public ReportHeader SetHeaderType(EHeaderType value)
        {
            HeaderType = value;
            return this;
        }
    }
}