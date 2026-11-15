using System;
using System.Globalization;
using Pusula.Wpf.Mvvm;

namespace Pusula.Wpf.ViewModels
{
    /// <summary>Kayıtlar · Projeler satırı: istatistikler. Dosya sayısı ve boyut arka planda sonradan gelir.</summary>
    public sealed class ProjectRow : ViewModelBase
    {
        private int? files;
        private long? bytes;

        /// <summary>Satırı kurar.</summary>
        public ProjectRow(string path, string color, string code, string name, int stocks, int tools, int openTasks, DateTime? lastChange)
        {
            Path = path;
            Color = color;
            Code = code;
            Name = name;
            Stocks = stocks;
            Tools = tools;
            OpenTasks = openTasks;
            LastChange = lastChange;
        }

        /// <summary>Proje klasörü.</summary>
        public string Path { get; }

        /// <summary>Projenin rengi.</summary>
        public string Color { get; }

        /// <summary>Proje kodu.</summary>
        public string Code { get; }

        /// <summary>Proje adı.</summary>
        public string Name { get; }

        /// <summary>Stok sayısı.</summary>
        public int Stocks { get; }

        /// <summary>Projeye ve stoklarına bağlı test yardımcı aracı sayısı.</summary>
        public int Tools { get; }

        /// <summary>Projedeki açık iş sayısı.</summary>
        public int OpenTasks { get; }

        /// <summary>En son değişen stokun zamanı.</summary>
        public DateTime? LastChange { get; }

        /// <summary>"15.11.2026".</summary>
        public string LastChangeText
        {
            get { return LastChange.HasValue ? LastChange.Value.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) : string.Empty; }
        }

        /// <summary>Dosya sayısı (hesaplanana kadar <c>null</c>).</summary>
        public int? Files
        {
            get { return files; }
            set
            {
                if (SetProperty(ref files, value))
                {
                    OnPropertyChanged(nameof(FilesText));
                }
            }
        }

        /// <summary>Toplam boyut, bayt (hesaplanana kadar <c>null</c>).</summary>
        public long? Bytes
        {
            get { return bytes; }
            set
            {
                if (SetProperty(ref bytes, value))
                {
                    OnPropertyChanged(nameof(SizeText));
                }
            }
        }

        /// <summary>"1 284"; hesaplanırken "…".</summary>
        public string FilesText
        {
            get { return files.HasValue ? files.Value.ToString("N0", CultureInfo.CurrentCulture) : "…"; }
        }

        /// <summary>"3,4 GB"; hesaplanırken "…".</summary>
        public string SizeText
        {
            get { return bytes.HasValue ? FormatSize(bytes.Value) : "…"; }
        }

        /// <summary>Bayt sayısını okunur yazar (KB, MB, GB).</summary>
        public static string FormatSize(long value)
        {
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            double size = value;
            var unit = 0;
            while (size >= 1024 && unit < units.Length - 1)
            {
                size /= 1024;
                unit++;
            }

            return size.ToString(unit == 0 ? "0" : "0.#", CultureInfo.CurrentCulture) + " " + units[unit];
        }
    }

    /// <summary>Kayıtlar · Stoklar satırı. Boyut arka planda sonradan gelir.</summary>
    public sealed class StockRow : ViewModelBase
    {
        private long? bytes;

        /// <summary>Stok klasörünün toplam boyutu, bayt (hesaplanana kadar <c>null</c>).</summary>
        public long? Bytes
        {
            get { return bytes; }
            set
            {
                if (SetProperty(ref bytes, value))
                {
                    OnPropertyChanged(nameof(SizeText));
                }
            }
        }

        /// <summary>"820 MB"; hesaplanırken "…".</summary>
        public string SizeText
        {
            get { return bytes.HasValue ? ProjectRow.FormatSize(bytes.Value) : "…"; }
        }

        /// <summary>Satırı kurar.</summary>
        public StockRow(string path, string color, string number, string name, string project, string template, string status, int tools, int openTasks, DateTime? lastChange)
        {
            Path = path;
            Color = color;
            Number = number;
            Name = name;
            Project = project;
            Template = template;
            Status = status;
            Tools = tools;
            OpenTasks = openTasks;
            LastChange = lastChange;
        }

        /// <summary>Stok klasörü.</summary>
        public string Path { get; }

        /// <summary>Projenin rengi.</summary>
        public string Color { get; }

        /// <summary>Stok numarası.</summary>
        public string Number { get; }

        /// <summary>Tanım.</summary>
        public string Name { get; }

        /// <summary>Projenin kodu.</summary>
        public string Project { get; }

        /// <summary>Şablon.</summary>
        public string Template { get; }

        /// <summary>Durum.</summary>
        public string Status { get; }

        /// <summary>Bağlı test yardımcı aracı sayısı.</summary>
        public int Tools { get; }

        /// <summary>Açık iş sayısı.</summary>
        public int OpenTasks { get; }

        /// <summary>Son değişiklik.</summary>
        public DateTime? LastChange { get; }

        /// <summary>"15.11.2026".</summary>
        public string LastChangeText
        {
            get { return LastChange.HasValue ? LastChange.Value.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) : string.Empty; }
        }
    }

    /// <summary>Kayıtlar · Test yardımcı araçları satırı (jig, kablo, yazılım …).</summary>
    public sealed class ToolRow
    {
        /// <summary>Satırı kurar.</summary>
        public ToolRow(string path, string glyph, string color, string typeKey, string typeName, string number, string name, string subType, string attachedTo, string location, string status)
        {
            Path = path;
            Glyph = glyph;
            Color = color;
            TypeKey = typeKey;
            TypeName = typeName;
            Number = number;
            Name = name;
            SubType = subType;
            AttachedTo = attachedTo;
            Location = location;
            Status = status;
        }

        /// <summary>Kayıt klasörü.</summary>
        public string Path { get; }

        /// <summary>Tür simgesi.</summary>
        public string Glyph { get; }

        /// <summary>Tür rengi.</summary>
        public string Color { get; }

        /// <summary>Tür anahtarı (jig, cable, software …).</summary>
        public string TypeKey { get; }

        /// <summary>Tür adı.</summary>
        public string TypeName { get; }

        /// <summary>Numara.</summary>
        public string Number { get; }

        /// <summary>Ad.</summary>
        public string Name { get; }

        /// <summary>Alt tür.</summary>
        public string SubType { get; }

        /// <summary>Bağlı olduğu yer: Genel, proje kodu ya da stok numarası (bağlandığı stoklar dahil).</summary>
        public string AttachedTo { get; }

        /// <summary>Konum.</summary>
        public string Location { get; }

        /// <summary>Durum.</summary>
        public string Status { get; }
    }

    /// <summary>Kapsam süzgecinde bir seçenek (proje ya da stok): onay kutusu ve renk.</summary>
    public sealed class ScopeOption : ViewModelBase
    {
        private readonly Action changed;
        private bool isChecked;

        /// <summary>Seçeneği kurar; işaret değişince <paramref name="changed"/> çağrılır.</summary>
        public ScopeOption(string key, string label, string detail, string color, Action changed)
        {
            Key = key;
            Label = label;
            Detail = detail;
            Color = color;
            this.changed = changed;
        }

        /// <summary>Anahtar (proje kodu ya da stok numarası).</summary>
        public string Key { get; }

        /// <summary>Görünen ad.</summary>
        public string Label { get; }

        /// <summary>Yanındaki soluk yazı (ad / tanım).</summary>
        public string Detail { get; }

        /// <summary>Renk noktası.</summary>
        public string Color { get; }

        /// <summary>Seçili mi.</summary>
        public bool IsChecked
        {
            get { return isChecked; }
            set
            {
                if (SetProperty(ref isChecked, value))
                {
                    changed();
                }
            }
        }

        /// <summary>Bildirim göndermeden işaretler (toplu Tümü / Temizle için).</summary>
        internal void SetQuiet(bool value)
        {
            if (isChecked != value)
            {
                isChecked = value;
                OnPropertyChanged(nameof(IsChecked));
            }
        }
    }
}
