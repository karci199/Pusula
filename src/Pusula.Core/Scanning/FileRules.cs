using System;
using System.Linq;

namespace Pusula.Core.Scanning
{
    /// <summary>
    /// Sayımlara (dosya sayısı, boyut, son değişiklik, tür özeti) girmeyen dosya ve klasörler.
    /// Dosyalar silinmez, yalnız sayılmaz.
    /// </summary>
    public static class FileRules
    {
        private static readonly string[] JunkNames =
        {
            "thumbs.db", "ehthumbs.db", "ehthumbs_vista.db", "desktop.ini", ".ds_store",
        };

        private static readonly string[] JunkExtensions = { ".tmp", ".temp", ".crdownload", ".partial" };

        private static readonly string[] ArchiveNames = { "arsiv", "arşiv", "archive", "eski", "old" };

        /// <summary>
        /// Önemsiz dosya mı: Windows/macOS sistem dosyaları (Thumbs.db, desktop.ini …), Office kilit dosyaları
        /// (<c>~$Rapor.docx</c>), LibreOffice kilitleri (<c>.~lock.*#</c>), geçici ve yarım indirilmiş dosyalar.
        /// </summary>
        public static bool IsJunkFile(string fileName)
        {
            var name = System.IO.Path.GetFileName(fileName ?? string.Empty);
            if (name.Length == 0)
            {
                return true;
            }

            var lower = name.ToLowerInvariant();
            return JunkNames.Contains(lower)
                || lower.StartsWith("~$", StringComparison.Ordinal)
                || lower.StartsWith(".~lock.", StringComparison.Ordinal)
                || JunkExtensions.Any(e => lower.EndsWith(e, StringComparison.Ordinal))
                || (lower.StartsWith("~", StringComparison.Ordinal) && lower.EndsWith(".tmp", StringComparison.Ordinal));
        }

        /// <summary>
        /// Sayılmayan klasör mü: <c>_</c> ile başlayanlar (<c>_TAKIP</c>, <c>_ARSIV</c> …), <c>.</c> ile başlayanlar
        /// (<c>.obsidian</c>, <c>.git</c> …) ve arşiv klasörleri (<c>Arsiv</c>, <c>99_Arşiv</c>, <c>Archive</c>, <c>Eski</c>).
        /// </summary>
        public static bool IsSkippedFolder(string folderName)
        {
            var name = System.IO.Path.GetFileName((folderName ?? string.Empty).TrimEnd('\\', '/'));
            return name.StartsWith("_", StringComparison.Ordinal)
                || name.StartsWith(".", StringComparison.Ordinal)
                || IsArchiveFolder(name);
        }

        /// <summary>Arşiv klasörü mü (baştaki numara atılarak: <c>99_Arsiv</c> → <c>Arsiv</c>).</summary>
        public static bool IsArchiveFolder(string folderName)
        {
            var name = System.IO.Path.GetFileName((folderName ?? string.Empty).TrimEnd('\\', '/')).TrimStart('_');
            var cut = 0;
            while (cut < name.Length && (char.IsDigit(name[cut]) || name[cut] == '_' || name[cut] == '-' || name[cut] == ' '))
            {
                cut++;
            }

            // Türkçe harfler katlanır: ARŞİV, Arşiv, arsiv aynı sayılır.
            var label = name.Substring(cut).Replace('İ', 'i').Replace('I', 'i').ToLowerInvariant().Replace('ş', 's').Replace('ı', 'i');
            return ArchiveNames.Any(a => string.Equals(label, a.Replace('ş', 's'), StringComparison.Ordinal));
        }
    }
}
