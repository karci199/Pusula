using System;
using System.Text.RegularExpressions;

namespace Pusula.Core.Markdown
{
    /// <summary>Bir düzenleme sonucu: yeni metin ve seçim.</summary>
    public sealed class EditResult
    {
        /// <summary>Sonucu kurar.</summary>
        public EditResult(string text, int selectionStart, int selectionLength)
        {
            Text = text;
            SelectionStart = selectionStart;
            SelectionLength = selectionLength;
        }

        /// <summary>Yeni metin.</summary>
        public string Text { get; }

        /// <summary>Yeni seçim başı.</summary>
        public int SelectionStart { get; }

        /// <summary>Yeni seçim uzunluğu.</summary>
        public int SelectionLength { get; }
    }

    /// <summary>Öneri listesini açan yazım: <c>@sorgu</c> ya da <c>[[sorgu</c>.</summary>
    public sealed class SuggestQuery
    {
        /// <summary>Tetikleyicinin metindeki yeri (<c>@</c> ya da <c>[[</c>'nin ilk karakteri).</summary>
        public int Start { get; set; }

        /// <summary>Tetikleyiciden sonra yazılan.</summary>
        public string Text { get; set; } = string.Empty;

        /// <summary><c>[[</c> ile mi açıldı.</summary>
        public bool IsLink { get; set; }
    }

    /// <summary>
    /// md düzenleyicisinin metin işlemleri (arayüzden bağımsız, test edilebilir): sarma (kalın, italik …), satır başı ekleme
    /// (liste, görev, başlık, alıntı), Enter'da listeyi sürdürme, girinti, öneri sorgusunu bulma ve öneriyi yerleştirme.
    /// Satır sonu olarak metindeki biçim (CRLF ya da LF) korunur.
    /// </summary>
    public static class MarkdownEditing
    {
        private static readonly Regex ListLine = new Regex(@"^(\s*)((?:[-*+])|(?:\d+)([.)]))(\s+)(\[[ xX]\]\s+)?", RegexOptions.CultureInvariant);

        /// <summary>
        /// Seçimi işaretle sarar (<c>**</c>, <c>*</c>, <c>~~</c>, <c>==</c>, <c>`</c>); seçim zaten sarılıysa işareti kaldırır.
        /// Seçim boşsa işaret çifti eklenir ve imleç arasına konur.
        /// </summary>
        public static EditResult Wrap(string text, int start, int length, string mark)
        {
            text = text ?? string.Empty;
            start = Clamp(start, 0, text.Length);
            length = Clamp(length, 0, text.Length - start);
            var m = mark.Length;
            if (start >= m && start + length + m <= text.Length && text.Substring(start - m, m) == mark && text.Substring(start + length, m) == mark)
            {
                var removed = text.Remove(start + length, m).Remove(start - m, m);
                return new EditResult(removed, start - m, length);
            }

            var selected = text.Substring(start, length);
            if (selected.Length >= 2 * m && selected.StartsWith(mark, StringComparison.Ordinal) && selected.EndsWith(mark, StringComparison.Ordinal))
            {
                var inner = selected.Substring(m, selected.Length - 2 * m);
                return new EditResult(text.Substring(0, start) + inner + text.Substring(start + length), start, inner.Length);
            }

            var result = text.Substring(0, start) + mark + selected + mark + text.Substring(start + length);
            return new EditResult(result, start + m, length);
        }

        /// <summary>
        /// Seçimin kapsadığı satırların başına öneki ekler (<c>- </c>, <c>- [ ] </c>, <c>1. </c>, <c>&gt; </c>, <c>## </c>); bütün satırlarda
        /// zaten varsa kaldırır. Başlıkta önceki başlık işareti değiştirilir.
        /// </summary>
        public static EditResult TogglePrefix(string text, int start, int length, string prefix)
        {
            text = text ?? string.Empty;
            start = Clamp(start, 0, text.Length);
            length = Clamp(length, 0, text.Length - start);
            var first = LineStart(text, start);
            var last = LineEnd(text, start + length);
            var block = text.Substring(first, last - first);
            var newline = NewlineOf(text);
            var lines = block.Replace("\r\n", "\n").Split('\n');
            var heading = prefix.TrimEnd().Trim('#').Length == 0 && prefix.StartsWith("#", StringComparison.Ordinal);
            var all = true;
            foreach (var line in lines)
            {
                if (!line.StartsWith(prefix, StringComparison.Ordinal))
                {
                    all = false;
                }
            }

            for (var i = 0; i < lines.Length; i++)
            {
                if (all)
                {
                    lines[i] = lines[i].Substring(prefix.Length);
                }
                else if (heading)
                {
                    lines[i] = prefix + Regex.Replace(lines[i], @"^#{1,6}\s+", string.Empty);
                }
                else if (lines[i].Length > 0 || lines.Length == 1)
                {
                    lines[i] = prefix + lines[i];
                }
            }

            var replaced = string.Join(newline, lines);
            var result = text.Substring(0, first) + replaced + text.Substring(last);
            var caret = lines.Length == 1 && length == 0 ? Math.Max(first, start + (replaced.Length - block.Length)) : first;
            return new EditResult(result, caret, lines.Length == 1 && length == 0 ? 0 : replaced.Length);
        }

        /// <summary>
        /// Enter: imleç bir liste satırındaysa yeni satıra aynı işareti koyar (numarayı artırır, görev kutusunu boş açar).
        /// Liste satırı boşsa (yalnız işaret) işareti siler ve listeden çıkar. Liste satırı değilse <c>null</c>.
        /// </summary>
        public static EditResult? ContinueList(string text, int caret)
        {
            text = text ?? string.Empty;
            caret = Clamp(caret, 0, text.Length);
            var first = LineStart(text, caret);
            var line = text.Substring(first, LineEnd(text, caret) - first);
            var match = ListLine.Match(line);
            if (!match.Success || caret - first < match.Length)
            {
                return null;
            }

            var newline = NewlineOf(text);
            if (line.Trim().Length == match.Value.Trim().Length)
            {
                // Boş madde: işaret silinir.
                var cleared = text.Remove(first, line.Length);
                return new EditResult(cleared, first, 0);
            }

            string marker;
            int number;
            if (match.Groups[3].Success && int.TryParse(match.Groups[2].Value.TrimEnd('.', ')'), out number))
            {
                marker = (number + 1).ToString(System.Globalization.CultureInfo.InvariantCulture) + match.Groups[3].Value;
            }
            else
            {
                marker = match.Groups[2].Value;
            }

            var insert = newline + match.Groups[1].Value + marker + match.Groups[4].Value + (match.Groups[5].Success ? "[ ] " : string.Empty);
            return new EditResult(text.Insert(caret, insert), caret + insert.Length, 0);
        }

        /// <summary>Tab / Shift+Tab: seçili satırları iki boşluk içeri ya da dışarı alır.</summary>
        public static EditResult Indent(string text, int start, int length, bool outdent)
        {
            text = text ?? string.Empty;
            start = Clamp(start, 0, text.Length);
            length = Clamp(length, 0, text.Length - start);
            var first = LineStart(text, start);
            var last = LineEnd(text, start + length);
            var newline = NewlineOf(text);
            var lines = text.Substring(first, last - first).Replace("\r\n", "\n").Split('\n');
            var shiftFirst = 0;
            for (var i = 0; i < lines.Length; i++)
            {
                var before = lines[i].Length;
                if (outdent)
                {
                    lines[i] = lines[i].StartsWith("  ", StringComparison.Ordinal) ? lines[i].Substring(2) : lines[i].StartsWith("\t", StringComparison.Ordinal) || lines[i].StartsWith(" ", StringComparison.Ordinal) ? lines[i].Substring(1) : lines[i];
                }
                else
                {
                    lines[i] = "  " + lines[i];
                }

                if (i == 0)
                {
                    shiftFirst = lines[i].Length - before;
                }
            }

            var replaced = string.Join(newline, lines);
            var result = text.Substring(0, first) + replaced + text.Substring(last);
            if (length == 0)
            {
                return new EditResult(result, Math.Max(first, start + shiftFirst), 0);
            }

            return new EditResult(result, first, replaced.Length);
        }

        /// <summary>
        /// İmlecin önünde açık bir öneri yazımı var mı: <c>@</c> (satır başında ya da boşluktan sonra) veya <c>[[</c>; sorgu boşluk
        /// içerebilir (en çok 40 karakter, satır sonu ve <c>]]</c> yok). Yoksa <c>null</c>.
        /// </summary>
        public static SuggestQuery? FindQuery(string text, int caret)
        {
            text = text ?? string.Empty;
            caret = Clamp(caret, 0, text.Length);
            var limit = Math.Max(0, caret - 42);
            for (var i = caret - 1; i >= limit; i--)
            {
                var c = text[i];
                if (c == '\n' || c == '\r' || c == ']')
                {
                    return null;
                }

                if (c == '[' && i > 0 && text[i - 1] == '[')
                {
                    return new SuggestQuery { Start = i - 1, Text = text.Substring(i + 1, caret - i - 1), IsLink = true };
                }

                if (c == '@' && (i == 0 || char.IsWhiteSpace(text[i - 1]) || text[i - 1] == '('))
                {
                    var query = text.Substring(i + 1, caret - i - 1);
                    if (query.Contains("  "))
                    {
                        return null;
                    }

                    return new SuggestQuery { Start = i, Text = query, IsLink = false };
                }
            }

            return null;
        }

        /// <summary>
        /// Öneriyi yerleştirir: tetikleyiciden imlece kadar olan yazı silinir, yerine <paramref name="insert"/> konur; imleç sonuna.
        /// <c>[[</c> ile açıldıysa ve sonra <c>]]</c> geliyorsa çift kapanış eklenmez.
        /// </summary>
        public static EditResult ApplySuggestion(string text, int caret, SuggestQuery query, string insert)
        {
            text = text ?? string.Empty;
            caret = Clamp(caret, query.Start, text.Length);
            var end = caret;
            if (query.IsLink && insert.EndsWith("]]", StringComparison.Ordinal) && end + 2 <= text.Length && text.Substring(end, 2) == "]]")
            {
                end += 2;
            }

            var result = text.Substring(0, query.Start) + insert + text.Substring(end);
            return new EditResult(result, query.Start + insert.Length, 0);
        }

        /// <summary>İmlecin satır ve sütunu (1'den).</summary>
        public static Tuple<int, int> LineColumn(string text, int caret)
        {
            text = text ?? string.Empty;
            caret = Clamp(caret, 0, text.Length);
            var line = 1;
            var lineStart = 0;
            for (var i = 0; i < caret; i++)
            {
                if (text[i] == '\n')
                {
                    line++;
                    lineStart = i + 1;
                }
            }

            return Tuple.Create(line, caret - lineStart + 1);
        }

        private static int LineStart(string text, int index)
        {
            if (index <= 0 || text.Length == 0)
            {
                return 0;
            }

            return text.LastIndexOf('\n', Math.Min(index, text.Length) - 1) + 1;
        }

        private static int LineEnd(string text, int index)
        {
            var i = text.IndexOf('\n', Math.Min(index, text.Length));
            if (i < 0)
            {
                return text.Length;
            }

            return i > 0 && text[i - 1] == '\r' ? i - 1 : i;
        }

        private static string NewlineOf(string text)
        {
            return text.Contains("\r\n") ? "\r\n" : text.Contains("\n") ? "\n" : "\r\n";
        }

        private static int Clamp(int value, int min, int max)
        {
            return value < min ? min : value > max ? max : value;
        }
    }
}
