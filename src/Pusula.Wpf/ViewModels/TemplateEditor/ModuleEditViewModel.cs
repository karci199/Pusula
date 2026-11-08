using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Pusula.Core.Templates;
using Pusula.Wpf.Mvvm;
using Pusula.Wpf.Properties;

namespace Pusula.Wpf.ViewModels.TemplateEditor
{
    /// <summary>
    /// Bir modülün düzenlenebilir hali: ad, numara, klasör adı, alt klasör ağacı.
    /// Hem kütüphanedeki modül hem de modül penceresindeki çalışma kopyası için kullanılır.
    /// </summary>
    public sealed class ModuleEditViewModel : ViewModelBase
    {
        private string name = string.Empty;
        private string number = string.Empty;
        private string folderName = string.Empty;

        /// <summary>Modül tanımından oluşturur.</summary>
        public ModuleEditViewModel(ModuleDefinition module)
        {
            Key = module.Key;
            AddSubFolderCommand = new RelayCommand(AddSubFolder);
            Load(module);
        }

        /// <summary>Değişmez anahtar (şablonlar bununla bağlanır).</summary>
        public string Key { get; }

        /// <summary>Görünen ad.</summary>
        public string Name
        {
            get { return name; }
            set { SetProperty(ref name, value ?? string.Empty); }
        }

        /// <summary>Klasör numarası (örn. 03); boş olabilir.</summary>
        public string Number
        {
            get { return number; }
            set
            {
                if (SetProperty(ref number, (value ?? string.Empty).Trim()))
                {
                    OnPropertyChanged(nameof(Folder));
                }
            }
        }

        /// <summary>Numarasız klasör adı (örn. Test_Sonuclari).</summary>
        public string FolderName
        {
            get { return folderName; }
            set
            {
                if (SetProperty(ref folderName, (value ?? string.Empty).Trim()))
                {
                    OnPropertyChanged(nameof(Folder));
                }
            }
        }

        /// <summary>Tam klasör adı: numara + "_" + ad, ya da yalnızca ad.</summary>
        public string Folder
        {
            get { return number.Length == 0 ? folderName : number + "_" + folderName; }
        }

        /// <summary>Alt klasör ağacının kökleri.</summary>
        public ObservableCollection<FolderNodeViewModel> SubFolders { get; } = new ObservableCollection<FolderNodeViewModel>();

        /// <summary>Alt klasör sayısı metni (şablon listesinde gösterilir).</summary>
        public string SubFolderText
        {
            get
            {
                var count = FolderNodeViewModel.All(SubFolders).Count();
                return count == 0 ? string.Empty : string.Format(Resources.Editor_SubFolderCount, count);
            }
        }

        /// <summary>Modül klasörünün hemen altına klasör ekler.</summary>
        public ICommand AddSubFolderCommand { get; }

        private void AddSubFolder()
        {
            SubFolders.Add(new FolderNodeViewModel(Resources.Editor_NewFolder, SubFolders));
        }

        /// <summary>Düzenlenen değerlerle modül tanımı üretir.</summary>
        public ModuleDefinition ToDefinition()
        {
            return new ModuleDefinition
            {
                Key = Key,
                Name = name.Trim(),
                Folder = Folder,
                SubFolders = FolderNodeViewModel.Flatten(SubFolders),
            };
        }

        /// <summary>Değerleri bir tanımdan (örn. modül penceresindeki kopyadan) yükler.</summary>
        public void Load(ModuleDefinition module)
        {
            Name = module.Name;
            var parts = ModuleDefinition.SplitFolder(module.Folder);
            Number = parts.Item1;
            FolderName = parts.Item2;
            SubFolders.Clear();
            FolderNodeViewModel.BuildTree(module.SubFolders, SubFolders);
            OnPropertyChanged(nameof(SubFolderText));
        }
    }
}
