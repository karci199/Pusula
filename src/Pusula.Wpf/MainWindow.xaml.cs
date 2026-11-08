using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using DesignKit.Controls;
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

            // ContextMenu görsel ağacın dışında durur; DataContext'i elle verilir.
            foreach (var key in new[] { "AddMenu", "NodeMenu", "TreeMenu" })
            {
                ((ContextMenu)FindResource(key)).DataContext = viewModel;
            }
        }

        private void FolderTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            viewModel.SelectedNode = e.NewValue as TreeNodeViewModel;
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
                item.Icon = new FontIcon { Glyph = AppIcons.Check };
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

        // Klasör ağacında çift tıklayınca klasörü açar.
        private void FolderTreeView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && !viewModel.IsDialogBusy && (sender as TreeView)?.SelectedItem is FolderTreeItem item)
            {
                viewModel.OpenPath(item.Path);
                e.Handled = true;
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
                viewModel.OpenPath(row.Path);
            }
        }

        private void EditTemplates_Click(object sender, RoutedEventArgs e)
        {
            if (!viewModel.CanEditTemplates())
            {
                return;
            }

            var editor = new TemplateEditorWindow(new TemplateEditorViewModel(viewModel.RootPath, viewModel.Templates))
            {
                Owner = this,
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
