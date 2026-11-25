using System.Runtime.Serialization;

namespace Pusula.Core.Templates
{
    /// <summary>
    /// Stokta izlenen bir doküman (25.11.2026): kod (261, 032 …), ad ve hangi stoklarda arandığı (stok no öneki).
    /// Kodu boş olan satır yalnız yer tutar ("kodu sonra girilecek"); stok sayfasında görünmez.
    /// </summary>
    [DataContract]
    public sealed class DocumentDefinition
    {
        /// <summary>Serileştirici kurucuyu çağırmaz; dosyada olmayan alanlar boş metinle başlar.</summary>
        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
            Code = string.Empty;
            Name = string.Empty;
            StockPrefix = string.Empty;
            Group = string.Empty;
        }

        /// <summary>Dosya adındaki doküman kodu (<c>AB-7000-0003_261_AA</c> → <c>261</c>).</summary>
        [DataMember(Name = "code", Order = 0)]
        public string Code { get; set; } = string.Empty;

        /// <summary>Görünen ad ("Üretim test dokümanı").</summary>
        [DataMember(Name = "name", Order = 1)]
        public string Name { get; set; } = string.Empty;

        /// <summary>Yalnız bu önekle başlayan stoklarda aranır ("AB", "JY"); boşsa bütün stoklarda.</summary>
        [DataMember(Name = "stockPrefix", Order = 2)]
        public string StockPrefix { get; set; } = string.Empty;

        /// <summary>
        /// Birlikte aktarılan dokümanların grubu ("Yazılım dokümanları": 032, 132, 133); durumları aynıysa stokta tek çip
        /// olarak görünür. Boşsa doküman tek başınadır.
        /// </summary>
        [DataMember(Name = "group", Order = 3)]
        public string Group { get; set; } = string.Empty;
    }
}
