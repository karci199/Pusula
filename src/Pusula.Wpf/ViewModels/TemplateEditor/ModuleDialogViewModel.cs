using System.ComponentModel;
using System.Text;
using Pusula.Core.Templates;
using Pusula.Wpf.Mvvm;
using Pusula.Wpf.Properties;

namespace Pusula.Wpf.ViewModels.TemplateEditor
{
    /// <summary>
    /// Modül penceresinin durumu: bir çalışma kopyası üzerinde düzenlenir; onaylanınca uygulanır.
    /// </summary>
    public sealed class ModuleDialogViewModel : ViewModelBase
    {
        private readonly TemplateEditorViewModel editor;
        private readonly ModuleEditViewModel? original;
        private string error = string.Empty;
        private bool folderNameTouched;
        private bool updatingFolderName;

        /// <summary>
        /// Pencereyi hazırlar. <paramref name="original"/> <c>null</c> ise yeni modül oluşturulur.
        /// </summary>
        public ModuleDialogViewModel(TemplateEditorViewModel editor, ModuleEditViewModel? original)
        {
            this.editor = editor;
            this.original = original;
            if (original == null)
            {
                Working = new ModuleEditViewModel(editor.NewModuleDraft());
                Working.Number = editor.NextNumber();
            }
            else
            {
                Working = new ModuleEditViewModel(original.ToDefinition());
                folderNameTouched = true;
            }

            Working.PropertyChanged += OnWorkingChanged;
        }

        /// <summary>Düzenlenen kopya.</summary>
        public ModuleEditViewModel Working { get; }

        /// <summary>Yeni modül mü.</summary>
        public bool IsNew
        {
            get { return original == null; }
        }

        /// <summary>Pencere başlığı.</summary>
        public string Title
        {
            get { return IsNew ? Resources.ModuleDialog_NewTitle : Resources.ModuleDialog_EditTitle; }
        }

        /// <summary>Onay düğmesinin metni.</summary>
        public string ConfirmText
        {
            get { return IsNew ? Resources.Dialog_Create : Resources.ModuleDialog_Apply; }
        }

        /// <summary>Stokta açılacak klasörün tam adı.</summary>
        public string FolderPreview
        {
            get { return string.Format(Resources.ModuleDialog_FolderPreview, Working.Folder); }
        }

        /// <summary>Doğrulama hatası; boşsa gizlidir.</summary>
        public string Error
        {
            get { return error; }
            private set { SetProperty(ref error, value); }
        }

        /// <summary>Doğrular ve uygular. Hata varsa <c>false</c> döner ve <see cref="Error"/> dolar.</summary>
        public bool Confirm()
        {
            var definition = Working.ToDefinition();
            var problem = editor.ValidateModule(definition, original);
            if (problem.Length > 0)
            {
                Error = problem;
                return false;
            }

            if (original == null)
            {
                editor.AddNewModule(definition);
            }
            else
            {
                editor.ApplyModuleEdit(original, definition);
            }

            return true;
        }

        // Yeni modülde, kullanıcı klasör adına dokunana kadar klasör adı addan üretilir.
        private void OnWorkingChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ModuleEditViewModel.FolderName) && !updatingFolderName)
            {
                folderNameTouched = true;
            }

            if (e.PropertyName == nameof(ModuleEditViewModel.Name) && !folderNameTouched)
            {
                updatingFolderName = true;
                Working.FolderName = ToFolderName(Working.Name);
                updatingFolderName = false;
            }

            OnPropertyChanged(nameof(FolderPreview));
        }

        /// <summary>"Ölçüm düzeneği" → "Olcum_duzenegi": Türkçe karaktersiz, boşluksuz klasör adı.</summary>
        public static string ToFolderName(string name)
        {
            var builder = new StringBuilder();
            foreach (var c in (name ?? string.Empty).Trim())
            {
                switch (c)
                {
                    case 'ç': builder.Append('c'); break;
                    case 'Ç': builder.Append('C'); break;
                    case 'ğ': builder.Append('g'); break;
                    case 'Ğ': builder.Append('G'); break;
                    case 'ı': builder.Append('i'); break;
                    case 'İ': builder.Append('I'); break;
                    case 'ö': builder.Append('o'); break;
                    case 'Ö': builder.Append('O'); break;
                    case 'ş': builder.Append('s'); break;
                    case 'Ş': builder.Append('S'); break;
                    case 'ü': builder.Append('u'); break;
                    case 'Ü': builder.Append('U'); break;
                    case ' ': builder.Append('_'); break;
                    default:
                        if (char.IsLetterOrDigit(c) || c == '_' || c == '-')
                        {
                            builder.Append(c);
                        }

                        break;
                }
            }

            return builder.ToString();
        }
    }
}
