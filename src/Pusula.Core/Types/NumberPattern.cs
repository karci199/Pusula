using System;
using System.Text;
using System.Text.RegularExpressions;

namespace Pusula.Core.Types
{
    /// <summary>
    /// Numara biçimi: <c>#</c> rakam, <c>@</c> harf, diğer her karakter kendisi.
    /// Örnek: <c>@@-####-####</c> → AB-7000-0001; <c>JY-####-####</c> → JY-7000-0001.
    /// Büyük/küçük harf duyarsızdır.
    /// </summary>
    public static class NumberPattern
    {
        /// <summary>Numara biçime uyuyor mu.</summary>
        public static bool IsMatch(string pattern, string text)
        {
            if (string.IsNullOrEmpty(pattern) || string.IsNullOrEmpty(text))
            {
                return false;
            }

            return Regex.IsMatch(text.Trim(), ToRegex(pattern), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        /// <summary>Biçimi kullanıcıya gösterilecek örneğe çevirir: <c>@@-####</c> → <c>AB-0000</c>.</summary>
        public static string Example(string pattern)
        {
            var builder = new StringBuilder();
            var letter = 'A';
            foreach (var c in pattern ?? string.Empty)
            {
                if (c == '#')
                {
                    builder.Append('0');
                }
                else if (c == '@')
                {
                    builder.Append(letter);
                    letter = letter == 'Z' ? 'A' : (char)(letter + 1);
                }
                else
                {
                    builder.Append(c);
                }
            }

            return builder.ToString();
        }

        private static string ToRegex(string pattern)
        {
            var builder = new StringBuilder("^");
            foreach (var c in pattern)
            {
                if (c == '#')
                {
                    builder.Append("[0-9]");
                }
                else if (c == '@')
                {
                    builder.Append("[A-Za-z]");
                }
                else
                {
                    builder.Append(Regex.Escape(c.ToString()));
                }
            }

            builder.Append('$');
            return builder.ToString();
        }
    }
}
