using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Pusula.Core.Notes;
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
