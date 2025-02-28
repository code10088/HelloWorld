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
        private Toolbar _bottomToolbar;
#if UNITY_2022_3_OR_NEWER
        private TreeView _childTreeView;
#endif

        private int _treeItemID = 0;
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
            _operationTableView.SelectionChangedEvent = OnOperationTableViewSelectionChanged;
            CreateOperationTableViewColumns();

            // 底部标题栏
            _bottomToolbar = _root.Q<Toolbar>("BottomToolbar");
            CreateBottomToolbarHeaders();

            // 子列表
#if UNITY_2022_3_OR_NEWER
            _childTreeView = _root.Q<TreeView>("BottomTreeView");
            _childTreeView.makeItem = MakeTreeViewItem;
            _childTreeView.bindItem = BindTreeViewItem;
#endif

            // 面板分屏
            var topGroup = _root.Q<VisualElement>("TopGroup");
            var bottomGroup = _root.Q<VisualElement>("BottomGroup");
            topGroup.style.minHeight = 100;
            bottomGroup.style.minHeight = 100f;
            UIElementsTools.SplitVerticalPanel(_root, topGroup, bottomGroup);
        }
        private void CreateOperationTableViewColumns()
        {
            // PackageName
            {
                var columnStyle = new ColumnStyle(200);
                columnStyle.Searchable = true;
                columnStyle.Sortable = true;
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
                var column = new TableColumn("ProcessTime", "Process Time", columnStyle);
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

            // Desc
            {
                var columnStyle = new ColumnStyle(500, 500, 1000);
                columnStyle.Stretchable = true;
                columnStyle.Searchable = true;
                var column = new TableColumn("Desc", "Desc", columnStyle);
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
        }
        private void CreateBottomToolbarHeaders()
        {
            // OperationName
            {
                ToolbarButton button = new ToolbarButton();
                button.text = "OperationName";
                button.style.flexGrow = 0;
                button.style.width = 315;
                _bottomToolbar.Add(button);
            }

            // Progress
            {
                ToolbarButton button = new ToolbarButton();
                button.text = "Progress";
                button.style.flexGrow = 0;
                button.style.width = 100;
                _bottomToolbar.Add(button);
            }

            // BeginTime
            {
                ToolbarButton button = new ToolbarButton();
                button.text = "BeginTime";
                button.style.flexGrow = 0;
                button.style.width = 100;
                _bottomToolbar.Add(button);
            }

            // ProcessTime
            {
                ToolbarButton button = new ToolbarButton();
                button.text = "ProcessTime";
                button.style.flexGrow = 0;
                button.style.width = 100;
                _bottomToolbar.Add(button);
            }

            // Status
            {
                ToolbarButton button = new ToolbarButton();
                button.text = "Status";
                button.style.flexGrow = 0;
                button.style.width = 100;
                _bottomToolbar.Add(button);
            }

            // Desc
            {
                ToolbarButton button = new ToolbarButton();
                button.text = "Desc";
                button.style.flexGrow = 0;
                button.style.width = 500;
                _bottomToolbar.Add(button);
            }
        }

        /// <summary>
        /// 填充页面数据
        /// </summary>
        public void FillViewData(DebugReport debugReport)
        {
            // 清空旧数据
            _operationTableView.ClearAll(false, true);

#if UNITY_2022_3_OR_NEWER
            _childTreeView.SetRootItems(new List<TreeViewItemData<DebugOperationInfo>>());
            _childTreeView.Rebuild();
#endif

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
                    rowData.AddStringValueCell("Desc", operationInfo.OperationDesc);
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
            _operationTableView.ClearAll(false, true);
#if UNITY_2022_3_OR_NEWER
            _childTreeView.SetRootItems(new List<TreeViewItemData<DebugOperationInfo>>());
            _childTreeView.Rebuild();
#endif
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

#if UNITY_2022_3_OR_NEWER
        private void OnOperationTableViewSelectionChanged(ITableData data)
        {
            var operationTableData = data as OperationTableData;
            DebugPackageData packageData = operationTableData.PackageData;
            DebugOperationInfo operationInfo = operationTableData.OperationInfo;

            _treeItemID = 0;
            var rootItems = CreateTreeData(operationInfo);
            _childTreeView.SetRootItems(rootItems);
            _childTreeView.Rebuild();
        }
        private VisualElement MakeTreeViewItem()
        {
            VisualElement treeViewElement = new VisualElement();
            treeViewElement.style.flexDirection = FlexDirection.Row;

            // OperationName
            {
                Label label = new Label();
                label.name = "OperationName";
                label.style.flexGrow = 0f;
                label.style.width = 300;
                label.style.unityTextAlign = TextAnchor.MiddleLeft;
                treeViewElement.Add(label);
            }

            // Progress
            {
                var label = new Label();
                label.name = "Progress";
                label.style.flexGrow = 0f;
                label.style.width = 100;
                label.style.unityTextAlign = TextAnchor.MiddleLeft;
                treeViewElement.Add(label);
            }

            // BeginTime
            {
                var label = new Label();
                label.name = "BeginTime";
                label.style.flexGrow = 0f;
                label.style.width = 100;
                label.style.unityTextAlign = TextAnchor.MiddleLeft;
                treeViewElement.Add(label);
            }

            // ProcessTime
            {
                var label = new Label();
                label.name = "ProcessTime";
                label.style.flexGrow = 0f;
                label.style.width = 100;
                label.style.unityTextAlign = TextAnchor.MiddleLeft;
                treeViewElement.Add(label);
            }

            // Status
            {
                var label = new Label();
                label.name = "Status";
                label.style.flexGrow = 0f;
                label.style.width = 100;
                label.style.unityTextAlign = TextAnchor.MiddleLeft;
                treeViewElement.Add(label);
            }

            // Desc
            {
                Label label = new Label();
                label.name = "Desc";
                label.style.flexGrow = 1f;
                label.style.width = 500;
                label.style.unityTextAlign = TextAnchor.MiddleLeft;
                treeViewElement.Add(label);
            }

            return treeViewElement;
        }
        private void BindTreeViewItem(VisualElement element, int index)
        {
            var operationInfo = _childTreeView.GetItemDataForIndex<DebugOperationInfo>(index);

            // OperationName
            {
                var label = element.Q<Label>("OperationName");
                label.text = operationInfo.OperationName;
            }

            // Progress
            {
                var label = element.Q<Label>("Progress");
                label.text = operationInfo.Progress.ToString();
            }

            // BeginTime
            {
                var label = element.Q<Label>("BeginTime");
                label.text = operationInfo.BeginTime;
            }

            // ProcessTime
            {
                var label = element.Q<Label>("ProcessTime");
                label.text = operationInfo.ProcessTime.ToString();
            }

            // Status
            {
                StyleColor textColor;
                if (operationInfo.Status == EOperationStatus.Failed.ToString())
                    textColor = new StyleColor(Color.yellow);
                else
                    textColor = new StyleColor(Color.white);

                var label = element.Q<Label>("Status");
                label.text = operationInfo.Status;
                label.style.color = textColor;
            }

            // Desc
            {
                var label = element.Q<Label>("Desc");
                label.text = operationInfo.OperationDesc;
            }
        }
        private List<TreeViewItemData<DebugOperationInfo>> CreateTreeData(DebugOperationInfo parentOperation)
        {
            var rootItemList = new List<TreeViewItemData<DebugOperationInfo>>();
            foreach (var childOperation in parentOperation.Childs)
            {
                var childItemList = CreateTreeData(childOperation);
                var treeItem = new TreeViewItemData<DebugOperationInfo>(_treeItemID++, childOperation, childItemList);
                rootItemList.Add(treeItem);
            }
            return rootItemList;
        }
#else
        private void OnOperationTableViewSelectionChanged(ITableData data)
        {
        }
#endif
    }
}
#endif