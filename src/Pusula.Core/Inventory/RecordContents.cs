using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Pusula.Core.Inventory
{
    /// <summary>Kayıt klasöründeki bir alt klasörün özeti (örn. <c>01_Cizimler</c>: 3 dosya, son Rev AC).</summary>
    public sealed class RecordFolderSummary
    {
        /// <summary>Klasörün tam yolu.</summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>Görünen ad: numara öneki atılmış, alt çizgiler boşluk ("Cizimler").</summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>İçindeki dosya sayısı (alt klasörler dahil, <c>_</c> ile başlayanlar hariç).</summary>
        public int FileCount { get; set; }

        /// <summary>Dosya adlarındaki en yüksek revizyon ("AC"); yoksa boş.</summary>
        public string LatestRevision { get; set; } = string.Empty;

        /// <summary>İçindeki dosyalar (tür özeti için).</summary>
        public List<string> Files { get; set; } = new List<string>();
    }

    /// <summary>
    /// Kayıt klasöründe kendiliğinden tanınanlar: Obsidian notu, alt klasör özetleri, fotoğraflar.
    /// Yalnızca okur; diske yazmaz.
    /// </summary>
    public sealed class RecordContents
    {
        private static readonly string[] ImageExtensions = { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tif", ".tiff" };

        // "RevAC", "Rev_B", "REV-2"; harf revizyonu büyük harf olmalı ki "Review" sayılmasın.
        private static readonly Regex Revision = new Regex(@"(?:^|[_\-\s\.])(?i:rev)[_\-\s]?([A-Z]{1,3}|\d{1,3})(?=$|[_\-\s\.])", RegexOptions.CultureInvariant);

        /// <summary>Olay notlarının durduğu klasörün adı; kutucuk olarak gösterilmez.</summary>
        public const string NotesLabel = "Notlar";

        /// <summary>Notun tam yolu; yoksa boş.</summary>
        public string NoteFile { get; private set; } = string.Empty;

        /// <summary>Not gövdesinin başı (başlıklar ve boş satırlar atlanır, en çok ~240 karakter).</summary>
        public string NoteExcerpt { get; private set; } = string.Empty;

        /// <summary>Alt klasörler (ada göre sıralı).</summary>
        public List<RecordFolderSummary> Folders { get; } = new List<RecordFolderSummary>();

        /// <summary>Fotoğraflar (klasör içinde ada göre sıralı); ilki kapaktır.</summary>
        public List<string> Photos { get; } = new List<string>();

        /// <summary>Kapak fotoğrafı; yoksa boş.</summary>
        public string Cover
        {
            get { return Photos.Count > 0 ? Photos[0] : string.Empty; }
        }

        /// <summary>Kaydın not dosyasının olması gereken yol: <c>&lt;klasör&gt;\&lt;numara&gt;.md</c>.</summary>
        public static string NotePath(string folder, string number)
        {
            return System.IO.Path.Combine(folder, number + ".md");
        }

        /// <summary>Kayıt klasörünü okur. Erişilemeyen klasörler atlanır.</summary>
        public static RecordContents Read(string folder, string number)
        {
            var result = new RecordContents();
            if (!Directory.Exists(folder))
            {
                return result;
            }

            // Not: önce <numara>.md, yoksa kökteki ilk .md.
            var note = Notes.NoteFile.Find(folder, number);
            if (note.Length > 0)
            {
                result.NoteFile = note;
                result.NoteExcerpt = Notes.NoteFile.Excerpt(note);
            }

            result.Photos.AddRange(SafeFiles(folder, "*").Where(IsImage).OrderBy(f => f, StringComparer.OrdinalIgnoreCase));
            foreach (var sub in SafeDirectories(folder).OrderBy(d => d, StringComparer.OrdinalIgnoreCase))
            {
                var name = System.IO.Path.GetFileName(sub);
                // Notlar klasörü kutucuk olmaz: içindeki olay notları Geçmiş'te görünür.
                if (name.StartsWith("_", StringComparison.Ordinal) || string.Equals(LabelOf(name), NotesLabel, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var files = AllFiles(sub).ToList();
                result.Folders.Add(new RecordFolderSummary
                {
                    Path = sub,
                    Label = LabelOf(name),
                    FileCount = files.Count,
                    LatestRevision = LatestRevision(files),
                    Files = files,
                });
                result.Photos.AddRange(files.Where(IsImage).OrderBy(f => f, StringComparer.OrdinalIgnoreCase));
            }

            return result;
        }

        /// <summary>"01_Cizimler" → "Cizimler"; "Teknik_Resim" → "Teknik Resim".</summary>
        public static string LabelOf(string folderName)
        {
            var name = folderName;
            var underscore = name.IndexOf('_');
            if (underscore > 0 && name.Substring(0, underscore).All(char.IsDigit))
            {
                name = name.Substring(underscore + 1);
            }

            name = name.Replace('_', ' ').Trim();
            return name.Length == 0 ? folderName : name;
        }

        /// <summary>Dosya adlarındaki en yüksek revizyon: harfler önce uzunluk sonra alfabe (AB &lt; AC &lt; BA), sayılar değerce.</summary>
        public static string LatestRevision(IEnumerable<string> files)
        {
            var best = string.Empty;
            foreach (var file in files)
            {
                var match = Revision.Match(System.IO.Path.GetFileNameWithoutExtension(file));
                if (!match.Success)
                {
                    continue;
                }

                var rev = match.Groups[1].Value.ToUpperInvariant();
                if (best.Length == 0 || CompareRevision(rev, best) > 0)
                {
                    best = rev;
                }
            }

            return best;
        }

        private static int CompareRevision(string a, string b)
        {
            int na;
            int nb;
            if (int.TryParse(a, out na) && int.TryParse(b, out nb))
            {
                return na.CompareTo(nb);
            }

            return a.Length != b.Length ? a.Length.CompareTo(b.Length) : string.CompareOrdinal(a, b);
        }

        /// <summary>Klasördeki ve alt klasörlerindeki fotoğraflar (<c>_</c> ile başlayanlar hariç), ada göre sıralı.</summary>
        public static List<string> ImagesUnder(string folder)
        {
            return Directory.Exists(folder)
                ? AllFiles(folder).Where(IsImage).OrderBy(f => f, StringComparer.OrdinalIgnoreCase).ToList()
                : new List<string>();
        }

        /// <summary>Klasördeki ve alt klasörlerindeki dosya sayısı (<c>_</c> ile başlayanlar hariç).</summary>
        public static int CountFiles(string folder)
        {
            return Directory.Exists(folder) ? AllFiles(folder).Count() : 0;
        }

        /// <summary>Kökte adı (numara öneki atılınca) verilen ada eşit ilk alt klasör ("07_Fotograflar" → "Fotograflar"); yoksa boş.</summary>
        public static string FindFolder(string root, string label)
        {
            return SafeDirectories(root)
                .OrderBy(d => d, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(d => string.Equals(LabelOf(System.IO.Path.GetFileName(d)), label, StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
        }

        private static bool IsImage(string file)
        {
            var extension = System.IO.Path.GetExtension(file);
            return ImageExtensions.Any(e => string.Equals(e, extension, StringComparison.OrdinalIgnoreCase));
        }

        private static IEnumerable<string> AllFiles(string folder)
        {
            foreach (var file in SafeFiles(folder, "*"))
            {
                yield return file;
            }

            foreach (var sub in SafeDirectories(folder))
            {
                if (System.IO.Path.GetFileName(sub).StartsWith("_", StringComparison.Ordinal))
                {
                    continue;
                }

                foreach (var file in AllFiles(sub))
                {
                    yield return file;
                }
            }
        }

        private static string[] SafeFiles(string folder, string pattern)
        {
            try
            {
                return Directory.GetFiles(folder, pattern);
            }
            catch (IOException)
            {
                return new string[0];
            }
            catch (UnauthorizedAccessException)
            {
                return new string[0];
            }
        }

        private static string[] SafeDirectories(string folder)
        {
            try
            {
                return Directory.GetDirectories(folder);
            }
            catch (IOException)
            {
                return new string[0];
            }
            catch (UnauthorizedAccessException)
            {
                return new string[0];
            }
        }
    }
}
