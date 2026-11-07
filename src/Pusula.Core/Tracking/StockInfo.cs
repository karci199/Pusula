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
    }
}
