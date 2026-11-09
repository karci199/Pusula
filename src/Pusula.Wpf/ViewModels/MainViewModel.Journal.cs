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

        /// <summary>Tür (listeden ya da yazılarak).</summary>
        public string JournalKind
        {
            get { return journalKind; }
            set { SetProperty(ref journalKind, value ?? string.Empty); }
        }

        /// <summary>Olayın günü (varsayılan bugün).</summary>
        public DateTime? JournalDate
        {
            get { return journalDate; }
            set { SetProperty(ref journalDate, value); }
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
            get { return journalHeading.Trim().Length > 0 ? Resources.Journal_Add : string.Empty; }
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
            JournalTitle = string.Format(Resources.Journal_Title, name);
            JournalKind = string.Empty;
            JournalDate = DateTime.Today;
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
            entry.Tags.AddRange(JournalTags.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries));
            var error = Journal.Add(journalNotePath, entry);
            if (error.Length > 0)
            {
                Status = string.Format(Resources.Error_Io, error);
                return false;
            }

            Status = string.Format(Resources.Journal_Added, Path.GetFileName(journalNotePath));
            RefreshShowing(journalFolder);
            return true;
        }
    }
}
