using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using DesignKit.Controls;
using Pusula.Core.Types;
using Pusula.Wpf.ViewModels;
using Pusula.Wpf.ViewModels.TemplateEditor;

namespace Pusula.Wpf
{
    /// <summary>
    /// Ana pencere: solda klasör ağacı, sağda seçilen düğümün paneli.
    /// </summary>
    public partial class MainWindow : ChromeWindow
    {
        private readonly MainViewModel viewModel = new MainViewModel();

        /// <summary>
        /// Ana pencereyi oluşturur.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            DataContext = viewModel;
            viewModel.NoteEditorOpener = OpenNoteWindow;
            MentionAssist.Provider = viewModel.NoteSuggestions;
            MentionAssist.TagProvider = viewModel.TagSuggestions;
            viewModel.ActivateMain = () =>
            {
                if (WindowState == WindowState.Minimized)
                {
                    WindowState = WindowState.Normal;
                }

                Activate();
            };

            // İlk açılışta kök klasör seçilmemişse Ayarlar açılır (kök seçimi artık orada).
            ContentRendered += (s, e) =>
            {
                if (App.Started && !viewModel.HasRoot)
                {
                    Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => Settings_Click(this, new RoutedEventArgs())));
                }
            };

            // ContextMenu görsel ağacın dışında durur; DataContext'i elle verilir.
            foreach (var key in new[] { "AddMenu", "NodeMenu", "TreeMenu" })
            {
                ((ContextMenu)FindResource(key)).DataContext = viewModel;
            }

            // Sekmeler, liste değişirken değil değişiklik bittikten sonra ve bir kez kurulur. Değişiklik anında kurulunca
            // Genel'deki "Jig ve kablo" kartları, bildirim gelmeden listeyi yeniden okuyup aynı kartı iki kez ekliyordu
            // (fazladan "Jig 0" kutucuğu).
            viewModel.Detail.InventoryGroups.CollectionChanged += (s, e) => ScheduleInventoryTabs();
            StockTabs.SelectionChanged += StockTabs_SelectionChanged;
            SetUpLayout();

            // Tür düğümünde (hepsi aynı tür) envanter listesinin Tür sütunu gizlenir.
            viewModel.Inventory.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(InventoryDetailViewModel.TypeColumnWidth))
                {
                    InventoryTypeColumn.Width = viewModel.Inventory.TypeColumnWidth;
                }
            };
        }

        // Stok sekmeleri: her envanter türü (Jigler, Kablolar …) Kalifikasyon'dan sonra kendi sekmesinde.
        // Stok değişince sekmeler yeniden kurulur; açık olan tür sekmesi korunur.
        private const string InventoryTabTag = "inventory:";
        private string? preferredInventoryKey;
        private bool rebuildingTabs;

        // Kullanıcının seçtiği sekme hatırlanır (yeniden kurulurken oluşan seçim değişiklikleri sayılmaz).
        private void StockTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (rebuildingTabs || !ReferenceEquals(e.OriginalSource, StockTabs) || !(StockTabs.SelectedItem is TabItem selected))
            {
                return;
            }

            var tag = selected.Tag as string;
            preferredInventoryKey = tag != null && tag.StartsWith(InventoryTabTag, StringComparison.Ordinal) ? tag : null;
        }

        private bool inventoryTabsPending;

        private void ScheduleInventoryTabs()
        {
            if (inventoryTabsPending)
            {
                return;
            }

            inventoryTabsPending = true;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                inventoryTabsPending = false;
                RebuildInventoryTabs();
            }), DispatcherPriority.Loaded);
        }

        private void RebuildInventoryTabs()
        {
            rebuildingTabs = true;
            try
            {
                RebuildInventoryTabsCore();
            }
            finally
            {
                rebuildingTabs = false;
            }

            ApplyTabOrder();
        }

        private void RebuildInventoryTabsCore()
        {
            foreach (var old in StockTabs.Items.OfType<TabItem>().Where(i => (i.Tag as string)?.StartsWith(InventoryTabTag, StringComparison.Ordinal) == true).ToList())
            {
                StockTabs.Items.Remove(old);
            }

            var index = 3;
            var template = (DataTemplate)FindResource("StockInventoryTabTemplate");
            foreach (var group in viewModel.Detail.InventoryGroups)
            {
                var header = new StackPanel { Orientation = Orientation.Horizontal };
                header.Children.Add(new AppIcon { Glyph = TreeNodeViewModel.GlyphOf(group.Type.Icon), FontSize = 13, Foreground = BrushOf(group.Type.Color), VerticalAlignment = VerticalAlignment.Center });
                header.Children.Add(new TextBlock { Text = group.Header, Margin = new Thickness(7, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center });
                var tab = new TabItem
                {
                    Header = header,
                    Content = group,
                    ContentTemplate = template,
                    Tag = InventoryTabTag + group.Type.Key,
                };
                StockTabs.Items.Insert(index++, tab);
                if (tab.Tag as string == preferredInventoryKey)
                {
                    StockTabs.SelectedItem = tab;
                }
            }
        }

        private void FolderTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            // İki ağaç var (projeler, envanter); birinde seçim kalkınca (boş) seçim silinmez.
            if (e.NewValue is TreeNodeViewModel node)
            {
                viewModel.SelectedNode = node;
            }
        }

        // Envanter projelerin hemen altında durur; projeler çoksa üst ağaç bölmenin üçte ikisinde kayar.
        private void TreePane_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            FolderTree.MaxHeight = Math.Max(80, TreePane.ActualHeight * 2 / 3);
        }

        // Sağ tık, menü açılmadan önce tıklanan düğümü seçer.
        private void FolderTree_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var source = e.OriginalSource as DependencyObject;
            while (source != null && !(source is TreeViewItem))
            {
                source = VisualTreeHelper.GetParent(source);
            }

            if (source is TreeViewItem item)
            {
                item.IsSelected = true;
                item.Focus();
            }
        }

        // Diyalog açılınca ilk alana odaklanır.
        private void FocusWhenVisible(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is TextBox box && box.IsVisible)
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() => Keyboard.Focus(box)));
            }
        }

        // ContentDialog birincil düğmede kendiliğinden kapanır; hata varsa hata metniyle yeniden açılır.
        private void ProjectDialog_PrimaryButtonClick(object sender, RoutedEventArgs e)
        {
            if (!viewModel.ConfirmNewProject())
            {
                Dispatcher.BeginInvoke(new Action(() => viewModel.IsProjectDialogOpen = true));
            }
        }

        // Durum menüsü her açılışta tür kalıbından kurulur: sıralı durumlar, ayraç, sıra dışı durumlar.
        private void StatusMenu_Opened(object sender, RoutedEventArgs e)
        {
            var menu = (ContextMenu)sender;
            menu.Items.Clear();
            var type = viewModel.Types.Stock;
            var current = viewModel.Detail.Status.Name;

            foreach (var status in type.Statuses)
            {
                menu.Items.Add(CreateStatusItem(status, current));
            }

            if (type.SideStatuses.Count > 0)
            {
                menu.Items.Add(new Separator());
                foreach (var status in type.SideStatuses)
                {
                    menu.Items.Add(CreateStatusItem(status, current));
                }
            }
        }

        private MenuItem CreateStatusItem(Core.Types.StatusDefinition status, string current)
        {
            var item = new MenuItem
            {
                Header = status.NeedsNote ? status.Name + "…" : status.Name,
            };
            if (string.Equals(status.Name, current, StringComparison.OrdinalIgnoreCase))
            {
                item.Icon = new AppIcon { Glyph = AppIcons.Check };
            }

            item.Click += (s, args) =>
            {
                viewModel.NoteMenuChoice();
                if (status.NeedsNote)
                {
                    viewModel.AskStatusNote(status.Name);
                }
                else
                {
                    viewModel.SetStockStatus(status.Name, string.Empty);
                }
            };
            return item;
        }

        private void MarkDialog_PrimaryButtonClick(object sender, RoutedEventArgs e)
        {
            if (!viewModel.ConfirmMark())
            {
                Dispatcher.BeginInvoke(new Action(() => viewModel.IsMarkDialogOpen = true));
            }
        }

        private void StatusNote_PrimaryButtonClick(object sender, RoutedEventArgs e)
        {
            viewModel.ConfirmStatusNote();
        }

        // Testler / Kalifikasyon: sütun başlığına tıklayınca o sütuna göre sıralar (ikinci tık ters çevirir).
        private void RunsHeader_Click(object sender, RoutedEventArgs e)
        {
            var header = e.OriginalSource as GridViewColumnHeader;
            var property = header?.Tag as string;
            var list = sender as ListView;
            if (property == null || list?.ItemsSource == null)
            {
                return;
            }

            var view = System.Windows.Data.CollectionViewSource.GetDefaultView(list.ItemsSource);
            var direction = System.ComponentModel.ListSortDirection.Ascending;
            if (view.SortDescriptions.Count > 0 && view.SortDescriptions[0].PropertyName == property
                && view.SortDescriptions[0].Direction == System.ComponentModel.ListSortDirection.Ascending)
            {
                direction = System.ComponentModel.ListSortDirection.Descending;
            }

            view.SortDescriptions.Clear();
            view.SortDescriptions.Add(new System.ComponentModel.SortDescription(property, direction));
        }

        // Koşuya çift tıklayınca notu (test.md / _kosu.md) varsayılan programla açar; not yoksa klasörü açar.
        private void RunsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left || viewModel.IsDialogBusy)
            {
                return;
            }

            var source = e.OriginalSource as DependencyObject;
            while (source != null && !(source is ListViewItem))
            {
                source = VisualTreeHelper.GetParent(source);
            }

            if ((source as ListViewItem)?.DataContext is TestRunRow run)
            {
                viewModel.OpenNoteOrFolder(run.NotePath, run.Folder);
            }
        }

        // Zaman çizelgesi kartına tıklayınca ayrıntı açılır / kapanır (karttaki düğmeler hariç).
        private void TimelineCard_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var source = e.OriginalSource as DependencyObject;
            while (source != null && source != sender)
            {
                if (source is ButtonBase)
                {
                    return;
                }

                source = VisualTreeHelper.GetParent(source);
            }

            if ((sender as FrameworkElement)?.DataContext is TimelineItem item && item.HasDetail)
            {
                item.IsExpanded = !item.IsExpanded;
            }
        }

        private void TimelineNote_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is TimelineItem item)
            {
                viewModel.OpenNoteOrFolder(item.NotePath, item.Folder);
            }
        }

        private void TimelineFolder_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is TimelineItem item)
            {
                viewModel.OpenPath(item.Folder);
            }
        }

        // Testler / Kalifikasyon sağ tık menüsü: seçili koşu üzerinde.
        private static TestRunRow? SelectedRun(object sender)
        {
            var menu = (sender as MenuItem)?.Parent as ContextMenu;
            return (menu?.PlacementTarget as ListView)?.SelectedItem as TestRunRow;
        }

        private readonly Dictionary<string, NoteWindow> noteWindows = new Dictionary<string, NoteWindow>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Notu Pusula'nın md penceresinde açar; aynı not zaten açıksa o pencere öne gelir. Kapanınca, not kaydedildiyse
        /// ana pencere yenilenir (not özeti, bahisler).
        /// </summary>
        public bool OpenNoteWindow(string path)
        {
            NoteWindow existing;
            if (noteWindows.TryGetValue(path, out existing))
            {
                if (existing.WindowState == WindowState.Minimized)
                {
                    existing.WindowState = WindowState.Normal;
                }

                existing.Activate();
                return true;
            }

            var editor = viewModel.CreateNoteEditor(path);
            var window = new NoteWindow(editor, viewModel.NavigateToKey, viewModel.OpenNoteExternally, viewModel.OpenPath);
            editor.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(NoteEditorViewModel.SaveCount) && editor.SaveCount > 0)
                {
                    viewModel.NoteSaved(path);
                }
            };
            window.Closed += (s, e) =>
            {
                noteWindows.Remove(path);
                if (editor.SaveCount > 0)
                {
                    viewModel.Refresh();
                }
            };
            noteWindows[path] = window;
            window.Show();
            return true;
        }

        /// <summary>Açık not pencereleri (testler için).</summary>
        public IEnumerable<NoteWindow> NoteWindows
        {
            get { return noteWindows.Values; }
        }


        private void Mention_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is MentionRow mention)
            {
                viewModel.OpenNoteOrFolder(mention.Path, mention.Folder);
            }
        }

        private void RunMenuAnalysis_Click(object sender, RoutedEventArgs e)
        {
            var run = SelectedRun(sender);
            if (run != null)
            {
                viewModel.OpenAnalysis(run);
            }
        }

        private void AnalysisDialog_PrimaryButtonClick(object sender, RoutedEventArgs e)
        {
            if (!viewModel.ConfirmAnalysis())
            {
                Dispatcher.BeginInvoke(new Action(() => viewModel.IsAnalysisOpen = true));
            }
        }

        private void RunMenuNote_Click(object sender, RoutedEventArgs e)
        {
            var run = SelectedRun(sender);
            if (run != null)
            {
                viewModel.OpenNoteOrFolder(run.NotePath, run.Folder);
            }
        }

        private void RunMenuFolder_Click(object sender, RoutedEventArgs e)
        {
            var run = SelectedRun(sender);
            if (run != null)
            {
                viewModel.OpenPath(run.Folder);
            }
        }

        private void RunMenuRaw_Click(object sender, RoutedEventArgs e)
        {
            var run = SelectedRun(sender);
            if (run != null)
            {
                viewModel.OpenSubFolderOrFolder(run.Folder, "Ham");
            }
        }

        private void RunMenuPhotos_Click(object sender, RoutedEventArgs e)
        {
            var run = SelectedRun(sender);
            if (run != null)
            {
                viewModel.OpenSubFolderOrFolder(run.Folder, "Foto");
            }
        }

        // Stok Genel kapağı: not, fotoğraf klasörü, tek fotoğraf.
        private void StockNote_Click(object sender, RoutedEventArgs e)
        {
            var cover = viewModel.Detail.Cover;
            viewModel.OpenNoteOrFolder(cover.NoteFile, cover.StockFolder);
        }

        private void StockCreateNote_Click(object sender, RoutedEventArgs e)
        {
            viewModel.CreateStockNote();
        }

        // Kapağa tıklayınca fotoğraf varsayılan görüntüleyicide tam boy açılır.
        private void CoverPhoto_Click(object sender, RoutedEventArgs e)
        {
            var cover = viewModel.Detail.Cover;
            viewModel.OpenNoteOrFolder(cover.CoverPhoto, cover.HasPhotosFolder ? cover.PhotosFolder : cover.StockFolder);
        }

        private void StockPhotos_Click(object sender, RoutedEventArgs e)
        {
            var cover = viewModel.Detail.Cover;
            viewModel.OpenPath(cover.HasPhotosFolder ? cover.PhotosFolder : cover.StockFolder);
        }

        private void StockDocuments_Click(object sender, RoutedEventArgs e)
        {
            var cover = viewModel.Detail.Cover;
            viewModel.OpenPath(cover.HasDocumentsFolder ? cover.DocumentsFolder : cover.StockFolder);
        }

        private void StockPhoto_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is string path)
            {
                viewModel.OpenNoteOrFolder(path, viewModel.Detail.Cover.PhotosFolder);
            }
        }

        private void EditTemplates_Click(object sender, RoutedEventArgs e)
        {
            EditTemplates(this);
        }

        private void EditTypes_Click(object sender, RoutedEventArgs e)
        {
            EditTypes(this);
        }
        // Ayarlar düğmesi: kök klasör, tema, tanımlar (şablon, tür, dosya türü).
        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            new SettingsWindow(this, viewModel) { Owner = this }.ShowDialog();
        }

        /// <summary>Dosya türleri penceresini açar.</summary>
        internal void EditFileTypes(Window owner)
        {
            if (!viewModel.CanEditTemplates())
            {
                return;
            }

            var editor = new FileTypesWindow(new ViewModels.TypeEditor.FileTypesViewModel(viewModel.RootPath, viewModel.Types))
            {
                Owner = owner,
            };
            if (editor.ShowDialog() == true)
            {
                viewModel.ReloadTypes();
            }
        }

        /// <summary>Türler penceresini açar.</summary>
        internal void EditTypes(Window owner)
        {
            if (!viewModel.CanEditTemplates())
            {
                return;
            }

            var editor = new TypeEditorWindow(new ViewModels.TypeEditor.TypeEditorViewModel(viewModel.RootPath, viewModel.Types))
            {
                Owner = owner,
            };
            if (editor.ShowDialog() == true)
            {
                viewModel.ReloadTypes();
            }
        }

        // Stok sayfası, Jig ve kablo: "Ekle" düğmesi genel / proje kayıtlarını menüde listeler, seçilen stoka bağlanır.
        private void StockInvAdd_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (!(button?.Tag is TypeDefinition type))
            {
                return;
            }

            var menu = new ContextMenu { PlacementTarget = button, Placement = PlacementMode.Bottom };
            var candidates = viewModel.LinkCandidates(type);
            foreach (var candidate in candidates)
            {
                var number = candidate.Record.Number;
                var item = new MenuItem
                {
                    Header = number + " · " + candidate.Record.Name + "  (" + InventoryDetailViewModel.LevelText(candidate) + ")",
                };
                item.Click += (s, args) => viewModel.SetStockLink(number, true);
                menu.Items.Add(item);
            }

            if (candidates.Count == 0)
            {
                menu.Items.Add(new MenuItem { Header = Properties.Resources.StockInv_NoCandidates, IsEnabled = false });
            }

            menu.IsOpen = true;
        }

        private void StockInvNew_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is TypeDefinition type)
            {
                viewModel.OpenNewInventory(type);
            }
        }

        private static InventoryRow? MenuRow(object sender)
        {
            var menu = (sender as MenuItem)?.Parent as ContextMenu;
            return (menu?.PlacementTarget as ListView)?.SelectedItem as InventoryRow;
        }

        private void StockInvOpen_Click(object sender, RoutedEventArgs e)
        {
            var row = MenuRow(sender);
            if (row != null && row.Folder.Length > 0)
            {
                viewModel.ShowInventory(row.Folder);
            }
        }

        private void StockInvFolder_Click(object sender, RoutedEventArgs e)
        {
            var row = MenuRow(sender);
            if (row != null && row.Folder.Length > 0)
            {
                viewModel.OpenPath(row.Folder);
            }
        }

        private void StockInvNote_Click(object sender, RoutedEventArgs e)
        {
            var row = MenuRow(sender);
            if (row != null && row.Folder.Length > 0)
            {
                viewModel.OpenQuickNote(QuickNoteTarget.Record, row.Folder, row.Number);
            }
        }

        private void StockInvUnlink_Click(object sender, RoutedEventArgs e)
        {
            var row = MenuRow(sender);
            if (row != null && row.IsLinked)
            {
                viewModel.SetStockLink(row.Number, false);
            }
        }

        // Kayıt sayfası: klasör kutucuğu klasörü, fotoğraf resmi açar.
        private void RecordFolder_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is string path)
            {
                viewModel.OpenPath(path);
            }
        }

        private void RecordPhoto_Click(object sender, RoutedEventArgs e)
        {
            var shown = viewModel.Inventory.Item;
            if ((sender as FrameworkElement)?.Tag is string path && shown != null)
            {
                viewModel.OpenNoteOrFolder(path, shown.Folder);
            }
        }

        private void RecordNote_Click(object sender, RoutedEventArgs e)
        {
            var shown = viewModel.Inventory.Item;
            if (shown != null)
            {
                viewModel.OpenNoteOrFolder(viewModel.Inventory.NoteFile, shown.Folder);
            }
        }

        private async void RecordCreateNote_Click(object sender, RoutedEventArgs e)
        {
            await viewModel.CreateInventoryNoteAsync().ConfigureAwait(true);
        }

        // Klasör ağacında çift tıklayınca klasörü açar.
        private void FolderTreeView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && !viewModel.IsDialogBusy && (sender as TreeView)?.SelectedItem is FolderTreeItem item)
            {
                viewModel.OpenPath(item.Path);
                e.Handled = true;
            }
        }

        // "+" menüsü: envanter türleri (Yeni kablo, Yeni jig …) her açılışta tür kalıbından eklenir.
        private void AddMenu_Opened(object sender, RoutedEventArgs e)
        {
            var menu = (ContextMenu)sender;
            foreach (var old in menu.Items.OfType<FrameworkElement>().Where(i => Equals(i.Tag, "inventory")).ToList())
            {
                menu.Items.Remove(old);
            }

            var index = 2;
            menu.Items.Insert(index++, new MenuItem { Header = Properties.Resources.Tree_Inventory, Tag = "inventory", IsEnabled = false, FontSize = 12, Margin = new Thickness(0, 6, 0, 0) });
            foreach (var type in viewModel.InventoryTypes)
            {
                var item = new MenuItem
                {
                    Header = type.Name.Replace("_", "__"),
                    Tag = "inventory",
                    Icon = new AppIcon { Glyph = TreeNodeViewModel.GlyphOf(type.Icon), Foreground = BrushOf(type.Color) },
                };
                var chosen = type;
                item.Click += (s, args) => viewModel.OpenNewInventory(chosen);
                menu.Items.Insert(index++, item);
            }
        }

        // Başlıktaki üst kayıt (proje / stok) bağlantısı: oraya gider.
        private void HeaderParent_Click(object sender, RoutedEventArgs e)
        {
            if (viewModel.Detail.ParentPath.Length > 0)
            {
                viewModel.SelectNodeByPath(viewModel.Detail.ParentPath);
            }
        }

        // Başlıktaki ⋯: yolu kopyala, düzenle, takipten çıkar, sil.
        private void HeaderMore_Click(object sender, RoutedEventArgs e)
        {
            var node = viewModel.SelectedNode;
            if (node == null)
            {
                return;
            }

            var menu = new ContextMenu { PlacementTarget = (UIElement)sender, Placement = PlacementMode.Bottom };
            var copy = new MenuItem { Header = Properties.Resources.Header_CopyPath, Icon = new AppIcon { Glyph = AppIcons.Copy } };
            copy.Click += (s, args) =>
            {
                try
                {
                    Clipboard.SetText(node.Path);
                }
                catch (System.Runtime.InteropServices.COMException)
                {
                    // Pano başka bir uygulamada kilitliyse sessizce geçilir.
                }
            };
            menu.Items.Add(copy);
            if (viewModel.CanEditSelected)
            {
                menu.Items.Add(new Separator());
                var edit = new MenuItem { Header = Properties.Resources.Menu_Edit, Icon = new AppIcon { Glyph = AppIcons.Edit } };
                edit.Click += NodeEdit_Click;
                var untrack = new MenuItem { Header = Properties.Resources.Menu_Untrack, Icon = new AppIcon { Glyph = AppIcons.Remove } };
                untrack.Click += NodeUntrack_Click;
                var delete = new MenuItem { Header = Properties.Resources.Menu_Delete, Icon = new AppIcon { Glyph = AppIcons.Delete } };
                delete.Click += NodeDelete_Click;
                menu.Items.Add(edit);
                menu.Items.Add(untrack);
                menu.Items.Add(delete);
            }

            menu.IsOpen = true;
        }

        // Seri numaraları tablosunda hücre: o turun notunu (yoksa klasörünü) açar.
        private void SerialCell_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is MatrixCell cell && cell.HasRun)
            {
                viewModel.OpenNoteOrFolder(cell.NotePath, cell.Folder);
            }
        }

        // Genel'deki jig / kablo kutucuğu: o türün sekmesine geçer.
        private void InventoryTile_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var key = (sender as FrameworkElement)?.Tag as string;
            var tab = StockTabs.Items.OfType<TabItem>().FirstOrDefault(t => t.Tag as string == InventoryTabTag + key);
            if (tab != null)
            {
                StockTabs.SelectedItem = tab;
            }
        }

        // "+" düğmesi: menüyü düğmenin altında açar (ok yok, sade ikon).
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var menu = (ContextMenu)FindResource("AddMenu");
            menu.PlacementTarget = (UIElement)sender;
            menu.Placement = PlacementMode.Bottom;
            menu.IsOpen = true;
        }

        private static Brush? BrushOf(string color)
        {
            try
            {
                return color.Length == 0 ? null : (Brush?)new BrushConverter().ConvertFromString(color);
            }
            catch (FormatException)
            {
                return null;
            }
        }

        // Envanter kaydının durum menüsü: kayıt türünün durumları; seçince tarih / kişi / konum / not sorulur.
        private void InventoryStatusMenu_Opened(object sender, RoutedEventArgs e)
        {
            var menu = (ContextMenu)sender;
            menu.Items.Clear();
            var type = viewModel.Inventory.Type;
            if (type == null)
            {
                return;
            }

            var current = viewModel.Inventory.Status.Name;
            foreach (var status in type.Statuses)
            {
                menu.Items.Add(CreateInventoryStatusItem(status.Name, current));
            }

            if (type.SideStatuses.Count > 0)
            {
                menu.Items.Add(new Separator());
                foreach (var status in type.SideStatuses)
                {
                    menu.Items.Add(CreateInventoryStatusItem(status.Name, current));
                }
            }
        }

        private MenuItem CreateInventoryStatusItem(string status, string current)
        {
            var item = new MenuItem { Header = status + "…" };
            if (string.Equals(status, current, StringComparison.OrdinalIgnoreCase))
            {
                item.Icon = new AppIcon { Glyph = AppIcons.Check };
            }

            item.Click += (s, args) =>
            {
                viewModel.NoteMenuChoice();
                viewModel.AskInventoryEvent(status);
            };
            return item;
        }

        private void InventoryDialog_PrimaryButtonClick(object sender, RoutedEventArgs e)
        {
            if (!viewModel.ConfirmNewInventory())
            {
                Dispatcher.BeginInvoke(new Action(() => viewModel.IsInventoryDialogOpen = true));
            }
        }

        private void EventDialog_PrimaryButtonClick(object sender, RoutedEventArgs e)
        {
            viewModel.ConfirmInventoryEvent();
        }

        // Envanter listesinde satıra çift tıklayınca ağaçta o kayıt seçilir.
        private void InventoryList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left || viewModel.IsDialogBusy)
            {
                return;
            }

            var source = e.OriginalSource as DependencyObject;
            while (source != null && !(source is ListViewItem))
            {
                source = VisualTreeHelper.GetParent(source);
            }

            if ((source as ListViewItem)?.DataContext is InventoryRow row && row.Folder.Length > 0)
            {
                viewModel.ShowInventory(row.Folder);
            }
        }

        // Özet tablosunda satıra çift tıklayınca o klasör açılır.
        private void DetailList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Yalnızca bir satırın üzerine sol tuşla yapılan çift tıklama; diyalog ya da menü az önce kapandıysa yok say.
            if (e.ChangedButton != MouseButton.Left || viewModel.IsDialogBusy)
            {
                return;
            }

            var source = e.OriginalSource as DependencyObject;
            while (source != null && !(source is ListViewItem))
            {
                source = VisualTreeHelper.GetParent(source);
            }

            if ((source as ListViewItem)?.DataContext is DetailRowViewModel row)
            {
                OpenDetailRow(row);
            }
        }

        // Proje tablosu: çift tık / "Aç" stokun sayfasına gider; ağaçta yoksa klasörü açar.
        private void OpenDetailRow(DetailRowViewModel row)
        {
            viewModel.SelectNodeByPath(row.Path);
            if (!string.Equals(viewModel.SelectedNode?.Path, row.Path, StringComparison.OrdinalIgnoreCase))
            {
                viewModel.OpenPath(row.Path);
            }
        }

        private static DetailRowViewModel? DetailMenuRow(object sender)
        {
            var menu = (sender as MenuItem)?.Parent as ContextMenu;
            return (menu?.PlacementTarget as ListView)?.SelectedItem as DetailRowViewModel;
        }

        private void DetailRowOpen_Click(object sender, RoutedEventArgs e)
        {
            var row = DetailMenuRow(sender);
            if (row != null)
            {
                OpenDetailRow(row);
            }
        }

        private void DetailRowFolder_Click(object sender, RoutedEventArgs e)
        {
            var row = DetailMenuRow(sender);
            if (row != null)
            {
                viewModel.OpenPath(row.Path);
            }
        }

        private void DetailRowNote_Click(object sender, RoutedEventArgs e)
        {
            var row = DetailMenuRow(sender);
            if (row != null)
            {
                viewModel.OpenQuickNote(QuickNoteTarget.Stock, row.Path, row.Name);
            }
        }

        // Geçmiş / proje notlarında Obsidian olay notunu açar.
        private void HistoryNote_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is string path && path.Length > 0)
            {
                viewModel.OpenNoteOrFolder(path, System.IO.Path.GetDirectoryName(path) ?? string.Empty);
            }
        }

        // Satır başındaki klasör düğmesi.
        private void RowFolder_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is string path && path.Length > 0)
            {
                viewModel.OpenPath(path);
            }
        }

        // Kısa not kutusu: Enter ya da "Not ekle" yazar; kutunun Tag'i nereye yazılacağını söyler (Project, Stock, Record).
        private void QuickNoteBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && sender is TextBox box)
            {
                SubmitQuickNote(box);
                e.Handled = true;
            }
        }

        private void StockFilterClear_Click(object sender, RoutedEventArgs e)
        {
            viewModel.Detail.ClearStockFilter();
        }

        private void RunFilterClear_Click(object sender, RoutedEventArgs e)
        {
            viewModel.Detail.ClearRunFilter();
        }

        private void QuickNoteAdd_Click(object sender, RoutedEventArgs e)
        {
            var grid = (sender as FrameworkElement)?.Parent as Panel;
            var box = grid?.Children.OfType<TextBox>().FirstOrDefault();
            if (box != null)
            {
                SubmitQuickNote(box);
            }
        }

        private void SubmitQuickNote(TextBox box)
        {
            QuickNoteTarget target;
            if (Enum.TryParse(box.Tag as string, out target) && viewModel.AddQuickNoteHere(target, box.Text))
            {
                box.Text = string.Empty;
            }
        }

        private void QuickNote_PrimaryButtonClick(object sender, RoutedEventArgs e)
        {
            viewModel.ConfirmQuickNote();
        }

        // Düzenle / takipten çıkar: ağaç, envanter satırı, proje tablosu satırı aynı diyaloğu açar.
        private void NodeEdit_Click(object sender, RoutedEventArgs e)
        {
            viewModel.OpenEditSelected();
        }

        private void NodeUntrack_Click(object sender, RoutedEventArgs e)
        {
            viewModel.OpenUntrackSelected();
        }

        private void StockInvEdit_Click(object sender, RoutedEventArgs e)
        {
            var row = MenuRow(sender);
            if (row != null && row.Folder.Length > 0)
            {
                viewModel.OpenEdit(row.Folder);
            }
        }

        private void StockInvUntrack_Click(object sender, RoutedEventArgs e)
        {
            var row = MenuRow(sender);
            if (row != null && row.Folder.Length > 0)
            {
                viewModel.OpenUntrack(row.Folder);
            }
        }

        private void DetailRowEdit_Click(object sender, RoutedEventArgs e)
        {
            var row = DetailMenuRow(sender);
            if (row != null)
            {
                viewModel.OpenEdit(row.Path);
            }
        }

        private void DetailRowUntrack_Click(object sender, RoutedEventArgs e)
        {
            var row = DetailMenuRow(sender);
            if (row != null)
            {
                viewModel.OpenUntrack(row.Path);
            }
        }

        private void Edit_PrimaryButtonClick(object sender, RoutedEventArgs e)
        {
            viewModel.ConfirmEdit();
        }

        private void Untrack_PrimaryButtonClick(object sender, RoutedEventArgs e)
        {
            viewModel.ConfirmUntrack();
        }

        // Test planı kartı: dosyayı (KIWI planı KIWI ile) ya da klasörünü açar.
        private void PlanOpen_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is string path && path.Length > 0)
            {
                viewModel.OpenNoteOrFolder(path, System.IO.Path.GetDirectoryName(path) ?? string.Empty);
            }
        }

        private void PlanFolder_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is string path && path.Length > 0)
            {
                viewModel.OpenPath(path);
            }
        }

        // Açık iş: notu açar (Ayarlar'daki programla).
        // Yapılacaklar: Ekle düğmesi / Enter işi ekler; kutu işi kapatır ya da yeniden açar.
        private void TaskAdd_Click(object sender, RoutedEventArgs e)
        {
            viewModel.AddTask();
        }

        private void TaskBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && sender is TextBox box && !MentionAssist.IsOpen(box))
            {
                box.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
                viewModel.AddTask();
                e.Handled = true;
            }
        }

        private void TaskDone_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox box && box.DataContext is OpenTaskRow row)
            {
                viewModel.SetTaskDone(row, box.IsChecked == true);
                box.IsChecked = row.IsDone;
            }
        }

        // Başlangıç sayfası: son açılan düğüme gider.
        private void HomeRecent_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is TreeNodeViewModel node)
            {
                viewModel.SelectNodeByPath(node.Path);
            }
        }

        // Başlangıç sayfası: işin geldiği stoka / kayda gider.
        private void HomeTask_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is OpenTaskRow row)
            {
                viewModel.OpenHomeTask(row);
            }
        }

        private void OpenTaskNote_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is string path && path.Length > 0)
            {
                viewModel.OpenNoteOrFolder(path, System.IO.Path.GetDirectoryName(path) ?? string.Empty);
            }
        }

        // + Olay: gösterilen stokun ya da kaydın ana notuna.
        private void JournalAdd_Click(object sender, RoutedEventArgs e)
        {
            viewModel.OpenJournalHere();
        }

        // Günlük olayında sağ tık: düzenle / sil.
        private static Pusula.Core.Notes.EventNote? JournalOf(object sender)
        {
            var context = (sender as FrameworkElement)?.DataContext;
            return (context as TimelineItem)?.Journal ?? (context as HistoryRow)?.Journal;
        }

        private void JournalEdit_Click(object sender, RoutedEventArgs e)
        {
            var note = JournalOf(sender);
            if (note != null)
            {
                viewModel.OpenJournalEdit(note);
            }
        }

        private void JournalDelete_Click(object sender, RoutedEventArgs e)
        {
            var note = JournalOf(sender);
            if (note != null)
            {
                viewModel.OpenJournalDelete(note);
            }
        }

        private void JournalDelete_PrimaryButtonClick(object sender, RoutedEventArgs e)
        {
            viewModel.ConfirmJournalDelete();
        }

        private void Journal_PrimaryButtonClick(object sender, RoutedEventArgs e)
        {
            viewModel.ConfirmJournal();
        }

        private void NodeDelete_Click(object sender, RoutedEventArgs e)
        {
            viewModel.OpenDeleteSelected();
        }

        private void StockInvDelete_Click(object sender, RoutedEventArgs e)
        {
            var row = MenuRow(sender);
            if (row != null && row.Folder.Length > 0)
            {
                viewModel.OpenDelete(row.Folder);
            }
        }

        private void DetailRowDelete_Click(object sender, RoutedEventArgs e)
        {
            var row = DetailMenuRow(sender);
            if (row != null)
            {
                viewModel.OpenDelete(row.Path);
            }
        }

        private void Delete_PrimaryButtonClick(object sender, RoutedEventArgs e)
        {
            viewModel.ConfirmDelete();
        }

        /// <summary>Şablonlar penceresini açar.</summary>
        internal void EditTemplates(Window owner)
        {
            if (!viewModel.CanEditTemplates())
            {
                return;
            }

            var editor = new TemplateEditorWindow(new TemplateEditorViewModel(viewModel.RootPath, viewModel.Templates))
            {
                Owner = owner,
            };
            if (editor.ShowDialog() == true)
            {
                viewModel.ReloadTemplates();
            }
        }

        private void ModuleDialog_PrimaryButtonClick(object sender, RoutedEventArgs e)
        {
            viewModel.ConfirmAddModules();
        }

        private void StockDialog_PrimaryButtonClick(object sender, RoutedEventArgs e)
        {
            if (!viewModel.ConfirmNewStock())
            {
                Dispatcher.BeginInvoke(new Action(() => viewModel.IsStockDialogOpen = true));
            }
        }
    }
}
