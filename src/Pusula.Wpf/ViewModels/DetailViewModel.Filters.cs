using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using Pusula.Wpf.Properties;

namespace Pusula.Wpf.ViewModels
{
    /// <summary>
    /// Tablo süzgeçleri: proje sayfasındaki stok tablosu (metin, durum) ve test koşuları (metin, sonuç, sistem). Süzgeç
    /// tablonun görünümüne takılır (sütun sıralaması korunur, alttaki liste değişmez). Başka bir düğüme geçince sıfırlanır,
    /// aynı düğüm yenilenince korunur.
    /// </summary>
    public sealed partial class DetailViewModel
    {
        private string stockFilterText = string.Empty;
        private string stockFilterStatus = Resources.Filter_All;
        private string stockFilterSummary = string.Empty;
        private string runFilterText = string.Empty;
        private string runFilterResult = Resources.Filter_All;
        private string runFilterSystem = Resources.Filter_All;
        private string devRunsSummary = string.Empty;
        private string qualRunsSummary = string.Empty;
        private string filteredPath = string.Empty;
        private bool filtersHooked;

        /// <summary>Stok tablosunda aranan (numara, ad, şablon, durum, son dosya).</summary>
        public string StockFilterText
        {
            get { return stockFilterText; }
            set
            {
                if (SetProperty(ref stockFilterText, value ?? string.Empty))
                {
                    ApplyStockFilter();
                }
            }
        }

        /// <summary>Stok tablosu durum süzgeci ("Tümü" ya da bir durum).</summary>
        public string StockFilterStatus
        {
            get { return stockFilterStatus; }
            set
            {
                // Seçenekler yenilenirken açılır kutu boş yazar; yok sayılır.
                if (value != null && SetProperty(ref stockFilterStatus, value))
                {
                    ApplyStockFilter();
                }
            }
        }

        /// <summary>Durum süzgecinin seçenekleri ("Tümü" ilk).</summary>
        public ObservableCollection<string> StockStatusOptions { get; } = new ObservableCollection<string> { Resources.Filter_All };

        /// <summary>"8 stok" ya da "8 stoktan 3'ü".</summary>
        public string StockFilterSummary
        {
            get { return stockFilterSummary; }
            private set { SetProperty(ref stockFilterSummary, value); }
        }

        /// <summary>Koşularda aranan (SN, sistem, kişi, plan, özet, analiz notu).</summary>
        public string RunFilterText
        {
            get { return runFilterText; }
            set
            {
                if (SetProperty(ref runFilterText, value ?? string.Empty))
                {
                    ApplyRunFilter();
                }
            }
        }

        /// <summary>Koşu sonucu süzgeci.</summary>
        public string RunFilterResult
        {
            get { return runFilterResult; }
            set
            {
                if (value != null && SetProperty(ref runFilterResult, value))
                {
                    ApplyRunFilter();
                }
            }
        }

        /// <summary>Koşu sistemi süzgeci (SATIMO, VNA …).</summary>
        public string RunFilterSystem
        {
            get { return runFilterSystem; }
            set
            {
                if (value != null && SetProperty(ref runFilterSystem, value))
                {
                    ApplyRunFilter();
                }
            }
        }

        /// <summary>Sonuç süzgecinin seçenekleri.</summary>
        public ObservableCollection<string> RunResultOptions { get; } = new ObservableCollection<string> { Resources.Filter_All };

        /// <summary>Sistem süzgecinin seçenekleri.</summary>
        public ObservableCollection<string> RunSystemOptions { get; } = new ObservableCollection<string> { Resources.Filter_All };

        /// <summary>Testler sekmesi: "12 koşu" ya da "12 koşudan 3'ü".</summary>
        public string DevRunsSummary
        {
            get { return devRunsSummary; }
            private set { SetProperty(ref devRunsSummary, value); }
        }

        /// <summary>Kalifikasyon sekmesi: "4 koşu" ya da "4 koşudan 1'i".</summary>
        public string QualRunsSummary
        {
            get { return qualRunsSummary; }
            private set { SetProperty(ref qualRunsSummary, value); }
        }

        /// <summary>Herhangi bir koşu süzgeci açık mı ("Süzgeci temizle" düğmesi için).</summary>
        public bool IsRunFiltered
        {
            get { return runFilterText.Trim().Length > 0 || runFilterResult != Resources.Filter_All || runFilterSystem != Resources.Filter_All; }
        }

        /// <summary>Stok süzgeci açık mı.</summary>
        public bool IsStockFiltered
        {
            get { return stockFilterText.Trim().Length > 0 || stockFilterStatus != Resources.Filter_All; }
        }

        /// <summary>Koşu süzgeçlerini temizler.</summary>
        public void ClearRunFilter()
        {
            runFilterText = string.Empty;
            runFilterResult = Resources.Filter_All;
            runFilterSystem = Resources.Filter_All;
            OnPropertyChanged(nameof(RunFilterText));
            OnPropertyChanged(nameof(RunFilterResult));
            OnPropertyChanged(nameof(RunFilterSystem));
            ApplyRunFilter();
        }

        /// <summary>Stok süzgeçlerini temizler.</summary>
        public void ClearStockFilter()
        {
            stockFilterText = string.Empty;
            stockFilterStatus = Resources.Filter_All;
            OnPropertyChanged(nameof(StockFilterText));
            OnPropertyChanged(nameof(StockFilterStatus));
            ApplyStockFilter();
        }

        // Yüklemenin başında: başka bir düğüme geçildiyse süzgeçler sıfırlanır (aynı düğüm yenilenince kalır).
        private void PrepareFilters(string path)
        {
            HookFilters();
            if (!string.Equals(path, filteredPath, StringComparison.OrdinalIgnoreCase))
            {
                filteredPath = path;
                ClearStockFilter();
                ClearRunFilter();
            }
        }

        // Yükleme bitince: seçenekler yeni satırlardan, özetler yeniden.
        private void RefreshFilters()
        {
            HookFilters();
            FillOptions(StockStatusOptions, Rows.Select(r => r.Status.Name), stockFilterStatus);
            var runs = DevelopmentRuns.Concat(QualificationRuns).ToList();
            FillOptions(RunResultOptions, runs.Select(r => r.ResultName), runFilterResult);
            FillOptions(RunSystemOptions, runs.Select(r => r.System), runFilterSystem);
            ApplyStockFilter();
            ApplyRunFilter();
        }

        private void HookFilters()
        {
            if (filtersHooked)
            {
                return;
            }

            filtersHooked = true;
            CollectionViewSource.GetDefaultView(Rows).Filter = o => MatchesStock((DetailRowViewModel)o);
            CollectionViewSource.GetDefaultView(DevelopmentRuns).Filter = o => MatchesRun((TestRunRow)o);
            CollectionViewSource.GetDefaultView(QualificationRuns).Filter = o => MatchesRun((TestRunRow)o);
        }

        private bool MatchesStock(DetailRowViewModel row)
        {
            var text = stockFilterText.Trim();
            var haystack = string.Join(" ", row.Name, row.Template, row.Status.Name, row.LastFile, row.LastChange);
            return (stockFilterStatus == Resources.Filter_All || string.Equals(row.Status.Name, stockFilterStatus, StringComparison.CurrentCultureIgnoreCase))
                && (text.Length == 0 || SearchMatch.Contains(haystack, text));
        }

        private bool MatchesRun(TestRunRow run)
        {
            var text = runFilterText.Trim();
            var haystack = string.Join(" ", run.Serial, run.RoundText, run.System, run.ResultName, run.Person, run.Title, run.Summary, run.DateText, run.AnalysisNote);
            return (runFilterResult == Resources.Filter_All || string.Equals(run.ResultName, runFilterResult, StringComparison.CurrentCultureIgnoreCase))
                && (runFilterSystem == Resources.Filter_All || string.Equals(run.System, runFilterSystem, StringComparison.CurrentCultureIgnoreCase))
                && (text.Length == 0 || SearchMatch.Contains(haystack, text));
        }

        private void ApplyStockFilter()
        {
            if (filtersHooked)
            {
                CollectionViewSource.GetDefaultView(Rows).Refresh();
            }

            var shown = Rows.Count(MatchesStock);
            StockFilterSummary = shown == Rows.Count ? string.Format(Resources.Filter_StockCount, Rows.Count) : string.Format(Resources.Filter_StockCountOf, Rows.Count, shown);
            OnPropertyChanged(nameof(IsStockFiltered));
        }

        private void ApplyRunFilter()
        {
            if (filtersHooked)
            {
                CollectionViewSource.GetDefaultView(DevelopmentRuns).Refresh();
                CollectionViewSource.GetDefaultView(QualificationRuns).Refresh();
            }

            DevRunsSummary = RunSummary(DevelopmentRuns);
            QualRunsSummary = RunSummary(QualificationRuns);
            OnPropertyChanged(nameof(IsRunFiltered));
        }

        private string RunSummary(ICollection<TestRunRow> runs)
        {
            var shown = runs.Count(MatchesRun);
            return shown == runs.Count ? string.Format(Resources.Filter_RunCount, runs.Count) : string.Format(Resources.Filter_RunCountOf, runs.Count, shown);
        }

        // "Tümü" ilk sırada kalır; seçili değer listede yoksa yine de eklenir (süzgeç görünür kalsın).
        private static void FillOptions(ObservableCollection<string> target, IEnumerable<string> values, string selected)
        {
            var list = values.Where(v => !string.IsNullOrWhiteSpace(v) && v != "—")
                .Concat(selected != Resources.Filter_All ? new[] { selected } : new string[0])
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .OrderBy(v => v, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
            while (target.Count > 1)
            {
                target.RemoveAt(target.Count - 1);
            }

            foreach (var value in list)
            {
                target.Add(value);
            }
        }
    }
}
