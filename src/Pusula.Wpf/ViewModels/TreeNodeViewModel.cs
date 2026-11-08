using System;
using System.Collections.ObjectModel;
using System.Globalization;
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
                if (Type != null && Type.Icon.Length > 0)
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
