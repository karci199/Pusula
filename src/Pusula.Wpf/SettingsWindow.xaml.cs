using System.Windows;
using DesignKit.Controls;
using Pusula.Wpf.ViewModels;

namespace Pusula.Wpf
{
    /// <summary>Ayarlar: kök klasör, tema ve tanım pencereleri (şablon, tür, dosya türü). Değişiklikler hemen uygulanır.</summary>
    public partial class SettingsWindow : ChromeWindow
    {
        private readonly MainWindow main;

        /// <summary>Pencereyi ana pencerenin görünüm modeline bağlar.</summary>
        public SettingsWindow(MainWindow main, MainViewModel viewModel)
        {
            this.main = main;
            InitializeComponent();
            DataContext = viewModel;
        }

        private void Templates_Click(object sender, RoutedEventArgs e)
        {
            main.EditTemplates(this);
        }

        private void Types_Click(object sender, RoutedEventArgs e)
        {
            main.EditTypes(this);
        }

        private void FileTypes_Click(object sender, RoutedEventArgs e)
        {
            main.EditFileTypes(this);
        }

        private void Trash_Click(object sender, RoutedEventArgs e)
        {
            ((MainViewModel)DataContext).OpenTrash();
        }

        private void ResetLayout_Click(object sender, RoutedEventArgs e)
        {
            main.ResetLayout();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
