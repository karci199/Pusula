using System;
using System.Windows;
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
        }

        private void FolderTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            viewModel.SelectedNode = e.NewValue as TreeNodeViewModel;
        }

        // ContentDialog birincil düğmede kendiliğinden kapanır; hata varsa hata metniyle yeniden açılır.
        private void ProjectDialog_PrimaryButtonClick(object sender, RoutedEventArgs e)
        {
            if (!viewModel.ConfirmNewProject())
            {
                Dispatcher.BeginInvoke(new Action(() => viewModel.IsProjectDialogOpen = true));
            }
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
