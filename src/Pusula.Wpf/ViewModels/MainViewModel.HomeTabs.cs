using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Pusula.Core.Notes;
using Pusula.Core.Testing;
using Pusula.Core.Types;
using Pusula.Core.Tracking;
using Pusula.Core.Scanning;
using Pusula.Wpf.Properties;

namespace Pusula.Wpf.ViewModels
{
    /// <summary>
    /// Ana sayfanın alt kısmı: sekmeler (Günlük, Yapılacaklar, Kayıtlar). Günlük bütün stokların zaman çizelgesi
    /// kayıtlarını stok sayfasındaki kartlarla gösterir; Yapılacaklar ve Kayıtlar da gerçek veriden.
    /// </summary>
    public sealed partial class MainViewModel
    {
        private int homeTab;
        private List<StockTimelineCard> homeEvents = new List<StockTimelineCard>();

        /// <summary>Seçili sekme: 0 Günlük, 1 Yapılacaklar, 2 Kayıtlar, 3 İstatistik.</summary>
        public int HomeTab
        {
            get { return homeTab; }
            set
            {
                if (SetProperty(ref homeTab, value))
                {
                    OnPropertyChanged(nameof(IsHomeRecordsTab));
                    OnPropertyChanged(nameof(IsHomeRangeTab));
                }
            }
        }

        /// <summary>Kayıtlar sekmesi açık mı (zaman süzgeci yerine tür süzgeci görünür).</summary>
        public bool IsHomeRecordsTab
        {
            get { return homeTab == 2; }
        }

        /// <summary>Zaman süzgeci görünür mü (yalnız Günlük ve Yapılacaklar sekmelerinde).</summary>
        public bool IsHomeRangeTab
        {
            get { return homeTab < 2; }
        }

        /// <summary>Günlük: gün gün kartlar (seçili zaman aralığı, geriye doğru).</summary>
        public ObservableCollection<StockTimelineDay> HomeTimeline { get; } = new ObservableCollection<StockTimelineDay>();

        /// <summary>Günlük boş mu (seçili aralıkta olay yok).</summary>
        public bool IsHomeTimelineEmpty
        {
            get { return HomeTimeline.Count == 0; }
        }

        // Seçili aralığın başlangıcı (geçmiş için): bugün, bu haftanın pazartesisi, ayın biri; Hepsi'de yok.
        private DateTime? RangeStart()
        {
            var today = DateTime.Today;
            switch (homeRange)
            {
                case "today":
                    return today;
                case "week":
                    var fromMonday = ((int)today.DayOfWeek + 6) % 7;
                    return today.AddDays(-fromMonday);
                case "month":
                    return new DateTime(today.Year, today.Month, 1);
                default:
                    return null;
            }
        }

        private void FilterHomeTimeline()
        {
            var start = RangeStart();
            HomeTimeline.Clear();
            foreach (var day in GroupDays(homeEvents.Where(c => !start.HasValue || c.When.Date >= start.Value).OrderByDescending(c => c.When)))
            {
                HomeTimeline.Add(day);
            }

            OnPropertyChanged(nameof(IsHomeTimelineEmpty));
            BuildHomeEmptySummary();
        }

        /// <summary>Günlük boşken son olay: "Son olay 12.11.2026 · AB-7000-0003 · Kalibrasyon"; hiç olay yoksa boş.</summary>
        public string HomeLastEventText { get; private set; } = string.Empty;

        /// <summary>Günlük boşken özet kartlar: geciken iş, açık iş, değişiklik bekleyen doküman (25.11.2026, genel düzen).</summary>
        public ObservableCollection<StatLine> HomeEmptyCards { get; } = new ObservableCollection<StatLine>();

        /// <summary>Günlük boşken son açılanlar (en çok üç).</summary>
        public ObservableCollection<TreeNodeViewModel> HomeEmptyRecent { get; } = new ObservableCollection<TreeNodeViewModel>();

        /// <summary>Son açılan var mı.</summary>
        public bool HasHomeEmptyRecent
        {
            get { return HomeEmptyRecent.Count > 0; }
        }

        // Seçili aralıkta olay yoksa: son olay, sayılar ve son açılanlar (yalnız boşken hesaplanır).
        private void BuildHomeEmptySummary()
        {
            HomeEmptyCards.Clear();
            HomeEmptyRecent.Clear();
            HomeLastEventText = string.Empty;
            if (HomeTimeline.Count == 0)
            {
                var last = homeEvents.OrderByDescending(c => c.When).FirstOrDefault();
                if (last != null)
                {
                    HomeLastEventText = string.Format(CultureInfo.CurrentCulture, Resources.Home_LastEvent, last.When.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture), last.Source, last.Title);
                }

                var late = allHomeTasks.Count(t => t.IsLate);
                HomeEmptyCards.Add(new StatLine(Resources.Stats_Late, late, late > 0 ? StatusTone.Failed : string.Empty, () => HomeTab = 1));
                HomeEmptyCards.Add(new StatLine(Resources.Home_OpenTasks, allHomeTasks.Count, string.Empty, () => HomeTab = 1));
                var pending = PendingDocumentStocks();
                HomeEmptyCards.Add(new StatLine(Resources.Home_PendingDocuments, pending.Count, pending.Count > 0 ? StatusTone.Waiting : string.Empty,
                    pending.Count > 0 ? () => SelectNodeByPath(pending[0]) : (Action?)null));
                foreach (var node in RecentNodes.Take(3))
                {
                    HomeEmptyRecent.Add(node);
                }
            }

            OnPropertyChanged(nameof(HomeLastEventText));
            OnPropertyChanged(nameof(HasHomeEmptyRecent));
        }

        // Kapsamdaki stoklardan dokümanı "değişiklik bekliyor" olanlar (stok klasörleri; doküman klasörü okunur).
        private List<string> PendingDocumentStocks()
        {
            var result = new List<string>();
            foreach (var project in Nodes.Where(n => n.Kind == FolderKind.Project && (scopeProject == null || string.Equals(n.Path, scopeProject, StringComparison.OrdinalIgnoreCase))))
            {
                foreach (var stock in project.Children.Where(c => c.Kind == FolderKind.Stock))
                {
                    if (StockDocuments.Read(stock.Path, System.IO.Path.GetFileName(stock.Path), templates.Documents).Any(d => d.State == DocumentState.ChangePending))
                    {
                        result.Add(stock.Path);
                    }
                }
            }

            return result;
        }

        // Kapsamdaki stokların zaman çizelgesi kayıtları (ana nottaki Günlük ve olay notları); kartta stok numarası, proje rengi.
        private static List<StockTimelineCard> ReadHomeEvents(IList<TreeNodeViewModel> projects)
        {
            var cards = new List<StockTimelineCard>();
            foreach (var project in projects)
            {
                var color = string.IsNullOrEmpty(project.ProjectColor) ? ProjectColors.Palette[2] : project.ProjectColor;
                foreach (var stock in project.Children.Where(c => c.Kind == FolderKind.Stock))
                {
                    var notes = Journal.Read(NoteFile.Find(stock.Path, System.IO.Path.GetFileName(stock.Path)));
                    notes.AddRange(Pusula.Core.Notes.EventNote.ReadAll(stock.Path));
                    cards.AddRange(notes.Select(n => new StockTimelineCard(new TimelineItem(n)) { ProjectColor = color, Source = stock.Title, SourcePath = stock.Path }));
                }
            }

            return cards;
        }
    }
}
