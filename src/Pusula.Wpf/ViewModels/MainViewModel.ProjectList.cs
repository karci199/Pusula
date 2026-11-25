using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Pusula.Core.Tracking;
using Pusula.Wpf.Properties;

namespace Pusula.Wpf.ViewModels
{
    /// <summary>
    /// Sol listede proje sırası, etiket süzgeci ve "hepsini kapat" (25.11.2026). Sıra: numara (varsayılan), son değişiklik
    /// ya da ad; etiket seçiliyse yalnız o etiketli projeler. Seçim kullanıcı ayarlarında hatırlanır. Proje olmayan kök
    /// klasörler projelerden sonra, adına göre durur ve süzgeçten etkilenmez.
    /// </summary>
    public sealed partial class MainViewModel
    {
        /// <summary>Sıra: numara.</summary>
        public const string SortByNumber = "number";

        /// <summary>Sıra: son değişiklik (en yeni üstte).</summary>
        public const string SortByRecent = "recent";

        /// <summary>Sıra: ad.</summary>
        public const string SortByName = "name";

        private readonly Dictionary<string, List<string>> projectTags = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> projectNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Seçili proje sırası (<see cref="SortByNumber"/>, <see cref="SortByRecent"/>, <see cref="SortByName"/>).</summary>
        public string ProjectSort
        {
            get { return settings.ProjectSort.Length == 0 ? SortByNumber : settings.ProjectSort; }
            set
            {
                var next = value == SortByRecent || value == SortByName ? value : SortByNumber;
                if (next == ProjectSort)
                {
                    return;
                }

                settings.ProjectSort = next == SortByNumber ? string.Empty : next;
                settings.Save();
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasProjectListFilter));
                ArrangeProjects();
            }
        }

        /// <summary>Yalnız bu etiketli projeler (boş: hepsi).</summary>
        public string ProjectTag
        {
            get { return settings.ProjectTag; }
            set
            {
                var next = value ?? string.Empty;
                if (string.Equals(next, settings.ProjectTag, StringComparison.CurrentCultureIgnoreCase))
                {
                    return;
                }

                settings.ProjectTag = next;
                settings.Save();
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasProjectListFilter));
                ArrangeProjects();
            }
        }

        /// <summary>Projelerdeki etiketler (menüde süzgeç seçenekleri; sıralı).</summary>
        public ObservableCollection<string> ProjectTagOptions { get; } = new ObservableCollection<string>();

        /// <summary>Sıra varsayılandan farklı ya da etiket seçili mi (menü düğmesi vurgulu).</summary>
        public bool HasProjectListFilter
        {
            get { return ProjectSort != SortByNumber || settings.ProjectTag.Length > 0; }
        }

        /// <summary>Etiket süzgecinde gizlenen proje sayısı (ağacın altında "2 proje gizli").</summary>
        public int HiddenProjectCount { get; private set; }

        /// <summary>
        /// Sol listedeki düğümleri kapatır (projeler, envanter). Seçili kaydın yolu açık kalır: üstü kapanırsa ağaç seçimi
        /// üst düğüme kayar ve sayfa değişirdi.
        /// </summary>
        public void CollapseAll()
        {
            var keep = new HashSet<TreeNodeViewModel>();
            if (selectedNode != null)
            {
                foreach (var root in Nodes)
                {
                    CollectPath(root, selectedNode, keep);
                }
            }

            var pending = new Stack<TreeNodeViewModel>(Nodes);
            while (pending.Count > 0)
            {
                var node = pending.Pop();
                if (!keep.Contains(node))
                {
                    node.IsExpanded = false;
                }

                foreach (var child in node.Children)
                {
                    pending.Push(child);
                }
            }
        }

        // Hedefe giden yoldaki üst düğümleri toplar (hedefin kendisi hariç).
        private static bool CollectPath(TreeNodeViewModel node, TreeNodeViewModel target, HashSet<TreeNodeViewModel> path)
        {
            if (ReferenceEquals(node, target))
            {
                return true;
            }

            foreach (var child in node.AllChildren)
            {
                if (CollectPath(child, target, path))
                {
                    path.Add(node);
                    return true;
                }
            }

            return false;
        }

        // Ağaç okunduktan sonra (Refresh) projelerin etiketleri ve adları okunur, sonra sıra / süzgeç uygulanır.
        private void ReadProjectInfo()
        {
            projectTags.Clear();
            projectNames.Clear();
            foreach (var project in Nodes.Where(n => n.Kind == FolderKind.Project))
            {
                var info = JsonFile.TryRead<ProjectInfo>(TrackingFiles.ProjectFile(project.Path));
                projectTags[project.Path] = (info?.Tags ?? new List<string>()).Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim()).ToList();
                projectNames[project.Path] = info != null && info.Name.Trim().Length > 0 ? info.Name.Trim() : NameFromFolder(project.Path);
            }

            var tags = projectTags.Values.SelectMany(t => t).Distinct(StringComparer.CurrentCultureIgnoreCase).OrderBy(t => t, StringComparer.CurrentCultureIgnoreCase).ToList();
            ProjectTagOptions.Clear();
            foreach (var tag in tags)
            {
                ProjectTagOptions.Add(tag);
            }
        }

        // ProjectNodes (ağaç) sırası ve süzgeci; Nodes değişmez (seçme ve arama bütün projeleri bulur).
        private void ArrangeProjects()
        {
            var projects = Nodes.Where(n => n.Kind == FolderKind.Project).ToList();
            var others = Nodes.Where(n => n.Kind != FolderKind.Project && n.Kind != FolderKind.InventoryGroup)
                .OrderBy(n => n.Title, StringComparer.CurrentCultureIgnoreCase).ToList();
            var tag = settings.ProjectTag;
            var shown = projects.Where(p => tag.Length == 0 || TagsOf(p).Contains(tag, StringComparer.CurrentCultureIgnoreCase)).ToList();
            IEnumerable<TreeNodeViewModel> ordered;
            switch (ProjectSort)
            {
                case SortByRecent:
                    var times = shown.ToDictionary(p => p.Path, p => ProjectCard.NewestWrite(p.Path, 2) ?? DateTime.MinValue, StringComparer.OrdinalIgnoreCase);
                    ordered = shown.OrderByDescending(p => times[p.Path]).ThenBy(p => p.Title, Comparer<string>.Create(CompareNumbers));
                    break;
                case SortByName:
                    ordered = shown.OrderBy(p => NameOf(p), StringComparer.CurrentCultureIgnoreCase).ThenBy(p => p.Title, Comparer<string>.Create(CompareNumbers));
                    break;
                default:
                    ordered = shown.OrderBy(p => p.Title, Comparer<string>.Create(CompareNumbers));
                    break;
            }

            var list = ordered.Concat(others).ToList();
            if (!list.SequenceEqual(ProjectNodes))
            {
                ProjectNodes.Clear();
                foreach (var node in list)
                {
                    ProjectNodes.Add(node);
                }
            }

            HiddenProjectCount = projects.Count - shown.Count;
            OnPropertyChanged(nameof(HiddenProjectCount));
            OnPropertyChanged(nameof(HiddenProjectText));
        }

        /// <summary>"2 proje etiket süzgecinde gizli"; gizli yoksa boş.</summary>
        public string HiddenProjectText
        {
            get { return HiddenProjectCount == 0 ? string.Empty : string.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.Tree_HiddenProjects, HiddenProjectCount, settings.ProjectTag); }
        }

        private List<string> TagsOf(TreeNodeViewModel project)
        {
            List<string> tags;
            return projectTags.TryGetValue(project.Path, out tags) ? tags : new List<string>();
        }

        private string NameOf(TreeNodeViewModel project)
        {
            string name;
            return projectNames.TryGetValue(project.Path, out name) ? name : project.Title;
        }

        private static string NameFromFolder(string path)
        {
            var folder = Path.GetFileName(path);
            var cut = folder.IndexOf('_');
            return cut > 0 ? folder.Substring(cut + 1) : folder;
        }

        // "7000_A" < "10000_B": baştaki sayı sayı olarak karşılaştırılır, sonra ad.
        private static int CompareNumbers(string a, string b)
        {
            long x, y;
            var ax = ProjectLeadingNumber(a, out x);
            var bx = ProjectLeadingNumber(b, out y);
            if (ax && bx && x != y)
            {
                return x.CompareTo(y);
            }

            if (ax != bx)
            {
                return ax ? -1 : 1;
            }

            return string.Compare(a, b, StringComparison.CurrentCultureIgnoreCase);
        }

        private static bool ProjectLeadingNumber(string text, out long value)
        {
            var digits = new string((text ?? string.Empty).TakeWhile(char.IsDigit).ToArray());
            return long.TryParse(digits, out value);
        }
    }
}
