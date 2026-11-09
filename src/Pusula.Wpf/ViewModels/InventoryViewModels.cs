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
            SortDate = inventoryEvent.DateValue ?? DateTime.MinValue;
        }

        /// <summary>Uygulamadan yazılan kısa nottan satır oluşturur ("Not" etiketiyle).</summary>
        public HistoryRow(Pusula.Core.Tracking.QuickNote note)
        {
            Status = new StatusChip(Resources.QuickNote_Chip, StatusTone.Neutral);
            var date = note.DateValue.HasValue ? note.DateValue.Value.ToString(Resources.Format_DateTime) : note.Date;
            Meta = note.Person.Length == 0 ? date : date + " · " + note.Person;
            Detail = note.Text;
            SortDate = note.DateValue ?? DateTime.MinValue;
        }

        /// <summary>Obsidian olay notundan satır oluşturur (revizyon, arıza …).</summary>
        public HistoryRow(Pusula.Core.Notes.EventNote note)
        {
            Journal = note.IsJournal ? note : null;
            Status = new StatusChip(EventKinds.Label(note.Kind), EventKinds.ToneFor(note.Kind, note.Importance));
            var date = note.Date.ToString(Resources.Format_Date);
            Meta = note.Person.Length == 0 ? date : date + " · " + note.Person;
            Detail = note.Excerpt.Length == 0 ? note.Title : note.Title + " · " + note.Excerpt;
            SortDate = note.Date;
            NotePath = note.Path;
        }

        /// <summary>Sıralama tarihi.</summary>
        public DateTime SortDate { get; }

        /// <summary>Notun Günlük bölümündeki olay (düzenlenebilir / silinebilir); değilse <c>null</c>.</summary>
        public Pusula.Core.Notes.EventNote? Journal { get; }

        /// <summary>Günlük olayı mı.</summary>
        public bool IsJournal
        {
            get { return Journal != null; }
        }

        /// <summary>Olay notunun yolu (Obsidian notundan gelen satırda); diğerlerinde boş.</summary>
        public string NotePath { get; } = string.Empty;

        /// <summary>Açılacak not var mı.</summary>
        public bool HasNote
        {
            get { return NotePath.Length > 0; }
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
        /// <summary>Özetten kutucuk oluşturur; <paramref name="kinds"/> dosya türü özetidir ("2 STEP · 1 PDF").</summary>
        public RecordFolderTile(RecordFolderSummary summary, string kinds)
        {
            Path = summary.Path;
            Kinds = kinds;
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

        /// <summary>Dosya türü özeti ("2 STEP · 1 PDF"); boş olabilir.</summary>
        public string Kinds { get; }

        /// <summary>Tür özeti var mı.</summary>
        public bool HasKinds
        {
            get { return Kinds.Length > 0; }
        }
    }

    /// <summary>Envanter listesinde (tür ya da bölüm seçiliyken) bir satır.</summary>
    public sealed class InventoryRow
    {
        /// <summary>Kayıttan satır oluşturur.</summary>
        public InventoryRow(InventoryItem item, TypeDefinition type, bool isLinked = false)
        {
            Item = item;
            IsLinked = isLinked;
            Folder = item.Folder;
            Type = type;
            Number = item.Record.Number;
            Name = item.Record.Name;
            TypeName = type.Name;
            SubType = item.Record.SubType.Length == 0 ? "—" : item.Record.SubType;
            Status = StatusChip.For(type, item.Record.CurrentStatus);
            Location = item.Record.Location.Length == 0 ? "—" : item.Record.Location;
            Level = InventoryDetailViewModel.LevelText(item);
            LevelName = InventoryDetailViewModel.LevelName(item.Level);
        }

        /// <summary>Kayıt; bulunamayan bağda <c>null</c>.</summary>
        public InventoryItem? Item { get; }

        /// <summary>Kayıt klasörü; bulunamayan bağda boş.</summary>
        public string Folder { get; } = string.Empty;

        /// <summary>Stok sayfasında: stoka bağla eklenmiş (genel / proje) kayıt mı; bağ kaldırılabilir.</summary>
        public bool IsLinked { get; }

        /// <summary>Stok sayfasında: bağlı numara bulunamadı.</summary>
        public bool IsMissing { get; }

        /// <summary>Bulunamayan bağ için satır ("JIG-1000-0099 · bulunamadı").</summary>
        public InventoryRow(string number, TypeDefinition? type)
        {
            Type = type;
            IsLinked = true;
            IsMissing = true;
            Number = number;
            Name = Resources.Inv_LinkMissing;
            TypeName = type?.Name ?? string.Empty;
            SubType = "—";
            Status = new StatusChip("—", StatusTone.None);
            Location = "—";
            Level = "—";
        }

        /// <summary>Tür.</summary>
        public TypeDefinition? Type { get; }

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

        /// <summary>Düzeyin yalın adı ("Genel", "Proje", "Stok"); süzgeç için.</summary>
        public string LevelName { get; } = string.Empty;
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
        private readonly List<InventoryRow> allRows = new List<InventoryRow>();
        private bool showTypeFilter;
        private string filterType = string.Empty;
        private string filterSubType = string.Empty;
        private string filterStatus = string.Empty;
        private string filterLevel = string.Empty;
        private string filterText = string.Empty;
        private string filterSummary = string.Empty;
        private string cover = string.Empty;
        private string noteFile = string.Empty;
        private string noteExcerpt = string.Empty;
        private string morePhotos = string.Empty;
        private int contentsVersion;
        private TypeSet fileTypes = new TypeSet();

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

            fileTypes = types;
            if (node.Item != null && node.Type != null)
            {
                ShowRecord(node.Item, node.Type);
                return;
            }

            var filter = node.Type;
            allRows.Clear();
            foreach (var entry in allItems)
            {
                var entryType = types.Find(entry.Record.Type);
                if (entryType != null && (filter == null || entryType.Key == filter.Key))
                {
                    allRows.Add(new InventoryRow(entry, entryType));
                }
            }

            // Filtre seçenekleri listedeki kayıtlardan; tür seçeneği yalnızca bölüm seçiliyken anlamlı.
            ShowTypeFilter = filter == null;

            // Önce seçimler "Tümü"ye döner, sonra seçenekler yenilenir: seçili seçenek silinip kutu boş kalmasın.
            filterType = Resources.Filter_All;
            filterSubType = Resources.Filter_All;
            filterStatus = Resources.Filter_All;
            filterLevel = Resources.Filter_All;
            filterText = string.Empty;
            NotifyFilters();
            FillOptions(TypeOptions, allRows.Select(r => r.TypeName));
            FillOptions(StatusOptions, allRows.Select(r => r.Status.Name));
            FillOptions(LevelOptions, new[] { InventoryLevel.General, InventoryLevel.Project, InventoryLevel.Stock }.Select(LevelName));
            RefreshSubTypes();
            NotifyFilters();
            ApplyFilter();
            IsList = true;
        }

        /// <summary>Bölüm seçiliyken tür süzgeci gösterilsin mi (tür düğümünde tür zaten belli).</summary>
        public bool ShowTypeFilter
        {
            get { return showTypeFilter; }
            private set
            {
                if (SetProperty(ref showTypeFilter, value))
                {
                    OnPropertyChanged(nameof(TypeColumnWidth));
                }
            }
        }

        /// <summary>Tür sütununun genişliği: tür düğümünde (hepsi aynı tür) gizli.</summary>
        public double TypeColumnWidth
        {
            get { return showTypeFilter ? 60 : 0; }
        }

        /// <summary>Tür seçenekleri ("Tümü", "Jig", "Kablo").</summary>
        public ObservableCollection<string> TypeOptions { get; } = new ObservableCollection<string>();

        /// <summary>Alt tür seçenekleri (seçili türe göre).</summary>
        public ObservableCollection<string> SubTypeOptions { get; } = new ObservableCollection<string>();

        /// <summary>Durum seçenekleri.</summary>
        public ObservableCollection<string> StatusOptions { get; } = new ObservableCollection<string>();

        /// <summary>Seçili tür süzgeci.</summary>
        public string FilterType
        {
            get { return filterType; }
            set
            {
                // Seçenekler yenilenirken açılır kutu boş (null) yazar; yok sayıp seçimi geri göster.
                if (value == null)
                {
                    RenotifyLater(nameof(FilterType));
                    return;
                }

                if (SetProperty(ref filterType, value))
                {
                    filterSubType = Resources.Filter_All;
                    RefreshSubTypes();
                    ApplyFilter();
                }
            }
        }

        /// <summary>Seçili alt tür süzgeci.</summary>
        public string FilterSubType
        {
            get { return filterSubType; }
            set
            {
                // Seçenekler yenilenirken açılır kutu boş (null) yazar; yok sayıp seçimi geri göster.
                if (value == null)
                {
                    RenotifyLater(nameof(FilterSubType));
                    return;
                }

                if (SetProperty(ref filterSubType, value))
                {
                    ApplyFilter();
                }
            }
        }

        /// <summary>Düzey seçenekleri ("Tümü", "Genel", "Proje", "Stok").</summary>
        public ObservableCollection<string> LevelOptions { get; } = new ObservableCollection<string>();

        /// <summary>Seçili düzey süzgeci (stoka özgü kayıtlar için "Stok").</summary>
        public string FilterLevel
        {
            get { return filterLevel; }
            set
            {
                // Seçenekler yenilenirken açılır kutu boş (null) yazar; yok sayıp seçimi geri göster.
                if (value == null)
                {
                    RenotifyLater(nameof(FilterLevel));
                    return;
                }

                if (SetProperty(ref filterLevel, value))
                {
                    ApplyFilter();
                }
            }
        }

        /// <summary>Seçili durum süzgeci.</summary>
        public string FilterStatus
        {
            get { return filterStatus; }
            set
            {
                // Seçenekler yenilenirken açılır kutu boş (null) yazar; yok sayıp seçimi geri göster.
                if (value == null)
                {
                    RenotifyLater(nameof(FilterStatus));
                    return;
                }

                if (SetProperty(ref filterStatus, value))
                {
                    ApplyFilter();
                }
            }
        }

        /// <summary>Aranan metin: numara, tanım, alt tür, konum, not (örn. "SATIMO").</summary>
        public string FilterText
        {
            get { return filterText; }
            set
            {
                if (SetProperty(ref filterText, value ?? string.Empty))
                {
                    ApplyFilter();
                }
            }
        }

        /// <summary>"8 kayıttan 3'ü" ya da "8 kayıt".</summary>
        public string FilterSummary
        {
            get { return filterSummary; }
            private set { SetProperty(ref filterSummary, value); }
        }

        // Açılır kutu seçimi boşalttıktan sonra değeri yeniden bildirir (kutu "Tümü"yü yeniden seçer).
        private void RenotifyLater(string propertyName)
        {
            System.Windows.Application.Current?.Dispatcher.BeginInvoke(new Action(() => OnPropertyChanged(propertyName)), System.Windows.Threading.DispatcherPriority.Background);
        }

        private static void FillOptions(ObservableCollection<string> target, IEnumerable<string> values)
        {
            // "Tümü" hep ilk sırada kalır, silinmez: açılır kutunun seçimi boşa düşmesin.
            if (target.Count == 0 || target[0] != Resources.Filter_All)
            {
                target.Clear();
                target.Add(Resources.Filter_All);
            }

            while (target.Count > 1)
            {
                target.RemoveAt(target.Count - 1);
            }
            foreach (var value in values.Where(v => v.Length > 0 && v != "—").Distinct(StringComparer.CurrentCultureIgnoreCase).OrderBy(v => v, StringComparer.CurrentCultureIgnoreCase))
            {
                target.Add(value);
            }
        }

        private void RefreshSubTypes()
        {
            OnPropertyChanged(nameof(FilterSubType));
            FillOptions(SubTypeOptions, allRows.Where(MatchesType).Select(r => r.SubType));
            OnPropertyChanged(nameof(FilterSubType));
        }

        private void NotifyFilters()
        {
            OnPropertyChanged(nameof(FilterType));
            OnPropertyChanged(nameof(FilterSubType));
            OnPropertyChanged(nameof(FilterStatus));
            OnPropertyChanged(nameof(FilterLevel));
            OnPropertyChanged(nameof(FilterText));
        }

        private bool MatchesType(InventoryRow row)
        {
            return filterType == Resources.Filter_All || string.Equals(row.TypeName, filterType, StringComparison.CurrentCultureIgnoreCase);
        }

        private void ApplyFilter()
        {
            var text = filterText.Trim();
            Rows.Clear();
            foreach (var row in allRows)
            {
                var record = row.Item?.Record;
                var haystack = string.Join(" ", row.Number, row.Name, row.SubType, row.Location, row.Level, record?.Note ?? string.Empty);
                if (MatchesType(row)
                    && (filterSubType == Resources.Filter_All || string.Equals(row.SubType, filterSubType, StringComparison.CurrentCultureIgnoreCase))
                    && (filterLevel == Resources.Filter_All || row.LevelName == filterLevel)
                    && (filterStatus == Resources.Filter_All || string.Equals(row.Status.Name, filterStatus, StringComparison.CurrentCultureIgnoreCase))
                    && (text.Length == 0 || SearchMatch.Contains(haystack, text)))
                {
                    Rows.Add(row);
                }
            }

            FilterSummary = Rows.Count == allRows.Count
                ? string.Format(Resources.Filter_Count, allRows.Count)
                : string.Format(Resources.Filter_CountOf, allRows.Count, Rows.Count);
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

            // Geçmiş: durum olayları ve kısa notlar, yeniden eskiye (aynı gün: not olaydan sonra yazılmış sayılır).
            var rows = record.Events.Select(e => new HistoryRow(e, shownType))
                .Concat(record.Notes.Select(n => new HistoryRow(n)))
                .Select((row, index) => new { row, index })
                .OrderByDescending(x => x.row.SortDate)
                .ThenByDescending(x => x.index)
                .Select(x => x.row);
            foreach (var row in rows)
            {
                History.Add(row);
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
        // Stoka ait kaydın stok klasörü (klasör yolunda sahip stok numarasının geçtiği yer); genel kayıtta boş.
        private static string OwnerStockFolder(InventoryItem shown)
        {
            if (shown.Owner.Length == 0)
            {
                return string.Empty;
            }

            var folder = System.IO.Path.GetDirectoryName(shown.Folder);
            while (!string.IsNullOrEmpty(folder) && !string.Equals(System.IO.Path.GetFileName(folder), shown.Owner, StringComparison.OrdinalIgnoreCase))
            {
                folder = System.IO.Path.GetDirectoryName(folder);
            }

            return folder ?? string.Empty;
        }

        private async Task LoadContentsAsync(InventoryItem shown)
        {
            ClearContents();
            var version = contentsVersion;
            List<Pusula.Core.Notes.EventNote> events = new List<Pusula.Core.Notes.EventNote>();
            var contents = await Task.Run(() =>
            {
                events = Pusula.Core.Notes.EventNote.ReadAll(shown.Folder);
                var contentsRead = RecordContents.Read(shown.Folder, shown.Record.Number);

                // Kendi notunun Günlük'ü, ayrıca ait olduğu stokun Günlük'ünde bu kayda bağlanan olaylar.
                events.AddRange(Pusula.Core.Notes.Journal.Read(contentsRead.NoteFile));
                var stockFolder = OwnerStockFolder(shown);
                if (stockFolder.Length > 0)
                {
                    var stockNote = Pusula.Core.Notes.NoteFile.Find(stockFolder, System.IO.Path.GetFileName(stockFolder));
                    events.AddRange(Pusula.Core.Notes.Journal.Read(stockNote)
                        .Where(e => e.Related.Any(r => string.Equals(r, shown.Record.Number, StringComparison.OrdinalIgnoreCase))));
                }

                return contentsRead;
            }).ConfigureAwait(true);
            if (version != contentsVersion)
            {
                return;
            }

            // Obsidian olay notları Geçmiş'e tarih sırasıyla katılır.
            if (events.Count > 0)
            {
                // Önceki yüklemeden kalan olay satırları atılır (yeniden yüklemede çiftlenmesin).
                var merged = History.Where(r => !r.HasNote).Concat(events.Select(e => new HistoryRow(e))).OrderByDescending(r => r.SortDate).ToList();
                History.Clear();
                foreach (var row in merged)
                {
                    History.Add(row);
                }
            }

            foreach (var folder in contents.Folders)
            {
                FolderTiles.Add(new RecordFolderTile(folder, TypeStore.DescribeFiles(fileTypes, folder.Files, Resources.FileType_Other)));
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

        /// <summary>Düzeyin yalın adı.</summary>
        public static string LevelName(InventoryLevel level)
        {
            switch (level)
            {
                case InventoryLevel.Project:
                    return Resources.Kind_Project;
                case InventoryLevel.Stock:
                    return Resources.Kind_Stock;
                default:
                    return Resources.Inv_LevelGeneral;
            }
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
