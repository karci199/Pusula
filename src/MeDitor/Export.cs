using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Markdig;
using MeDitor.Markdown;
using MeDitor.ViewModels;

namespace MeDitor
{
    /// <summary>
    /// Notu HTML'e çevirir: dosyaya dışa aktarma ve yazdırma (açık temalı HTML varsayılan tarayıcıda açılır, yazdırma
    /// penceresi kendiliğinden gelir; "PDF olarak kaydet" de oradan). Obsidian bağlantıları (<c>[[not]]</c>) metne,
    /// gömülü resimler (<c>![[resim.png]]</c>) dosya yoluna çevrilir.
    /// </summary>
    public static class Export
    {
        private static readonly Regex Embed = new Regex(@"!\[\[([^\]\|]+)(\|[^\]]*)?\]\]", RegexOptions.Compiled);
        private static readonly Regex Wiki = new Regex(@"\[\[([^\]\|]+)(?:\|([^\]]*))?\]\]", RegexOptions.Compiled);
        private static readonly Regex Comment = new Regex(@"%%.*?%%", RegexOptions.Compiled | RegexOptions.Singleline);

        private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UseYamlFrontMatter()
            .UseTaskLists()
            .Build();

        /// <summary>Notun tam HTML belgesi (başlık, açık tema biçimi, notun klasörüne göre göreli yollar).</summary>
        public static string ToHtml(EditorViewModel note, bool printOnLoad)
        {
            var folder = note.Folder;
            var text = Comment.Replace(note.Text ?? string.Empty, string.Empty);
            text = Embed.Replace(text, m =>
            {
                var path = MarkdownNotes.ResolveFile(folder, note.Root, m.Groups[1].Value.Trim());
                return path.Length > 0 ? "![](" + new Uri(path).AbsoluteUri + ")" : m.Value;
            });
            text = Wiki.Replace(text, m => m.Groups[2].Success && m.Groups[2].Value.Length > 0 ? m.Groups[2].Value : m.Groups[1].Value);
            var body = Markdig.Markdown.ToHtml(text, Pipeline);
            var baseHref = folder.Length > 0 ? "<base href=\"" + new Uri(folder.TrimEnd('\\') + "\\").AbsoluteUri + "\">" : string.Empty;
            var print = printOnLoad ? "<script>window.onload=function(){setTimeout(function(){window.print();},300);};</script>" : string.Empty;
            return "<!doctype html>\n<html lang=\"tr\"><head><meta charset=\"utf-8\">" + baseHref
                + "<title>" + WebUtility.HtmlEncode(note.FileName) + "</title><style>" + Css + "</style>" + print + "</head>\n<body><article>\n"
                + body + "\n</article></body></html>\n";
        }

        /// <summary>HTML dosyası yazar (atomik). Hata metni ya da boş döndürür.</summary>
        public static string SaveHtml(EditorViewModel note, string path)
        {
            try
            {
                var temp = path + ".tmp";
                File.WriteAllText(temp, ToHtml(note, false), new UTF8Encoding(false));
                if (File.Exists(path))
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

        /// <summary>Yazdırma: geçici HTML'i varsayılan tarayıcıda açar; sayfa açılınca yazdırma penceresi gelir.</summary>
        public static void Print(EditorViewModel note)
        {
            var folder = Path.Combine(Path.GetTempPath(), "MeDitor");
            Directory.CreateDirectory(folder);
            var path = Path.Combine(folder, "yazdir_" + DateTime.Now.ToString("yyyyMMdd_HHmmss", System.Globalization.CultureInfo.InvariantCulture) + ".html");
            File.WriteAllText(path, ToHtml(note, true), new UTF8Encoding(false));
            App.OpenShell(path);
        }

        private const string Css =
            "body{margin:0;background:#fff;color:#1f2328;font:15px/1.6 'Segoe UI',Arial,sans-serif}" +
            "article{max-width:860px;margin:32px auto;padding:0 28px}" +
            "h1,h2,h3,h4{line-height:1.25;margin:1.4em 0 .5em}h1{font-size:2em;border-bottom:1px solid #d8dee4;padding-bottom:.3em}" +
            "h2{font-size:1.5em;border-bottom:1px solid #d8dee4;padding-bottom:.3em}" +
            "code{font-family:'Cascadia Mono',Consolas,monospace;background:#f3f4f6;border-radius:4px;padding:.1em .35em;font-size:.9em}" +
            "pre{background:#f6f8fa;border-radius:6px;padding:12px 16px;overflow:auto}pre code{background:none;padding:0}" +
            "blockquote{margin:0;padding:0 1em;color:#59636e;border-left:.25em solid #d1d9e0}" +
            "table{border-collapse:collapse;margin:1em 0}th,td{border:1px solid #d1d9e0;padding:6px 12px}th{background:#f6f8fa}" +
            "img{max-width:100%}a{color:#0969da}hr{border:0;border-top:1px solid #d1d9e0;margin:1.5em 0}" +
            "mark{background:#fff3a3}ul.contains-task-list{list-style:none;padding-left:1.2em}" +
            "@media print{article{margin:0;max-width:none}a{color:inherit}}";
    }
}
