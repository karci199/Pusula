using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using Pusula.Core.Inventory;
using Pusula.Core.Tracking;
using Pusula.Core.Types;
using Pusula.Wpf.Mvvm;
using Pusula.Wpf.Properties;

namespace Pusula.Wpf.ViewModels
{
    /// <summary>
    /// Ağaçtaki bir düğüm: klasör (proje, stok, işaretsiz) ya da envanter (bölüm, tür, kayıt).
    /// </summary>
    public sealed class TreeNodeViewModel : ViewModelBase
    {
        private bool isExpanded;
        private bool isSelected;
        private bool isVisible = true;
        private string searchCount = string.Empty;

        /// <summary>Düğümü bir klasör kaydından oluşturur.</summary>
        public TreeNodeViewModel(FolderEntry entry)
        {
            Entry = entry;
        }

        /// <summary>Envanter düğümü oluşturur (tür ya da kayıt).</summary>
        public TreeNodeViewModel(FolderEntry entry, TypeDefinition? type, InventoryItem? item, string subtitle)
        {
            Entry = entry;
            Type = type;
            Item = item;
            Subtitle = subtitle;
        }

        /// <summary>Temsil edilen klasör.</summary>
        public FolderEntry Entry { get; }

        /// <summary>Envanter türü (tür ve kayıt düğümlerinde); diğerlerinde <c>null</c>.</summary>
        public TypeDefinition? Type { get; }

        /// <summary>Envanter kaydı (kayıt düğümünde); diğerlerinde <c>null</c>.</summary>
        public InventoryItem? Item { get; }

        /// <summary>Görünen ad.</summary>
        public string Title
        {
            get { return Entry.Title; }
        }

        /// <summary>Adın yanında soluk ek bilgi (kaydın tanımı, türdeki kayıt sayısı); boş olabilir.</summary>
        public string Subtitle { get; } = string.Empty;

        /// <summary>Ağaçta gösterilen ek bilgi: aramada tür düğümünde bulunan kayıt sayısı, yoksa <see cref="Subtitle"/>.</summary>
        public string DisplaySubtitle
        {
            get { return searchCount.Length > 0 ? searchCount : Subtitle; }
        }

        /// <summary>Üzerine gelince: ad ve ek bilgi ("JIG-1000-0009 · SATIMO adaptör plakası").</summary>
        public string ToolTipText
        {
            get { return Subtitle.Length == 0 ? Title : Title + " · " + Subtitle; }
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

        /// <summary>Envanter düzey klasöründe kaydı olmayan (elle açılmış) klasör mü.</summary>
        public bool IsUnmarkedInventory
        {
            get { return Entry.Kind == FolderKind.Plain && Type != null; }
        }

        /// <summary>Envanter düğümü mü (bölüm, tür ya da kayıt).</summary>
        public bool IsInventory
        {
            get { return Entry.Kind == FolderKind.InventoryGroup || Entry.Kind == FolderKind.InventoryType || Entry.Kind == FolderKind.InventoryItem; }
        }

        /// <summary>Türe göre simge.</summary>
        public string Glyph
        {
            get
            {
                if (Type != null && Type.Icon.Length > 0 && Entry.Kind != FolderKind.Plain)
                {
                    return GlyphOf(Type.Icon);
                }

                switch (Entry.Kind)
                {
                    case FolderKind.Project:
                        return AppIcons.Project;
                    case FolderKind.Stock:
                        return AppIcons.Stock;
                    case FolderKind.InventoryGroup:
                        return AppIcons.Inventory;
                    default:
                        return AppIcons.Folder;
                }
            }
        }

        /// <summary>Envanter türünün rengi ("#F7A35C"); diğerlerinde boş (stil rengi kullanılır).</summary>
        public string Color
        {
            get { return Type?.Color ?? string.Empty; }
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
                    case FolderKind.InventoryGroup:
                        return Resources.Tree_Inventory;
                    case FolderKind.InventoryType:
                    case FolderKind.InventoryItem:
                        return Type?.Name ?? string.Empty;
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

        /// <summary>Ağaçta seçili mi (kod tarafından seçim için).</summary>
        public bool IsSelected
        {
            get { return isSelected; }
            set { SetProperty(ref isSelected, value); }
        }

        /// <summary>Aramada görünür mü.</summary>
        public bool IsVisible
        {
            get { return isVisible; }
            private set { SetProperty(ref isVisible, value); }
        }

        /// <summary>
        /// Arama uygular: düğüm, adı / ek bilgisi aranan metni içeriyorsa ya da bir alt düğümü eşleşiyorsa görünür.
        /// Eşleşen alt düğümü olan düğüm açılır. Boş metin her şeyi gösterir. Kendisi ya da altı eşleşirse <c>true</c>.
        /// </summary>
        public bool ApplySearch(string text)
        {
            var anyChild = false;
            foreach (var child in Children)
            {
                anyChild |= child.ApplySearch(text);
            }

            if (text.Length == 0)
            {
                IsVisible = true;
                if (searchCount.Length > 0)
                {
                    searchCount = string.Empty;
                    OnPropertyChanged(nameof(DisplaySubtitle));
                }

                return true;
            }

            var self = SearchMatch.Contains(ToolTipText, text);
            if (self && !anyChild)
            {
                // Düğümün kendisi eşleşiyorsa altı da görünsün (proje bulunca stokları).
                foreach (var child in Children)
                {
                    child.ShowAll();
                }
            }

            if (anyChild)
            {
                IsExpanded = true;
            }

            // Tür düğümü aramada bulunan kayıt sayısını gösterir ("5" yerine "1").
            var count = Entry.Kind == FolderKind.InventoryType && text.Length > 0
                ? Children.Count(c => c.IsVisible).ToString(System.Globalization.CultureInfo.CurrentCulture)
                : string.Empty;
            if (count != searchCount)
            {
                searchCount = count;
                OnPropertyChanged(nameof(DisplaySubtitle));
            }

            IsVisible = self || anyChild;
            return IsVisible;
        }

        private void ShowAll()
        {
            IsVisible = true;
            foreach (var child in Children)
            {
                child.ShowAll();
            }
        }

        /// <summary>Alt düğümler.</summary>
        public ObservableCollection<TreeNodeViewModel> Children { get; } = new ObservableCollection<TreeNodeViewModel>();

        /// <summary>"E703" → ikon karakteri; okunamazsa klasör ikonu.</summary>
        public static string GlyphOf(string hex)
        {
            int code;
            return int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out code)
                ? ((char)code).ToString()
                : AppIcons.Folder;
        }
    }
}
