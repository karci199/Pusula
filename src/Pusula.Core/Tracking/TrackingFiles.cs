using System.IO;

namespace Pusula.Core.Tracking
{
    /// <summary>
    /// Uygulamanın klasörlere koyduğu işaret dosyalarının adları ve yolları.
    /// Bir klasörün proje mi stok mu olduğunu klasör adı değil, bu dosyalar söyler.
    /// </summary>
    public static class TrackingFiles
    {
        /// <summary>Uygulama dosyalarının durduğu alt klasör.</summary>
        public const string FolderName = "_TAKIP";

        /// <summary>Proje işaret dosyası.</summary>
        public const string ProjectFileName = "proje.json";

        /// <summary>Stok işaret dosyası.</summary>
        public const string StockFileName = "stok.json";

        /// <summary>Verilen klasörün proje dosyası yolunu döner.</summary>
        public static string ProjectFile(string folder)
        {
            return Path.Combine(folder, FolderName, ProjectFileName);
        }

        /// <summary>Verilen klasörün stok dosyası yolunu döner.</summary>
        public static string StockFile(string folder)
        {
            return Path.Combine(folder, FolderName, StockFileName);
        }

        /// <summary>
        /// <c>_TAKIP</c> klasörünü Windows Gezgini'nde gizler; klasör yoksa bir şey yapmaz.
        /// </summary>
        public static void HideTrackingFolder(string folder)
        {
            var tracking = new DirectoryInfo(Path.Combine(folder, FolderName));
            if (tracking.Exists && (tracking.Attributes & FileAttributes.Hidden) == 0)
            {
                tracking.Attributes |= FileAttributes.Hidden;
            }
        }
    }
}
