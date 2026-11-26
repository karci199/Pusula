using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Pusula.Core.Inventory;
using Pusula.Core.Tracking;
using Pusula.Core.Types;
using Pusula.Wpf.Mvvm;
using Pusula.Wpf.Properties;

namespace Pusula.Wpf.ViewModels
{
    /// <summary>İstatistik kartında bir satır: ad, sayı, renk; tıklanınca ilgili listeye gider.</summary>
    public sealed class StatLine
    {
        /// <summary>Satırı kurar; <paramref name="open"/> boşsa satır tıklanmaz.</summary>
        public StatLine(string label, int count, string tone, Action? open)
        {
            Label = label;
            Count = count;
            Tone = tone;
            Open = new RelayCommand(() => open?.Invoke(), () => open != null);
        }

        /// <summary>Ad ("Testte", "Kablaj").</summary>
        public string Label { get; }

        /// <summary>Sayı.</summary>
        public int Count { get; }

        /// <summary>Sayının rengi (<see cref="StatusTone"/>; boş: normal).</summary>
        public string Tone { get; }

        /// <summary>Tıklanınca ilgili liste.</summary>
        public ICommand Open { get; }
    }

    /// <summary>İstatistik kartı: başlık, büyük toplam ve satırlar.</summary>
    public sealed class StatCard
    {
        /// <summary>Kartı kurar.</summary>
        public StatCard(string title, int total, IEnumerable<StatLine> lines)
        {
            Title = title;
            Total = total;
            Lines = lines.ToList();
        }

        /// <summary>Başlık.</summary>
        public string Title { get; }

        /// <summary>Toplam.</summary>
        public int Total { get; }

        /// <summary>Satırlar.</summary>
        public IList<StatLine> Lines { get; }

        /// <summary>Satır yoksa kartta "—".</summary>
        public bool IsEmpty
        {
            get { return Lines.Count == 0; }
        }
    }

    /// <summary>
    /// İstatistik (22.11.2026, kullanıcı kararı: en genel sayılar). Ana sayfa / proje sekmesi: stoklar (durumuna göre),
    /// test yardımcı araçları (türüne göre), açık işler. Stok: ayrı sekme yok (23.11.2026); Doğrulamalar'ın üstünde tek
    /// satır özet. Doğrulama sayıları yalnız stokta. Hepsi zaten okunmuş veriden; ayrı dosya yazılmaz.
    /// </summary>
    public sealed partial class MainViewModel
    {
        /// <summary>Ana sayfa / proje sayfasının İstatistik sekmesi.</summary>
        public ObservableCollection<StatCard> HomeStatCards { get; } = new ObservableCollection<StatCard>();

        /// <summary>Stok sayfası, Doğrulamalar'ın üstündeki özet: Geçti · Kaldı · Sürüyor · Bekliyor (tıklanınca Durum süzgeci).</summary>
        public ObservableCollection<StatLine> VerificationSummary { get; } = new ObservableCollection<StatLine>();

        /// <summary>Özetin solundaki yazı: "112 gereksinim".</summary>
        public string VerificationSummaryTotal { get; private set; } = string.Empty;

        /// <summary>Geçme oranı: "%66" (kapsam dışı sayılmaz; madde yoksa boş).</summary>
        public string VerificationPassedPercent { get; private set; } = string.Empty;

        /// <summary>Özet çubuğunun parçaları (yıldız genişlik): geçti, kaldı, sürüyor, kalan.</summary>
        public IList<GridLength> VerificationBar { get; private set; } = new[] { new GridLength(0, GridUnitType.Star), new GridLength(0, GridUnitType.Star), new GridLength(0, GridUnitType.Star), new GridLength(1, GridUnitType.Star) };

        /// <summary>Özet görünür mü (stokta en az bir madde varsa).</summary>
        public bool HasVerificationSummary
        {
            get { return VerificationSummary.Count > 0; }
        }

        // Ana sayfa / proje: kapsamdaki stoklar (data) ve araçlar; işler ve olaylar kapsama göre okunmuş olanlar.
        private void BuildHomeStats(IList<StockData> data, IList<string> projectFolders)
        {
            HomeStatCards.Clear();
            var stockType = types.Find(TypeSet.StockKey);
            var order = stockType?.Statuses.Select(s => s.Name).Concat(stockType.SideStatuses.Select(s => s.Name)).ToList() ?? new List<string>();
            var stockLines = data
                .GroupBy(d => (d.Info?.Status ?? string.Empty).Trim(), StringComparer.CurrentCultureIgnoreCase)
                .OrderBy(g => StatusRank(order, g.Key))
                .Select(g => new StatLine(g.Key.Length == 0 ? Resources.Stats_NoStatus : g.Key, g.Count(), string.Empty, OpenStockRecords))
                .ToList();
            HomeStatCards.Add(new StatCard(Resources.Stats_Stocks, data.Count, stockLines));

            var tools = scopeProject == null
                ? inventoryItems.ToList()
                : inventoryItems.Where(i => ToolBelongsTo(i, data, projectFolders, true)).ToList();
            HomeStatCards.Add(new StatCard(Resources.Records_ViewTools, tools.Count, ToolLines(tools, true)));

            var late = allHomeTasks.Count(t => t.IsLate);
            var issues = homeEvents.Count(c => c.IsIssueOpen);
            var requests = homeEvents.Count(IsRequestOpen);
            HomeStatCards.Add(new StatCard(Resources.Stats_OpenWork, allHomeTasks.Count, new[]
            {
                new StatLine(Resources.Stats_Late, late, late > 0 ? StatusTone.Failed : string.Empty, () => HomeTab = 1),
                new StatLine(Resources.Stats_OpenIssues, issues, string.Empty, () => HomeTab = 1),
                new StatLine(Resources.Stats_OpenRequests, requests, string.Empty, () => HomeTab = 1),
            }));
        }

        // Stok: Doğrulamalar'ın üstündeki tek satır özet (23.11.2026: stokta ayrı İstatistik sekmesi kalktı).
        // Plansız / raporsuz sayıları üstteki kapsama uyarısında zaten var; burada tekrarlanmaz.
        private void BuildStockStats()
        {
            VerificationSummary.Clear();
            // 261 ile doğrulanan maddeler de sayılır (26.11.2026): durumları 261'den hesaplanır (aktarılınca Geçti).
            var isStock = selectedNode != null && selectedNode.Kind == FolderKind.Stock;
            var kinds = isStock
                ? verificationItems.Select(i => VerificationRow.KindOf(i.EffectiveStatus)).ToList()
                : new List<int>();
            var counted = kinds.Count(k => k != 5);
            var passed = kinds.Count(VerificationRow.IsPassedKind);
            var failed = kinds.Count(IsFailedKind);
            var running = kinds.Count(IsRunningKind);
            if (kinds.Count > 0)
            {
                VerificationSummary.Add(new StatLine(Resources.Stats_Passed, passed, StatusTone.Done, () => ShowVerificationKinds(VerificationRow.IsPassedKind)));
                VerificationSummary.Add(new StatLine(Resources.Stats_Failed, failed, StatusTone.Failed, () => ShowVerificationKinds(IsFailedKind)));
                VerificationSummary.Add(new StatLine(Resources.Stats_Running, running, StatusTone.Active, () => ShowVerificationKinds(IsRunningKind)));
                VerificationSummary.Add(new StatLine(Resources.Stats_Pending, kinds.Count(k => k == 0), string.Empty, () => ShowVerificationKinds(k => k == 0)));
            }

            VerificationSummaryTotal = string.Format(CultureInfo.CurrentCulture, Resources.Stats_Requirements, counted);
            VerificationPassedPercent = counted > 0 ? string.Format(CultureInfo.CurrentCulture, Resources.Stats_Percent, 100 * passed / counted) : string.Empty;
            VerificationBar = new[]
            {
                new GridLength(passed, GridUnitType.Star),
                new GridLength(failed, GridUnitType.Star),
                new GridLength(running, GridUnitType.Star),
                new GridLength(Math.Max(0, counted - passed - failed - running) + (counted == 0 ? 1 : 0), GridUnitType.Star),
            };
            OnPropertyChanged(nameof(VerificationSummaryTotal));
            OnPropertyChanged(nameof(VerificationPassedPercent));
            OnPropertyChanged(nameof(VerificationBar));
            OnPropertyChanged(nameof(HasVerificationSummary));
        }

        private static bool IsFailedKind(int kind)
        {
            return kind == 2 || kind == 4;
        }

        private static bool IsRunningKind(int kind)
        {
            return kind == 3 || kind == 6 || kind == 7 || kind == 8;
        }

        // Özetteki sayıya tıklanınca: Gereksinimler görünümü, Durum süzgeci o gruptaki durumlar.
        private void ShowVerificationKinds(Func<int, bool> kind)
        {
            IsVerReqView = true;
            verificationStatusFilter.Clear();
            foreach (var status in VerificationRow.Statuses.Where(s => kind(VerificationRow.KindOf(s))))
            {
                verificationStatusFilter.Add(status);
            }

            BuildStatusFilters();
            FillVerificationGroups();
        }

        // Araç satırları: bütün test yardımcı aracı türleri (kaydı olmayan 0); ana sayfada tıklanınca Kayıtlar o türle açılır.
        private IEnumerable<StatLine> ToolLines(IList<InventoryItem> tools, bool clickable)
        {
            foreach (var type in types.InventoryTypes.GroupBy(t => t.Key, StringComparer.OrdinalIgnoreCase).Select(g => g.First()))
            {
                var key = type.Key;
                yield return new StatLine(type.Name, tools.Count(i => string.Equals(i.Record.Type, key, StringComparison.OrdinalIgnoreCase)), string.Empty,
                    clickable ? () => OpenToolRecords(key) : (Action?)null);
            }
        }

        // Durum sırası türdeki sırayla; bilinmeyen sonda, durumsuz en sonda.
        private static int StatusRank(IList<string> order, string status)
        {
            if (status.Length == 0)
            {
                return int.MaxValue;
            }

            var index = order.ToList().FindIndex(n => string.Equals(n, status, StringComparison.CurrentCultureIgnoreCase));
            return index >= 0 ? index : int.MaxValue - 1;
        }

        private static bool IsRequestOpen(StockTimelineCard card)
        {
            var journal = card.Item?.Journal;
            return journal != null && journal.RequestType.Length > 0 && !IsRequestClosed(journal.RequestStatus);
        }

        private void OpenStockRecords()
        {
            HomeTab = 2;
            IsViewStocks = true;
        }

        private void OpenToolRecords(string key)
        {
            HomeTab = 2;
            IsViewTools = true;
            var filter = ToolKindFilters.FirstOrDefault(f => string.Equals(f.Key, key, StringComparison.OrdinalIgnoreCase));
            if (filter != null)
            {
                // Seçmek süzgeci uygular (HomeRecordFilter seçilince türü seçer).
                filter.IsSelected = true;
            }
        }
    }
}
