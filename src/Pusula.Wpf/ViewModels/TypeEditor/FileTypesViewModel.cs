using System;
using System.Collections.ObjectModel;
using System.Linq;
using Pusula.Core.Types;
using Pusula.Wpf.Mvvm;
using Pusula.Wpf.Properties;

namespace Pusula.Wpf.ViewModels.TypeEditor
{
    /// <summary>Dosya türü satırı; uzantılar virgülle düzenlenir ("vsd, vsdx").</summary>
    public sealed class FileTypeRow
    {
        /// <summary>Görünen ad ("Visio").</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Uzantılar, virgülle.</summary>
        public string Extensions { get; set; } = string.Empty;

        /// <summary>Grup ("Şema").</summary>
        public string Group { get; set; } = string.Empty;
    }

    /// <summary>Dosya türleri penceresi: uzantıya göre tanınan türlerin listesi (<c>turler.json</c> → <c>fileTypes</c>).</summary>
    public sealed class FileTypesViewModel : ViewModelBase
    {
        private readonly string root;
        private string error = string.Empty;

        /// <summary>Kökün tür kalıbından düzenleyici kurar.</summary>
        public FileTypesViewModel(string root, TypeSet types)
        {
            this.root = root;
            foreach (var type in types.FileTypes)
            {
                Rows.Add(new FileTypeRow { Name = type.Name, Extensions = string.Join(", ", type.Extensions), Group = type.Group });
            }
        }

        /// <summary>Satırlar.</summary>
        public ObservableCollection<FileTypeRow> Rows { get; } = new ObservableCollection<FileTypeRow>();

        /// <summary>Kaydetmede bulunan hata; yoksa boş.</summary>
        public string Error
        {
            get { return error; }
            private set { SetProperty(ref error, value); }
        }

        /// <summary>Doğrular ve yazar (tür kalıbının diğer kısımlarına dokunmaz). Başarılıysa <c>true</c>.</summary>
        public bool Save()
        {
            var list = new System.Collections.Generic.List<FileTypeDefinition>();
            var seen = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in Rows.Where(r => r.Name.Trim().Length > 0 || r.Extensions.Trim().Length > 0))
            {
                var name = row.Name.Trim();
                var extensions = row.Extensions.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(e => e.Trim().TrimStart('*').TrimStart('.').ToLowerInvariant())
                    .Where(e => e.Length > 0)
                    .Distinct()
                    .ToList();
                if (name.Length == 0 || extensions.Count == 0)
                {
                    Error = string.Format(Resources.FileTypes_ErrorRow, name.Length == 0 ? row.Extensions : name);
                    return false;
                }

                foreach (var extension in extensions)
                {
                    string other;
                    if (seen.TryGetValue(extension, out other))
                    {
                        Error = string.Format(Resources.FileTypes_ErrorDuplicate, extension, other, name);
                        return false;
                    }

                    seen[extension] = name;
                }

                var type = new FileTypeDefinition { Name = name, Group = row.Group.Trim() };
                type.Extensions.AddRange(extensions);
                list.Add(type);
            }

            var set = TypeStore.LoadOrCreate(root);
            set.FileTypes = list;
            var failure = TypeStore.Save(root, set);
            if (failure.Length > 0)
            {
                Error = string.Format(Resources.Types_ErrorSave, failure);
                return false;
            }

            Error = string.Empty;
            return true;
        }
    }
}
