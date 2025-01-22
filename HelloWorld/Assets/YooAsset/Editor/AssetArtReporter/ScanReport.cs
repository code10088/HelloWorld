using System;
using System.Collections;
using System.Collections.Generic;

namespace YooAsset.Editor
{
    [Serializable]
    public class ScanReport
    {
        /// <summary>
        /// 文件签名（自动填写）
        /// </summary>
        public string FileSign;

        /// <summary>
        /// 文件版本（自动填写）
        /// </summary>
        public string FileVersion;

        /// <summary>
        /// 模式类型（自动填写）
        /// </summary>
        public string SchemaType;

        /// <summary>
        /// 扫描器GUID（自动填写）
        /// </summary>
        public string ScannerGUID;


        /// <summary>
        /// 报告标题
        /// </summary>
        public string ReportTitle;

        /// <summary>
        /// 报告介绍
        /// </summary>
        public string ReportDesc;

        /// <summary>
        /// 报告的标题列表
        /// </summary>
        public List<ReportHeader> HeaderTitles = new List<ReportHeader>();

        /// <summary>
        /// 扫描的元素列表
        /// </summary>
        public List<ReportElement> ReportElements = new List<ReportElement>();


        public ScanReport(string reportTitle, string reportDesc)
        {
            ReportTitle = reportTitle;
            ReportDesc = reportDesc;
        }

        public ReportHeader AddHeader(string headerTitle, int width)
        {
            var reportHeader = new ReportHeader(headerTitle, width);
            HeaderTitles.Add(reportHeader);
            return reportHeader;
        }
    }
}