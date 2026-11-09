using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace Pusula.Core.Notes
{
    /// <summary>Bir notta bir kayda yapılan bahis (bağlantı ya da düz yazılmış numara).</summary>
    public sealed class Mention
    {
        /// <summary>Bahsedilen anahtar (numara ya da bağlantı hedefi).</summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>Notun yolu.</summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>Satır numarası (0'dan).</summary>
        public int Line { get; set; }

        /// <summary>Satırın sadeleştirilmiş hali (bağlantı köşeli parantezsiz, en çok 200 karakter).</summary>
        public string Snippet { get; set; } = string.Empty;

        /// <summary>Tarih: satır bir Günlük olayının altındaysa o olayın tarihi, değilse notun <c>tarih:</c> alanı, o da yoksa dosya tarihi.</summary>
        public DateTime Date { get; set; }

        /// <summary>Bağlantı mı (<c>[[…]]</c>), düz numara mı.</summary>
        public bool IsLink { get; set; }
    }

    /// <summary>
    /// Kök altındaki bütün md notlarında bahisleri bulur: <c>[[JIG-1000-0009]]</c>, <c>[[JIG-1000-0009|adaptör]]</c>,
    /// <c>[[7000_MILKON]]</c> gibi bağlantılar ve düz yazılmış numaralar (<c>AB-7000-0003</c>, <c>JIG-1000-0009</c>, <c>1234-5678-9012</c>).
    /// Gizli klasörler (<c>.obsidian</c>), <c>_SILINENLER</c>, <c>_PUSULA</c>, <c>_TAKIP</c> ve arşivler taranmaz. Notlara yazmaz.
    /// </summary>
    public sealed class MentionIndex
    {
        private static readonly Regex Link = new Regex(@"!?\[\[([^\]\|#\r\n]+)(#[^\]\|\r\n]*)?(\|[^\]\r\n]*)?\]\]", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex Number = new Regex(@"(?<![\w-])([A-Za-z]{2,5}-\d{4}-\d{4}|\d{4}-\d{4}-\d{4})(?![\w-])", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex JournalHeading = new Regex(@"^###\s+(\d{1,2}\.\d{1,2}\.\d{4})(\s+\d{1,2}:\d{2})?", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly string[] SkippedFolders = { "_SILINENLER", "_PUSULA", "_TAKIP", "_ARSIV", "Arsiv", "Arşiv" };

        private readonly Dictionary<string, List<Mention>> byKey = new Dictionary<string, List<Mention>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Taranan not sayısı.</summary>
        public int NoteCount { get; private set; }

        /// <summary>Kökü tarar (arka planda çağrılmalı).</summary>
        public static MentionIndex Build(string root, CancellationToken token)
        {
            var index = new MentionIndex();
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
            {
                return index;
            }

            var pending = new Stack<string>();
            pending.Push(root);
            while (pending.Count > 0)
            {
                token.ThrowIfCancellationRequested();
                var folder = pending.Pop();
                try
                {
                    foreach (var child in Directory.EnumerateDirectories(folder))
                    {
                        var name = System.IO.Path.GetFileName(child);
                        if (!name.StartsWith(".", StringComparison.Ordinal) && !SkippedFolders.Any(s => string.Equals(s, name, StringComparison.OrdinalIgnoreCase)))
                        {
                            pending.Push(child);
                        }
                    }

                    foreach (var file in Directory.EnumerateFiles(folder, "*.md"))
                    {
                        index.Add(file);
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
        /// Anahtarlardan herhangi birine yapılan bahisler, yeniden eskiye. <paramref name="ownFolder"/> altındaki notlar
        /// (kaydın kendi notları) sayılmaz. Aynı satır bir kez döner.
        /// </summary>
        public List<Mention> Find(IEnumerable<string> keys, string ownFolder)
        {
            var own = string.IsNullOrEmpty(ownFolder) ? string.Empty : ownFolder.TrimEnd('\\', '/') + System.IO.Path.DirectorySeparatorChar;
            var result = new List<Mention>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var key in keys.Where(k => !string.IsNullOrWhiteSpace(k)).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                List<Mention> list;
                if (!byKey.TryGetValue(key.Trim(), out list))
                {
                    continue;
                }

                foreach (var mention in list)
                {
                    if (own.Length > 0 && mention.Path.StartsWith(own, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (seen.Add(mention.Path + "|" + mention.Line.ToString(CultureInfo.InvariantCulture)))
                    {
                        result.Add(mention);
                    }
                }
            }

            return result.OrderByDescending(m => m.Date).ThenBy(m => m.Path, StringComparer.OrdinalIgnoreCase).ThenBy(m => m.Line).ToList();
        }

        /// <summary>Tek bir notun metnini tarar (testler ve <see cref="Build"/> için).</summary>
        public void AddText(string path, string text, DateTime fileDate)
        {
            NoteCount++;
            var lines = (text ?? string.Empty).Replace("\r\n", "\n").Split('\n');
            var noteDate = fileDate;
            var start = 0;
            if (lines.Length > 0 && lines[0].Trim().TrimStart('﻿') == "---")
            {
                for (var i = 1; i < lines.Length; i++)
                {
                    if (lines[i].Trim() == "---")
                    {
                        start = i + 1;
                        break;
                    }

                    DateTime parsed;
                    var line = lines[i].Trim();
                    if (line.StartsWith("tarih:", StringComparison.OrdinalIgnoreCase)
                        && DateTime.TryParseExact(line.Substring(6).Trim().Trim('"', '\''), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
                    {
                        noteDate = parsed;
                    }
                }
            }

            DateTime? entryDate = null;
            var inCode = false;
            for (var i = start; i < lines.Length; i++)
            {
                var line = lines[i];
                if (line.TrimStart().StartsWith("```", StringComparison.Ordinal))
                {
                    inCode = !inCode;
                    continue;
                }

                if (inCode)
                {
                    continue;
                }

                var heading = JournalHeading.Match(line);
                if (heading.Success)
                {
                    DateTime parsed;
                    entryDate = DateTime.TryParseExact(heading.Groups[1].Value + heading.Groups[2].Value, new[] { "d.M.yyyy", "d.M.yyyy H:mm", "d.M.yyyy HH:mm" }, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out parsed) ? parsed : (DateTime?)null;
                }
                else if (line.StartsWith("#", StringComparison.Ordinal) && !line.StartsWith("###", StringComparison.Ordinal))
                {
                    entryDate = null;
                }

                var keys = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
                foreach (Match link in Link.Matches(line))
                {
                    var target = link.Groups[1].Value.Trim();
                    keys[target] = true;
                    var inner = Number.Match(target);
                    if (inner.Success)
                    {
                        keys[inner.Groups[1].Value] = true;
                    }
                }

                foreach (Match number in Number.Matches(Link.Replace(line, string.Empty)))
                {
                    if (!keys.ContainsKey(number.Groups[1].Value))
                    {
                        keys[number.Groups[1].Value] = false;
                    }
                }

                if (keys.Count == 0)
                {
                    continue;
                }

                var snippet = Simplify(line);
                foreach (var pair in keys)
                {
                    List<Mention> list;
                    if (!byKey.TryGetValue(pair.Key, out list))
                    {
                        list = new List<Mention>();
                        byKey[pair.Key] = list;
                    }

                    list.Add(new Mention { Key = pair.Key, Path = path, Line = i, Snippet = snippet, Date = entryDate ?? noteDate, IsLink = pair.Value });
                }
            }
        }

        private void Add(string file)
        {
            try
            {
                AddText(file, File.ReadAllText(file), File.GetLastWriteTime(file));
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        // "[[JIG-1|adaptör]]" → "adaptör", "[[JIG-1]]" → "JIG-1"; başlık / liste işaretleri atılır.
        private static string Simplify(string line)
        {
            var text = Link.Replace(line, m => m.Groups[3].Success && m.Groups[3].Value.Length > 1 ? m.Groups[3].Value.Substring(1) : m.Groups[1].Value);
            text = text.Trim().TrimStart('#', '-', '*', '>', ' ').Replace("**", string.Empty).Replace("`", string.Empty).Trim();
            if (text.StartsWith("[ ]", StringComparison.Ordinal) || text.StartsWith("[x]", StringComparison.OrdinalIgnoreCase))
            {
                text = text.Substring(3).Trim();
            }

            return text.Length > 200 ? text.Substring(0, 199) + "…" : text;
        }
    }
}
