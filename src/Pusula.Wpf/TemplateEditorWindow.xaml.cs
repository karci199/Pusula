using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DesignKit.Controls;
using Pusula.Wpf.ViewModels.TemplateEditor;

namespace Pusula.Wpf
{
    /// <summary>
    /// Şablonlar penceresi: solda şablonlar, sağda seçili şablonun modülleri.
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

        // "Modül ekle" menüsü her açılışta yeniden kurulur: şablonda olmayan modüller + "Yeni modül oluştur…".
        private void AddModuleMenu_Opened(object sender, RoutedEventArgs e)
        {
            var menu = (ContextMenu)sender;
            menu.Items.Clear();

            var available = viewModel.AvailableModules();
            if (available.Count == 0)
            {
                menu.Items.Add(new MenuItem { Header = Properties.Resources.Editor_NoMoreModules, IsEnabled = false });
            }

            foreach (var module in available)
            {
                var item = new MenuItem
                {
                    Header = module.Name,
                    InputGestureText = module.Folder,
                };
                item.Click += (s, args) => viewModel.AddToSelected(module);
                menu.Items.Add(item);
            }

            menu.Items.Add(new Separator());
            var create = new MenuItem
            {
                Header = Properties.Resources.Editor_CreateModule,
                Icon = new AppIcon { Glyph = AppIcons.Add },
            };
            create.Click += (s, args) => OpenModuleDialog(null);
            menu.Items.Add(create);
        }

        private void EditModule_Click(object sender, RoutedEventArgs e)
        {
            var module = (sender as FrameworkElement)?.DataContext as ModuleEditViewModel;
            if (module != null)
            {
                OpenModuleDialog(module);
            }
        }

        private void ModuleList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var module = viewModel.SelectedTemplate?.SelectedModule;
            if (module != null)
            {
                OpenModuleDialog(module);
            }
        }

        private void OpenModuleDialog(ModuleEditViewModel? module)
        {
            var dialog = new ModuleWindow(new ModuleDialogViewModel(viewModel, module))
            {
                Owner = this,
            };
            dialog.ShowDialog();
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
