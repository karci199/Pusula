using System.Windows;
using System.Windows.Controls;
using DesignKit.Controls;
using Pusula.Wpf.ViewModels.TypeEditor;

namespace Pusula.Wpf
{
    /// <summary>Dosya türleri penceresi: uzantı → ad, grup. Kaydedilirse <c>DialogResult</c> <c>true</c> olur.</summary>
    public partial class FileTypesWindow : ChromeWindow
    {
        private readonly FileTypesViewModel viewModel;

        /// <summary>Pencereyi verilen düzenleyiciyle açar.</summary>
        public FileTypesWindow(FileTypesViewModel viewModel)
        {
            InitializeComponent();
            this.viewModel = viewModel;
            DataContext = viewModel;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            Grid.CommitEdit(DataGridEditingUnit.Row, true);
            if (viewModel.Save())
            {
                DialogResult = true;
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
