using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace Pusula.Wpf
{
    /// <summary>
    /// md düzenleyicisinde renklendirme (Obsidian'a yakın): başlık, kalın / italik / üstü çizili / vurgu, satır içi kod
    /// ve kod bloğu, [[bağlantı]] ve [metin](adres), #etiket, @kişi, alıntı ve uyarı kutusu, liste ve görev işaretleri,
    /// tablo çizgileri, %% yorum %%, YAML başlığı. Renkler temanın fırçalarından okunur (koyu / açık temada uyumlu).
    /// </summary>
    public sealed class MarkdownColorizer : DocumentColorizingTransformer
    {
        private const int Normal = 0;
        private const int FrontMatter = 1;
        private const int Code = 2;

        private static readonly Regex Heading = new Regex(@"^(#{1,6})\s", RegexOptions.Compiled);
        private static readonly Regex Quote = new Regex(@"^\s*(>\s?)+", RegexOptions.Compiled);
        private static readonly Regex Callout = new Regex(@"\[![\w-]+\][+-]?", RegexOptions.Compiled);
        private static readonly Regex ListMark = new Regex(@"^\s*([-*+]|\d+[.)])\s", RegexOptions.Compiled);
        private static readonly Regex TaskBox = new Regex(@"^\s*[-*+]\s+(\[[ xX]\])", RegexOptions.Compiled);
        private static readonly Regex Bold = new Regex(@"(\*\*|__)(?=\S)(.+?)(?<=\S)\1", RegexOptions.Compiled);
        private static readonly Regex Italic = new Regex(@"(?<![*\w])([*_])(?=[^\s*_])([^*_]+?)(?<=\S)\1(?![*\w])", RegexOptions.Compiled);
        private static readonly Regex Strike = new Regex(@"~~(?=\S).+?(?<=\S)~~", RegexOptions.Compiled);
        private static readonly Regex Highlight = new Regex(@"==(?=\S).+?(?<=\S)==", RegexOptions.Compiled);
        private static readonly Regex InlineCode = new Regex(@"`[^`]+`", RegexOptions.Compiled);
        private static readonly Regex WikiLink = new Regex(@"!?\[\[[^\]]+\]\]", RegexOptions.Compiled);
        private static readonly Regex MdLink = new Regex(@"!?\[[^\]]*\]\([^)]+\)", RegexOptions.Compiled);
        private static readonly Regex Tag = new Regex(@"(?<=^|\s)#[\p{L}\p{N}_/-]+", RegexOptions.Compiled);
        private static readonly Regex Mention = new Regex(@"(?<=^|\s)@[\p{L}\p{N}_.-]+", RegexOptions.Compiled);
        private static readonly Regex Comment = new Regex(@"%%.*?%%|<!--.*?-->", RegexOptions.Compiled);
        private static readonly Regex TaskDate = new Regex("(\uD83D\uDCC5|\u2705)\\s*\\d{4}-\\d{2}-\\d{2}", RegexOptions.Compiled);
        private static readonly Regex Pipe = new Regex(@"\|", RegexOptions.Compiled);
        private static readonly Regex YamlKey = new Regex(@"^\s*[\w-]+(?=:)", RegexOptions.Compiled);

        private ITextSourceVersion? version;
        private TextDocument? document;
        private int[] kinds = new int[0];

        /// <summary>Satırı renklendirir.</summary>
        protected override void ColorizeLine(DocumentLine line)
        {
            var doc = CurrentContext.Document;
            var kind = KindOf(doc, line.LineNumber);
            var text = doc.GetText(line);
            var start = line.Offset;
            if (text.Length == 0)
            {
                return;
            }

            if (kind == Code)
            {
                Paint(start, text.Length, "CodeNumberBrush", false, false);
                return;
            }

            if (kind == FrontMatter)
            {
                Paint(start, text.Length, "TextTertiaryBrush", false, false);
                var key = YamlKey.Match(text);
                if (key.Success)
                {
                    Paint(start + key.Index, key.Length, "CodeKeyBrush", false, false);
                }

                return;
            }

            var heading = Heading.Match(text);
            if (heading.Success)
            {
                Paint(start, text.Length, "AccentTextFillColorPrimaryBrush", true, false);
                Paint(start, heading.Groups[1].Length, "CodePunctBrush", true, false);
            }

            var quote = Quote.Match(text);
            if (quote.Success)
            {
                Paint(start, text.Length, "TextSecondaryBrush", false, false);
                Paint(start + quote.Index, quote.Length, "CodePunctBrush", false, false);
                var callout = Callout.Match(text);
                if (callout.Success)
                {
                    Paint(start + callout.Index, callout.Length, "SystemFillColorCautionBrush", true, false);
                }
            }

            var task = TaskBox.Match(text);
            if (task.Success)
            {
                var box = task.Groups[1];
                if (box.Value.IndexOf('x') >= 0 || box.Value.IndexOf('X') >= 0)
                {
                    Paint(start + box.Index, text.Length - box.Index, "TextTertiaryBrush", false, false);
                }

                Paint(start + box.Index, box.Length, "CodePunctBrush", true, false);
            }

            var list = ListMark.Match(text);
            if (list.Success)
            {
                Paint(start + list.Groups[1].Index, list.Groups[1].Length, "CodePunctBrush", true, false);
            }

            if (text.TrimStart().StartsWith("|", StringComparison.Ordinal))
            {
                foreach (Match m in Pipe.Matches(text))
                {
                    Paint(start + m.Index, m.Length, "CodePunctBrush", false, false);
                }
            }

            foreach (Match m in Bold.Matches(text))
            {
                PaintStyle(start + m.Index, m.Length, true, false);
                Paint(start + m.Index, 2, "CodePunctBrush", true, false);
                Paint(start + m.Index + m.Length - 2, 2, "CodePunctBrush", true, false);
            }

            foreach (Match m in Italic.Matches(text))
            {
                PaintStyle(start + m.Index, m.Length, false, true);
            }

            foreach (Match m in Strike.Matches(text))
            {
                Paint(start + m.Index, m.Length, "TextTertiaryBrush", false, false);
            }

            foreach (Match m in Highlight.Matches(text))
            {
                Paint(start + m.Index, m.Length, "SystemFillColorCautionBrush", false, false);
            }

            foreach (Match m in MdLink.Matches(text))
            {
                Paint(start + m.Index, m.Length, "CodeStringBrush", false, false);
            }

            foreach (Match m in WikiLink.Matches(text))
            {
                Paint(start + m.Index, m.Length, "CodeStringBrush", false, false);
            }

            if (!heading.Success)
            {
                foreach (Match m in Tag.Matches(text))
                {
                    Paint(start + m.Index, m.Length, "CodeKeyBrush", false, false);
                }
            }

            foreach (Match m in Mention.Matches(text))
            {
                Paint(start + m.Index, m.Length, "CodeKeyBrush", false, false);
            }

            foreach (Match m in TaskDate.Matches(text))
            {
                Paint(start + m.Index, m.Length, "TextTertiaryBrush", false, false);
            }

            foreach (Match m in InlineCode.Matches(text))
            {
                Paint(start + m.Index, m.Length, "CodeNumberBrush", false, false);
            }

            foreach (Match m in Comment.Matches(text))
            {
                Paint(start + m.Index, m.Length, "TextTertiaryBrush", false, true);
            }
        }

        // Satırların türü (YAML başlığı / kod bloğu / normal) belge değiştikçe bir kez baştan hesaplanır.
        private int KindOf(TextDocument doc, int lineNumber)
        {
            var current = doc.Version;
            if (!ReferenceEquals(doc, document) || version == null || current == null || !version.BelongsToSameDocumentAs(current) || version.CompareAge(current) != 0)
            {
                document = doc;
                version = current;
                kinds = new int[doc.LineCount + 1];
                var inCode = false;
                var inFront = false;
                foreach (var line in doc.Lines)
                {
                    var trimmed = doc.GetText(line).Trim();
                    if (line.LineNumber == 1 && trimmed == "---")
                    {
                        inFront = true;
                        kinds[1] = FrontMatter;
                        continue;
                    }

                    if (inFront)
                    {
                        kinds[line.LineNumber] = FrontMatter;
                        if (trimmed == "---")
                        {
                            inFront = false;
                        }

                        continue;
                    }

                    if (trimmed.StartsWith("```", StringComparison.Ordinal) || trimmed.StartsWith("~~~", StringComparison.Ordinal))
                    {
                        kinds[line.LineNumber] = Code;
                        inCode = !inCode;
                        continue;
                    }

                    kinds[line.LineNumber] = inCode ? Code : Normal;
                }
            }

            return lineNumber < kinds.Length ? kinds[lineNumber] : Normal;
        }

        private void Paint(int offset, int length, string brushKey, bool bold, bool italic)
        {
            var brush = Application.Current?.TryFindResource(brushKey) as Brush;
            if (length <= 0)
            {
                return;
            }

            ChangeLinePart(offset, offset + length, element =>
            {
                if (brush != null)
                {
                    element.TextRunProperties.SetForegroundBrush(brush);
                }

                if (bold || italic)
                {
                    var face = element.TextRunProperties.Typeface;
                    element.TextRunProperties.SetTypeface(new Typeface(face.FontFamily, italic ? FontStyles.Italic : face.Style, bold ? FontWeights.SemiBold : face.Weight, face.Stretch));
                }
            });
        }

        private void PaintStyle(int offset, int length, bool bold, bool italic)
        {
            ChangeLinePart(offset, offset + length, element =>
            {
                var face = element.TextRunProperties.Typeface;
                element.TextRunProperties.SetTypeface(new Typeface(face.FontFamily, italic ? FontStyles.Italic : face.Style, bold ? FontWeights.Bold : face.Weight, face.Stretch));
            });
        }
    }
}