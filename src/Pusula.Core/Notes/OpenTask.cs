using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Pusula.Core.Scanning;

namespace Pusula.Core.Notes
{
    /// <summary>
    /// md notlarındaki görev: <c>- [ ] SN02 VNA S11 @Ayşe_Demir #gorev 📅 2026-11-14</c>. Obsidian Tasks biçimi okunur:
    /// <c>📅</c> son tarih, <c>✅</c> bitiş tarihi; <c>@Ad_Soyad</c> kişi. Ekleme ve kapatma <see cref="TaskList"/> ile yapılır.
    /// </summary>
    public sealed class OpenTask
    {
        /// <summary>Görev satırı: madde işareti, kutu (<c>[ ]</c> / <c>[x]</c>), metin.</summary>
        internal static readonly Regex TaskLine = new Regex(@"^\s*[-*+]\s+\[(?<mark>[ xX])\]\s+(?<text>.+?)\s*$", RegexOptions.Compiled);

        private static readonly Regex Tag = new Regex(@"(^|\s)#[\p{L}\p{N}_/-]+", RegexOptions.Compiled);

        private static readonly Regex Person = new Regex(@"(^|\s)@(?<name>[\p{L}\p{N}_.-]+)", RegexOptions.Compiled);

        private static readonly Regex DueDate = new Regex("\uD83D\uDCC5\\s*(?<date>\\d{4}-\\d{2}-\\d{2})", RegexOptions.Compiled);

        private static readonly Regex DoneDateMark = new Regex("\u2705\\s*(?<date>\\d{4}-\\d{2}-\\d{2})", RegexOptions.Compiled);

        /// <summary>Görev metni (etiket, kişi ve tarihler atılmış).</summary>
        public string Text { get; private set; } = string.Empty;

        /// <summary>Satırın kendisi (kapatırken notun değişmediği bununla denetlenir).</summary>
        public string RawLine { get; private set; } = string.Empty;

        /// <summary>Notta satır numarası (0'dan).</summary>
        public int Line { get; private set; }

        /// <summary>Bitti mi (<c>[x]</c>).</summary>
        public bool IsDone { get; private set; }

        /// <summary>Kişi (<c>@Ad_Soyad</c> → "Ad Soyad"); yoksa boş.</summary>
        public string PersonName { get; private set; } = string.Empty;

        /// <summary>Son tarih (<c>📅 yyyy-MM-dd</c>); yoksa <c>null</c>.</summary>
        public DateTime? Due { get; private set; }

        /// <summary>Bitiş tarihi (<c>✅ yyyy-MM-dd</c>); yoksa <c>null</c>.</summary>
        public DateTime? DoneDate { get; private set; }

        /// <summary>Görevin bulunduğu notun tam yolu.</summary>
        public string NotePath { get; private set; } = string.Empty;

        /// <summary>
        /// Notun adı (uzantısız). Test koşusu notlarında (<c>test.md</c>, <c>_kosu.md</c>) klasörün adı verilir
        /// (<c>SNP1_T1</c>), çünkü bütün koşuların notu aynı adı taşır.
        /// </summary>
        public string NoteName
        {
            get
            {
                var name = Path.GetFileNameWithoutExtension(NotePath);
                if (string.Equals(name, "test", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "_kosu", StringComparison.OrdinalIgnoreCase))
                {
                    return Path.GetFileName(Path.GetDirectoryName(NotePath) ?? string.Empty);
                }

                return name;
            }
        }

        /// <summary>
        /// Klasördeki (alt klasörler dahil; <c>_</c> ile başlayan ve arşiv klasörleri hariç) md notlarındaki görevler,
        /// notların ve satırların sırasıyla. <paramref name="includeDone"/> ise bitenler de.
        /// </summary>
        public static List<OpenTask> ReadAll(string folder, bool includeDone = false)
        {
            var result = new List<OpenTask>();
            foreach (var file in MarkdownFiles(folder))
            {
                result.AddRange(Read(file, includeDone));
            }

            return result;
        }

        /// <summary>Tek notun görevleri (varsayılan: yalnız açıklar); okunamazsa boş.</summary>
        public static List<OpenTask> Read(string path, bool includeDone = false)
        {
            var result = new List<OpenTask>();
            string[] lines;
            try
            {
                lines = Fs.ReadAllLines(path);
            }
            catch (IOException)
            {
                return result;
            }
            catch (UnauthorizedAccessException)
            {
                return result;
            }

            var inCode = false;
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (line.TrimStart().StartsWith("```", StringComparison.Ordinal))
                {
                    inCode = !inCode;
                    continue;
                }

                var task = inCode ? null : Parse(line);
                if (task == null || (task.IsDone && !includeDone))
                {
                    continue;
                }

                task.Line = i;
                task.NotePath = path;
                result.Add(task);
            }

            return result;
        }

        /// <summary>Satırı görev olarak okur; görev değilse (ya da metni boşsa) <c>null</c>.</summary>
        public static OpenTask? Parse(string line)
        {
            var match = TaskLine.Match(line ?? string.Empty);
            if (!match.Success)
            {
                return null;
            }

            var body = match.Groups["text"].Value;
            var task = new OpenTask { RawLine = line ?? string.Empty, IsDone = match.Groups["mark"].Value != " " };
            var due = DueDate.Match(body);
            if (due.Success)
            {
                task.Due = ParseDate(due.Groups["date"].Value);
            }

            var done = DoneDateMark.Match(body);
            if (done.Success)
            {
                task.DoneDate = ParseDate(done.Groups["date"].Value);
            }

            var person = Person.Match(body);
            if (person.Success)
            {
                task.PersonName = person.Groups["name"].Value.Replace('_', ' ');
            }

            var text = DoneDateMark.Replace(DueDate.Replace(body, string.Empty), string.Empty);
            text = Person.Replace(Tag.Replace(text, " "), " ");
            task.Text = string.Join(" ", text.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries));
            return task.Text.Length == 0 ? null : task;
        }

        private static DateTime? ParseDate(string text)
        {
            DateTime value;
            return DateTime.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out value) ? value : (DateTime?)null;
        }

        private static IEnumerable<string> MarkdownFiles(string folder)
        {
            if (!Fs.DirectoryExists(folder))
            {
                yield break;
            }

            string[] files;
            string[] folders;
            try
            {
                files = Fs.GetFiles(folder, "*.md");
                folders = Fs.GetDirectories(folder);
            }
            catch (IOException)
            {
                yield break;
            }
            catch (UnauthorizedAccessException)
            {
                yield break;
            }

            Array.Sort(files, StringComparer.OrdinalIgnoreCase);
            foreach (var file in files)
            {
                yield return file;
            }

            Array.Sort(folders, StringComparer.OrdinalIgnoreCase);
            foreach (var sub in folders)
            {
                if (FileRules.IsSkippedFolder(sub))
                {
                    continue;
                }

                foreach (var file in MarkdownFiles(sub))
                {
                    yield return file;
                }
            }
        }
    }
}