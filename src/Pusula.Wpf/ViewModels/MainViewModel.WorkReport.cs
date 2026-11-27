using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Input;
using Pusula.Core.Office;
using Pusula.Wpf.Mvvm;
using Pusula.Wpf.Properties;

namespace Pusula.Wpf.ViewModels
{
    /// <summary>
    /// İş raporu (27.11.2026): ana sayfa Günlük'teki kayıtlar seçilen dönem için (gün, hafta, ay, yıl, tüm zaman) Excel'e
    /// yazılır. Excel'de başlık satırında süzgeç vardır (proje, stok, tür); ikinci sayfa proje ve stoka göre özet.
    /// Durum sütunu isteğe bağlıdır: ölçüm sonucu, talep / sorun durumu ya da revizyon; yoksa boş.
    /// </summary>
    public sealed partial class MainViewModel
    {
        private string reportProject = string.Empty;
        private string reportStock = string.Empty;
        private bool fillingReportFilters;
        private ReportPeriod reportPeriod = ReportPeriod.Week;
        private DateTime workReportDate = DateTime.Today;
        private ICommand? openWorkReportCommand;

        private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");

        /// <summary>Ana sayfada Rapor sekmesine geçer (dönem Günlük süzgecinden).</summary>
        public ICommand OpenWorkReportCommand
        {
            get { return openWorkReportCommand ?? (openWorkReportCommand = new RelayCommand(OpenWorkReport)); }
        }


        /// <summary>Seçili dönem.</summary>
        public ReportPeriod ReportPeriod
        {
            get { return reportPeriod; }
            set
            {
                if (SetProperty(ref reportPeriod, value))
                {
                    OnPropertyChanged(nameof(IsReportDay));
                    OnPropertyChanged(nameof(IsReportWeek));
                    OnPropertyChanged(nameof(IsReportMonth));
                    OnPropertyChanged(nameof(IsReportYear));
                    OnPropertyChanged(nameof(IsReportAll));
                    OnPropertyChanged(nameof(IsReportDateVisible));
                    OnPropertyChanged(nameof(WorkReportFileName));
                    RefreshReport();
                }
            }
        }

        /// <summary>Dönem: gün.</summary>
        public bool IsReportDay
        {
            get { return reportPeriod == ReportPeriod.Day; }
            set { SetReportPeriod(value, ReportPeriod.Day); }
        }

        /// <summary>Dönem: hafta.</summary>
        public bool IsReportWeek
        {
            get { return reportPeriod == ReportPeriod.Week; }
            set { SetReportPeriod(value, ReportPeriod.Week); }
        }

        /// <summary>Dönem: ay.</summary>
        public bool IsReportMonth
        {
            get { return reportPeriod == ReportPeriod.Month; }
            set { SetReportPeriod(value, ReportPeriod.Month); }
        }

        /// <summary>Dönem: yıl.</summary>
        public bool IsReportYear
        {
            get { return reportPeriod == ReportPeriod.Year; }
            set { SetReportPeriod(value, ReportPeriod.Year); }
        }

        /// <summary>Dönem: tüm zaman.</summary>
        public bool IsReportAll
        {
            get { return reportPeriod == ReportPeriod.All; }
            set { SetReportPeriod(value, ReportPeriod.All); }
        }

        /// <summary>Dönemin içindeki gün (o günü içeren hafta / ay / yıl alınır).</summary>
        public DateTime? WorkReportDate
        {
            get { return workReportDate; }
            set
            {
                var day = (value ?? DateTime.Today).Date;
                if (day != workReportDate)
                {
                    workReportDate = day;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(WorkReportFileName));
                    RefreshReport();
                }
            }
        }

        /// <summary>Tarih seçimi görünür mü (tüm zamanda yok).</summary>
        public bool IsReportDateVisible
        {
            get { return reportPeriod != ReportPeriod.All; }
        }

        /// <summary>Rapor başlığı: "23 – 29 Kasım 2026 · 11 kayıt" (proje / stok süzgeciyle).</summary>
        public string ReportPreview
        {
            get
            {
                var rows = WorkReportRows();
                return string.Format(CultureInfo.CurrentCulture, Resources.Report_Preview, ReportRangeText(rows), rows.Count);
            }
        }

        /// <summary>Kaydetme penceresinde önerilen dosya adı.</summary>
        public string WorkReportFileName
        {
            get { return WorkReport.FileName(Resources.Report_FilePrefix, reportPeriod, workReportDate, Resources.Report_FileAll); }
        }

        private void SetReportPeriod(bool on, ReportPeriod period)
        {
            if (on)
            {
                ReportPeriod = period;
            }
        }

        private void OpenWorkReport()
        {
            // Rapor sekmesi ana sayfadaki süzgeçle açılır: Bugün → gün, Bu ay → ay, Bu yıl → yıl, Hepsi → tüm zaman, yoksa hafta.
            workReportDate = DateTime.Today;
            OnPropertyChanged(nameof(WorkReportDate));
            ReportPeriod = homeRange == "today" ? ReportPeriod.Day : homeRange == "month" ? ReportPeriod.Month : homeRange == "year" ? ReportPeriod.Year : homeRange == "all" ? ReportPeriod.All : ReportPeriod.Week;
            RefreshReport();
            HomeTab = ReportTabIndex;
        }

        /// <summary>Seçili dönemin (ve seçili proje / stokun) satırları, eskiden yeniye.</summary>
        internal List<WorkReportRow> WorkReportRows()
        {
            DateTime? from;
            DateTime? to;
            WorkReport.RangeOf(reportPeriod, workReportDate, out from, out to);
            return WorkReport.Sorted(homeEvents.Where(c => WorkReport.InRange(c.When, from, to)
                && (reportProject.Length == 0 || string.Equals(c.ProjectName, reportProject, StringComparison.OrdinalIgnoreCase))
                && (reportStock.Length == 0 || string.Equals(c.Source, reportStock, StringComparison.OrdinalIgnoreCase))).Select(ReportRowOf));
        }

        private static WorkReportRow ReportRowOf(StockTimelineCard card)
        {
            var journal = card.Item?.Journal;
            // Yapılan iş = başlık; açıklama = kartın kısa açıklaması ("#### Ayrıntı" bölümü alınmaz).
            var summary = (card.Description ?? string.Empty).Trim();
            var hasTitle = card.Title.Trim().Length > 0;
            var title = hasTitle ? card.Title.Trim() : summary;
            return new WorkReportRow
            {
                Date = card.When,
                Project = card.ProjectName,
                Stock = card.Source,
                StockName = card.StockName,
                Kind = card.KindLabel,
                Work = title,
                Note = hasTitle && !string.Equals(summary, title, StringComparison.CurrentCultureIgnoreCase) ? summary : string.Empty,
                Status = ReportStatusOf(card, journal?.Result.Trim() ?? string.Empty),
            };
        }

        // Durum: ölçüm sonucu, talep durumu, sorun durumu, revizyon; hiçbiri yoksa boş (zorunlu değil).
        private static string ReportStatusOf(StockTimelineCard card, string result)
        {
            foreach (var s in new[] { result, card.RequestStatusText, card.IssueStatusText, card.RevisionText })
            {
                if (!string.IsNullOrWhiteSpace(s))
                {
                    return s.Trim();
                }
            }

            return string.Empty;
        }

        // "23 – 29 Kasım 2026"; tüm zamanda ilk ve son kaydın günleri.
        private string ReportRangeText(IList<WorkReportRow> rows)
        {
            DateTime? from;
            DateTime? to;
            WorkReport.RangeOf(reportPeriod, workReportDate, out from, out to);
            if (from.HasValue && to.HasValue)
            {
                return WorkReport.RangeText(from.Value, to.Value.AddDays(-1), Turkish);
            }

            return rows.Count == 0 ? Resources.Report_NoData : WorkReport.RangeText(rows[0].Date.Date, rows[rows.Count - 1].Date.Date, Turkish);
        }

        private string ReportTitle()
        {
            switch (reportPeriod)
            {
                case ReportPeriod.Day:
                    return Resources.Report_TitleDay;
                case ReportPeriod.Month:
                    return Resources.Report_TitleMonth;
                case ReportPeriod.Year:
                    return Resources.Report_TitleYear;
                case ReportPeriod.All:
                    return Resources.Report_TitleAll;
                default:
                    return Resources.Report_TitleWeek;
            }
        }

        // Sağ üstte: "Cuma", "48. hafta", "Kasım 2026", "2026", "Tüm zaman".
        private string ReportTag()
        {
            switch (reportPeriod)
            {
                case ReportPeriod.Day:
                    return workReportDate.ToString("dddd", Turkish);
                case ReportPeriod.Week:
                    return string.Format(CultureInfo.CurrentCulture, Resources.Report_WeekTag, WorkReport.IsoWeek(workReportDate));
                case ReportPeriod.Month:
                    return workReportDate.ToString("MMMM yyyy", Turkish);
                case ReportPeriod.Year:
                    return workReportDate.Year.ToString(CultureInfo.InvariantCulture);
                default:
                    return Resources.Report_All;
            }
        }

        /// <summary>Seçili dönemin raporunu yazar; durum satırına sonucu yazar.</summary>
        public bool ExportWorkReport(string path)
        {
            var rows = WorkReportRows();
            var scope = reportStock.Length > 0 ? reportStock : reportProject.Length > 0 ? reportProject
                : scopeProject == null ? Resources.Report_AllProjects : System.IO.Path.GetFileName(scopeProject);
            var labels = new WorkReportLabels
            {
                SheetWork = Resources.Report_SheetWork,
                SheetSummary = Resources.Report_SheetSummary,
                Title = ReportTitle(),
                PeriodTag = ReportTag(),
                Subtitle = string.Join("  ·  ", new[] { ReportRangeText(rows), scope, string.Format(CultureInfo.CurrentCulture, Resources.Report_Count, rows.Count) }),
                FilterHint = Resources.Report_FilterHint,
                Records = Resources.Report_Records,
                WorkSection = Resources.Report_WorkSection,
                Columns = new[] { Resources.Report_ColDate, Resources.Report_ColDay, Resources.Report_ColProject, Resources.Report_ColStock, Resources.Report_ColKind, Resources.Report_ColWork, Resources.Report_ColStatus, Resources.Report_ColNote },
                SummarySection = Resources.Report_SummarySection,
                SummaryColumns = new[] { Resources.Report_ColProject, Resources.Report_ColStock, Resources.Report_ColStockName, Resources.Report_ColCount, Resources.Report_ColLast, Resources.Report_ColKinds },
                Empty = Resources.Report_Empty,
                Footer = string.Format(CultureInfo.CurrentCulture, Resources.Report_Footer, DateTime.Today.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)),
                GoodWords = new[] { Resources.Verify_StatusPassed, Resources.Issue_Solved, Resources.Request_StatusDelivered },
                BadWords = new[] { Resources.Verify_StatusFailed, Resources.Request_DueLate, Resources.Request_StatusCancelled },
                WaitWords = new[] { Resources.Request_StatusOpen, Resources.Issue_Open },
            };
            var error = WorkReportWriter.Write(path, labels, rows);
            Status = error.Length > 0
                ? string.Format(CultureInfo.CurrentCulture, Resources.Verify_WriteFailed, error)
                : string.Format(CultureInfo.CurrentCulture, Resources.Report_Done, rows.Count, System.IO.Path.GetFileName(path));
            return error.Length == 0;
        }
    }
}
