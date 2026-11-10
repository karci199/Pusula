using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Markdig.Extensions.EmphasisExtras;
using Markdig.Extensions.Footnotes;
using Markdig.Extensions.Tables;
using Markdig.Extensions.TaskLists;
using Markdig.Extensions.Yaml;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Pusula.Core.Markdown;
using MdBlock = Markdig.Syntax.Block;
using MdInline = Markdig.Syntax.Inlines.Inline;
using WpfBlock = System.Windows.Documents.Block;
using WpfInline = System.Windows.Documents.Inline;
using WpfTable = System.Windows.Documents.Table;

namespace Pusula.Wpf.Markdown
{
    /// <summary>Görünümün ayarları ve tıklamalarda çağrılacaklar.</summary>
    public sealed class MarkdownRenderContext
    {
        private Dictionary<string, string>? nameIndex;

        /// <summary>Notun klasörü (göreli resim yolları için).</summary>
        public string NoteFolder { get; set; } = string.Empty;

        /// <summary>Kök (adla resim araması için).</summary>
        public string Root { get; set; } = string.Empty;

        /// <summary>Ayrıştırılan metin (kutu başlıkları için).</summary>
        public string Source { get; set; } = string.Empty;

        /// <summary>Gövde yazı boyutu.</summary>
        public double FontSize { get; set; } = 15;

        /// <summary>Gövde yazı tipi.</summary>
        public string FontFamily { get; set; } = "Segoe UI";

        /// <summary>Kod yazı tipi.</summary>
        public string CodeFontFamily { get; set; } = "Cascadia Mono, Consolas";

        /// <summary>Tek satır sonu satır sonu olarak gösterilsin mi (Obsidian varsayılanı gibi).</summary>
        public bool KeepLineBreaks { get; set; } = true;

        /// <summary>YAML başlığı özellik kutusu olarak gösterilsin mi.</summary>
        public bool ShowFrontMatter { get; set; } = true;

        /// <summary>Resimlerin en büyük genişliği.</summary>
        public double MaxImageWidth { get; set; } = 900;

        /// <summary><c>[[hedef]]</c> tıklandı.</summary>
        public Action<string>? WikiLinkClicked { get; set; }

        /// <summary>Web ya da dosya bağlantısı tıklandı.</summary>
        public Action<string>? UrlClicked { get; set; }

        /// <summary>Görev kutusu tıklandı (satır, 0'dan).</summary>
        public Action<int>? TaskToggled { get; set; }

        /// <summary>Resim ya da gömülü dosya tıklandı (tam yol).</summary>
        public Action<string>? FileClicked { get; set; }

        /// <summary>Çizimde bulunamayan dosyalar (bilgi / test için).</summary>
        public List<string> MissingFiles { get; } = new List<string>();

        /// <summary>Çizimde bulunan resimler (tam yol).</summary>
        public List<string> Images { get; } = new List<string>();

        /// <summary>Dosyayı bulur (ad dizini ilk adla aramada bir kez kurulur).</summary>
        public string Resolve(string target)
        {
            var direct = MarkdownNotes.ResolveFile(NoteFolder, Root, target, nameIndex ?? (nameIndex = MarkdownNotes.BuildNameIndex(Root.Length > 0 ? Root : NoteFolder)));
            return direct;
        }
    }

    /// <summary>
    /// md ağacını (Markdig) WPF <see cref="FlowDocument"/>'e çizer. Renkler DesignKit kaynaklarına bağlanır; tema değişince uyar.
    /// Desteklenen: başlık, paragraf, liste (iç içe, numaralı, görev kutulu), alıntı ve Obsidian kutuları, kod, tablo, ayraç,
    /// YAML özellikleri, resim (<c>![](…)</c>, <c>![[…]]</c>), bağlantı (<c>[[…]]</c>, web), <c>#etiket</c>, vurgu, dipnot.
    /// </summary>
    public sealed class MarkdownRenderer
    {
        private static readonly Regex Tag = new Regex(@"(?<=^|\s)#[\p{L}\p{N}_/-]*\p{L}[\p{L}\p{N}_/-]*", RegexOptions.CultureInvariant);

        private static readonly double[] HeadingScale = { 1.9, 1.55, 1.3, 1.15, 1.05, 1.0 };

        private readonly MarkdownRenderContext context;

        private MarkdownRenderer(MarkdownRenderContext context)
        {
            this.context = context;
        }

        /// <summary>Metni ayrıştırıp çizer.</summary>
        public static FlowDocument Render(string text, MarkdownRenderContext context)
        {
            // %% yorumlar %% görünümde gizlenir; örtme uzunluğu korur, konumlar düz metinle aynı kalır.
            context.Source = MarkdownNotes.MaskComments(text ?? string.Empty);
            return new MarkdownRenderer(context).RenderDocument(MarkdownNotes.Parse(context.Source));
        }

        private FlowDocument RenderDocument(MarkdownDocument document)
        {
            var flow = new FlowDocument
            {
                FontSize = context.FontSize,
                FontFamily = new FontFamily(context.FontFamily),
                PagePadding = new Thickness(0),
                LineHeight = context.FontSize * 1.55,
                TextAlignment = TextAlignment.Left,
                IsHyphenationEnabled = false,
            };
            flow.SetResourceReference(TextElement.ForegroundProperty, "TextPrimaryBrush");
            foreach (var block in document)
            {
                var rendered = RenderBlock(block);
                if (rendered != null)
                {
                    flow.Blocks.Add(rendered);
                }
            }

            return flow;
        }

        private WpfBlock? RenderBlock(MdBlock block)
        {
            switch (block)
            {
                case YamlFrontMatterBlock yaml:
                    return context.ShowFrontMatter ? RenderFrontMatter(yaml) : null;
                case HeadingBlock heading:
                    return RenderHeading(heading);
                case ParagraphBlock paragraph:
                    return RenderParagraph(paragraph);
                case ListBlock list:
                    return RenderList(list);
                case QuoteBlock quote:
                    return RenderQuote(quote);
                case CodeBlock code:
                    return RenderCode(code);
                case ThematicBreakBlock _:
                    return Divider(new Thickness(0, 12, 0, 12));
                case Markdig.Extensions.Tables.Table table:
                    return RenderTable(table);
                case HtmlBlock html:
                    // <!-- yorum --> görünümde gizlenir (Obsidian gibi).
                    return html.Type == HtmlBlockType.Comment ? null : RenderRaw(html.Lines.ToString());
                case FootnoteGroup footnotes:
                    return RenderFootnotes(footnotes);
                case LinkReferenceDefinitionGroup _:
                    return null;
                case ContainerBlock container:
                    var section = new Section();
                    AddChildren(section.Blocks, container);
                    return section;
                default:
                    return null;
            }
        }

        private void AddChildren(BlockCollection target, ContainerBlock container, int skip = 0)
        {
            foreach (var child in container.Skip(skip))
            {
                var rendered = RenderBlock(child);
                if (rendered != null)
                {
                    target.Add(rendered);
                }
            }
        }

        private WpfBlock RenderFrontMatter(YamlFrontMatterBlock yaml)
        {
            var grid = new Grid { Margin = new Thickness(0, 0, 0, 4) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var row = 0;
            foreach (var raw in yaml.Lines.ToString().Replace("\r\n", "\n").Split('\n'))
            {
                var line = raw.TrimEnd();
                var colon = line.IndexOf(':');
                if (line.Trim().Length == 0 || line.Trim() == "---" || colon <= 0)
                {
                    continue;
                }

                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                var key = new TextBlock { Text = line.Substring(0, colon).Trim(), Margin = new Thickness(0, 2, 16, 2), FontSize = context.FontSize * 0.85 };
                key.SetResourceReference(TextBlock.ForegroundProperty, "TextSecondaryBrush");
                var value = new TextBlock { Text = line.Substring(colon + 1).Trim().Trim('"'), Margin = new Thickness(0, 2, 0, 2), FontSize = context.FontSize * 0.85, TextWrapping = TextWrapping.Wrap };
                Grid.SetRow(key, row);
                Grid.SetRow(value, row);
                Grid.SetColumn(value, 1);
                grid.Children.Add(key);
                grid.Children.Add(value);
                row++;
            }

            var border = new Border { Child = grid, Padding = new Thickness(12, 8, 12, 8), CornerRadius = new CornerRadius(6), BorderThickness = new Thickness(1) };
            border.SetResourceReference(Border.BackgroundProperty, "SubtleFillColorSecondaryBrush");
            border.SetResourceReference(Border.BorderBrushProperty, "CardStrokeColorDefaultBrush");
            return new BlockUIContainer(border) { Margin = new Thickness(0, 0, 0, 14) };
        }

        private WpfBlock RenderHeading(HeadingBlock heading)
        {
            var level = Math.Max(1, Math.Min(6, heading.Level));
            var paragraph = new Paragraph
            {
                FontSize = context.FontSize * HeadingScale[level - 1],
                FontWeight = level <= 2 ? FontWeights.SemiBold : FontWeights.SemiBold,
                Margin = new Thickness(0, level == 1 ? 6 : 18, 0, level <= 2 ? 10 : 6),
            };
            if (level <= 2)
            {
                paragraph.BorderThickness = new Thickness(0, 0, 0, 1);
                paragraph.Padding = new Thickness(0, 0, 0, 6);
                paragraph.SetResourceReference(Paragraph.BorderBrushProperty, "DividerStrokeColorDefaultBrush");
            }

            if (level == 6)
            {
                paragraph.SetResourceReference(TextElement.ForegroundProperty, "TextSecondaryBrush");
            }

            AddInlines(paragraph.Inlines, heading.Inline);
            return paragraph;
        }

        private WpfBlock RenderParagraph(ParagraphBlock block)
        {
            var paragraph = new Paragraph { Margin = new Thickness(0, 0, 0, 10) };
            AddInlines(paragraph.Inlines, block.Inline);
            return paragraph;
        }

        private WpfBlock RenderList(ListBlock list)
        {
            var isTaskList = list.OfType<ListItemBlock>().All(item => (item.FirstOrDefault() as ParagraphBlock)?.Inline?.FirstChild is TaskList);
            var wpfList = new List
            {
                MarkerStyle = isTaskList ? TextMarkerStyle.None : list.IsOrdered ? TextMarkerStyle.Decimal : TextMarkerStyle.Disc,
                Margin = new Thickness(0, 0, 0, 10),
                Padding = new Thickness(isTaskList ? 4 : 24, 0, 0, 0),
            };
            int start;
            if (list.IsOrdered && int.TryParse(list.OrderedStart, out start) && start > 1)
            {
                wpfList.StartIndex = start;
            }

            foreach (var item in list.OfType<ListItemBlock>())
            {
                var listItem = new ListItem();
                foreach (var child in item)
                {
                    var rendered = RenderBlock(child);
                    if (rendered is Paragraph paragraph)
                    {
                        paragraph.Margin = new Thickness(0, 0, 0, 3);
                    }

                    if (rendered != null)
                    {
                        listItem.Blocks.Add(rendered);
                    }
                }

                wpfList.ListItems.Add(listItem);
            }

            return wpfList;
        }

        private WpfBlock RenderQuote(QuoteBlock quote)
        {
            var callout = MarkdownNotes.CalloutOf(quote, context.Source);
            var section = new Section { Margin = new Thickness(0, 0, 0, 12), Padding = new Thickness(14, 8, 12, 4), BorderThickness = new Thickness(3, 0, 0, 0) };
            if (callout == null)
            {
                section.SetResourceReference(Section.BorderBrushProperty, "DividerStrokeColorDefaultBrush");
                section.SetResourceReference(TextElement.ForegroundProperty, "TextSecondaryBrush");
                AddChildren(section.Blocks, quote);
                return section;
            }

            var tone = CalloutTone(callout.Kind);
            section.SetResourceReference(Section.BorderBrushProperty, tone.Item1);
            section.SetResourceReference(Section.BackgroundProperty, tone.Item2);
            var title = new Paragraph { Margin = new Thickness(0, 0, 0, 6), FontWeight = FontWeights.SemiBold };
            var icon = new Run(tone.Item3 + "  ") { FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets") };
            icon.SetResourceReference(TextElement.ForegroundProperty, tone.Item1);
            title.Inlines.Add(icon);
            title.Inlines.Add(new Run(callout.Title.Length > 0 ? callout.Title : CalloutName(callout.Kind)));
            section.Blocks.Add(title);

            // İlk paragrafın "[!tür] Başlık" satırı atlanır; aynı paragrafta devam eden satırlar gösterilir.
            var first = quote.FirstOrDefault() as ParagraphBlock;
            if (first?.Inline != null)
            {
                var rest = new Paragraph { Margin = new Thickness(0, 0, 0, 6) };
                var afterBreak = false;
                foreach (var inline in first.Inline.ToList())
                {
                    if (!afterBreak)
                    {
                        afterBreak = inline is LineBreakInline;
                        continue;
                    }

                    var rendered = RenderInline(inline);
                    if (rendered != null)
                    {
                        rest.Inlines.Add(rendered);
                    }
                }

                if (rest.Inlines.Count > 0)
                {
                    section.Blocks.Add(rest);
                }
            }

            AddChildren(section.Blocks, quote, first != null ? 1 : 0);
            return section;
        }

        // (çizgi rengi, arka plan, ikon)
        private static Tuple<string, string, string> CalloutTone(string kind)
        {
            switch (kind)
            {
                case "warning":
                case "caution":
                case "attention":
                case "uyari":
                case "dikkat":
                    return Tuple.Create("SystemFillColorCautionBrush", "SystemFillColorCautionBackgroundBrush", "");
                case "danger":
                case "error":
                case "bug":
                case "failure":
                case "fail":
                case "missing":
                case "hata":
                    return Tuple.Create("SystemFillColorCriticalBrush", "SystemFillColorCriticalBackgroundBrush", "");
                case "success":
                case "check":
                case "done":
                case "tamam":
                    return Tuple.Create("SystemFillColorSuccessBrush", "SystemFillColorSuccessBackgroundBrush", "");
                case "question":
                case "help":
                case "faq":
                case "soru":
                    return Tuple.Create("SystemFillColorCautionBrush", "SubtleFillColorSecondaryBrush", "");
                case "quote":
                case "cite":
                    return Tuple.Create("TextSecondaryBrush", "SubtleFillColorSecondaryBrush", "");
                case "tip":
                case "hint":
                case "important":
                case "ipucu":
                    return Tuple.Create("SystemFillColorSuccessBrush", "SubtleFillColorSecondaryBrush", "");
                default:
                    return Tuple.Create("AccentTextFillColorPrimaryBrush", "SubtleFillColorSecondaryBrush", "");
            }
        }

        private static string CalloutName(string kind)
        {
            return kind.Length == 0 ? kind : char.ToUpperInvariant(kind[0]) + kind.Substring(1);
        }

        private WpfBlock RenderCode(CodeBlock code)
        {
            var text = code.Lines.ToString().TrimEnd('\r', '\n');
            var paragraph = new Paragraph(new Run(text))
            {
                FontFamily = new FontFamily(context.CodeFontFamily),
                FontSize = context.FontSize * 0.88,
                Padding = new Thickness(12, 8, 12, 8),
                Margin = new Thickness(0, 0, 0, 12),
                LineHeight = context.FontSize * 1.35,
            };
            paragraph.SetResourceReference(Paragraph.BackgroundProperty, "SubtleFillColorSecondaryBrush");
            return paragraph;
        }

        private WpfBlock RenderRaw(string text)
        {
            var paragraph = new Paragraph(new Run(text.TrimEnd())) { FontFamily = new FontFamily(context.CodeFontFamily), FontSize = context.FontSize * 0.85, Margin = new Thickness(0, 0, 0, 10) };
            paragraph.SetResourceReference(TextElement.ForegroundProperty, "TextTertiaryBrush");
            return paragraph;
        }

        private WpfBlock Divider(Thickness margin)
        {
            var line = new Border { Height = 1 };
            line.SetResourceReference(Border.BackgroundProperty, "DividerStrokeColorDefaultBrush");
            return new BlockUIContainer(line) { Margin = margin };
        }

        private WpfBlock RenderTable(Markdig.Extensions.Tables.Table table)
        {
            var wpf = new WpfTable { CellSpacing = 0, Margin = new Thickness(0, 0, 0, 14), BorderThickness = new Thickness(1, 1, 0, 0) };
            wpf.SetResourceReference(WpfTable.BorderBrushProperty, "CardStrokeColorDefaultBrush");
                        // Sütun sayısı hücrelerden (Markdig satır sonundaki "|" için fazladan tanım ekleyebilir).
            var columns = table.OfType<Markdig.Extensions.Tables.TableRow>().Select(r => r.Count).DefaultIfEmpty(0).Max();
            if (columns == 0)
            {
                columns = table.ColumnDefinitions.Count;
            }
            for (var i = 0; i < columns; i++)
            {
                wpf.Columns.Add(new TableColumn());
            }

            var group = new TableRowGroup();
            foreach (var row in table.OfType<Markdig.Extensions.Tables.TableRow>())
            {
                var wpfRow = new System.Windows.Documents.TableRow();
                if (row.IsHeader)
                {
                    wpfRow.FontWeight = FontWeights.SemiBold;
                    wpfRow.SetResourceReference(System.Windows.Documents.TableRow.BackgroundProperty, "SubtleFillColorSecondaryBrush");
                }

                var index = 0;
                foreach (var cell in row.OfType<Markdig.Extensions.Tables.TableCell>())
                {
                    var wpfCell = new System.Windows.Documents.TableCell { Padding = new Thickness(10, 5, 10, 5), BorderThickness = new Thickness(0, 0, 1, 1) };
                    wpfCell.SetResourceReference(System.Windows.Documents.TableCell.BorderBrushProperty, "CardStrokeColorDefaultBrush");
                    var alignment = index < table.ColumnDefinitions.Count ? table.ColumnDefinitions[index].Alignment : null;
                    foreach (var child in cell)
                    {
                        var rendered = RenderBlock(child);
                        if (rendered is Paragraph paragraph)
                        {
                            paragraph.Margin = new Thickness(0);
                            paragraph.TextAlignment = alignment == TableColumnAlign.Center ? TextAlignment.Center : alignment == TableColumnAlign.Right ? TextAlignment.Right : TextAlignment.Left;
                        }

                        if (rendered != null)
                        {
                            wpfCell.Blocks.Add(rendered);
                        }
                    }

                    if (wpfCell.Blocks.Count == 0)
                    {
                        wpfCell.Blocks.Add(new Paragraph { Margin = new Thickness(0) });
                    }

                    wpfRow.Cells.Add(wpfCell);
                    index++;
                }

                group.Rows.Add(wpfRow);
            }

            wpf.RowGroups.Add(group);
            return wpf;
        }

        private WpfBlock RenderFootnotes(FootnoteGroup footnotes)
        {
            var section = new Section { FontSize = context.FontSize * 0.85, Margin = new Thickness(0, 8, 0, 0) };
            section.SetResourceReference(TextElement.ForegroundProperty, "TextSecondaryBrush");
            section.Blocks.Add(Divider(new Thickness(0, 0, 0, 8)));
            foreach (var footnote in footnotes.OfType<Footnote>())
            {
                var number = new Paragraph(new Run(footnote.Order.ToString(System.Globalization.CultureInfo.InvariantCulture) + ".")) { Margin = new Thickness(0, 0, 0, 2), FontWeight = FontWeights.SemiBold };
                section.Blocks.Add(number);
                AddChildren(section.Blocks, footnote);
            }

            return section;
        }

        private void AddInlines(InlineCollection target, ContainerInline? container)
        {
            if (container == null)
            {
                return;
            }

            foreach (var inline in container)
            {
                var rendered = RenderInline(inline);
                if (rendered != null)
                {
                    target.Add(rendered);
                }
            }
        }

        private WpfInline? RenderInline(MdInline inline)
        {
            switch (inline)
            {
                case LiteralInline literal:
                    return RenderText(literal.Content.ToString());
                case WikiLinkInline wiki:
                    return RenderWikiLink(wiki);
                case TaskList task:
                    return RenderTask(task);
                case EmphasisInline emphasis:
                    return RenderEmphasis(emphasis);
                case CodeInline code:
                    var run = new Run(code.Content) { FontFamily = new FontFamily(context.CodeFontFamily), FontSize = context.FontSize * 0.9 };
                    run.SetResourceReference(TextElement.BackgroundProperty, "SubtleFillColorSecondaryBrush");
                    return run;
                case LineBreakInline lineBreak:
                    return lineBreak.IsHard || context.KeepLineBreaks ? (WpfInline)new LineBreak() : new Run(" ");
                case LinkInline link:
                    return link.IsImage ? RenderImage(link.Url ?? string.Empty, 0, ChildText(link)) : RenderLink(link);
                case AutolinkInline autolink:
                    return Hyperlink(new Run(autolink.Url), () => context.UrlClicked?.Invoke(autolink.IsEmail ? "mailto:" + autolink.Url : autolink.Url), autolink.Url);
                case HtmlEntityInline entity:
                    return new Run(entity.Transcoded.ToString());
                case HtmlInline html when html.Tag.StartsWith("<!--", StringComparison.Ordinal):
                    return null;
                case HtmlInline html:
                    var raw = new Run(html.Tag);
                    raw.SetResourceReference(TextElement.ForegroundProperty, "TextTertiaryBrush");
                    return raw;
                case FootnoteLink footnoteLink:
                    return new Run(footnoteLink.Footnote.Order.ToString(System.Globalization.CultureInfo.InvariantCulture)) { BaselineAlignment = BaselineAlignment.Superscript, FontSize = context.FontSize * 0.75 };
                case ContainerInline container:
                    var span = new Span();
                    AddInlines(span.Inlines, container);
                    return span;
                default:
                    return null;
            }
        }

        // Düz yazı; #etiketler vurgu renginde.
        private WpfInline RenderText(string text)
        {
            if (text.IndexOf('#') < 0)
            {
                return new Run(text);
            }

            var span = new Span();
            var last = 0;
            foreach (Match match in Tag.Matches(text))
            {
                if (match.Index > last)
                {
                    span.Inlines.Add(new Run(text.Substring(last, match.Index - last)));
                }

                var tag = new Run(match.Value);
                tag.SetResourceReference(TextElement.ForegroundProperty, "AccentTextFillColorPrimaryBrush");
                span.Inlines.Add(tag);
                last = match.Index + match.Length;
            }

            if (last < text.Length)
            {
                span.Inlines.Add(new Run(text.Substring(last)));
            }

            return span;
        }

        private WpfInline RenderEmphasis(EmphasisInline emphasis)
        {
            var span = new Span();
            AddInlines(span.Inlines, emphasis);
            if (emphasis.DelimiterChar == '~')
            {
                span.TextDecorations = TextDecorations.Strikethrough;
                span.SetResourceReference(TextElement.ForegroundProperty, "TextSecondaryBrush");
            }
            else if (emphasis.DelimiterChar == '=')
            {
                span.SetResourceReference(TextElement.BackgroundProperty, "SystemFillColorCautionBackgroundBrush");
            }
            else if (emphasis.DelimiterCount >= 2)
            {
                span.FontWeight = FontWeights.Bold;
                if (emphasis.DelimiterCount >= 3)
                {
                    span.FontStyle = FontStyles.Italic;
                }
            }
            else
            {
                span.FontStyle = FontStyles.Italic;
            }

            return span;
        }

        private WpfInline RenderTask(TaskList task)
        {
            var box = new CheckBox
            {
                IsChecked = task.Checked,
                Margin = new Thickness(0, 0, 6, -4),
                Padding = new Thickness(0),
                MinWidth = 0,
                MinHeight = 0,
                Cursor = Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Center,
                Tag = task.Line,
            };
            box.Click += (s, e) =>
            {
                // Görünüm metinden yeniden çizilir; kutunun kendi durumu metinden gelir.
                box.IsChecked = task.Checked;
                context.TaskToggled?.Invoke(task.Line);
            };
            return new InlineUIContainer(box) { BaselineAlignment = BaselineAlignment.Center };
        }

        private WpfInline RenderWikiLink(WikiLinkInline wiki)
        {
            if (wiki.IsEmbed)
            {
                if (MarkdownNotes.IsImage(wiki.Target))
                {
                    return RenderImage(wiki.Target, wiki.EmbedWidth, wiki.Target);
                }

                var file = context.Resolve(wiki.Target);
                if (file.Length == 0)
                {
                    context.MissingFiles.Add(wiki.Target);
                }

                var label = new Run("  ") { FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets") };
                var span = new Span(label);
                span.Inlines.Add(new Run(wiki.DisplayText));
                return file.Length > 0
                    ? Hyperlink(span, () => context.FileClicked?.Invoke(file), file)
                    : Missing(wiki.Target);
            }

            var text = new Run(wiki.DisplayText);
            return Hyperlink(text, () => context.WikiLinkClicked?.Invoke(wiki.Target), wiki.Target + (wiki.Heading.Length > 0 ? "#" + wiki.Heading : string.Empty));
        }

        private WpfInline RenderLink(LinkInline link)
        {
            var span = new Span();
            AddInlines(span.Inlines, link);
            if (span.Inlines.Count == 0)
            {
                span.Inlines.Add(new Run(link.Url ?? string.Empty));
            }

            var url = link.Url ?? string.Empty;
            return Hyperlink(span, () =>
            {
                if (url.Contains("://") || url.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
                {
                    context.UrlClicked?.Invoke(url);
                    return;
                }

                // Göreli md bağlantısı: not adı gibi açılır; başka dosya: dosya açılır.
                var file = context.Resolve(url);
                if (file.Length > 0 && !string.Equals(Path.GetExtension(file), ".md", StringComparison.OrdinalIgnoreCase))
                {
                    context.FileClicked?.Invoke(file);
                }
                else
                {
                    context.WikiLinkClicked?.Invoke(Path.GetFileNameWithoutExtension(Uri.UnescapeDataString(url)));
                }
            }, url);
        }

        private static string ChildText(ContainerInline container)
        {
            return string.Concat(container.Descendants<LiteralInline>().Select(l => l.Content.ToString()));
        }

        private WpfInline RenderImage(string target, int width, string alt)
        {
            if (target.Contains("://"))
            {
                // Dış ağ çağrısı yapılmaz: web resmi yüklenmez, bağlantı olarak gösterilir.
                return Hyperlink(new Run(alt.Length > 0 ? alt : target), () => context.UrlClicked?.Invoke(target), target);
            }

            var path = context.Resolve(target);
            if (path.Length == 0)
            {
                context.MissingFiles.Add(target);
                return Missing(target);
            }

            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
                bitmap.DecodePixelWidth = (int)Math.Min(1600, Math.Max(200, width > 0 ? width * 2 : context.MaxImageWidth * 1.5));
                bitmap.UriSource = new Uri(path);
                bitmap.EndInit();
                bitmap.Freeze();
                var image = new Image
                {
                    Source = bitmap,
                    Stretch = Stretch.Uniform,
                    MaxWidth = width > 0 ? Math.Min(width, context.MaxImageWidth) : context.MaxImageWidth,
                    Cursor = Cursors.Hand,
                    ToolTip = path,
                    Margin = new Thickness(0, 4, 0, 4),
                    Tag = path,
                };
                if (width > 0)
                {
                    image.Width = Math.Min(width, context.MaxImageWidth);
                }

                image.MouseLeftButtonUp += (s, e) => context.FileClicked?.Invoke(path);
                context.Images.Add(path);
                return new InlineUIContainer(image) { BaselineAlignment = BaselineAlignment.Bottom };
            }
            catch (Exception ex) when (ex is IOException || ex is NotSupportedException || ex is UriFormatException || ex is UnauthorizedAccessException || ex is ArgumentException)
            {
                context.MissingFiles.Add(target);
                return Missing(target);
            }
        }

        private static WpfInline Missing(string target)
        {
            var run = new Run("⚠ " + target) { FontStyle = FontStyles.Italic };
            run.SetResourceReference(TextElement.ForegroundProperty, "TextTertiaryBrush");
            return run;
        }

        private static Hyperlink Hyperlink(WpfInline content, Action click, string tip)
        {
            var link = new Hyperlink(content) { ToolTip = tip, TextDecorations = null, Cursor = Cursors.Hand };
            link.SetResourceReference(TextElement.ForegroundProperty, "AccentTextFillColorPrimaryBrush");
            link.Click += (s, e) => click();
            return link;
        }
    }
}
