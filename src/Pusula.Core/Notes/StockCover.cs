using System.Collections.Generic;
using System.IO;
using System.Linq;
using Pusula.Core.Inventory;
using Pusula.Core.Tracking;

namespace Pusula.Core.Notes
{
    /// <summary>
    /// Stokun kapağı: kökteki Obsidian notu (<c>&lt;stok no&gt;.md</c>) ve kapak fotoğrafı. 15.11.2026 düzeninde kapak
    /// <c>01_Ortak</c> içinde adında "kapak" geçen resimdir (diğer resimler gösterilmez); eski stoklarda fotoğraf klasörü
    /// (<c>07_Fotograflar</c>) okunur.
    /// Yalnızca okur.
    /// </summary>
    public sealed class StockCover
    {
        /// <summary>Ortak klasörün adı (numara öneki olmadan): spek, DM, çizim, kapak.</summary>
        public const string CommonLabel = "Ortak";

        /// <summary>Eski düzende fotoğraf klasörünün adı (numara öneki olmadan).</summary>
        public const string PhotosLabel = "Fotograflar";

        /// <summary>Eski düzende doküman klasörünün adı (numara öneki olmadan).</summary>
        public const string DocumentsLabel = "Dokumanlar";

        /// <summary>Doküman klasörü (<c>01_Ortak</c>, eski stoklarda <c>01_Dokumanlar</c>); yoksa boş.</summary>
        public string DocumentsFolder { get; private set; } = string.Empty;

        /// <summary>Doküman klasöründeki dosya sayısı (alt klasörler dahil).</summary>
        public int DocumentCount { get; private set; }

        /// <summary>Kapak fotoğrafını belirten sözcük (örn. <c>SH2000_kapak.jpg</c>).</summary>
        public const string CoverWord = "kapak";

        /// <summary>Notun yolu; yoksa boş.</summary>
        public string NoteFile { get; private set; } = string.Empty;

        /// <summary>Not gövdesinin başı.</summary>
        public string NoteExcerpt { get; private set; } = string.Empty;

        /// <summary>Fotoğraf klasörü; yoksa boş.</summary>
        public string PhotosFolder { get; private set; } = string.Empty;

        /// <summary>Fotoğraflar (ada göre); ilki kapaktır.</summary>
        public List<string> Photos { get; private set; } = new List<string>();

        /// <summary>Stok klasörünü okur.</summary>
        public static StockCover Read(string stockFolder, string stockNo)
        {
            var cover = new StockCover();
            if (!Directory.Exists(stockFolder))
            {
                return cover;
            }

            cover.NoteFile = Notes.NoteFile.Find(stockFolder, stockNo);
            cover.NoteExcerpt = cover.NoteFile.Length == 0 ? string.Empty : Notes.NoteFile.Excerpt(cover.NoteFile);
            var common = RecordContents.FindFolder(stockFolder, CommonLabel);
            if (common.Length > 0)
            {
                // 01_Ortak'ta yalnız doğrudan klasörde duran, adında "kapak" geçen resimler.
                var covers = RecordContents.ImagesUnder(common)
                    .Where(p => string.Equals(Path.GetDirectoryName(p), common, System.StringComparison.OrdinalIgnoreCase)
                        && Path.GetFileNameWithoutExtension(p).IndexOf(CoverWord, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();
                if (covers.Count > 0)
                {
                    cover.PhotosFolder = common;
                    cover.Photos = covers;
                }
            }

            if (cover.Photos.Count == 0)
            {
                cover.PhotosFolder = RecordContents.FindFolder(stockFolder, PhotosLabel);
                cover.Photos = cover.PhotosFolder.Length == 0 ? new List<string>() : RecordContents.ImagesUnder(cover.PhotosFolder);
            }

            cover.DocumentsFolder = common.Length > 0 ? common : RecordContents.FindFolder(stockFolder, DocumentsLabel);
            cover.DocumentCount = cover.DocumentsFolder.Length == 0 ? 0 : RecordContents.CountFiles(cover.DocumentsFolder);

            // Adında "kapak" geçen fotoğraf varsa kapak odur; yoksa ada göre ilki.
            var chosen = cover.Photos.FirstOrDefault(p => Path.GetFileNameWithoutExtension(p).IndexOf(CoverWord, System.StringComparison.OrdinalIgnoreCase) >= 0);
            if (chosen != null)
            {
                cover.Photos.Remove(chosen);
                cover.Photos.Insert(0, chosen);
            }

            return cover;
        }

        /// <summary>
        /// Stokun notunu (<c>&lt;stok no&gt;.md</c>) YAML bloğuyla oluşturur; varsa dokunmaz. Notun yolunu, yazılamazsa boş döner.
        /// </summary>
        public static string CreateNote(string stockFolder, StockInfo info, string project)
        {
            var number = info.StockNo.Length > 0 ? info.StockNo : Path.GetFileName(stockFolder);
            var fields = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("stok", number),
                new KeyValuePair<string, string>("ad", info.Name),
                new KeyValuePair<string, string>("proje", project),
                new KeyValuePair<string, string>("sablon", info.Template),
            };
            var heading = info.Name.Length == 0 ? number : number + " " + info.Name;
            return Notes.NoteFile.Create(Path.Combine(stockFolder, number + ".md"), fields, heading);
        }
    }
}
