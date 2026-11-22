using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Pusula.Core.Tracking;
using Pusula.Wpf.Properties;

namespace Pusula.Wpf.ViewModels
{
    /// <summary>Stok sayfası · Yapılacaklar: bir grup (Gecikmiş, Bugün, Bu hafta, Sonra, Tarihsiz) ve işleri.</summary>
    public sealed class TaskGroup
    {
        /// <summary>Grubu kurar.</summary>
        public TaskGroup(string title, bool isLate, IList<OpenTaskRow> rows)
        {
            Title = title;
            IsLate = isLate;
            Rows = rows;
        }

        /// <summary>Başlık.</summary>
        public string Title { get; }

        /// <summary>Gecikmiş grubu mu (kırmızı).</summary>
        public bool IsLate { get; }

        /// <summary>İşler.</summary>
        public IList<OpenTaskRow> Rows { get; }

        /// <summary>İş sayısı.</summary>
        public int Count
        {
            get { return Rows.Count; }
        }
    }

    /// <summary>
    /// Stok sayfası: üstte sayılar, TEÜ görünümü ve zaman çizelgesi; sekmeler Doğrulamalar, Yapılacaklar,
    /// Test yardımcı araçları. Veri, stok seçilince dolan Detail'den (zaman çizelgesi, açık işler, seri no'lar) türetilir.
    /// </summary>
    public sealed partial class MainViewModel
    {
        private bool stockRebuildPending;
        private int stockVersion;
        private long? stockBytes;

        /// <summary>Zaman çizelgesi: üstte yapılacaklar, sonra gün gün kartlar (seçili zaman aralığı, geriye doğru).</summary>
        public ObservableCollection<StockTimelineDay> StockDays { get; } = new ObservableCollection<StockTimelineDay>();

        /// <summary>Zaman çizelgesinde gösterilecek olay yok.</summary>
        public bool IsStockTimelineEmpty
        {
            get { return StockDays.Count == 0; }
        }

        /// <summary>Yapılacaklar: tarihe göre gruplu açık işler.</summary>
        public ObservableCollection<TaskGroup> StockTaskGroups { get; } = new ObservableCollection<TaskGroup>();

        /// <summary>Test yardımcı araçları: bu stoka bağlı (stokta duran ya da bağlanmış) araçlar.</summary>
        public ObservableCollection<ToolRow> StockToolRows { get; } = new ObservableCollection<ToolRow>();

        /// <summary>Seri no'lar (test koşularından).</summary>
        public ObservableCollection<string> StockSerials { get; } = new ObservableCollection<string>();

        /// <summary>Seri no var mı.</summary>
        public bool HasStockSerials
        {
            get { return StockSerials.Count > 0; }
        }

        /// <summary>Açık iş sayısı.</summary>
        public int StockOpenCount
        {
            get { return StockTaskGroups.Sum(g => g.Count); }
        }

        /// <summary>Geciken iş var mı.</summary>
        public bool HasStockLate
        {
            get { return StockTaskGroups.Any(g => g.IsLate && g.Count > 0); }
        }

        /// <summary>Başlık altı: "Açık iş 6 (1 geciken)".</summary>
        public string StockTasksText
        {
            get
            {
                var late = StockTaskGroups.Where(g => g.IsLate).Sum(g => g.Count);
                var text = string.Format(CultureInfo.CurrentCulture, Resources.Stock_OpenTasks, StockOpenCount);
                return late == 0 ? text : text + " " + string.Format(CultureInfo.CurrentCulture, Resources.Stock_LateTasks, late);
            }
        }

        /// <summary>Başlık altı: klasör boyutu ("820 MB"); hesaplanırken "…".</summary>
        public string StockSizeText
        {
            get { return stockBytes.HasValue ? ProjectRow.FormatSize(stockBytes.Value) : "…"; }
        }

        /// <summary>Araç sayısı (sekme rozeti).</summary>
        public int StockToolCount
        {
            get { return StockToolRows.Count; }
        }

        // Detail dolarken (zaman çizelgesi, işler, seri no'lar ayrı ayrı gelir) birden çok kez çağrılır; bir kez kurulur.
        private void ScheduleStockRebuild()
        {
            if (stockRebuildPending)
            {
                return;
            }

            stockRebuildPending = true;
            System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
            {
                stockRebuildPending = false;
                RebuildStockPage();
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void HookStockPage()
        {
            Detail.Timeline.CollectionChanged += (s, e) => ScheduleStockRebuild();
            Detail.OpenTasks.CollectionChanged += (s, e) => ScheduleStockRebuild();
            Detail.MatrixRows.CollectionChanged += (s, e) => ScheduleStockRebuild();
        }

        private void RebuildStockPage()
        {
            var stock = selectedNode != null && selectedNode.Kind == FolderKind.Stock ? selectedNode : null;
            FilterStockTimeline();

            // Yapılacaklar: Gecikmiş · Bugün · Bu hafta · Sonra · Tarihsiz (boş grup gösterilmez).
            StockTaskGroups.Clear();
            var open = Detail.OpenTasks.Where(t => !t.IsDone).ToList();
            var today = DateTime.Today;
            var sunday = today.AddDays(((int)DayOfWeek.Sunday - (int)today.DayOfWeek + 7) % 7);
            AddTaskGroup(Resources.Group_Late, true, open.Where(t => t.IsLate));
            AddTaskGroup(Resources.Day_Today, false, open.Where(t => !t.IsLate && t.Task.Due.HasValue && t.Task.Due.Value.Date == today));
            AddTaskGroup(Resources.Home_RangeWeek, false, open.Where(t => t.Task.Due.HasValue && t.Task.Due.Value.Date > today && t.Task.Due.Value.Date <= sunday));
            AddTaskGroup(Resources.Group_Later, false, open.Where(t => t.Task.Due.HasValue && t.Task.Due.Value.Date > sunday));
            AddTaskGroup(Resources.Group_NoDate, false, open.Where(t => !t.Task.Due.HasValue));

            StockSerials.Clear();
            foreach (var serial in Detail.MatrixRows.Select(r => r.Serial).Where(s => s.Length > 0).Distinct())
            {
                StockSerials.Add(serial);
            }

            StockToolRows.Clear();
            if (stock != null)
            {
                var info = JsonFile.TryRead<StockInfo>(TrackingFiles.StockFile(stock.Path));
                var data = new List<StockData> { new StockData { Node = stock, Project = ProjectOf(stock.Path + "\\x") ?? stock, Info = info } };
                var typeList = types.InventoryTypes.ToList();
                foreach (var item in inventoryItems.Where(i => ToolBelongsTo(i, data, new string[0], false)))
                {
                    var record = item.Record;
                    var type = typeList.FirstOrDefault(t => string.Equals(t.Key, record.Type, StringComparison.OrdinalIgnoreCase));
                    StockToolRows.Add(new ToolRow(item.Folder, TreeNodeViewModel.GlyphOf(type?.Icon ?? string.Empty), type?.Color ?? string.Empty, record.Type,
                        type?.Name ?? record.Type, record.Number, record.Name, record.SubType,
                        item.Level == Pusula.Core.Inventory.InventoryLevel.General ? Resources.Records_General : item.Owner, record.Location, record.CurrentStatus));
                }
            }

            LoadVerifications();
            BuildStockStats();
            OnPropertyChanged(nameof(HasStockSerials));
            OnPropertyChanged(nameof(StockOpenCount));
            OnPropertyChanged(nameof(HasStockLate));
            OnPropertyChanged(nameof(StockTasksText));
            OnPropertyChanged(nameof(StockToolCount));
        }

        private void AddTaskGroup(string title, bool late, IEnumerable<OpenTaskRow> rows)
        {
            var list = rows.OrderBy(t => t.Task.Due ?? DateTime.MaxValue).ToList();
            if (list.Count > 0)
            {
                StockTaskGroups.Add(new TaskGroup(title, late, list));
            }
        }

        // Zaman çizelgesi: yapılacaklar ve seçili aralıktaki kayıtlar (aralık ana sayfadaki zaman süzgeciyle aynı, geriye doğru).
        private void FilterStockTimeline()
        {
            BuildStockTimeline(RangeStart());
            OnPropertyChanged(nameof(IsStockTimelineEmpty));
        }

        // Stok seçilince klasör boyutu arka planda hesaplanır (önbellekte varsa hemen).
        private async void LoadStockSize()
        {
            var stock = selectedNode != null && selectedNode.Kind == FolderKind.Stock ? selectedNode : null;
            var version = ++stockVersion;
            stockBytes = null;
            OnPropertyChanged(nameof(StockSizeText));
            if (stock == null)
            {
                return;
            }

            long cached;
            if (stockSizes.TryGetValue(stock.Path, out cached))
            {
                stockBytes = cached;
                OnPropertyChanged(nameof(StockSizeText));
                return;
            }

            var path = stock.Path;
            var result = await Task.Run(() => CountFiles(path)).ConfigureAwait(true);
            if (version != stockVersion)
            {
                return;
            }

            stockSizes[path] = result.Item2;
            stockBytes = result.Item2;
            OnPropertyChanged(nameof(StockSizeText));
        }
    }
}
