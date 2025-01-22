#if UNITY_2019_4_OR_NEWER
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEngine.Tilemaps;

namespace YooAsset.Editor
{
    public class AssetArtReporterWindow : EditorWindow
    {
        [MenuItem("YooAsset/AssetArt Reporter", false, 302)]
        public static AssetArtReporterWindow OpenWindow()
        {
            AssetArtReporterWindow window = GetWindow<AssetArtReporterWindow>("资源扫描报告", true, WindowsDefine.DockedWindowTypes);
            window.minSize = new Vector2(800, 600);
            return window;
        }

        private class ElementTableData : DefaultTableData
        {
            public ReportElement Element;
        }
        private class PassesBtnCell : ITableCell, IComparable
        {
            public object CellValue { set; get; }
            public string SearchTag { private set; get; }
            public ReportElement Element
            {
                get
                {
                    return (ReportElement)CellValue;
                }
            }

            public PassesBtnCell(string searchTag, ReportElement element)
            {
                SearchTag = searchTag;
                CellValue = element;
            }
            public object GetDisplayObject()
            {
                return string.Empty;
            }
            public int CompareTo(object other)
            {
                if (other is PassesBtnCell cell)
                {
                    return this.Element.Passes.CompareTo(cell.Element.Passes);
                }
                else
                {
                    return 0;
                }
            }
        }
        private class WhiteListBtnCell : ITableCell, IComparable
        {
            public object CellValue { set; get; }
            public string SearchTag { private set; get; }
            public ReportElement Element
            {
                get
                {
                    return (ReportElement)CellValue;
                }
            }

            public WhiteListBtnCell(string searchTag, ReportElement element)
            {
                SearchTag = searchTag;
                CellValue = element;
            }
            public object GetDisplayObject()
            {
                return string.Empty;
            }
            public int CompareTo(object other)
            {
                if (other is PassesBtnCell cell)
                {
                    return this.Element.IsWhiteList.CompareTo(cell.Element.IsWhiteList);
                }
                else
                {
                    return 0;
                }
            }
        }

        private ToolbarSearchField _searchField;
        private Button _whiteVisibleBtn;
        private Button _stateVisibleBtn;
        private Label _titleLabel;
        private Label _descLabel;
        private TableView _elementTableView;

        private ScanReportCombiner _reportCombiner;
        private string _lastestOpenFolder;
        private List<ITableData> _sourceDatas;
        private bool _showWhiteElements = true;
        private bool _showPassesElements = true;


        public void CreateGUI()
        {
            try
            {
                VisualElement root = this.rootVisualElement;

                // 加载布局文件
                var visualAsset = UxmlLoader.LoadWindowUXML<AssetArtReporterWindow>();
                if (visualAsset == null)
                    return;

                visualAsset.CloneTree(root);

                // 导入按钮
                var singleImportBtn = root.Q<Button>("SingleImportButton");
                singleImportBtn.clicked += SingleImportBtn_clicked;
                var multiImportBtn = root.Q<Button>("MultiImportButton");
                multiImportBtn.clicked += MultiImportBtn_clicked;

                // 修复按钮
                var fixAllBtn = root.Q<Button>("FixAllButton");
                fixAllBtn.clicked += FixAllBtn_clicked;
                var fixSelectBtn = root.Q<Button>("FixSelectButton");
                fixSelectBtn.clicked += FixSelectBtn_clicked;

                // 白名单按钮
                _whiteVisibleBtn = root.Q<Button>("WhiteVisibleButton");
                _whiteVisibleBtn.clicked += WhiteVisibleBtn_clicked;
                _stateVisibleBtn = root.Q<Button>("StateVisibleButton");
                _stateVisibleBtn.clicked += StateVsibleBtn_clicked;

                // 文件导出按钮
                var exportFilesBtn = root.Q<Button>("ExportFilesButton");
                exportFilesBtn.clicked += ExportFilesBtn_clicked;

                // 搜索过滤
                _searchField = root.Q<ToolbarSearchField>("SearchField");
                _searchField.RegisterValueChangedCallback(OnSearchKeyWordChange);

                // 标题和备注
                _titleLabel = root.Q<Label>("ReportTitle");
                _descLabel = root.Q<Label>("ReportDesc");

                // 列表相关
                _elementTableView = root.Q<TableView>("TopTableView");
                _elementTableView.ClickTableDataEvent = OnClickListItem;

                _lastestOpenFolder = EditorTools.GetProjectPath();
            }
            catch (System.Exception e)
            {
                Debug.LogError(e.ToString());
            }
        }
        public void OnDestroy()
        {
            if (_reportCombiner != null)
                _reportCombiner.SaveChange();
        }

        /// <summary>
        /// 导入单个报告文件
        /// </summary>
        public void ImportSingleReprotFile(string filePath)
        {
            // 记录本次打开目录
            _lastestOpenFolder = Path.GetDirectoryName(filePath);
            _reportCombiner = new ScanReportCombiner();

            try
            {
                var scanReport = ScanReportConfig.ImportJsonConfig(filePath);
                _reportCombiner.Combine(scanReport);

                // 刷新页面
                RefreshToolbar();
                FillTableView();
                RebuildView();
            }
            catch (System.Exception e)
            {
                _reportCombiner = null;
                _titleLabel.text = "导入报告失败！";
                _descLabel.text = e.Message;
                UnityEngine.Debug.LogError(e.StackTrace);
            }
        }

        private void SingleImportBtn_clicked()
        {
            string selectFilePath = EditorUtility.OpenFilePanel("导入报告", _lastestOpenFolder, "json");
            if (string.IsNullOrEmpty(selectFilePath))
                return;

            ImportSingleReprotFile(selectFilePath);
        }
        private void MultiImportBtn_clicked()
        {
            string selectFolderPath = EditorUtility.OpenFolderPanel("导入报告", _lastestOpenFolder, null);
            if (string.IsNullOrEmpty(selectFolderPath))
                return;

            // 记录本次打开目录
            _lastestOpenFolder = selectFolderPath;
            _reportCombiner = new ScanReportCombiner();

            try
            {
                string[] files = Directory.GetFiles(selectFolderPath);
                foreach (string filePath in files)
                {
                    string extension = System.IO.Path.GetExtension(filePath);
                    if (extension == ".json")
                    {
                        var scanReport = ScanReportConfig.ImportJsonConfig(filePath);
                        _reportCombiner.Combine(scanReport);
                    }
                }

                // 刷新页面
                RefreshToolbar();
                FillTableView();
                RebuildView();
            }
            catch (System.Exception e)
            {
                _reportCombiner = null;
                _titleLabel.text = "导入报告失败！";
                _descLabel.text = e.Message;
                UnityEngine.Debug.LogError(e.StackTrace);
            }
        }
        private void FixAllBtn_clicked()
        {
            if (EditorUtility.DisplayDialog("提示", "修复全部资源（白名单除外）", "Yes", "No"))
            {
                if (_reportCombiner != null)
                    _reportCombiner.FixAll();
            }
        }
        private void FixSelectBtn_clicked()
        {
            if (EditorUtility.DisplayDialog("提示", "修复所有选中资源（白名单除外）", "Yes", "No"))
            {
                if (_reportCombiner != null)
                    _reportCombiner.FixSelect();
            }
        }
        private void WhiteVisibleBtn_clicked()
        {
            _showWhiteElements = !_showWhiteElements;
            RefreshToolbar();
            RebuildView();
        }
        private void StateVsibleBtn_clicked()
        {
            _showPassesElements = !_showPassesElements;
            RefreshToolbar();
            RebuildView();
        }
        private void ExportFilesBtn_clicked()
        {
            string selectFolderPath = EditorUtility.OpenFolderPanel("导入所有选中资源", EditorTools.GetProjectPath(), string.Empty);
            if (string.IsNullOrEmpty(selectFolderPath) == false)
            {
                if (_reportCombiner != null)
                    _reportCombiner.ExportFiles(selectFolderPath);
            }
        }

        private void RefreshToolbar()
        {
            if (_reportCombiner == null)
                return;

            _titleLabel.text = _reportCombiner.ReportTitle;
            _descLabel.text = _reportCombiner.ReportDesc;

            if (_showWhiteElements)
                _whiteVisibleBtn.style.backgroundColor = new StyleColor(new Color32(56, 147, 58, 255));
            else
                _whiteVisibleBtn.style.backgroundColor = new StyleColor(new Color32(100, 100, 100, 255));

            if (_showPassesElements)
                _stateVisibleBtn.style.backgroundColor = new StyleColor(new Color32(56, 147, 58, 255));
            else
                _stateVisibleBtn.style.backgroundColor = new StyleColor(new Color32(100, 100, 100, 255));
        }
        private void FillTableView()
        {
            if (_reportCombiner == null)
                return;

            _elementTableView.ClearAll(true, true);

            // 状态标题
            {
                var columnStyle = new ColumnStyle();
                columnStyle.Width = 80;
                columnStyle.MinWidth = 80;
                columnStyle.MaxWidth = 80;
                columnStyle.Stretchable = false;
                columnStyle.Searchable = false;
                columnStyle.Sortable = true;
                var column = new TableColumn("状态", "状态", columnStyle);

                column.MakeCell = () =>
                {
                    var button = new Button();
                    button.text = "状态";
                    button.style.unityTextAlign = TextAnchor.MiddleCenter;
                    button.style.marginLeft = 3f;
                    button.style.flexGrow = columnStyle.Stretchable ? 1f : 0f;
                    button.style.width = columnStyle.Width;
                    button.style.maxWidth = columnStyle.MaxWidth;
                    button.style.minWidth = columnStyle.MinWidth;
                    return button;
                };
                column.BindCell = (VisualElement element, ITableData data, ITableCell cell) =>
                {
                    Button button = element as Button;
                    var elementTableData = data as ElementTableData;
                    if (elementTableData.Element.Passes)
                    {
                        button.style.backgroundColor = new StyleColor(new Color32(56, 147, 58, 255));
                        button.text = "通过";
                    }
                    else
                    {
                        button.style.backgroundColor = new StyleColor(new Color32(137, 0, 0, 255));
                        button.text = "失败";
                    }
                };
                _elementTableView.AddColumn(column);
            }

            // 白名单标题
            {
                var columnStyle = new ColumnStyle();
                columnStyle.Width = 80;
                columnStyle.MinWidth = 80;
                columnStyle.MaxWidth = 80;
                columnStyle.Stretchable = false;
                columnStyle.Searchable = false;
                columnStyle.Sortable = true;
                var column = new TableColumn("白名单", "白名单", columnStyle);
                column.MakeCell = () =>
                {
                    Button button = new Button();
                    button.text = "白名单";
                    button.style.unityTextAlign = TextAnchor.MiddleCenter;
                    button.style.marginLeft = 3f;
                    button.style.flexGrow = columnStyle.Stretchable ? 1f : 0f;
                    button.style.width = columnStyle.Width;
                    button.style.maxWidth = columnStyle.MaxWidth;
                    button.style.minWidth = columnStyle.MinWidth;
                    button.clickable.clickedWithEventInfo += OnClickWhitListButton;
                    return button;
                };
                column.BindCell = (VisualElement element, ITableData data, ITableCell cell) =>
                {
                    Button button = element as Button;
                    button.userData = data;
                    var elementTableData = data as ElementTableData;
                    if (elementTableData.Element.IsWhiteList)
                        button.style.backgroundColor = new StyleColor(new Color32(56, 147, 58, 255));
                    else
                        button.style.backgroundColor = new StyleColor(new Color32(100, 100, 100, 255));
                };
                _elementTableView.AddColumn(column);
            }

            // 选中标题
            {
                var columnStyle = new ColumnStyle();
                columnStyle.Width = 20;
                columnStyle.MinWidth = 20;
                columnStyle.MaxWidth = 20;
                columnStyle.Stretchable = false;
                columnStyle.Searchable = false;
                columnStyle.Sortable = false;
                var column = new TableColumn("选中框", string.Empty, columnStyle);
                column.MakeCell = () =>
                {
                    var toggle = new Toggle();
                    toggle.text = string.Empty;
                    toggle.style.unityTextAlign = TextAnchor.MiddleCenter;
                    toggle.style.marginLeft = 3f;
                    toggle.style.flexGrow = columnStyle.Stretchable ? 1f : 0f;
                    toggle.style.width = columnStyle.Width;
                    toggle.style.maxWidth = columnStyle.MaxWidth;
                    toggle.style.minWidth = columnStyle.MinWidth;
                    toggle.RegisterValueChangedCallback((evt) => { OnToggleValueChange(toggle, evt); });
                    return toggle;
                };
                column.BindCell = (VisualElement element, ITableData data, ITableCell cell) =>
                {
                    var toggle = element as Toggle;
                    toggle.userData = data;
                    var tableData = data as ElementTableData;
                    toggle.SetValueWithoutNotify(tableData.Element.IsSelected);
                };
                _elementTableView.AddColumn(column);
            }

            // 自定义标题栏
            foreach (var header in _reportCombiner.Headers)
            {
                var columnStyle = new ColumnStyle();
                columnStyle.Width = header.Width;
                columnStyle.MinWidth = header.MinWidth;
                columnStyle.MaxWidth = header.MaxWidth;
                columnStyle.Stretchable = header.Stretchable;
                columnStyle.Searchable = header.Searchable;
                columnStyle.Sortable = header.Sortable;
                var column = new TableColumn(header.HeaderTitle, header.HeaderTitle, columnStyle);
                column.MakeCell = () =>
                {
                    var label = new Label();
                    label.style.unityTextAlign = TextAnchor.MiddleLeft;
                    label.style.marginLeft = 3f;
                    label.style.flexGrow = columnStyle.Stretchable ? 1f : 0f;
                    label.style.width = columnStyle.Width;
                    label.style.maxWidth = columnStyle.MaxWidth;
                    label.style.minWidth = columnStyle.MinWidth;
                    return label;
                };
                column.BindCell = (VisualElement element, ITableData data, ITableCell cell) =>
                {
                    var infoLabel = element as Label;
                    infoLabel.text = (string)cell.GetDisplayObject();
                };
                _elementTableView.AddColumn(column);
            }

            // 填充数据源
            _sourceDatas = new List<ITableData>(_reportCombiner.Elements.Count);
            foreach (var element in _reportCombiner.Elements)
            {
                var tableData = new ElementTableData();
                tableData.Element = element;

                // 固定标题
                tableData.AddCell(new PassesBtnCell("状态", element));
                tableData.AddCell(new WhiteListBtnCell("白名单", element));
                tableData.AddButtonCell("选中框");

                // 自定义标题
                foreach (var scanInfo in element.ScanInfos)
                {
                    var header = _reportCombiner.GetHeader(scanInfo.HeaderTitle);
                    if (header.HeaderType == EHeaderType.AssetPath)
                    {
                        tableData.AddAssetPathCell(scanInfo.HeaderTitle, scanInfo.ScanInfo);
                    }
                    else if (header.HeaderType == EHeaderType.StringValue)
                    {
                        tableData.AddStringValueCell(scanInfo.HeaderTitle, scanInfo.ScanInfo);
                    }
                    else if (header.HeaderType == EHeaderType.LongValue)
                    {
                        long value = Convert.ToInt64(scanInfo.ScanInfo);
                        tableData.AddLongValueCell(scanInfo.HeaderTitle, value);
                    }
                    else if (header.HeaderType == EHeaderType.DoubleValue)
                    {
                        double value = Convert.ToDouble(scanInfo.ScanInfo);
                        tableData.AddDoubleValueCell(scanInfo.HeaderTitle, value);
                    }
                    else
                    {
                        throw new NotImplementedException(header.HeaderType.ToString());
                    }
                }

                _sourceDatas.Add(tableData);
            }
            _elementTableView.itemsSource = _sourceDatas;
        }
        private void RebuildView()
        {
            if (_reportCombiner == null)
                return;

            string searchKeyword = _searchField.value;

            // 搜索匹配
            DefaultSearchSystem.Search(_sourceDatas, searchKeyword);

            // 开关匹配
            foreach (var tableData in _sourceDatas)
            {
                var elementTableData = tableData as ElementTableData;
                if (_showPassesElements == false && elementTableData.Element.Passes)
                {
                    tableData.Visible = false;
                    continue;
                }
                if (_showWhiteElements == false && elementTableData.Element.IsWhiteList)
                {
                    tableData.Visible = false;
                    continue;
                }
            }

            // 重建视图
            _elementTableView.RebuildView();
        }

        private void OnClickListItem(PointerDownEvent evt, ITableData tableData)
        {
            // 双击后检视对应的资源
            if (evt.clickCount == 2)
            {
                foreach (var cell in tableData.Cells)
                {
                    if (cell is AssetPathCell assetPathCell)
                    {
                        if (assetPathCell.PingAssetObject())
                            break;
                    }
                }
            }
        }
        private void OnSearchKeyWordChange(ChangeEvent<string> e)
        {
            RebuildView();
        }
        private void OnToggleValueChange(Toggle toggle, ChangeEvent<bool> e)
        {
            // 处理自身
            toggle.SetValueWithoutNotify(e.newValue);
            var elementTableData = toggle.userData as ElementTableData;
            elementTableData.Element.IsSelected = e.newValue;

            // 处理多选目标
            var selectedItems = _elementTableView.selectedItems;
            foreach (var selectedItem in selectedItems)
            {
                var selectElement = selectedItem as ElementTableData;
                selectElement.Element.IsSelected = e.newValue;
            }

            // 重绘视图
            RebuildView();
        }
        private void OnClickWhitListButton(EventBase evt)
        {
            // 刷新点击的按钮
            Button button = evt.target as Button;
            var elementTableData = button.userData as ElementTableData;
            elementTableData.Element.IsWhiteList = !elementTableData.Element.IsWhiteList;

            // 刷新框选的按钮
            var selectedItems = _elementTableView.selectedItems;
            if (selectedItems.Count() > 1)
            {
                foreach (var selectedItem in selectedItems)
                {
                    var selectElement = selectedItem as ElementTableData;
                    selectElement.Element.IsWhiteList = selectElement.Element.IsWhiteList;
                }
            }

            RebuildView();
        }
    }
}
#endif