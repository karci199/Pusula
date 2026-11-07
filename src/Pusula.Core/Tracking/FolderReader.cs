using System;
using System.Collections.Generic;
using System.IO;

namespace Pusula.Core.Tracking
{
    /// <summary>Klasörün işaret dosyasına göre türü.</summary>
    public enum FolderKind
    {
        /// <summary>İşaret dosyası yok.</summary>
        Plain,

        /// <summary>İçinde <c>_TAKIP\proje.json</c> var.</summary>
        Project,

        /// <summary>İçinde <c>_TAKIP\stok.json</c> var.</summary>
        Stock,
    }

    /// <summary>Okunan bir klasör: yolu, türü ve görünen adı.</summary>
    public sealed class FolderEntry
    {
        /// <summary>Yeni bir kayıt oluşturur.</summary>
        public FolderEntry(string path, FolderKind kind, string title)
        {
            Path = path;
            Kind = kind;
            Title = title;
        }

        /// <summary>Klasörün tam yolu.</summary>
        public string Path { get; }

        /// <summary>Klasörün türü.</summary>
        public FolderKind Kind { get; }

        /// <summary>Ağaçta görünen ad (klasör adı).</summary>
        public string Title { get; }
    }

    /// <summary>
    /// Bir klasörün doğrudan alt klasörlerini türleriyle listeler.
    /// <c>_</c> ile başlayan klasörler (<c>_TAKIP</c>, <c>_ARSIV</c> …) listeye girmez.
    /// </summary>
    public static class FolderReader
    {
        /// <summary>Alt klasörleri ada göre sıralı döner. Okunamayan klasör boş liste verir.</summary>
        public static IReadOnlyList<FolderEntry> ListChildren(string folder)
        {
            var result = new List<FolderEntry>();
            string[] children;
            try
            {
                children = Directory.GetDirectories(folder);
            }
            catch (IOException)
            {
                return result;
            }
            catch (UnauthorizedAccessException)
            {
                return result;
            }

            Array.Sort(children, StringComparer.OrdinalIgnoreCase);
            foreach (var child in children)
            {
                var name = Path.GetFileName(child);
                if (name.StartsWith("_", StringComparison.Ordinal))
                {
                    continue;
                }

                result.Add(new FolderEntry(child, KindOf(child), name));
            }

            return result;
        }

        /// <summary>Klasörün türünü işaret dosyasına bakarak bulur.</summary>
        public static FolderKind KindOf(string folder)
        {
            if (File.Exists(TrackingFiles.ProjectFile(folder)))
            {
                return FolderKind.Project;
            }

            if (File.Exists(TrackingFiles.StockFile(folder)))
            {
                return FolderKind.Stock;
            }

            return FolderKind.Plain;
        }
    }
}
