using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Pusula.Core.Inventory;
using Pusula.Core.Markdown;
using Pusula.Core.Notes;
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
            set
            {
                // Seçenekler yenilenirken açılır kutu boş (null) yazar; yok sayılır, yoksa alt tür silinirdi.
                if (value != null && SetProperty(ref editSubType, value))
                {
                    BuildEditFields();
                }
            }
        }

        private string editRequestDateText = string.Empty;
        private string editRequester = string.Empty;
        private string editMaker = string.Empty;
        private bool editDocsReady;
        private DateTime? editRequestOriginal;
        private string editRequesterOriginal = string.Empty;

        /// <summary>Süreç (isteğe bağlı): talep tarihi, elle (gg.aa.yyyy).</summary>
        public string EditRequestDateText
        {
            get { return editRequestDateText; }
            set { SetProperty(ref editRequestDateText, value ?? string.Empty); }
        }

        /// <summary>Süreç: talep eden (# ile kişi önerisi).</summary>
        public string EditRequester
        {
            get { return editRequester; }
            set { SetProperty(ref editRequester, value ?? string.Empty); }
        }

        /// <summary>Süreç: yapan kişi (kablonun / jigin yapanı).</summary>
        public string EditMaker
        {
            get { return editMaker; }
            set { SetProperty(ref editMaker, value ?? string.Empty); }
        }

        /// <summary>Süreç: doküman hazır mı.</summary>
        public bool EditDocsReady
        {
            get { return editDocsReady; }
            set { SetProperty(ref editDocsReady, value); }
        }

        /// <summary>Kayıt alanında yapan kişinin anahtarı (türe özel alan değil, süreç bilgisi).</summary>
        public const string MakerKey = "yapan";

        /// <summary>Kayıt alanında "doküman hazır" anahtarı ("Evet" ya da yok).</summary>
        public const string DocsReadyKey = "dokuman_hazir";

        private string editTags = string.Empty;

        /// <summary>Kaydın etiketleri, kutuda yazıldığı gibi: <c>#SATIMO #faz_kararli</c>.</summary>
        public string EditTags
        {
            get { return editTags; }
            set { SetProperty(ref editTags, value ?? string.Empty); }
        }

        /// <summary>
        /// Açık kayıt penceresinin (düzenle ya da yeni kayıt) türünde ve alt türünde geçerli etiketler (<c>#</c> önerisi için).
        /// </summary>
        public System.Collections.Generic.IEnumerable<NoteSuggestion> TagSuggestions()
        {
            // Bağlam: açık düzenle / yeni kayıt penceresi, yoksa gösterilen kayıt ya da stok; hiçbiri değilse bütün etiketler.
            var type = IsEditOpen ? editType : IsInventoryDialogOpen ? newInventoryType : null;
            var subType = IsEditOpen ? editSubType : IsInventoryDialogOpen ? NewInventorySubType : string.Empty;
            if (type == null && Inventory.Item != null)
            {
                type = types.Find(Inventory.Item.Record.Type);
                subType = Inventory.Item.Record.SubType;
            }
            else if (type == null && Detail.IsStock)
            {
                type = types.Stock;
            }

            var list = type == null ? types.TagList : types.TagsFor(type.Key, subType);
            return list.Select(t => new NoteSuggestion("#" + t.Name, TagScopeText(t), Resources.Tag_Kind, string.Empty, "#" + t.Name)).ToList();
        }

        // "Her yerde" ya da "Kablo · RF, Jig".
        private string TagScopeText(Pusula.Core.Types.TagDefinition tag)
        {
            if (tag.Scopes.Count == 0)
            {
                return Resources.Tag_Everywhere;
            }

            return string.Join(", ", tag.Scopes.Select(s => (types.Find(s.Type)?.Name ?? s.Type) + (s.SubType.Length > 0 ? " · " + s.SubType : string.Empty)));
        }

        /// <summary>Düzenlenen türün alt türü var mı (yoksa Alt tür satırı gizli).</summary>
        public bool HasEditSubTypes
        {
            get { return EditSubTypes.Count > 0; }
        }

        /// <summary>Tanım kutusunun ipucu, türe göre (kablo: <c>TST KBL XXXX</c>, jig: <c>TST MKNK XXXX</c> …).</summary>
        public string EditNamePlaceholder
        {
            get { return NamePlaceholder(editType?.Key); }
        }

        /// <summary>Türe göre tanım örneği; tanımlı değilse boş.</summary>
        public static string NamePlaceholder(string? typeKey)
        {
            switch (typeKey)
            {
                case "cable":
                    return Resources.Placeholder_NameCable;
                case "jig":
                    return Resources.Placeholder_NameJig;
                case "software":
                    return Resources.Placeholder_NameSoftware;
                case Pusula.Core.Types.TypeSet.EquipmentKey:
                    return Resources.Placeholder_NameEquipment;
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Talep eden kutusunda <c>#</c> (ya da <c>@</c>) ile önerilen kişiler: kayıtların olay ve notlarındaki kişiler ve
        /// bu bilgisayarın kullanıcısı. Seçilince yalnız ad yazılır.
        /// </summary>
        public System.Collections.Generic.IEnumerable<NoteSuggestion> PersonSuggestions()
        {
            return NoteSuggestions().Where(s => s.Kind == Resources.Note_KindPerson).Select(s => new NoteSuggestion(s.Key, s.Title, s.Kind, s.Glyph, s.Key)).ToList();
        }

        /// <summary>Alan bölümünün başlığı: "Özellikler · Mekanik".</summary>
        public string EditFieldsHeader
        {
            get { return FieldsHeader(editSubType); }
        }

        private Pusula.Core.Types.TypeDefinition? editType;
        private System.Collections.Generic.Dictionary<string, string> editValues = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Türün bu alt türe uyan alanları (Adet, Boy, Uç tipi …); değerleri düzenlenir.</summary>
        public ObservableCollection<FieldInput> EditFields { get; } = new ObservableCollection<FieldInput>();

        /// <summary>Alan var mı.</summary>
        public bool HasEditFields
        {
            get { return EditFields.Count > 0; }
        }

        // Alt tür değişince alan listesi yeniden kurulur; girilen değerler kaybolmaz.
        private void BuildEditFields()
        {
            foreach (var input in EditFields)
            {
                editValues[input.Key] = input.Value;
            }

            EditFields.Clear();
            if (editType != null)
            {
                foreach (var field in editType.FieldsFor(editSubType))
                {
                    string value;
                    EditFields.Add(new FieldInput(field, editValues.TryGetValue(field.Key, out value) ? value : string.Empty, editSubType));
                }
            }

            OnPropertyChanged(nameof(HasEditFields));
            OnPropertyChanged(nameof(EditFieldsHeader));
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
            editType = null;
            EditFields.Clear();
            OnPropertyChanged(nameof(HasEditFields));
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
                    OnPropertyChanged(nameof(EditTemplate));
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

                    var currentSub = type?.CanonicalSubType(record?.SubType ?? string.Empty) ?? record?.SubType ?? string.Empty;
                    if (currentSub.Length > 0 && !EditSubTypes.Contains(currentSub))
                    {
                        EditSubTypes.Add(currentSub);
                    }

                    editType = type;
                    OnPropertyChanged(nameof(EditNamePlaceholder));
                    OnPropertyChanged(nameof(HasEditSubTypes));
                    editValues = new System.Collections.Generic.Dictionary<string, string>(record?.Fields ?? new System.Collections.Generic.Dictionary<string, string>(), StringComparer.OrdinalIgnoreCase);
                    EditTags = MarkdownEditing.FormatTags(record?.EffectiveTags() ?? new System.Collections.Generic.List<string>());
                    var request = record?.Events.FirstOrDefault();
                    editRequestOriginal = request?.DateValue?.Date;
                    editRequesterOriginal = request?.Person ?? string.Empty;
                    EditRequestDateText = editRequestOriginal.HasValue ? editRequestOriginal.Value.ToString("dd.MM.yyyy", System.Globalization.CultureInfo.InvariantCulture) : string.Empty;
                    EditRequester = editRequesterOriginal;
                    string maker, docs;
                    var making = type != null && type.Statuses.Count > 1 ? record?.FirstEventOf(type.Statuses[1].Name) : null;
                    EditMaker = record != null && record.Fields.TryGetValue(MakerKey, out maker) ? maker : making?.Person ?? string.Empty;
                    EditDocsReady = record != null && record.Fields.TryGetValue(DocsReadyKey, out docs) && string.Equals(docs, Resources.Field_Yes, StringComparison.OrdinalIgnoreCase);
                    EditFields.Clear();
                    EditSubType = currentSub;
                    BuildEditFields();

                    // Liste yenilenince kutunun seçimi boşalmış olabilir; değer aynı kalsa da yeniden bildirilir.
                    OnPropertyChanged(nameof(EditSubType));
                    break;
                }
            }

            OnPropertyChanged(nameof(IsEditStock));
            OnPropertyChanged(nameof(IsEditInventory));
            EditTitle = string.Format(Resources.Edit_Title, EditNumber);
            IsEditOpen = true;
            return true;
        }

        // Alan değerleri notun YAML başlığına da yazılır (gövdeye dokunulmaz). Başlıkta yalnız bu alt türde geçerli alanlar
        // durur; ötekiler başlıktan kalkar (değerleri kayıtta saklı kalır). Not yoksa yalnız kayıt dosyası yazılmıştır.
        private void WriteFieldsToNote(string folder, Pusula.Core.Types.TypeDefinition? type, string name, string subType, System.Collections.Generic.IDictionary<string, string> values, System.Collections.Generic.IList<string>? tags = null)
        {
            var record = JsonFile.TryRead<InventoryRecord>(InventoryService.RecordFile(folder));
            var note = NoteFile.Find(folder, record?.Number ?? Path.GetFileName(folder));
            if (string.IsNullOrEmpty(note) || !File.Exists(note))
            {
                return;
            }

            var yaml = new System.Collections.Generic.Dictionary<string, string> { { "tanim", name }, { "alt_tur", subType } };
            var applies = new System.Collections.Generic.HashSet<string>(type?.FieldsFor(subType).Select(f => f.Key) ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            foreach (var field in type?.Fields ?? new System.Collections.Generic.List<Pusula.Core.Types.FieldDefinition>())
            {
                string value;
                yaml[field.Key] = applies.Contains(field.Key) && values.TryGetValue(field.Key, out value) && value.Trim().Length > 0
                    ? value.Trim() + (field.Unit.Length > 0 && field.Kind == Pusula.Core.Types.FieldKind.Number ? " " + field.Unit : string.Empty)
                    : string.Empty;
            }

            // Etiketler Obsidian'ın kendi alanına: tags: [a, b] (Obsidian'da etiket olarak görünür, aranır).
            var lists = tags == null ? null : new System.Collections.Generic.Dictionary<string, System.Collections.Generic.IList<string>> { { "tags", tags.Select(Pusula.Core.Types.TagDefinition.NameFrom).Where(t => t.Length > 0).ToList() } };
            var yamlError = Journal.SetFrontMatter(note, yaml, lists);
            if (yamlError.Length > 0)
            {
                Status = string.Format(Resources.Error_Io, yamlError);
            }
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
                    foreach (var input in EditFields)
                    {
                        editValues[input.Key] = input.Value;
                    }

                    // Etikete dönen eski alanlar (faz_kararli, protokol): türde artık alan değilse kayıttan düşer; etiket listesi esas.
                    foreach (var legacy in new[] { "faz_kararli", "protokol" })
                    {
                        if (editType == null || !editType.Fields.Any(x => string.Equals(x.Key, legacy, StringComparison.OrdinalIgnoreCase)))
                        {
                            editValues.Remove(legacy);
                        }
                    }

                    // Süreç bilgisi (isteğe bağlı): tarih denetlenir; yapan ve doküman kayıt alanında saklanır.
                    DateTime? requestDay = null;
                    if (EditRequestDateText.Trim().Length > 0)
                    {
                        requestDay = ParseDay(EditRequestDateText);
                        if (!requestDay.HasValue || requestDay.Value > DateTime.Today)
                        {
                            EditError = requestDay.HasValue ? Resources.Date_Future : string.Format(Resources.Date_Invalid, EditRequestDateText);
                            return false;
                        }
                    }

                    editValues[MakerKey] = EditMaker.Trim();
                    editValues[DocsReadyKey] = EditDocsReady ? Resources.Field_Yes : string.Empty;
                    var tags = MarkdownEditing.ParseTags(EditTags);
                    error = RecordEdit.EditInventory(editFolder, EditName, EditSubType, EditNote, editValues, tags);
                    if (error.Length == 0)
                    {
                        WriteFieldsToNote(editFolder, editType, EditName, EditSubType, editValues, tags);
                        var requester = EditRequester.Trim();
                        if ((requestDay.HasValue && requestDay != editRequestOriginal) || !string.Equals(requester, editRequesterOriginal, StringComparison.Ordinal))
                        {
                            error = RecordEdit.SetRequest(editFolder, requestDay ?? editRequestOriginal ?? DateTime.Today, requester);
                        }
                    }

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
            Done(string.Format(Resources.Edit_Saved, EditNumber));
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
            Done(string.Format(Resources.Untrack_Done, Path.GetFileName(editFolder)));
            return true;
        }

        private bool isDeleteOpen;
        private string deleteTitle = string.Empty;
        private string deleteText = string.Empty;
        private string deleteTyped = string.Empty;
        private string deleteExpected = string.Empty;

        /// <summary>Silme onayı açık mı.</summary>
        public bool IsDeleteOpen
        {
            get { return isDeleteOpen; }
            set { SetDialogOpen(ref isDeleteOpen, value); }
        }

        /// <summary>Silme onayının başlığı.</summary>
        public string DeleteTitle
        {
            get { return deleteTitle; }
            private set { SetProperty(ref deleteTitle, value); }
        }

        /// <summary>Silme onayının açıklaması (nereye taşınacağı).</summary>
        public string DeleteText
        {
            get { return deleteText; }
            private set { SetProperty(ref deleteText, value); }
        }

        /// <summary>Onay için yazılması gereken ad (klasör adı).</summary>
        public string DeleteExpected
        {
            get { return deleteExpected; }
            private set { SetProperty(ref deleteExpected, value); }
        }

        /// <summary>Kullanıcının yazdığı onay metni.</summary>
        public string DeleteTyped
        {
            get { return deleteTyped; }
            set
            {
                if (SetProperty(ref deleteTyped, value ?? string.Empty))
                {
                    OnPropertyChanged(nameof(DeletePrimaryText));
                }
            }
        }

        /// <summary>Sil düğmesinin yazısı: ad doğru yazılana kadar boş (düğme görünmez).</summary>
        public string DeletePrimaryText
        {
            get { return IsDeleteTypedRight ? Resources.Delete_Confirm : string.Empty; }
        }

        private bool IsDeleteTypedRight
        {
            get { return deleteExpected.Length > 0 && string.Equals(deleteTyped.Trim(), deleteExpected, StringComparison.OrdinalIgnoreCase); }
        }

        /// <summary>Seçili düğüm için silme onayını açar.</summary>
        public void OpenDeleteSelected()
        {
            if (selectedNode != null)
            {
                OpenDelete(selectedNode.Path);
            }
        }

        /// <summary>Silme onayını açar (takip edilen kayıtlar için).</summary>
        public bool OpenDelete(string folder)
        {
            if (!KindOf(folder).HasValue)
            {
                return false;
            }

            editFolder = folder;
            DeleteExpected = Path.GetFileName(folder.TrimEnd('\\', '/'));
            DeleteTyped = string.Empty;
            DeleteTitle = string.Format(Resources.Delete_Title, DeleteExpected);
            DeleteText = string.Format(Resources.Delete_Text, Path.Combine(rootPath, RecordEdit.TrashFolderName), DeleteExpected);
            OnPropertyChanged(nameof(DeletePrimaryText));
            IsDeleteOpen = true;
            return true;
        }

        /// <summary>Ad doğru yazıldıysa klasörü silinenlere taşır. Taşındıysa <c>true</c>.</summary>
        public bool ConfirmDelete()
        {
            IsDeleteOpen = false;
            if (!IsDeleteTypedRight)
            {
                Status = Resources.Delete_NotTyped;
                return false;
            }

            string target;
            var error = RecordEdit.MoveToTrash(rootPath, editFolder, DateTime.Now, CurrentUser, out target);
            if (error.Length > 0)
            {
                Status = string.Format(Resources.Error_Io, error);
                return false;
            }

            Refresh();
            Done(string.Format(Resources.Delete_Done, DeleteExpected, target));
            return true;
        }

        /// <summary>Kökteki silinenler klasörünü açar (yoksa bir şey yapmaz).</summary>
        public void OpenTrash()
        {
            OpenPath(Path.Combine(rootPath, RecordEdit.TrashFolderName));
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

    /// <summary>Türe özel bir alanın girişi (düzenle ve yeni kayıt pencereleri ortak): metin, sayı, evet/hayır ya da seçim.</summary>
    public sealed class FieldInput : Mvvm.ViewModelBase
    {
        private string value;

        /// <summary>Alanı ve şimdiki değerini kurar.</summary>
        public FieldInput(Pusula.Core.Types.FieldDefinition field, string current, string subType = "")
        {
            Key = field.Key;
            Label = field.Unit.Length > 0 ? field.Name + " (" + field.Unit + ")" : field.Name;
            // Başta boş seçenek (değeri silmek için); liste alt türe göre (kablo ucu: RF'de SMA, Güç'te Banana …);
            // kayıtlı değer listede yoksa o da eklenir (seçili görünsün).
            Choices = new[] { string.Empty }.Concat(field.ChoicesFor(subType)).ToList();
            if (!string.IsNullOrEmpty(current) && !Choices.Contains(current, StringComparer.CurrentCultureIgnoreCase))
            {
                Choices.Add(current);
            }
            IsChoice = field.Kind == Pusula.Core.Types.FieldKind.Choice;
            // İpucu birimin kendisi ("m", "MHz"); birimsizse "sayı" / "yaz…".
            Placeholder = field.Unit.Length > 0 ? field.Unit
                : field.Kind == Pusula.Core.Types.FieldKind.Number ? Pusula.Wpf.Properties.Resources.Placeholder_Number : Pusula.Wpf.Properties.Resources.Placeholder_Text;
            IsYesNo = field.Kind == Pusula.Core.Types.FieldKind.YesNo;
            value = current ?? string.Empty;
        }

        /// <summary>Anahtar.</summary>
        public string Key { get; }

        /// <summary>Görünen ad (birimle).</summary>
        public string Label { get; }

        /// <summary>Seçim listesi (seçim alanında).</summary>
        public System.Collections.Generic.List<string> Choices { get; }

        /// <summary>Kutu boşken görünen ipucu ("sayı (m)", "yaz …").</summary>
        public string Placeholder { get; }

        /// <summary>Seçim alanı mı (listeden ya da yazarak).</summary>
        public bool IsChoice { get; }

        /// <summary>Evet / hayır alanı mı.</summary>
        public bool IsYesNo { get; }

        /// <summary>Düz yazı ya da sayı alanı mı.</summary>
        public bool IsText
        {
            get { return !IsChoice && !IsYesNo; }
        }

        /// <summary>Değer (metin olarak; evet/hayır "Evet" / "Hayır").</summary>
        public string Value
        {
            get { return value; }
            set
            {
                if (SetProperty(ref this.value, value ?? string.Empty))
                {
                    OnPropertyChanged(nameof(IsYes));
                }
            }
        }

        /// <summary>Evet / hayır kutusu.</summary>
        public bool IsYes
        {
            get { return string.Equals(value, Pusula.Wpf.Properties.Resources.Field_Yes, StringComparison.OrdinalIgnoreCase); }
            set { Value = value ? Pusula.Wpf.Properties.Resources.Field_Yes : Pusula.Wpf.Properties.Resources.Field_No; }
        }
    }
}