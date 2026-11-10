using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Pusula.Core.Inventory;
using Pusula.Core.Markdown;
using Pusula.Core.Tracking;
using Pusula.Core.Types;
using Pusula.Wpf.Properties;

namespace Pusula.Wpf.ViewModels
{
    /// <summary>Yeni kayıt diyaloğunda düzey seçeneği.</summary>
    public sealed class LevelOption
    {
        /// <summary>Seçeneği oluşturur.</summary>
        public LevelOption(string label, InventoryLevel level, string owner)
        {
            Label = label;
            Level = level;
            Owner = owner;
        }

        /// <summary>Görünen metin.</summary>
        public string Label { get; }

        /// <summary>Düzey.</summary>
        public InventoryLevel Level { get; }

        /// <summary>Proje / stok klasörü (genelde boş).</summary>
        public string Owner { get; }

        /// <inheritdoc />
        public override string ToString()
        {
            return Label;
        }
    }

    /// <summary>Ana pencere: envanter (kablo, jig …) — ağaç düğümleri, kayıt sayfası, yeni kayıt ve durum olayı.</summary>
    public sealed partial class MainViewModel
    {
        private List<InventoryItem> inventoryItems = new List<InventoryItem>();

        private bool isInventoryDialogOpen;
        private bool isInventoryMarkMode;
        private string markInventoryFolder = string.Empty;
        private string newInventoryStatus = string.Empty;
        private TypeDefinition? newInventoryType;
        private LevelOption? newInventoryLevel;
        private string newInventoryNumber = string.Empty;
        private string newInventoryName = string.Empty;
        private string newInventorySubType = string.Empty;
        private string newInventoryNote = string.Empty;
        private string inventoryError = string.Empty;

        private bool isEventDialogOpen;
        private string eventStatus = string.Empty;
        private DateTime eventDate = DateTime.Today;
        private string eventPerson = string.Empty;
        private string eventLocation = string.Empty;
        private string eventNote = string.Empty;

        /// <summary>Envanter seçiliyken sağ panel.</summary>
        public InventoryDetailViewModel Inventory { get; } = new InventoryDetailViewModel();

        /// <summary>Oturum açmış kullanıcı (olaylarda varsayılan kişi).</summary>
        public static string CurrentUser
        {
            get { return Environment.UserName; }
        }

        #region Ağaç

        // Genel düzey kök klasörü (örn. "ENVANTER"): ağaçta klasör olarak değil, Envanter bölümü olarak görünür.
        private bool IsGeneralInventoryRoot(string path)
        {
            return types.InventoryTypes
                .Select(t => t.GeneralFolder.Split('\\', '/').FirstOrDefault() ?? string.Empty)
                .Where(first => first.Length > 0)
                .Any(first => string.Equals(Path.GetFileName(path), first, StringComparison.OrdinalIgnoreCase));
        }

        // Envanter bölümü: bölüm → türler (Kablolar, Jigler) → kayıtlar (üç düzeyden).
        private void AddInventoryNodes(HashSet<string> expanded)
        {
            var inventoryTypes = types.InventoryTypes.ToList();
            inventoryItems = InventoryService.ReadAll(rootPath, inventoryTypes);
            var unmarked = InventoryService.ReadUnmarked(rootPath, inventoryTypes);
            if (inventoryTypes.Count == 0)
            {
                return;
            }

            var groupPath = Path.Combine(rootPath, inventoryTypes[0].GeneralFolder.Split('\\', '/')[0]);
            var group = new TreeNodeViewModel(new FolderEntry(groupPath, FolderKind.InventoryGroup, Resources.Tree_Inventory), null, null, string.Empty);
            foreach (var type in inventoryTypes)
            {
                var items = inventoryItems.Where(i => string.Equals(i.Record.Type, type.Key, StringComparison.OrdinalIgnoreCase)).ToList();
                var typePath = InventoryService.LevelFolder(type, InventoryLevel.General, rootPath, string.Empty);
                var typeNode = new TreeNodeViewModel(
                    new FolderEntry(typePath, FolderKind.InventoryType, type.PluralName.Length > 0 ? type.PluralName : type.Name),
                    type,
                    null,
                    items.Count.ToString());
                foreach (var entry in items)
                {
                    typeNode.Children.Add(new TreeNodeViewModel(new FolderEntry(entry.Folder, FolderKind.InventoryItem, entry.Record.Number), type, entry, entry.Record.Name));
                }

                // Elle açılmış, kaydı olmayan klasörler: soluk, sağ tıkla "… olarak işaretle".
                foreach (var entry in unmarked.Where(i => string.Equals(i.Record.Type, type.Key, StringComparison.OrdinalIgnoreCase)))
                {
                    typeNode.Children.Add(new TreeNodeViewModel(new FolderEntry(entry.Folder, FolderKind.Plain, entry.Record.Number), type, null, Resources.Inv_Unmarked));
                }

                typeNode.IsExpanded = expanded.Contains(typePath);
                group.Children.Add(typeNode);
            }

            group.IsExpanded = expanded.Count == 0 || expanded.Contains(groupPath);
            Nodes.Add(group);
            InventoryNodes.Add(group);
        }

        #endregion

        #region Yeni kayıt

        /// <summary>"Yeni kablo" gibi menü / başlık metni (tür adı küçük harfle).</summary>
        public static string NewMenuText(TypeDefinition? type)
        {
            var name = type?.Name ?? string.Empty;
            return string.Format(Resources.Inv_NewTitle, name.ToLower(System.Globalization.CultureInfo.GetCultureInfo("tr-TR")));
        }

        /// <summary>"+" menüsünde gösterilecek envanter türleri.</summary>
        public IEnumerable<TypeDefinition> InventoryTypes
        {
            get { return types.InventoryTypes; }
        }

        /// <summary>Yeni kayıt diyaloğu açık mı.</summary>
        public bool IsInventoryDialogOpen
        {
            get { return isInventoryDialogOpen; }
            set { SetDialogOpen(ref isInventoryDialogOpen, value); }
        }

        /// <summary>Diyalog başlığı ("Yeni kablo" ya da "Jig olarak işaretle · klasör").</summary>
        public string InventoryDialogTitle
        {
            get
            {
                return isInventoryMarkMode
                    ? MarkMenuText(newInventoryType) + " · " + Path.GetFileName(markInventoryFolder)
                    : NewMenuText(newInventoryType);
            }
        }

        /// <summary>Diyalog işaretleme için mi (elle açılmış klasör); değilse yeni kayıt.</summary>
        public bool IsInventoryMarkMode
        {
            get { return isInventoryMarkMode; }
            private set { SetProperty(ref isInventoryMarkMode, value); }
        }

        /// <summary>İşaretlemede seçilebilen durumlar (klasör zaten var; çoğu zaman "Kullanımda").</summary>
        public ObservableCollection<string> InventoryStatuses { get; } = new ObservableCollection<string>();

        /// <summary>İşaretlemede seçilen durum.</summary>
        public string NewInventoryStatus
        {
            get { return newInventoryStatus; }
            set { SetProperty(ref newInventoryStatus, value ?? string.Empty); }
        }

        /// <summary>Seçili düğüm elle açılmış bir envanter klasörü mü (sağ tık menüsü için).</summary>
        public bool CanMarkAsInventory
        {
            get { return selectedNode != null && selectedNode.IsUnmarkedInventory; }
        }

        /// <summary>Sağ tık menüsündeki "Jig olarak işaretle" metni.</summary>
        public string MarkInventoryMenuText
        {
            get { return MarkMenuText(selectedNode?.Type); }
        }

        /// <summary>Seçili elle açılmış klasörü işaretleme diyaloğunu açar.</summary>
        public System.Windows.Input.ICommand MarkAsInventoryCommand { get; }

        /// <summary>"Jig olarak işaretle" metni.</summary>
        public static string MarkMenuText(TypeDefinition? type)
        {
            return string.Format(Resources.Inv_MarkAs, type?.Name ?? string.Empty);
        }

        private void OpenMarkInventory()
        {
            if (selectedNode == null || !selectedNode.IsUnmarkedInventory || selectedNode.Type == null)
            {
                return;
            }

            var type = selectedNode.Type;
            newInventoryType = type;
            markInventoryFolder = selectedNode.Path;
            IsInventoryMarkMode = true;
            var suggestion = InventoryService.SuggestFromFolderName(type, Path.GetFileName(markInventoryFolder));
            InventorySubTypes.Clear();
            foreach (var subType in type.SubTypes)
            {
                InventorySubTypes.Add(subType);
            }

            InventoryStatuses.Clear();
            foreach (var status in type.Statuses.Concat(type.SideStatuses))
            {
                InventoryStatuses.Add(status.Name);
            }

            NewInventoryNumber = suggestion.Item1;
            NewInventoryName = suggestion.Item2;
            newInventoryValues.Clear();
            newInventorySubType = string.Empty;
            NewInventorySubType = InventorySubTypes.FirstOrDefault() ?? string.Empty;
            BuildNewInventoryFields();
            NewInventoryStatus = InventoryStatuses.Contains(Resources.Inv_DefaultMarkStatus) ? Resources.Inv_DefaultMarkStatus : type.InitialStatus;
            NewInventoryNote = string.Empty;
            NewInventoryRequestDate = DateTime.Today;
            NewInventoryRequester = CurrentUser;
            NewInventoryTags = string.Empty;
            InventoryError = string.Empty;
            OnPropertyChanged(nameof(InventoryDialogTitle));
            OnPropertyChanged(nameof(InventoryNumberHint));
            IsInventoryDialogOpen = true;
        }

        /// <summary>Düzey seçenekleri (Genel; seçime göre proje ve stok).</summary>
        public ObservableCollection<LevelOption> InventoryLevels { get; } = new ObservableCollection<LevelOption>();

        /// <summary>Seçili düzey.</summary>
        public LevelOption? NewInventoryLevel
        {
            get { return newInventoryLevel; }
            set { SetProperty(ref newInventoryLevel, value); }
        }

        /// <summary>Alt tür seçenekleri.</summary>
        public ObservableCollection<string> InventorySubTypes { get; } = new ObservableCollection<string>();

        /// <summary>Girilen numara.</summary>
        public string NewInventoryNumber
        {
            get { return newInventoryNumber; }
            set { SetProperty(ref newInventoryNumber, value ?? string.Empty); }
        }

        /// <summary>Girilen tanım.</summary>
        public string NewInventoryName
        {
            get { return newInventoryName; }
            set { SetProperty(ref newInventoryName, value ?? string.Empty); }
        }

        /// <summary>Seçilen alt tür; değişince türe özel alanlar o alt türe göre yeniden kurulur (girilenler kaybolmaz).</summary>
        public string NewInventorySubType
        {
            get { return newInventorySubType; }
            set
            {
                // Seçenekler yenilenirken açılır kutu boş yazar; yok sayılır.
                if (value != null && SetProperty(ref newInventorySubType, value))
                {
                    BuildNewInventoryFields();
                }
            }
        }

        private readonly System.Collections.Generic.Dictionary<string, string> newInventoryValues = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Yeni kayıtta türe özel alanlar (alt türe göre: Adet, Malzeme, Tork …).</summary>
        public ObservableCollection<FieldInput> NewInventoryFields { get; } = new ObservableCollection<FieldInput>();

        /// <summary>Yeni kayıtta alan var mı.</summary>
        public bool HasNewInventoryFields
        {
            get { return NewInventoryFields.Count > 0; }
        }

        /// <summary>Alan bölümünün başlığı: "Özellikler · Mekanik".</summary>
        public string NewInventoryFieldsHeader
        {
            get { return FieldsHeader(newInventorySubType); }
        }

        /// <summary>"Özellikler" ya da "Özellikler · alt tür".</summary>
        public static string FieldsHeader(string subType)
        {
            return string.IsNullOrEmpty(subType) ? Resources.Fields_Header : string.Format(Resources.Fields_HeaderSub, subType);
        }

        private void BuildNewInventoryFields()
        {
            foreach (var input in NewInventoryFields)
            {
                newInventoryValues[input.Key] = input.Value;
            }

            NewInventoryFields.Clear();
            if (newInventoryType != null)
            {
                foreach (var field in newInventoryType.FieldsFor(newInventorySubType))
                {
                    string value;
                    NewInventoryFields.Add(new FieldInput(field, newInventoryValues.TryGetValue(field.Key, out value) ? value : string.Empty, newInventorySubType));
                }
            }

            OnPropertyChanged(nameof(HasNewInventoryFields));
            OnPropertyChanged(nameof(NewInventoryFieldsHeader));
        }

        private DateTime? newInventoryRequestDate = DateTime.Today;
        private string newInventoryRequester = string.Empty;

        /// <summary>Talep tarihi (ilk durum olayının tarihi); varsayılan bugün.</summary>
        public DateTime? NewInventoryRequestDate
        {
            get { return newInventoryRequestDate; }
            set { SetProperty(ref newInventoryRequestDate, value); }
        }

        /// <summary>Talep eden (ilk durum olayının kişisi); varsayılan bu bilgisayarın kullanıcısı.</summary>
        public string NewInventoryRequester
        {
            get { return newInventoryRequester; }
            set { SetProperty(ref newInventoryRequester, value ?? string.Empty); }
        }

        private string newInventoryTags = string.Empty;

        /// <summary>Yeni kaydın etiketleri, kutuda yazıldığı gibi: <c>#SATIMO #faz_kararli</c>.</summary>
        public string NewInventoryTags
        {
            get { return newInventoryTags; }
            set { SetProperty(ref newInventoryTags, value ?? string.Empty); }
        }

        /// <summary>Girilen not.</summary>
        public string NewInventoryNote
        {
            get { return newInventoryNote; }
            set { SetProperty(ref newInventoryNote, value ?? string.Empty); }
        }

        /// <summary>Kabul edilen numara biçimleri (örnekle).</summary>
        public string InventoryNumberHint
        {
            get
            {
                if (newInventoryType == null)
                {
                    return string.Empty;
                }

                return string.Join(" · ", newInventoryType.NumberFormats.Select(f => NumberPattern.Example(f.Pattern)));
            }
        }

        /// <summary>Diyalogdaki hata; boşsa gizlidir.</summary>
        public string InventoryError
        {
            get { return inventoryError; }
            private set { SetProperty(ref inventoryError, value); }
        }

        /// <summary>Verilen tür için yeni kayıt diyaloğunu açar; düzey seçenekleri seçime göre kurulur.</summary>
        public void OpenNewInventory(TypeDefinition type)
        {
            if (!HasRoot)
            {
                Status = Resources.Status_SelectRootFirst;
                return;
            }

            newInventoryType = type;
            IsInventoryMarkMode = false;
            InventoryLevels.Clear();
            InventoryLevels.Add(new LevelOption(Resources.Inv_LevelGeneral, InventoryLevel.General, string.Empty));
            var project = SelectedProjectFolder();
            if (project.Length > 0)
            {
                InventoryLevels.Add(new LevelOption(string.Format(Resources.Inv_LevelProject, Path.GetFileName(project)), InventoryLevel.Project, project));
            }

            if (selectedNode != null && selectedNode.Kind == FolderKind.Stock)
            {
                InventoryLevels.Add(new LevelOption(string.Format(Resources.Inv_LevelStock, selectedNode.Title), InventoryLevel.Stock, selectedNode.Path));
            }

            InventorySubTypes.Clear();
            foreach (var subType in type.SubTypes)
            {
                InventorySubTypes.Add(subType);
            }

            NewInventoryLevel = InventoryLevels.Last();
            NewInventoryNumber = string.Empty;
            NewInventoryName = string.Empty;
            newInventoryValues.Clear();
            newInventorySubType = string.Empty;
            NewInventorySubType = InventorySubTypes.FirstOrDefault() ?? string.Empty;
            BuildNewInventoryFields();
            NewInventoryNote = string.Empty;
            NewInventoryRequestDate = DateTime.Today;
            NewInventoryRequester = CurrentUser;
            NewInventoryTags = string.Empty;
            InventoryError = string.Empty;
            OnPropertyChanged(nameof(InventoryDialogTitle));
            OnPropertyChanged(nameof(InventoryNumberHint));
            IsInventoryDialogOpen = true;
        }

        /// <summary>Kaydı açar. Başarısızsa <c>false</c> döner ve hata metnini doldurur; çağıran diyaloğu yeniden açar.</summary>
        public bool ConfirmNewInventory()
        {
            if (newInventoryType == null || (!isInventoryMarkMode && newInventoryLevel == null))
            {
                return true;
            }

            var result = isInventoryMarkMode
                ? InventoryService.Mark(rootPath, newInventoryType, markInventoryFolder, NewInventoryNumber, NewInventoryName, NewInventorySubType, NewInventoryStatus, CurrentUser, NewInventoryNote)
                : InventoryService.Create(rootPath, newInventoryType, newInventoryLevel!.Level, newInventoryLevel.Owner,
                    NewInventoryNumber, NewInventoryName, NewInventorySubType, NewInventoryNote,
                    NewInventoryRequester.Trim().Length > 0 ? NewInventoryRequester.Trim() : CurrentUser, NewInventoryRequestDate);
            if (!result.Succeeded)
            {
                InventoryError = result.Error == CreateError.NumberFormat
                    ? string.Format(Resources.Error_StockNumberFormat, InventoryNumberHint)
                    : ErrorText(result);
                return false;
            }

            // Türe özel alanlar: kayıt açıldıktan sonra kayda ve notun YAML başlığına yazılır.
            foreach (var input in NewInventoryFields)
            {
                newInventoryValues[input.Key] = input.Value;
            }

            var newTags = MarkdownEditing.ParseTags(NewInventoryTags);
            if (NewInventoryFields.Any(i => i.Value.Trim().Length > 0) || newTags.Count > 0)
            {
                var fieldsError = RecordEdit.EditInventory(result.Folder, NewInventoryName, NewInventorySubType, NewInventoryNote, newInventoryValues, newTags);
                if (fieldsError.Length == 0)
                {
                    WriteFieldsToNote(result.Folder, newInventoryType, NewInventoryName, NewInventorySubType, newInventoryValues, newTags);
                }
                else
                {
                    Status = string.Format(Resources.Error_Io, fieldsError);
                }
            }

            Refresh();
            ShowInventory(result.Folder);
            Done(string.Format(Resources.Inv_Created, result.Folder));
            return true;
        }

        /// <summary>Ağaçta kaydı bulur, türünü açar ve seçer.</summary>
        public void ShowInventory(string folder)
        {
            foreach (var group in Nodes.Where(n => n.Kind == FolderKind.InventoryGroup))
            {
                group.IsExpanded = true;
                foreach (var typeNode in group.Children)
                {
                    var match = typeNode.Children.FirstOrDefault(c => string.Equals(c.Path, folder, StringComparison.OrdinalIgnoreCase));
                    if (match != null)
                    {
                        typeNode.IsExpanded = true;
                        match.IsSelected = true;
                        SelectedNode = match;
                    }
                }
            }
        }

        /// <summary>
        /// Seçili stoka bağlanabilecek kayıtlar: türün genel ve proje düzeyindeki kayıtlarından henüz bağlı olmayanlar.
        /// </summary>
        public List<InventoryItem> LinkCandidates(TypeDefinition type)
        {
            var stock = selectedNode;
            if (stock == null || stock.Kind != FolderKind.Stock)
            {
                return new List<InventoryItem>();
            }

            var info = JsonFile.TryRead<StockInfo>(TrackingFiles.StockFile(stock.Path));
            var links = info?.Inventory ?? new List<string>();
            return inventoryItems
                .Where(i => i.Level != InventoryLevel.Stock
                    && string.Equals(i.Record.Type, type.Key, StringComparison.OrdinalIgnoreCase)
                    && !links.Contains(i.Record.Number, StringComparer.OrdinalIgnoreCase))
                .OrderBy(i => i.Level)
                .ThenBy(i => i.Record.Number, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>Seçili stoka kaydı bağlar ya da bağı kaldırır; kaydın klasörüne dokunulmaz.</summary>
        public void SetStockLink(string number, bool linked)
        {
            var stock = selectedNode;
            if (stock == null || stock.Kind != FolderKind.Stock)
            {
                return;
            }

            if (!InventoryService.SetStockLink(stock.Path, number, linked))
            {
                Status = Resources.StockInv_LinkFailed;
                return;
            }

            Status = string.Format(linked ? Resources.StockInv_Linked : Resources.StockInv_Unlinked, number, stock.Title);
            Detail.Reload();
        }

        /// <summary>Seçili stokun notunu (<c>&lt;stok no&gt;.md</c>) oluşturur ve açar; varsa yalnızca açar.</summary>
        public void CreateStockNote()
        {
            var stock = selectedNode;
            if (stock == null || stock.Kind != FolderKind.Stock)
            {
                return;
            }

            var info = JsonFile.TryRead<StockInfo>(TrackingFiles.StockFile(stock.Path)) ?? new StockInfo { StockNo = stock.Title };
            var project = Path.GetFileName(Path.GetDirectoryName(stock.Path) ?? string.Empty);
            var note = Pusula.Core.Notes.StockCover.CreateNote(stock.Path, info, project);
            if (note.Length == 0)
            {
                Status = Resources.Inv_NoteFailed;
                return;
            }

            Detail.Reload();
            OpenNoteOrFolder(note, stock.Path);
        }

        /// <summary>Seçili kaydın notunu (<c>&lt;numara&gt;.md</c>) oluşturur ve açar; varsa yalnızca açar.</summary>
        public async System.Threading.Tasks.Task CreateInventoryNoteAsync()
        {
            var shown = Inventory.Item;
            if (shown == null)
            {
                return;
            }

            var note = InventoryService.CreateNote(shown.Folder, shown.Record);
            if (note.Length == 0)
            {
                Status = Resources.Inv_NoteFailed;
                return;
            }

            await Inventory.ReloadContentsAsync().ConfigureAwait(true);
            OpenNoteOrFolder(note, shown.Folder);
        }

        #endregion

        #region Durum olayı

        /// <summary>Durum değiştirme diyaloğu açık mı.</summary>
        public bool IsEventDialogOpen
        {
            get { return isEventDialogOpen; }
            set { SetDialogOpen(ref isEventDialogOpen, value); }
        }

        /// <summary>Diyalog başlığı ("Durum → Kullanımda").</summary>
        public string EventTitle
        {
            get { return string.Format(Resources.Inv_EventTitle, eventStatus); }
        }

        /// <summary>Olay tarihi.</summary>
        public DateTime EventDate
        {
            get { return eventDate; }
            set { SetProperty(ref eventDate, value); }
        }

        /// <summary>Olaydaki kişi.</summary>
        public string EventPerson
        {
            get { return eventPerson; }
            set { SetProperty(ref eventPerson, value ?? string.Empty); }
        }

        /// <summary>Olaydaki konum (boşsa önceki konum geçerli kalır).</summary>
        public string EventLocation
        {
            get { return eventLocation; }
            set { SetProperty(ref eventLocation, value ?? string.Empty); }
        }

        /// <summary>Olay notu.</summary>
        public string EventNote
        {
            get { return eventNote; }
            set { SetProperty(ref eventNote, value ?? string.Empty); }
        }

        /// <summary>Gösterilen kayıt için durum değiştirme diyaloğunu açar.</summary>
        public void AskInventoryEvent(string status)
        {
            if (Inventory.Item == null)
            {
                return;
            }

            eventStatus = status;
            OnPropertyChanged(nameof(EventTitle));
            EventDate = DateTime.Today;
            EventPerson = CurrentUser;
            EventLocation = string.Empty;
            EventNote = string.Empty;
            IsEventDialogOpen = true;
        }

        /// <summary>Olayı kayda yazar ve sayfayı yeniler.</summary>
        public void ConfirmInventoryEvent()
        {
            var shown = Inventory.Item;
            if (shown == null)
            {
                return;
            }

            var inventoryEvent = InventoryEvent.Create(eventStatus, EventDate, EventPerson, EventLocation, EventNote);
            if (!InventoryService.AddEvent(shown.Folder, inventoryEvent))
            {
                Status = string.Format(Resources.Error_Io, shown.Folder);
                return;
            }

            var folder = shown.Folder;
            Refresh();
            ShowInventory(folder);
            Done(string.Format(Resources.Status_StatusChanged, shown.Record.Number, eventStatus));
        }

        #endregion
    }
}
