using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Pusula.Core.Scanning;
using Pusula.Core.Templates;
using Pusula.Core.Tracking;
using Pusula.Core.Types;
using Pusula.Wpf.Mvvm;
using Pusula.Wpf.Properties;

namespace Pusula.Wpf.ViewModels
{
    /// <summary>Özet kutucuğu: etiket ve değer.</summary>
    public sealed class SummaryTile
    {
        /// <summary>Kutucuğu oluşturur.</summary>
        public SummaryTile(string label, string value)
        {
            Label = label;
            Value = value;
        }

        /// <summary>Etiket.</summary>
        public string Label { get; }

        /// <summary>Değer.</summary>
        public string Value { get; }
    }

    /// <summary>Özet tablosunun bir satırı (görünüm için biçimlenmiş).</summary>
    public sealed class DetailRowViewModel
    {
        /// <summary>Satırı bir özet satırından oluşturur.</summary>
        public DetailRowViewModel(SummaryRow row, TypeDefinition stockType)
        {
            Name = row.Name;
            Path = row.Path;
            Template = row.Template.Length == 0 ? "—" : row.Template;
            Status = StatusChip.For(stockType, row.Status);
            IsMissing = row.IsMissing;
            IsDim = row.IsMissing || row.IsSpecial || row.Stats.FileCount == 0;
            FileCount = row.IsMissing ? "—" : row.Stats.FileCount.ToString();
            Size = row.Stats.FileCount == 0 ? "—" : Formats.Size(row.Stats.TotalBytes);
            LastChange = row.Stats.LastWrite.HasValue ? Formats.Date(row.Stats.LastWrite.Value) : Resources.Row_Empty;

            if (row.IsMissing)
            {
                LastFile = Resources.Row_Missing;
            }
            else if (row.IsSpecial)
            {
                LastFile = Resources.Row_Special;
            }
            else if (row.Stats.LastWrite.HasValue)
            {
                LastFile = System.IO.Path.GetFileName(row.Stats.LastFile) + " · " + Formats.ShortDate(row.Stats.LastWrite.Value);
            }
            else
            {
                LastFile = Resources.Row_Empty;
            }
        }

        /// <summary>Klasör ya da stok adı.</summary>
        public string Name { get; }

        /// <summary>Tam yol (çift tıklayınca açılır).</summary>
        public string Path { get; }

        /// <summary>Stok satırında şablon.</summary>
        public string Template { get; }

        /// <summary>Stok satırında ana durum.</summary>
        public StatusChip Status { get; }

        /// <summary>Dosya sayısı.</summary>
        public string FileCount { get; }

        /// <summary>Boyut.</summary>
        public string Size { get; }

        /// <summary>Son değişiklik tarihi (stok satırı).</summary>
        public string LastChange { get; }

        /// <summary>Son değişen dosya ve tarihi (klasör satırı).</summary>
        public string LastFile { get; }

        /// <summary>Soluk gösterilir (boş, eksik ya da özel klasör).</summary>
        public bool IsDim { get; }

        /// <summary>Şablonda var, diskte yok.</summary>
        public bool IsMissing { get; }
    }

    /// <summary>
    /// Sağ paneldeki özet: proje seçiliyse stok listesi ve durum dağılımı, stok seçiliyse durum ve klasör listesi.
    /// Sayım arka planda yapılır; seçim değişince önceki sayım iptal edilir.
    /// </summary>
    public sealed class DetailViewModel : ViewModelBase
    {
        private CancellationTokenSource? cancellation;
        private TreeNodeViewModel? node;
        private TemplateSet templates = TemplateSet.CreateDefault();
        private TypeSet types = TypeSet.CreateDefault();
        private bool isLoading;
        private bool isProject;
        private bool isStock;
        private string subtitle = string.Empty;
        private string note = string.Empty;
        private StatusChip status = new StatusChip("—", StatusTone.Neutral);
        private string statusInfo = string.Empty;

        /// <summary>Özet kutucukları.</summary>
        public ObservableCollection<SummaryTile> Tiles { get; } = new ObservableCollection<SummaryTile>();

        /// <summary>Tablo satırları.</summary>
        public ObservableCollection<DetailRowViewModel> Rows { get; } = new ObservableCollection<DetailRowViewModel>();

        /// <summary>Stokta aşama çubuğu.</summary>
        public ObservableCollection<StatusStep> Steps { get; } = new ObservableCollection<StatusStep>();

        /// <summary>Projede durum dağılımı ("2 Testte" …).</summary>
        public ObservableCollection<StatusChip> Distribution { get; } = new ObservableCollection<StatusChip>();

        /// <summary>Hesaplanıyor mu.</summary>
        public bool IsLoading
        {
            get { return isLoading; }
            private set { SetProperty(ref isLoading, value); }
        }

        /// <summary>Proje özeti mi gösteriliyor (stok tablosu).</summary>
        public bool IsProject
        {
            get { return isProject; }
            private set { SetProperty(ref isProject, value); }
        }

        /// <summary>Stok özeti mi gösteriliyor (durum ve klasör tablosu).</summary>
        public bool IsStock
        {
            get { return isStock; }
            private set { SetProperty(ref isStock, value); }
        }

        /// <summary>Başlığın üstündeki satır (tür, numara türü, şablon, proje).</summary>
        public string Subtitle
        {
            get { return subtitle; }
            private set { SetProperty(ref subtitle, value); }
        }

        /// <summary>Tablonun altındaki not.</summary>
        public string Note
        {
            get { return note; }
            private set { SetProperty(ref note, value); }
        }

        /// <summary>Stokun ana durumu.</summary>
        public StatusChip Status
        {
            get { return status; }
            private set { SetProperty(ref status, value); }
        }

        /// <summary>Durumun yanındaki bilgi ("03.11.2026'dan beri · numune gelmedi").</summary>
        public string StatusInfo
        {
            get { return statusInfo; }
            private set { SetProperty(ref statusInfo, value); }
        }

        /// <summary>Gösterilen stokun klasörü (durum değiştirmek için); stok değilse boş.</summary>
        public string StockPath
        {
            get { return node != null && node.Kind == FolderKind.Stock ? node.Path : string.Empty; }
        }

        /// <summary>Son gösterilen düğümü aynı ayarlarla yeniden hesaplar.</summary>
        public void Reload()
        {
            Load(node, templates, types);
        }

        /// <summary>Seçili düğümün özetini arka planda hesaplar ve gösterir.</summary>
        public async void Load(TreeNodeViewModel? selected, TemplateSet templateSet, TypeSet typeSet)
        {
            cancellation?.Cancel();
            node = selected;
            templates = templateSet;
            types = typeSet;
            Tiles.Clear();
            Rows.Clear();
            Steps.Clear();
            Distribution.Clear();
            IsProject = false;
            IsStock = false;
            Note = string.Empty;
            StatusInfo = string.Empty;
            Subtitle = selected == null ? string.Empty : selected.KindText;

            if (selected == null || selected.Kind == FolderKind.Plain)
            {
                IsLoading = false;
                return;
            }

            var source = new CancellationTokenSource();
            cancellation = source;
            IsLoading = true;
            var path = selected.Path;
            var kind = selected.Kind;
            FolderSummary summary;
            try
            {
                summary = await Task.Run(
                    () => kind == FolderKind.Project
                        ? FolderSummary.ForProject(path, templates.ProjectFolders, source.Token)
                        : FolderSummary.ForStock(path, templates, source.Token),
                    source.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (source.IsCancellationRequested)
            {
                return;
            }

            IsLoading = false;
            if (kind == FolderKind.Project)
            {
                ShowProject(summary);
            }
            else
            {
                ShowStock(summary, path);
            }
        }

        private void ShowProject(FolderSummary summary)
        {
            var stockType = types.Stock;
            Tiles.Add(new SummaryTile(Resources.Tile_Stocks, summary.StockCount.ToString()));
            AddTotalTiles(summary.Total);
            Tiles.Add(new SummaryTile(Resources.Tile_LastChange, summary.Total.LastWrite.HasValue ? Formats.ShortDate(summary.Total.LastWrite.Value) : "—"));
            foreach (var row in summary.Rows)
            {
                Rows.Add(new DetailRowViewModel(row, stockType));
            }

            // Dağılım, tür kalıbındaki sırayla: sıralı durumlar, sıra dışılar, sonra durumu olmayanlar.
            var order = stockType.Statuses.Concat(stockType.SideStatuses).Select(s => s.Name).ToList();
            foreach (var group in Rows.GroupBy(r => r.Status.Name).OrderBy(g => order.IndexOf(g.Key) < 0 ? int.MaxValue : order.IndexOf(g.Key)))
            {
                var name = group.Key == "—" ? Resources.Status_None : group.Key;
                Distribution.Add(new StatusChip(group.Count() + " " + name, group.First().Status.Tone));
            }

            Note = string.Format(Resources.Detail_General, summary.General.FileCount);
            IsProject = true;
        }

        private void ShowStock(FolderSummary summary, string path)
        {
            var stockType = types.Stock;
            var info = summary.Stock;
            var number = info?.StockNo ?? Path.GetFileName(path);
            var format = stockType.MatchNumber(number);
            var project = Path.GetFileName(Path.GetDirectoryName(path) ?? string.Empty);
            var parts = new[] { format?.Label ?? Resources.Kind_Stock, summary.Template, project }.Where(p => !string.IsNullOrEmpty(p));
            Subtitle = string.Join(" · ", parts);

            var current = info?.Status ?? string.Empty;
            Status = StatusChip.For(stockType, current);
            foreach (var step in StatusStep.Build(stockType, current))
            {
                Steps.Add(step);
            }

            var since = info?.StatusSinceDate;
            var infoParts = new[]
            {
                !since.HasValue ? string.Empty : since.Value.Date == DateTime.Today ? Resources.Status_SinceToday : string.Format(Resources.Status_Since, Formats.ShortDate(since.Value)),
                info?.StatusNote ?? string.Empty,
            };
            StatusInfo = string.Join(" · ", infoParts.Where(p => p.Length > 0));

            AddTotalTiles(summary.Total);
            Tiles.Add(new SummaryTile(Resources.Tile_LastFile, summary.Total.LastWrite.HasValue ? Path.GetFileName(summary.Total.LastFile) : "—"));
            foreach (var row in summary.Rows)
            {
                Rows.Add(new DetailRowViewModel(row, stockType));
            }

            Note = Resources.Detail_RowHint;
            IsStock = true;
            OnPropertyChanged(nameof(StockPath));
        }

        private void AddTotalTiles(FolderStats total)
        {
            Tiles.Add(new SummaryTile(Resources.Tile_Files, total.FileCount.ToString()));
            Tiles.Add(new SummaryTile(Resources.Tile_Size, total.FileCount == 0 ? "—" : Formats.Size(total.TotalBytes)));
        }
    }

    /// <summary>Boyut ve tarih biçimleri.</summary>
    public static class Formats
    {
        /// <summary>Bayt → "1,8 GB" gibi.</summary>
        public static string Size(long bytes)
        {
            const double Kb = 1024;
            if (bytes < Kb)
            {
                return string.Format(Resources.Size_Bytes, bytes);
            }

            if (bytes < Kb * Kb)
            {
                return string.Format(Resources.Size_KB, bytes / Kb);
            }

            if (bytes < Kb * Kb * Kb)
            {
                return string.Format(Resources.Size_MB, bytes / (Kb * Kb));
            }

            return string.Format(Resources.Size_GB, bytes / (Kb * Kb * Kb));
        }

        /// <summary>"08.11.2026 14:20".</summary>
        public static string Date(DateTime value)
        {
            return value.ToString(Resources.Format_DateTime);
        }

        /// <summary>Bugünse "bugün 14:20" (saat yoksa "bugün"), değilse "08.11.2026".</summary>
        public static string ShortDate(DateTime value)
        {
            if (value.Date == DateTime.Today)
            {
                return value.TimeOfDay == TimeSpan.Zero
                    ? Resources.Format_TodayOnly
                    : string.Format(Resources.Format_Today, value.ToString("HH:mm"));
            }

            return value.ToString(Resources.Format_Date);
        }
    }
}
