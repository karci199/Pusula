using System.Collections.Generic;
using System.Windows.Input;
using Pusula.Wpf.Mvvm;

namespace Pusula.Wpf.ViewModels.TemplateEditor
{
    /// <summary>
    /// Düzenleyicide bir şablon: adı ve işaretli gelen modül anahtarları.
    /// </summary>
    public sealed class TemplateEditViewModel : ViewModelBase
    {
        private string name;

        /// <summary>Şablonu oluşturur.</summary>
        public TemplateEditViewModel(string name, IEnumerable<string> moduleKeys, ICommand deleteCommand)
        {
            this.name = name;
            ModuleKeys = new HashSet<string>(moduleKeys, System.StringComparer.OrdinalIgnoreCase);
            DeleteCommand = deleteCommand;
        }

        /// <summary>Şablon adı.</summary>
        public string Name
        {
            get { return name; }
            set { SetProperty(ref name, value ?? string.Empty); }
        }

        /// <summary>İşaretli gelen modül anahtarları.</summary>
        public HashSet<string> ModuleKeys { get; }

        /// <summary>Şablonu siler.</summary>
        public ICommand DeleteCommand { get; }
    }

    /// <summary>
    /// Bir modülün bir şablonda işaretli gelip gelmediği (modül düzenleyicideki onay kutusu).
    /// </summary>
    public sealed class TemplateFlagViewModel : ViewModelBase
    {
        private readonly string moduleKey;

        /// <summary>Bayrağı oluşturur.</summary>
        public TemplateFlagViewModel(TemplateEditViewModel template, string moduleKey)
        {
            Template = template;
            this.moduleKey = moduleKey;
        }

        /// <summary>İlgili şablon.</summary>
        public TemplateEditViewModel Template { get; }

        /// <summary>Modül bu şablonda işaretli mi.</summary>
        public bool IsChecked
        {
            get { return Template.ModuleKeys.Contains(moduleKey); }
            set
            {
                if (value)
                {
                    Template.ModuleKeys.Add(moduleKey);
                }
                else
                {
                    Template.ModuleKeys.Remove(moduleKey);
                }

                OnPropertyChanged();
            }
        }
    }
}
