#if UNITY_2019_4_OR_NEWER
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace YooAsset.Editor
{
    internal class DebuggerOperationListViewer
    {
        private class OperationTableData : DefaultTableData
        {
            public DebugPackageData PackageData;
            public DebugOperationInfo OperationInfo;
        }

        private VisualTreeAsset _visualAsset;
        private TemplateContainer _root;

        private TableView _operationTableView;

        private DebugReport _debugReport;
        private List<ITableData> _sourceDatas;


        /// <summary>
        /// 初始化页面
        /// </summary>
        public void InitViewer()
        {
            // 加载布局文件		
            _visualAsset = UxmlLoader.LoadWindowUXML<DebuggerOperationListViewer>();
            if (_visualAsset == null)
                return;

            _root = _visualAsset.CloneTree();
            _root.style.flexGrow = 1f;

            // 任务列表
            _operationTableView = _root.Q<TableView>("TopTableView");
            CreateOperationTableViewColumns();
        }
        private void CreateOperationTableViewColumns()
        {

            // PackageName
            {
                var columnStyle = new ColumnStyle(200);
                columnStyle.Stretchable = true;
                columnStyle.Searchable = true;
                columnStyle.Sortable = true;
                columnStyle.Counter = true;
                var column = new TableColumn("PackageName", "Package Name", columnStyle);
                column.MakeCell = () =>
                {
                    var label = new Label();
                    label.style.unityTextAlign = TextAnchor.MiddleLeft;
                    return label;
                };
                column.BindCell = (VisualElement element, ITableData data, ITableCell cell) =>
                {
                    var infoLabel = element as Label;
                    infoLabel.text = (string)cell.GetDisplayObject();
                };
                _operationTableView.AddColumn(column);
            }

            // OperationName
            {
                var columnStyle = new ColumnStyle(300, 300, 600);
                columnStyle.Stretchable = true;
                columnStyle.Searchable = true;
                columnStyle.Sortable = true;
                columnStyle.Counter = true;
                var column = new TableColumn("OperationName", "Operation Name", columnStyle);
                column.MakeCell = () =>
                {
                    var label = new Label();
                    label.style.unityTextAlign = TextAnchor.MiddleLeft;
                    return label;
                };
                column.BindCell = (VisualElement element, ITableData data, ITableCell cell) =>
                {
                    var infoLabel = element as Label;
                    infoLabel.text = (string)cell.GetDisplayObject();
                };
                _operationTableView.AddColumn(column);
            }

            // Priority
            {
                var columnStyle = new ColumnStyle(100);
                columnStyle.Stretchable = false;
                columnStyle.Searchable = false;
                columnStyle.Sortable = true;
                var column = new TableColumn("Priority", "Priority", columnStyle);
                column.MakeCell = () =>
                {
                    var label = new Label();
                    label.style.unityTextAlign = TextAnchor.MiddleLeft;
                    return label;
                };
                column.BindCell = (VisualElement element, ITableData data, ITableCell cell) =>
                {
                    var infoLabel = element as Label;
                    infoLabel.text = (string)cell.GetDisplayObject();
                };
                _operationTableView.AddColumn(column);
            }

            // Progress
            {
                var columnStyle = new ColumnStyle(100);
                columnStyle.Stretchable = false;
                columnStyle.Searchable = false;
                columnStyle.Sortable = false;
                var column = new TableColumn("Progress", "Progress", columnStyle);
                column.MakeCell = () =>
                {
                    var label = new Label();
                    label.style.unityTextAlign = TextAnchor.MiddleLeft;
                    return label;
                };
                column.BindCell = (VisualElement element, ITableData data, ITableCell cell) =>
                {
                    var infoLabel = element as Label;
                    infoLabel.text = (string)cell.GetDisplayObject();
                };
                _operationTableView.AddColumn(column);
            }

            // BeginTime
            {
                var columnStyle = new ColumnStyle(100);
                columnStyle.Stretchable = false;
                columnStyle.Searchable = false;
                columnStyle.Sortable = true;
                var column = new TableColumn("BeginTime", "Begin Time", columnStyle);
                column.MakeCell = () =>
                {
                    var label = new Label();
                    label.style.unityTextAlign = TextAnchor.MiddleLeft;
                    return label;
                };
                column.BindCell = (VisualElement element, ITableData data, ITableCell cell) =>
                {
                    var infoLabel = element as Label;
                    infoLabel.text = (string)cell.GetDisplayObject();
                };
                _operationTableView.AddColumn(column);
            }

            // ProcessTime
            {
                var columnStyle = new ColumnStyle(100);
                columnStyle.Stretchable = false;
                columnStyle.Searchable = false;
                columnStyle.Sortable = true;
                var column = new TableColumn("LoadingTime", "Loading Time", columnStyle);
                column.MakeCell = () =>
                {
                    var label = new Label();
                    label.style.unityTextAlign = TextAnchor.MiddleLeft;
                    return label;
                };
                column.BindCell = (VisualElement element, ITableData data, ITableCell cell) =>
                {
                    var infoLabel = element as Label;
                    infoLabel.text = (string)cell.GetDisplayObject();
                };
                _operationTableView.AddColumn(column);
            }

            // Status
            {
                var columnStyle = new ColumnStyle(100);
                columnStyle.Stretchable = false;
                columnStyle.Searchable = false;
                columnStyle.Sortable = true;
                var column = new TableColumn("Status", "Status", columnStyle);
                column.MakeCell = () =>
                {
                    var label = new Label();
                    label.style.unityTextAlign = TextAnchor.MiddleLeft;
                    return label;
                };
                column.BindCell = (VisualElement element, ITableData data, ITableCell cell) =>
                {
                    StyleColor textColor;
                    var operationTableData = data as OperationTableData;
                    if (operationTableData.OperationInfo.Status == EOperationStatus.Failed.ToString())
                        textColor = new StyleColor(Color.yellow);
                    else
                        textColor = new StyleColor(Color.white);

                    var infoLabel = element as Label;
                    infoLabel.text = (string)cell.GetDisplayObject();
                    infoLabel.style.color = textColor;
                };
                _operationTableView.AddColumn(column);
            }
        }

        /// <summary>
        /// 填充页面数据
        /// </summary>
        public void FillViewData(DebugReport debugReport)
        {
            _debugReport = debugReport;

            // 清空旧数据
            _operationTableView.ClearAll(false, true);

            // 填充数据源
            _sourceDatas = new List<ITableData>(1000);
            foreach (var packageData in debugReport.PackageDatas)
            {
                foreach (var operationInfo in packageData.OperationInfos)
                {
                    var rowData = new OperationTableData();
                    rowData.PackageData = packageData;
                    rowData.OperationInfo = operationInfo;
                    rowData.AddStringValueCell("PackageName", packageData.PackageName);
                    rowData.AddStringValueCell("OperationName", operationInfo.OperationName);
                    rowData.AddLongValueCell("Priority", operationInfo.Priority);
                    rowData.AddDoubleValueCell("Progress", operationInfo.Progress);
                    rowData.AddStringValueCell("BeginTime", operationInfo.BeginTime);
                    rowData.AddLongValueCell("LoadingTime", operationInfo.ProcessTime);
                    rowData.AddStringValueCell("Status", operationInfo.Status.ToString());
                    _sourceDatas.Add(rowData);
                }
            }
            _operationTableView.itemsSource = _sourceDatas;

            // 重建视图
            RebuildView(null);
        }

        /// <summary>
        /// 清空页面
        /// </summary>
        public void ClearView()
        {
            _debugReport = null;
            _operationTableView.ClearAll(false, true);
            RebuildView(null);
        }

        /// <summary>
        /// 重建视图
        /// </summary>
        public void RebuildView(string searchKeyWord)
        {
            // 搜索匹配
            DefaultSearchSystem.Search(_sourceDatas, searchKeyWord);

            // 重建视图
            _operationTableView.RebuildView();
        }

        /// <summary>
        /// 挂接到父类页面上
        /// </summary>
        public void AttachParent(VisualElement parent)
        {
            parent.Add(_root);
        }

        /// <summary>
        /// 从父类页面脱离开
        /// </summary>
        public void DetachParent()
        {
            _root.RemoveFromHierarchy();
        }
    }
}
#endif