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
        public int SchemaVersion { get; set; } = 2;

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

        /// <summary>Envanter türleri (kablo, jig, yazılım …): kendi klasörü ve kaydı olanlar.</summary>
        public IEnumerable<TypeDefinition> InventoryTypes
        {
            get { return Types.Where(t => t.IsInventory); }
        }

        /// <summary>
        /// Eski dosyayı geçerli biçime çevirir (yalnızca bellekte). Sürüm 1'deki envanter türleri hiç kullanılmadığı için
        /// varsayılanlarla değiştirilir; eksik ikon, renk ve klasörler varsayılandan doldurulur.
        /// </summary>
        public void Normalize()
        {
            var defaults = CreateDefault();
            if (SchemaVersion < 2)
            {
                Types.RemoveAll(t => t.Key == "jig" || t.Key == "cable" || t.Key == "software");
                Types.AddRange(defaults.Types.Where(t => t.IsInventory));
            }

            foreach (var type in Types)
            {
                var fallback = defaults.Find(type.Key);
                if (fallback == null)
                {
                    continue;
                }

                if (type.Icon.Length == 0)
                {
                    type.Icon = fallback.Icon;
                }

                if (type.Color.Length == 0)
                {
                    type.Color = fallback.Color;
                }

                if (type.IsInventory && type.PluralName.Length == 0)
                {
                    type.PluralName = fallback.PluralName;
                }

                if (type.IsInventory && type.RecordFolders.Count == 0)
                {
                    type.RecordFolders.AddRange(fallback.RecordFolders);
                }

                if (type.IsInventory && type.GeneralFolder.Length == 0)
                {
                    type.GeneralFolder = fallback.GeneralFolder;
                    type.ProjectFolder = fallback.ProjectFolder;
                    type.StockFolder = fallback.StockFolder;
                }
            }

            SchemaVersion = 2;
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
                Icon = "E821",
                Color = "#E8B04B",
                NumberFormats = { new NumberFormat { Pattern = "####", Label = "Proje numarası" } },
            });

            set.Types.Add(new TypeDefinition
            {
                Key = StockKey,
                Name = "Stok",
                Icon = "E7B8",
                Color = "#60CDFF",
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

            set.Types.Add(Inventory("cable", "Kablo", "Kablolar", "E703", "#F7A35C", "JT-####-####", "Kablo numarası", "Kablolar", "05_Kablolar",
                new[] { "RF", "Haberleşme", "Güç / veri", "Kablaj" }, new[] { "01_Belgeler", "02_Fotograflar" }));
            set.Types.Add(Inventory("jig", "Jig", "Jigler", "E90F", "#B4A7F5", "JIG-####-####", "Jig numarası", "Jigler", "05_Jigler",
                new[] { "Mekanik", "Ölçüm adaptörü", "Montaj", "Elektriksel" }, new[] { "01_Cizimler", "02_Belgeler", "03_Fotograflar" }));

            return set;
        }

        // Kablo ve jig ortak durumları: Talep → Hazırlanıyor → Teslim alındı → Kullanımda → Üretime teslim edildi;
        // sıra dışı: Onarım / değişim gerekiyor, Çoğaltma isteniyor.
        private static TypeDefinition Inventory(string key, string name, string plural, string icon, string color, string pattern, string label, string folder, string stockFolder, string[] subTypes, string[] recordFolders)
        {
            var type = new TypeDefinition
            {
                Key = key,
                Name = name,
                PluralName = plural,
                Icon = icon,
                Color = color,
                IsInventory = true,
                GeneralFolder = @"ENVANTER\" + folder,
                ProjectFolder = @"00_Proje_Geneli\Envanter\" + folder,
                StockFolder = stockFolder,
                NumberFormats = { new NumberFormat { Pattern = pattern, Label = label } },
                Statuses =
                {
                    new StatusDefinition { Name = "Talep", Tone = StatusTone.Neutral },
                    new StatusDefinition { Name = "Hazırlanıyor", Tone = StatusTone.Active },
                    new StatusDefinition { Name = "Teslim alındı", Tone = StatusTone.Waiting },
                    new StatusDefinition { Name = "Kullanımda", Tone = StatusTone.Done },
                    new StatusDefinition { Name = "Üretime teslim edildi", Tone = StatusTone.Done },
                },
                SideStatuses =
                {
                    new StatusDefinition { Name = "Onarım / değişim gerekiyor", Tone = StatusTone.Warning, NeedsNote = true },
                    new StatusDefinition { Name = "Çoğaltma isteniyor", Tone = StatusTone.Warning },
                },
            };
            type.SubTypes.AddRange(subTypes);
            type.RecordFolders.AddRange(recordFolders);
            return type;
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
            PluralName = string.Empty;
            Folder = string.Empty;
            Icon = string.Empty;
            Color = string.Empty;
            IsInventory = false;
            GeneralFolder = string.Empty;
            ProjectFolder = string.Empty;
            StockFolder = string.Empty;
            RecordFolders = new List<string>();
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

        /// <summary>Çoğul ad (ağaçta grup başlığı: "Kablolar").</summary>
        [DataMember(Name = "pluralName", Order = 7, EmitDefaultValue = false)]
        public string PluralName { get; set; } = string.Empty;

        /// <summary>İkon: Segoe Fluent Icons kod noktası (onaltılık, örn. "E703").</summary>
        [DataMember(Name = "icon", Order = 8)]
        public string Icon { get; set; } = string.Empty;

        /// <summary>Renk (örn. "#F7A35C").</summary>
        [DataMember(Name = "color", Order = 9)]
        public string Color { get; set; } = string.Empty;

        /// <summary>Envanter türü mü (kendi klasörü ve <c>kayit.json</c> dosyası olan kayıtlar: kablo, jig …).</summary>
        [DataMember(Name = "inventory", Order = 10, EmitDefaultValue = false)]
        public bool IsInventory { get; set; }

        /// <summary>Genel düzey klasörü (köke göre, örn. <c>ENVANTER\Kablolar</c>).</summary>
        [DataMember(Name = "generalFolder", Order = 11, EmitDefaultValue = false)]
        public string GeneralFolder { get; set; } = string.Empty;

        /// <summary>Proje düzeyi klasörü (projeye göre, örn. <c>00_Proje_Geneli\Envanter\Kablolar</c>).</summary>
        [DataMember(Name = "projectFolder", Order = 12, EmitDefaultValue = false)]
        public string ProjectFolder { get; set; } = string.Empty;

        /// <summary>Stok düzeyi klasörü (stoka göre, örn. <c>05_Kablolar</c>).</summary>
        [DataMember(Name = "stockFolder", Order = 13, EmitDefaultValue = false)]
        public string StockFolder { get; set; } = string.Empty;

        /// <summary>Yeni kayıt açılırken içinde açılan klasörler (kayıt şablonu, örn. 01_Cizimler).</summary>
        [DataMember(Name = "recordFolders", Order = 14, EmitDefaultValue = false)]
        public List<string> RecordFolders { get; set; } = new List<string>();

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
