using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Pusula.Core.Testing;

using Pusula.Core.Scanning;

namespace Pusula.Core.Notes
{
    /// <summary>
    /// Obsidian notundan okunan olay: YAML'ında <c>olay:</c> ve <c>tarih:</c> olan her <c>.md</c> bir olaydır
    /// (toplantı, karar, numune, revizyon …). Uygulama yalnızca okur.
    /// </summary>
    public sealed class EventNote
    {
        /// <summary>Olay çeşidini veren YAML alanı.</summary>
        public const string KindKey = "olay";

        /// <summary>Test koşusu çeşidi (koşular kendi okuyucusundan gelir; burada atlanır).</summary>
        public const string TestKind = "test";

        /// <summary>Notun tam yolu.</summary>
        public string Path { get; private set; } = string.Empty;

        /// <summary>Çeşit, küçük harfle (<c>toplanti</c>, <c>karar</c> …).</summary>
        public string Kind { get; private set; } = string.Empty;

        /// <summary>Tarih (saat yoksa gün başı).</summary>
        public DateTime Date { get; private set; }

        /// <summary>Kişi (<c>kisi:</c>); boş olabilir.</summary>
        public string Person { get; private set; } = string.Empty;

        /// <summary>Başlık: gövdedeki ilk <c># başlık</c>, yoksa dosya adı.</summary>
        public string Title { get; private set; } = string.Empty;

        /// <summary>Gövdenin başı (başlık hariç).</summary>
        public string Excerpt { get; private set; } = string.Empty;

        /// <summary>
        /// Klasördeki (alt klasörler dahil, <c>_</c> ile başlayanlar hariç) olay notlarını okur, yeniden eskiye sıralar.
        /// <c>olay: test</c> notları atlanır (test koşuları ayrıca okunur).
        /// </summary>
        public static List<EventNote> ReadAll(string folder)
        {
            var result = new List<EventNote>();
            foreach (var file in MarkdownFiles(folder))
            {
                var note = TryRead(file);
                if (note != null && note.Kind != TestKind)
                {
                    result.Add(note);
                }
            }

            return result.OrderByDescending(n => n.Date).ToList();
        }

        /// <summary>Tek notu okur; olay değilse (YAML'da <c>olay</c> ya da geçerli <c>tarih</c> yoksa) <c>null</c>.</summary>
        public static EventNote? TryRead(string path)
        {
            // Önce yalnızca YAML başlığına bakılır; olay değilse dosyanın geri kalanı okunmaz (KIWI raporları vb. büyük olabilir).
            if (!HeaderHasKind(path))
            {
                return null;
            }

            string text;
            try
            {
                text = Fs.ReadAllText(path);
            }
            catch (IOException)
            {
                return null;
            }
            catch (UnauthorizedAccessException)
            {
                return null;
            }

            string body;
            var fields = FrontMatter.Parse(text, out body);
            string kind;
            string dateText;
            DateTime date;
            if (!fields.TryGetValue(KindKey, out kind) || kind.Trim().Length == 0
                || !fields.TryGetValue("tarih", out dateText)
                || !DateTime.TryParseExact(dateText.Trim(), new[] { "yyyy-MM-dd", "yyyy-MM-dd HH:mm", "dd.MM.yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            {
                return null;
            }

            string person;
            var title = string.Empty;
            foreach (var raw in body.Replace("\r\n", "\n").Split('\n'))
            {
                var line = raw.Trim();
                if (line.StartsWith("# ", StringComparison.Ordinal))
                {
                    title = line.Substring(2).Trim();
                    break;
                }
            }

            var excerpt = NoteFile.Excerpt(path);
            return new EventNote
            {
                Path = path,
                Kind = kind.Trim().ToLowerInvariant(),
                Date = date,
                Person = fields.TryGetValue("kisi", out person) ? person.Trim() : string.Empty,
                Title = title.Length > 0 ? title : System.IO.Path.GetFileNameWithoutExtension(path).Replace('_', ' '),
                Excerpt = excerpt,
            };
        }

        private static bool HeaderHasKind(string path)
        {
            try
            {
                // Tarama etkinse metin bellekten gelir (açık işler de aynı notu okur); değilse yalnız baş satırlar okunur.
                using (var reader = FolderSnapshot.Active != null && FolderSnapshot.Active.Covers(path)
                    ? (TextReader)new StringReader(Fs.ReadAllText(path))
                    : new StreamReader(path, Encoding.UTF8))
                {
                    var first = reader.ReadLine();
                    if (first == null || first.Trim().TrimStart('\uFEFF') != "---")
                    {
                        return false;
                    }

                    for (var i = 0; i < 60; i++)
                    {
                        var line = reader.ReadLine();
                        if (line == null || line.Trim() == "---")
                        {
                            return false;
                        }

                        if (line.TrimStart().StartsWith(KindKey + ":", StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }

            return false;
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

            foreach (var file in files)
            {
                yield return file;
            }

            foreach (var sub in folders)
            {
                if (System.IO.Path.GetFileName(sub).StartsWith("_", StringComparison.Ordinal))
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
