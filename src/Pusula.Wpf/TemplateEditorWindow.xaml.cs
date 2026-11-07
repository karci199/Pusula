using System.Windows;
using DesignKit.Controls;
using Pusula.Wpf.ViewModels.TemplateEditor;

namespace Pusula.Wpf
{
    /// <summary>
    /// Şablonlar penceresi: modülleri, alt klasörlerini ve şablonları düzenler.
    /// Kaydedilirse <c>DialogResult</c> <c>true</c> olur.
    /// </summary>
    public partial class TemplateEditorWindow : ChromeWindow
    {
        private readonly TemplateEditorViewModel viewModel;

        /// <summary>Pencereyi verilen düzenleyici durumuyla açar.</summary>
        public TemplateEditorWindow(TemplateEditorViewModel viewModel)
        {
            InitializeComponent();
            this.viewModel = viewModel;
            DataContext = viewModel;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
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
