using System.Linq;
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

        // Kişi tablosu görünür olunca alan sütunları kişi türünün o anki alanlarından yeniden kurulur
        // (metin alanı kutu, liste alanı açılır liste); ad, rol ve ayrıldı sütunları sabit.
        private void PeopleGrid_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!PeopleGrid.IsVisible)
            {
                return;
            }

            // Sabit sütunlar: ad, rol, ayrıldı; alan sütunları bunlardan sonra.
            while (PeopleGrid.Columns.Count > 3)
            {
                PeopleGrid.Columns.RemoveAt(3);
            }

            var columns = viewModel.PersonColumns();
            for (var i = 0; i < columns.Count; i++)
            {
                var field = columns[i];
                var path = new System.Windows.Data.Binding("Values[" + TypeEditorViewModel.ColumnKey(field) + "]");
                var width = i == columns.Count - 1 ? new DataGridLength(1, DataGridLengthUnitType.Star) : new DataGridLength(150);
                var choices = field.Choices.Split(new[] { ',', ';' }, System.StringSplitOptions.RemoveEmptyEntries).Select(c => c.Trim()).Where(c => c.Length > 0).ToList();
                if (field.Kind == Pusula.Core.Types.FieldKind.Choice && choices.Count > 0)
                {
                    PeopleGrid.Columns.Add(new DataGridComboBoxColumn { Header = field.Name, Width = width, ItemsSource = choices, SelectedItemBinding = path });
                }
                else
                {
                    PeopleGrid.Columns.Add(new DataGridTextColumn { Header = field.Name, Width = width, Binding = path, EditingElementStyle = (Style)FindResource("GridEditBox") });
                }
            }
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
