using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace Pusula.Core.Templates
{
    /// <summary>
    /// Kökteki <c>_PUSULA\sablon.json</c> içeriği: yeni projede açılan klasörler, modül kütüphanesi
    /// ve şablonlar. Her şablon, stokta açılacak modüllerin tam listesini tutar.
    /// </summary>
    [DataContract]
    public sealed class TemplateSet
    {
        /// <summary>Geçerli dosya biçimi sürümü.</summary>
        public const int CurrentSchemaVersion = 6;

        /// <summary>Ortak klasörün içindeki fotoğraf klasörü (<c>01_Ortak\Fotograflar</c>).</summary>
        public const string PhotosFolder = "Fotograflar";

        /// <summary>Ortak klasörün içindeki doküman klasörü (<c>01_Ortak\Dokumanlar</c>; 25.11.2026).</summary>
        public const string DocumentsFolder = "Dokumanlar";

        /// <summary>Ortak klasörün içinde tasarımcıdan gelen girdiler (spek, DM, çizim, ÇK/EMC gerekleri; 25.11.2026).</summary>
        public const string InputsFolder = "Girdiler";

        /// <summary>Birlikte aktarılan yazılım dokümanlarının grubu (032, 132, 133).</summary>
        public const string SoftwareDocumentsGroup = "Yazılım dokümanları";

        /// <summary>Yeni şablonda hazır gelen temel modüllerin anahtarları.</summary>
        public static readonly string[] BasicModuleKeys = { "common", "tests" };

        // Sürüm 4 (16.11.2026): eski varsayılan modül klasörleri → yeni modül anahtarı (15.11.2026 klasör düzeni).
        private static readonly Dictionary<string, string> LegacyModuleMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "00_Diger", "common" },
            { "01_Dokumanlar", "common" },
            { "07_Fotograflar", "common" },
            { "02_Test_Planlari", "tests" },
            { "03_Test_Sonuclari", "tests" },
            { "04_Kalifikasyon", "tests" },
            { "05_Jigler", "testTools" },
            { "05_Kablolar", "testTools" },
            { "06_Yazilimlar", "testTools" },
        };

        /// <summary>Serileştirici kurucuyu çağırmaz; dosyada olmayan alanlar boş değerle başlar.</summary>
        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
            SchemaVersion = 1;
            ProjectFolders = new List<string>();
            LegacyCoreFolders = null;
            Modules = new List<ModuleDefinition>();
            Templates = new List<TemplatePreset>();
            Documents = new List<DocumentDefinition>();
        }

        /// <summary>Dosya biçiminin sürümü.</summary>
        [DataMember(Name = "schemaVersion", Order = 0)]
        public int SchemaVersion { get; set; } = CurrentSchemaVersion;

        /// <summary>Her yeni projede açılan klasörler.</summary>
        [DataMember(Name = "projectFolders", Order = 1)]
        public List<string> ProjectFolders { get; set; } = new List<string>();

        /// <summary>Sürüm 1'deki çekirdek klasör listesi; okunurken çevrilir, yazılmaz.</summary>
        [DataMember(Name = "stockCoreFolders", Order = 2, EmitDefaultValue = false)]
        public List<string>? LegacyCoreFolders { get; set; }

        /// <summary>Modül kütüphanesi (şablonlar buradan seçer).</summary>
        [DataMember(Name = "stockModules", Order = 3)]
        public List<ModuleDefinition> Modules { get; set; } = new List<ModuleDefinition>();

        /// <summary>Stok şablonları.</summary>
        [DataMember(Name = "stockTemplates", Order = 4)]
        public List<TemplatePreset> Templates { get; set; } = new List<TemplatePreset>();

        /// <summary>Stokta izlenen dokümanlar (kod, ad, stok öneki); Şablonlar penceresinden düzenlenir.</summary>
        [DataMember(Name = "documents", Order = 5)]
        public List<DocumentDefinition> Documents { get; set; } = new List<DocumentDefinition>();

        /// <summary>
        /// Varsayılan doküman listesi (25.11.2026, kullanıcı): 261 üretim test dokümanı; yazılımlarda 032 kaynak kod,
        /// 132 çalıştırılabilir kod, 133 açıklama. Test gereksinim ve test planı dokümanlarının kodu sonra girilecek (boş).
        /// </summary>
        public static List<DocumentDefinition> DefaultDocuments()
        {
            return new List<DocumentDefinition>
            {
                new DocumentDefinition { Code = "261", Name = "Üretim test dokümanı", StockPrefix = "AB" },
                new DocumentDefinition { Code = string.Empty, Name = "Test gereksinim dokümanı" },
                new DocumentDefinition { Code = string.Empty, Name = "Test planı dokümanı" },
                new DocumentDefinition { Code = "032", Name = "Kaynak kod", StockPrefix = "JY", Group = SoftwareDocumentsGroup },
                new DocumentDefinition { Code = "132", Name = "Çalıştırılabilir kod", StockPrefix = "JY", Group = SoftwareDocumentsGroup },
                new DocumentDefinition { Code = "133", Name = "Açıklama dokümanı", StockPrefix = "JY", Group = SoftwareDocumentsGroup },
            };
        }

        /// <summary>Anahtarı verilen modüllerin klasörleri ve alt klasörleri (stoka göre göreli yollar).</summary>
        public IReadOnlyList<string> StockFoldersFor(IEnumerable<string> moduleKeys)
        {
            var keys = new HashSet<string>(moduleKeys, StringComparer.OrdinalIgnoreCase);
            return Modules
                .Where(m => keys.Contains(m.Key))
                .SelectMany(m => m.AllFolders())
                .ToList();
        }

        /// <summary>Modülleri klasör adına göre sıralar; <c>_</c> ile başlayanlar en sona.</summary>
        public static IEnumerable<ModuleDefinition> Sorted(IEnumerable<ModuleDefinition> modules)
        {
            return modules
                .OrderBy(m => m.Folder.StartsWith("_", StringComparison.Ordinal))
                .ThenBy(m => m.Folder, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>Eski sürüm dosyayı geçerli biçime çevirir (yalnızca bellekte).</summary>
        public void Normalize()
        {
            var defaults = CreateDefault().Modules;

            // Sürüm 1: çekirdek klasör listesi → "her stokta" modüller.
            if (LegacyCoreFolders != null)
            {
                foreach (var folder in LegacyCoreFolders)
                {
                    if (!Modules.Any(m => string.Equals(m.Folder, folder, StringComparison.OrdinalIgnoreCase)))
                    {
                        Modules.Add(new ModuleDefinition { Key = folder, Name = ModuleDefinition.NameFromFolder(folder), Folder = folder, Core = true });
                    }
                }

                LegacyCoreFolders = null;
            }

            // Sürüm 1–2: "her stokta" modüller her şablonun listesine açıkça eklenir.
            if (SchemaVersion < 3)
            {
                var coreKeys = Modules.Where(m => m.Core).Select(m => m.Key).ToList();
                foreach (var template in Templates)
                {
                    foreach (var key in coreKeys)
                    {
                        if (!template.Modules.Contains(key, StringComparer.OrdinalIgnoreCase))
                        {
                            template.Modules.Add(key);
                        }
                    }
                }

                // Klasör adından üretilmiş (Türkçe karaktersiz) adları varsayılan adlarla düzelt.
                foreach (var module in Modules)
                {
                    var match = defaults.FirstOrDefault(d => string.Equals(d.Folder, module.Folder, StringComparison.OrdinalIgnoreCase));
                    if (match != null && string.Equals(module.Name.Trim(), ModuleDefinition.NameFromFolder(module.Folder), StringComparison.Ordinal))
                    {
                        module.Name = match.Name;
                    }
                }
            }

            // Sürüm 4 (16.11.2026): yeni klasör düzeni. Eski varsayılan modüller (Dokümanlar, Test sonuçları, Kalifikasyon,
            // Jigler, Yazılımlar, Fotoğraflar, Diğer) 01_Ortak · 02_Test_Araclari · 03_Testler'e döner; şablonlardaki
            // anahtarlar da çevrilir. Kullanıcının kendi eklediği modüller kalır. Diskteki klasörlere dokunulmaz.
            if (SchemaVersion < 4)
            {
                var renamed = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var module in Modules.ToList())
                {
                    string target;
                    if (LegacyModuleMap.TryGetValue(module.Folder, out target))
                    {
                        renamed[module.Key] = target;
                        Modules.Remove(module);
                    }
                }

                if (renamed.Count > 0)
                {
                    foreach (var fresh in defaults.Where(d => renamed.Values.Contains(d.Key, StringComparer.OrdinalIgnoreCase)))
                    {
                        if (!Modules.Any(m => string.Equals(m.Key, fresh.Key, StringComparison.OrdinalIgnoreCase) || string.Equals(m.Folder, fresh.Folder, StringComparison.OrdinalIgnoreCase)))
                        {
                            Modules.Add(fresh);
                        }
                    }

                    foreach (var template in Templates)
                    {
                        template.Modules = template.Modules
                            .Select(k => renamed.ContainsKey(k) ? renamed[k] : k)
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .ToList();
                    }
                }
            }

            // Sürüm 5 (21.11.2026): fotoğraflar 01_Ortak\Fotograflar'da; Ortak modülüne boş alt klasör olarak eklenir.
            if (SchemaVersion < 5)
            {
                var common = Modules.FirstOrDefault(m => string.Equals(m.Key, "common", StringComparison.OrdinalIgnoreCase));
                if (common != null && !common.SubFolders.Contains(PhotosFolder, StringComparer.OrdinalIgnoreCase))
                {
                    common.SubFolders.Add(PhotosFolder);
                }
            }

            // Sürüm 6 (25.11.2026): doküman listesi ve 01_Ortak\Dokumanlar alt klasörü.
            if (SchemaVersion < 6)
            {
                if (Documents.Count == 0)
                {
                    Documents = DefaultDocuments();
                }

                var commonModule = Modules.FirstOrDefault(m => string.Equals(m.Key, "common", StringComparison.OrdinalIgnoreCase));
                if (commonModule != null && !commonModule.SubFolders.Contains(DocumentsFolder, StringComparer.OrdinalIgnoreCase))
                {
                    commonModule.SubFolders.Insert(0, DocumentsFolder);
                }

                if (commonModule != null && !commonModule.SubFolders.Contains(InputsFolder, StringComparer.OrdinalIgnoreCase))
                {
                    commonModule.SubFolders.Add(InputsFolder);
                }
            }

            foreach (var module in Modules)
            {
                module.Core = false;
            }

            Modules = Sorted(Modules).ToList();
            SchemaVersion = CurrentSchemaVersion;
        }

        /// <summary>İlk kullanımda yazılan varsayılan içerik.</summary>
        public static TemplateSet CreateDefault()
        {
            var set = new TemplateSet();
            set.ProjectFolders.Add("00_Proje_Geneli");
            // 15.11.2026 klasör düzeni: 01_Ortak her stokta açılır (şablondan bağımsız); test araçları ve testler şablona göre.
            // Test alt klasörleri (Anten_Oruntu …) iş tanımlanınca, koşum klasörleri (SNP1_T1) koşum eklenince açılır.
            set.Modules.Add(new ModuleDefinition { Key = "common", Name = "Ortak", Folder = "01_Ortak", SubFolders = new List<string> { DocumentsFolder, PhotosFolder, InputsFolder } });
            set.Documents = DefaultDocuments();
            set.Modules.Add(new ModuleDefinition
            {
                Key = "testTools",
                Name = "Test araçları",
                Folder = "02_Test_Araclari",
                SubFolders = new List<string> { "Jig", "Kablo", "Yazilim", "Diger" },
            });
            set.Modules.Add(new ModuleDefinition
            {
                Key = "tests",
                Name = "Testler",
                Folder = "03_Testler",
                SubFolders = new List<string> { "01_Islevsel", "02_Ozellik", "03_CK_EMC", "04_Diger" },
            });
            set.Modules.Add(new ModuleDefinition { Key = "archive", Name = "Arşiv", Folder = "_ARSIV" });
            set.Templates.Add(new TemplatePreset { Name = "Pasif anten", Modules = BasicModuleKeys.ToList() });
            set.Templates.Add(new TemplatePreset
            {
                Name = "Döner anten",
                Modules = new List<string> { "common", "testTools", "tests" },
            });
            return set;
        }
    }

    /// <summary>Bir stok modülü: ad, klasör, iç içe alt klasörler.</summary>
    [DataContract]
    public sealed class ModuleDefinition
    {
        private static readonly Regex NumberedFolder = new Regex(@"^(\d+)_(.+)$");

        /// <summary>Serileştirici kurucuyu çağırmaz; dosyada olmayan alanlar boş değerle başlar.</summary>
        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
            Key = string.Empty;
            Name = string.Empty;
            Folder = string.Empty;
            Core = false;
            SubFolders = new List<string>();
        }

        /// <summary>Şablonlarda kullanılan değişmez anahtar.</summary>
        [DataMember(Name = "key", Order = 0)]
        public string Key { get; set; } = string.Empty;

        /// <summary>Arayüzde görünen ad.</summary>
        [DataMember(Name = "name", Order = 1)]
        public string Name { get; set; } = string.Empty;

        /// <summary>Stok altında açılacak klasör adı (örn. <c>03_Test_Sonuclari</c>).</summary>
        [DataMember(Name = "folder", Order = 2)]
        public string Folder { get; set; } = string.Empty;

        /// <summary>Sürüm 1–2'deki "her stokta" işareti; okunurken çevrilir, yazılmaz.</summary>
        [DataMember(Name = "core", Order = 3, EmitDefaultValue = false)]
        public bool Core { get; set; }

        /// <summary>Modül klasörünün altındaki klasörler; iç içe olabilir (örn. <c>SATIMO\Ham</c>).</summary>
        [DataMember(Name = "subFolders", Order = 4)]
        public List<string> SubFolders { get; set; } = new List<string>();

        /// <summary>Modül klasörü ve alt klasörleri (stoka göre göreli yollar).</summary>
        public IEnumerable<string> AllFolders()
        {
            yield return Folder;
            foreach (var sub in SubFolders)
            {
                yield return Path.Combine(Folder, sub);
            }
        }

        /// <summary><c>03_Test_Sonuclari</c> → (<c>03</c>, <c>Test_Sonuclari</c>); numarasızsa numara boş.</summary>
        public static Tuple<string, string> SplitFolder(string folder)
        {
            var match = NumberedFolder.Match(folder ?? string.Empty);
            if (match.Success)
            {
                return Tuple.Create(match.Groups[1].Value, match.Groups[2].Value);
            }

            return Tuple.Create(string.Empty, folder ?? string.Empty);
        }

        /// <summary>Klasör adından kaba bir görünen ad üretir (<c>03_Test_Sonuclari</c> → <c>Test Sonuclari</c>).</summary>
        public static string NameFromFolder(string folder)
        {
            return SplitFolder(folder).Item2.TrimStart('_').Replace('_', ' ');
        }
    }

    /// <summary>Şablon: stokta açılacak modüllerin listesi.</summary>
    [DataContract]
    public sealed class TemplatePreset
    {
        /// <summary>Serileştirici kurucuyu çağırmaz; dosyada olmayan alanlar boş değerle başlar.</summary>
        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
            Name = string.Empty;
            Modules = new List<string>();
        }

        /// <summary>Şablon adı.</summary>
        [DataMember(Name = "name", Order = 0)]
        public string Name { get; set; } = string.Empty;

        /// <summary>Şablondaki modül anahtarları.</summary>
        [DataMember(Name = "modules", Order = 1)]
        public List<string> Modules { get; set; } = new List<string>();
    }
}
