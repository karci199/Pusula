using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using Pusula.Core.Office;
using Pusula.Core.Types;
using Pusula.Wpf.Properties;

namespace Pusula.Wpf.ViewModels
{
    /// <summary>Rapor sekmesinde bir satır (Excel'deki İşler sayfasının satırı).</summary>
    public sealed class WorkReportRowView
    {
        /// <summary>Satırı kurar; <paramref name="firstOfDay"/> değilse tarih ve gün soluk yazılır.</summary>
        public WorkReportRowView(WorkReportRow row, bool firstOfDay, string tone)
        {
            DateText = row.Date.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
            DayText = row.Date.ToString("ddd", CultureInfo.GetCultureInfo("tr-TR"));
            IsFirstOfDay = firstOfDay;
            Project = row.Project;
            Stock = row.Stock;
            Kind = row.Kind;
            Work = row.Work;
            Note = row.Note;
            Status = new StatusChip(row.Status, tone);
            HasStatus = row.Status.Length > 0;
        }

        /// <summary>Tarih ("27.11.2026").</summary>
        public string DateText { get; }

        /// <summary>Gün ("Cum").</summary>
        public string DayText { get; }

        /// <summary>Günün ilk satırı mı (sonrakilerde tarih soluk).</summary>
        public bool IsFirstOfDay { get; }

        /// <summary>Proje.</summary>
        public string Project { get; }

        /// <summary>Stok numarası.</summary>
        public string Stock { get; }

        /// <summary>Tür.</summary>
        public string Kind { get; }

        /// <summary>Yapılan iş.</summary>
        public string Work { get; }

        /// <summary>Kısa açıklama (Ayrıntı hariç); boş olabilir.</summary>
        public string Note { get; }

        /// <summary>Açıklama var mı (ikinci satır görünür).</summary>
        public bool HasNote
        {
            get { return Note.Length > 0; }
        }

        /// <summary>Durum etiketi (isteğe bağlı).</summary>
        public StatusChip Status { get; }

        /// <summary>Durum var mı.</summary>
        public bool HasStatus { get; }
    }

    /// <summary>Rapor sekmesi: dönem, proje / stok süzgeci, sayılar ve satırlar (27.11.2026).</summary>
    public sealed partial class MainViewModel
    {
        /// <summary>Ana sayfada Rapor sekmesinin sırası.</summary>
        public const int ReportTabIndex = 4;

        /// <summary>Rapor sekmesi açık mı.</summary>
        public bool IsHomeReportTab
        {
            get { return homeTab == ReportTabIndex; }
        }

        /// <summary>Proje süzgecinin seçenekleri (ilki "Tüm projeler").</summary>
        public ObservableCollection<string> ReportProjects { get; } = new ObservableCollection<string>();

        /// <summary>Stok süzgecinin seçenekleri (ilki "Tüm stoklar"; seçili projenin stokları).</summary>
        public ObservableCollection<string> ReportStocks { get; } = new ObservableCollection<string>();

        /// <summary>Seçili proje (görünen metin; "Tüm projeler" süzgeç yok demek).</summary>
        public string ReportProject
        {
            get { return reportProject.Length == 0 ? Resources.Report_AllProjects : reportProject; }
            set
            {
                if (fillingReportFilters)
                {
                    return;
                }

                var v = value == null || value == Resources.Report_AllProjects ? string.Empty : value;
                if (v != reportProject)
                {
                    reportProject = v;
                    reportStock = string.Empty;
                    OnPropertyChanged();
                    RefreshReport();
                }
            }
        }

        /// <summary>Seçili stok ("Tüm stoklar" süzgeç yok demek).</summary>
        public string ReportStock
        {
            get { return reportStock.Length == 0 ? Resources.Report_AllStocks : reportStock; }
            set
            {
                if (fillingReportFilters)
                {
                    return;
                }

                var v = value == null || value == Resources.Report_AllStocks ? string.Empty : value;
                if (v != reportStock)
                {
                    reportStock = v;
                    OnPropertyChanged();
                    RefreshReport();
                }
            }
        }

        /// <summary>Satırlar (Excel'e aktarılanlarla aynı).</summary>
        public ObservableCollection<WorkReportRowView> WorkReportLines { get; } = new ObservableCollection<WorkReportRowView>();

        /// <summary>Sayılar: Kayıt + en sık türler.</summary>
        public ObservableCollection<StatLine> WorkReportTiles { get; } = new ObservableCollection<StatLine>();

        /// <summary>Dönemde (süzgeçte) kayıt yok mu.</summary>
        public bool IsWorkReportEmpty
        {
            get { return WorkReportLines.Count == 0; }
        }

        /// <summary>Süzgeçleri, sayıları ve satırları yeniden kurar (dönem, tarih, süzgeç ya da veri değişince).</summary>
        internal void RefreshReport()
        {
            FillReportFilters();
            var rows = WorkReportRows();
            WorkReportLines.Clear();
            DateTime? previous = null;
            foreach (var row in rows)
            {
                WorkReportLines.Add(new WorkReportRowView(row, previous != row.Date.Date, ToneOf(row.Status)));
                previous = row.Date.Date;
            }

            WorkReportTiles.Clear();
            WorkReportTiles.Add(new StatLine(Resources.Report_Records, rows.Count, string.Empty, null));
            foreach (var g in rows.Where(r => r.Kind.Length > 0).GroupBy(r => r.Kind, StringComparer.CurrentCultureIgnoreCase)
                .OrderByDescending(g => g.Count()).ThenBy(g => g.Key, StringComparer.CurrentCultureIgnoreCase).Take(5))
            {
                WorkReportTiles.Add(new StatLine(g.Key, g.Count(), string.Empty, null));
            }

            OnPropertyChanged(nameof(ReportPreview));
            OnPropertyChanged(nameof(IsWorkReportEmpty));
        }

        // Proje listesi verideki projelerden; stok listesi seçili projenin (yoksa hepsinin) stoklarından.
        private void FillReportFilters()
        {
            fillingReportFilters = true;
            try
            {
                var projects = homeEvents.Select(c => c.ProjectName).Where(p => p.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(p => p, StringComparer.CurrentCultureIgnoreCase).ToList();
                if (reportProject.Length > 0 && !projects.Contains(reportProject, StringComparer.OrdinalIgnoreCase))
                {
                    reportProject = string.Empty;
                }

                var stocks = homeEvents.Where(c => reportProject.Length == 0 || string.Equals(c.ProjectName, reportProject, StringComparison.OrdinalIgnoreCase))
                    .Select(c => c.Source).Where(s => s.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(s => s, StringComparer.CurrentCultureIgnoreCase).ToList();
                if (reportStock.Length > 0 && !stocks.Contains(reportStock, StringComparer.OrdinalIgnoreCase))
                {
                    reportStock = string.Empty;
                }

                Replace(ReportProjects, new[] { Resources.Report_AllProjects }.Concat(projects).ToList());
                Replace(ReportStocks, new[] { Resources.Report_AllStocks }.Concat(stocks).ToList());
            }
            finally
            {
                fillingReportFilters = false;
            }

            OnPropertyChanged(nameof(ReportProject));
            OnPropertyChanged(nameof(ReportStock));
        }

        private static void Replace(ObservableCollection<string> target, System.Collections.Generic.IList<string> items)
        {
            if (target.SequenceEqual(items))
            {
                return;
            }

            target.Clear();
            foreach (var item in items)
            {
                target.Add(item);
            }
        }

        // Durumun rengi: Geçti / Teslim alındı / Çözüldü yeşil, Kaldı / Gecikti / İptal kırmızı, Açık sarı.
        private static string ToneOf(string status)
        {
            if (status.Length == 0)
            {
                return StatusTone.Neutral;
            }

            Func<string, bool> has = w => w.Length > 0 && status.IndexOf(w, StringComparison.CurrentCultureIgnoreCase) >= 0;
            if (has(Resources.Verify_StatusFailed) || has(Resources.Request_DueLate) || has(Resources.Request_StatusCancelled))
            {
                return StatusTone.Failed;
            }

            if (has(Resources.Verify_StatusPassed) || has(Resources.Request_StatusDelivered) || has(Resources.Issue_Solved))
            {
                return StatusTone.Done;
            }

            return has(Resources.Request_StatusOpen) || has(Resources.Issue_Open) ? StatusTone.Waiting : StatusTone.Neutral;
        }
    }
}
