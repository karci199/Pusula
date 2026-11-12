using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Pusula.Core.Notes;
using Pusula.Core.Tracking;

namespace Pusula.Wpf.ViewModels
{
    /// <summary>
    /// Ana görünüm: hiçbir şey seçili değilken görünen başlangıç sayfası. Son açılanlar ve bütün projelerdeki
    /// geciken işler ile kullanıcının açık işleri.
    /// </summary>
    public sealed partial class MainViewModel
    {
        private const int RecentLimit = 8;
        private const int HomeTaskLimit = 12;
        private int homeVersion;

        /// <summary>Son açılan proje, stok ve kayıtlar (ağaçta hâlâ duranlar).</summary>
        public ObservableCollection<TreeNodeViewModel> RecentNodes { get; } = new ObservableCollection<TreeNodeViewModel>();

        /// <summary>Son tarihi geçmiş açık işler (bütün projeler).</summary>
        public ObservableCollection<OpenTaskRow> HomeLateTasks { get; } = new ObservableCollection<OpenTaskRow>();

        /// <summary>Kullanıcının gecikmemiş açık işleri (bütün projeler).</summary>
        public ObservableCollection<OpenTaskRow> HomeMyTasks { get; } = new ObservableCollection<OpenTaskRow>();

        /// <summary>En genel sayılar: projeler, stoklar, her kayıt türü, kişiler.</summary>
        public ObservableCollection<RecordCount> HomeCounts { get; } = new ObservableCollection<RecordCount>();

        /// <summary>Kullanıcının sayıları: açık işleri, geciken işleri, adının geçtiği kayıtlar.</summary>
        public ObservableCollection<RecordCount> MyCounts { get; } = new ObservableCollection<RecordCount>();

        // Ağaçtan ve okunmuş kayıtlardan genel sayılar (diske ayrıca gidilmez).
        private void FillHomeCounts()
        {
            HomeCounts.Clear();
            var projects = Nodes.Where(n => n.Kind == FolderKind.Project).ToList();
            var project = types.Find(Pusula.Core.Types.TypeSet.ProjectKey);
            var projectLabel = string.IsNullOrEmpty(project?.PluralName) ? Properties.Resources.Home_Projects : project!.PluralName;
            HomeCounts.Add(new RecordCount(projectLabel, project?.Icon ?? "E821", project?.Color ?? string.Empty, projects.Count));
            var stock = types.Stock;
            var stockLabel = stock.PluralName.Length == 0 ? Properties.Resources.Home_Stocks : stock.PluralName;
            HomeCounts.Add(new RecordCount(stockLabel, stock.Icon, stock.Color, projects.Sum(p => p.Children.Count(c => c.Kind == FolderKind.Stock))));
            foreach (var type in types.InventoryTypes.GroupBy(t => t.Key, StringComparer.OrdinalIgnoreCase).Select(g => g.First()))
            {
                HomeCounts.Add(new RecordCount(type.PluralName, type.Icon, type.Color,
                    inventoryItems.Count(i => string.Equals(i.Record.Type, type.Key, StringComparison.OrdinalIgnoreCase))));
            }

            var person = types.Person;
            HomeCounts.Add(new RecordCount(person.PluralName, person.Icon, person.Color, types.People.Count(p => !p.HasLeft)));
        }

        /// <summary>Son açılan var mı.</summary>
        public bool HasRecent
        {
            get { return RecentNodes.Count > 0; }
        }

        /// <summary>Geciken iş var mı.</summary>
        public bool HasHomeLateTasks
        {
            get { return HomeLateTasks.Count > 0; }
        }

        /// <summary>Kullanıcıya ait açık iş var mı.</summary>
        public bool HasHomeMyTasks
        {
            get { return HomeMyTasks.Count > 0; }
        }

        /// <summary>Başlangıç sayfasında hiçbir liste yoksa yalnız ipucu görünür.</summary>
        public bool IsHomeEmpty
        {
            get { return !HasRecent && !HasHomeLateTasks && !HasHomeMyTasks; }
        }

        /// <summary>Kullanıcı sayıları gösterilir mi (kullanıcı adı varsa).</summary>
        public bool HasMyCounts
        {
            get { return MyCounts.Count > 0; }
        }

        // Seçilen proje / stok / kayıt son açılanların başına yazılır.
        private void RememberRecent(TreeNodeViewModel? node)
        {
            if (node == null || node.Kind == FolderKind.Plain || node.Kind == FolderKind.InventoryGroup || node.Kind == FolderKind.InventoryType)
            {
                return;
            }

            var list = settings.Recent ?? new List<string>();
            list.RemoveAll(p => string.Equals(p, node.Path, StringComparison.OrdinalIgnoreCase));
            list.Insert(0, node.Path);
            if (list.Count > RecentLimit)
            {
                list.RemoveRange(RecentLimit, list.Count - RecentLimit);
            }

            settings.Recent = list;

            // Ok tuşuyla gezerken her adımda diske yazılmasın: kısa bir sessizlikten sonra tek seferde yazılır.
            if (recentSaveTimer == null)
            {
                recentSaveTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
                recentSaveTimer.Tick += (s, e) => SaveRecentNow();
            }

            recentSaveTimer.Stop();
            recentSaveTimer.Start();
            recentDirty = true;
        }

        private System.Windows.Threading.DispatcherTimer? recentSaveTimer;
        private bool recentDirty;

        /// <summary>Bekleyen "son açılanlar" yazımını hemen yapar (pencere kapanırken de çağrılır).</summary>
        public void SaveRecentNow()
        {
            recentSaveTimer?.Stop();
            if (!recentDirty)
            {
                return;
            }

            recentDirty = false;
            settings.Save();
        }

        /// <summary>Başlangıç sayfasını doldurur: son açılanlar hemen, işler arka planda okunur.</summary>
        public async void LoadHome()
        {
            RecentNodes.Clear();
            foreach (var path in settings.Recent ?? new List<string>())
            {
                var node = FindNodeByPath(path);
                if (node != null)
                {
                    RecentNodes.Add(node);
                }
            }

            FillHomeCounts();
            var version = ++homeVersion;
            var root = rootPath;
            var user = CurrentUser;
            List<OpenTask> tasks;
            if (!HasRoot)
            {
                tasks = new List<OpenTask>();
            }
            else
            {
                try
                {
                    tasks = await Task.Run(() => OpenTask.ReadAll(root, false)).ConfigureAwait(true);
                }
                catch (Exception ex) when (ex is System.IO.IOException || ex is UnauthorizedAccessException)
                {
                    tasks = new List<OpenTask>();
                }
            }

            if (version != homeVersion)
            {
                return;
            }

            HomeLateTasks.Clear();
            HomeMyTasks.Clear();
            var today = DateTime.Today;
            foreach (var task in tasks.Where(t => t.Due.HasValue && t.Due.Value.Date < today).OrderBy(t => t.Due).Take(HomeTaskLimit))
            {
                HomeLateTasks.Add(new OpenTaskRow(task, root));
            }

            var mine = tasks
                .Where(t => !(t.Due.HasValue && t.Due.Value.Date < today))
                .Where(t => user.Length > 0 && string.Equals(t.PersonName, user, StringComparison.CurrentCultureIgnoreCase))
                .OrderBy(t => t.Due ?? DateTime.MaxValue)
                .Take(HomeTaskLimit);
            foreach (var task in mine)
            {
                HomeMyTasks.Add(new OpenTaskRow(task, root));
            }

            // Benim sayılarım: bütün açık işlerim (liste sınırından bağımsız), gecikenler, adımın geçtiği kayıtlar.
            MyCounts.Clear();
            if (user.Length > 0)
            {
                var myTasks = tasks.Where(t => string.Equals(t.PersonName, user, StringComparison.CurrentCultureIgnoreCase)).ToList();
                MyCounts.Add(new RecordCount(Properties.Resources.Home_MyOpen, "E73E", "#4CC2FF", myTasks.Count));
                MyCounts.Add(new RecordCount(Properties.Resources.Home_MyLate, "E823", "#FF6B6B", myTasks.Count(t => t.Due.HasValue && t.Due.Value.Date < today)));
                MyCounts.Add(new RecordCount(Properties.Resources.Home_MyRecords, "E8F1", string.Empty,
                    inventoryItems.Count(i => i.Record.Events.Any(e => string.Equals(e.Person, user, StringComparison.CurrentCultureIgnoreCase)))));
            }

            OnPropertyChanged(nameof(HasRecent));
            OnPropertyChanged(nameof(HasHomeLateTasks));
            OnPropertyChanged(nameof(HasHomeMyTasks));
            OnPropertyChanged(nameof(IsHomeEmpty));
            OnPropertyChanged(nameof(HasMyCounts));
        }

        private TreeNodeViewModel? FindNodeByPath(string path)
        {
            var pending = new Stack<TreeNodeViewModel>(Nodes);
            while (pending.Count > 0)
            {
                var node = pending.Pop();
                if (string.Equals(node.Path, path, StringComparison.OrdinalIgnoreCase))
                {
                    return node;
                }

                foreach (var child in node.Children)
                {
                    pending.Push(child);
                }
            }

            return null;
        }

        /// <summary>İşin geldiği notun klasörünü ağaçta seçer (stok ya da kayıt); bulunamazsa notu açar.</summary>
        public void OpenHomeTask(OpenTaskRow row)
        {
            if (row == null)
            {
                return;
            }

            var folder = System.IO.Path.GetDirectoryName(row.NotePath) ?? string.Empty;
            while (folder.Length > 0 && !string.Equals(folder.TrimEnd('\\'), rootPath.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
            {
                if (FindNodeByPath(folder) != null)
                {
                    SelectNodeByPath(folder);
                    return;
                }

                folder = System.IO.Path.GetDirectoryName(folder) ?? string.Empty;
            }

            OpenNoteOrFolder(row.NotePath, System.IO.Path.GetDirectoryName(row.NotePath) ?? string.Empty);
        }
    }
}
