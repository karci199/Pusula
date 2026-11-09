using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Pusula.Core.Testing;

using Pusula.Core.Scanning;

namespace Pusula.Core.Notes
{
    /// <summary>
    /// Obsidian notları (<c>.md</c>): YAML bloğu ve başlıkla oluşturma, gövdenin başını okuma.
    /// Var olan notun gövdesine asla dokunulmaz.
    /// </summary>
    public static class NoteFile
    {
        private const int ExcerptLength = 240;

        /// <summary>
        /// Notu oluşturur: YAML bloğu (<paramref name="fields"/>, sırasıyla) ve <c># başlık</c>. Not zaten varsa dokunmaz.
        /// Oluşan (ya da var olan) notun yolunu döndürür; yazılamazsa boş.
        /// </summary>
        public static string Create(string path, IEnumerable<KeyValuePair<string, string>> fields, string heading)
        {
            if (File.Exists(path))
            {
                return path;
            }

            var text = new StringBuilder();
            text.Append("---\r\n");
            foreach (var field in fields)
            {
                text.Append(field.Key).Append(": ").Append(field.Value ?? string.Empty).Append("\r\n");
            }

            text.Append("---\r\n\r\n");
            text.Append("# ").Append(heading).Append("\r\n\r\n");
            var temp = path + ".tmp";
            try
            {
                File.WriteAllText(temp, text.ToString(), new UTF8Encoding(false));
                File.Move(temp, path);
                return path;
            }
            catch (IOException)
            {
                return File.Exists(path) ? path : string.Empty;
            }
            catch (UnauthorizedAccessException)
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Klasörün notu: önce <c>&lt;ad&gt;.md</c>, yoksa klasördeki ilk <c>.md</c>; hiç yoksa boş.
        /// </summary>
        public static string Find(string folder, string name)
        {
            var preferred = Path.Combine(folder, name + ".md");
            if (File.Exists(preferred))
            {
                return preferred;
            }

            try
            {
                return Fs.GetFiles(folder, "*.md").OrderBy(f => f, StringComparer.OrdinalIgnoreCase).FirstOrDefault() ?? string.Empty;
            }
            catch (IOException)
            {
                return string.Empty;
            }
            catch (UnauthorizedAccessException)
            {
                return string.Empty;
            }
        }

        /// <summary>Gövdenin başı: YAML, başlıklar ve boş satırlar atlanır; en çok ~240 karakter.</summary>
        public static string Excerpt(string path)
        {
            string text;
            try
            {
                text = Fs.ReadAllText(path);
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

                // Günlük bölümü (ve önündeki ayırıcı) özete girmez; özet notun serbest kısmından alınır.
                if (string.Equals(line, Journal.Heading, StringComparison.OrdinalIgnoreCase) || string.Equals(line, "## Gunluk", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                if (line.Length == 0 || line == "---" || line.StartsWith("#", StringComparison.Ordinal))
                {
                    continue;
                }

                line = line.TrimStart('-', '*', ' ');
                parts.Add(line);
                length += line.Length + 1;
                if (length >= ExcerptLength)
                {
                    break;
                }
            }

            var joined = string.Join(" ", parts);
            return joined.Length > ExcerptLength ? joined.Substring(0, ExcerptLength - 3) + "…" : joined;
        }
    }
}
