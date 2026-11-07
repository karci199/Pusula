using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Pusula.Core.Templates;
using Pusula.Core.Tracking;
using Pusula.Wpf.Mvvm;
using Pusula.Wpf.Properties;

namespace Pusula.Wpf.ViewModels.TemplateEditor
{
    /// <summary>
    /// Şablonlar penceresinin durumu. Şablon dosyasının bir kopyası üzerinde çalışır;
    /// yalnızca <see cref="Save"/> başarılı olursa dosya değişir.
    /// </summary>
    public sealed class TemplateEditorViewModel : ViewModelBase
    {
        private readonly string root;
        private readonly List<string> projectFolders;
        private ModuleEditViewModel? selectedModule;
        private string error = string.Empty;

        /// <summary>Verilen kökün şablonlarıyla düzenleyiciyi hazırlar.</summary>
        public TemplateEditorViewModel(string root, TemplateSet source)
        {
            this.root = root;
            projectFolders = new List<string>(source.ProjectFolders);

            foreach (var module in source.Modules)
            {
                Modules.Add(new ModuleEditViewModel(module));
            }

            foreach (var preset in source.Templates)
            {
                Templates.Add(CreateTemplate(preset.Name, preset.Modules));
            }

            RebuildFlags();
            AddModuleCommand = new RelayCommand(AddModule);
            DeleteModuleCommand = new RelayCommand(DeleteModule);
            AddTemplateCommand = new RelayCommand(AddTemplate);
            SelectedModule = Modules.FirstOrDefault();
        }

        /// <summary>Şablon dosyasının yolu (bilgi metni).</summary>
        public string FileNote
        {
            get { return string.Format(Resources.Editor_SharedNote, TemplateStore.FilePath(root)); }
        }

        /// <summary>Modüller.</summary>
        public ObservableCollection<ModuleEditViewModel> Modules { get; } = new ObservableCollection<ModuleEditViewModel>();

        /// <summary>Şablonlar.</summary>
        public ObservableCollection<TemplateEditViewModel> Templates { get; } = new ObservableCollection<TemplateEditViewModel>();

        /// <summary>Sağda düzenlenen modül.</summary>
        public ModuleEditViewModel? SelectedModule
        {
            get { return selectedModule; }
            set
            {
                if (SetProperty(ref selectedModule, value))
                {
                    OnPropertyChanged(nameof(HasSelection));
                }
            }
        }

        /// <summary>Bir modül seçili mi.</summary>
        public bool HasSelection
        {
            get { return selectedModule != null; }
        }

        /// <summary>Kaydetme hatası; boşsa gizlidir.</summary>
        public string Error
        {
            get { return error; }
            private set { SetProperty(ref error, value); }
        }

        /// <summary>Yeni modül ekler (sıradaki boş numarayla).</summary>
        public ICommand AddModuleCommand { get; }

        /// <summary>Seçili modülü şablondan çıkarır (klasörlere dokunmaz).</summary>
        public ICommand DeleteModuleCommand { get; }

        /// <summary>Yeni şablon ekler.</summary>
        public ICommand AddTemplateCommand { get; }

        private void AddModule()
        {
            var used = new HashSet<int>();
            foreach (var module in Modules)
            {
                int n;
                if (int.TryParse(module.Number, out n))
                {
                    used.Add(n);
                }
            }

            var next = 1;
            while (used.Contains(next))
            {
                next++;
            }

            var definition = new ModuleDefinition
            {
                Key = "m" + Guid.NewGuid().ToString("N").Substring(0, 8),
                Name = Resources.Editor_NewModule,
                Folder = next.ToString("00") + "_" + Resources.Editor_NewFolder,
            };
            var created = new ModuleEditViewModel(definition);
            created.RebuildFlags(Templates);
            Modules.Add(created);
            SelectedModule = created;
        }

        private void DeleteModule()
        {
            if (selectedModule == null)
            {
                return;
            }

            var index = Modules.IndexOf(selectedModule);
            foreach (var template in Templates)
            {
                template.ModuleKeys.Remove(selectedModule.Key);
            }

            Modules.Remove(selectedModule);
            SelectedModule = Modules.Count == 0 ? null : Modules[Math.Min(index, Modules.Count - 1)];
        }

        private void AddTemplate()
        {
            Templates.Add(CreateTemplate(Resources.Editor_NewTemplate, Enumerable.Empty<string>()));
            RebuildFlags();
        }

        private TemplateEditViewModel CreateTemplate(string name, IEnumerable<string> keys)
        {
            TemplateEditViewModel? created = null;
            created = new TemplateEditViewModel(name, keys, new RelayCommand(() =>
            {
                Templates.Remove(created!);
                RebuildFlags();
            }));
            return created;
        }

        private void RebuildFlags()
        {
            foreach (var module in Modules)
            {
                module.RebuildFlags(Templates);
            }
        }

        /// <summary>
        /// Değerleri doğrular ve dosyaya yazar. Hata varsa <c>false</c> döner ve <see cref="Error"/> dolar.
        /// </summary>
        public bool Save()
        {
            var problem = Validate();
            if (problem.Length > 0)
            {
                Error = problem;
                return false;
            }

            var set = new TemplateSet();
            set.ProjectFolders.AddRange(projectFolders);
            var optionalKeys = new HashSet<string>(Modules.Where(m => !m.Core).Select(m => m.Key), StringComparer.OrdinalIgnoreCase);
            foreach (var module in Modules)
            {
                set.Modules.Add(module.ToDefinition());
            }

            foreach (var template in Templates)
            {
                set.Templates.Add(new TemplatePreset
                {
                    Name = template.Name.Trim(),
                    Modules = template.ModuleKeys.Where(optionalKeys.Contains).ToList(),
                });
            }

            try
            {
                TemplateStore.Save(root, set);
            }
            catch (IOException ex)
            {
                Error = string.Format(Resources.Error_Io, ex.Message);
                return false;
            }
            catch (UnauthorizedAccessException ex)
            {
                Error = string.Format(Resources.Error_Io, ex.Message);
                return false;
            }

            Error = string.Empty;
            return true;
        }

        private string Validate()
        {
            var folders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var module in Modules)
            {
                if (module.Name.Trim().Length == 0)
                {
                    return Resources.Editor_ErrorModuleName;
                }

                if (!IsSingleSegment(module.Folder))
                {
                    return string.Format(Resources.Editor_ErrorFolder, module.Folder);
                }

                if (!folders.Add(module.Folder))
                {
                    return string.Format(Resources.Editor_ErrorDuplicateFolder, module.Folder);
                }

                foreach (var node in FolderNodeViewModel.All(module.SubFolders))
                {
                    if (!IsSingleSegment(node.Name.Trim()))
                    {
                        return string.Format(Resources.Editor_ErrorSubFolder, module.Folder, node.Name);
                    }
                }
            }

            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var template in Templates)
            {
                if (template.Name.Trim().Length == 0 || !names.Add(template.Name.Trim()))
                {
                    return Resources.Editor_ErrorTemplateName;
                }
            }

            return string.Empty;
        }

        private static bool IsSingleSegment(string name)
        {
            return ProjectService.IsSafeRelativePath(name) && name.IndexOfAny(new[] { '\\', '/' }) < 0;
        }
    }
}
