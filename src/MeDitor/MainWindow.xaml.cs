using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using DesignKit.Controls;
using MeDitor.Markdown;
using MeDitor.ViewModels;
using Microsoft.Win32;
using Res = MeDitor.Properties.Resources;

namespace MeDitor
{
    /// <summary>
    /// MeDitor ana penceresi: Dosya / Görünüm menüsü, sekmeler (her md bir sekme), klasör kenar çubuğu, boş ekran
    /// (son açılanlar), tam ekran, kapatırken kaydedilmemiş sekmeleri sorma ve oturumu (açık sekmeler) saklama.
    /// </summary>
    public partial class MainWindow : ChromeWindow
    {
        /// <summary><see cref="HasFolder"/> bağlanabilir özelliği.</summary>
        public static readonly DependencyProperty HasFolderProperty =
            DependencyProperty.Register(nameof(HasFolder), typeof(bool), typeof(MainWindow), new PropertyMetadata(false));

        private readonly List<EditorView> tabs = new List<EditorView>();
        private readonly Queue<EditorView> closeQueue = new Queue<EditorView>();
        private List<FolderNode> folderNodes = new List<FolderNode>();
        private List<NoteSuggestion> folderSuggestions = new List<NoteSuggestion>();
        private Dictionary<string, string>? nameIndex;
        private EditorView? closing;
        private bool closingWindow;
        private bool allowClose;
        private bool fullScreen;
        private WindowState stateBeforeFullScreen;
        private bool suppressTreeOpen;

        /// <summary>Pencereyi kurar; ayarlardaki kenar çubuğu, klasör ve tam ekran durumunu uygular.</summary>
        public MainWindow()
        {
            InitializeComponent();
            var settings = App.Settings;
            SidebarColumn.Width = new GridLength(Math.Max(160, Math.Min(600, settings.SidebarWidth)));
            ApplySidebar(settings.ShowSidebar);
            if ((settings.Editor ?? new EditorSettings()).Maximized)
            {
                WindowState = WindowState.Maximized;
            }

            if (settings.Folder.Length > 0 && Directory.Exists(settings.Folder))
            {
                LoadFolder(settings.Folder);
            }

            Activated += (s, e) => Active?.CheckExternalNow();
            Closing += MainWindow_Closing;
            UpdateEmpty();
        }

        /// <summary>Kenar çubuğunda klasör açık mı.</summary>
        public bool HasFolder
        {
            get { return (bool)GetValue(HasFolderProperty); }
            set { SetValue(HasFolderProperty, value); }
        }

        /// <summary>Açık sekmeler (testler için).</summary>
        public IReadOnlyList<EditorView> Tabs
        {
            get { return tabs; }
        }

        /// <summary>Seçili sekme; yoksa <c>null</c>.</summary>
        public EditorView? Active
        {
            get { return TabStrip.SelectedItem is EditorViewModel vm ? tabs.FirstOrDefault(t => t.ViewModel == vm) : null; }
        }

        /// <summary>Kenar çubuğunda açık klasör; yoksa boş.</summary>
        public string Folder { get; private set; } = string.Empty;

        /// <summary>Dosyayı yeni sekmede açar; zaten açıksa o sekmeyi seçer. Yol boşsa adsız not. Açılan sekmeyi döndürür.</summary>
        public EditorView Open(string path)
        {
            path = path ?? string.Empty;
            if (path.Length > 0)
            {
                path = Path.GetFullPath(path);
                var existing = tabs.FirstOrDefault(t => string.Equals(t.ViewModel.Path, path, StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                {
                    Select(existing);
                    return existing;
                }

                // Boş, değişmemiş tek adsız sekme varsa yerine açılır.
                var blank = tabs.Count == 1 && tabs[0].ViewModel.IsUntitled && !tabs[0].ViewModel.IsDirty && tabs[0].ViewModel.Text.Length == 0 ? tabs[0] : null;
                if (blank != null)
                {
                    RemoveTab(blank);
                }

                App.Settings.AddRecent(path);
                App.Settings.Save();
            }

            var settings = App.Settings.Editor ?? (App.Settings.Editor = new EditorSettings());
            var viewModel = new EditorViewModel(path, Folder, settings, App.Settings.Save, Suggestions);
            var view = new EditorView(viewModel, Navigate, App.OpenShell, App.OpenShell);
            view.SaveHandler = () => Save(view);
            view.TagProvider = TagSuggestions;
            view.IsFullScreen = () => fullScreen;
            view.FullScreenRequested += (s, e) => ToggleFullScreen();
            view.Visibility = Visibility.Collapsed;
            viewModel.PropertyChanged += (s, e) =>
            {
                if (view == Active && (e.PropertyName == nameof(EditorViewModel.TabTitle) || e.PropertyName == nameof(EditorViewModel.FileName)))
                {
                    UpdateTitle();
                }
            };
            tabs.Add(view);
            Host.Children.Add(view);
            TabStrip.Items.Add(viewModel);
            Select(view);
            UpdateEmpty();
            return view;
        }

        /// <summary>Sekmeyi seçer (sekme şeridinde dosyanın görünüm modeli durur, düzenleyici içerik alanındadır).</summary>
        public void Select(EditorView? view)
        {
            TabStrip.SelectedItem = view?.ViewModel;
        }

        // Sekme şeridindeki bir öğenin (görünüm modeli) düzenleyicisi.
        private EditorView? ViewOf(object sender)
        {
            var vm = (sender as FrameworkElement)?.DataContext as EditorViewModel;
            return vm == null ? null : tabs.FirstOrDefault(t => t.ViewModel == vm);
        }

        /// <summary>Oturumu (açık sekmeler, seçili sekme) ayarlara yazar.</summary>
        public void SaveSession()
        {
            App.Settings.Session = tabs.Where(t => !t.ViewModel.IsUntitled).Select(t => t.ViewModel.Path).ToList();
            App.Settings.ActiveFile = Active != null && !Active.ViewModel.IsUntitled ? Active.ViewModel.Path : string.Empty;
            App.Settings.Folder = Folder;
            App.Settings.ShowSidebar = Sidebar.Visibility == Visibility.Visible;
            App.Settings.Save();
        }

        /// <summary>Önceki oturumun sekmelerini açar (bulunamayanlar atlanır).</summary>
        public void RestoreSession()
        {
            foreach (var path in App.Settings.Session.Where(File.Exists).ToList())
            {
                Open(path);
            }

            var active = tabs.FirstOrDefault(t => string.Equals(t.ViewModel.Path, App.Settings.ActiveFile, StringComparison.OrdinalIgnoreCase));
            if (active != null)
            {
                Select(active);
            }
        }

        /// <summary>Sekmeyi kaydeder; adsızsa yer sorar. Kaydedildiyse <c>true</c>.</summary>
        public bool Save(EditorView view)
        {
            return view.ViewModel.IsUntitled ? SaveAs(view) : view.ViewModel.Save();
        }

        /// <summary>Sekmeyi yeni bir yere kaydeder (yer sorulur). Kaydedildiyse <c>true</c>.</summary>
        public bool SaveAs(EditorView view)
        {
            var dialog = new SaveFileDialog
            {
                Filter = Res.Main_MdFilter,
                DefaultExt = ".md",
                AddExtension = true,
                FileName = view.ViewModel.IsUntitled ? FirstHeading(view.ViewModel.Text) : Path.GetFileName(view.ViewModel.Path),
                InitialDirectory = view.ViewModel.IsUntitled ? (Folder.Length > 0 ? Folder : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)) : view.ViewModel.Folder,
            };
            if (dialog.ShowDialog(this) != true)
            {
                return false;
            }

            if (!view.ViewModel.SaveAs(dialog.FileName))
            {
                return false;
            }

            App.Settings.AddRecent(dialog.FileName);
            App.Settings.Save();
            if (Folder.Length > 0 && dialog.FileName.StartsWith(Folder, StringComparison.OrdinalIgnoreCase))
            {
                RefreshFolder();
            }

            UpdateTitle();
            return true;
        }

        /// <summary>Sekmeyi kapatır; kaydedilmemişse önce sorar.</summary>
        public void CloseTab(EditorView view)
        {
            if (!view.ViewModel.IsDirty)
            {
                RemoveTab(view);
                return;
            }

            AskToClose(view);
        }

        // [[hedef]] tıklanınca: açık klasörde (yoksa notun klasöründe) bu adla md varsa yeni sekmede açılır.
        private bool Navigate(string target)
        {
            var name = (target ?? string.Empty).Split('#', '|')[0].Trim();
            if (name.Length == 0)
            {
                return false;
            }

            var root = Folder.Length > 0 ? Folder : Active?.ViewModel.Folder ?? string.Empty;
            if (root.Length == 0)
            {
                return false;
            }

            var index = nameIndex ?? (nameIndex = MarkdownNotes.BuildNameIndex(root));
            var file = FolderNode.IsMarkdown(name) ? name : name + ".md";
            var path = MarkdownNotes.ResolveFile(Active?.ViewModel.Folder ?? root, root, file, index);
            if (path.Length == 0 || !File.Exists(path))
            {
                return false;
            }

            Open(path);
            return true;
        }

        // [[ önerileri: açık klasördeki md dosyaları (yoksa notun klasöründekiler).
        private IEnumerable<NoteSuggestion> Suggestions()
        {
            if (Folder.Length > 0)
            {
                return folderSuggestions;
            }

            var folder = Active?.ViewModel.Folder ?? string.Empty;
            if (folder.Length == 0 || !Directory.Exists(folder))
            {
                return Enumerable.Empty<NoteSuggestion>();
            }

            return BuildSuggestions(FolderNode.AllFiles(FolderNode.Scan(folder, 2)), folder);
        }

        private static List<NoteSuggestion> BuildSuggestions(IEnumerable<string> files, string root)
        {
            return files.Select(f =>
            {
                var name = Path.GetFileNameWithoutExtension(f);
                var folder = Path.GetDirectoryName(f) ?? string.Empty;
                var relative = folder.StartsWith(root, StringComparison.OrdinalIgnoreCase) ? folder.Substring(root.Length).TrimStart('\\') : folder;
                return new NoteSuggestion(name, relative, Res.Main_KindNote, AppIcons.Note, "[[" + name + "]]");
            }).ToList();
        }

        // #etiket önerileri: açık klasördeki notlarda geçen etiketler.
        private IEnumerable<NoteSuggestion> TagSuggestions()
        {
            var files = Folder.Length > 0 ? FolderNode.AllFiles(folderNodes) : new List<string>();
            var tags = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);
            foreach (var file in files.Take(2000))
            {
                try
                {
                    foreach (System.Text.RegularExpressions.Match match in TagPattern.Matches(File.ReadAllText(file)))
                    {
                        tags.Add(match.Groups[1].Value);
                    }
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }

            return tags.OrderBy(t => t, StringComparer.CurrentCultureIgnoreCase).Select(t => new NoteSuggestion("#" + t, string.Empty, Res.Main_KindTag, "#", "#" + t)).ToList();
        }

        private static readonly System.Text.RegularExpressions.Regex TagPattern =
            new System.Text.RegularExpressions.Regex(@"(?<![\w#&/])#([\p{L}][\p{L}\p{N}_\-/]*)", System.Text.RegularExpressions.RegexOptions.Compiled);

        /// <summary>Klasörü kenar çubuğunda açar (md dosyaları ağaçta; [[ önerileri ve bağlantılar bu klasörden).</summary>
        public void LoadFolder(string folder)
        {
            Folder = folder;
            HasFolder = true;
            FolderTitle.Text = Path.GetFileName(folder.TrimEnd('\\'));
            FolderTitle.ToolTip = folder;
            NoFolderPanel.Visibility = Visibility.Collapsed;
            RefreshFolder();
            foreach (var tab in tabs)
            {
                tab.ViewModel.Root = folder;
            }
        }

        private void RefreshFolder()
        {
            if (Folder.Length == 0)
            {
                return;
            }

            folderNodes = FolderNode.Scan(Folder);
            folderSuggestions = BuildSuggestions(FolderNode.AllFiles(folderNodes), Folder);
            nameIndex = null;
            FolderTree.ItemsSource = folderNodes;
        }

        private void CloseFolder()
        {
            Folder = string.Empty;
            HasFolder = false;
            folderNodes = new List<FolderNode>();
            folderSuggestions = new List<NoteSuggestion>();
            nameIndex = null;
            FolderTree.ItemsSource = null;
            FolderTitle.Text = Res.Main_NoFolder;
            FolderTitle.ToolTip = null;
            NoFolderPanel.Visibility = Visibility.Visible;
            foreach (var tab in tabs)
            {
                tab.ViewModel.Root = string.Empty;
            }

            SaveSession();
        }

        private void RemoveTab(EditorView view)
        {
            var index = tabs.IndexOf(view);
            view.Stop();
            tabs.Remove(view);
            Host.Children.Remove(view);
            TabStrip.Items.Remove(view.ViewModel);
            if (tabs.Count > 0 && TabStrip.SelectedItem == null)
            {
                Select(tabs[Math.Max(0, Math.Min(index, tabs.Count) - 1)]);
            }

            UpdateEmpty();
        }

        private void AskToClose(EditorView view)
        {
            closing = view;
            Select(view);
            CloseText.Text = string.Format(Res.Note_CloseText, view.ViewModel.FileName);
            CloseDialog.IsOpen = true;
        }

        private void CloseDialog_Save(object sender, RoutedEventArgs e)
        {
            var view = closing;
            if (view == null)
            {
                return;
            }

            // Kaydetme penceresi (Farklı kaydet) diyalog kapandıktan sonra açılsın.
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (Save(view))
                {
                    FinishClose(view);
                }
                else
                {
                    CancelClose();
                }
            }));
        }

        private void CloseDialog_Discard(object sender, RoutedEventArgs e)
        {
            var view = closing;
            if (view != null)
            {
                Dispatcher.BeginInvoke(new Action(() => FinishClose(view)));
            }
        }

        private void CloseDialog_Cancel(object sender, RoutedEventArgs e)
        {
            CancelClose();
        }

        private void FinishClose(EditorView view)
        {
            closing = null;
            if (closingWindow)
            {
                // Pencere kapanırken oturum, sekmeler kaldırılmadan önce saklanmıştı.
                ContinueWindowClose();
                return;
            }

            RemoveTab(view);
        }

        private void CancelClose()
        {
            closing = null;
            closingWindow = false;
            closeQueue.Clear();
        }

        // Pencere kapanırken kaydedilmemiş sekmeler sırayla sorulur; hepsi bitince kapanır.
        private void MainWindow_Closing(object? sender, CancelEventArgs e)
        {
            if (allowClose)
            {
                return;
            }

            SaveSession();
            var dirty = tabs.Where(t => t.ViewModel.IsDirty).ToList();
            if (dirty.Count == 0)
            {
                return;
            }

            e.Cancel = true;
            closingWindow = true;
            closeQueue.Clear();
            foreach (var view in dirty)
            {
                closeQueue.Enqueue(view);
            }

            ContinueWindowClose();
        }

        private void ContinueWindowClose()
        {
            if (closeQueue.Count > 0)
            {
                AskToClose(closeQueue.Dequeue());
                return;
            }

            allowClose = true;
            Dispatcher.BeginInvoke(new Action(Close));
        }

        private void UpdateEmpty()
        {
            var empty = tabs.Count == 0;
            EmptyPanel.Visibility = empty ? Visibility.Visible : Visibility.Collapsed;
            TabStrip.Visibility = empty ? Visibility.Hidden : Visibility.Visible;
            var recent = App.Settings.Recent.Where(File.Exists).Take(8).ToList();
            EmptyRecent.ItemsSource = recent;
            EmptyRecentTitle.Visibility = recent.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            UpdateTitle();
        }

        private void UpdateTitle()
        {
            Title = Active == null ? Res.App_Name : string.Format(Res.Main_WindowTitle, Active.ViewModel.TabTitle);
        }

        private void ApplySidebar(bool show)
        {
            Sidebar.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            SidebarSplitter.Visibility = Sidebar.Visibility;
            SidebarColumn.MinWidth = show ? 160 : 0;
            if (show && SidebarColumn.Width.Value < 160)
            {
                SidebarColumn.Width = new GridLength(Math.Max(160, App.Settings.SidebarWidth));
            }
            else if (!show)
            {
                SidebarColumn.Width = new GridLength(0);
            }

            SidebarMenuItem.IsChecked = show;
        }

        private void ToggleSidebar()
        {
            ApplySidebar(Sidebar.Visibility != Visibility.Visible);
            App.Settings.ShowSidebar = Sidebar.Visibility == Visibility.Visible;
            App.Settings.Save();
        }

        private void ToggleFullScreen()
        {
            fullScreen = !fullScreen;
            if (fullScreen)
            {
                stateBeforeFullScreen = WindowState;
                WindowState = WindowState.Normal;
                WindowStyle = WindowStyle.None;
                WindowState = WindowState.Maximized;
            }
            else
            {
                WindowStyle = WindowStyle.SingleBorderWindow;
                WindowState = stateBeforeFullScreen;
            }
        }

        private void OpenWithDialog()
        {
            var dialog = new OpenFileDialog
            {
                Filter = Res.Main_MdFilter,
                Multiselect = true,
                InitialDirectory = Folder.Length > 0 ? Folder : (Active != null && !Active.ViewModel.IsUntitled ? Active.ViewModel.Folder : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)),
            };
            if (dialog.ShowDialog(this) == true)
            {
                foreach (var file in dialog.FileNames)
                {
                    Open(file);
                }
            }
        }

        private void OpenFolderWithDialog()
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                ShowNewFolderButton = true,
                SelectedPath = Folder.Length > 0 ? Folder : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            };
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                LoadFolder(dialog.SelectedPath);
                ApplySidebar(true);
                SaveSession();
            }
        }

        private static string FirstHeading(string text)
        {
            var line = (text ?? string.Empty).Replace("\r\n", "\n").Split('\n').Select(l => l.Trim()).FirstOrDefault(l => l.StartsWith("#", StringComparison.Ordinal));
            var name = line == null ? Res.Main_Untitled : line.TrimStart('#').Trim();
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, ' ');
            }

            name = name.Trim();
            return (name.Length == 0 ? Res.Main_Untitled : name) + ".md";
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var modifiers = Keyboard.Modifiers;
            var ctrl = (modifiers & ModifierKeys.Control) != 0 && (modifiers & ModifierKeys.Alt) == 0;
            var shift = (modifiers & ModifierKeys.Shift) != 0;
            var key = e.Key == Key.System ? e.SystemKey : e.Key;
            if (HandleKey(key, ctrl, shift))
            {
                e.Handled = true;
            }
        }

        /// <summary>Pencere kısayollarını işler (testler için açık); işlediyse <c>true</c>.</summary>
        public bool HandleKey(Key key, bool ctrl, bool shift)
        {
            if (!ctrl)
            {
                if (key == Key.F11 && Active == null)
                {
                    ToggleFullScreen();
                    return true;
                }

                return false;
            }

            switch (key)
            {
                case Key.N when !shift:
                    Open(string.Empty);
                    return true;
                case Key.O when !shift:
                    OpenWithDialog();
                    return true;
                case Key.O when shift && Active == null:
                    OpenFolderWithDialog();
                    return true;
                case Key.S when shift:
                    if (Active != null)
                    {
                        SaveAs(Active);
                    }

                    return true;
                case Key.W:
                case Key.F4:
                    if (Active != null)
                    {
                        CloseTab(Active);
                    }

                    return true;
                case Key.P when !shift:
                    if (Active != null)
                    {
                        Export.Print(Active.ViewModel);
                    }

                    return true;
                case Key.E when shift:
                    ToggleSidebar();
                    return true;
                case Key.Tab:
                    if (tabs.Count > 1)
                    {
                        var index = Active == null ? 0 : tabs.IndexOf(Active);
                        Select(tabs[(index + (shift ? tabs.Count - 1 : 1)) % tabs.Count]);
                    }

                    return true;
            }

            return false;
        }

        private void TabStrip_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (var tab in tabs)
            {
                tab.Visibility = tab == Active ? Visibility.Visible : Visibility.Collapsed;
            }

            if (Active != null)
            {
                var view = Active;
                view.CheckExternalNow();
                TabStrip.ScrollIntoView(view.ViewModel);
                Dispatcher.BeginInvoke(new Action(view.FocusEditor), System.Windows.Threading.DispatcherPriority.Input);
                SelectInTree(view.ViewModel.Path);
            }

            UpdateTitle();
        }

        // Seçili sekmenin dosyası kenar çubuğunda da seçili görünsün (tıklama dosyayı yeniden açmaz).
        private void SelectInTree(string path)
        {
            if (path.Length == 0 || folderNodes.Count == 0)
            {
                return;
            }

            suppressTreeOpen = true;
            try
            {
                SelectNode(FolderTree, path);
            }
            finally
            {
                suppressTreeOpen = false;
            }
        }

        private static bool SelectNode(ItemsControl parent, string path)
        {
            foreach (var item in parent.Items.OfType<FolderNode>())
            {
                if (!item.IsFolder && string.Equals(item.Path, path, StringComparison.OrdinalIgnoreCase))
                {
                    if (parent.ItemContainerGenerator.ContainerFromItem(item) is TreeViewItem container)
                    {
                        container.IsSelected = true;
                        container.BringIntoView();
                    }

                    return true;
                }

                if (item.IsFolder && path.StartsWith(item.Path + "\\", StringComparison.OrdinalIgnoreCase)
                    && parent.ItemContainerGenerator.ContainerFromItem(item) is TreeViewItem folder)
                {
                    folder.IsExpanded = true;
                    folder.UpdateLayout();
                    return SelectNode(folder, path);
                }
            }

            return false;
        }

        private void Tab_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle && ViewOf(sender) is EditorView view)
            {
                e.Handled = true;
                CloseTab(view);
            }
        }

        private void TabClose_Click(object sender, RoutedEventArgs e)
        {
            if (ViewOf(sender) is EditorView view)
            {
                CloseTab(view);
            }
        }

        private void FolderTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (!suppressTreeOpen && e.NewValue is FolderNode node && !node.IsFolder)
            {
                Open(node.Path);
            }
        }

        private void FileMenu_SubmenuOpened(object sender, RoutedEventArgs e)
        {
            RecentMenu.Items.Clear();
            var recent = App.Settings.Recent.Where(File.Exists).ToList();
            foreach (var path in recent)
            {
                var item = new MenuItem { Header = Path.GetFileNameWithoutExtension(path), InputGestureText = Path.GetDirectoryName(path), Tag = path };
                item.Click += RecentItem_Click;
                RecentMenu.Items.Add(item);
            }

            if (recent.Count == 0)
            {
                RecentMenu.Items.Add(new MenuItem { Header = Res.Main_RecentEmpty, IsEnabled = false });
            }
            else
            {
                RecentMenu.Items.Add(new Separator());
                var clear = new MenuItem { Header = Res.Main_RecentClear };
                clear.Click += (s, a) =>
                {
                    App.Settings.Recent.Clear();
                    App.Settings.Save();
                    UpdateEmpty();
                };
                RecentMenu.Items.Add(clear);
            }
        }

        private void ViewMenu_SubmenuOpened(object sender, RoutedEventArgs e)
        {
            SidebarMenuItem.IsChecked = Sidebar.Visibility == Visibility.Visible;
            ThemeMenu.Items.Clear();
            foreach (var palette in Themes.All)
            {
                var item = new MenuItem { Header = palette.Name, IsCheckable = true, IsChecked = Themes.Current != null && Themes.Current.Key == palette.Key, Tag = palette };
                item.Click += (s, a) =>
                {
                    Themes.Apply(palette);
                    App.Settings.Theme = palette.Key;
                    App.Settings.Save();
                    foreach (var tab in tabs)
                    {
                        tab.RenderPreview();
                    }
                };
                ThemeMenu.Items.Add(item);
            }
        }

        private void RecentItem_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is string path)
            {
                Open(path);
            }
        }

        private void New_Click(object sender, RoutedEventArgs e)
        {
            Open(string.Empty);
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            OpenWithDialog();
        }

        private void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            OpenFolderWithDialog();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (Active != null)
            {
                Save(Active);
            }
        }

        private void SaveAs_Click(object sender, RoutedEventArgs e)
        {
            if (Active != null)
            {
                SaveAs(Active);
            }
        }

        private void SaveAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var tab in tabs.Where(t => t.ViewModel.IsDirty).ToList())
            {
                Select(tab);
                if (!Save(tab))
                {
                    break;
                }
            }
        }

        private void ExportHtml_Click(object sender, RoutedEventArgs e)
        {
            if (Active == null)
            {
                return;
            }

            var vm = Active.ViewModel;
            var dialog = new SaveFileDialog
            {
                Filter = Res.Main_HtmlFilter,
                DefaultExt = ".html",
                AddExtension = true,
                FileName = vm.FileName + ".html",
                InitialDirectory = vm.IsUntitled ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) : vm.Folder,
            };
            if (dialog.ShowDialog(this) == true)
            {
                var error = Export.SaveHtml(vm, dialog.FileName);
                vm.Status = error.Length > 0 ? string.Format(Res.Note_SaveError, error) : string.Format(Res.Main_Exported, dialog.FileName);
            }
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            if (Active != null)
            {
                Export.Print(Active.ViewModel);
            }
        }

        private void CloseTab_Click(object sender, RoutedEventArgs e)
        {
            if (Active != null)
            {
                CloseTab(Active);
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SidebarMenu_Click(object sender, RoutedEventArgs e)
        {
            ApplySidebar(SidebarMenuItem.IsChecked);
            App.Settings.ShowSidebar = SidebarMenuItem.IsChecked;
            App.Settings.Save();
        }

        private void SidebarToggle_Click(object sender, RoutedEventArgs e)
        {
            ToggleSidebar();
        }

        private void SidebarSplitter_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            App.Settings.SidebarWidth = SidebarColumn.ActualWidth;
            App.Settings.Save();
        }

        private void RefreshFolder_Click(object sender, RoutedEventArgs e)
        {
            RefreshFolder();
        }

        private void CloseFolder_Click(object sender, RoutedEventArgs e)
        {
            CloseFolder();
        }

        private void Window_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop) && e.Data.GetData(DataFormats.FileDrop) is string[] files && files.Any(f => FolderNode.IsMarkdown(f) || Directory.Exists(f)))
            {
                e.Effects = DragDropEffects.Copy;
                e.Handled = true;
            }
        }

        // Pencereye bırakılan md dosyaları sekmede, klasör kenar çubuğunda açılır (düzenleyiciye bırakılan resimler orada işlenir).
        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (!(e.Data.GetData(DataFormats.FileDrop) is string[] files))
            {
                return;
            }

            foreach (var file in files)
            {
                if (Directory.Exists(file))
                {
                    LoadFolder(file);
                    ApplySidebar(true);
                    SaveSession();
                }
                else if (FolderNode.IsMarkdown(file))
                {
                    Open(file);
                }
            }

            e.Handled = true;
        }
    }
}
