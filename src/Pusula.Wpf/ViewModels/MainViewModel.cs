using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using Pusula.Core.Tracking;
using Pusula.Wpf.Mvvm;
using Pusula.Wpf.Properties;

namespace Pusula.Wpf.ViewModels
{
    /// <summary>
    /// Ana pencerenin durumu: kök klasör, ağaç, seçili düğüm, yeni proje/stok diyalogları.
    /// </summary>
    public sealed class MainViewModel : ViewModelBase
    {
        private string rootPath = string.Empty;
        private TreeNodeViewModel? selectedNode;
        private string status = string.Empty;

        private bool isProjectDialogOpen;
        private string newProjectNo = string.Empty;
        private string newProjectName = string.Empty;
        private string projectError = string.Empty;

        private bool isStockDialogOpen;
        private string stockParentFolder = string.Empty;
        private string newStockNo = string.Empty;
        private string newStockName = string.Empty;
        private string stockError = string.Empty;

        /// <summary>Komutları hazırlar.</summary>
        public MainViewModel()
        {
            RefreshCommand = new RelayCommand(Refresh);
            NewProjectCommand = new RelayCommand(OpenNewProject);
            NewStockCommand = new RelayCommand(OpenNewStock);
            OpenFolderCommand = new RelayCommand(OpenSelectedFolder);

            settings = AppSettings.Load();
            rootPath = settings.RootPath ?? string.Empty;
            Refresh();
        }

        private readonly AppSettings settings;

        #region Kök ve ağaç

        /// <summary>Projelerin durduğu kök klasör. Değişince ağaç yeniden okunur.</summary>
        public string RootPath
        {
            get { return rootPath; }
            set
            {
                if (SetProperty(ref rootPath, value ?? string.Empty))
                {
                    settings.RootPath = rootPath;
                    settings.Save();
                    Refresh();
                }
            }
        }

        /// <summary>Ağacın üst düzey düğümleri.</summary>
        public ObservableCollection<TreeNodeViewModel> Nodes { get; } = new ObservableCollection<TreeNodeViewModel>();

        /// <summary>Kök seçili değilken ağaç yerine ipucu gösterilir.</summary>
        public bool HasRoot
        {
            get { return Directory.Exists(rootPath); }
        }

        /// <summary>Ağaçta seçili düğüm.</summary>
        public TreeNodeViewModel? SelectedNode
        {
            get { return selectedNode; }
            set
            {
                if (SetProperty(ref selectedNode, value))
                {
                    OnPropertyChanged(nameof(HasSelection));
                    OnPropertyChanged(nameof(NewStockMenuText));
                }
            }
        }

        /// <summary>Bir düğüm seçili mi.</summary>
        public bool HasSelection
        {
            get { return selectedNode != null; }
        }

        /// <summary>Alt çubuktaki durum metni.</summary>
        public string Status
        {
            get { return status; }
            private set { SetProperty(ref status, value); }
        }

        /// <summary>Ağacı yeniden okur.</summary>
        public ICommand RefreshCommand { get; }

        /// <summary>Kök klasörden iki düzey okur: projeler (ve diğer klasörler) ile altlarındaki stoklar.</summary>
        public void Refresh()
        {
            var expanded = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            foreach (var node in Nodes)
            {
                if (node.IsExpanded)
                {
                    expanded.Add(node.Path);
                }
            }

            Nodes.Clear();
            SelectedNode = null;
            OnPropertyChanged(nameof(HasRoot));
            if (!HasRoot)
            {
                return;
            }

            foreach (var entry in FolderReader.ListChildren(rootPath))
            {
                var node = new TreeNodeViewModel(entry);
                foreach (var child in FolderReader.ListChildren(entry.Path))
                {
                    node.Children.Add(new TreeNodeViewModel(child));
                }

                node.IsExpanded = expanded.Contains(entry.Path);
                Nodes.Add(node);
            }
        }

        #endregion

        #region Yeni proje

        /// <summary>Yeni proje diyaloğunu açar.</summary>
        public ICommand NewProjectCommand { get; }

        /// <summary>Yeni proje diyaloğu açık mı.</summary>
        public bool IsProjectDialogOpen
        {
            get { return isProjectDialogOpen; }
            set { SetProperty(ref isProjectDialogOpen, value); }
        }

        /// <summary>Girilen proje numarası.</summary>
        public string NewProjectNo
        {
            get { return newProjectNo; }
            set { SetProperty(ref newProjectNo, value); }
        }

        /// <summary>Girilen proje adı.</summary>
        public string NewProjectName
        {
            get { return newProjectName; }
            set { SetProperty(ref newProjectName, value); }
        }

        /// <summary>Diyalogdaki hata metni; boşsa gizlidir.</summary>
        public string ProjectError
        {
            get { return projectError; }
            private set { SetProperty(ref projectError, value); }
        }

        private void OpenNewProject()
        {
            if (!HasRoot)
            {
                Status = Resources.Status_SelectRootFirst;
                return;
            }

            NewProjectNo = string.Empty;
            NewProjectName = string.Empty;
            ProjectError = string.Empty;
            IsProjectDialogOpen = true;
        }

        /// <summary>
        /// Diyalogdaki değerlerle projeyi oluşturur. Başarısızsa <c>false</c> döner ve hata metnini doldurur;
        /// çağıran diyaloğu yeniden açar.
        /// </summary>
        public bool ConfirmNewProject()
        {
            var result = ProjectService.CreateProject(rootPath, NewProjectNo, NewProjectName);
            if (!result.Succeeded)
            {
                ProjectError = ErrorText(result);
                return false;
            }

            Refresh();
            Status = string.Format(Resources.Status_ProjectCreated, result.Folder);
            return true;
        }

        #endregion

        #region Yeni stok

        /// <summary>Yeni stok diyaloğunu açar (seçili projenin altına).</summary>
        public ICommand NewStockCommand { get; }

        /// <summary>Yeni stok diyaloğu açık mı.</summary>
        public bool IsStockDialogOpen
        {
            get { return isStockDialogOpen; }
            set { SetProperty(ref isStockDialogOpen, value); }
        }

        /// <summary>Yeni stok diyaloğunun başlığı; stokun ekleneceği projeyi de söyler.</summary>
        public string StockDialogTitle
        {
            get { return string.Format(Resources.Dialog_NewStockTitleFor, Path.GetFileName(stockParentFolder)); }
        }

        /// <summary>Menüdeki "Yeni stok" metni; seçime göre hedef projeyi gösterir.</summary>
        public string NewStockMenuText
        {
            get
            {
                var project = SelectedProjectFolder();
                if (project.Length == 0)
                {
                    return Resources.Menu_NewStock;
                }

                return string.Format(Resources.Menu_NewStockFor, Path.GetFileName(project));
            }
        }

        /// <summary>Girilen stok numarası.</summary>
        public string NewStockNo
        {
            get { return newStockNo; }
            set { SetProperty(ref newStockNo, value); }
        }

        /// <summary>Girilen stok adı (boş olabilir).</summary>
        public string NewStockName
        {
            get { return newStockName; }
            set { SetProperty(ref newStockName, value); }
        }

        /// <summary>Diyalogdaki hata metni; boşsa gizlidir.</summary>
        public string StockError
        {
            get { return stockError; }
            private set { SetProperty(ref stockError, value); }
        }

        private void OpenNewStock()
        {
            var project = SelectedProjectFolder();
            if (project.Length == 0)
            {
                Status = Resources.Status_SelectProjectFirst;
                return;
            }

            stockParentFolder = project;
            OnPropertyChanged(nameof(StockDialogTitle));
            NewStockNo = string.Empty;
            NewStockName = string.Empty;
            StockError = string.Empty;
            IsStockDialogOpen = true;
        }

        /// <summary>
        /// Diyalogdaki değerlerle stoku oluşturur. Başarısızsa <c>false</c> döner ve hata metnini doldurur.
        /// </summary>
        public bool ConfirmNewStock()
        {
            var result = ProjectService.CreateStock(stockParentFolder, NewStockNo, NewStockName);
            if (!result.Succeeded)
            {
                StockError = ErrorText(result);
                return false;
            }

            Refresh();
            foreach (var node in Nodes)
            {
                if (string.Equals(node.Path, stockParentFolder, System.StringComparison.OrdinalIgnoreCase))
                {
                    node.IsExpanded = true;
                }
            }

            Status = string.Format(Resources.Status_StockCreated, result.Folder);
            return true;
        }

        /// <summary>Seçili düğüm proje ise onun, projenin altındaki stok ise üstünün yolu; yoksa boş.</summary>
        private string SelectedProjectFolder()
        {
            if (selectedNode == null)
            {
                return string.Empty;
            }

            if (selectedNode.Kind == FolderKind.Project)
            {
                return selectedNode.Path;
            }

            var parent = Path.GetDirectoryName(selectedNode.Path) ?? string.Empty;
            if (parent.Length > 0 && FolderReader.KindOf(parent) == FolderKind.Project)
            {
                return parent;
            }

            return string.Empty;
        }

        #endregion

        #region Klasörü aç

        /// <summary>Seçili klasörü Windows Gezgini'nde açar.</summary>
        public ICommand OpenFolderCommand { get; }

        private void OpenSelectedFolder()
        {
            if (selectedNode == null || !Directory.Exists(selectedNode.Path))
            {
                return;
            }

            System.Diagnostics.Process.Start("explorer.exe", "\"" + selectedNode.Path + "\"");
        }

        #endregion

        private static string ErrorText(CreateResult result)
        {
            switch (result.Error)
            {
                case CreateError.ParentMissing:
                    return Resources.Error_ParentMissing;
                case CreateError.NumberEmpty:
                    return Resources.Error_NumberEmpty;
                case CreateError.NameEmpty:
                    return Resources.Error_NameEmpty;
                case CreateError.InvalidCharacters:
                    return Resources.Error_InvalidCharacters;
                case CreateError.AlreadyExists:
                    return string.Format(Resources.Error_AlreadyExists, result.Folder);
                default:
                    return string.Format(Resources.Error_Io, result.Detail);
            }
        }
    }
}
