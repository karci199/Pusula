using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Pusula.Core.Scanning;

namespace Pusula.Core.Notes
{
    /// <summary>Günlüğe eklenecek olay.</summary>
    public sealed class JournalEntry
    {
        /// <summary>Olayın günü.</summary>
        public DateTime Date { get; set; } = DateTime.Today;

        /// <summary>Tür adı ("Karar", "Teslim" …); boşsa yazılmaz.</summary>
        public string Kind { get; set; } = string.Empty;

        /// <summary>Başlık (zorunlu).</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>Açıklama (birkaç satır olabilir).</summary>
        public string Body { get; set; } = string.Empty;

        /// <summary>Önem: <c>Yüksek</c>, <c>Normal</c>, <c>Düşük</c> (Normal yazılmaz).</summary>
        public string Importance { get; set; } = string.Empty;

        /// <summary>Yazan kişi.</summary>
        public string Person { get; set; } = string.Empty;

        /// <summary>İlgili kayıt numaraları (jig, kablo, stok …).</summary>
        public List<string> Related { get; set; } = new List<string>();

        /// <summary>Etiketler (# işaretsiz).</summary>
        public List<string> Tags { get; set; } = new List<string>();

        /// <summary>Bağlı doğrulama maddesi (TG no, <c>EK-1</c> ya da <c>#5</c>); boşsa yazılmaz.</summary>
        public string Item { get; set; } = string.Empty;

        /// <summary>Ölçüm sonucu: <c>Geçti</c>, <c>Kaldı</c> ya da <c>Bilgi</c>; boşsa yazılmaz.</summary>
        public string Result { get; set; } = string.Empty;

        /// <summary>
        /// <c>#### Ayrıntı</c> başlığının altındaki metin (MD'de yazılır; düzenlemede korunur). Başlık her kayıtta yazılır,
        /// ayrıntı boş olsa da.
        /// </summary>
        public string Details { get; set; } = string.Empty;

        /// <summary>Ölçüm dosyalarının klasörü, kayıt klasörüne göre <c>/</c> ile (<c>03_Testler/Olcumler/…</c>); boşsa yazılmaz.</summary>
        public string Folder { get; set; } = string.Empty;

        /// <summary>Toplantı katılımcıları (@ işaretsiz); boşsa yazılmaz.</summary>
        public List<string> Participants { get; set; } = new List<string>();

        /// <summary>Toplantı ortamı (Yüz yüze, Teams, Skype, Zoom, Telefon); boşsa yazılmaz.</summary>
        public string Setting { get; set; } = string.Empty;

        /// <summary>Toplantıda alınan kararlar (serbest metin); notta <c>#### Kararlar</c> başlığının altına yazılır.</summary>
        public string Decisions { get; set; } = string.Empty;
    }

    /// <summary>
    /// Ana notun en altındaki <c>## Günlük</c> bölümü. Her olay bir <c>### gün · tür · başlık</c> bloğudur; altında
    /// isteğe bağlı bir bilgi satırı (<c>önem: Yüksek · kişi: katan · ilgili: [[JIG-1000-0009]] · #SATIMO</c>) ve açıklama.
    /// En yeni olay en üstte. Uygulama yalnızca başlığın hemen altına ekler; notun geri kalanına dokunmaz.
    /// </summary>
    public static class Journal
    {
        /// <summary>Bölüm başlığı.</summary>
        public const string Heading = "## Günlük";

        /// <summary>Normal önem (yazılmaz).</summary>
        public const string NormalImportance = "Normal";

        /// <summary>Kaydın içinde ayrıntı başlığı: bundan sonrası kartta görünmez (MD'de okunur).</summary>
        public const string DetailsHeading = "#### Ayrıntı";

        private static readonly Regex SectionLine = new Regex(@"^[ \t]*####[ \t]+(Ayr[ıi]nt[ıi]|Kararlar)[ \t]*$", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);

        private static readonly Regex DecisionsLine = new Regex(@"^[ \t]*####[ \t]+Kararlar[ \t]*$", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);

        private static readonly Regex AnyHeading = new Regex(@"^[ \t]*####[ \t]", RegexOptions.Compiled | RegexOptions.Multiline);

        private static readonly Regex DetailsLine = new Regex(@"^[ \t]*####[ \t]+Ayr[ıi]nt[ıi][ \t]*$", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);

        /// <summary>Açıklamanın kartta görünen kısmı: <c>#### Ayrıntı</c> başlığından öncesi (başlık yoksa tamamı).</summary>
        public static string SummaryOf(string body)
        {
            var text = (body ?? string.Empty).Replace("\r\n", "\n");
            var match = SectionLine.Match(text);
            return (match.Success ? text.Substring(0, match.Index) : text).Trim();
        }

        /// <summary>Açıklamanın <c>#### Kararlar</c> başlığının altındaki metin (sonraki <c>####</c> başlığına kadar); yoksa boş.</summary>
        public static string DecisionsOf(string body)
        {
            var text = (body ?? string.Empty).Replace("\r\n", "\n");
            var match = DecisionsLine.Match(text);
            if (!match.Success)
            {
                return string.Empty;
            }

            var rest = text.Substring(match.Index + match.Length);
            var next = AnyHeading.Match(rest);
            return (next.Success ? rest.Substring(0, next.Index) : rest).Trim('\n', ' ', '\t');
        }

        /// <summary>Açıklamanın <c>#### Ayrıntı</c> başlığından sonraki kısmı (başlık satırı hariç); başlık yoksa boş.</summary>
        public static string DetailsOf(string body)
        {
            var text = (body ?? string.Empty).Replace("\r\n", "\n");
            var match = DetailsLine.Match(text);
            return match.Success ? text.Substring(match.Index + match.Length).Trim('\n', ' ', '\t') : string.Empty;
        }

        /// <summary>Bilgi satırında bağlı maddenin öneki (<c>madde: TG-1</c>).</summary>
        public const string ItemKey = "madde:";

        /// <summary>Bilgi satırında ölçüm sonucunun öneki (<c>sonuç: Kaldı</c>).</summary>
        public const string ResultKey = "sonuç:";

        /// <summary>Bilgi satırında ölçüm klasörünün öneki (<c>klasör: 03_Testler/Olcumler/…</c>).</summary>
        public const string FolderKey = "klasör:";

        /// <summary>Bilgi satırında toplantı katılımcılarının öneki (<c>katılımcı: @katan @Ali</c>).</summary>
        public const string ParticipantsKey = "katılımcı:";

        /// <summary>Bilgi satırında toplantı ortamının öneki (<c>ortam: Teams</c>).</summary>
        public const string SettingKey = "ortam:";

        /// <summary>Kaydın içinde kararlar başlığı.</summary>
        public const string DecisionsHeading = "#### Kararlar";

        /// <summary>Okunan kayıttan yeniden yazılabilir girdi kurar (tüm alanlar, ayrıntı dahil).</summary>
        public static JournalEntry EntryOf(EventNote note)
        {
            if (note == null)
            {
                throw new ArgumentNullException(nameof(note));
            }

            var entry = new JournalEntry
            {
                Date = note.Date,
                Kind = note.KindLabel,
                Title = note.Title,
                Body = note.Body,
                Importance = note.Importance,
                Person = note.Person,
                Item = note.Item,
                Result = note.Result,
                Folder = note.Folder,
                Setting = note.Setting,
                Decisions = note.Decisions,
            };
            entry.Participants.AddRange(note.Participants);
            entry.Related.AddRange(note.Related);
            entry.Tags.AddRange(note.Tags);
            return entry;
        }

        private static readonly Regex Link = new Regex(@"\[\[([^\]\|]+)(\|[^\]]*)?\]\]", RegexOptions.Compiled);

        private static readonly Regex HashTag = new Regex(@"(?:^|\s)#([\p{L}\p{N}_/\-]+)", RegexOptions.Compiled);

        private static readonly string[] DateFormats = { "dd.MM.yyyy", "d.M.yyyy", "yyyy-MM-dd", "dd.MM.yyyy HH:mm", "yyyy-MM-dd HH:mm" };

        /// <summary>Notun Günlük bölümündeki olaylar (yazıldıkları sırayla). Not yoksa ya da bölüm yoksa boş.</summary>
        public static List<EventNote> Read(string notePath)
        {
            var result = new List<EventNote>();
            if (string.IsNullOrEmpty(notePath) || !Fs.FileExists(notePath))
            {
                return result;
            }

            string text;
            try
            {
                text = Fs.ReadAllText(notePath);
            }
            catch (IOException)
            {
                return result;
            }
            catch (UnauthorizedAccessException)
            {
                return result;
            }

            var lines = text.Replace("\r\n", "\n").Split('\n');
            var start = Array.FindIndex(lines, l => IsHeading(l));
            if (start < 0)
            {
                return result;
            }

            for (var i = start + 1; i < lines.Length; i++)
            {
                var line = lines[i];
                if (line.StartsWith("## ", StringComparison.Ordinal))
                {
                    break;
                }

                if (!line.StartsWith("### ", StringComparison.Ordinal))
                {
                    continue;
                }

                var body = new List<string>();
                var j = i + 1;
                for (; j < lines.Length && !lines[j].StartsWith("### ", StringComparison.Ordinal) && !lines[j].StartsWith("## ", StringComparison.Ordinal); j++)
                {
                    body.Add(lines[j]);
                }

                var entry = Parse(notePath, line.Substring(4), body, line);
                if (entry != null)
                {
                    result.Add(entry);
                }

                i = j - 1;
            }

            return result;
        }

        /// <summary>
        /// Olayı Günlük başlığının hemen altına ekler (en yeni en üstte). Bölüm yoksa notun sonuna <c>---</c> ve başlık
        /// eklenir; not yoksa yalnız Günlük'ten oluşan not açılır. Yazım atomiktir. Hata metni; başarıda boş.
        /// </summary>
        public static string Add(string notePath, JournalEntry entry)
        {
            if (entry.Title.Trim().Length == 0)
            {
                return "title";
            }

            string text;
            try
            {
                text = File.Exists(notePath) ? File.ReadAllText(notePath, Encoding.UTF8) : string.Empty;
            }
            catch (IOException ex)
            {
                return ex.Message;
            }
            catch (UnauthorizedAccessException ex)
            {
                return ex.Message;
            }

            var newline = text.Contains("\r\n") || text.Length == 0 ? "\r\n" : "\n";
            var block = Format(entry).Replace("\n", newline);
            var lines = text.Replace("\r\n", "\n").Split('\n').ToList();
            var headingIndex = lines.FindIndex(IsHeading);
            string updated;
            if (headingIndex < 0)
            {
                var trimmed = text.TrimEnd('\r', '\n', ' ');
                updated = (trimmed.Length > 0 ? trimmed + newline + newline + "---" + newline + newline : string.Empty)
                    + Heading + newline + newline + block;
            }
            else
            {
                // Başlığın hemen altındaki boş satırlardan sonra; böylece en yeni en üstte.
                var insertAt = headingIndex + 1;
                while (insertAt < lines.Count && lines[insertAt].Trim().Length == 0)
                {
                    insertAt++;
                }

                var before = string.Join(newline, lines.Take(insertAt).Concat(insertAt == headingIndex + 1 ? new[] { string.Empty } : new string[0]));
                var after = string.Join(newline, lines.Skip(insertAt));
                updated = before + newline + block.TrimEnd('\r', '\n') + newline + (after.Length > 0 ? newline + after : string.Empty);
            }

            if (!updated.EndsWith(newline, StringComparison.Ordinal))
            {
                updated += newline;
            }

            try
            {
                var directory = Path.GetDirectoryName(notePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var temp = notePath + ".tmp-" + Guid.NewGuid().ToString("N").Substring(0, 8);
                Pusula.Core.Scanning.DiskWatcher.MarkOwnWrite();
                File.WriteAllText(temp, updated, new UTF8Encoding(HasBom(notePath)));
                if (File.Exists(notePath))
                {
                    File.Replace(temp, notePath, null);
                }
                else
                {
                    File.Move(temp, notePath);
                }
            }
            catch (IOException ex)
            {
                return ex.Message;
            }
            catch (UnauthorizedAccessException ex)
            {
                return ex.Message;
            }

            return string.Empty;
        }

        /// <summary>
        /// Notta başlık satırı <paramref name="sourceLine"/> olan girdiyi yeni haliyle değiştirir (<paramref name="entry"/> boşsa siler).
        /// Notun geri kalanına dokunulmaz. Hata metni; başarıda boş.
        /// </summary>
        public static string Replace(string notePath, string sourceLine, JournalEntry? entry)
        {
            string text;
            try
            {
                text = File.ReadAllText(notePath, Encoding.UTF8);
            }
            catch (IOException ex)
            {
                return ex.Message;
            }
            catch (UnauthorizedAccessException ex)
            {
                return ex.Message;
            }

            var newline = text.Contains("\r\n") ? "\r\n" : "\n";
            var lines = text.Replace("\r\n", "\n").Split('\n').ToList();
            var heading = lines.FindIndex(IsHeading);
            var start = heading < 0 ? -1 : lines.FindIndex(heading + 1, l => l == sourceLine.TrimEnd('\r'));
            if (start < 0)
            {
                return "not found";
            }

            var end = start + 1;
            while (end < lines.Count && !lines[end].StartsWith("### ", StringComparison.Ordinal) && !lines[end].StartsWith("## ", StringComparison.Ordinal))
            {
                end++;
            }

            lines.RemoveRange(start, end - start);
            if (entry != null)
            {
                lines.InsertRange(start, Format(entry).TrimEnd('\n').Split('\n').Concat(new[] { string.Empty }));
            }

            return WriteAtomic(notePath, string.Join(newline, lines));
        }

        /// <summary>
        /// Notun YAML başlığındaki alanları yazar: verilen anahtar varsa satırı değiştirilir, yoksa başlığın sonuna eklenir;
        /// değer boşsa satır kaldırılır. YAML yoksa notun başına açılır. Gövdeye dokunulmaz. Not yoksa bir şey yapmaz.
        /// Hata metni; başarıda (ya da not yoksa) boş.
        /// </summary>
        public static string SetFrontMatter(string notePath, IDictionary<string, string> values)
        {
            return SetFrontMatter(notePath, values, null);
        }

        /// <summary>
        /// <see cref="SetFrontMatter(string, IDictionary{string, string})"/> gibi; ayrıca liste alanları (Obsidian <c>tags</c>)
        /// tek satırda <c>tags: [a, b]</c> olarak yazılır, boş liste satırı kaldırır. Değiştirilen ya da kaldırılan
        /// anahtarın altındaki çok satırlı değer (Obsidian'ın yazdığı <c>  - a</c> satırları) da temizlenir.
        /// </summary>
        public static string SetFrontMatter(string notePath, IDictionary<string, string> values, IDictionary<string, IList<string>>? lists)
        {
            if (string.IsNullOrEmpty(notePath) || !File.Exists(notePath) || values.Count + (lists?.Count ?? 0) == 0)
            {
                return string.Empty;
            }

            string text;
            try
            {
                text = File.ReadAllText(notePath, Encoding.UTF8);
            }
            catch (IOException ex)
            {
                return ex.Message;
            }
            catch (UnauthorizedAccessException ex)
            {
                return ex.Message;
            }

            var newline = text.Contains("\r\n") ? "\r\n" : "\n";
            var lines = text.Replace("\r\n", "\n").Split('\n').ToList();
            int close;
            if (lines.Count > 0 && lines[0].Trim().TrimStart('﻿') == "---" && (close = lines.FindIndex(1, l => l.Trim() == "---")) > 0)
            {
                // var olan başlık
            }
            else
            {
                lines.InsertRange(0, new[] { "---", "---" });
                close = 1;
            }

            var entries = values.Select(p => new KeyValuePair<string, string>(p.Key, Pusula.Core.Testing.FrontMatter.Quote(p.Value ?? string.Empty))).ToList();
            foreach (var list in lists ?? new Dictionary<string, IList<string>>())
            {
                var items = (list.Value ?? new List<string>()).Select(i => (i ?? string.Empty).Trim()).Where(i => i.Length > 0).Select(Pusula.Core.Testing.FrontMatter.Quote).ToList();
                entries.Add(new KeyValuePair<string, string>(list.Key, items.Count == 0 ? string.Empty : "[" + string.Join(", ", items) + "]"));
            }

            foreach (var pair in entries)
            {
                var key = pair.Key.Trim();
                if (key.Length == 0)
                {
                    continue;
                }

                var index = lines.FindIndex(1, close - 1, l => l.TrimStart().StartsWith(key + ":", StringComparison.OrdinalIgnoreCase));
                var value = pair.Value;

                // Anahtarın altındaki çok satırlı değer (girintili ya da "- " ile başlayan satırlar) da onun parçasıdır.
                if (index > 0)
                {
                    while (index + 1 < close && (lines[index + 1].StartsWith(" ", StringComparison.Ordinal) || lines[index + 1].StartsWith("\t", StringComparison.Ordinal) || lines[index + 1].StartsWith("- ", StringComparison.Ordinal)))
                    {
                        lines.RemoveAt(index + 1);
                        close--;
                    }
                }

                if (value.Length == 0)
                {
                    if (index > 0)
                    {
                        lines.RemoveAt(index);
                        close--;
                    }

                    continue;
                }

                var line = key + ": " + value;
                if (index > 0)
                {
                    lines[index] = line;
                }
                else
                {
                    lines.Insert(close, line);
                    close++;
                }
            }

            return WriteAtomic(notePath, string.Join(newline, lines));
        }

        private static string WriteAtomic(string notePath, string content)
        {
            try
            {
                var temp = notePath + ".tmp-" + Guid.NewGuid().ToString("N").Substring(0, 8);
                Pusula.Core.Scanning.DiskWatcher.MarkOwnWrite();
                File.WriteAllText(temp, content, new UTF8Encoding(HasBom(notePath)));
                File.Replace(temp, notePath, null);
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

        // Notun kodlaması korunur: BOM'lu ise BOM'lu, değilse (Obsidian'ın yazdığı gibi) BOM'suz; yeni not BOM'suz.
        internal static bool HasBom(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    return false;
                }

                using (var stream = File.OpenRead(path))
                {
                    return stream.ReadByte() == 0xEF && stream.ReadByte() == 0xBB && stream.ReadByte() == 0xBF;
                }
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }

        /// <summary>Olayın md bloğu (sonunda boş satır).</summary>
        public static string Format(JournalEntry entry)
        {
            var sb = new StringBuilder();
            var head = new List<string> { entry.Date.ToString(entry.Date.TimeOfDay == TimeSpan.Zero ? "dd.MM.yyyy" : "dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture) };
            if (entry.Kind.Trim().Length > 0)
            {
                head.Add(entry.Kind.Trim());
            }

            head.Add(entry.Title.Trim().Replace("\r", " ").Replace("\n", " "));
            sb.Append("### ").Append(string.Join(" · ", head)).Append('\n');

            var meta = new List<string>();
            if (entry.Importance.Trim().Length > 0 && !string.Equals(entry.Importance.Trim(), NormalImportance, StringComparison.OrdinalIgnoreCase))
            {
                meta.Add("önem: " + entry.Importance.Trim());
            }

            if (entry.Person.Trim().Length > 0)
            {
                meta.Add("kişi: " + entry.Person.Trim());
            }

            if (entry.Item.Trim().Length > 0)
            {
                meta.Add(ItemKey + " " + entry.Item.Trim());
            }

            if (entry.Result.Trim().Length > 0)
            {
                meta.Add(ResultKey + " " + entry.Result.Trim());
            }

            if (entry.Folder.Trim().Length > 0)
            {
                meta.Add(FolderKey + " " + entry.Folder.Trim().Replace('\\', '/'));
            }

            var people = entry.Participants.Select(p => p.Trim().TrimStart('@').Replace(' ', '_')).Where(p => p.Length > 0).Distinct(StringComparer.CurrentCultureIgnoreCase).ToList();
            if (people.Count > 0)
            {
                meta.Add(ParticipantsKey + " " + string.Join(" ", people.Select(p => "@" + p)));
            }

            if (entry.Setting.Trim().Length > 0)
            {
                meta.Add(SettingKey + " " + entry.Setting.Trim());
            }

            var related = entry.Related.Select(r => r.Trim()).Where(r => r.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (related.Count > 0)
            {
                meta.Add("ilgili: " + string.Join(" ", related.Select(r => "[[" + r + "]]")));
            }

            var tags = entry.Tags.Select(t => t.Trim().TrimStart('#').Replace(' ', '_')).Where(t => t.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (tags.Count > 0)
            {
                meta.Add(string.Join(" ", tags.Select(t => "#" + t)));
            }

            if (meta.Count > 0)
            {
                sb.Append(string.Join(" · ", meta)).Append('\n');
            }

            // Açıklama (kartta görünen), sonra her zaman "#### Ayrıntı" başlığı: ayrıntı MD'de onun altına yazılır.
            var body = SummaryOf(entry.Body);
            var details = (entry.Details.Length > 0 ? entry.Details : DetailsOf(entry.Body)).Replace("\r\n", "\n").Trim('\n', ' ', '\t');
            if (body.Length > 0)
            {
                sb.Append('\n').Append(body).Append('\n');
            }

            var decisions = (entry.Decisions.Length > 0 ? entry.Decisions : DecisionsOf(entry.Body)).Replace("\r\n", "\n").Trim('\n', ' ', '\t');
            if (decisions.Length > 0)
            {
                sb.Append('\n').Append(DecisionsHeading).Append('\n').Append(decisions).Append('\n');
            }

            sb.Append('\n').Append(DetailsHeading).Append('\n');
            if (details.Length > 0)
            {
                sb.Append(details).Append('\n');
            }

            sb.Append('\n');
            return sb.ToString();
        }

        /// <summary>Tür adından anahtar ("Toplantı" → <c>toplanti</c>).</summary>
        public static string KindKey(string kind)
        {
            var s = (kind ?? string.Empty).Trim().Replace('İ', 'i').Replace('I', 'ı').ToLower(CultureInfo.GetCultureInfo("tr-TR"));
            return s.Replace('ı', 'i').Replace('ğ', 'g').Replace('ü', 'u').Replace('ş', 's').Replace('ö', 'o').Replace('ç', 'c');
        }

        private static bool IsHeading(string line)
        {
            var trimmed = line.Trim();
            return string.Equals(trimmed, Heading, StringComparison.OrdinalIgnoreCase) || string.Equals(trimmed, "## Gunluk", StringComparison.OrdinalIgnoreCase);
        }

        private static EventNote? Parse(string path, string head, List<string> body, string sourceLine)
        {
            var parts = head.Split(new[] { " · " }, StringSplitOptions.None).Select(p => p.Trim()).ToList();
            DateTime date;
            if (parts.Count == 0 || !DateTime.TryParseExact(parts[0], DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            {
                return null;
            }

            var kind = parts.Count >= 3 ? parts[1] : string.Empty;
            var title = parts.Count >= 3 ? string.Join(" · ", parts.Skip(2)) : (parts.Count == 2 ? parts[1] : string.Empty);
            var person = string.Empty;
            var importance = string.Empty;
            var item = string.Empty;
            var result = string.Empty;
            var folder = string.Empty;
            var setting = string.Empty;
            var participants = new List<string>();
            var related = new List<string>();
            var tags = new List<string>();
            var text = new List<string>();
            var metaDone = false;
            foreach (var raw in body)
            {
                var line = raw.Trim();
                if (!metaDone && line.Length > 0 && IsMetaLine(line))
                {
                    foreach (var segment in line.Split(new[] { " · " }, StringSplitOptions.None).Select(s => s.Trim()))
                    {
                        var lower = segment.ToLower(CultureInfo.GetCultureInfo("tr-TR"));
                        if (lower.StartsWith("önem:", StringComparison.Ordinal))
                        {
                            importance = segment.Substring(5).Trim();
                        }
                        else if (lower.StartsWith("kişi:", StringComparison.Ordinal))
                        {
                            person = segment.Substring(5).Trim();
                        }
                        else if (lower.StartsWith(ItemKey, StringComparison.Ordinal))
                        {
                            item = segment.Substring(ItemKey.Length).Trim();
                        }
                        else if (lower.StartsWith(ResultKey, StringComparison.Ordinal))
                        {
                            result = segment.Substring(ResultKey.Length).Trim();
                        }
                        else if (lower.StartsWith(FolderKey, StringComparison.Ordinal))
                        {
                            folder = segment.Substring(FolderKey.Length).Trim();
                        }
                        else if (lower.StartsWith(ParticipantsKey, StringComparison.Ordinal))
                        {
                            participants.AddRange(segment.Substring(ParticipantsKey.Length).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(p => p.TrimStart('@')).Where(p => p.Length > 0));
                        }
                        else if (lower.StartsWith(SettingKey, StringComparison.Ordinal))
                        {
                            setting = segment.Substring(SettingKey.Length).Trim();
                        }
                    }

                    related.AddRange(Link.Matches(line).Cast<Match>().Select(m => m.Groups[1].Value.Trim()));
                    tags.AddRange(HashTag.Matches(line).Cast<Match>().Select(m => m.Groups[1].Value));
                    metaDone = true;
                    continue;
                }

                if (line.Length > 0)
                {
                    metaDone = true;
                }

                text.Add(raw);
            }

            var bodyText = string.Join("\n", text).Trim();
            related.AddRange(Link.Matches(bodyText).Cast<Match>().Select(m => m.Groups[1].Value.Trim()));
            var excerpt = Regex.Replace(SummaryOf(bodyText), @"\s+", " ");
            if (excerpt.Length > 240)
            {
                excerpt = excerpt.Substring(0, 237) + "…";
            }

            return EventNote.FromJournal(path, kind.Length > 0 ? KindKey(kind) : "not", date, title.Length > 0 ? title : kind, excerpt, person,
                string.Equals(importance, NormalImportance, StringComparison.OrdinalIgnoreCase) ? string.Empty : importance,
                related.Distinct(StringComparer.OrdinalIgnoreCase).ToList(), tags.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                sourceLine, bodyText, kind, item, result, folder, participants, setting, DecisionsOf(bodyText));
        }

        private static bool IsMetaLine(string line)
        {
            var lower = line.ToLower(CultureInfo.GetCultureInfo("tr-TR"));
            return lower.StartsWith("önem:", StringComparison.Ordinal) || lower.StartsWith("kişi:", StringComparison.Ordinal)
                || lower.StartsWith(ItemKey, StringComparison.Ordinal) || lower.StartsWith(ResultKey, StringComparison.Ordinal) || lower.StartsWith(FolderKey, StringComparison.Ordinal)
                || lower.StartsWith(ParticipantsKey, StringComparison.Ordinal) || lower.StartsWith(SettingKey, StringComparison.Ordinal)
                || lower.StartsWith("ilgili:", StringComparison.Ordinal) || (line.StartsWith("#", StringComparison.Ordinal) && !line.StartsWith("# ", StringComparison.Ordinal));
        }
    }
}
