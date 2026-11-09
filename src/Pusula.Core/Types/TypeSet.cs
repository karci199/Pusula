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
            FileTypes = new List<FileTypeDefinition>();
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

        /// <summary>Dosya türleri (uzantıya göre: Visio, STEP, Touchstone …); klasör kutucuklarında sayılır.</summary>
        [DataMember(Name = "fileTypes", Order = 3, EmitDefaultValue = false)]
        public List<FileTypeDefinition> FileTypes { get; set; } = new List<FileTypeDefinition>();

        /// <summary>Uzantısı (".vsdx" ya da "vsdx") verilen dosya türü; tanımlı değilse <c>null</c>.</summary>
        public FileTypeDefinition? FindFileType(string extension)
        {
            var ext = (extension ?? string.Empty).TrimStart('.');
            return FileTypes.FirstOrDefault(f => f.Extensions.Any(e => string.Equals(e.TrimStart('.'), ext, StringComparison.OrdinalIgnoreCase)));
        }

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


                if (type.IsInventory && type.GeneralFolder.Length == 0)
                {
                    type.GeneralFolder = fallback.GeneralFolder;
                    type.ProjectFolder = fallback.ProjectFolder;
                    type.StockFolder = fallback.StockFolder;
                }
            }

            if (FileTypes.Count == 0)
            {
                FileTypes.AddRange(defaults.FileTypes);
            }

            // Sürüm 3: hazır alanlar (kablo, jig, yazılım) ve Yazılım türü. Kullanıcının tanımladığı alanlara dokunulmaz.
            if (SchemaVersion < 3)
            {
                foreach (var fallback in defaults.Types.Where(t => t.IsInventory))
                {
                    var type = Find(fallback.Key);
                    if (type == null)
                    {
                        Types.Add(fallback);
                    }
                    else if (type.Fields.Count == 0)
                    {
                        type.Fields.AddRange(fallback.Fields);
                    }
                }
            }

            SchemaVersion = 3;
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

            set.FileTypes.AddRange(new[]
            {
                FileType("PDF", "Belge", "pdf"),
                FileType("Word", "Belge", "doc", "docx"),
                FileType("Excel", "Tablo", "xls", "xlsx", "csv"),
                FileType("PowerPoint", "Sunum", "ppt", "pptx"),
                FileType("Visio", "Şema", "vsd", "vsdx"),
                FileType("STEP", "Mekanik çizim", "step", "stp"),
                FileType("DWG", "Mekanik çizim", "dwg", "dxf"),
                FileType("SolidWorks", "Mekanik çizim", "sldprt", "sldasm", "slddrw"),
                FileType("Touchstone", "Ölçüm", "s1p", "s2p", "s3p", "s4p"),
                FileType("CST", "Simülasyon", "cst"),
                FileType("HFSS", "Simülasyon", "aedt", "hfss"),
                FileType("MATLAB", "Analiz", "m", "mat", "fig"),
                FileType("Fotoğraf", "Fotoğraf", "jpg", "jpeg", "png", "bmp", "gif", "tif", "tiff"),
                FileType("Not", "Not", "md"),
                FileType("Arşiv", "Arşiv", "zip", "7z", "rar"),
            });

            set.Types.Add(Inventory("cable", "Kablo", "Kablolar", "E703", "#F7A35C", "JT-####-####", "Kablo numarası", "Kablolar", "05_Kablolar",
                new[] { "RF", "Haberleşme", "Güç / veri", "Kablaj" }, new[] { "01_Belgeler", "02_Fotograflar" }));
            set.Types.Add(Inventory("jig", "Jig", "Jigler", "E90F", "#B4A7F5", "JIG-####-####", "Jig numarası", "Jigler", "05_Jigler",
                new[] { "Mekanik", "Ölçüm adaptörü", "Montaj", "Elektriksel" }, new[] { "01_Cizimler", "02_Belgeler", "03_Fotograflar" }));

            var software = Inventory("software", "Yazılım", "Yazılımlar", "E943", "#4CC38A", "YZ-####-####", "Yazılım numarası", "Yazilimlar", "06_Yazilimlar",
                new[] { "Test betiği", "Analiz aracı", "Firmware", "Kütüphane" }, new[] { "01_Kaynak", "02_Belgeler" });
            software.Statuses.Clear();
            software.Statuses.AddRange(new[]
            {
                new StatusDefinition { Name = "Talep", Tone = StatusTone.Neutral },
                new StatusDefinition { Name = "Geliştiriliyor", Tone = StatusTone.Active },
                new StatusDefinition { Name = "Testte", Tone = StatusTone.Waiting },
                new StatusDefinition { Name = "Kullanımda", Tone = StatusTone.Done },
            });
            software.SideStatuses.Clear();
            software.SideStatuses.Add(new StatusDefinition { Name = "Hata var", Tone = StatusTone.Warning, NeedsNote = true });
            software.SideStatuses.Add(new StatusDefinition { Name = "Kullanım dışı", Tone = StatusTone.Neutral });
            set.Types.Add(software);

            AddDefaultFields(set);
            return set;
        }

        // Türlerin hazır alanları (Türler penceresinden değiştirilebilir). Alt tür verilen alan yalnız o alt türlerde görünür.
        private static void AddDefaultFields(TypeSet set)
        {
            var connectors = new[] { "SMA (E)", "SMA (D)", "N (E)", "N (D)", "BNC", "TNC", "2.92 mm", "Banana", "Krokodil", "D-Sub 9", "D-Sub 15", "RJ45", "Açık uç" };
            set.Find("cable")?.Fields.AddRange(new[]
            {
                Field("Adet", FieldKind.Number, string.Empty),
                Field("Boy", FieldKind.Number, "m"),
                Field("Uç A", FieldKind.Choice, string.Empty, connectors),
                Field("Uç B", FieldKind.Choice, string.Empty, connectors),
                Field("Frekans", FieldKind.Text, "GHz", null, "RF"),
                Field("Faz kararlı", FieldKind.YesNo, string.Empty, null, "RF"),
                Field("Protokol", FieldKind.Choice, string.Empty, new[] { "RS422", "RS232", "RS485", "CAN", "Ethernet", "USB", "MIL-STD-1553" }, "Haberleşme"),
                Field("Pin yapısı", FieldKind.Text, string.Empty, null, "Haberleşme"),
                Field("Akım", FieldKind.Number, "A", null, "Güç / veri"),
                Field("Kesit", FieldKind.Number, "mm²", null, "Güç / veri"),
            });
            set.Find("jig")?.Fields.AddRange(new[]
            {
                Field("Adet", FieldKind.Number, string.Empty),
                Field("Malzeme", FieldKind.Choice, string.Empty, new[] { "Al 6061", "Al 7075", "Paslanmaz", "Delrin", "PLA (baskı)", "Diğer" }, "Mekanik", "Montaj", "Ölçüm adaptörü"),
                Field("Ölçüler", FieldKind.Text, "mm", null, "Mekanik", "Montaj", "Ölçüm adaptörü"),
                Field("Yüzey", FieldKind.Choice, string.Empty, new[] { "Eloksal", "Boya", "Ham" }, "Mekanik", "Ölçüm adaptörü"),
                Field("Bağlantı", FieldKind.Text, string.Empty, null, "Mekanik", "Montaj", "Ölçüm adaptörü"),
                Field("Tork", FieldKind.Number, "Nm", null, "Montaj"),
                Field("Ölçüm sistemi", FieldKind.Choice, string.Empty, new[] { "SATIMO", "Yankısız", "VNA", "Diğer" }, "Ölçüm adaptörü"),
                Field("Çizim revizyonu", FieldKind.Text, string.Empty),
            });
            set.Find("software")?.Fields.AddRange(new[]
            {
                Field("Sürüm", FieldKind.Text, string.Empty),
                Field("Dil / platform", FieldKind.Choice, string.Empty, new[] { "C#", "Python", "MATLAB", "LabVIEW", "C", "Diğer" }),
                Field("Konum", FieldKind.Text, string.Empty),
                Field("Bağlı cihaz", FieldKind.Text, string.Empty, null, "Test betiği", "Firmware"),
            });
        }

        private static FieldDefinition Field(string name, string kind, string unit, string[]? choices = null, params string[] subTypes)
        {
            var field = new FieldDefinition { Key = FieldDefinition.KeyFrom(name), Name = name, Kind = kind, Unit = unit };
            if (choices != null)
            {
                field.Choices.AddRange(choices);
            }

            field.SubTypes.AddRange(subTypes);
            return field;
        }

        private static FileTypeDefinition FileType(string name, string group, params string[] extensions)
        {
            var type = new FileTypeDefinition { Name = name, Group = group };
            type.Extensions.AddRange(extensions);
            return type;
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
            Fields = new List<FieldDefinition>();
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

        /// <summary>Türe özel alanlar (konnektör, uzunluk …); kayıt sayfasında doldurulur.</summary>
        [DataMember(Name = "fields", Order = 15, EmitDefaultValue = false)]
        public List<FieldDefinition> Fields { get; set; } = new List<FieldDefinition>();

        /// <summary>Alt türe göre geçerli alanlar (alanın alt tür listesi boşsa her alt türde geçerlidir).</summary>
        public IEnumerable<FieldDefinition> FieldsFor(string subType)
        {
            return Fields.Where(f => f.SubTypes.Count == 0 || f.SubTypes.Any(s => string.Equals(s, subType, StringComparison.OrdinalIgnoreCase)));
        }

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

    /// <summary>Uzantıya göre tanınan dosya türü (örn. Visio: vsd, vsdx · Şema).</summary>
    [DataContract]
    public sealed class FileTypeDefinition
    {
        /// <summary>Serileştirici kurucuyu çağırmaz; dosyada olmayan alanlar boş değerle başlar.</summary>
        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
            Name = string.Empty;
            Group = string.Empty;
            Extensions = new List<string>();
        }

        /// <summary>Görünen ad ("Visio").</summary>
        [DataMember(Name = "name", Order = 0)]
        public string Name { get; set; } = string.Empty;

        /// <summary>Uzantılar, noktasız ("vsd", "vsdx").</summary>
        [DataMember(Name = "extensions", Order = 1)]
        public List<string> Extensions { get; set; } = new List<string>();

        /// <summary>Grup ("Şema", "Ölçüm"); boş olabilir.</summary>
        [DataMember(Name = "group", Order = 2, EmitDefaultValue = false)]
        public string Group { get; set; } = string.Empty;
    }

    /// <summary>Alan çeşitleri.</summary>
    public static class FieldKind
    {
        /// <summary>Serbest metin.</summary>
        public const string Text = "text";

        /// <summary>Sayı (birimi olabilir).</summary>
        public const string Number = "number";

        /// <summary>Evet / hayır.</summary>
        public const string YesNo = "yesno";

        /// <summary>Listeden seçim.</summary>
        public const string Choice = "choice";

        /// <summary>Bütün çeşitler, ekrandaki sırayla.</summary>
        public static readonly string[] All = { Text, Number, YesNo, Choice };
    }

    /// <summary>Türe özel bir alanın tanımı (örn. Uzunluk, sayı, m; yalnızca RF alt türünde).</summary>
    [DataContract]
    public sealed class FieldDefinition
    {
        /// <summary>Serileştirici kurucuyu çağırmaz; dosyada olmayan alanlar boş değerle başlar.</summary>
        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
            Key = string.Empty;
            Name = string.Empty;
            Kind = FieldKind.Text;
            Unit = string.Empty;
            Choices = new List<string>();
            SubTypes = new List<string>();
        }

        /// <summary>Değişmez anahtar (<c>kayit.json</c> ve YAML'da kullanılır, örn. <c>konnektor_a</c>).</summary>
        [DataMember(Name = "key", Order = 0)]
        public string Key { get; set; } = string.Empty;

        /// <summary>Görünen ad ("Konnektör A").</summary>
        [DataMember(Name = "name", Order = 1)]
        public string Name { get; set; } = string.Empty;

        /// <summary>Çeşit: text, number, yesno, choice (<see cref="FieldKind"/>).</summary>
        [DataMember(Name = "kind", Order = 2)]
        public string Kind { get; set; } = FieldKind.Text;

        /// <summary>Birim (sayı için, örn. "m"); boş olabilir.</summary>
        [DataMember(Name = "unit", Order = 3, EmitDefaultValue = false)]
        public string Unit { get; set; } = string.Empty;

        /// <summary>Seçenekler (liste çeşidi için).</summary>
        [DataMember(Name = "choices", Order = 4, EmitDefaultValue = false)]
        public List<string> Choices { get; set; } = new List<string>();

        /// <summary>Geçerli olduğu alt türler; boşsa hepsi.</summary>
        [DataMember(Name = "subTypes", Order = 5, EmitDefaultValue = false)]
        public List<string> SubTypes { get; set; } = new List<string>();

        /// <summary>Addan anahtar üretir: küçük harf, Türkçe harfler sadeleşir, boşluk yerine alt çizgi ("Konnektör A" → "konnektor_a").</summary>
        public static string KeyFrom(string name)
        {
            var map = new Dictionary<char, char> { { 'ç', 'c' }, { 'ğ', 'g' }, { 'ı', 'i' }, { 'ö', 'o' }, { 'ş', 's' }, { 'ü', 'u' } };
            var builder = new System.Text.StringBuilder();
            foreach (var raw in (name ?? string.Empty).Trim().ToLower(new System.Globalization.CultureInfo("tr-TR")))
            {
                char mapped;
                var c = map.TryGetValue(raw, out mapped) ? mapped : raw;
                if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9'))
                {
                    builder.Append(c);
                }
                else if (builder.Length > 0 && builder[builder.Length - 1] != '_')
                {
                    builder.Append('_');
                }
            }

            return builder.ToString().Trim('_');
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
