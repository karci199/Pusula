using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Pusula.Core.Inventory;
using Pusula.Core.Types;
using Pusula.Wpf.Mvvm;
using Pusula.Wpf.Properties;

namespace Pusula.Wpf.ViewModels
{
    /// <summary>Bilgiler kartında bir satır: etiket ve değer.</summary>
    public sealed class FactRow
    {
        /// <summary>Satırı oluşturur.</summary>
        public FactRow(string label, string value)
        {
            Label = label;
            Value = value.Length == 0 ? "—" : value;
        }

        /// <summary>Etiket.</summary>
        public string Label { get; }

        /// <summary>Değer ("—" boşsa).</summary>
        public string Value { get; }
    }

    /// <summary>Geçmiş listesinde bir olay.</summary>
    public sealed class HistoryRow
    {
        /// <summary>Olaydan satır oluşturur.</summary>
        public HistoryRow(InventoryEvent inventoryEvent, TypeDefinition type)
        {
            Status = StatusChip.For(type, inventoryEvent.Status);
            var date = inventoryEvent.DateValue.HasValue ? inventoryEvent.DateValue.Value.ToString(Resources.Format_Date) : inventoryEvent.Date;
            Meta = inventoryEvent.Person.Length == 0 ? date : date + " · " + inventoryEvent.Person;
            var details = new List<string>();
            if (inventoryEvent.Location.Length > 0)
            {
                details.Add(Resources.Inv_Location + ": " + inventoryEvent.Location);
            }

            if (inventoryEvent.Note.Length > 0)
            {
                details.Add(inventoryEvent.Note);
            }

            Detail = string.Join(" · ", details);
        }

        /// <summary>Durum etiketi (nokta rengi ve ad).</summary>
        public StatusChip Status { get; }

        /// <summary>"18.09.2026 · katan".</summary>
        public string Meta { get; }

        /// <summary>Konum ve not.</summary>
        public string Detail { get; }

        /// <summary>Ayrıntı var mı.</summary>
        public bool HasDetail
        {
            get { return Detail.Length > 0; }
        }
    }

    /// <summary>Kayıt sayfasında bir klasör kutucuğu ("Cizimler · 3 · Rev AC").</summary>
    public sealed class RecordFolderTile
    {
        /// <summary>Özetten kutucuk oluşturur.</summary>
        public RecordFolderTile(RecordFolderSummary summary)
        {
            Path = summary.Path;
            Label = summary.Label;
            IsEmpty = summary.FileCount == 0;
            Value = IsEmpty
                ? Resources.Inv_FolderEmpty
                : summary.LatestRevision.Length == 0
                    ? summary.FileCount.ToString(System.Globalization.CultureInfo.CurrentCulture)
                    : string.Format(Resources.Inv_FilesWithRevision, summary.FileCount, summary.LatestRevision);
        }

        /// <summary>Klasörün yolu (tıklayınca açılır).</summary>
        public string Path { get; }

        /// <summary>Klasör adı.</summary>
        public string Label { get; }

        /// <summary>"3 · Rev AC", "5" ya da "boş".</summary>
        public string Value { get; }

        /// <summary>Klasör boş mu (soluk gösterilir).</summary>
        public bool IsEmpty { get; }
    }

    /// <summary>Envanter listesinde (tür ya da bölüm seçiliyken) bir satır.</summary>
    public sealed class InventoryRow
    {
        /// <summary>Kayıttan satır oluşturur.</summary>
        public InventoryRow(InventoryItem item, TypeDefinition type)
        {
            Item = item;
            Type = type;
            Number = item.Record.Number;
            Name = item.Record.Name;
            TypeName = type.Name;
            SubType = item.Record.SubType.Length == 0 ? "—" : item.Record.SubType;
            Status = StatusChip.For(type, item.Record.CurrentStatus);
            Location = item.Record.Location.Length == 0 ? "—" : item.Record.Location;
            Level = InventoryDetailViewModel.LevelText(item);
        }

        /// <summary>Kayıt.</summary>
        public InventoryItem Item { get; }

        /// <summary>Tür.</summary>
        public TypeDefinition Type { get; }

        /// <summary>Stok (numara).</summary>
        public string Number { get; }

        /// <summary>Tanım.</summary>
        public string Name { get; }

        /// <summary>Tür adı.</summary>
        public string TypeName { get; }

        /// <summary>Alt tür.</summary>
        public string SubType { get; }

        /// <summary>Durum.</summary>
        public StatusChip Status { get; }

        /// <summary>Konum.</summary>
        public string Location { get; }

        /// <summary>Düzey ("Genel", "Proje · 7000_MILKON", "Stok · AB-7000-0003").</summary>
        public string Level { get; }
    }

    /// <summary>
    /// Envanter seçiliyken sağ panel: kayıt sayfası (bilgiler, durum, geçmiş) ya da kayıt listesi (tür / bölüm).
    /// </summary>
    public sealed class InventoryDetailViewModel : ViewModelBase
    {
        private bool isRecord;
        private bool isList;
        private InventoryItem? item;
        private TypeDefinition? type;
        private const int PhotoStripCount = 4;
        private StatusChip status = new StatusChip("—", StatusTone.Neutral);
        private string statusInfo = string.Empty;
        private string cover = string.Empty;
        private string noteFile = string.Empty;
        private string noteExcerpt = string.Empty;
        private string morePhotos = string.Empty;
        private int contentsVersion;

        /// <summary>Kayıt sayfası mı gösteriliyor.</summary>
        public bool IsRecord
        {
            get { return isRecord; }
            private set { SetProperty(ref isRecord, value); }
        }

        /// <summary>Liste mi gösteriliyor.</summary>
        public bool IsList
        {
            get { return isList; }
            private set { SetProperty(ref isList, value); }
        }

        /// <summary>Gösterilen kayıt; yoksa <c>null</c>.</summary>
        public InventoryItem? Item
        {
            get { return item; }
        }

        /// <summary>Gösterilen kaydın türü; yoksa <c>null</c>.</summary>
        public TypeDefinition? Type
        {
            get { return type; }
        }

        /// <summary>Durum etiketi.</summary>
        public StatusChip Status
        {
            get { return status; }
            private set { SetProperty(ref status, value); }
        }

        /// <summary>Durumun yanındaki bilgi ("18.09.2026'dan beri · Lab-A raf 3").</summary>
        public string StatusInfo
        {
            get { return statusInfo; }
            private set { SetProperty(ref statusInfo, value); }
        }

        /// <summary>Aşama çubuğu.</summary>
        public ObservableCollection<StatusStep> Steps { get; } = new ObservableCollection<StatusStep>();

        /// <summary>Bilgiler kartı.</summary>
        public ObservableCollection<FactRow> Facts { get; } = new ObservableCollection<FactRow>();

        /// <summary>Geçmiş (yeniden eskiye).</summary>
        public ObservableCollection<HistoryRow> History { get; } = new ObservableCollection<HistoryRow>();

        /// <summary>Kayıt klasörünün alt klasörleri.</summary>
        public ObservableCollection<RecordFolderTile> FolderTiles { get; } = new ObservableCollection<RecordFolderTile>();

        /// <summary>Fotoğraf şeridi (en çok dört).</summary>
        public ObservableCollection<string> Photos { get; } = new ObservableCollection<string>();

        /// <summary>Şeride sığmayan fotoğraflar ("+2"); yoksa boş.</summary>
        public string MorePhotos
        {
            get { return morePhotos; }
            private set { SetProperty(ref morePhotos, value); }
        }

        /// <summary>Kapak fotoğrafı; yoksa boş.</summary>
        public string Cover
        {
            get { return cover; }
            private set
            {
                if (SetProperty(ref cover, value))
                {
                    OnPropertyChanged(nameof(HasCover));
                }
            }
        }

        /// <summary>Kapak fotoğrafı var mı.</summary>
        public bool HasCover
        {
            get { return cover.Length > 0; }
        }

        /// <summary>Obsidian notunun yolu; yoksa boş.</summary>
        public string NoteFile
        {
            get { return noteFile; }
            private set
            {
                if (SetProperty(ref noteFile, value))
                {
                    OnPropertyChanged(nameof(HasNote));
                }
            }
        }

        /// <summary>Not var mı.</summary>
        public bool HasNote
        {
            get { return noteFile.Length > 0; }
        }

        /// <summary>Not gövdesinin başı; boşsa "Not boş" yazılır.</summary>
        public string NoteExcerpt
        {
            get { return noteExcerpt; }
            private set { SetProperty(ref noteExcerpt, value); }
        }

        /// <summary>Liste satırları.</summary>
        public ObservableCollection<InventoryRow> Rows { get; } = new ObservableCollection<InventoryRow>();

        /// <summary>Seçili düğüme göre kayıt sayfasını ya da listeyi kurar; envanter dışı düğümde temizler.</summary>
        public void Load(TreeNodeViewModel? node, IEnumerable<InventoryItem> allItems, TypeSet types)
        {
            IsRecord = false;
            IsList = false;
            Steps.Clear();
            Facts.Clear();
            History.Clear();
            Rows.Clear();
            ClearContents();
            item = null;
            type = null;
            if (node == null || !node.IsInventory)
            {
                return;
            }

            if (node.Item != null && node.Type != null)
            {
                ShowRecord(node.Item, node.Type);
                return;
            }

            var filter = node.Type;
            foreach (var entry in allItems)
            {
                var entryType = types.Find(entry.Record.Type);
                if (entryType != null && (filter == null || entryType.Key == filter.Key))
                {
                    Rows.Add(new InventoryRow(entry, entryType));
                }
            }

            IsList = true;
        }

        private void ShowRecord(InventoryItem shown, TypeDefinition shownType)
        {
            item = shown;
            type = shownType;
            var record = shown.Record;
            Status = StatusChip.For(shownType, record.CurrentStatus);
            foreach (var step in StatusStep.Build(shownType, record.CurrentStatus))
            {
                Steps.Add(step);
            }

            var last = record.LastEvent;
            var since = last?.DateValue;
            var infoParts = new List<string>();
            if (since.HasValue)
            {
                infoParts.Add(since.Value.Date == DateTime.Today ? Resources.Status_SinceToday : string.Format(Resources.Status_Since, Formats.ShortDate(since.Value)));
            }

            if (record.Location.Length > 0)
            {
                infoParts.Add(record.Location);
            }

            StatusInfo = string.Join(" · ", infoParts);

            var request = record.FirstEventOf(shownType.InitialStatus);
            var making = shownType.Statuses.Count > 1 ? record.FirstEventOf(shownType.Statuses[1].Name) : null;
            var delivered = shownType.Statuses.Count > 2 ? record.FirstEventOf(shownType.Statuses[2].Name) : null;
            Facts.Add(new FactRow(Resources.Inv_Number, record.Number));
            Facts.Add(new FactRow(Resources.Inv_Name, record.Name));
            Facts.Add(new FactRow(Resources.Inv_SubType, record.SubType));
            Facts.Add(new FactRow(Resources.Inv_Level, LevelText(shown)));
            Facts.Add(new FactRow(Resources.Inv_Requested, EventText(request)));
            Facts.Add(new FactRow(Resources.Inv_MadeBy, making?.Person ?? string.Empty));
            Facts.Add(new FactRow(Resources.Inv_Delivered, delivered?.DateValue?.ToString(Resources.Format_Date) ?? string.Empty));
            Facts.Add(new FactRow(Resources.Inv_Location, record.Location));
            Facts.Add(new FactRow(Resources.Inv_Note, record.Note));

            foreach (var inventoryEvent in Enumerable.Reverse(record.Events))
            {
                History.Add(new HistoryRow(inventoryEvent, shownType));
            }

            IsRecord = true;
            var unused = LoadContentsAsync(shown);
        }

        /// <summary>Kayıt klasörünü yeniden okur (not oluşturulunca vb.).</summary>
        public Task ReloadContentsAsync()
        {
            return item == null ? Task.FromResult(0) : LoadContentsAsync(item);
        }

        private void ClearContents()
        {
            contentsVersion++;
            FolderTiles.Clear();
            Photos.Clear();
            MorePhotos = string.Empty;
            Cover = string.Empty;
            NoteFile = string.Empty;
            NoteExcerpt = string.Empty;
        }

        // Klasörü arka planda okur; bu arada başka kayıt seçildiyse sonucu atar.
        private async Task LoadContentsAsync(InventoryItem shown)
        {
            ClearContents();
            var version = contentsVersion;
            var contents = await Task.Run(() => RecordContents.Read(shown.Folder, shown.Record.Number)).ConfigureAwait(true);
            if (version != contentsVersion)
            {
                return;
            }

            foreach (var folder in contents.Folders)
            {
                FolderTiles.Add(new RecordFolderTile(folder));
            }

            foreach (var photo in contents.Photos.Take(PhotoStripCount))
            {
                Photos.Add(photo);
            }

            MorePhotos = contents.Photos.Count > PhotoStripCount ? "+" + (contents.Photos.Count - PhotoStripCount).ToString(System.Globalization.CultureInfo.CurrentCulture) : string.Empty;
            Cover = contents.Cover;
            NoteFile = contents.NoteFile;
            NoteExcerpt = contents.NoteFile.Length == 0 ? string.Empty : contents.NoteExcerpt.Length == 0 ? Resources.Inv_NoteEmpty : contents.NoteExcerpt;
        }

        private static string EventText(InventoryEvent? inventoryEvent)
        {
            if (inventoryEvent == null)
            {
                return string.Empty;
            }

            var date = inventoryEvent.DateValue?.ToString(Resources.Format_Date) ?? inventoryEvent.Date;
            return inventoryEvent.Person.Length == 0 ? date : date + " · " + inventoryEvent.Person;
        }

        /// <summary>Düzey metni.</summary>
        public static string LevelText(InventoryItem shown)
        {
            switch (shown.Level)
            {
                case InventoryLevel.Project:
                    return string.Format(Resources.Inv_LevelProject, shown.Owner);
                case InventoryLevel.Stock:
                    return string.Format(Resources.Inv_LevelStock, shown.Owner);
                default:
                    return Resources.Inv_LevelGeneral;
            }
        }
    }
}
