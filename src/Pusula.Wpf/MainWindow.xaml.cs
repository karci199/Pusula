using System;
using System.Windows;
using System.Windows.Controls;
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

        // Özet tablosunda satıra çift tıklayınca o klasör açılır.
        private void DetailList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if ((sender as ListView)?.SelectedItem is DetailRowViewModel row)
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
