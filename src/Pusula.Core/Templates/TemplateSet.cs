using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;

namespace Pusula.Core.Templates
{
    /// <summary>
    /// Kökteki <c>_PUSULA\sablon.json</c> içeriği: yeni projede ve stokta açılan klasörler,
    /// seçilebilir modüller ve modüllerin hazır seçimleri (şablonlar).
    /// </summary>
    [DataContract]
    public sealed class TemplateSet
    {
        /// <summary>Serileştirici kurucuyu çağırmaz; dosyada olmayan alanlar boş değerle başlar.</summary>
        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
            SchemaVersion = 1;
            ProjectFolders = new List<string>();
            CoreFolders = new List<string>();
            Modules = new List<ModuleDefinition>();
            Templates = new List<TemplatePreset>();
        }

        /// <summary>Dosya biçiminin sürümü.</summary>
        [DataMember(Name = "schemaVersion", Order = 0)]
        public int SchemaVersion { get; set; } = 1;

        /// <summary>Her yeni projede açılan klasörler.</summary>
        [DataMember(Name = "projectFolders", Order = 1)]
        public List<string> ProjectFolders { get; set; } = new List<string>();

        /// <summary>Her yeni stokta açılan klasörler.</summary>
        [DataMember(Name = "stockCoreFolders", Order = 2)]
        public List<string> CoreFolders { get; set; } = new List<string>();

        /// <summary>Stokta seçilebilir modüller.</summary>
        [DataMember(Name = "stockModules", Order = 3)]
        public List<ModuleDefinition> Modules { get; set; } = new List<ModuleDefinition>();

        /// <summary>Hazır stok şablonları.</summary>
        [DataMember(Name = "stockTemplates", Order = 4)]
        public List<TemplatePreset> Templates { get; set; } = new List<TemplatePreset>();

        /// <summary>
        /// Çekirdek klasörler + seçilen modüllerin klasörleri ve alt klasörleri (stoka göre göreli yollar).
        /// </summary>
        public IReadOnlyList<string> StockFoldersFor(IEnumerable<string> moduleKeys)
        {
            var keys = new HashSet<string>(moduleKeys, System.StringComparer.OrdinalIgnoreCase);
            var result = new List<string>(CoreFolders);
            foreach (var module in Modules.Where(m => keys.Contains(m.Key)))
            {
                result.AddRange(module.AllFolders());
            }

            return result;
        }

        /// <summary>İlk kullanımda yazılan varsayılan içerik.</summary>
        public static TemplateSet CreateDefault()
        {
            var set = new TemplateSet();
            set.ProjectFolders.Add("00_Proje_Geneli");
            set.CoreFolders.AddRange(new[] { "00_Diger", "01_Dokumanlar", "03_Test_Sonuclari", "07_Fotograflar", "_ARSIV" });
            set.Modules.Add(new ModuleDefinition { Key = "testPlans", Name = "Test planları", Folder = "02_Test_Planlari" });
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
            set.Templates.Add(new TemplatePreset { Name = "Pasif anten" });
            set.Templates.Add(new TemplatePreset
            {
                Name = "Döner anten",
                Modules = new List<string> { "testPlans", "qualification", "jig", "software" },
            });
            return set;
        }
    }

    /// <summary>Seçilebilir bir modül: ad, açılacak klasör ve alt klasörleri.</summary>
    [DataContract]
    public sealed class ModuleDefinition
    {
        /// <summary>Serileştirici kurucuyu çağırmaz; dosyada olmayan alanlar boş değerle başlar.</summary>
        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
            Key = string.Empty;
            Name = string.Empty;
            Folder = string.Empty;
            SubFolders = new List<string>();
        }

        /// <summary>Şablonlarda kullanılan anahtar.</summary>
        [DataMember(Name = "key", Order = 0)]
        public string Key { get; set; } = string.Empty;

        /// <summary>Arayüzde görünen ad.</summary>
        [DataMember(Name = "name", Order = 1)]
        public string Name { get; set; } = string.Empty;

        /// <summary>Stok altında açılacak klasör adı (sabit numaralı).</summary>
        [DataMember(Name = "folder", Order = 2)]
        public string Folder { get; set; } = string.Empty;

        /// <summary>Modül klasörünün altında açılacak klasörler.</summary>
        [DataMember(Name = "subFolders", Order = 3)]
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
    }

    /// <summary>Hazır şablon: hangi modüllerin işaretli geleceği.</summary>
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

        /// <summary>İşaretli gelen modül anahtarları.</summary>
        [DataMember(Name = "modules", Order = 1)]
        public List<string> Modules { get; set; } = new List<string>();
    }
}
