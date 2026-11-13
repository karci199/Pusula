using Pusula.Core.Templates;
using Pusula.Wpf.Mvvm;

namespace Pusula.Wpf.ViewModels
{
    /// <summary>
    /// Diyalogdaki bir modül onay kutusu.
    /// </summary>
    public sealed class ModuleOptionViewModel : ViewModelBase
    {
        private bool isChecked;

        /// <summary>Modül tanımından oluşturur.</summary>
        public ModuleOptionViewModel(ModuleDefinition module, bool exists)
        {
            Module = module;
            Exists = exists;
            isChecked = exists;
        }

        /// <summary>Modül tanımı.</summary>
        public ModuleDefinition Module { get; }

        /// <summary>Modül adı.</summary>
        public string Name
        {
            get { return Module.Name; }
        }

        /// <summary>Açılacak klasör adı.</summary>
        public string Folder
        {
            get { return Module.Folder; }
        }

        /// <summary>Klasör stokta zaten var (değiştirilemez, işaretli görünür).</summary>
        public bool Exists { get; }

        /// <summary>Onay kutusu kullanıcı tarafından değiştirilebilir mi.</summary>
        public bool CanChange
        {
            get { return !Exists; }
        }

        /// <summary>İşaretli mi.</summary>
        public bool IsChecked
        {
            get { return isChecked; }
            set
            {
                if (!Exists)
                {
                    SetProperty(ref isChecked, value);
                }
            }
        }
    }
}
