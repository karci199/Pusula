using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Pusula.Core.Inventory;
using Pusula.Core.Tracking;
using Pusula.Wpf.Properties;

namespace Pusula.Wpf.ViewModels
{
    /// <summary>
    /// Ana sayfa · Kayıtlar sekmesi: üç görünüm (Projeler, Stoklar, Test yardımcı araçları) ve ortak kapsam.
    /// Kapsam: seçilen projeler stok listesini, seçilen stoklar araç listesini daraltır (hiçbiri seçili değilse hepsi).
    /// </summary>
    public sealed partial class MainViewModel
    {
        private string recordView = "projects";
        private string toolKind = string.Empty;
        private string toolSubType = string.Empty;
        private string recordSearch = string.Empty;
        private bool scopeQuiet;
        private List<StockData> stockData = new List<StockData>();
        private Dictionary<string, ProjectRow> projectRows = new Dictionary<string, ProjectRow>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, long> stockSizes = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);

        // Okunmuş stok: ağaç düğümü, projesi, stok.json ve son değişiklik.
        private sealed class StockData
        {
            public TreeNodeViewModel Node = null!;
            public TreeNodeViewModel Project = null!;
            public StockInfo? Info;
            public DateTime? LastChange;
        }

        /// <summary>Projeler görünümü.</summary>
        public ObservableCollection<ProjectRow> ProjectRows { get; } = new ObservableCollection<ProjectRow>();

        /// <summary>Stoklar görünümü.</summary>
        public ObservableCollection<StockRow> StockRows { get; } = new ObservableCollection<StockRow>();

        /// <summary>Test yardımcı araçları görünümü.</summary>
        public ObservableCollection<ToolRow> ToolRows { get; } = new ObservableCollection<ToolRow>();

        /// <summary>Kapsam: projeler (çoklu seçim).</summary>
        public ObservableCollection<ScopeOption> ProjectOptions { get; } = new ObservableCollection<ScopeOption>();

        /// <summary>Kapsam: stoklar (seçili projelerinkiler; çoklu seçim).</summary>
        public ObservableCollection<ScopeOption> StockOptions { get; } = new ObservableCollection<ScopeOption>();

        /// <summary>Araç türü süzgeci (Hepsi, Jig, Kablo, Yazılım …).</summary>
        public ObservableCollection<HomeRecordFilter> ToolKindFilters { get; } = new ObservableCollection<HomeRecordFilter>();

        /// <summary>Alt tür seçenekleri (seçili türün); ilki boş = Hepsi.</summary>
        public ObservableCollection<string> ToolSubTypes { get; } = new ObservableCollection<string>();

        /// <summary>Görünüm: Projeler.</summary>
        public bool IsViewProjects
        {
            get { return recordView == "projects"; }
            set { SetRecordView(value, "projects"); }
        }

        /// <summary>Görünüm: Stoklar.</summary>
        public bool IsViewStocks
        {
            get { return recordView == "stocks"; }
            set { SetRecordView(value, "stocks"); }
        }

        /// <summary>Görünüm: Test yardımcı araçları.</summary>
        public bool IsViewTools
        {
            get { return recordView == "tools"; }
            set { SetRecordView(value, "tools"); }
        }

        /// <summary>Seçili alt tür (boş = hepsi).</summary>
        public string ToolSubType
        {
            get { return toolSubType; }
            set
            {
                if (SetProperty(ref toolSubType, value ?? string.Empty))
                {
                    ApplyRecords();
                }
            }
        }

        /// <summary>Kayıtlarda arama (numara, ad, tanım).</summary>
        public string RecordSearch
        {
            get { return recordSearch; }
            set
            {
                if (SetProperty(ref recordSearch, value ?? string.Empty))
                {
                    ApplyRecords();
                }
            }
        }

        /// <summary>Proje süzgecinin düğme yazısı: "Tüm projeler" ya da "2 proje".</summary>
        public string ScopeProjectText
        {
            get
            {
                var n = ProjectOptions.Count(o => o.IsChecked);
                return n == ProjectOptions.Count ? Resources.Scope_AllProjects : n == 0 ? Resources.Scope_None : string.Format(CultureInfo.CurrentCulture, Resources.Scope_Projects, n);
            }
        }

        /// <summary>Stok süzgecinin düğme yazısı: "Tüm stoklar" ya da "3 stok".</summary>
        public string ScopeStockText
        {
            get
            {
                var n = StockOptions.Count(o => o.IsChecked);
                return n == StockOptions.Count ? Resources.Scope_AllStocks : n == 0 ? Resources.Scope_None : string.Format(CultureInfo.CurrentCulture, Resources.Scope_Stocks, n);
            }
        }

        /// <summary>Görünüm başlıklarındaki sayılar.</summary>
        public int ProjectRowCount
        {
            get { return ProjectRows.Count; }
        }

        /// <summary>Stok satırı sayısı.</summary>
        public int StockRowCount
        {
            get { return StockRows.Count; }
        }

        /// <summary>Araç satırı sayısı.</summary>
        public int ToolRowCount
        {
            get { return ToolRows.Count; }
        }

        /// <summary>Projeler görünümünün alt satırı: toplamlar.</summary>
        public string ProjectTotalsText
        {
            get
            {
                var rows = ProjectRows.ToList();
                var text = string.Format(CultureInfo.CurrentCulture, Resources.Records_Totals, rows.Count, rows.Sum(r => r.Stocks));
                if (rows.Count > 0 && rows.All(r => r.Bytes.HasValue))
                {
                    text += " · " + ProjectRow.FormatSize(rows.Sum(r => r.Bytes!.Value));
                }

                return text;
            }
        }

        /// <summary>Tablonun altındaki toplam satırı (görünüme göre): "4 stok · 2 testte …", "8 araç · Kablo 5 · Jig 3".</summary>
        public string RecordFooterText
        {
            get
            {
                switch (recordView)
                {
                    case "stocks":
                        var byStatus = StockRows.Where(r => r.Status.Length > 0).GroupBy(r => r.Status).OrderByDescending(g => g.Count())
                            .Select(g => g.Key + " " + g.Count().ToString(CultureInfo.CurrentCulture));
                        return string.Join(" · ", new[] { string.Format(CultureInfo.CurrentCulture, Resources.Scope_Stocks, StockRows.Count) }.Concat(byStatus));
                    case "tools":
                        var byType = ToolRows.GroupBy(r => r.TypeName).OrderByDescending(g => g.Count())
                            .Select(g => g.Key + " " + g.Count().ToString(CultureInfo.CurrentCulture));
                        return string.Join(" · ", new[] { string.Format(CultureInfo.CurrentCulture, Resources.Records_ToolCount, ToolRows.Count) }.Concat(byType));
                    default:
                        return ProjectTotalsText;
                }
            }
        }

        /// <summary>Kapsam: bütün projeleri seç.</summary>
        public void SelectAllProjects(bool on)
        {
            scopeQuiet = true;
            foreach (var option in ProjectOptions)
            {
                option.SetQuiet(on);
            }

            scopeQuiet = false;
            ScopeChanged(true);
        }

        /// <summary>Kapsam: bütün stokları seç / temizle.</summary>
        public void SelectAllStocks(bool on)
        {
            scopeQuiet = true;
            foreach (var option in StockOptions)
            {
                option.SetQuiet(on);
            }

            scopeQuiet = false;
            ScopeChanged(false);
        }

        /// <summary>Satıra tıklanınca kaydı ağaçta seçer (proje, stok ya da araç).</summary>
        public void OpenRecordPath(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                SelectNodeByPath(path);
            }
        }

        private void SetRecordView(bool on, string view)
        {
            if (!on || recordView == view)
            {
                return;
            }

            recordView = view;
            OnPropertyChanged(nameof(IsViewProjects));
            OnPropertyChanged(nameof(IsViewStocks));
            OnPropertyChanged(nameof(IsViewTools));
            OnPropertyChanged(nameof(RecordFooterText));
        }

        // Kayıtlar arka planda okunur: stok.json'lar ve son değişiklik; sonra dosya sayısı / boyut ayrıca hesaplanır.
        private async Task LoadRecordsAsync(int version)
        {
            var projects = Nodes.Where(n => n.Kind == FolderKind.Project && (scopeProject == null || string.Equals(n.Path, scopeProject, StringComparison.OrdinalIgnoreCase))).ToList();
            var pairs = projects.SelectMany(p => p.Children.Where(c => c.Kind == FolderKind.Stock).Select(s => new { Project = p, Stock = s })).ToList();
            List<StockData> data;
            try
            {
                data = await Task.Run(() => pairs.Select(x => new StockData
                {
                    Node = x.Stock,
                    Project = x.Project,
                    Info = JsonFile.TryRead<StockInfo>(TrackingFiles.StockFile(x.Stock.Path)),
                    LastChange = ProjectCard.NewestWrite(x.Stock.Path, 2),
                }).ToList()).ConfigureAwait(true);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                data = new List<StockData>();
            }

            if (version != homeVersion)
            {
                return;
            }

            stockData = data;
            var uncheckedProjects = new HashSet<string>(ProjectOptions.Where(o => !o.IsChecked).Select(o => o.Key), StringComparer.OrdinalIgnoreCase);
            ProjectOptions.Clear();
            projectRows = new Dictionary<string, ProjectRow>(StringComparer.OrdinalIgnoreCase);
            foreach (var project in projects)
            {
                var info = JsonFile.TryRead<ProjectInfo>(TrackingFiles.ProjectFile(project.Path));
                var folder = Path.GetFileName(project.Path);
                var cut = folder.IndexOf('_');
                var code = cut > 0 ? folder.Substring(0, cut) : folder;
                var name = info != null && info.Name.Length > 0 ? info.Name : (cut > 0 ? folder.Substring(cut + 1) : string.Empty);
                var option = new ScopeOption(project.Path, folder, name, project.ProjectColor, () => ScopeChanged(true));
                option.SetQuiet(!uncheckedProjects.Contains(project.Path));
                ProjectOptions.Add(option);

                var stocks = data.Where(d => ReferenceEquals(d.Project, project)).ToList();
                projectRows[project.Path] = new ProjectRow(project.Path, project.ProjectColor, code, name, stocks.Count,
                    inventoryItems.Count(i => ToolBelongsTo(i, stocks, new[] { folder }, true)),
                    allHomeTasks.Count(t => t.NotePath.StartsWith(project.Path.TrimEnd('\\') + "\\", StringComparison.OrdinalIgnoreCase)),
                    stocks.Max(s => s.LastChange));
            }

            BuildHomeStats(data, projects.Select(p => Path.GetFileName(p.Path)).ToList());
            // Araç türleri: türün kendi adıyla ("Jig", "Kablo" …); hiç kaydı olmayanlar da (0) görünür.
            var kind = toolKind;
            ToolKindFilters.Clear();
            ToolKindFilters.Add(new HomeRecordFilter(string.Empty, Resources.Home_RangeAll, inventoryItems.Count, ChooseToolKind));
            foreach (var type in types.InventoryTypes.GroupBy(t => t.Key, StringComparer.OrdinalIgnoreCase).Select(g => g.First()))
            {
                ToolKindFilters.Add(new HomeRecordFilter(type.Key, type.Name, inventoryItems.Count(i => string.Equals(i.Record.Type, type.Key, StringComparison.OrdinalIgnoreCase)), ChooseToolKind));
            }

            (ToolKindFilters.FirstOrDefault(f => string.Equals(f.Key, kind, StringComparison.OrdinalIgnoreCase)) ?? ToolKindFilters[0]).IsSelected = true;
            RebuildStockOptions();
            ApplyRecords();
            NotifyScopeHeader();
            await LoadSizesAsync(version).ConfigureAwait(true);
            NotifyScopeHeader();
        }

        // Önce her stokun, sonra her projenin boyutu (bütün alt klasörler); ağ yolunda uzun sürebilir, tek tek gelir.
        private async Task LoadSizesAsync(int version)
        {
            foreach (var stock in stockData.ToList())
            {
                var stockPath = stock.Node.Path;
                var size = await Task.Run(() => CountFiles(stockPath)).ConfigureAwait(true);
                if (version != homeVersion)
                {
                    return;
                }

                stockSizes[stockPath] = size.Item2;
                foreach (var shown in StockRows.Where(r => string.Equals(r.Path, stockPath, StringComparison.OrdinalIgnoreCase)))
                {
                    shown.Bytes = size.Item2;
                }
            }

            foreach (var row in projectRows.Values.ToList())
            {
                var path = row.Path;
                var result = await Task.Run(() => CountFiles(path)).ConfigureAwait(true);
                if (version != homeVersion)
                {
                    return;
                }

                row.Files = result.Item1;
                row.Bytes = result.Item2;
                OnPropertyChanged(nameof(ProjectTotalsText));
                OnPropertyChanged(nameof(RecordFooterText));
            }
        }

        private static Tuple<int, long> CountFiles(string folder)
        {
            var count = 0;
            long bytes = 0;
            var pending = new Stack<string>();
            pending.Push(folder);
            while (pending.Count > 0)
            {
                var dir = pending.Pop();
                try
                {
                    foreach (var file in new DirectoryInfo(dir).EnumerateFiles())
                    {
                        count++;
                        bytes += file.Length;
                    }

                    foreach (var sub in Directory.EnumerateDirectories(dir))
                    {
                        pending.Push(sub);
                    }
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                }
            }

            return Tuple.Create(count, bytes);
        }

        private void ChooseToolKind(HomeRecordFilter filter)
        {
            foreach (var other in ToolKindFilters.Where(f => !ReferenceEquals(f, filter)))
            {
                other.IsSelected = false;
            }

            toolKind = filter.Key;
            ToolSubTypes.Clear();
            ToolSubTypes.Add(string.Empty);
            var type = toolKind.Length == 0 ? null : types.Find(toolKind);
            foreach (var sub in type?.SubTypes ?? new List<string>())
            {
                ToolSubTypes.Add(sub);
            }

            toolSubType = string.Empty;
            OnPropertyChanged(nameof(ToolSubType));
            OnPropertyChanged(nameof(HasToolSubTypes));
            ApplyRecords();
        }

        /// <summary>Seçili türün alt türü var mı (alt tür kutusu görünür).</summary>
        public bool HasToolSubTypes
        {
            get { return ToolSubTypes.Count > 1; }
        }

        private void ScopeChanged(bool projects)
        {
            if (scopeQuiet)
            {
                return;
            }

            if (projects)
            {
                RebuildStockOptions();
            }

            OnPropertyChanged(nameof(ScopeProjectText));
            OnPropertyChanged(nameof(ScopeStockText));
            ApplyRecords();
        }

        // Stok seçenekleri yalnız işaretli projelerinki; yeni gelenler işaretli, kullanıcının kaldırdıkları kalkık kalır.
        private void RebuildStockOptions()
        {
            var uncheckedStocks = new HashSet<string>(StockOptions.Where(o => !o.IsChecked).Select(o => o.Key), StringComparer.OrdinalIgnoreCase);
            StockOptions.Clear();
            foreach (var stock in StocksOfScopeProjects())
            {
                var option = new ScopeOption(stock.Node.Path, stock.Node.Title, stock.Info?.Name ?? string.Empty, stock.Project.ProjectColor, () => ScopeChanged(false));
                option.SetQuiet(!uncheckedStocks.Contains(stock.Node.Path));
                StockOptions.Add(option);
            }

            OnPropertyChanged(nameof(ScopeStockText));
        }

        private List<StockData> StocksOfScopeProjects()
        {
            var projects = new HashSet<string>(ProjectOptions.Where(o => o.IsChecked).Select(o => o.Key), StringComparer.OrdinalIgnoreCase);
            return stockData.Where(s => projects.Contains(s.Project.Path)).ToList();
        }

        // Araç bu stoklara / projelere bağlı mı: stok düzeyinde sahibi, proje düzeyinde sahibi (stok seçilmediyse), ya da stok.json bağı.
        private static bool ToolBelongsTo(InventoryItem item, IList<StockData> stocks, IEnumerable<string> projectFolders, bool includeProjectLevel)
        {
            switch (item.Level)
            {
                case InventoryLevel.Stock:
                    if (stocks.Any(s => string.Equals(s.Node.Title, item.Owner, StringComparison.OrdinalIgnoreCase)))
                    {
                        return true;
                    }

                    break;
                case InventoryLevel.Project:
                    if (includeProjectLevel && projectFolders.Contains(item.Owner, StringComparer.OrdinalIgnoreCase))
                    {
                        return true;
                    }

                    break;
            }

            return stocks.Any(s => s.Info != null && s.Info.Inventory.Contains(item.Record.Number, StringComparer.OrdinalIgnoreCase));
        }

        private bool Matches(params string[] fields)
        {
            return recordSearch.Length == 0 || fields.Any(f => f != null && f.IndexOf(recordSearch, StringComparison.CurrentCultureIgnoreCase) >= 0);
        }

        private void ApplyRecords()
        {
            var checkedProjects = ProjectOptions.Where(o => o.IsChecked).Select(o => o.Key).ToList();
            var scopeStocks = StocksOfScopeProjects();
            var checkedStocks = new HashSet<string>(StockOptions.Where(o => o.IsChecked).Select(o => o.Key), StringComparer.OrdinalIgnoreCase);
            var stocks = scopeStocks.Where(s => checkedStocks.Contains(s.Node.Path)).ToList();
            var stocksNarrowed = StockOptions.Any(o => !o.IsChecked);
            var narrowed = ProjectOptions.Any(o => !o.IsChecked) || stocksNarrowed;

            ProjectRows.Clear();
            foreach (var option in ProjectOptions.Where(o => o.IsChecked))
            {
                ProjectRow? row;
                if (projectRows.TryGetValue(option.Key, out row) && Matches(row.Code, row.Name))
                {
                    ProjectRows.Add(row);
                }
            }

            StockRows.Clear();
            foreach (var s in stocks)
            {
                var info = s.Info;
                if (!Matches(s.Node.Title, info?.Name ?? string.Empty))
                {
                    continue;
                }

                var one = new[] { s };
                var stockRow = new StockRow(s.Node.Path, s.Project.ProjectColor, s.Node.Title, info?.Name ?? string.Empty,
                    projectRows.TryGetValue(s.Project.Path, out var p) ? p.Code : s.Project.Title,
                    info?.Template ?? string.Empty, info?.Status ?? string.Empty,
                    inventoryItems.Count(i => ToolBelongsTo(i, one, new string[0], false)),
                    allHomeTasks.Count(t => t.NotePath.StartsWith(s.Node.Path.TrimEnd('\\') + "\\", StringComparison.OrdinalIgnoreCase)),
                    s.LastChange);
                long size;
                if (stockSizes.TryGetValue(s.Node.Path, out size))
                {
                    stockRow.Bytes = size;
                }

                StockRows.Add(stockRow);
            }

            ToolRows.Clear();
            var projectFolders = checkedProjects.Select(Path.GetFileName).ToList();
            var typeList = types.InventoryTypes.ToList();
            foreach (var item in inventoryItems)
            {
                var record = item.Record;
                if (toolKind.Length > 0 && !string.Equals(record.Type, toolKind, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (toolSubType.Length > 0 && !string.Equals(record.SubType, toolSubType, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (narrowed && !ToolBelongsTo(item, stocks, projectFolders, !stocksNarrowed))
                {
                    continue;
                }

                if (!Matches(record.Number, record.Name, record.SubType))
                {
                    continue;
                }

                var type = typeList.FirstOrDefault(t => string.Equals(t.Key, record.Type, StringComparison.OrdinalIgnoreCase));
                var linked = stockData.Where(s => s.Info != null && s.Info.Inventory.Contains(record.Number, StringComparer.OrdinalIgnoreCase)).Select(s => s.Node.Title).ToList();
                var attached = item.Level == InventoryLevel.General
                    ? (linked.Count == 0 ? Resources.Records_General : string.Join(", ", linked))
                    : item.Owner;
                ToolRows.Add(new ToolRow(item.Folder, TreeNodeViewModel.GlyphOf(type?.Icon ?? string.Empty), type?.Color ?? string.Empty, record.Type,
                    type?.Name ?? record.Type, record.Number, record.Name, record.SubType, attached, record.Location, record.CurrentStatus));
            }

            OnPropertyChanged(nameof(ProjectRowCount));
            OnPropertyChanged(nameof(StockRowCount));
            OnPropertyChanged(nameof(ToolRowCount));
            OnPropertyChanged(nameof(ProjectTotalsText));
            OnPropertyChanged(nameof(RecordFooterText));
        }
    }
}
