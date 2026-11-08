using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Pusula.Core.Types
{
    /// <summary>
    /// Kökteki <c>_PUSULA\turler.json</c> içeriği: kayıt türlerinin kalıpları ve ortak etiket listesi.
    /// Yeni bir tür gerektiğinde bu dosyaya bir blok eklenir; kod değişmez.
    /// </summary>
    [DataContract]
    public sealed class TypeSet
    {
        /// <summary>Stok türünün anahtarı.</summary>
        public const string StockKey = "stock";

        /// <summary>Proje türünün anahtarı.</summary>
        public const string ProjectKey = "project";

        /// <summary>Serileştirici kurucuyu çağırmaz; dosyada olmayan alanlar boş değerle başlar.</summary>
        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
            SchemaVersion = 1;
            Types = new List<TypeDefinition>();
            Tags = new List<string>();
        }

        /// <summary>Dosya biçiminin sürümü.</summary>
        [DataMember(Name = "schemaVersion", Order = 0)]
        public int SchemaVersion { get; set; } = 1;

        /// <summary>Kayıt türleri.</summary>
        [DataMember(Name = "types", Order = 1)]
        public List<TypeDefinition> Types { get; set; } = new List<TypeDefinition>();

        /// <summary>Ortak etiket listesi (nerede / ne için: SATIMO, VNA, ÇK …).</summary>
        [DataMember(Name = "tags", Order = 2)]
        public List<string> Tags { get; set; } = new List<string>();

        /// <summary>Anahtarı verilen tür; yoksa <c>null</c>.</summary>
        public TypeDefinition? Find(string key)
        {
            return Types.FirstOrDefault(t => string.Equals(t.Key, key, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>Stok türü; dosyada yoksa varsayılandan gelir.</summary>
        public TypeDefinition Stock
        {
            get { return Find(StockKey) ?? CreateDefault().Find(StockKey)!; }
        }

        /// <summary>İlk kullanımda yazılan varsayılan içerik.</summary>
        public static TypeSet CreateDefault()
        {
            var set = new TypeSet();
            set.Tags.AddRange(new[] { "SATIMO", "VNA", "Yankısız", "ÇK", "Kalifikasyon" });

            set.Types.Add(new TypeDefinition
            {
                Key = ProjectKey,
                Name = "Proje",
                NumberFormats = { new NumberFormat { Pattern = "####", Label = "Proje numarası" } },
            });

            set.Types.Add(new TypeDefinition
            {
                Key = StockKey,
                Name = "Stok",
                NumberFormats =
                {
                    new NumberFormat { Pattern = "JY-####-####", Label = "Geliştirme numarası" },
                    new NumberFormat { Pattern = "@@-####-####", Label = "Stok numarası" },
                    new NumberFormat { Pattern = "####-####-####", Label = "Stok numarası" },
                },
                Statuses =
                {
                    new StatusDefinition { Name = "Hazırlık", Tone = StatusTone.Neutral },
                    new StatusDefinition { Name = "Ölçüm bekliyor", Tone = StatusTone.Waiting },
                    new StatusDefinition { Name = "Testte", Tone = StatusTone.Active },
                    new StatusDefinition { Name = "Tamamlandı", Tone = StatusTone.Done },
                    new StatusDefinition { Name = "Üretime teslim edildi", Tone = StatusTone.Done },
                },
                SideStatuses =
                {
                    new StatusDefinition { Name = "Beklemede", Tone = StatusTone.Warning, NeedsNote = true },
                },
            });

            set.Types.Add(new TypeDefinition
            {
                Key = "jig",
                Name = "Jig",
                Folder = @"02_ENVANTER\Jigler",
                NumberFormats = { new NumberFormat { Pattern = "JIG-####-####", Label = "Jig numarası" } },
                SubTypes = { "Mekanik", "Elektriksel", "Ölçüm adaptörü", "Montaj" },
                Statuses =
                {
                    new StatusDefinition { Name = "Tasarımda", Tone = StatusTone.Neutral },
                    new StatusDefinition { Name = "Üretimde", Tone = StatusTone.Active },
                    new StatusDefinition { Name = "Hazır", Tone = StatusTone.Done },
                },
                SideStatuses =
                {
                    new StatusDefinition { Name = "Tamirde", Tone = StatusTone.Warning, NeedsNote = true },
                    new StatusDefinition { Name = "Kalibrasyon bekliyor", Tone = StatusTone.Warning },
                    new StatusDefinition { Name = "Kullanım dışı", Tone = StatusTone.Neutral },
                },
            });

            set.Types.Add(new TypeDefinition
            {
                Key = "cable",
                Name = "Kablo",
                Folder = @"02_ENVANTER\Kablolar",
                NumberFormats = { new NumberFormat { Pattern = "KBL-####-####", Label = "Kablo numarası" } },
                SubTypes = { "RF kablo", "EMC kablosu", "Güç / veri kablosu" },
                Statuses =
                {
                    new StatusDefinition { Name = "Sipariş", Tone = StatusTone.Neutral },
                    new StatusDefinition { Name = "Geldi", Tone = StatusTone.Active },
                    new StatusDefinition { Name = "Kullanımda", Tone = StatusTone.Done },
                },
                SideStatuses =
                {
                    new StatusDefinition { Name = "Arızalı", Tone = StatusTone.Warning, NeedsNote = true },
                    new StatusDefinition { Name = "Kalibrasyon bekliyor", Tone = StatusTone.Warning },
                },
            });

            set.Types.Add(new TypeDefinition
            {
                Key = "software",
                Name = "Yazılım",
                Folder = @"02_ENVANTER\Yazilimlar",
                SubTypes = { "Analiz aracı", "Test betiği", "Firmware", "Kütüphane" },
                Statuses =
                {
                    new StatusDefinition { Name = "Geliştiriliyor", Tone = StatusTone.Active },
                    new StatusDefinition { Name = "Kullanımda", Tone = StatusTone.Done },
                },
                SideStatuses =
                {
                    new StatusDefinition { Name = "Kullanım dışı", Tone = StatusTone.Neutral },
                },
            });

            return set;
        }
    }

    /// <summary>Bir kayıt türünün kalıbı.</summary>
    [DataContract]
    public sealed class TypeDefinition
    {
        /// <summary>Serileştirici kurucuyu çağırmaz; dosyada olmayan alanlar boş değerle başlar.</summary>
        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
            Key = string.Empty;
            Name = string.Empty;
            Folder = string.Empty;
            NumberFormats = new List<NumberFormat>();
            SubTypes = new List<string>();
            Statuses = new List<StatusDefinition>();
            SideStatuses = new List<StatusDefinition>();
        }

        /// <summary>Değişmez anahtar (örn. stock, jig).</summary>
        [DataMember(Name = "key", Order = 0)]
        public string Key { get; set; } = string.Empty;

        /// <summary>Görünen ad.</summary>
        [DataMember(Name = "name", Order = 1)]
        public string Name { get; set; } = string.Empty;

        /// <summary>Kayıtların kökte durduğu klasör (envanter türleri için); boş olabilir.</summary>
        [DataMember(Name = "folder", Order = 2, EmitDefaultValue = false)]
        public string Folder { get; set; } = string.Empty;

        /// <summary>Kabul edilen numara biçimleri; ilk uyan biçimin etiketi kullanılır.</summary>
        [DataMember(Name = "numberFormats", Order = 3)]
        public List<NumberFormat> NumberFormats { get; set; } = new List<NumberFormat>();

        /// <summary>Alt türler (tek seçim: "bu ne?").</summary>
        [DataMember(Name = "subTypes", Order = 4)]
        public List<string> SubTypes { get; set; } = new List<string>();

        /// <summary>Sırayla ilerleyen durumlar (aşama çubuğu).</summary>
        [DataMember(Name = "statuses", Order = 5)]
        public List<StatusDefinition> Statuses { get; set; } = new List<StatusDefinition>();

        /// <summary>Sıra dışı durumlar (her aşamada seçilebilir: Beklemede, Tamirde …).</summary>
        [DataMember(Name = "sideStatuses", Order = 6)]
        public List<StatusDefinition> SideStatuses { get; set; } = new List<StatusDefinition>();

        /// <summary>Numaraya uyan ilk biçim; uymuyorsa <c>null</c>.</summary>
        public NumberFormat? MatchNumber(string number)
        {
            return NumberFormats.FirstOrDefault(f => NumberPattern.IsMatch(f.Pattern, number));
        }

        /// <summary>Biçim tanımlı değilse her numara geçerlidir; tanımlıysa en az birine uymalı.</summary>
        public bool AcceptsNumber(string number)
        {
            return NumberFormats.Count == 0 || MatchNumber(number) != null;
        }

        /// <summary>Adı verilen durum (sıralı ya da sıra dışı); yoksa <c>null</c>.</summary>
        public StatusDefinition? FindStatus(string name)
        {
            return Statuses.Concat(SideStatuses).FirstOrDefault(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>İlk (başlangıç) durumun adı; durum tanımlı değilse boş.</summary>
        public string InitialStatus
        {
            get { return Statuses.Count == 0 ? string.Empty : Statuses[0].Name; }
        }
    }

    /// <summary>Numara biçimi ve etiketi.</summary>
    [DataContract]
    public sealed class NumberFormat
    {
        /// <summary>Serileştirici kurucuyu çağırmaz; dosyada olmayan alanlar boş değerle başlar.</summary>
        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
            Pattern = string.Empty;
            Label = string.Empty;
        }

        /// <summary>Biçim: <c>#</c> rakam, <c>@</c> harf (bkz. <see cref="NumberPattern"/>).</summary>
        [DataMember(Name = "pattern", Order = 0)]
        public string Pattern { get; set; } = string.Empty;

        /// <summary>Bu biçime uyan numaranın etiketi (örn. "Geliştirme numarası").</summary>
        [DataMember(Name = "label", Order = 1)]
        public string Label { get; set; } = string.Empty;
    }

    /// <summary>Durumun rengi / anlamı.</summary>
    public static class StatusTone
    {
        /// <summary>Başlamadı / nötr.</summary>
        public const string Neutral = "neutral";

        /// <summary>Bir şey bekleniyor.</summary>
        public const string Waiting = "waiting";

        /// <summary>Devam ediyor.</summary>
        public const string Active = "active";

        /// <summary>Bitti.</summary>
        public const string Done = "done";

        /// <summary>Dikkat (beklemede, arızalı …).</summary>
        public const string Warning = "warning";

        /// <summary>Başarısız (test kaldı).</summary>
        public const string Failed = "failed";

        /// <summary>Değer yok (tabloda "—").</summary>
        public const string None = "none";
    }

    /// <summary>Bir durum: ad, renk, not gerekip gerekmediği.</summary>
    [DataContract]
    public sealed class StatusDefinition
    {
        /// <summary>Serileştirici kurucuyu çağırmaz; dosyada olmayan alanlar boş değerle başlar.</summary>
        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
            Name = string.Empty;
            Tone = StatusTone.Neutral;
            NeedsNote = false;
        }

        /// <summary>Durum adı.</summary>
        [DataMember(Name = "name", Order = 0)]
        public string Name { get; set; } = string.Empty;

        /// <summary>Renk / anlam: neutral, waiting, active, done, warning.</summary>
        [DataMember(Name = "tone", Order = 1)]
        public string Tone { get; set; } = StatusTone.Neutral;

        /// <summary>Seçilirken kısa not istenir mi (örn. Beklemede: neden?).</summary>
        [DataMember(Name = "needsNote", Order = 2, EmitDefaultValue = false)]
        public bool NeedsNote { get; set; }
    }
}
