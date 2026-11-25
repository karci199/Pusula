using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Input;
using Pusula.Core.Templates;
using Pusula.Core.Tracking;
using Pusula.Wpf.Mvvm;
using Pusula.Wpf.Properties;

namespace Pusula.Wpf.ViewModels.TemplateEditor
{
    /// <summary>
    /// Şablonlar penceresinin durumu: solda şablonlar, sağda seçili şablonun modülleri.
    /// Şablon dosyasının bir kopyası üzerinde çalışır; yalnızca <see cref="Save"/> başarılı olursa dosya değişir.
    /// </summary>
    public sealed class TemplateEditorViewModel : ViewModelBase
    {
        private readonly string root;
        private readonly List<string> projectFolders;
        private TemplateEditViewModel? selectedTemplate;
        private string error = string.Empty;

        /// <summary>Verilen kökün şablonlarıyla düzenleyiciyi hazırlar.</summary>
        public TemplateEditorViewModel(string root, TemplateSet source)
        {
            this.root = root;
            projectFolders = new List<string>(source.ProjectFolders);

            foreach (var module in TemplateSet.Sorted(source.Modules))
            {
                Library.Add(new ModuleEditViewModel(module));
            }

            foreach (var preset in source.Templates)
            {
                var keys = new HashSet<string>(preset.Modules, StringComparer.OrdinalIgnoreCase);
                Templates.Add(new TemplateEditViewModel(preset.Name, Library.Where(m => keys.Contains(m.Key))));
            }

            foreach (var document in source.Documents)
            {
                Documents.Add(new DocumentEditRow(document));
            }

            AddTemplateCommand = new RelayCommand(AddTemplate);
            DeleteTemplateCommand = new RelayCommand(DeleteTemplate);
            RemoveModuleCommand = new RelayCommand(RemoveModule);
            SelectedTemplate = Templates.FirstOrDefault();
        }

        /// <summary>Şablon dosyasının yolu (bilgi metni).</summary>
        public string FileNote
        {
            get { return string.Format(Resources.Editor_SharedNote, TemplateStore.FilePath(root)); }
        }

        /// <summary>Modül kütüphanesi: tüm şablonların seçebildiği modüller.</summary>
        public ObservableCollection<ModuleEditViewModel> Library { get; } = new ObservableCollection<ModuleEditViewModel>();

        /// <summary>Stokta izlenen dokümanlar (bütün şablonlar için ortak; 25.11.2026).</summary>
        public ObservableCollection<DocumentEditRow> Documents { get; } = new ObservableCollection<DocumentEditRow>();

        /// <summary>Şablonlar (sol liste).</summary>
        public ObservableCollection<TemplateEditViewModel> Templates { get; } = new ObservableCollection<TemplateEditViewModel>();

        /// <summary>Sağda düzenlenen şablon.</summary>
        public TemplateEditViewModel? SelectedTemplate
        {
            get { return selectedTemplate; }
            set
            {
                if (selectedTemplate != null)
                {
                    selectedTemplate.PropertyChanged -= OnTemplateChanged;
                }

                if (SetProperty(ref selectedTemplate, value))
                {
                    OnPropertyChanged(nameof(HasSelection));
                    OnPropertyChanged(nameof(UsageText));
                }

                if (selectedTemplate != null)
                {
                    selectedTemplate.PropertyChanged += OnTemplateChanged;
                }
            }
        }

        /// <summary>Bir şablon seçili mi.</summary>
        public bool HasSelection
        {
            get { return selectedTemplate != null; }
        }

        /// <summary>Seçili modülün kaç şablonda kullanıldığı (değişikliğin hepsine yansıdığı uyarısı).</summary>
        public string UsageText
        {
            get
            {
                var module = selectedTemplate?.SelectedModule;
                if (module == null)
                {
                    return string.Empty;
                }

                var count = Templates.Count(t => t.Modules.Contains(module));
                return string.Format(Resources.Editor_ModuleUsage, module.Name, count);
            }
        }

        /// <summary>Kaydetme hatası; boşsa gizlidir.</summary>
        public string Error
        {
            get { return error; }
            private set { SetProperty(ref error, value); }
        }

        /// <summary>Yeni şablon ekler (temel modüllerle).</summary>
        public ICommand AddTemplateCommand { get; }

        /// <summary>Seçili şablonu siler (klasörlere dokunmaz).</summary>
        public ICommand DeleteTemplateCommand { get; }

        /// <summary>Parametre olarak verilen modülü seçili şablondan çıkarır.</summary>
        public ICommand RemoveModuleCommand { get; }

        private void OnTemplateChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TemplateEditViewModel.SelectedModule))
            {
                OnPropertyChanged(nameof(UsageText));
            }
        }

        private void AddTemplate()
        {
            var basic = new HashSet<string>(TemplateSet.BasicModuleKeys, StringComparer.OrdinalIgnoreCase);
            var name = Resources.Editor_NewTemplate;
            var suffix = 2;
            while (Templates.Any(t => string.Equals(t.Name.Trim(), name, StringComparison.OrdinalIgnoreCase)))
            {
                name = Resources.Editor_NewTemplate + " " + suffix++;
            }

            var created = new TemplateEditViewModel(name, Library.Where(m => basic.Contains(m.Key)));
            Templates.Add(created);
            SelectedTemplate = created;
        }

        private void DeleteTemplate()
        {
            if (selectedTemplate == null)
            {
                return;
            }

            var index = Templates.IndexOf(selectedTemplate);
            Templates.Remove(selectedTemplate);
            SelectedTemplate = Templates.Count == 0 ? null : Templates[Math.Min(index, Templates.Count - 1)];
        }

        private void RemoveModule(object? parameter)
        {
            var module = parameter as ModuleEditViewModel;
            if (selectedTemplate == null || module == null)
            {
                return;
            }

            selectedTemplate.Modules.Remove(module);
            if (selectedTemplate.SelectedModule == module)
            {
                selectedTemplate.SelectedModule = null;
            }
        }

        /// <summary>Seçili şablonda olmayan kütüphane modülleri ("Modül ekle" listesi).</summary>
        public IReadOnlyList<ModuleEditViewModel> AvailableModules()
        {
            if (selectedTemplate == null)
            {
                return new List<ModuleEditViewModel>();
            }

            return Library.Where(m => !selectedTemplate.Modules.Contains(m)).ToList();
        }

        /// <summary>Kütüphanedeki bir modülü seçili şablona ekler.</summary>
        public void AddToSelected(ModuleEditViewModel module)
        {
            selectedTemplate?.Add(module);
            OnPropertyChanged(nameof(UsageText));
        }

        /// <summary>Yeni modül penceresi için boş bir tanım hazırlar (numara ayrıca <see cref="NextNumber"/> ile verilir).</summary>
        public ModuleDefinition NewModuleDraft()
        {
            return new ModuleDefinition
            {
                Key = "m" + Guid.NewGuid().ToString("N").Substring(0, 8),
                Name = string.Empty,
                Folder = string.Empty,
            };
        }

        /// <summary>Kütüphanede kullanılmayan en küçük klasör numarası (örn. 08).</summary>
        public string NextNumber()
        {
            var used = new HashSet<int>();
            foreach (var module in Library)
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

            return next.ToString("00");
        }

        /// <summary>Modül penceresinde oluşturulan modülü kütüphaneye ve seçili şablona ekler.</summary>
        public void AddNewModule(ModuleDefinition definition)
        {
            var created = new ModuleEditViewModel(definition);
            var index = 0;
            while (index < Library.Count && string.Compare(Library[index].Folder, created.Folder, StringComparison.OrdinalIgnoreCase) < 0 && !Library[index].Folder.StartsWith("_", StringComparison.Ordinal))
            {
                index++;
            }

            Library.Insert(index, created);
            AddToSelected(created);
        }

        /// <summary>Modül penceresinde düzenlenen değerleri kütüphanedeki modüle uygular.</summary>
        public void ApplyModuleEdit(ModuleEditViewModel target, ModuleDefinition edited)
        {
            target.Load(edited);
            foreach (var template in Templates)
            {
                template.Resort();
            }

            OnPropertyChanged(nameof(UsageText));
        }

        /// <summary>
        /// Modül penceresindeki değerleri doğrular. Boş dönerse geçerlidir; değilse hata metnidir.
        /// <paramref name="original"/> düzenlenen modüldür (yeni modülde <c>null</c>).
        /// </summary>
        public string ValidateModule(ModuleDefinition candidate, ModuleEditViewModel? original)
        {
            if (candidate.Name.Trim().Length == 0)
            {
                return Resources.Editor_ErrorModuleName;
            }

            if (!IsSingleSegment(candidate.Folder) || ModuleDefinition.SplitFolder(candidate.Folder).Item2.Length == 0)
            {
                return string.Format(Resources.Editor_ErrorFolder, candidate.Folder);
            }

            if (Library.Any(m => m != original && string.Equals(m.Folder, candidate.Folder, StringComparison.OrdinalIgnoreCase)))
            {
                return string.Format(Resources.Editor_ErrorDuplicateFolder, candidate.Folder);
            }

            foreach (var sub in candidate.SubFolders)
            {
                foreach (var part in sub.Split('\\'))
                {
                    if (!IsSingleSegment(part))
                    {
                        return string.Format(Resources.Editor_ErrorSubFolder, candidate.Folder, part);
                    }
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Değerleri doğrular ve dosyaya yazar. Hata varsa <c>false</c> döner ve <see cref="Error"/> dolar.
        /// </summary>
        public bool Save()
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var template in Templates)
            {
                if (template.Name.Trim().Length == 0 || !names.Add(template.Name.Trim()))
                {
                    SelectedTemplate = template;
                    Error = Resources.Editor_ErrorTemplateName;
                    return false;
                }
            }

            var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in Documents.Where(d => !d.IsBlank))
            {
                var code = row.Code.Trim();
                if (code.Length > 0 && (!Regex.IsMatch(code, "^[A-Za-z0-9-]+$") || !codes.Add(code)))
                {
                    Error = string.Format(Resources.Editor_ErrorDocumentCode, code);
                    return false;
                }

                if (row.Name.Trim().Length == 0)
                {
                    Error = Resources.Editor_ErrorDocumentName;
                    return false;
                }
            }

            var set = new TemplateSet();
            set.ProjectFolders.AddRange(projectFolders);
            set.Documents = Documents.Where(d => !d.IsBlank).Select(d => d.ToDefinition()).ToList();
            foreach (var module in Library)
            {
                set.Modules.Add(module.ToDefinition());
            }

            foreach (var template in Templates)
            {
                set.Templates.Add(new TemplatePreset
                {
                    Name = template.Name.Trim(),
                    Modules = template.Modules.Select(m => m.Key).ToList(),
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

        private static bool IsSingleSegment(string name)
        {
            return ProjectService.IsSafeRelativePath(name) && name.IndexOfAny(new[] { '\\', '/' }) < 0;
        }
    }
}
