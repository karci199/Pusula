using System;
using System.Collections.Generic;
using System.Globalization;

namespace Pusula.Core.Office
{
    /// <summary>İş raporunun dönemi.</summary>
    public enum ReportPeriod
    {
        /// <summary>Tek gün.</summary>
        Day,

        /// <summary>Pazartesi – pazar.</summary>
        Week,

        /// <summary>Takvim ayı.</summary>
        Month,

        /// <summary>Takvim yılı.</summary>
        Year,

        /// <summary>Bütün kayıtlar.</summary>
        All,
    }

    /// <summary>İş raporunda bir satır: zaman çizelgesindeki bir kayıt.</summary>
    public sealed class WorkReportRow
    {
        /// <summary>Kaydın tarihi (saat varsa saatiyle).</summary>
        public DateTime Date { get; set; }

        /// <summary>Proje klasörünün adı.</summary>
        public string Project { get; set; } = string.Empty;

        /// <summary>Stok numarası (klasör adı).</summary>
        public string Stock { get; set; } = string.Empty;

        /// <summary>Stokun adı (özet sayfasında); boş olabilir.</summary>
        public string StockName { get; set; } = string.Empty;

        /// <summary>İşin türü ("Test", "Talep", "Doküman aktarımı" …).</summary>
        public string Kind { get; set; } = string.Empty;

        /// <summary>Yapılan iş (kaydın başlığı).</summary>
        public string Work { get; set; } = string.Empty;

        /// <summary>Durum (isteğe bağlı): ölçüm sonucu, talep durumu, revizyon …; boş olabilir.</summary>
        public string Status { get; set; } = string.Empty;
    }

    /// <summary>Raporun arayüzden gelen metinleri (çekirdekte sabit metin yok).</summary>
    public sealed class WorkReportLabels
    {
        /// <summary>İşler sayfasının adı.</summary>
        public string SheetWork { get; set; } = "Work";

        /// <summary>Özet sayfasının adı.</summary>
        public string SheetSummary { get; set; } = "Summary";

        /// <summary>Başlık ("Haftalık iş raporu").</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>Sağ üstte dönem ("48. hafta").</summary>
        public string PeriodTag { get; set; } = string.Empty;

        /// <summary>Başlığın altındaki satır (tarih aralığı · kapsam).</summary>
        public string Subtitle { get; set; } = string.Empty;

        /// <summary>Özet kutularının üstündeki açıklama ("Süzgece göre sayılır").</summary>
        public string FilterHint { get; set; } = string.Empty;

        /// <summary>"Kayıt" kutusu.</summary>
        public string Records { get; set; } = string.Empty;

        /// <summary>İşler tablosunun başlığı.</summary>
        public string WorkSection { get; set; } = string.Empty;

        /// <summary>Sütunlar: Tarih, Gün, Proje, Stok, Tür, Yapılan iş, Durum.</summary>
        public string[] Columns { get; set; } = new string[7];

        /// <summary>Özet sayfasının başlığı ("Projeye ve stoka göre").</summary>
        public string SummarySection { get; set; } = string.Empty;

        /// <summary>Özet sütunları: Proje, Stok, Stok adı, Kayıt, Son kayıt, Türler.</summary>
        public string[] SummaryColumns { get; set; } = new string[6];

        /// <summary>Kayıt yokken tabloda görünen satır.</summary>
        public string Empty { get; set; } = string.Empty;

        /// <summary>Sayfanın altındaki not.</summary>
        public string Footer { get; set; } = string.Empty;

        /// <summary>Durum hücresinde yeşil görünecek sözcükler (Geçti, Teslim alındı …).</summary>
        public string[] GoodWords { get; set; } = new string[0];

        /// <summary>Kırmızı görünecek sözcükler (Kaldı, Gecikti …).</summary>
        public string[] BadWords { get; set; } = new string[0];

        /// <summary>Sarı görünecek sözcükler (Açık, Devam …).</summary>
        public string[] WaitWords { get; set; } = new string[0];
    }

    /// <summary>Dönem hesabı: seçilen günü içeren gün / hafta / ay / yıl.</summary>
    public static class WorkReport
    {
        /// <summary>
        /// Dönemin ilk günü ve bitişi (bitiş hariç). <see cref="ReportPeriod.All"/> için ikisi de <c>null</c>.
        /// Hafta pazartesi başlar.
        /// </summary>
        public static void RangeOf(ReportPeriod period, DateTime day, out DateTime? from, out DateTime? to)
        {
            var d = day.Date;
            switch (period)
            {
                case ReportPeriod.Day:
                    from = d;
                    to = d.AddDays(1);
                    return;
                case ReportPeriod.Week:
                    var monday = d.AddDays(-(((int)d.DayOfWeek + 6) % 7));
                    from = monday;
                    to = monday.AddDays(7);
                    return;
                case ReportPeriod.Month:
                    from = new DateTime(d.Year, d.Month, 1);
                    to = from.Value.AddMonths(1);
                    return;
                case ReportPeriod.Year:
                    from = new DateTime(d.Year, 1, 1);
                    to = from.Value.AddYears(1);
                    return;
                default:
                    from = null;
                    to = null;
                    return;
            }
        }

        /// <summary>Tarih dönemin içinde mi.</summary>
        public static bool InRange(DateTime date, DateTime? from, DateTime? to)
        {
            return (!from.HasValue || date >= from.Value) && (!to.HasValue || date < to.Value);
        }

        /// <summary>ISO 8601 hafta numarası (pazartesi başlar, yılın ilk perşembesini içeren hafta 1).</summary>
        public static int IsoWeek(DateTime date)
        {
            var d = date.Date;
            var day = ((int)d.DayOfWeek + 6) % 7;
            var thursday = d.AddDays(3 - day);
            return ((thursday.DayOfYear - 1) / 7) + 1;
        }

        /// <summary>Tarih aralığının kısa yazımı: "23 – 29 Kasım 2026", "28 Kasım – 4 Aralık 2026", "29.12.2025 – 04.01.2026".</summary>
        public static string RangeText(DateTime from, DateTime lastDay, CultureInfo culture)
        {
            if (from.Date == lastDay.Date)
            {
                return from.ToString("d MMMM yyyy", culture);
            }

            if (from.Year != lastDay.Year)
            {
                return from.ToString("dd.MM.yyyy", culture) + " – " + lastDay.ToString("dd.MM.yyyy", culture);
            }

            if (from.Month != lastDay.Month)
            {
                return from.ToString("d MMMM", culture) + " – " + lastDay.ToString("d MMMM yyyy", culture);
            }

            return from.ToString("%d", culture) + " – " + lastDay.ToString("d MMMM yyyy", culture);
        }

        /// <summary>Önerilen dosya adı: <c>Is_raporu_2026-11-27</c>, <c>_2026-H48</c>, <c>_2026-11</c>, <c>_2026</c>, <c>_tum</c>.</summary>
        public static string FileName(string prefix, ReportPeriod period, DateTime day, string allSuffix)
        {
            var inv = CultureInfo.InvariantCulture;
            string tail;
            switch (period)
            {
                case ReportPeriod.Day:
                    tail = day.ToString("yyyy-MM-dd", inv);
                    break;
                case ReportPeriod.Week:
                    // ISO hafta yılı: haftanın perşembesinin yılı.
                    var thursday = day.Date.AddDays(3 - (((int)day.DayOfWeek + 6) % 7));
                    tail = thursday.Year.ToString(inv) + "-H" + IsoWeek(day).ToString("00", inv);
                    break;
                case ReportPeriod.Month:
                    tail = day.ToString("yyyy-MM", inv);
                    break;
                case ReportPeriod.Year:
                    tail = day.Year.ToString(inv);
                    break;
                default:
                    tail = allSuffix;
                    break;
            }

            return prefix + "_" + tail + ".xlsx";
        }

        /// <summary>Satırları tarihe göre sıralar (eskiden yeniye), aynı günde projeye ve stoka göre.</summary>
        public static List<WorkReportRow> Sorted(IEnumerable<WorkReportRow> rows)
        {
            var list = new List<WorkReportRow>(rows);
            list.Sort((a, b) =>
            {
                var c = a.Date.CompareTo(b.Date);
                if (c != 0)
                {
                    return c;
                }

                c = string.Compare(a.Project, b.Project, StringComparison.CurrentCultureIgnoreCase);
                return c != 0 ? c : string.Compare(a.Stock, b.Stock, StringComparison.CurrentCultureIgnoreCase);
            });
            return list;
        }
    }
}
