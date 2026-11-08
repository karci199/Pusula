using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Pusula.Core.Testing;

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
            var note = NotePath(folder, number);
            if (!File.Exists(note))
            {
                note = SafeFiles(folder, "*.md").OrderBy(f => f, StringComparer.OrdinalIgnoreCase).FirstOrDefault() ?? string.Empty;
            }

            if (note.Length > 0)
            {
                result.NoteFile = note;
                result.NoteExcerpt = Excerpt(note);
            }

            result.Photos.AddRange(SafeFiles(folder, "*").Where(IsImage).OrderBy(f => f, StringComparer.OrdinalIgnoreCase));
            foreach (var sub in SafeDirectories(folder).OrderBy(d => d, StringComparer.OrdinalIgnoreCase))
            {
                var name = System.IO.Path.GetFileName(sub);
                if (name.StartsWith("_", StringComparison.Ordinal))
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

        private static string Excerpt(string note)
        {
            string text;
            try
            {
                text = File.ReadAllText(note, Encoding.UTF8);
            }
            catch (IOException)
            {
                return string.Empty;
            }
            catch (UnauthorizedAccessException)
            {
                return string.Empty;
            }

            string body;
            FrontMatter.Parse(text, out body);
            var parts = new List<string>();
            var length = 0;
            foreach (var raw in body.Replace("\r\n", "\n").Split('\n'))
            {
                var line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                {
                    continue;
                }

                line = line.TrimStart('-', '*', ' ');
                parts.Add(line);
                length += line.Length + 1;
                if (length >= 240)
                {
                    break;
                }
            }

            var joined = string.Join(" ", parts);
            return joined.Length > 240 ? joined.Substring(0, 237) + "…" : joined;
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
