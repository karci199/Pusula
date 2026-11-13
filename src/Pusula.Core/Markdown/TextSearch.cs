using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Pusula.Core.Markdown
{
    /// <summary>Arama seçenekleri.</summary>
    public sealed class SearchOptions
    {
        /// <summary>Büyük / küçük harf ayrılsın mı.</summary>
        public bool MatchCase { get; set; }

        /// <summary>Yalnız tam kelime.</summary>
        public bool WholeWord { get; set; }

        /// <summary>Aranan metin düzenli ifade (regex) mi.</summary>
        public bool UseRegex { get; set; }
    }

    /// <summary>Metinde bir eşleşme.</summary>
    public sealed class TextMatch
    {
        /// <summary>Eşleşmeyi kurar.</summary>
        public TextMatch(int start, int length)
        {
            Start = start;
            Length = length;
        }

        /// <summary>Başlangıç (karakter).</summary>
        public int Start { get; }

        /// <summary>Uzunluk.</summary>
        public int Length { get; }
    }

    /// <summary>Arama sonucu: eşleşmeler ya da geçersiz ifade hatası.</summary>
    public sealed class SearchResult
    {
        /// <summary>Sonucu kurar.</summary>
        public SearchResult(IReadOnlyList<TextMatch> matches, string error)
        {
            Matches = matches;
            Error = error ?? string.Empty;
        }

        /// <summary>Eşleşmeler (baştan sona).</summary>
        public IReadOnlyList<TextMatch> Matches { get; }

        /// <summary>Düzenli ifade geçersizse açıklaması; değilse boş.</summary>
        public string Error { get; }

        /// <summary>İmleçten sonraki (ya da imleçteki) ilk eşleşmenin sırası; sona gelindiyse başa döner. Eşleşme yoksa -1.</summary>
        public int IndexFrom(int caret)
        {
            if (Matches.Count == 0)
            {
                return -1;
            }

            for (var i = 0; i < Matches.Count; i++)
            {
                if (Matches[i].Start >= caret)
                {
                    return i;
                }
            }

            return 0;
        }

        /// <summary>İmleçten önceki son eşleşmenin sırası; baştaysa sona döner. Eşleşme yoksa -1.</summary>
        public int IndexBefore(int caret)
        {
            if (Matches.Count == 0)
            {
                return -1;
            }

            for (var i = Matches.Count - 1; i >= 0; i--)
            {
                if (Matches[i].Start < caret)
                {
                    return i;
                }
            }

            return Matches.Count - 1;
        }

        /// <summary>Verilen aralıkla birebir aynı eşleşmenin sırası; yoksa -1.</summary>
        public int IndexOf(int start, int length)
        {
            for (var i = 0; i < Matches.Count; i++)
            {
                if (Matches[i].Start == start && Matches[i].Length == length)
                {
                    return i;
                }
            }

            return -1;
        }
    }

    /// <summary>
    /// Not penceresinde bul / değiştir: düz metin, tam kelime, büyük / küçük harf (Türkçe kurallarıyla) ve düzenli ifade.
    /// Boş eşleşmeler (ör. <c>^</c>) sayılmaz.
    /// </summary>
    public static class TextSearch
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(1);

        /// <summary>Bütün eşleşmeleri bulur.</summary>
        public static SearchResult FindAll(string text, string query, SearchOptions options)
        {
            using (new TurkishScope())
            {
                return FindAllCore(text, query, options);
            }
        }

        /// <summary>Tek eşleşmeyi değiştirir. Düzenli ifadede <c>$1</c> gibi gruplar kullanılabilir; eşleşme artık geçerli değilse <c>null</c>.</summary>
        public static EditResult? Replace(string text, TextMatch match, string query, string replacement, SearchOptions options)
        {
            using (new TurkishScope())
            {
                return ReplaceCore(text, match, query, replacement, options);
            }
        }

        /// <summary>Hepsini değiştirir; değişen sayıyı <paramref name="count"/> verir.</summary>
        public static string ReplaceAll(string text, string query, string replacement, SearchOptions options, out int count)
        {
            using (new TurkishScope())
            {
                return ReplaceAllCore(text, query, replacement, options, out count);
            }
        }

        private static SearchResult FindAllCore(string text, string query, SearchOptions options)
        {
            var matches = new List<TextMatch>();
            Regex regex;
            string error;
            if (!TryBuild(query, options, out regex, out error))
            {
                return new SearchResult(matches, error);
            }

            try
            {
                foreach (Match match in regex.Matches(text ?? string.Empty))
                {
                    if (match.Length > 0)
                    {
                        matches.Add(new TextMatch(match.Index, match.Length));
                    }
                }
            }
            catch (RegexMatchTimeoutException)
            {
                return new SearchResult(new List<TextMatch>(), "timeout");
            }

            return new SearchResult(matches, string.Empty);
        }

        private static EditResult? ReplaceCore(string text, TextMatch match, string query, string replacement, SearchOptions options)
        {
            text = text ?? string.Empty;
            Regex regex;
            string error;
            if (match == null || match.Start < 0 || match.Start + match.Length > text.Length || !TryBuild(query, options, out regex, out error))
            {
                return null;
            }

            try
            {
                var found = regex.Match(text, match.Start);
                if (!found.Success || found.Index != match.Start || found.Length != match.Length)
                {
                    return null;
                }

                var value = options.UseRegex ? found.Result(replacement ?? string.Empty) : replacement ?? string.Empty;
                return new EditResult(text.Substring(0, match.Start) + value + text.Substring(match.Start + match.Length), match.Start, value.Length);
            }
            catch (RegexMatchTimeoutException)
            {
                return null;
            }
        }

        private static string ReplaceAllCore(string text, string query, string replacement, SearchOptions options, out int count)
        {
            text = text ?? string.Empty;
            count = 0;
            Regex regex;
            string error;
            if (!TryBuild(query, options, out regex, out error))
            {
                return text;
            }

            var changed = 0;
            try
            {
                var result = regex.Replace(text, match =>
                {
                    if (match.Length == 0)
                    {
                        return match.Value;
                    }

                    changed++;
                    return options.UseRegex ? match.Result(replacement ?? string.Empty) : replacement ?? string.Empty;
                });
                count = changed;
                return result;
            }
            catch (RegexMatchTimeoutException)
            {
                count = 0;
                return text;
            }
        }

        private static bool TryBuild(string query, SearchOptions options, out Regex regex, out string error)
        {
            regex = null!;
            error = string.Empty;
            if (string.IsNullOrEmpty(query))
            {
                return false;
            }

            var pattern = options.UseRegex ? query : Regex.Escape(query);
            if (options.WholeWord)
            {
                pattern = @"(?<![\p{L}\p{N}_])(?:" + pattern + @")(?![\p{L}\p{N}_])";
            }

            var flags = RegexOptions.Multiline | (options.MatchCase ? RegexOptions.None : RegexOptions.IgnoreCase);
            try
            {
                regex = new Regex(pattern, flags, Timeout);
                return true;
            }
            catch (ArgumentException ex)
            {
                error = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// Büyük / küçük harf eşleştirmesi Türkçe kurallarıyla (İ ↔ i, I ↔ ı). .NET Framework kültürü hem ifade kurulurken hem
        /// eşleştirirken okur; bu yüzden işlemin tamamı bu kapsamın içinde yapılır.
        /// </summary>
        private sealed class TurkishScope : IDisposable
        {
            private readonly CultureInfo previous = CultureInfo.CurrentCulture;

            public TurkishScope()
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
            }

            public void Dispose()
            {
                CultureInfo.CurrentCulture = previous;
            }
        }
    }
}
