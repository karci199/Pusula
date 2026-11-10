using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Markdig;
using Markdig.Extensions.EmphasisExtras;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Pusula.Core.Markdown
{
    /// <summary>Obsidian kutusu (<c>&gt; [!uyari] Başlık</c>): tür, başlık, kapalı başlasın mı.</summary>
    public sealed class Callout
    {
        /// <summary>Tür (küçük harf: note, tip, warning, danger, question, success, info, quote …).</summary>
        public string Kind { get; set; } = string.Empty;

        /// <summary>Başlık (yoksa boş; görünümde tür adı yazılır).</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary><c>[!x]-</c> ile kapalı başlar.</summary>
        public bool IsFolded { get; set; }
    }

    /// <summary>
    /// md notları için yardımcılar: Obsidian uyumlu boru hattı, ayrıştırma, kutu (callout) tanıma, görev kutusunu çevirme,
    /// resim / dosya yolunu bulma ve notu kodlamasını koruyarak atomik kaydetme.
    /// </summary>
    public static class MarkdownNotes
    {
        private static readonly Regex CalloutHead = new Regex(@"^\[!([A-Za-zÇĞİÖŞÜçğıöşü_-]+)\]([+-]?)\s*(.*)$", RegexOptions.CultureInvariant);

        private static readonly Regex TaskMark = new Regex(@"^(\s*(?:[-*+]|\d+[.)])\s+\[)([ xX])(\])", RegexOptions.CultureInvariant);

        private static readonly string[] ImageExtensions = { ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp", ".tif", ".tiff" };

        private static MarkdownPipeline? pipeline;

        /// <summary>
        /// Boru hattı: YAML başlığı (gövdeden ayrılır), boru tabloları, görev listeleri, <c>~~çizili~~</c>, <c>==vurgu==</c>,
        /// otomatik bağlantılar, dipnotlar, liste ekleri, hassas kaynak konumu ve <c>[[…]]</c>.
        /// </summary>
        public static MarkdownPipeline Pipeline
        {
            get
            {
                return pipeline ?? (pipeline = new MarkdownPipelineBuilder()
                    .UseYamlFrontMatter()
                    .UsePipeTables()
                    .UseGridTables()
                    .UseTaskLists()
                    .UseEmphasisExtras(EmphasisExtraOptions.Strikethrough | EmphasisExtraOptions.Marked)
                    .UseAutoLinks()
                    .UseFootnotes()
                    .UseListExtras()
                    .UsePreciseSourceLocation()
                    .Use<WikiLinkExtension>()
                    .Build());
            }
        }

        /// <summary>Metni ayrıştırır (satır sonları korunur; konumlar metne göredir).</summary>
        public static MarkdownDocument Parse(string text)
        {
            return Markdig.Markdown.Parse(text ?? string.Empty, Pipeline);
        }

        /// <summary>
        /// Obsidian yorumlarını (<c>%% … %%</c>, birden çok satıra yayılabilir) boşlukla örter; satır sonları ve uzunluk
        /// korunur (konumlar ve satır numaraları değişmez). Kod bloklarındaki ve <c>`satır içi kod`</c>daki <c>%%</c> yorum
        /// sayılmaz. Kapanmamış <c>%%</c> notun sonuna kadar yorumdur (Obsidian gibi).
        /// </summary>
        public static string MaskComments(string text)
        {
            text = text ?? string.Empty;
            if (text.IndexOf("%%", StringComparison.Ordinal) < 0)
            {
                return text;
            }

            var chars = text.ToCharArray();
            var inComment = false;
            var inFence = false;
            var fence = string.Empty;
            var lineStart = 0;
            while (lineStart < chars.Length)
            {
                var lineEnd = text.IndexOf('\n', lineStart);
                if (lineEnd < 0)
                {
                    lineEnd = chars.Length;
                }

                var trimmed = text.Substring(lineStart, lineEnd - lineStart).TrimStart();
                if (!inComment && (trimmed.StartsWith("```", StringComparison.Ordinal) || trimmed.StartsWith("~~~", StringComparison.Ordinal)))
                {
                    var mark = trimmed.Substring(0, 3);
                    if (!inFence)
                    {
                        inFence = true;
                        fence = mark;
                    }
                    else if (mark == fence)
                    {
                        inFence = false;
                    }
                }
                else if (!inFence)
                {
                    var inCode = false;
                    for (var i = lineStart; i < lineEnd; i++)
                    {
                        if (!inComment && chars[i] == '`')
                        {
                            inCode = !inCode;
                            continue;
                        }

                        if (!inCode && i + 1 < lineEnd && chars[i] == '%' && chars[i + 1] == '%')
                        {
                            chars[i] = ' ';
                            chars[i + 1] = ' ';
                            inComment = !inComment;
                            i++;
                            continue;
                        }

                        if (inComment && chars[i] != '\r')
                        {
                            chars[i] = ' ';
                        }
                    }
                }

                lineStart = lineEnd + 1;
            }

            return new string(chars);
        }

        /// <summary>
        /// Alıntı bir Obsidian kutusu ise türünü ve başlığını verir (ilk satır <c>[!tür] Başlık</c>). <paramref name="source"/>
        /// ayrıştırılan metnin kendisidir; ilk satır oradan okunur ("[" bağlantı ayrıştırıcısında parçalandığı için).
        /// </summary>
        public static Callout? CalloutOf(QuoteBlock quote, string source)
        {
            var first = quote.FirstOrDefault() as ParagraphBlock;
            if (first == null || source == null || first.Span.Start < 0 || first.Span.Start >= source.Length)
            {
                return null;
            }

            var end = source.IndexOf('\n', first.Span.Start);
            var head = (end < 0 ? source.Substring(first.Span.Start) : source.Substring(first.Span.Start, end - first.Span.Start)).Trim();
            var match = CalloutHead.Match(head);
            if (!match.Success)
            {
                return null;
            }

            return new Callout
            {
                Kind = match.Groups[1].Value.ToLowerInvariant(),
                IsFolded = match.Groups[2].Value == "-",
                Title = match.Groups[3].Value.Trim(),
            };
        }

        /// <summary>
        /// Verilen satırdaki görev kutusunu çevirir (<c>- [ ]</c> ↔ <c>- [x]</c>); satırda kutu yoksa metin olduğu gibi döner.
        /// Satır sonu biçimi korunur.
        /// </summary>
        public static string ToggleTask(string text, int line)
        {
            var newline = text.Contains("\r\n") ? "\r\n" : "\n";
            var lines = text.Replace("\r\n", "\n").Split('\n');
            if (line < 0 || line >= lines.Length)
            {
                return text;
            }

            var match = TaskMark.Match(lines[line]);
            if (!match.Success)
            {
                return text;
            }

            var mark = match.Groups[2].Value == " " ? "x" : " ";
            lines[line] = match.Groups[1].Value + mark + match.Groups[3].Value + lines[line].Substring(match.Length);
            return string.Join(newline, lines);
        }

        /// <summary>Dosya adı resim mi (uzantıya göre).</summary>
        public static bool IsImage(string target)
        {
            var extension = Path.GetExtension(target ?? string.Empty).ToLowerInvariant();
            return ImageExtensions.Contains(extension);
        }

        /// <summary>
        /// Notta adı geçen dosyayı bulur (<c>![[foto.jpg]]</c>, <c>![](Foto/a%20b.png)</c>): önce notun klasörüne göre, sonra köke göre,
        /// sonra kökün altında aynı adlı ilk dosya (Obsidian gibi; gizli ve silinen klasörler hariç). <paramref name="nameIndex"/>
        /// verilirse ad araması ondan yapılır. Bulunamazsa boş; web adresi ise boş.
        /// </summary>
        public static string ResolveFile(string noteFolder, string root, string target, IDictionary<string, string>? nameIndex = null)
        {
            if (string.IsNullOrWhiteSpace(target) || target.Contains("://"))
            {
                return string.Empty;
            }

            var clean = Uri.UnescapeDataString(target.Trim().Trim('<', '>')).Replace('/', Path.DirectorySeparatorChar);
            try
            {
                foreach (var baseFolder in new[] { noteFolder, root })
                {
                    if (string.IsNullOrEmpty(baseFolder))
                    {
                        continue;
                    }

                    var candidate = Path.GetFullPath(Path.Combine(baseFolder, clean));
                    if (File.Exists(candidate))
                    {
                        return candidate;
                    }
                }
            }
            catch (ArgumentException)
            {
                return string.Empty;
            }
            catch (NotSupportedException)
            {
                return string.Empty;
            }
            catch (PathTooLongException)
            {
                return string.Empty;
            }

            var name = Path.GetFileName(clean);
            if (name.Length == 0)
            {
                return string.Empty;
            }

            string found;
            var index = nameIndex ?? BuildNameIndex(string.IsNullOrEmpty(root) ? noteFolder : root);
            return index.TryGetValue(name, out found) ? found : string.Empty;
        }

        /// <summary>Kökün altındaki dosyalar: ad → ilk yol (gizli, <c>_SILINENLER</c>, <c>_TAKIP</c>, <c>_PUSULA</c> hariç).</summary>
        public static Dictionary<string, string> BuildNameIndex(string root)
        {
            var index = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
            {
                return index;
            }

            var pending = new Stack<string>();
            pending.Push(root);
            while (pending.Count > 0)
            {
                var folder = pending.Pop();
                try
                {
                    foreach (var file in Directory.EnumerateFiles(folder))
                    {
                        var name = Path.GetFileName(file);
                        if (!index.ContainsKey(name))
                        {
                            index[name] = file;
                        }
                    }

                    foreach (var child in Directory.EnumerateDirectories(folder))
                    {
                        var name = Path.GetFileName(child);
                        if (!name.StartsWith(".", StringComparison.Ordinal) && name != "_SILINENLER" && name != "_TAKIP" && name != "_PUSULA")
                        {
                            pending.Push(child);
                        }
                    }
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }

            return index;
        }

        /// <summary>
        /// Notu kaydeder: geçici dosya + yeniden adlandırma; var olan notun kodlaması (BOM) korunur, yeni not BOM'suz.
        /// Hata metni; başarıda boş.
        /// </summary>
        public static string Save(string path, string text)
        {
            try
            {
                var bom = false;
                if (File.Exists(path))
                {
                    using (var stream = File.OpenRead(path))
                    {
                        bom = stream.ReadByte() == 0xEF && stream.ReadByte() == 0xBB && stream.ReadByte() == 0xBF;
                    }
                }

                var folder = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                var temp = path + ".tmp-" + Guid.NewGuid().ToString("N").Substring(0, 8);
                Scanning.DiskWatcher.MarkOwnWrite();
                File.WriteAllText(temp, text ?? string.Empty, new UTF8Encoding(bom));
                if (File.Exists(path))
                {
                    File.Replace(temp, path, null);
                }
                else
                {
                    File.Move(temp, path);
                }

                return string.Empty;
            }
            catch (IOException ex)
            {
                return ex.Message;
            }
            catch (UnauthorizedAccessException ex)
            {
                return ex.Message;
            }
        }

        /// <summary>Kelime sayısı (YAML başlığı ve md işaretleri hariç yaklaşık).</summary>
        public static int WordCount(string text)
        {
            var body = text ?? string.Empty;
            if (body.StartsWith("---", StringComparison.Ordinal))
            {
                var end = body.IndexOf("\n---", 3, StringComparison.Ordinal);
                if (end > 0)
                {
                    body = body.Substring(end + 4);
                }
            }

            return Regex.Matches(body, @"[\p{L}\p{N}][\p{L}\p{N}'’.-]*").Count;
        }
    }
}
