using System;
using System.Collections.Generic;
using Pusula.Wpf.Mvvm;

namespace Pusula.Wpf.ViewModels
{
    /// <summary>Ana sayfa Günlük sekmesinde bir gün: başlık ve o günün kartları.</summary>
    public sealed class HomeDay
    {
        /// <summary>Günü kurar.</summary>
        public HomeDay(string title, IList<HomeEvent> events)
        {
            Title = title;
            Events = events;
        }

        /// <summary>"Bugün", "Dün" ya da "12.11.2026".</summary>
        public string Title { get; }

        /// <summary>O günün kartları (saate göre, yeni üstte).</summary>
        public IList<HomeEvent> Events { get; }
    }

    /// <summary>
    /// Günlük kartı (stok penceresi taslağındaki zaman çizelgesi kartıyla aynı): kaynak numara, seri no, saat,
    /// metin ve metindeki <c>#</c> / <c>@</c> referansları çip olarak.
    /// </summary>
    public sealed class HomeEvent
    {
        /// <summary>Kartı kurar.</summary>
        public HomeEvent(DateTime when, bool hasTime, string source, string serial, string text, string projectColor, IList<string> refs)
        {
            When = when;
            HasTime = hasTime;
            Source = source;
            Serial = serial;
            Text = text;
            ProjectColor = projectColor;
            Refs = refs;
        }

        /// <summary>Olayın tarihi (saatsizse günün başı).</summary>
        public DateTime When { get; }

        /// <summary>Saat girilmiş mi.</summary>
        public bool HasTime { get; }

        /// <summary>"14:20"; saatsizse boş.</summary>
        public string TimeText
        {
            get { return HasTime ? When.ToString("HH:mm", System.Globalization.CultureInfo.InvariantCulture) : string.Empty; }
        }

        /// <summary>Olayın ait olduğu kayıt (stok, jig …) numarası.</summary>
        public string Source { get; }

        /// <summary>Seri no ("SNP1"); yoksa boş.</summary>
        public string Serial { get; }

        /// <summary>Seri no var mı.</summary>
        public bool HasSerial
        {
            get { return Serial.Length > 0; }
        }

        /// <summary>Kısa olay metni.</summary>
        public string Text { get; }

        /// <summary>Kaynağın projesinin rengi.</summary>
        public string ProjectColor { get; }

        /// <summary>Metindeki <c>#</c> / <c>@</c> referansları.</summary>
        public IList<string> Refs { get; }

        /// <summary>Referans var mı.</summary>
        public bool HasRefs
        {
            get { return Refs.Count > 0; }
        }
    }

    /// <summary>Ana sayfa Kayıtlar sekmesinde bir satır: proje, stok ya da envanter kaydı.</summary>
    public sealed class HomeRecord
    {
        /// <summary>Satırı kurar.</summary>
        public HomeRecord(string kind, string kindName, string glyph, string color, string number, string name, string parent, string status, string path)
        {
            Kind = kind;
            KindName = kindName;
            Glyph = glyph;
            Color = color;
            Number = number;
            Name = name;
            Parent = parent;
            Status = status;
            Path = path;
        }

        /// <summary>Tür anahtarı ("project", "stock", envanter türü anahtarı).</summary>
        public string Kind { get; }

        /// <summary>Türün adı ("Stok", "Jig" …).</summary>
        public string KindName { get; }

        /// <summary>Simge.</summary>
        public string Glyph { get; }

        /// <summary>Simge rengi.</summary>
        public string Color { get; }

        /// <summary>Numara.</summary>
        public string Number { get; }

        /// <summary>Ad / tanım.</summary>
        public string Name { get; }

        /// <summary>Üst kayıt ya da konum (stokta proje, kayıtta sahibi).</summary>
        public string Parent { get; }

        /// <summary>Durum; yoksa boş.</summary>
        public string Status { get; }

        /// <summary>Klasör (tıklayınca ağaçta seçilir).</summary>
        public string Path { get; }
    }

    /// <summary>Kayıtlar sekmesinde tür süzgecinin bir seçeneği ("Stok · 4").</summary>
    public sealed class HomeRecordFilter : ViewModelBase
    {
        private readonly Action<HomeRecordFilter> chosen;
        private bool isSelected;

        /// <summary>Seçeneği kurar; seçilince <paramref name="chosen"/> çağrılır.</summary>
        public HomeRecordFilter(string key, string label, int count, Action<HomeRecordFilter> chosen)
        {
            Key = key;
            Label = label;
            Count = count;
            this.chosen = chosen;
        }

        /// <summary>Tür anahtarı; "Hepsi" için boş.</summary>
        public string Key { get; }

        /// <summary>Görünen ad.</summary>
        public string Label { get; }

        /// <summary>Kayıt sayısı.</summary>
        public int Count { get; }

        /// <summary>Seçili mi.</summary>
        public bool IsSelected
        {
            get { return isSelected; }
            set
            {
                if (SetProperty(ref isSelected, value) && value)
                {
                    chosen(this);
                }
            }
        }
    }
}
