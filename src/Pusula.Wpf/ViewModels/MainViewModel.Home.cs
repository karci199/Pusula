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

            OnPropertyChanged(nameof(HasRecent));
            OnPropertyChanged(nameof(HasHomeLateTasks));
            OnPropertyChanged(nameof(HasHomeMyTasks));
            OnPropertyChanged(nameof(IsHomeEmpty));
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
