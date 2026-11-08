using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Pusula.Core.Scanning;
using Pusula.Core.Templates;
using Pusula.Core.Tracking;
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
        public DetailRowViewModel(SummaryRow row)
        {
            Name = row.Name;
            Path = row.Path;
            Template = row.Template.Length == 0 ? "—" : row.Template;
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
    /// Sağ paneldeki özet: proje seçiliyse stok listesi, stok seçiliyse klasör listesi.
    /// Sayım arka planda yapılır; seçim değişince önceki sayım iptal edilir.
    /// </summary>
    public sealed class DetailViewModel : ViewModelBase
    {
        private CancellationTokenSource? cancellation;
        private bool isLoading;
        private bool isProject;
        private bool isStock;
        private string subtitle = string.Empty;
        private string note = string.Empty;

        /// <summary>Özet kutucukları.</summary>
        public ObservableCollection<SummaryTile> Tiles { get; } = new ObservableCollection<SummaryTile>();

        /// <summary>Tablo satırları.</summary>
        public ObservableCollection<DetailRowViewModel> Rows { get; } = new ObservableCollection<DetailRowViewModel>();

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

        /// <summary>Stok özeti mi gösteriliyor (klasör tablosu).</summary>
        public bool IsStock
        {
            get { return isStock; }
            private set { SetProperty(ref isStock, value); }
        }

        /// <summary>Başlığın üstündeki satır (tür, şablon, proje).</summary>
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

        /// <summary>Seçili düğümün özetini arka planda hesaplar ve gösterir.</summary>
        public async void Load(TreeNodeViewModel? node, TemplateSet templates)
        {
            cancellation?.Cancel();
            Tiles.Clear();
            Rows.Clear();
            IsProject = false;
            IsStock = false;
            Note = string.Empty;
            Subtitle = node == null ? string.Empty : node.KindText;

            if (node == null || node.Kind == FolderKind.Plain)
            {
                IsLoading = false;
                return;
            }

            var source = new CancellationTokenSource();
            cancellation = source;
            IsLoading = true;
            var path = node.Path;
            var kind = node.Kind;
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
            Tiles.Add(new SummaryTile(Resources.Tile_Stocks, summary.StockCount.ToString()));
            AddTotalTiles(summary.Total);
            Tiles.Add(new SummaryTile(Resources.Tile_LastChange, summary.Total.LastWrite.HasValue ? Formats.ShortDate(summary.Total.LastWrite.Value) : "—"));
            foreach (var row in summary.Rows)
            {
                Rows.Add(new DetailRowViewModel(row));
            }

            Note = string.Format(Resources.Detail_General, summary.General.FileCount);
            IsProject = true;
        }

        private void ShowStock(FolderSummary summary, string path)
        {
            var project = Path.GetFileName(Path.GetDirectoryName(path) ?? string.Empty);
            Subtitle = summary.Template.Length == 0
                ? string.Format(Resources.Detail_StockSubtitleNoTemplate, project)
                : string.Format(Resources.Detail_StockSubtitle, summary.Template, project);
            AddTotalTiles(summary.Total);
            Tiles.Add(new SummaryTile(Resources.Tile_LastFile, summary.Total.LastWrite.HasValue ? Path.GetFileName(summary.Total.LastFile) : "—"));
            foreach (var row in summary.Rows)
            {
                Rows.Add(new DetailRowViewModel(row));
            }

            Note = Resources.Detail_RowHint;
            IsStock = true;
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

        /// <summary>Bugünse "bugün 14:20", değilse "08.11.2026".</summary>
        public static string ShortDate(DateTime value)
        {
            if (value.Date == DateTime.Today)
            {
                return string.Format(Resources.Format_Today, value.ToString("HH:mm"));
            }

            return value.ToString(Resources.Format_Date);
        }
    }
}
