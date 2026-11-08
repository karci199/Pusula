using System.Runtime.Serialization;

namespace Pusula.Core.Tracking
{
    /// <summary>
    /// <c>_TAKIP\stok.json</c> içeriği.
    /// </summary>
    [DataContract]
    public sealed class StockInfo
    {
        /// <summary>Serileştirici kurucuyu çağırmaz; dosyada olmayan alanlar boş değerle başlar.</summary>
        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
            SchemaVersion = 1;
            StockNo = string.Empty;
            Name = string.Empty;
            Template = string.Empty;
            Status = string.Empty;
            StatusSince = string.Empty;
            StatusNote = string.Empty;
            Inventory = new System.Collections.Generic.List<string>();
        }

        /// <summary>Dosya biçiminin sürümü.</summary>
        [DataMember(Name = "schemaVersion", Order = 0)]
        public int SchemaVersion { get; set; } = 1;

        /// <summary>Stok numarası, örn. AB-7000-0000.</summary>
        [DataMember(Name = "stockNo", Order = 1)]
        public string StockNo { get; set; } = string.Empty;

        /// <summary>Stok adı (boş olabilir).</summary>
        [DataMember(Name = "name", Order = 2)]
        public string Name { get; set; } = string.Empty;

        /// <summary>Oluşturulurken seçilen şablonun adı (bilgi amaçlı; boş olabilir).</summary>
        [DataMember(Name = "template", Order = 3)]
        public string Template { get; set; } = string.Empty;

        /// <summary>Ana durum (tür kalıbındaki durumlardan biri); boşsa henüz verilmemiş.</summary>
        [DataMember(Name = "status", Order = 4)]
        public string Status { get; set; } = string.Empty;

        /// <summary>Durumun verildiği tarih (yyyy-MM-dd).</summary>
        [DataMember(Name = "statusSince", Order = 5)]
        public string StatusSince { get; set; } = string.Empty;

        /// <summary>Durum notu (örn. Beklemede: numune gelmedi); boş olabilir.</summary>
        [DataMember(Name = "statusNote", Order = 6)]
        public string StatusNote { get; set; } = string.Empty;

        /// <summary>
        /// Stokun kullandığı genel / proje düzeyindeki envanter kayıtlarının numaraları (bağ). Stok düzeyindeki
        /// kayıtlar (<c>05_Jigler</c> …) zaten stokundur, burada tutulmaz.
        /// </summary>
        [DataMember(Name = "inventory", Order = 7, EmitDefaultValue = false)]
        public System.Collections.Generic.List<string> Inventory { get; set; } = new System.Collections.Generic.List<string>();

        /// <summary>Durumu ve tarihini ayarlar.</summary>
        public void SetStatus(string status, string note, System.DateTime when)
        {
            Status = status ?? string.Empty;
            StatusNote = note ?? string.Empty;
            StatusSince = when.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary><see cref="StatusSince"/> tarih olarak; okunamazsa <c>null</c>.</summary>
        public System.DateTime? StatusSinceDate
        {
            get
            {
                System.DateTime value;
                return System.DateTime.TryParseExact(StatusSince, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out value)
                    ? value
                    : (System.DateTime?)null;
            }
        }
    }
}
