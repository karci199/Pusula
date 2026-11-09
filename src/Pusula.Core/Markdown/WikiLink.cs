using System;
using Markdig;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Parsers.Inlines;
using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Pusula.Core.Markdown
{
    /// <summary>
    /// Obsidian bağlantısı: <c>[[hedef]]</c>, <c>[[hedef|görünen ad]]</c>, <c>[[hedef#başlık]]</c>; başında <c>!</c> varsa gömme
    /// (<c>![[foto.jpg]]</c>, <c>![[foto.jpg|300]]</c>).
    /// </summary>
    public sealed class WikiLinkInline : LeafInline
    {
        /// <summary>Hedef (not adı, numara ya da dosya adı).</summary>
        public string Target { get; set; } = string.Empty;

        /// <summary>Başlık (<c>#</c> sonrası); yoksa boş.</summary>
        public string Heading { get; set; } = string.Empty;

        /// <summary>Görünen ad (<c>|</c> sonrası); yoksa boş. Gömmede genişlik olabilir (<c>|300</c>).</summary>
        public string Alias { get; set; } = string.Empty;

        /// <summary>Gömme mi (<c>![[…]]</c>).</summary>
        public bool IsEmbed { get; set; }

        /// <summary>Ekranda görünecek yazı: görünen ad, yoksa hedef (ve başlık).</summary>
        public string DisplayText
        {
            get
            {
                if (Alias.Length > 0 && !IsEmbed)
                {
                    return Alias;
                }

                return Heading.Length > 0 ? Target + " › " + Heading : Target;
            }
        }

        /// <summary>Gömmede <c>|300</c> gibi genişlik; yoksa 0.</summary>
        public int EmbedWidth
        {
            get
            {
                int width;
                var text = Alias.Split('x')[0].Trim();
                return IsEmbed && int.TryParse(text, out width) && width > 0 ? width : 0;
            }
        }
    }

    /// <summary><c>[[…]]</c> ve <c>![[…]]</c> ayrıştırıcısı (Markdig'in bağlantı ayrıştırıcısından önce çalışır).</summary>
    public sealed class WikiLinkParser : InlineParser
    {
        /// <summary>Açılış karakterleri: <c>[</c> ve <c>!</c>.</summary>
        public WikiLinkParser()
        {
            OpeningCharacters = new[] { '[', '!' };
        }

        /// <summary>Konumda bir wiki bağlantısı varsa okur.</summary>
        public override bool Match(InlineProcessor processor, ref StringSlice slice)
        {
            var start = slice.Start;
            var embed = slice.CurrentChar == '!';
            var open = embed ? 1 : 0;
            if (slice.PeekCharExtra(open) != '[' || slice.PeekCharExtra(open + 1) != '[')
            {
                return false;
            }

            var contentStart = start + open + 2;
            var text = slice.Text;
            var close = text.IndexOf("]]", contentStart, slice.End - contentStart + 1, StringComparison.Ordinal);
            if (close < 0 || close == contentStart)
            {
                return false;
            }

            var content = text.Substring(contentStart, close - contentStart);
            if (content.IndexOf('\n') >= 0 || content.IndexOf('[') >= 0)
            {
                return false;
            }

            var alias = string.Empty;
            var pipe = content.IndexOf('|');
            if (pipe >= 0)
            {
                alias = content.Substring(pipe + 1).Trim();
                content = content.Substring(0, pipe);
            }

            var heading = string.Empty;
            var hash = content.IndexOf('#');
            if (hash >= 0)
            {
                heading = content.Substring(hash + 1).Trim();
                content = content.Substring(0, hash);
            }

            int line;
            int column;
            var inline = new WikiLinkInline
            {
                Target = content.Trim(),
                Heading = heading,
                Alias = alias,
                IsEmbed = embed,
                Span = new SourceSpan(processor.GetSourcePosition(start, out line, out column), processor.GetSourcePosition(close + 1)),
                Line = line,
                Column = column,
            };
            processor.Inline = inline;
            slice.Start = close + 2;
            return true;
        }
    }

    /// <summary>Wiki bağlantılarını boru hattına ekler.</summary>
    public sealed class WikiLinkExtension : IMarkdownExtension
    {
        /// <summary>Ayrıştırıcıyı bağlantı ayrıştırıcısından önce ekler.</summary>
        public void Setup(MarkdownPipelineBuilder pipeline)
        {
            if (!pipeline.InlineParsers.Contains<WikiLinkParser>())
            {
                pipeline.InlineParsers.InsertBefore<LinkInlineParser>(new WikiLinkParser());
            }
        }

        /// <summary>HTML çizici yok (görünüm WPF'te çizilir).</summary>
        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
        }
    }
}
