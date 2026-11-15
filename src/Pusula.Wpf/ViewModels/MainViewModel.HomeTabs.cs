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
    /// <summary>
    /// Ana sayfanın alt kısmı: sekmeler (Günlük, Yapılacaklar, Kayıtlar). Günlük şimdilik örnek kartlarla
    /// (zaman çizelgesi stok sayfasıyla birlikte tasarlanacak); Yapılacaklar ve Kayıtlar gerçek veriden.
    /// </summary>
    public sealed partial class MainViewModel
    {
        private int homeTab;
        private List<HomeEvent>? sampleEvents;

        /// <summary>Seçili sekme: 0 Günlük, 1 Yapılacaklar, 2 Kayıtlar.</summary>
        public int HomeTab
        {
            get { return homeTab; }
            set
            {
                if (SetProperty(ref homeTab, value))
                {
                    OnPropertyChanged(nameof(IsHomeRecordsTab));
                }
            }
        }

        /// <summary>Kayıtlar sekmesi açık mı (zaman süzgeci yerine tür süzgeci görünür).</summary>
        public bool IsHomeRecordsTab
        {
            get { return homeTab == 2; }
        }

        /// <summary>Günlük: gün gün kartlar (seçili zaman aralığı, geriye doğru).</summary>
        public ObservableCollection<HomeDay> HomeTimeline { get; } = new ObservableCollection<HomeDay>();

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
            sampleEvents = sampleEvents ?? SampleEvents();
            var start = RangeStart();
            var today = DateTime.Today;
            HomeTimeline.Clear();
            foreach (var day in sampleEvents.Where(e => !start.HasValue || e.When.Date >= start.Value)
                .OrderByDescending(e => e.When)
                .GroupBy(e => e.When.Date))
            {
                var title = day.Key == today ? Resources.Day_Today
                    : day.Key == today.AddDays(-1) ? Resources.Day_Yesterday
                    : day.Key.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
                HomeTimeline.Add(new HomeDay(title, day.ToList()));
            }

            OnPropertyChanged(nameof(IsHomeTimelineEmpty));
        }

        // ÖRNEK veri: zaman çizelgesi tasarlanana kadar görünümü göstermek için (stok penceresi taslağındaki kartlar).
        // Kaynak numaralar ağaçtaki ilk stoklardan alınır ki tıklanınca bir yere gitsin.
        private List<HomeEvent> SampleEvents()
        {
            var project = scopeProject != null ? selectedNode : Nodes.FirstOrDefault(n => n.Kind == FolderKind.Project);
            // Renk boş kalırsa nokta ve çizgi siyah görünüyordu; projesiz durumda da paletin ilk rengi.
            var color = string.IsNullOrEmpty(project?.ProjectColor) ? ProjectColors.Palette[2] : project!.ProjectColor;
            var stocks = project?.Children.Where(c => c.Kind == FolderKind.Stock).Select(c => c.Title).ToList() ?? new List<string>();
            var a = stocks.Count > 0 ? stocks[0] : "AB-7000-0001";
            var b = stocks.Count > 1 ? stocks[1] : "AB-7000-0002";
            var c = stocks.Count > 2 ? stocks[2] : "AB-7000-0003";
            var today = DateTime.Today;
            var none = new string[0];
            return new List<HomeEvent>
            {
                new HomeEvent(today.AddHours(14).AddMinutes(20), true, b, "SNP1", "SATIMO testi başlatıldı. 2–18 GHz.", color, none),
                new HomeEvent(today.AddHours(11).AddMinutes(30), true, c, string.Empty, "Tasarımcıyla ön değerlendirme yapıldı; 12 GHz civarı ayrıntılı tarama istendi.", color, none),
                new HomeEvent(today.AddHours(10).AddMinutes(5), true, b, string.Empty, "Jig teslim alındı.", color, none),
                new HomeEvent(today.AddDays(-1).AddHours(17).AddMinutes(40), true, a, "SNP2", "Yan lob tekrar ölçüldü, sonuç klasöre kondu.", color, none),
                new HomeEvent(today.AddDays(-1).AddHours(9), true, a, "SNP2", "SATIMO testleri bitti.", color, none),
                new HomeEvent(today.AddDays(-2), false, c, string.Empty, "Test dokümanı BA1 hazırlanıyor.", color, none),
                new HomeEvent(today.AddDays(-4), false, a, string.Empty, "⚠ Masa başı düzenekte kablo arızası, değiştirildi.", color, none),
                new HomeEvent(today.AddDays(-6).AddHours(15), true, b, "SNP1", "VSWR ölçümü yapıldı, sınır içinde.", color, none),
                new HomeEvent(today.AddDays(-9), false, c, string.Empty, "İklim kabini için randevu alındı.", color, none),
                new HomeEvent(today.AddDays(-12), false, b, string.Empty, "Jig talep edildi.", color, none),
                new HomeEvent(today.AddDays(-15), false, a, string.Empty, "Test dokümanı AA yayımlandı.", color, none),
                new HomeEvent(today.AddDays(-20), false, c, string.Empty, "Kalifikasyon planı tasarımcıyla konuşuldu.", color, none),
                new HomeEvent(today.AddDays(-28), false, b, string.Empty, "Stok açıldı, fotoğraf kondu.", color, none),
                new HomeEvent(today.AddDays(-40), false, a, string.Empty, "Test dokümanı XX hazırlanmaya başlandı.", color, none),
            };
        }
    }
}
