using System.Collections.Generic;
using System.IO;
using System.Linq;
using Pusula.Core.Inventory;
using Pusula.Core.Tracking;

namespace Pusula.Core.Notes
{
    /// <summary>
    /// Stokun kapağı: kökteki Obsidian notu (<c>&lt;stok no&gt;.md</c>) ve fotoğraflar. 21.11.2026 düzeninde fotoğraflar
    /// <c>01_Ortak\Fotograflar</c> içindedir; adında "kapak" geçen kapaktır, diğerleri görünüşe göre (<c>_on</c>, <c>_arka</c> …) dizilir.
    /// Eski stoklarda <c>01_Ortak</c>'taki "kapak" resmi ya da <c>07_Fotograflar</c> okunur.
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

        /// <summary>Fotoğrafların okunduğu klasör; yoksa boş.</summary>
        public string PhotosFolder { get; private set; } = string.Empty;

        /// <summary>Fotoğrafların durması gereken klasör (<c>01_Ortak\Fotograflar</c>, henüz açılmamış olabilir); eski stokta okunan klasör.</summary>
        public string PhotosHome { get; private set; } = string.Empty;

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
                // 21.11.2026: fotoğraflar 01_Ortak\Fotograflar'da (klasör yoksa da yolu tutulur; 📁 açar).
                var photos = RecordContents.FindFolder(common, PhotosLabel);
                cover.PhotosHome = photos.Length > 0 ? photos : Path.Combine(common, Templates.TemplateSet.PhotosFolder);
                if (photos.Length > 0)
                {
                    cover.PhotosFolder = photos;
                    cover.Photos = Order(RecordContents.ImagesUnder(photos));
                }
            }

            if (cover.Photos.Count == 0 && common.Length > 0)
            {
                // Eski: 01_Ortak'ta yalnız doğrudan klasörde duran, adında "kapak" geçen resimler.
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
                // Eski stok: 07_Fotograflar.
                var legacy = RecordContents.FindFolder(stockFolder, PhotosLabel);
                if (legacy.Length > 0)
                {
                    cover.PhotosFolder = legacy;
                    cover.Photos = Order(RecordContents.ImagesUnder(legacy));
                }
            }

            if (cover.PhotosHome.Length == 0)
            {
                // Eski stok: okunan klasör; hiç yoksa yeni düzendeki yer.
                cover.PhotosHome = cover.PhotosFolder.Length > 0 ? cover.PhotosFolder : Path.Combine(stockFolder, "01_" + CommonLabel, Templates.TemplateSet.PhotosFolder);
            }

            cover.DocumentsFolder = common.Length > 0 ? common : RecordContents.FindFolder(stockFolder, DocumentsLabel);
            cover.DocumentCount = cover.DocumentsFolder.Length == 0 ? 0 : RecordContents.CountFiles(cover.DocumentsFolder);

            // Adında "kapak" geçen fotoğraf varsa kapak odur; yoksa sıraya göre ilki.
            var chosen = cover.Photos.FirstOrDefault(p => Path.GetFileNameWithoutExtension(p).IndexOf(CoverWord, System.StringComparison.OrdinalIgnoreCase) >= 0);
            if (chosen != null)
            {
                cover.Photos.Remove(chosen);
                cover.Photos.Insert(0, chosen);
            }

            return cover;
        }

        /// <summary>Görünüş sırası (dosya adının sonu): kapak, ön, arka, yan, üst, montaj, iç, etiket.</summary>
        public static readonly string[] ViewKeys = { "kapak", "on", "arka", "yan", "ust", "montaj", "ic", "etiket" };

        /// <summary>
        /// Fotoğrafın görünüşü: dosya adının sonundaki <c>_on</c>, <c>_arka</c> … (<c>_on_2</c> gibi sıra numarasıyla).
        /// Görünüş anahtarı (<see cref="ViewKeys"/>) ve numara döner; kurala uymuyorsa anahtar boş.
        /// </summary>
        public static string ViewOf(string path, out int number)
        {
            number = 0;
            var parts = Path.GetFileNameWithoutExtension(path ?? string.Empty).ToLowerInvariant().Split('_', '-', ' ');
            var last = parts.Length - 1;
            int n;
            if (last > 0 && int.TryParse(parts[last], out n))
            {
                number = n;
                last--;
            }

            var key = last >= 0 ? Plain(parts[last]) : string.Empty;
            if (System.Array.IndexOf(ViewKeys, key) >= 0)
            {
                return key;
            }

            number = 0;
            return string.Empty;
        }

        /// <summary>Fotoğrafları görünüş sırasına dizer: kurala uyanlar görünüş ve numaraya göre, diğerleri sonda ada göre.</summary>
        public static List<string> Order(IEnumerable<string> photos)
        {
            return photos
                .Select(p =>
                {
                    int number;
                    var key = ViewOf(p, out number);
                    var rank = key.Length > 0 ? System.Array.IndexOf(ViewKeys, key) : ViewKeys.Length;
                    return new { Path = p, Rank = rank, Number = number };
                })
                .OrderBy(x => x.Rank)
                .ThenBy(x => x.Number)
                .ThenBy(x => Path.GetFileName(x.Path), System.StringComparer.OrdinalIgnoreCase)
                .Select(x => x.Path)
                .ToList();
        }

        // Türkçe harfler sadeleşir ("ön" → "on", "üst" → "ust", "iç" → "ic").
        private static string Plain(string text)
        {
            return text.Replace('ö', 'o').Replace('ü', 'u').Replace('ç', 'c').Replace('ı', 'i').Replace('ş', 's').Replace('ğ', 'g');
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
