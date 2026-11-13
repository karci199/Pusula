using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Pusula.Core.Notes
{
    /// <summary>
    /// Uygulamadan iş ekleme ve kapatma (kullanıcı kararı, 10.11.2026). İş, ana notun <c>## Yapılacaklar</c> bölümüne
    /// Obsidian Tasks biçiminde bir satır olarak eklenir: <c>- [ ] metin @Ad_Soyad 📅 2026-11-14</c>. Bölüm yoksa
    /// <c>## Günlük</c>'ün üstüne (o da yoksa notun sonuna) açılır. Kapatmak yalnız o satırın kutusunu değiştirir
    /// (<c>[x]</c> ve <c>✅ tarih</c>); notun başka hiçbir satırına dokunulmaz. Yazım atomiktir.
    /// </summary>
    public static class TaskList
    {
        /// <summary>Bölüm başlığı.</summary>
        public const string Heading = "## Yapılacaklar";

        private const string DueMark = "\uD83D\uDCC5";
        private const string DoneMark = "\u2705";

        /// <summary>İş satırını kurar: <c>- [ ] metin @Ad_Soyad 📅 yyyy-MM-dd</c> (kişi ve tarih isteğe bağlı).</summary>
        public static string FormatLine(string text, string person, DateTime? due)
        {
            var line = "- [ ] " + string.Join(" ", (text ?? string.Empty).Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
            var name = string.Join("_", (person ?? string.Empty).Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries));
            if (name.Length > 0)
            {
                line += " @" + name;
            }

            if (due.HasValue)
            {
                line += " " + DueMark + " " + due.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }

            return line;
        }

        /// <summary>İşi notun Yapılacaklar bölümünün sonuna ekler; not yoksa açılır. Hata metni; başarıda boş.</summary>
        public static string Add(string notePath, string text, string person, DateTime? due)
        {
            if ((text ?? string.Empty).Trim().Length == 0)
            {
                return "text";
            }

            string content;
            try
            {
                content = File.Exists(notePath) ? File.ReadAllText(notePath, Encoding.UTF8) : string.Empty;
            }
            catch (IOException ex)
            {
                return ex.Message;
            }
            catch (UnauthorizedAccessException ex)
            {
                return ex.Message;
            }

            var newline = content.Contains("\r\n") || content.Length == 0 ? "\r\n" : "\n";
            var line = FormatLine(text!, person, due);
            var lines = content.Replace("\r\n", "\n").Split('\n').ToList();
            var heading = lines.FindIndex(l => string.Equals(l.Trim(), Heading, StringComparison.OrdinalIgnoreCase));
            if (heading >= 0)
            {
                // Başlıktan sonraki boşlukları ve mevcut maddeleri geç; yeni iş listenin sonuna.
                var at = heading + 1;
                while (at < lines.Count && lines[at].Trim().Length == 0)
                {
                    at++;
                }

                var hasItems = false;
                while (at < lines.Count && OpenTask.TaskLine.IsMatch(lines[at]))
                {
                    at++;
                    hasItems = true;
                }

                if (!hasItems && at == heading + 1)
                {
                    lines.Insert(at++, string.Empty);
                }

                lines.Insert(at, line);
                if (at + 1 < lines.Count && lines[at + 1].Trim().Length > 0 && !OpenTask.TaskLine.IsMatch(lines[at + 1]))
                {
                    lines.Insert(at + 1, string.Empty);
                }
            }
            else
            {
                var journal = lines.FindIndex(l => string.Equals(l.Trim(), Journal.Heading, StringComparison.OrdinalIgnoreCase));
                if (journal >= 0)
                {
                    // Günlük'ün önündeki "---" ayracı bölümle Günlük arasında kalsın.
                    var at = journal;
                    var above = journal - 1;
                    while (above >= 0 && lines[above].Trim().Length == 0)
                    {
                        above--;
                    }

                    if (above >= 0 && lines[above].Trim() == "---")
                    {
                        at = above;
                    }

                    lines.InsertRange(at, new[] { Heading, string.Empty, line, string.Empty });
                }
                else
                {
                    while (lines.Count > 0 && lines[lines.Count - 1].Trim().Length == 0)
                    {
                        lines.RemoveAt(lines.Count - 1);
                    }

                    if (lines.Count > 0)
                    {
                        lines.Add(string.Empty);
                    }

                    lines.AddRange(new[] { Heading, string.Empty, line });
                }
            }

            var updated = string.Join(newline, lines);
            if (!updated.EndsWith(newline, StringComparison.Ordinal))
            {
                updated += newline;
            }

            return Write(notePath, updated);
        }

        /// <summary>
        /// Görevi bitti / açık yapar: yalnız o satırın kutusu ve <c>✅ tarih</c>'i değişir. Satır, okunduğundan beri
        /// değiştiyse (<paramref name="expectedLine"/> tutmuyorsa) dokunulmaz ve "changed" döner. Hata metni; başarıda boş.
        /// </summary>
        public static string SetDone(string notePath, int line, string expectedLine, bool done, DateTime day)
        {
            string content;
            try
            {
                content = File.ReadAllText(notePath, Encoding.UTF8);
            }
            catch (IOException ex)
            {
                return ex.Message;
            }
            catch (UnauthorizedAccessException ex)
            {
                return ex.Message;
            }

            var newline = content.Contains("\r\n") ? "\r\n" : "\n";
            var lines = content.Replace("\r\n", "\n").Split('\n');
            if (line < 0 || line >= lines.Length || !string.Equals(lines[line], expectedLine, StringComparison.Ordinal))
            {
                return "changed";
            }

            var match = OpenTask.TaskLine.Match(lines[line]);
            if (!match.Success)
            {
                return "changed";
            }

            var mark = match.Groups["mark"];
            var updated = lines[line].Substring(0, mark.Index) + (done ? "x" : " ") + lines[line].Substring(mark.Index + mark.Length);
            updated = System.Text.RegularExpressions.Regex.Replace(updated, "\\s*" + DoneMark + "\\s*\\d{4}-\\d{2}-\\d{2}", string.Empty).TrimEnd();
            if (done)
            {
                updated += " " + DoneMark + " " + day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }

            lines[line] = updated;
            return Write(notePath, string.Join(newline, lines));
        }

        private static string Write(string notePath, string content)
        {
            try
            {
                var directory = Path.GetDirectoryName(notePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var temp = notePath + ".tmp-" + Guid.NewGuid().ToString("N").Substring(0, 8);
                Scanning.DiskWatcher.MarkOwnWrite();
                File.WriteAllText(temp, content, new UTF8Encoding(Journal.HasBom(notePath)));
                if (File.Exists(notePath))
                {
                    File.Replace(temp, notePath, null);
                }
                else
                {
                    File.Move(temp, notePath);
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
    }
}