using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Pusula.Core.Scanning;

namespace Pusula.Core.Notes
{
    /// <summary>
    /// md notlarındaki işaretlenmemiş görev: <c>- [ ] SN02 VNA S11 #gorev</c>. İşaretli olanlar (<c>- [x]</c>) okunmaz.
    /// Uygulama yalnızca okur; işaretlemek Obsidian'da yapılır.
    /// </summary>
    public sealed class OpenTask
    {
        private static readonly Regex TaskLine = new Regex(@"^\s*[-*+]\s+\[ \]\s+(?<text>.+?)\s*$", RegexOptions.Compiled);

        private static readonly Regex Tag = new Regex(@"(^|\s)#[\p{L}\p{N}_/-]+", RegexOptions.Compiled);

        /// <summary>Görev metni (etiketler, <c>#gorev</c> gibi, atılmış).</summary>
        public string Text { get; private set; } = string.Empty;

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
        /// Klasördeki (alt klasörler dahil; <c>_</c> ile başlayan ve arşiv klasörleri hariç) md notlarındaki açık görevler,
        /// notların ve satırların sırasıyla.
        /// </summary>
        public static List<OpenTask> ReadAll(string folder)
        {
            var result = new List<OpenTask>();
            foreach (var file in MarkdownFiles(folder))
            {
                result.AddRange(Read(file));
            }

            return result;
        }

        /// <summary>Tek notun açık görevleri; okunamazsa boş.</summary>
        public static List<OpenTask> Read(string path)
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
            foreach (var line in lines)
            {
                if (line.TrimStart().StartsWith("```", StringComparison.Ordinal))
                {
                    inCode = !inCode;
                    continue;
                }

                var match = inCode ? Match.Empty : TaskLine.Match(line);
                if (!match.Success)
                {
                    continue;
                }

                var text = Tag.Replace(match.Groups["text"].Value, string.Empty).Trim();
                if (text.Length > 0)
                {
                    result.Add(new OpenTask { Text = text, NotePath = path });
                }
            }

            return result;
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
