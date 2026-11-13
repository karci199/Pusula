using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Pusula.Wpf.Mvvm;
using Pusula.Wpf.Properties;

namespace Pusula.Wpf.ViewModels.TemplateEditor
{
    /// <summary>
    /// Düzenleyicide bir alt klasör düğümü; adı düzenlenebilir, altına klasör eklenebilir, silinebilir.
    /// </summary>
    public sealed class FolderNodeViewModel : ViewModelBase
    {
        private readonly ObservableCollection<FolderNodeViewModel> owner;
        private string name;

        /// <summary>Düğümü oluşturur; <paramref name="owner"/> düğümün içinde durduğu listedir.</summary>
        public FolderNodeViewModel(string name, ObservableCollection<FolderNodeViewModel> owner)
        {
            this.name = name;
            this.owner = owner;
            AddChildCommand = new RelayCommand(AddChild);
            DeleteCommand = new RelayCommand(Delete);
        }

        /// <summary>Klasör adı (tek düzey).</summary>
        public string Name
        {
            get { return name; }
            set { SetProperty(ref name, value ?? string.Empty); }
        }

        /// <summary>Alt düğümler.</summary>
        public ObservableCollection<FolderNodeViewModel> Children { get; } = new ObservableCollection<FolderNodeViewModel>();

        /// <summary>Altına yeni klasör ekler.</summary>
        public ICommand AddChildCommand { get; }

        /// <summary>Düğümü (altlarıyla birlikte) şablondan çıkarır.</summary>
        public ICommand DeleteCommand { get; }

        private void AddChild()
        {
            Children.Add(new FolderNodeViewModel(Resources.Editor_NewFolder, Children));
        }

        private void Delete()
        {
            owner.Remove(this);
        }

        /// <summary>
        /// Düz yol listesinden (<c>SATIMO</c>, <c>SATIMO\Ham</c> …) ağaç kurar.
        /// </summary>
        public static void BuildTree(IEnumerable<string> paths, ObservableCollection<FolderNodeViewModel> roots)
        {
            foreach (var path in paths)
            {
                var level = roots;
                foreach (var part in path.Split('\\', '/'))
                {
                    if (part.Length == 0)
                    {
                        continue;
                    }

                    FolderNodeViewModel? found = null;
                    foreach (var node in level)
                    {
                        if (string.Equals(node.Name, part, System.StringComparison.OrdinalIgnoreCase))
                        {
                            found = node;
                            break;
                        }
                    }

                    if (found == null)
                    {
                        found = new FolderNodeViewModel(part, level);
                        level.Add(found);
                    }

                    level = found.Children;
                }
            }
        }

        /// <summary>Ağacı düz yol listesine çevirir; her düğüm kendi yoluyla listeye girer.</summary>
        public static List<string> Flatten(IEnumerable<FolderNodeViewModel> roots)
        {
            var result = new List<string>();
            Flatten(roots, string.Empty, result);
            return result;
        }

        private static void Flatten(IEnumerable<FolderNodeViewModel> nodes, string prefix, List<string> result)
        {
            foreach (var node in nodes)
            {
                var path = prefix.Length == 0 ? node.Name.Trim() : prefix + "\\" + node.Name.Trim();
                result.Add(path);
                Flatten(node.Children, path, result);
            }
        }

        /// <summary>Ağaçtaki tüm düğümleri döner.</summary>
        public static IEnumerable<FolderNodeViewModel> All(IEnumerable<FolderNodeViewModel> roots)
        {
            foreach (var node in roots)
            {
                yield return node;
                foreach (var child in All(node.Children))
                {
                    yield return child;
                }
            }
        }
    }
}
