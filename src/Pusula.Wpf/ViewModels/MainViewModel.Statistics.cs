using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
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
    /// İstatistik sekmesi (22.11.2026, kullanıcı kararı: en genel sayılar). Ana sayfa / proje: stoklar (durumuna göre),
    /// test yardımcı araçları (türüne göre), açık işler. Stok: doğrulama (durum grupları, plansız / raporsuz), araçlar,
    /// açık işler. Doğrulama sayıları yalnız stokta. Hepsi zaten okunmuş veriden; ayrı dosya yazılmaz.
    /// </summary>
    public sealed partial class MainViewModel
    {
        /// <summary>Ana sayfa / proje sayfasının İstatistik sekmesi.</summary>
        public ObservableCollection<StatCard> HomeStatCards { get; } = new ObservableCollection<StatCard>();

        /// <summary>Stok sayfasının İstatistik sekmesi.</summary>
        public ObservableCollection<StatCard> StockStatCards { get; } = new ObservableCollection<StatCard>();

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

        // Stok: doğrulama, araçlar, açık işler (stok sayfası doldurulduktan ve çizelge kurulduktan sonra çağrılır).
        private void BuildStockStats()
        {
            StockStatCards.Clear();
            if (selectedNode == null || selectedNode.Kind != FolderKind.Stock)
            {
                return;
            }

            var kinds = verificationItems.Select(i => VerificationRow.KindOf(i.EffectiveStatus)).ToList();
            var counted = kinds.Count(k => k != 5);
            var passed = kinds.Count(VerificationRow.IsPassedKind);
            var verification = new List<StatLine>
            {
                new StatLine(Resources.Stats_Passed + (counted > 0 ? "  %" + (100 * passed / counted).ToString(CultureInfo.CurrentCulture) : string.Empty), passed, StatusTone.Done, null),
                new StatLine(Resources.Stats_Failed, kinds.Count(k => k == 2 || k == 4), StatusTone.Failed, null),
                new StatLine(Resources.Stats_Running, kinds.Count(k => k == 3 || k == 6 || k == 7 || k == 8), StatusTone.Active, null),
                new StatLine(Resources.Stats_Pending, kinds.Count(k => k == 0), string.Empty, null),
            };
            if (kinds.Any(k => k == 5))
            {
                verification.Add(new StatLine(Resources.Verify_StatusNotApplicable, kinds.Count(k => k == 5), string.Empty, null));
            }

            verification.Add(new StatLine(Resources.Stats_NoPlan, RequirementsWithoutPlan.Count, RequirementsWithoutPlan.Count > 0 ? StatusTone.Waiting : string.Empty, null));
            verification.Add(new StatLine(Resources.Stats_NoReport, PlansWithoutReport.Count, PlansWithoutReport.Count > 0 ? StatusTone.Waiting : string.Empty, null));
            StockStatCards.Add(new StatCard(Resources.Stock_TabVerification, counted, verification));

            var stockTools = inventoryItems.Where(i => StockToolRows.Any(r => string.Equals(r.Path, i.Folder, StringComparison.OrdinalIgnoreCase))).ToList();
            StockStatCards.Add(new StatCard(Resources.Records_ViewTools, stockTools.Count, ToolLines(stockTools, false)));

            var open = Detail.OpenTasks.Where(t => !t.IsDone).ToList();
            var late = open.Count(t => t.IsLate);
            StockStatCards.Add(new StatCard(Resources.Stats_OpenWork, open.Count, new[]
            {
                new StatLine(Resources.Stats_Late, late, late > 0 ? StatusTone.Failed : string.Empty, null),
                new StatLine(Resources.Stats_OpenIssues, OpenIssues.Count, string.Empty, null),
                new StatLine(Resources.Stats_OpenRequests, OpenRequests.Count, string.Empty, null),
            }));
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
