using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Pusula.Core.Inventory;
using Pusula.Core.Notes;
using Pusula.Core.Tracking;
using Pusula.Wpf.Properties;

namespace Pusula.Wpf.ViewModels
{
    /// <summary>"İlgili" listesinde seçilebilen kayıt (jig, kablo, seri numarası).</summary>
    public sealed class RelatedOption : Mvvm.ViewModelBase
    {
        private bool isChecked;

        /// <summary>Seçeneği kurar.</summary>
        public RelatedOption(string number, string label)
        {
            Number = number;
            Label = label;
        }

        /// <summary>Numara (notta <c>[[numara]]</c> olur).</summary>
        public string Number { get; }

        /// <summary>Görünen ad ("JIG-1000-0009 · SATIMO adaptör plakası").</summary>
        public string Label { get; }

        /// <summary>Seçili mi.</summary>
        public bool IsChecked
        {
            get { return isChecked; }
            set { SetProperty(ref isChecked, value); }
        }
    }

    /// <summary>Ana görünüm: "+ Olay" — ana notun <c>## Günlük</c> bölümüne olay ekleme.</summary>
    public sealed partial class MainViewModel
    {
        private bool isJournalOpen;
        private string journalTitle = string.Empty;
        private string journalKind = string.Empty;
        private DateTime? journalDate = DateTime.Today;
        private string journalHeading = string.Empty;
        private string journalBody = string.Empty;
        private string journalImportance = string.Empty;
        private string journalTags = string.Empty;
        private string journalNotePath = string.Empty;
        private string journalFolder = string.Empty;

        /// <summary>Olay penceresi açık mı.</summary>
        public bool IsJournalOpen
        {
            get { return isJournalOpen; }
            set { SetDialogOpen(ref isJournalOpen, value); }
        }

        /// <summary>Pencere başlığı ("Olay ekle · AB-7000-0002").</summary>
        public string JournalTitle
        {
            get { return journalTitle; }
            private set { SetProperty(ref journalTitle, value); }
        }

        /// <summary>Tür adı (seçilen türden; notta bu ad yazılır).</summary>
        public string JournalKind
        {
            get { return journalKind; }
            set
            {
                if (SetProperty(ref journalKind, value ?? string.Empty))
                {
                    OnPropertyChanged(nameof(JournalKindOption));
                }
            }
        }

        /// <summary>Seçili tür (listede ikon ve renkle gösterilir).</summary>
        public EventKindOption? JournalKindOption
        {
            get { return EventKinds.Find(journalKind); }
            set { JournalKind = value?.Label ?? string.Empty; }
        }

        /// <summary>Olayın günü (varsayılan bugün).</summary>
        public DateTime? JournalDate
        {
            get { return journalDate; }
            set { SetProperty(ref journalDate, value); }
        }

        private string journalDateText = string.Empty;
        private string journalTarget = string.Empty;

        /// <summary>Tarih kutusu: <c>12.11.2026</c> ya da <c>12.11.2026 14:30</c>.</summary>
        public string JournalDateText
        {
            get { return journalDateText; }
            set
            {
                if (SetProperty(ref journalDateText, value ?? string.Empty))
                {
                    JournalDate = ParseDate(journalDateText);
                    OnPropertyChanged(nameof(JournalPrimaryText));
                }
            }
        }

        /// <summary>Pencerenin alt başlığı: hangi kaydın günlüğüne yazıldığı ("AB-7000-0002 · TAKIM ANTEN BOYNUZ Y günlüğüne").</summary>
        public string JournalTarget
        {
            get { return journalTarget; }
            private set { SetProperty(ref journalTarget, value); }
        }

        private static DateTime? ParseDate(string text)
        {
            DateTime value;
            var formats = new[] { "d.M.yyyy", "dd.MM.yyyy", "d.M.yyyy H:mm", "dd.MM.yyyy HH:mm", "yyyy-MM-dd", "yyyy-MM-dd HH:mm" };
            return DateTime.TryParseExact((text ?? string.Empty).Trim(), formats, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out value) ? value : (DateTime?)null;
        }

        /// <summary>Başlık (zorunlu).</summary>
        public string JournalHeading
        {
            get { return journalHeading; }
            set
            {
                if (SetProperty(ref journalHeading, value ?? string.Empty))
                {
                    OnPropertyChanged(nameof(JournalPrimaryText));
                }
            }
        }

        /// <summary>Açıklama.</summary>
        public string JournalBody
        {
            get { return journalBody; }
            set { SetProperty(ref journalBody, value ?? string.Empty); }
        }

        /// <summary>Önem (Yüksek / Normal / Düşük).</summary>
        public string JournalImportance
        {
            get { return journalImportance; }
            set { SetProperty(ref journalImportance, value ?? string.Empty); }
        }

        /// <summary>Etiketler (virgül ya da boşlukla).</summary>
        public string JournalTags
        {
            get { return journalTags; }
            set { SetProperty(ref journalTags, value ?? string.Empty); }
        }

        /// <summary>Ekle düğmesinin yazısı: başlık yazılmadan boş (düğme görünmez).</summary>
        public string JournalPrimaryText
        {
            get { return journalHeading.Trim().Length > 0 && JournalDate.HasValue ? (editing != null ? Resources.Edit_Save : Resources.Journal_Add) : string.Empty; }
        }

        private EventNote? editing;
        private EventNote? deleting;
        private bool isJournalDeleteOpen;
        private string journalDeleteText = string.Empty;

        /// <summary>Günlük olayını silme onayı açık mı.</summary>
        public bool IsJournalDeleteOpen
        {
            get { return isJournalDeleteOpen; }
            set { SetDialogOpen(ref isJournalDeleteOpen, value); }
        }

        /// <summary>Silme onayının metni.</summary>
        public string JournalDeleteText
        {
            get { return journalDeleteText; }
            private set { SetProperty(ref journalDeleteText, value); }
        }

        /// <summary>Günlükteki olayı aynı pencerede, dolu olarak açar.</summary>
        public void OpenJournalEdit(EventNote note)
        {
            if (!OpenJournalHere())
            {
                return;
            }

            editing = note;
            journalNotePath = note.Path;
            JournalTitle = Resources.Journal_EditTitle;
            JournalKind = note.KindLabel;
            JournalDateText = note.Date.ToString(note.Date.TimeOfDay == TimeSpan.Zero ? "dd.MM.yyyy" : "dd.MM.yyyy HH:mm", System.Globalization.CultureInfo.InvariantCulture);
            // Kutudaki metin öncekiyle aynıysa ayarlayıcı tarihi yeniden okumaz; tarih her açılışta metinden kurulur.
            JournalDate = ParseDate(JournalDateText);
            JournalHeading = note.Title;
            JournalBody = note.Body;
            JournalImportance = note.Importance.Length > 0 ? note.Importance : Resources.Importance_Normal;
            JournalTags = Pusula.Core.Markdown.MarkdownEditing.FormatTags(note.Tags.ToList());
            OnPropertyChanged(nameof(JournalPrimaryText));
        }

        /// <summary>Günlükteki olayı silme onayını açar.</summary>
        public void OpenJournalDelete(EventNote note)
        {
            deleting = note;
            journalFolder = Inventory.Item?.Folder ?? (selectedNode?.Path ?? string.Empty);
            JournalDeleteText = string.Format(Resources.Journal_DeleteText, note.Title, note.Date.ToString(Resources.Format_Date), Path.GetFileName(note.Path));
            IsJournalDeleteOpen = true;
        }

        /// <summary>Onaylanan olayı nottan kaldırır (yalnız o olayın satırları).</summary>
        public bool ConfirmJournalDelete()
        {
            IsJournalDeleteOpen = false;
            var note = deleting;
            deleting = null;
            if (note == null)
            {
                return false;
            }

            var error = Journal.Replace(note.Path, note.SourceLine, null);
            if (error.Length > 0)
            {
                Status = string.Format(Resources.Error_Io, error);
                return false;
            }

            Done(string.Format(Resources.Journal_Deleted, note.Title));
            RefreshShowing(journalFolder);
            return true;
        }

        /// <summary>Tür seçenekleri.</summary>
        public EventKindOption[] JournalKinds
        {
            get { return EventKinds.All; }
        }

        /// <summary>Önem seçenekleri.</summary>
        public string[] JournalImportances
        {
            get { return new[] { Resources.Importance_High, Resources.Importance_Normal, Resources.Importance_Low }; }
        }

        /// <summary>İlgili kayıt seçenekleri (stoka bağlı jig / kablolar ve seri numaraları).</summary>
        public ObservableCollection<RelatedOption> JournalRelated { get; } = new ObservableCollection<RelatedOption>();

        /// <summary>İlgili seçeneği var mı.</summary>
        public bool HasJournalRelated
        {
            get { return JournalRelated.Count > 0; }
        }

        /// <summary>Gösterilen sayfa (stok ya da envanter kaydı) için olay penceresini açar.</summary>
        public bool OpenJournalHere()
        {
            var folder = Inventory.Item?.Folder ?? (selectedNode?.Path ?? string.Empty);
            return OpenJournal(folder);
        }

        /// <summary>Klasörü verilen kaydın ana notuna olay eklemek için pencereyi açar.</summary>
        public bool OpenJournal(string folder)
        {
            var kind = KindOf(folder);
            if (!kind.HasValue)
            {
                return false;
            }

            journalFolder = folder;
            editing = null;
            var name = Path.GetFileName(folder.TrimEnd('\\'));
            JournalRelated.Clear();
            switch (kind.Value)
            {
                case RecordKind.Inventory:
                {
                    var record = JsonFile.TryRead<InventoryRecord>(InventoryService.RecordFile(folder));
                    var number = record?.Number ?? name;
                    var found = NoteFile.Find(folder, number);
                    journalNotePath = found.Length > 0 ? found : RecordContents.NotePath(folder, number);
                    name = number;
                    break;
                }

                default:
                {
                    var found = NoteFile.Find(folder, name);
                    journalNotePath = found.Length > 0 ? found : Path.Combine(folder, name + ".md");
                    if (kind.Value == RecordKind.Stock && string.Equals(Detail.StockPath, folder, StringComparison.OrdinalIgnoreCase))
                    {
                        foreach (var row in Detail.InventoryGroups.SelectMany(g => g.Rows).Where(r => !r.IsMissing))
                        {
                            JournalRelated.Add(new RelatedOption(row.Number, row.Number + (row.Name.Length > 0 ? " · " + row.Name : string.Empty)));
                        }

                        foreach (var serial in Detail.MatrixRows.Select(r => r.Serial).Distinct())
                        {
                            JournalRelated.Add(new RelatedOption(serial, serial));
                        }
                    }

                    break;
                }
            }

            OnPropertyChanged(nameof(HasJournalRelated));
            JournalTitle = Resources.Journal_Title;
            var shownName = kind.Value == RecordKind.Inventory ? Inventory.Item?.Record.Name ?? string.Empty : Detail.HeaderName;
            JournalTarget = string.Format(Resources.Journal_Target, name + (shownName.Length > 0 ? " · " + shownName : string.Empty));
            JournalKind = string.Empty;
            JournalDateText = DateTime.Today.ToString("dd.MM.yyyy", System.Globalization.CultureInfo.InvariantCulture);
            // Kutudaki metin öncekiyle aynıysa ayarlayıcı tarihi yeniden okumaz; tarih her açılışta metinden kurulur.
            JournalDate = ParseDate(JournalDateText);
            JournalHeading = string.Empty;
            JournalBody = string.Empty;
            JournalImportance = Resources.Importance_Normal;
            JournalTags = string.Empty;
            IsJournalOpen = true;
            return true;
        }

        /// <summary>Olayı ana notun Günlük bölümüne ekler (en yeni en üstte) ve sayfayı yeniler. Eklendiyse <c>true</c>.</summary>
        public bool ConfirmJournal()
        {
            IsJournalOpen = false;
            if (JournalHeading.Trim().Length == 0)
            {
                Status = Resources.Journal_NoTitle;
                return false;
            }

            var entry = new JournalEntry
            {
                Date = JournalDate ?? DateTime.Today,
                Kind = JournalKind.Trim(),
                Title = JournalHeading.Trim(),
                Body = JournalBody,
                Importance = JournalImportance,
                Person = CurrentUser,
            };
            entry.Related.AddRange(JournalRelated.Where(r => r.IsChecked).Select(r => r.Number));
            entry.Tags.AddRange(JournalTags.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(t => t.TrimStart('#')).Where(t => t.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase));
            var edited = editing;
            editing = null;
            if (edited != null)
            {
                entry.Related.AddRange(edited.Related);
            }

            var error = edited != null ? Journal.Replace(edited.Path, edited.SourceLine, entry) : Journal.Add(journalNotePath, entry);
            if (error.Length > 0)
            {
                Status = string.Format(Resources.Error_Io, error);
                return false;
            }

            Done(string.Format(Resources.Journal_Added, Path.GetFileName(journalNotePath)));
            RefreshShowing(journalFolder);
            return true;
        }
    }
}
