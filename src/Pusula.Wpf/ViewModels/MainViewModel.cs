using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Pusula.Core.Templates;
using Pusula.Core.Tracking;
using Pusula.Core.Types;
using Pusula.Wpf.Mvvm;
using Pusula.Wpf.Properties;

namespace Pusula.Wpf.ViewModels
{
    /// <summary>
    /// Ana pencerenin durumu: kök klasör, ağaç, seçili düğüm, yeni proje/stok diyalogları.
    /// </summary>
    public sealed partial class MainViewModel : ViewModelBase
    {
        private string rootPath = string.Empty;
        private string refreshedRoot = string.Empty;
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
        private string selectedTemplate = string.Empty;

        private bool isModuleDialogOpen;
        private string moduleStockFolder = string.Empty;

        private TemplateSet templates = TemplateSet.CreateDefault();
        private TypeSet types = TypeSet.CreateDefault();

        /// <summary>Komutları hazırlar.</summary>
        public MainViewModel()
        {
            RefreshCommand = new RelayCommand(Refresh);
            NewProjectCommand = new RelayCommand(OpenNewProject);
            NewStockCommand = new RelayCommand(OpenNewStock);
            OpenFolderCommand = new RelayCommand(OpenSelectedFolder);
            AddModulesCommand = new RelayCommand(OpenAddModules);
            MarkAsStockCommand = new RelayCommand(() => OpenMark(true));
            MarkAsProjectCommand = new RelayCommand(() => OpenMark(false));
            MarkAsInventoryCommand = new RelayCommand(OpenMarkInventory);

            settings = AppSettings.Load();
            rootPath = settings.RootPath ?? string.Empty;
            Refresh();
        }

        private readonly AppSettings settings;

        #region Kök ve ağaç

        private string searchText = string.Empty;

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

        /// <summary>Seçili tema; değişince uygulanır ve ayarlara yazılır.</summary>
        public Palette Theme
        {
            get { return Themes.Current ?? Themes.Find(settings.Theme); }
            set
            {
                if (value == null || ReferenceEquals(value, Themes.Current))
                {
                    return;
                }

                Themes.Apply(value);
                settings.Theme = value.Key;
                settings.Save();
                OnPropertyChanged();
            }
        }

        /// <summary>Seçilebilen temalar.</summary>
        public Palette[] AllThemes
        {
            get { return Themes.All; }
        }

        /// <summary>Ağacın üst düzey düğümleri.</summary>
        public ObservableCollection<TreeNodeViewModel> Nodes { get; } = new ObservableCollection<TreeNodeViewModel>();

        /// <summary>Üst ağaçta gösterilen düğümler (projeler ve diğer klasörler).</summary>
        public ObservableCollection<TreeNodeViewModel> ProjectNodes { get; } = new ObservableCollection<TreeNodeViewModel>();

        /// <summary>Alt bölmedeki envanter ağacı (tek düğüm: Envanter).</summary>
        public ObservableCollection<TreeNodeViewModel> InventoryNodes { get; } = new ObservableCollection<TreeNodeViewModel>();

        /// <summary>Envanter bölmesi gösterilir mi.</summary>
        public bool HasInventory
        {
            get { return InventoryNodes.Count > 0; }
        }

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
                var previous = selectedNode;
                if (SetProperty(ref selectedNode, value))
                {
                    // Öbür ağaçta kalan eski seçim kaldırılır.
                    if (previous != null && previous.IsSelected)
                    {
                        previous.IsSelected = false;
                    }

                    OnPropertyChanged(nameof(HasSelection));
                    OnPropertyChanged(nameof(NewStockMenuText));
                    OnPropertyChanged(nameof(CanMarkAsStock));
                    OnPropertyChanged(nameof(CanMarkAsProject));
                    OnPropertyChanged(nameof(CanMarkAsInventory));
                    OnPropertyChanged(nameof(MarkInventoryMenuText));
                    Detail.InventoryItems = inventoryItems;
                    Detail.Load(selectedNode, templates, types);
                    Inventory.Load(selectedNode, inventoryItems, types);
                }
            }
        }

        /// <summary>Seçili düğümün özeti (sağ panel).</summary>
        public DetailViewModel Detail { get; } = new DetailViewModel();

        private DateTime lastDialogClosed = DateTime.MinValue;
        private int openDialogs;

        /// <summary>
        /// Bir diyalog açık mı ya da az önce mi kapandı. Diyalog kapanırken tıklama arkadaki tabloya geçip
        /// "çift tıklayınca klasörü aç"ı tetiklemesin diye kullanılır.
        /// </summary>
        public bool IsDialogBusy
        {
            get { return openDialogs > 0 || (DateTime.Now - lastDialogClosed).TotalMilliseconds < 600; }
        }

        /// <summary>Menüden bir seçim yapıldı; hemen ardından gelen çift tıklama yok sayılır.</summary>
        public void NoteMenuChoice()
        {
            lastDialogClosed = DateTime.Now;
        }

        private void SetDialogOpen(ref bool field, bool value, [System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
        {
            if (field == value)
            {
                return;
            }

            openDialogs = Math.Max(0, openDialogs + (value ? 1 : -1));
            if (!value)
            {
                lastDialogClosed = DateTime.Now;
            }

            SetProperty(ref field, value, propertyName);
        }
        /// <summary>Klasörün altındaki <paramref name="sub"/> klasörünü açar; yoksa klasörün kendisini.</summary>
        public void OpenSubFolderOrFolder(string folder, string sub)
        {
            var path = Path.Combine(folder, sub);
            OpenPath(Directory.Exists(path) ? path : folder);
        }

        /// <summary>Notu varsayılan programla açar (Obsidian ya da metin düzenleyici); not yoksa klasörü açar.</summary>
        public void OpenNoteOrFolder(string notePath, string folder)
        {
            if (!string.IsNullOrEmpty(notePath) && File.Exists(notePath))
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(notePath) { UseShellExecute = true });
                    return;
                }
                catch (System.ComponentModel.Win32Exception)
                {
                    // .md için ilişkili program yoksa klasörü açarız.
                }
            }

            OpenPath(folder);
        }

        /// <summary>Bir satırın klasörünü Windows Gezgini'nde açar (yoksa bir şey yapmaz).</summary>
        public void OpenPath(string path)
        {
            if (Directory.Exists(path))
            {
                System.Diagnostics.Process.Start("explorer.exe", "\"" + path + "\"");
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
            // Açık düğümler yenilemede korunur; kök değiştiyse eski kökün düğümleri sayılmaz.
            var expanded = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            var sameRoot = string.Equals(refreshedRoot, rootPath, System.StringComparison.OrdinalIgnoreCase);
            refreshedRoot = rootPath;

            // Seçili düğüm de korunur: yenileyince en başa atılmaz, aynı stok / kayıt yeniden okunup gösterilir.
            var keepSelected = sameRoot && selectedNode != null ? selectedNode.Path : null;
            var pending = new Stack<TreeNodeViewModel>(sameRoot ? Nodes : Enumerable.Empty<TreeNodeViewModel>());
            while (pending.Count > 0)
            {
                var node = pending.Pop();
                if (node.IsExpanded)
                {
                    expanded.Add(node.Path);
                }

                foreach (var child in node.Children)
                {
                    pending.Push(child);
                }
            }

            Nodes.Clear();
            ProjectNodes.Clear();
            InventoryNodes.Clear();
            SelectedNode = null;
            OnPropertyChanged(nameof(HasRoot));
            if (!HasRoot)
            {
                return;
            }

            templates = TemplateStore.LoadOrCreate(rootPath);
            types = TypeStore.LoadOrCreate(rootPath);
            foreach (var entry in FolderReader.ListChildren(rootPath))
            {
                if (IsGeneralInventoryRoot(entry.Path))
                {
                    continue;
                }

                var node = new TreeNodeViewModel(entry);
                foreach (var child in FolderReader.ListChildren(entry.Path))
                {
                    node.Children.Add(new TreeNodeViewModel(child));
                }

                node.IsExpanded = expanded.Contains(entry.Path);
                Nodes.Add(node);
                ProjectNodes.Add(node);
            }

            AddInventoryNodes(expanded);
            OnPropertyChanged(nameof(HasInventory));
            ApplySearch();
            if (keepSelected != null)
            {
                SelectNodeByPath(keepSelected);
            }
        }

        /// <summary>Ağaçta aranan metin (numara, ad, tanım); boşsa hepsi görünür.</summary>
        public string SearchText
        {
            get { return searchText; }
            set
            {
                if (SetProperty(ref searchText, value ?? string.Empty))
                {
                    ApplySearch();
                }
            }
        }

        private void ApplySearch()
        {
            var text = searchText.Trim();
            foreach (var node in Nodes)
            {
                node.ApplySearch(text);
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
            set { SetDialogOpen(ref isProjectDialogOpen, value); }
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
            var result = ProjectService.CreateProject(rootPath, NewProjectNo, NewProjectName, templates.ProjectFolders);
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
            set { SetDialogOpen(ref isStockDialogOpen, value); }
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

        /// <summary>Kabul edilen stok numarası biçimleri (örnekle): "AA-0000-0000 · JY-0000-0000".</summary>
        public string StockNumberHint
        {
            get
            {
                var examples = types.Stock.NumberFormats.Select(f => NumberPattern.Example(f.Pattern) + " (" + f.Label + ")");
                return string.Join(" · ", examples);
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

        /// <summary>Seçilebilir şablon adları.</summary>
        public ObservableCollection<string> TemplateNames { get; } = new ObservableCollection<string>();

        /// <summary>Seçili şablon; değişince modül onay kutuları şablona göre işaretlenir.</summary>
        public string SelectedTemplate
        {
            get { return selectedTemplate; }
            set
            {
                if (SetProperty(ref selectedTemplate, value ?? string.Empty))
                {
                    ApplyTemplate();
                }
            }
        }

        /// <summary>Yeni stok diyaloğundaki modül onay kutuları.</summary>
        public ObservableCollection<ModuleOptionViewModel> StockModules { get; } = new ObservableCollection<ModuleOptionViewModel>();

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
            OnPropertyChanged(nameof(StockNumberHint));

            StockModules.Clear();
            foreach (var module in templates.Modules)
            {
                StockModules.Add(new ModuleOptionViewModel(module, false));
            }

            TemplateNames.Clear();
            foreach (var preset in templates.Templates)
            {
                TemplateNames.Add(preset.Name);
            }

            selectedTemplate = string.Empty;
            SelectedTemplate = TemplateNames.FirstOrDefault() ?? string.Empty;
            IsStockDialogOpen = true;
        }

        /// <summary>
        /// Diyalogdaki değerlerle stoku oluşturur. Başarısızsa <c>false</c> döner ve hata metnini doldurur.
        /// </summary>
        public bool ConfirmNewStock()
        {
            if (!types.Stock.AcceptsNumber(NewStockNo))
            {
                StockError = string.Format(Resources.Error_StockNumberFormat, StockNumberHint);
                return false;
            }

            var result = ProjectService.CreateStock(
                stockParentFolder,
                NewStockNo,
                NewStockName,
                SelectedTemplate,
                templates.StockFoldersFor(StockModules.Where(m => m.IsChecked).Select(m => m.Module.Key)),
                types.Stock.InitialStatus);
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

        private void ApplyTemplate()
        {
            var preset = templates.Templates.FirstOrDefault(t => t.Name == selectedTemplate);
            var keys = new HashSet<string>(preset == null ? Enumerable.Empty<string>() : preset.Modules, System.StringComparer.OrdinalIgnoreCase);
            foreach (var option in StockModules)
            {
                option.IsChecked = keys.Contains(option.Module.Key);
            }
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

        #region Şablonlar

        /// <summary>Geçerli kökün şablonları.</summary>
        public TemplateSet Templates
        {
            get { return templates; }
        }

        /// <summary>Şablon düzenleyici kapanınca çağrılır; şablonları dosyadan yeniden okur.</summary>
        public void ReloadTemplates()
        {
            if (HasRoot)
            {
                templates = TemplateStore.LoadOrCreate(rootPath);
                Status = Resources.Status_TemplatesSaved;
            }
        }

        /// <summary>Türler kaydedildikten sonra ağacı ve sayfayı yeniden kurar.</summary>
        public void ReloadTypes()
        {
            Refresh();
            Status = Resources.Status_TypesSaved;
        }

        /// <summary>Şablon düzenleyici açılabilir mi; değilse durum metnini doldurur.</summary>
        public bool CanEditTemplates()
        {
            if (!HasRoot)
            {
                Status = Resources.Status_SelectRootFirst;
                return false;
            }

            templates = TemplateStore.LoadOrCreate(rootPath);
            return true;
        }

        #endregion

        #region İşaretle (elle açılmış klasörler)

        private bool isMarkDialogOpen;
        private bool markIsStock;
        private string markFolder = string.Empty;
        private string markNumber = string.Empty;
        private string markName = string.Empty;
        private string markError = string.Empty;

        /// <summary>Seçili klasör stok olarak işaretlenebilir mi (işaretsiz klasör).</summary>
        public bool CanMarkAsStock
        {
            get { return selectedNode != null && selectedNode.Kind == FolderKind.Plain && !selectedNode.IsUnmarkedInventory; }
        }

        /// <summary>Seçili klasör proje olarak işaretlenebilir mi (kökün hemen altındaki işaretsiz klasör).</summary>
        public bool CanMarkAsProject
        {
            get
            {
                return CanMarkAsStock
                    && string.Equals(Path.GetDirectoryName(selectedNode!.Path), rootPath.TrimEnd('\\'), System.StringComparison.OrdinalIgnoreCase);
            }
        }

        /// <summary>Seçili klasörü stok olarak işaretleme diyaloğunu açar.</summary>
        public ICommand MarkAsStockCommand { get; }

        /// <summary>Seçili klasörü proje olarak işaretleme diyaloğunu açar.</summary>
        public ICommand MarkAsProjectCommand { get; }

        /// <summary>İşaretleme diyaloğu açık mı.</summary>
        public bool IsMarkDialogOpen
        {
            get { return isMarkDialogOpen; }
            set { SetDialogOpen(ref isMarkDialogOpen, value); }
        }

        /// <summary>Diyalog stok için mi (değilse proje).</summary>
        public bool MarkIsStock
        {
            get { return markIsStock; }
            private set { SetProperty(ref markIsStock, value); }
        }

        /// <summary>Diyalog başlığı.</summary>
        public string MarkTitle
        {
            get { return string.Format(markIsStock ? Resources.Mark_StockTitle : Resources.Mark_ProjectTitle, Path.GetFileName(markFolder)); }
        }

        /// <summary>Numara alanının başlığı.</summary>
        public string MarkNumberLabel
        {
            get { return markIsStock ? Resources.Field_StockNo : Resources.Field_ProjectNo; }
        }

        /// <summary>Ad alanının başlığı.</summary>
        public string MarkNameLabel
        {
            get { return markIsStock ? Resources.Field_StockName : Resources.Field_ProjectName; }
        }

        /// <summary>Girilen numara.</summary>
        public string MarkNumber
        {
            get { return markNumber; }
            set { SetProperty(ref markNumber, value ?? string.Empty); }
        }

        /// <summary>Girilen ad.</summary>
        public string MarkName
        {
            get { return markName; }
            set { SetProperty(ref markName, value ?? string.Empty); }
        }

        /// <summary>Diyalogdaki hata; boşsa gizlidir.</summary>
        public string MarkError
        {
            get { return markError; }
            private set { SetProperty(ref markError, value); }
        }

        private void OpenMark(bool asStock)
        {
            if (selectedNode == null || selectedNode.Kind != FolderKind.Plain || (!asStock && !CanMarkAsProject))
            {
                return;
            }

            markFolder = selectedNode.Path;
            MarkIsStock = asStock;
            var folderName = Path.GetFileName(markFolder);
            if (asStock)
            {
                MarkNumber = folderName;
                MarkName = string.Empty;
            }
            else
            {
                var match = System.Text.RegularExpressions.Regex.Match(folderName, @"^(\d+)_(.+)$");
                MarkNumber = match.Success ? match.Groups[1].Value : string.Empty;
                MarkName = match.Success ? match.Groups[2].Value : folderName;
            }

            MarkError = string.Empty;
            OnPropertyChanged(nameof(MarkTitle));
            OnPropertyChanged(nameof(MarkNumberLabel));
            OnPropertyChanged(nameof(MarkNameLabel));
            OnPropertyChanged(nameof(StockNumberHint));
            IsMarkDialogOpen = true;
        }

        /// <summary>
        /// Klasörü işaretler. Başarısızsa <c>false</c> döner ve hata metnini doldurur; çağıran diyaloğu yeniden açar.
        /// </summary>
        public bool ConfirmMark()
        {
            if (markIsStock && !types.Stock.AcceptsNumber(MarkNumber))
            {
                MarkError = string.Format(Resources.Error_StockNumberFormat, StockNumberHint);
                return false;
            }

            var result = markIsStock
                ? ProjectService.MarkAsStock(markFolder, MarkNumber, MarkName, types.Stock.InitialStatus)
                : ProjectService.MarkAsProject(markFolder, MarkNumber, MarkName);
            if (!result.Succeeded)
            {
                MarkError = ErrorText(result);
                return false;
            }

            var parent = Path.GetDirectoryName(markFolder) ?? string.Empty;
            Refresh();
            foreach (var node in Nodes)
            {
                if (string.Equals(node.Path, parent, System.StringComparison.OrdinalIgnoreCase)
                    || string.Equals(node.Path, markFolder, System.StringComparison.OrdinalIgnoreCase))
                {
                    node.IsExpanded = true;
                }
            }

            Status = string.Format(markIsStock ? Resources.Status_MarkedStock : Resources.Status_MarkedProject, markFolder);
            return true;
        }

        #endregion

        #region Durum

        /// <summary>Geçerli kökün tür kalıpları.</summary>
        public TypeSet Types
        {
            get { return types; }
        }

        private bool isStatusNoteOpen;
        private string pendingStatus = string.Empty;
        private string statusNoteText = string.Empty;

        /// <summary>Not isteyen durum (Beklemede gibi) seçilince açılan diyalog.</summary>
        public bool IsStatusNoteOpen
        {
            get { return isStatusNoteOpen; }
            set { SetDialogOpen(ref isStatusNoteOpen, value); }
        }

        /// <summary>Not diyaloğunun başlığı.</summary>
        public string StatusNoteTitle
        {
            get { return pendingStatus; }
        }

        /// <summary>Girilen not.</summary>
        public string StatusNoteText
        {
            get { return statusNoteText; }
            set { SetProperty(ref statusNoteText, value ?? string.Empty); }
        }

        /// <summary>Not isteyen bir durum seçildi: not diyaloğunu açar.</summary>
        public void AskStatusNote(string status)
        {
            pendingStatus = status;
            OnPropertyChanged(nameof(StatusNoteTitle));
            StatusNoteText = string.Empty;
            IsStatusNoteOpen = true;
        }

        /// <summary>Not diyaloğu onaylandı: durumu notla birlikte yazar.</summary>
        public void ConfirmStatusNote()
        {
            SetStockStatus(pendingStatus, StatusNoteText.Trim());
        }

        /// <summary>Gösterilen stokun durumunu değiştirir ve özeti yeniler.</summary>
        public void SetStockStatus(string status, string note)
        {
            var path = Detail.StockPath;
            if (path.Length == 0)
            {
                return;
            }

            if (ProjectService.SetStockStatus(path, status, note))
            {
                Status = string.Format(Resources.Status_StatusChanged, Path.GetFileName(path), status);
                Detail.Reload();
            }
            else
            {
                Status = string.Format(Resources.Error_Io, path);
            }
        }

        #endregion

        #region Modül ekle

        /// <summary>Seçili stoka eksik modül klasörlerini ekleme diyaloğunu açar.</summary>
        public ICommand AddModulesCommand { get; }

        /// <summary>Modül ekle diyaloğu açık mı.</summary>
        public bool IsModuleDialogOpen
        {
            get { return isModuleDialogOpen; }
            set { SetDialogOpen(ref isModuleDialogOpen, value); }
        }

        /// <summary>Modül ekle diyaloğunun başlığı.</summary>
        public string ModuleDialogTitle
        {
            get { return string.Format(Resources.Dialog_AddModulesTitle, Path.GetFileName(moduleStockFolder)); }
        }

        /// <summary>Modül ekle diyaloğundaki onay kutuları; stokta var olanlar işaretli ve kilitli gelir.</summary>
        public ObservableCollection<ModuleOptionViewModel> AddableModules { get; } = new ObservableCollection<ModuleOptionViewModel>();

        private void OpenAddModules()
        {
            if (selectedNode == null || selectedNode.Kind != FolderKind.Stock)
            {
                Status = Resources.Status_SelectStockFirst;
                return;
            }

            moduleStockFolder = selectedNode.Path;
            OnPropertyChanged(nameof(ModuleDialogTitle));
            AddableModules.Clear();
            foreach (var module in templates.Modules)
            {
                AddableModules.Add(new ModuleOptionViewModel(module, Directory.Exists(Path.Combine(moduleStockFolder, module.Folder))));
            }

            IsModuleDialogOpen = true;
        }

        /// <summary>İşaretlenen yeni modüllerin klasörlerini açar.</summary>
        public void ConfirmAddModules()
        {
            var keys = AddableModules.Where(m => m.IsChecked && !m.Exists).Select(m => m.Module.Key).ToList();
            if (keys.Count == 0)
            {
                Status = Resources.Status_NothingToAdd;
                return;
            }

            var folders = templates.Modules.Where(m => keys.Contains(m.Key)).SelectMany(m => m.AllFolders());
            try
            {
                var created = ProjectService.AddFolders(moduleStockFolder, folders);
                Status = string.Format(Resources.Status_ModulesAdded, created, Path.GetFileName(moduleStockFolder));
            }
            catch (IOException ex)
            {
                Status = string.Format(Resources.Error_Io, ex.Message);
            }
            catch (System.UnauthorizedAccessException ex)
            {
                Status = string.Format(Resources.Error_Io, ex.Message);
            }
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
                case CreateError.AlreadyMarked:
                    return Resources.Error_AlreadyMarked;
                case CreateError.AlreadyExists:
                    return string.Format(Resources.Error_AlreadyExists, result.Folder);
                default:
                    return string.Format(Resources.Error_Io, result.Detail);
            }
        }
    }
}
