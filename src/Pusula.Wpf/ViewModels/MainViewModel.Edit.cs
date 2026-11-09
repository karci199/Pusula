using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Pusula.Core.Inventory;
using Pusula.Core.Tracking;
using Pusula.Wpf.Properties;

namespace Pusula.Wpf.ViewModels
{
    /// <summary>Ana görünüm: proje, stok ve envanter kayıtları için ortak düzenleme ve takipten çıkarma.</summary>
    public sealed partial class MainViewModel
    {
        private bool isEditOpen;
        private bool isUntrackOpen;
        private string editTitle = string.Empty;
        private string editNumber = string.Empty;
        private string editName = string.Empty;
        private string editTemplate = string.Empty;
        private string editSubType = string.Empty;
        private string editNote = string.Empty;
        private string editError = string.Empty;
        private string untrackTitle = string.Empty;
        private string untrackText = string.Empty;
        private string editFolder = string.Empty;
        private RecordKind editKind;

        /// <summary>Takip dosyasına bakarak klasörün kayıt çeşidi; takip edilmiyorsa <c>null</c>.</summary>
        public static RecordKind? KindOf(string folder)
        {
            if (string.IsNullOrEmpty(folder))
            {
                return null;
            }

            foreach (var kind in new[] { RecordKind.Stock, RecordKind.Project, RecordKind.Inventory })
            {
                if (File.Exists(RecordEdit.TrackingFile(folder, kind)))
                {
                    return kind;
                }
            }

            return null;
        }

        /// <summary>Seçili düğüm düzenlenebilir mi (proje, stok ya da envanter kaydı).</summary>
        public bool CanEditSelected
        {
            get { return selectedNode != null && KindOf(selectedNode.Path).HasValue; }
        }

        /// <summary>Düzenleme diyaloğu açık mı.</summary>
        public bool IsEditOpen
        {
            get { return isEditOpen; }
            set { SetDialogOpen(ref isEditOpen, value); }
        }

        /// <summary>Takipten çıkarma onayı açık mı.</summary>
        public bool IsUntrackOpen
        {
            get { return isUntrackOpen; }
            set { SetDialogOpen(ref isUntrackOpen, value); }
        }

        /// <summary>Düzenleme diyaloğunun başlığı ("Düzenle · AB-7000-0002").</summary>
        public string EditTitle
        {
            get { return editTitle; }
            private set { SetProperty(ref editTitle, value); }
        }

        /// <summary>Numara (salt okunur; klasör adıdır).</summary>
        public string EditNumber
        {
            get { return editNumber; }
            private set { SetProperty(ref editNumber, value); }
        }

        /// <summary>Ad / tanım.</summary>
        public string EditName
        {
            get { return editName; }
            set { SetProperty(ref editName, value ?? string.Empty); }
        }

        /// <summary>Stok şablonu.</summary>
        public string EditTemplate
        {
            get { return editTemplate; }
            set { SetProperty(ref editTemplate, value ?? string.Empty); }
        }

        /// <summary>Envanter alt türü.</summary>
        public string EditSubType
        {
            get { return editSubType; }
            set { SetProperty(ref editSubType, value ?? string.Empty); }
        }

        /// <summary>Envanter kısa açıklaması.</summary>
        public string EditNote
        {
            get { return editNote; }
            set { SetProperty(ref editNote, value ?? string.Empty); }
        }

        /// <summary>Kaydetmede çıkan hata; yoksa boş.</summary>
        public string EditError
        {
            get { return editError; }
            private set { SetProperty(ref editError, value); }
        }

        /// <summary>Düzenlenen stok mu (şablon alanı görünür).</summary>
        public bool IsEditStock
        {
            get { return editKind == RecordKind.Stock; }
        }

        /// <summary>Düzenlenen envanter kaydı mı (alt tür ve açıklama görünür).</summary>
        public bool IsEditInventory
        {
            get { return editKind == RecordKind.Inventory; }
        }

        /// <summary>Stok şablonları (boş = şablonsuz).</summary>
        public ObservableCollection<string> EditTemplates { get; } = new ObservableCollection<string>();

        /// <summary>Envanter türünün alt türleri.</summary>
        public ObservableCollection<string> EditSubTypes { get; } = new ObservableCollection<string>();

        /// <summary>Takipten çıkarma onayının başlığı.</summary>
        public string UntrackTitle
        {
            get { return untrackTitle; }
            private set { SetProperty(ref untrackTitle, value); }
        }

        /// <summary>Takipten çıkarma onayının açıklaması.</summary>
        public string UntrackText
        {
            get { return untrackText; }
            private set { SetProperty(ref untrackText, value); }
        }

        /// <summary>Seçili düğüm için düzenleme diyaloğunu açar.</summary>
        public void OpenEditSelected()
        {
            if (selectedNode != null)
            {
                OpenEdit(selectedNode.Path);
            }
        }

        /// <summary>Klasörü verilen kaydın düzenleme diyaloğunu açar (ağaç, tablo satırı ya da sayfa).</summary>
        public bool OpenEdit(string folder)
        {
            var kind = KindOf(folder);
            if (!kind.HasValue)
            {
                return false;
            }

            editFolder = folder;
            editKind = kind.Value;
            EditError = string.Empty;
            EditTemplates.Clear();
            EditSubTypes.Clear();
            switch (editKind)
            {
                case RecordKind.Project:
                {
                    var info = JsonFile.TryRead<ProjectInfo>(TrackingFiles.ProjectFile(folder));
                    EditNumber = info?.ProjectNo ?? Path.GetFileName(folder);
                    EditName = info?.Name ?? string.Empty;
                    break;
                }

                case RecordKind.Stock:
                {
                    var info = JsonFile.TryRead<StockInfo>(TrackingFiles.StockFile(folder));
                    EditNumber = info?.StockNo ?? Path.GetFileName(folder);
                    EditName = info?.Name ?? string.Empty;
                    EditTemplates.Add(string.Empty);
                    foreach (var name in templates.Templates.Select(t => t.Name))
                    {
                        EditTemplates.Add(name);
                    }

                    var current = info?.Template ?? string.Empty;
                    if (!EditTemplates.Contains(current))
                    {
                        EditTemplates.Add(current);
                    }

                    EditTemplate = current;
                    break;
                }

                default:
                {
                    var record = JsonFile.TryRead<InventoryRecord>(InventoryService.RecordFile(folder));
                    EditNumber = record?.Number ?? Path.GetFileName(folder);
                    EditName = record?.Name ?? string.Empty;
                    EditNote = record?.Note ?? string.Empty;
                    var type = record != null ? types.Find(record.Type) : null;
                    foreach (var sub in type?.SubTypes ?? Enumerable.Empty<string>())
                    {
                        EditSubTypes.Add(sub);
                    }

                    var currentSub = record?.SubType ?? string.Empty;
                    if (currentSub.Length > 0 && !EditSubTypes.Contains(currentSub))
                    {
                        EditSubTypes.Add(currentSub);
                    }

                    EditSubType = currentSub;
                    break;
                }
            }

            OnPropertyChanged(nameof(IsEditStock));
            OnPropertyChanged(nameof(IsEditInventory));
            EditTitle = string.Format(Resources.Edit_Title, EditNumber);
            IsEditOpen = true;
            return true;
        }

        /// <summary>Düzenlemeyi kaydeder; başarılıysa ağaç yenilenir (seçim korunur). Kaydedildiyse <c>true</c>.</summary>
        public bool ConfirmEdit()
        {
            string error;
            switch (editKind)
            {
                case RecordKind.Project:
                    error = RecordEdit.EditProject(editFolder, EditName);
                    break;
                case RecordKind.Stock:
                    error = RecordEdit.EditStock(editFolder, EditName, EditTemplate);
                    break;
                default:
                    error = RecordEdit.EditInventory(editFolder, EditName, EditSubType, EditNote);
                    break;
            }

            if (error.Length > 0)
            {
                EditError = string.Format(Resources.Error_Io, error);
                Status = EditError;
                return false;
            }

            IsEditOpen = false;
            RefreshShowing(editFolder);
            Status = string.Format(Resources.Edit_Saved, EditNumber);
            return true;
        }

        /// <summary>Seçili düğüm için takipten çıkarma onayını açar.</summary>
        public void OpenUntrackSelected()
        {
            if (selectedNode != null)
            {
                OpenUntrack(selectedNode.Path);
            }
        }

        /// <summary>Takipten çıkarma onayını açar.</summary>
        public bool OpenUntrack(string folder)
        {
            var kind = KindOf(folder);
            if (!kind.HasValue)
            {
                return false;
            }

            editFolder = folder;
            editKind = kind.Value;
            var name = Path.GetFileName(folder);
            UntrackTitle = string.Format(Resources.Untrack_Title, name);
            UntrackText = string.Format(Resources.Untrack_Text, folder);
            IsUntrackOpen = true;
            return true;
        }

        /// <summary>Takipten çıkarır (takip dosyası yeniden adlandırılır, hiçbir dosya silinmez). Çıkarıldıysa <c>true</c>.</summary>
        public bool ConfirmUntrack()
        {
            var error = RecordEdit.Untrack(editFolder, editKind, DateTime.Now);
            IsUntrackOpen = false;
            if (error.Length > 0)
            {
                Status = string.Format(Resources.Error_Io, error);
                return false;
            }

            Refresh();
            Status = string.Format(Resources.Untrack_Done, Path.GetFileName(editFolder));
            return true;
        }

        // Kaydedince ağaç yenilenir; envanter kaydı gösteriliyorsa yine o gösterilir.
        private void RefreshShowing(string folder)
        {
            var shownRecord = Inventory.Item != null && string.Equals(Inventory.Item.Folder, folder, StringComparison.OrdinalIgnoreCase);
            Refresh();
            if (shownRecord)
            {
                ShowInventory(folder);
            }
        }
    }
}
