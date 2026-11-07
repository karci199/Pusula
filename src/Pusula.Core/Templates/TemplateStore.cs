using System;
using System.IO;
using Pusula.Core.Tracking;

namespace Pusula.Core.Templates
{
    /// <summary>
    /// Şablon dosyasını (<c>&lt;kök&gt;\_PUSULA\sablon.json</c>) okur; yoksa varsayılanı yazar.
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
        /// </summary>
        public static TemplateSet LoadOrCreate(string root)
        {
            var path = FilePath(root);
            if (File.Exists(path))
            {
                return JsonFile.TryRead<TemplateSet>(path) ?? TemplateSet.CreateDefault();
            }

            var set = TemplateSet.CreateDefault();
            try
            {
                JsonFile.WriteAtomic(path, set);
                var folder = new DirectoryInfo(Path.Combine(root, FolderName));
                folder.Attributes |= FileAttributes.Hidden;
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }

            return set;
        }
    }
}
