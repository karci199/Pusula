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
        public const int CurrentSchemaVersion = 3;

        /// <summary>Yeni şablonda hazır gelen temel modüllerin anahtarları.</summary>
        public static readonly string[] BasicModuleKeys = { "other", "documents", "testResults", "photos", "archive" };

        /// <summary>Serileştirici kurucuyu çağırmaz; dosyada olmayan alanlar boş değerle başlar.</summary>
        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
            SchemaVersion = 1;
            ProjectFolders = new List<string>();
            LegacyCoreFolders = null;
            Modules = new List<ModuleDefinition>();
            Templates = new List<TemplatePreset>();
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
            set.Modules.Add(new ModuleDefinition { Key = "other", Name = "Diğer", Folder = "00_Diger" });
            set.Modules.Add(new ModuleDefinition { Key = "documents", Name = "Dokümanlar", Folder = "01_Dokumanlar" });
            set.Modules.Add(new ModuleDefinition { Key = "testPlans", Name = "Test planları", Folder = "02_Test_Planlari" });
            set.Modules.Add(new ModuleDefinition
            {
                Key = "testResults",
                Name = "Test sonuçları",
                Folder = "03_Test_Sonuclari",
                SubFolders = new List<string> { "VNA", "SATIMO", @"SATIMO\Ham", @"SATIMO\Analiz", "Yankisiz", "CK" },
            });
            set.Modules.Add(new ModuleDefinition
            {
                Key = "qualification",
                Name = "Kalifikasyon",
                Folder = "04_Kalifikasyon",
                SubFolders = new List<string> { "01_Isterler", "02_Test_Plani", "03_Test_Sonuclari", "04_Rapor" },
            });
            set.Modules.Add(new ModuleDefinition
            {
                Key = "jig",
                Name = "Jigler (mekanik, kablaj)",
                Folder = "05_Jigler",
                SubFolders = new List<string> { "Mekanik", "Kablaj" },
            });
            set.Modules.Add(new ModuleDefinition { Key = "software", Name = "Yazılımlar", Folder = "06_Yazilimlar" });
            set.Modules.Add(new ModuleDefinition { Key = "photos", Name = "Fotoğraflar", Folder = "07_Fotograflar" });
            set.Modules.Add(new ModuleDefinition { Key = "archive", Name = "Arşiv", Folder = "_ARSIV" });
            set.Templates.Add(new TemplatePreset { Name = "Pasif anten", Modules = BasicModuleKeys.ToList() });
            set.Templates.Add(new TemplatePreset
            {
                Name = "Döner anten",
                Modules = BasicModuleKeys.Concat(new[] { "testPlans", "qualification", "jig", "software" }).ToList(),
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
