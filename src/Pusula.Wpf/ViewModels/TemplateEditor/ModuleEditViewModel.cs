using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Pusula.Core.Templates;
using Pusula.Wpf.Mvvm;
using Pusula.Wpf.Properties;

namespace Pusula.Wpf.ViewModels.TemplateEditor
{
    /// <summary>
    /// Düzenleyicide bir modül: ad, numara, klasör adı, her stokta mı, alt klasör ağacı, şablon bayrakları.
    /// </summary>
    public sealed class ModuleEditViewModel : ViewModelBase
    {
        private string name;
        private string number;
        private string folderName;
        private bool core;

        /// <summary>Var olan bir modül tanımından oluşturur.</summary>
        public ModuleEditViewModel(ModuleDefinition module)
        {
            Key = module.Key;
            name = module.Name;
            var parts = ModuleDefinition.SplitFolder(module.Folder);
            number = parts.Item1;
            folderName = parts.Item2;
            core = module.Core;
            FolderNodeViewModel.BuildTree(module.SubFolders, SubFolders);
            AddSubFolderCommand = new RelayCommand(AddSubFolder);
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

        /// <summary>Her yeni stokta açılır mı.</summary>
        public bool Core
        {
            get { return core; }
            set
            {
                if (SetProperty(ref core, value))
                {
                    OnPropertyChanged(nameof(IsOptional));
                }
            }
        }

        /// <summary>Seçilebilen modül mü (şablon bayrakları yalnızca bunlarda anlamlı).</summary>
        public bool IsOptional
        {
            get { return !core; }
        }

        /// <summary>Alt klasör ağacının kökleri.</summary>
        public ObservableCollection<FolderNodeViewModel> SubFolders { get; } = new ObservableCollection<FolderNodeViewModel>();

        /// <summary>Bu modülün hangi şablonlarda işaretli geldiği.</summary>
        public ObservableCollection<TemplateFlagViewModel> Flags { get; } = new ObservableCollection<TemplateFlagViewModel>();

        /// <summary>Modül klasörünün hemen altına klasör ekler.</summary>
        public ICommand AddSubFolderCommand { get; }

        private void AddSubFolder()
        {
            SubFolders.Add(new FolderNodeViewModel(Resources.Editor_NewFolder, SubFolders));
        }

        /// <summary>Şablon listesine göre bayrakları yeniden kurar.</summary>
        public void RebuildFlags(IEnumerable<TemplateEditViewModel> templates)
        {
            Flags.Clear();
            foreach (var template in templates)
            {
                Flags.Add(new TemplateFlagViewModel(template, Key));
            }
        }

        /// <summary>Düzenlenen değerlerle modül tanımı üretir.</summary>
        public ModuleDefinition ToDefinition()
        {
            return new ModuleDefinition
            {
                Key = Key,
                Name = name.Trim(),
                Folder = Folder,
                Core = core,
                SubFolders = FolderNodeViewModel.Flatten(SubFolders),
            };
        }
    }
}
