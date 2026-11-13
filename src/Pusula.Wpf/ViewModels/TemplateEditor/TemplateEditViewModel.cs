using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Pusula.Wpf.Mvvm;

namespace Pusula.Wpf.ViewModels.TemplateEditor
{
    /// <summary>
    /// Düzenleyicide bir şablon: adı ve stokta açılacak modüllerin listesi (kütüphanedeki modüllere başvurur).
    /// </summary>
    public sealed class TemplateEditViewModel : ViewModelBase
    {
        private string name;
        private ModuleEditViewModel? selectedModule;

        /// <summary>Şablonu oluşturur.</summary>
        public TemplateEditViewModel(string name, IEnumerable<ModuleEditViewModel> modules)
        {
            this.name = name;
            foreach (var module in modules)
            {
                Modules.Add(module);
            }

            Modules.CollectionChanged += (s, e) => OnPropertyChanged(nameof(ModuleCount));
        }

        /// <summary>Şablon adı.</summary>
        public string Name
        {
            get { return name; }
            set { SetProperty(ref name, value ?? string.Empty); }
        }

        /// <summary>Şablondaki modüller, klasör sırasıyla.</summary>
        public ObservableCollection<ModuleEditViewModel> Modules { get; } = new ObservableCollection<ModuleEditViewModel>();

        /// <summary>Modül sayısı (sol listede gösterilir).</summary>
        public int ModuleCount
        {
            get { return Modules.Count; }
        }

        /// <summary>Alt klasörleri gösterilen modül.</summary>
        public ModuleEditViewModel? SelectedModule
        {
            get { return selectedModule; }
            set { SetProperty(ref selectedModule, value); }
        }

        /// <summary>Modülü klasör sırasını bozmadan ekler; zaten varsa bir şey yapmaz.</summary>
        public void Add(ModuleEditViewModel module)
        {
            if (Modules.Contains(module))
            {
                return;
            }

            var index = 0;
            while (index < Modules.Count && Compare(Modules[index], module) < 0)
            {
                index++;
            }

            Modules.Insert(index, module);
            SelectedModule = module;
        }

        /// <summary>Klasör sırasını yeniden kurar (modül klasörü değişince).</summary>
        public void Resort()
        {
            var sorted = Modules.OrderBy(m => m, Comparer<ModuleEditViewModel>.Create(Compare)).ToList();
            for (var i = 0; i < sorted.Count; i++)
            {
                var current = Modules.IndexOf(sorted[i]);
                if (current != i)
                {
                    Modules.Move(current, i);
                }
            }
        }

        private static int Compare(ModuleEditViewModel a, ModuleEditViewModel b)
        {
            var aLast = a.Folder.StartsWith("_", StringComparison.Ordinal);
            var bLast = b.Folder.StartsWith("_", StringComparison.Ordinal);
            if (aLast != bLast)
            {
                return aLast ? 1 : -1;
            }

            return string.Compare(a.Folder, b.Folder, StringComparison.OrdinalIgnoreCase);
        }
    }
}
