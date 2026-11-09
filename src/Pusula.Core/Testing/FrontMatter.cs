using System;
using System.Collections.Generic;
using System.Text;

namespace Pusula.Core.Testing
{
    /// <summary>
    /// Markdown notunun başındaki YAML bloğunu (<c>---</c> … <c>---</c>) basitçe okur: yalnızca <c>anahtar: değer</c> satırları.
    /// İç içe yapı desteklenmez; liste değeri (<c>[a, b]</c>) olduğu gibi metin olarak döner.
    /// </summary>
    public static class FrontMatter
    {
        private const string SpecialStart = "-?:,[]{}#&*!|>'\"%@`";

        /// <summary>
        /// Değeri tek satırlık YAML değeri yapar: satır sonları boşluk olur; YAML'ı bozacak değer
        /// (<c>: </c> ya da <c> #</c> içeren, özel karakterle başlayan, <c>:</c> ile biten) çift tırnağa alınır.
        /// </summary>
        public static string Quote(string value)
        {
            var text = (value ?? string.Empty).Replace("\r", " ").Replace("\n", " ").Trim();
            if (text.Length == 0)
            {
                return text;
            }

            var needs = SpecialStart.IndexOf(text[0]) >= 0 || text.Contains(": ") || text.Contains(" #") || text.EndsWith(":", StringComparison.Ordinal);
            return needs ? "\"" + text.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"" : text;
        }

        /// <summary>YAML alanları (anahtarlar büyük/küçük harf duyarsız) ve gövde.</summary>
        public static Dictionary<string, string> Parse(string text, out string body)
        {
            var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            body = text ?? string.Empty;
            var lines = body.Replace("\r\n", "\n").Split('\n');
            if (lines.Length == 0 || lines[0].Trim().TrimStart('﻿') != "---")
            {
                return fields;
            }

            var end = -1;
            for (var i = 1; i < lines.Length; i++)
            {
                if (lines[i].Trim() == "---")
                {
                    end = i;
                    break;
                }

                var colon = lines[i].IndexOf(':');
                if (colon > 0)
                {
                    var key = lines[i].Substring(0, colon).Trim();
                    var value = lines[i].Substring(colon + 1).Trim().Trim('"', '\'');
                    fields[key] = value;
                }
            }

            if (end < 0)
            {
                fields.Clear();
                return fields;
            }

            var builder = new StringBuilder();
            for (var i = end + 1; i < lines.Length; i++)
            {
                builder.AppendLine(lines[i]);
            }

            body = builder.ToString();
            return fields;
        }
    }
}
