using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Pusula.Core.Templates;
using Pusula.Core.Tracking;

namespace Pusula.Core.Types
{
    /// <summary>
    /// Tür kalıbı dosyasını (<c>&lt;kök&gt;\_PUSULA\turler.json</c>) okur; yoksa varsayılanı yazar.
    /// </summary>
    public static class TypeStore
    {
        /// <summary>Tür kalıbı dosyasının adı.</summary>
        public const string FileName = "turler.json";

        /// <summary>Verilen kökün tür kalıbı dosyası yolu.</summary>
        public static string FilePath(string root)
        {
            return Path.Combine(root, TemplateStore.FolderName, FileName);
        }

        /// <summary>
        /// Türleri okur. Dosya yoksa varsayılanı oluşturup yazar; bozuksa varsayılanı döner ama dosyaya dokunmaz.
        /// </summary>
        public static TypeSet LoadOrCreate(string root)
        {
            var path = FilePath(root);
            if (File.Exists(path))
            {
                var loaded = JsonFile.TryRead<TypeSet>(path) ?? TypeSet.CreateDefault();
                loaded.Normalize();
                return loaded;
            }

            var set = TypeSet.CreateDefault();
            try
            {
                JsonFile.WriteAtomic(path, set);
                var folder = new DirectoryInfo(Path.Combine(root, TemplateStore.FolderName));
                if ((folder.Attributes & FileAttributes.Hidden) == 0)
                {
                    folder.Attributes |= FileAttributes.Hidden;
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }

            return set;
        }

        /// <summary>
        /// Dosyaların tür özeti: en çok olandan aza, "2 STEP · 1 Visio · 1 diğer". Tanımsız uzantılar "diğer" sayılır
        /// (<paramref name="otherLabel"/>); hiç dosya yoksa boş.
        /// </summary>
        public static string DescribeFiles(TypeSet set, IEnumerable<string> files, string otherLabel)
        {
            var counts = files
                .GroupBy(f => set.FindFileType(Path.GetExtension(f))?.Name ?? otherLabel)
                .Select(g => new { Name = g.Key, Count = g.Count() })
                .OrderBy(g => g.Name == otherLabel ? 1 : 0)
                .ThenByDescending(g => g.Count)
                .ThenBy(g => g.Name, StringComparer.CurrentCultureIgnoreCase);
            return string.Join(" · ", counts.Select(g => g.Count.ToString(System.Globalization.CultureInfo.CurrentCulture) + " " + g.Name));
        }

        /// <summary>Türleri dosyaya yazar (geçici dosya + yeniden adlandırma). Yazılamazsa hata mesajı, başarıda boş döner.</summary>
        public static string Save(string root, TypeSet set)
        {
            try
            {
                Pusula.Core.Scanning.DiskWatcher.MarkOwnWrite();
                Directory.CreateDirectory(Path.Combine(root, TemplateStore.FolderName));
                JsonFile.WriteAtomic(FilePath(root), set);
                return string.Empty;
            }
            catch (IOException ex)
            {
                return ex.Message;
            }
            catch (UnauthorizedAccessException ex)
            {
                return ex.Message;
            }
        }
    }
}
