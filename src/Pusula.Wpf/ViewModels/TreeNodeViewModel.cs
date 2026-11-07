using System.Collections.ObjectModel;
using DesignKit.Icons;
using Pusula.Core.Tracking;
using Pusula.Wpf.Mvvm;
using Pusula.Wpf.Properties;

namespace Pusula.Wpf.ViewModels
{
    /// <summary>
    /// Ağaçtaki bir klasör düğümü.
    /// </summary>
    public sealed class TreeNodeViewModel : ViewModelBase
    {
        private bool isExpanded;

        /// <summary>Düğümü bir klasör kaydından oluşturur.</summary>
        public TreeNodeViewModel(FolderEntry entry)
        {
            Entry = entry;
        }

        /// <summary>Temsil edilen klasör.</summary>
        public FolderEntry Entry { get; }

        /// <summary>Görünen ad.</summary>
        public string Title
        {
            get { return Entry.Title; }
        }

        /// <summary>Klasörün tam yolu.</summary>
        public string Path
        {
            get { return Entry.Path; }
        }

        /// <summary>Klasör türü.</summary>
        public FolderKind Kind
        {
            get { return Entry.Kind; }
        }

        /// <summary>Türe göre simge.</summary>
        public string Glyph
        {
            get
            {
                switch (Entry.Kind)
                {
                    case FolderKind.Project:
                        return Symbol.ViewAll;
                    case FolderKind.Stock:
                        return Symbol.Device;
                    default:
                        return Symbol.Folder;
                }
            }
        }

        /// <summary>Türün görünen adı.</summary>
        public string KindText
        {
            get
            {
                switch (Entry.Kind)
                {
                    case FolderKind.Project:
                        return Resources.Kind_Project;
                    case FolderKind.Stock:
                        return Resources.Kind_Stock;
                    default:
                        return Resources.Kind_Plain;
                }
            }
        }

        /// <summary>Ağaçta açık mı.</summary>
        public bool IsExpanded
        {
            get { return isExpanded; }
            set { SetProperty(ref isExpanded, value); }
        }

        /// <summary>Alt düğümler.</summary>
        public ObservableCollection<TreeNodeViewModel> Children { get; } = new ObservableCollection<TreeNodeViewModel>();
    }
}
