using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            return Wrap(text, start, length, mark, mark);
        }

        /// <summary>
        /// Seçimi açılış ve kapanış işaretiyle sarar (ör. yorum: <c>&lt;!-- </c> … <c> --&gt;</c>); seçim zaten sarılıysa işaretler
        /// kaldırılır. Seçim boşsa işaretler eklenir ve imleç arasına konur.
        /// </summary>
        public static EditResult Wrap(string text, int start, int length, string open, string close)
        {
            text = text ?? string.Empty;
            start = Clamp(start, 0, text.Length);
            length = Clamp(length, 0, text.Length - start);
            var o = open.Length;
            var c = close.Length;
            if (start >= o && start + length + c <= text.Length && text.Substring(start - o, o) == open && text.Substring(start + length, c) == close)
            {
                var removed = text.Remove(start + length, c).Remove(start - o, o);
                return new EditResult(removed, start - o, length);
            }

            var selected = text.Substring(start, length);
            if (selected.Length >= o + c && selected.StartsWith(open, StringComparison.Ordinal) && selected.EndsWith(close, StringComparison.Ordinal))
            {
                var inner = selected.Substring(o, selected.Length - o - c);
                return new EditResult(text.Substring(0, start) + inner + text.Substring(start + length), start, inner.Length);
            }

            var result = text.Substring(0, start) + open + selected + close + text.Substring(start + length);
            return new EditResult(result, start + o, length);
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
        /// Etiket sorgusu: imleçten geriye, kelime başındaki <c>#</c>'a kadar (harf, rakam, <c>_</c>, <c>-</c>). Yoksa <c>null</c>.
        /// </summary>
        public static SuggestQuery? FindTagQuery(string text, int caret)
        {
            text = text ?? string.Empty;
            caret = Clamp(caret, 0, text.Length);
            for (var i = caret - 1; i >= 0 && caret - i <= 40; i--)
            {
                var c = text[i];
                if (c == '#')
                {
                    return i == 0 || char.IsWhiteSpace(text[i - 1]) || text[i - 1] == ','
                        ? new SuggestQuery { Start = i, Text = text.Substring(i + 1, caret - i - 1), IsLink = false }
                        : null;
                }

                if (!char.IsLetterOrDigit(c) && c != '_' && c != '-')
                {
                    return null;
                }
            }

            return null;
        }

        /// <summary>Metindeki etiketler: <c>#a #b, c</c> → <c>a, b, c</c> (<c>#</c>'sız, sırayla, tekrar yok).</summary>
        public static List<string> ParseTags(string text)
        {
            return (text ?? string.Empty).Split(new[] { ' ', ',', ';', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim().TrimStart('#'))
                .Where(t => t.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>Etiket listesini kutuda gösterilecek metne çevirir: <c>#a #b</c>.</summary>
        public static string FormatTags(IEnumerable<string> tags)
        {
            return string.Join(" ", (tags ?? Enumerable.Empty<string>()).Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => "#" + t.Trim().TrimStart('#')));
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

        private const string Openers = "([{\"`";
        private const string Closers = ")]}\"`";
        private const string WrapOnly = "*_~=";
        private static readonly Regex PlainUrl = new Regex(@"^(?:https?://|mailto:|file:///)\S+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        /// <summary>Seçimin kapsadığı satırları bir satır yukarı / aşağı taşır (Alt+↑ / Alt+↓). Kenardaysa <c>null</c>.</summary>
        public static EditResult? MoveLines(string text, int start, int length, bool up)
        {
            text = text ?? string.Empty;
            int blockStart, blockEnd;
            LineBlock(text, start, length, out blockStart, out blockEnd);
            var block = text.Substring(blockStart, blockEnd - blockStart);
            if (up)
            {
                if (blockStart == 0)
                {
                    return null;
                }

                var prevEnd = blockStart - (blockStart >= 2 && text[blockStart - 2] == '\r' ? 2 : 1);
                var prevStart = LineStart(text, prevEnd);
                var prev = text.Substring(prevStart, prevEnd - prevStart);
                var sep = text.Substring(prevEnd, blockStart - prevEnd);
                var moved = text.Substring(0, prevStart) + block + sep + prev + text.Substring(blockEnd);
                return new EditResult(moved, start - prev.Length - sep.Length, length);
            }

            if (blockEnd >= text.Length)
            {
                return null;
            }

            var nextStart = blockEnd + (text[blockEnd] == '\r' ? 2 : 1);
            var nextEnd = LineEnd(text, nextStart);
            var next = text.Substring(nextStart, nextEnd - nextStart);
            var gap = text.Substring(blockEnd, nextStart - blockEnd);
            var result = text.Substring(0, blockStart) + next + gap + block + text.Substring(nextEnd);
            return new EditResult(result, start + next.Length + gap.Length, length);
        }

        /// <summary>Seçimin kapsadığı satırları altına çoğaltır (Ctrl+D); seçim kopyaya geçer.</summary>
        public static EditResult DuplicateLines(string text, int start, int length)
        {
            text = text ?? string.Empty;
            int blockStart, blockEnd;
            LineBlock(text, start, length, out blockStart, out blockEnd);
            var block = text.Substring(blockStart, blockEnd - blockStart);
            var newline = NewlineOf(text);
            var result = text.Substring(0, blockEnd) + newline + block + text.Substring(blockEnd);
            return new EditResult(result, start + block.Length + newline.Length, length);
        }

        /// <summary>Seçimin kapsadığı satırları siler (Ctrl+Shift+K); imleç aynı yerdeki satırın başına gelir.</summary>
        public static EditResult DeleteLines(string text, int start, int length)
        {
            text = text ?? string.Empty;
            int blockStart, blockEnd;
            LineBlock(text, start, length, out blockStart, out blockEnd);
            int from = blockStart, to = blockEnd;
            if (to < text.Length)
            {
                to += text[to] == '\r' ? 2 : 1;
            }
            else if (from > 0)
            {
                from -= from >= 2 && text[from - 2] == '\r' ? 2 : 1;
            }

            var result = text.Remove(from, to - from);
            return new EditResult(result, Math.Min(LineStart(result, Math.Min(from, result.Length)), result.Length), 0);
        }

        /// <summary>
        /// Yazılan karakter için otomatik çift: <c>( [ { " `</c> kapanışıyla eklenir (imleç arada; yalnız önü boş ya da kapanışsa;
        /// tırnak ve ters tırnak harften sonra açılmaz), seçim varsa sarılır. <c>* _ ~ =</c> yalnız seçimi sarar. Kapanış
        /// karakteri yazılırken sağda zaten varsa üzerinden geçilir. Tek tırnak çiftlenmez (Türkçe kesme işareti). İş
        /// yoksa <c>null</c>.
        /// </summary>
        public static EditResult? AutoPair(string text, int start, int length, char typed)
        {
            text = text ?? string.Empty;
            start = Clamp(start, 0, text.Length);
            length = Clamp(length, 0, text.Length - start);
            var open = Openers.IndexOf(typed);
            if (length > 0)
            {
                if (open < 0 && WrapOnly.IndexOf(typed) < 0)
                {
                    return null;
                }

                var close = open >= 0 ? Closers[open] : typed;
                var selected = text.Substring(start, length);
                return new EditResult(text.Substring(0, start) + typed + selected + close + text.Substring(start + length), start + 1, length);
            }

            var next = start < text.Length ? text[start] : '\0';
            if (Closers.IndexOf(typed) >= 0 && next == typed)
            {
                return new EditResult(text, start + 1, 0);
            }

            if (open < 0)
            {
                return null;
            }

            var free = next == '\0' || char.IsWhiteSpace(next) || Closers.IndexOf(next) >= 0 || ",.;:!?".IndexOf(next) >= 0;
            var prev = start > 0 ? text[start - 1] : '\0';
            if (!free || (typed == '"' || typed == '`') && (char.IsLetterOrDigit(prev) || prev == typed))
            {
                return null;
            }

            return new EditResult(text.Insert(start, typed.ToString() + Closers[open]), start + 1, 0);
        }

        /// <summary>İmleç boş bir çiftin arasındaysa (<c>(|)</c>, <c>[|]</c> …) Backspace ikisini birden siler; değilse <c>null</c>.</summary>
        public static EditResult? DeletePair(string text, int caret)
        {
            text = text ?? string.Empty;
            if (caret <= 0 || caret >= text.Length)
            {
                return null;
            }

            var open = Openers.IndexOf(text[caret - 1]);
            if (open < 0 || text[caret] != Closers[open])
            {
                return null;
            }

            return new EditResult(text.Remove(caret - 1, 2), caret - 1, 0);
        }

        /// <summary>
        /// Seçili metnin üzerine bir web adresi yapıştırılınca bağlantı olur: <c>[seçim](adres)</c>. Seçim yoksa, birden çok
        /// satırsa ya da kendisi adresse ya da panodaki bir adres değilse <c>null</c> (normal yapıştırma).
        /// </summary>
        public static EditResult? PasteLink(string text, int start, int length, string clipboard)
        {
            text = text ?? string.Empty;
            var url = (clipboard ?? string.Empty).Trim();
            if (length <= 0 || start < 0 || start + length > text.Length || !PlainUrl.IsMatch(url))
            {
                return null;
            }

            var selected = text.Substring(start, length);
            if (selected.IndexOf('\n') >= 0 || PlainUrl.IsMatch(selected.Trim()))
            {
                return null;
            }

            var link = "[" + selected + "](" + url + ")";
            return new EditResult(text.Substring(0, start) + link + text.Substring(start + length), start + link.Length, 0);
        }

        /// <summary>İmleç bir md tablosunun (başı <c>|</c> olan satırlar, ikinci satırı <c>|---|</c>) içinde mi.</summary>
        public static bool IsInTable(string text, int caret)
        {
            return FindTable(text ?? string.Empty, caret) != null;
        }

        /// <summary>
        /// İmlecin bulunduğu tabloyu hizalar (Ctrl+Shift+T): hücreler kırpılır, sütunlar en geniş hücreye göre doldurulur,
        /// hizalama (<c>:---</c>, <c>---:</c>, <c>:---:</c>) korunur, eksik hücreler eklenir. İmleç aynı hücrenin başına
        /// gelir. Tablo yoksa <c>null</c>.
        /// </summary>
        public static EditResult? FormatTable(string text, int caret)
        {
            return TableEdit(text, caret, 0);
        }

        /// <summary>
        /// Tablo içinde Tab / Shift+Tab: tablo hizalanır, sonraki / önceki hücrenin içeriği seçilir (ayraç satırı atlanır).
        /// Son hücrede Tab yeni boş satır açar. Tablo yoksa <c>null</c>.
        /// </summary>
        public static EditResult? TableNextCell(string text, int caret, bool backwards)
        {
            return TableEdit(text, caret, backwards ? -1 : 1);
        }

        private sealed class TableBlock
        {
            public int Start;
            public int End;
            public List<string> Lines = new List<string>();
            public int CaretRow;
            public int CaretCell;
        }

        private static TableBlock? FindTable(string text, int caret)
        {
            caret = Clamp(caret, 0, text.Length);
            var lineStart = LineStart(text, caret);
            if (!IsTableLine(text, lineStart))
            {
                return null;
            }

            var start = lineStart;
            while (start > 0)
            {
                var previous = LineStart(text, start - 1);
                if (!IsTableLine(text, previous))
                {
                    break;
                }

                start = previous;
            }

            var block = new TableBlock { Start = start };
            var position = start;
            while (true)
            {
                var end = LineEnd(text, position);
                if (position == lineStart)
                {
                    block.CaretRow = block.Lines.Count;
                    block.CaretCell = Math.Max(0, CountPipes(text.Substring(position, caret - position)) - 1);
                }

                block.Lines.Add(text.Substring(position, end - position));
                block.End = end;
                if (end >= text.Length)
                {
                    break;
                }

                var next = end + (text[end] == '\r' ? 2 : 1);
                if (next >= text.Length || !IsTableLine(text, next))
                {
                    break;
                }

                position = next;
            }

            if (block.Lines.Count < 2 || !SplitCells(block.Lines[1]).All(c => Regex.IsMatch(c.Trim(), @"^:?-+:?$")))
            {
                return null;
            }

            return block;
        }

        private static EditResult? TableEdit(string text, int caret, int step)
        {
            text = text ?? string.Empty;
            var table = FindTable(text, caret);
            if (table == null)
            {
                return null;
            }

            var rows = table.Lines.Select(SplitCells).Select(r => r.Select(c => c.Trim()).ToList()).ToList();
            var columns = rows.Max(r => r.Count);
            foreach (var row in rows)
            {
                while (row.Count < columns)
                {
                    row.Add(string.Empty);
                }
            }

            var align = rows[1].Select(c => c.StartsWith(":", StringComparison.Ordinal) && c.EndsWith(":", StringComparison.Ordinal) && c.Length > 1 ? 'c'
                : c.EndsWith(":", StringComparison.Ordinal) ? 'r' : c.StartsWith(":", StringComparison.Ordinal) ? 'L' : 'l').ToList();

            // Hedef hücre: Tab ile ilerlenir, ayraç satırı atlanır; sondaysa yeni satır.
            var targetRow = Math.Max(0, table.CaretRow);
            var targetCell = Math.Min(table.CaretCell, columns - 1);
            if (targetRow == 1)
            {
                targetRow = 2 < rows.Count ? 2 : 0;
                targetCell = 0;
            }
            else if (step > 0)
            {
                targetCell++;
                if (targetCell >= columns)
                {
                    targetCell = 0;
                    targetRow = targetRow == 0 ? 2 : targetRow + 1;
                    if (targetRow >= rows.Count)
                    {
                        rows.Add(Enumerable.Repeat(string.Empty, columns).ToList());
                    }
                }
            }
            else if (step < 0)
            {
                targetCell--;
                if (targetCell < 0)
                {
                    if (targetRow == 0)
                    {
                        targetCell = 0;
                    }
                    else
                    {
                        targetRow = targetRow == 2 ? 0 : targetRow - 1;
                        targetCell = columns - 1;
                    }
                }
            }

            var widths = Enumerable.Range(0, columns).Select(c => Math.Max(3, rows.Where((r, i) => i != 1).Max(r => r[c].Length))).ToList();
            var newline = NewlineOf(text);
            var builder = new StringBuilder();
            int selectStart = 0, selectLength = 0;
            for (var r = 0; r < rows.Count; r++)
            {
                if (r > 0)
                {
                    builder.Append(newline);
                }

                builder.Append('|');
                for (var c = 0; c < columns; c++)
                {
                    builder.Append(' ');
                    var cell = r == 1 ? Delimiter(align[c], widths[c]) : Pad(rows[r][c], widths[c], align[c]);
                    if (r == targetRow && c == targetCell)
                    {
                        var lead = cell.Length - cell.TrimStart().Length;
                        selectStart = table.Start + builder.Length + (rows[r][c].Length == 0 ? 0 : lead);
                        selectLength = rows[r][c].Length;
                    }

                    builder.Append(cell).Append(" |");
                }
            }

            var result = text.Substring(0, table.Start) + builder + text.Substring(table.End);
            return new EditResult(result, selectStart, step == 0 ? 0 : selectLength);
        }

        private static bool IsTableLine(string text, int lineStart)
        {
            var end = LineEnd(text, lineStart);
            return end > lineStart && text.Substring(lineStart, end - lineStart).TrimStart().StartsWith("|", StringComparison.Ordinal);
        }

        // Hücreler: baştaki ve sondaki | atılır; \| ve `kod` içindeki | ayırmaz.
        private static List<string> SplitCells(string line)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("|", StringComparison.Ordinal))
            {
                trimmed = trimmed.Substring(1);
            }

            if (trimmed.EndsWith("|", StringComparison.Ordinal) && !trimmed.EndsWith("\\|", StringComparison.Ordinal))
            {
                trimmed = trimmed.Substring(0, trimmed.Length - 1);
            }

            var cells = new List<string>();
            var current = new StringBuilder();
            var inCode = false;
            for (var i = 0; i < trimmed.Length; i++)
            {
                var ch = trimmed[i];
                if (ch == '`')
                {
                    inCode = !inCode;
                }

                if (ch == '|' && !inCode && (i == 0 || trimmed[i - 1] != '\\'))
                {
                    cells.Add(current.ToString());
                    current.Clear();
                    continue;
                }

                current.Append(ch);
            }

            cells.Add(current.ToString());
            return cells;
        }

        // İmleçten önceki ayırıcı | sayısı (hücre sırası için).
        private static int CountPipes(string before)
        {
            var count = 0;
            var inCode = false;
            for (var i = 0; i < before.Length; i++)
            {
                if (before[i] == '`')
                {
                    inCode = !inCode;
                }
                else if (before[i] == '|' && !inCode && (i == 0 || before[i - 1] != '\\'))
                {
                    count++;
                }
            }

            return before.TrimStart().StartsWith("|", StringComparison.Ordinal) ? count : count + 1;
        }

        private static string Pad(string cell, int width, char align)
        {
            var space = width - cell.Length;
            if (space <= 0)
            {
                return cell;
            }

            if (align == 'r')
            {
                return new string(' ', space) + cell;
            }

            if (align == 'c')
            {
                var left = space / 2;
                return new string(' ', left) + cell + new string(' ', space - left);
            }

            return cell + new string(' ', space);
        }

        private static string Delimiter(char align, int width)
        {
            switch (align)
            {
                case 'c':
                    return ":" + new string('-', width - 2) + ":";
                case 'r':
                    return new string('-', width - 1) + ":";
                case 'L':
                    return ":" + new string('-', width - 1);
                default:
                    return new string('-', width);
            }
        }

        // Seçimin kapsadığı tam satırlar (satır sonu hariç). Seçim bir satırın başında bitiyorsa o satır sayılmaz.
        private static void LineBlock(string text, int start, int length, out int blockStart, out int blockEnd)
        {
            start = Clamp(start, 0, text.Length);
            length = Clamp(length, 0, text.Length - start);
            var end = start + length;
            if (length > 0 && text[end - 1] == '\n')
            {
                end--;
            }

            blockStart = LineStart(text, start);
            blockEnd = Math.Max(blockStart, LineEnd(text, Math.Max(blockStart, end)));
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
