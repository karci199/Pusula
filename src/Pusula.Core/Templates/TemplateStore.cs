using System;
using System.IO;
using Pusula.Core.Tracking;

namespace Pusula.Core.Templates
{
    /// <summary>
    /// Şablon dosyasını (<c>&lt;kök&gt;\_PUSULA\sablon.json</c>) okur ve yazar; yoksa varsayılanı oluşturur.
    /// </summary>
    public static class TemplateStore
    {
        /// <summary>Kökte uygulamaya ait gizli klasör.</summary>
        public const string FolderName = "_PUSULA";

        /// <summary>Şablon dosyasının adı.</summary>
        public const string FileName = "sablon.json";

        /// <summary>Verilen kökün şablon dosyası yolu.</summary>
        public static string FilePath(string root)
        {
            return Path.Combine(root, FolderName, FileName);
        }

        /// <summary>
        /// Şablonları okur. Dosya yoksa varsayılanı oluşturup yazar; bozuksa varsayılanı döner ama dosyaya dokunmaz.
        /// Eski sürüm dosya bellekte geçerli biçime çevrilir; dosya ancak kaydedilince güncellenir.
        /// </summary>
        public static TemplateSet LoadOrCreate(string root)
        {
            var path = FilePath(root);
            if (File.Exists(path))
            {
                var loaded = JsonFile.TryRead<TemplateSet>(path) ?? TemplateSet.CreateDefault();
                loaded.Normalize();
                return loaded;
            }

            var set = TemplateSet.CreateDefault();
            try
            {
                Save(root, set);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }

            return set;
        }

        /// <summary>Şablonları atomik olarak yazar ve <c>_PUSULA</c> klasörünü gizler.</summary>
        public static void Save(string root, TemplateSet set)
        {
            set.Normalize();
            JsonFile.WriteAtomic(FilePath(root), set);
            var folder = new DirectoryInfo(Path.Combine(root, FolderName));
            if ((folder.Attributes & FileAttributes.Hidden) == 0)
            {
                folder.Attributes |= FileAttributes.Hidden;
            }
        }
    }
}
