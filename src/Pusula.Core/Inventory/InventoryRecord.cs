using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;

namespace Pusula.Core.Inventory
{
    /// <summary>
    /// Envanter kaydı (kablo, jig …): <c>&lt;kayıt klasörü&gt;\_TAKIP\kayit.json</c>.
    /// Genel alanlar burada sabittir; türe özel alanlar <see cref="Fields"/> içinde durur.
    /// Durum, tarih, kişi ve konum ayrı alan değil, <see cref="Events"/> içindeki olaylardan okunur.
    /// </summary>
    [DataContract]
    public sealed class InventoryRecord
    {
        /// <summary>Kayıt dosyasının adı.</summary>
        public const string FileName = "kayit.json";

        /// <summary>Serileştirici kurucuyu çağırmaz; dosyada olmayan alanlar boş değerle başlar.</summary>
        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
            SchemaVersion = 1;
            Type = string.Empty;
            Number = string.Empty;
            Name = string.Empty;
            SubType = string.Empty;
            Note = string.Empty;
            Fields = new Dictionary<string, string>();
            Events = new List<InventoryEvent>();
            Notes = new List<Tracking.QuickNote>();
        }

        /// <summary>Dosya biçiminin sürümü.</summary>
        [DataMember(Name = "schemaVersion", Order = 0)]
        public int SchemaVersion { get; set; } = 1;

        /// <summary>Tür anahtarı (cable, jig …).</summary>
        [DataMember(Name = "type", Order = 1)]
        public string Type { get; set; } = string.Empty;

        /// <summary>Stok (numara), örn. JT-0001-0002.</summary>
        [DataMember(Name = "number", Order = 2)]
        public string Number { get; set; } = string.Empty;

        /// <summary>Tanım, örn. "RS422 test kablosu".</summary>
        [DataMember(Name = "name", Order = 3)]
        public string Name { get; set; } = string.Empty;

        /// <summary>Alt tür, örn. "Haberleşme".</summary>
        [DataMember(Name = "subType", Order = 4)]
        public string SubType { get; set; } = string.Empty;

        /// <summary>Serbest not.</summary>
        [DataMember(Name = "note", Order = 5)]
        public string Note { get; set; } = string.Empty;

        /// <summary>Türe özel alanların değerleri (anahtar → değer); şimdilik kullanılmıyor.</summary>
        [DataMember(Name = "fields", Order = 6)]
        public Dictionary<string, string> Fields { get; set; } = new Dictionary<string, string>();

        /// <summary>Uygulamadan yazılan kısa notlar (eskiden yeniye).</summary>
        [DataMember(Name = "notes", Order = 20, EmitDefaultValue = false)]
        public List<Tracking.QuickNote> Notes { get; set; } = new List<Tracking.QuickNote>();

        /// <summary>Durum olayları, eskiden yeniye.</summary>
        [DataMember(Name = "events", Order = 7)]
        public List<InventoryEvent> Events { get; set; } = new List<InventoryEvent>();

        /// <summary>Son olayın durumu; olay yoksa boş.</summary>
        public string CurrentStatus
        {
            get { return Events.Count == 0 ? string.Empty : Events[Events.Count - 1].Status; }
        }

        /// <summary>Son olay; yoksa <c>null</c>.</summary>
        public InventoryEvent? LastEvent
        {
            get { return Events.Count == 0 ? null : Events[Events.Count - 1]; }
        }

        /// <summary>Şu anki konum: konum yazılmış en son olaydan.</summary>
        public string Location
        {
            get { return Events.LastOrDefault(e => e.Location.Length > 0)?.Location ?? string.Empty; }
        }

        /// <summary>Adı verilen durumun ilk olayı (örn. "Talep" → talep tarihi ve talep eden); yoksa <c>null</c>.</summary>
        public InventoryEvent? FirstEventOf(string status)
        {
            return Events.FirstOrDefault(e => string.Equals(e.Status, status, StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>Bir durum olayı: ne zaman, kim, nerede, neden.</summary>
    [DataContract]
    public sealed class InventoryEvent
    {
        /// <summary>Serileştirici kurucuyu çağırmaz; dosyada olmayan alanlar boş değerle başlar.</summary>
        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
            Status = string.Empty;
            Date = string.Empty;
            Person = string.Empty;
            Location = string.Empty;
            Note = string.Empty;
        }

        /// <summary>Yeni durum.</summary>
        [DataMember(Name = "status", Order = 0)]
        public string Status { get; set; } = string.Empty;

        /// <summary>Tarih (yyyy-MM-dd).</summary>
        [DataMember(Name = "date", Order = 1)]
        public string Date { get; set; } = string.Empty;

        /// <summary>Kişi.</summary>
        [DataMember(Name = "person", Order = 2)]
        public string Person { get; set; } = string.Empty;

        /// <summary>Konum (boşsa önceki konum geçerli).</summary>
        [DataMember(Name = "location", Order = 3)]
        public string Location { get; set; } = string.Empty;

        /// <summary>Not.</summary>
        [DataMember(Name = "note", Order = 4)]
        public string Note { get; set; } = string.Empty;

        /// <summary><see cref="Date"/> tarih olarak; okunamazsa <c>null</c>.</summary>
        public DateTime? DateValue
        {
            get
            {
                DateTime value;
                return DateTime.TryParseExact(Date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out value)
                    ? value
                    : (DateTime?)null;
            }
        }

        /// <summary>Olay oluşturur.</summary>
        public static InventoryEvent Create(string status, DateTime date, string person, string location, string note)
        {
            return new InventoryEvent
            {
                Status = status ?? string.Empty,
                Date = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                Person = (person ?? string.Empty).Trim(),
                Location = (location ?? string.Empty).Trim(),
                Note = (note ?? string.Empty).Trim(),
            };
        }
    }
}
