using System.Windows;
using DesignKit.Controls;
using Pusula.Wpf.ViewModels.TemplateEditor;

namespace Pusula.Wpf
{
    /// <summary>
    /// Modül penceresi: yeni modül oluşturur ya da var olanı düzenler (ad, klasör, alt klasörler).
    /// </summary>
    public partial class ModuleWindow : ChromeWindow
    {
        private readonly ModuleDialogViewModel viewModel;

        /// <summary>Pencereyi verilen durumla açar.</summary>
        public ModuleWindow(ModuleDialogViewModel viewModel)
        {
            InitializeComponent();
            this.viewModel = viewModel;
            DataContext = viewModel;
            Loaded += (s, e) => NameBox.Focus();
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            if (viewModel.Confirm())
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
