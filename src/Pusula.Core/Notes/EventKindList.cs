using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Pusula.Core.Scanning;

namespace Pusula.Core.Notes
{
    /// <summary>Kullanıcının eklediği işlem türü (zaman çizelgesi): ad, ikon kodu, renk tonu.</summary>
    public sealed class CustomEventKind
    {
        /// <summary>Görünen ad ("Kalibrasyon").</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Segoe Fluent ikon kodu (onaltılık, örn. <c>E8A5</c>); boşsa varsayılan ikon.</summary>
        public string Glyph { get; set; } = string.Empty;

        /// <summary>Renk tonu (<c>active</c>, <c>done</c>, <c>waiting</c>, <c>failed</c>, <c>neutral</c>).</summary>
        public string Tone { get; set; } = string.Empty;
    }

    /// <summary>
    /// Eklenen işlem türleri: tüm projelerde ortak, kökte <c>_TAKIP\Islemler.md</c>. Her tür bir madde satırı:
    /// <c>- Kalibrasyon · E8A5 · active</c>. Obsidian'dan elle de eklenebilir (yalnız ad yazılırsa varsayılan ikon ve renk).
    /// </summary>
    public static class EventKindList
    {
        /// <summary>Listenin kökteki yolu.</summary>
        public static string PathFor(string root)
        {
            return Path.Combine(root, "_TAKIP", "Islemler.md");
        }

        /// <summary>Türler (dosyadaki sırayla, adı tekrarsız); dosya yoksa boş.</summary>
        public static List<CustomEventKind> Read(string root)
        {
            var result = new List<CustomEventKind>();
            if (string.IsNullOrEmpty(root))
            {
                return result;
            }

            var path = PathFor(root);
            try
            {
                if (!File.Exists(path))
                {
                    return result;
                }

                foreach (var line in File.ReadAllLines(path))
                {
                    var t = line.Trim();
                    if (!t.StartsWith("- ", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    var parts = t.Substring(2).Split(new[] { " · ", "|" }, StringSplitOptions.None).Select(p => p.Trim()).ToList();
                    if (parts.Count == 0 || parts[0].Length == 0 || result.Any(k => string.Equals(k.Name, parts[0], StringComparison.InvariantCultureIgnoreCase)))
                    {
                        continue;
                    }

                    result.Add(new CustomEventKind
                    {
                        Name = parts[0],
                        Glyph = parts.Count > 1 ? parts[1] : string.Empty,
                        Tone = parts.Count > 2 ? parts[2] : string.Empty,
                    });
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }

            return result;
        }

        /// <summary>
        /// Türü listenin sonuna ekler (aynı adla varsa dokunmaz); dosya yoksa <paramref name="newFileTemplate"/> ile açar.
        /// Yazım atomiktir. Hata metni ya da boş döndürür.
        /// </summary>
        public static string Add(string root, CustomEventKind kind, string newFileTemplate)
        {
            if (kind == null || kind.Name.Trim().Length == 0 || string.IsNullOrEmpty(root))
            {
                return string.Empty;
            }

            if (Read(root).Any(k => string.Equals(k.Name, kind.Name.Trim(), StringComparison.InvariantCultureIgnoreCase)))
            {
                return string.Empty;
            }

            var path = PathFor(root);
            try
            {
                var exists = File.Exists(path);
                var text = exists ? File.ReadAllText(path) : (newFileTemplate ?? string.Empty);
                var newline = text.Contains("\r\n") || !exists ? "\r\n" : "\n";
                text = text.TrimEnd('\r', '\n', ' ', '\t');
                var line = "- " + kind.Name.Trim().Replace("·", "-").Replace("|", "-") + " · " + kind.Glyph.Trim() + " · " + kind.Tone.Trim();
                text = (text.Length == 0 ? string.Empty : text + newline + newline) + line + newline;
                if (exists)
                {
                    // Var olan listeye boş satır koymadan eklenir.
                    var raw = File.ReadAllText(path).TrimEnd('\r', '\n', ' ', '\t');
                    text = raw + newline + line + newline;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(path) ?? root);
                var temp = path + ".tmp-" + Guid.NewGuid().ToString("N").Substring(0, 8);
                DiskWatcher.MarkOwnWrite();
                File.WriteAllText(temp, text, new UTF8Encoding(exists && Journal.HasBom(path)));
                if (exists)
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
    }
}
