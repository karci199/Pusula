using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using DesignKit.Controls;
using Pusula.Wpf.ViewModels;

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
