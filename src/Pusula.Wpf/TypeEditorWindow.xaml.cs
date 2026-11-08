using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DesignKit.Controls;
using Pusula.Wpf.ViewModels.TypeEditor;

namespace Pusula.Wpf
{
    /// <summary>
    /// Türler penceresi: envanter türlerinin numara biçimleri, alt türleri, alanları, durumları ve klasörleri.
    /// Kaydedilirse <c>DialogResult</c> <c>true</c> olur.
    /// </summary>
    public partial class TypeEditorWindow : ChromeWindow
    {
        private readonly TypeEditorViewModel viewModel;

        /// <summary>Pencereyi verilen düzenleyici durumuyla açar.</summary>
        public TypeEditorWindow(TypeEditorViewModel viewModel)
        {
            InitializeComponent();
            this.viewModel = viewModel;
            DataContext = viewModel;
        }

        // Tablolar kendi içinde kaymaz; tekerlek sağ paneli kaydırsın.
        private void Grid_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true;
            Pane.ScrollToVerticalOffset(Pane.VerticalOffset - e.Delta);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // Düzenlenmekte olan hücre önce tablolara işlensin.
            foreach (var grid in FindGrids(Pane))
            {
                grid.CommitEdit(DataGridEditingUnit.Row, true);
            }

            if (viewModel.Save())
            {
                DialogResult = true;
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private static System.Collections.Generic.IEnumerable<DataGrid> FindGrids(DependencyObject parent)
        {
            for (var i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is DataGrid grid)
                {
                    yield return grid;
                }

                foreach (var nested in FindGrids(child))
                {
                    yield return nested;
                }
            }
        }
    }
}
