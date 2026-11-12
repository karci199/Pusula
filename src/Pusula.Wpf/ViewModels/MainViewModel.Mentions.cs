using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Pusula.Core.Notes;
using Pusula.Core.Tracking;
using Pusula.Wpf.Properties;

namespace Pusula.Wpf.ViewModels
{
    /// <summary>"Bahsedildiği yerler": seçili proje / stok / kayda başka notlarda yapılan bahisler.</summary>
    public sealed partial class MainViewModel
    {
        private static readonly Regex LeadingNumber = new Regex(@"^([A-Za-z]{2,5}-\d{4}-\d{4}|\d{4}-\d{4}-\d{4}|\d{3,6})(?=$|[_\s-])", RegexOptions.CultureInvariant);

        private Task<MentionIndex>? mentionIndex;
        private string mentionRoot = string.Empty;
        private CancellationTokenSource? mentionLoad;
        private bool mentionsExpanded = true;

        /// <summary>Seçili düğüme yapılan bahisler (yeniden eskiye).</summary>
        public ObservableCollection<MentionRow> Mentions { get; } = new ObservableCollection<MentionRow>();

        /// <summary>Bahis var mı.</summary>
        public bool HasMentions
        {
            get { return Mentions.Count > 0; }
        }

        /// <summary>"Bahsedildiği yerler · 3".</summary>
        public string MentionsHeader
        {
            get { return string.Format(Resources.Mentions_Header, Mentions.Count); }
        }

        /// <summary>Kart açık mı.</summary>
        public bool MentionsExpanded
        {
            get { return mentionsExpanded; }
            set { SetProperty(ref mentionsExpanded, value); }
        }

        /// <summary>Notlar taranır (kök başına bir kez; yenileyince baştan). Arka planda çalışır.</summary>
        public Task<MentionIndex> MentionIndexTask()
        {
            if (mentionIndex == null || !string.Equals(mentionRoot, rootPath, StringComparison.OrdinalIgnoreCase))
            {
                var root = rootPath;
                mentionRoot = root;
                mentionIndex = Task.Run(() => MentionIndex.Build(root, CancellationToken.None));
            }

            return mentionIndex;
        }

        /// <summary>Düğümün bahis anahtarları: klasör adı, baştaki numarası; kayıtta kayıt numarası.</summary>
        public static List<string> MentionKeys(TreeNodeViewModel node)
        {
            var keys = new List<string>();
            var name = Path.GetFileName(node.Path.TrimEnd('\\', '/'));
            keys.Add(name);
            var number = LeadingNumber.Match(name);
            if (number.Success)
            {
                keys.Add(number.Groups[1].Value);
            }

            if (node.Item != null)
            {
                keys.Add(node.Item.Record.Number);
            }

            return keys.Where(k => k.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        // Yenilemede tarama bayatlar; bir sonraki istekte yeniden yapılır.
        private void ResetMentions()
        {
            mentionIndex = null;
        }

        private async void LoadMentions(TreeNodeViewModel? node)
        {
            mentionLoad?.Cancel();
            Mentions.Clear();
            NotifyMentions();
            if (node == null || !(node.Kind == FolderKind.Project || node.Kind == FolderKind.Stock || node.Item != null) || !HasRoot)
            {
                return;
            }

            var source = new CancellationTokenSource();
            mentionLoad = source;
            var keys = MentionKeys(node);
            var own = node.Path;
            List<Mention> found;
            try
            {
                var index = await MentionIndexTask().ConfigureAwait(true);
                found = await Task.Run(() => index.Find(keys, own), source.Token).ConfigureAwait(true);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex) when (ex is System.IO.IOException || ex is UnauthorizedAccessException)
            {
                // Notlar okunamadı (ağ paylaşımı koptu …): anılma listesi boş kalır.
                return;
            }

            if (source.IsCancellationRequested || !ReferenceEquals(node, selectedNode))
            {
                return;
            }

            foreach (var mention in found)
            {
                Mentions.Add(new MentionRow(mention, rootPath));
            }

            NotifyMentions();
        }

        private void NotifyMentions()
        {
            OnPropertyChanged(nameof(HasMentions));
            OnPropertyChanged(nameof(MentionsHeader));
        }
    }

    /// <summary>Bahsedildiği yerler kartında bir satır.</summary>
    public sealed class MentionRow
    {
        /// <summary>Bahisten satır kurar; başlık notun adı ve klasörü (köke göre).</summary>
        public MentionRow(Mention mention, string root)
        {
            Path = mention.Path;
            Line = mention.Line;
            Snippet = mention.Snippet;
            IsLink = mention.IsLink;
            Date = mention.Date;
            var folder = System.IO.Path.GetDirectoryName(mention.Path) ?? string.Empty;
            Source = System.IO.Path.GetFileNameWithoutExtension(mention.Path);
            var parent = System.IO.Path.GetFileName(folder);
            if (parent.Length > 0 && !string.Equals(parent, Source, StringComparison.OrdinalIgnoreCase))
            {
                Source = parent + " · " + Source;
            }

            Meta = mention.Date == DateTime.MinValue ? string.Empty : mention.Date.ToString(Resources.Format_Date);
            Folder = folder;
            RelativePath = root.Length > 0 && mention.Path.StartsWith(root, StringComparison.OrdinalIgnoreCase)
                ? mention.Path.Substring(root.Length).TrimStart('\\', '/')
                : mention.Path;
        }

        /// <summary>Notun yolu.</summary>
        public string Path { get; }

        /// <summary>Satır (0'dan).</summary>
        public int Line { get; }

        /// <summary>Satırın kendisi.</summary>
        public string Snippet { get; }

        /// <summary>Bağlantıyla mı bahsedilmiş (<c>[[…]]</c>).</summary>
        public bool IsLink { get; }

        /// <summary>Tarih (sıralama).</summary>
        public DateTime Date { get; }

        /// <summary>"AB-7000-0003 · test" gibi kaynak.</summary>
        public string Source { get; }

        /// <summary>Tarih metni.</summary>
        public string Meta { get; }

        /// <summary>Notun klasörü.</summary>
        public string Folder { get; }

        /// <summary>Köke göre yol (ipucu).</summary>
        public string RelativePath { get; }
    }
}
