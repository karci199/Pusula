using System;
using System.Globalization;
using System.IO;
using Pusula.Core.Notes;
using Pusula.Core.Tracking;
using Pusula.Wpf.Properties;

namespace Pusula.Wpf.ViewModels
{
    /// <summary>
    /// Ölçüm klasörü: kayıt eklenirken (ya da sonradan kartın menüsünden) kaydın boş klasörü açılır, nota
    /// <c>klasör:</c> olarak yazılır ve Gezgini'nde açılır; dosyaları kullanıcı kendisi koyar (bkz. <see cref="MeasurementFiles"/>).
    /// </summary>
    public sealed partial class MainViewModel
    {
        private string journalFolderRelative = string.Empty;
        private bool journalCreateFolder = true;

        /// <summary>Oluşturulan klasör Gezgini'nde açılsın mı (testlerde kapatılır).</summary>
        public bool OpenCreatedFolders { get; set; } = true;

        /// <summary>Pencerede: kayıtla birlikte ölçüm klasörü oluşturulup açılsın mı (varsayılan evet).</summary>
        public bool JournalCreateFolder
        {
            get { return journalCreateFolder; }
            set { SetProperty(ref journalCreateFolder, value); }
        }

        /// <summary>Düzenlenen kaydın zaten klasörü var mı (kutu yerine yolu gösterilir).</summary>
        public bool HasJournalFolder
        {
            get { return journalFolderRelative.Length > 0; }
        }

        /// <summary>Kutunun altındaki yol: açılacak (ya da var olan) klasör, kayıt klasörüne göre.</summary>
        public string JournalFolderText
        {
            get
            {
                var relative = journalFolderRelative.Length > 0 ? journalFolderRelative.Replace('/', '\\') : TargetRelative();
                return string.Format(CultureInfo.CurrentCulture, Resources.Journal_FolderHint, relative + "\\");
            }
        }

        /// <summary>
        /// Var olan kayda ölçüm klasörü açar (yoksa) ve Gezgini'nde gösterir; klasör Günlük'te kayda yazılır.
        /// Açıldıysa ya da zaten varsa <c>true</c>.
        /// </summary>
        public bool CreateEntryFolder(EventNote note)
        {
            if (note == null || !note.IsJournal)
            {
                return false;
            }

            var record = Path.GetDirectoryName(note.Path) ?? string.Empty;
            if (note.Folder.Length > 0)
            {
                var existing = MeasurementFiles.Resolve(record, note.Folder);
                Directory.CreateDirectory(existing);
                ShowFolder(existing);
                return true;
            }

            string error;
            var path = MeasurementFiles.Create(MeasurementFiles.BaseFolder(record, KindOf(record) == RecordKind.Stock), MeasurementFiles.NameFor(note.Date, note.Item, note.Title), out error);
            if (error.Length == 0)
            {
                var entry = Journal.EntryOf(note);
                entry.Folder = MeasurementFiles.Relative(record, path);
                error = Journal.Replace(note.Path, note.SourceLine, entry);
            }

            if (error.Length > 0)
            {
                Status = string.Format(Resources.Error_Io, error);
                return false;
            }

            Done(string.Format(CultureInfo.CurrentCulture, Resources.Journal_FolderCreated, Path.GetFileName(path)), path);
            RefreshShowing(record);
            ShowFolder(path);
            return true;
        }

        // Pencere açılınca: düzenlemede kaydın klasörü korunur; yeni kayıtta kutu işaretli.
        private void ResetJournalFiles(EventNote? note)
        {
            journalFolderRelative = note?.Folder ?? string.Empty;
            JournalCreateFolder = true;
            OnPropertyChanged(nameof(HasJournalFolder));
            OnPropertyChanged(nameof(JournalFolderText));
        }

        // Kayıt yazılmadan önce: istendiyse boş ölçüm klasörü açılır ve kayda yazılır. Hata olursa false.
        private bool CreateJournalFolder(JournalEntry entry)
        {
            entry.Folder = journalFolderRelative;
            if (!IsJournalMeasurement || journalFolderRelative.Length > 0 || !journalCreateFolder)
            {
                return true;
            }

            var record = Path.GetDirectoryName(journalNotePath) ?? journalFolder;
            string error;
            var path = MeasurementFiles.Create(MeasurementFiles.BaseFolder(record, KindOf(journalFolder) == RecordKind.Stock), MeasurementFiles.NameFor(entry.Date, entry.Item, entry.Title), out error);
            if (error.Length > 0)
            {
                Status = string.Format(Resources.Error_Io, error);
                return false;
            }

            entry.Folder = MeasurementFiles.Relative(record, path);
            ShowFolder(path);
            return true;
        }

        private void ShowFolder(string path)
        {
            if (OpenCreatedFolders)
            {
                OpenPath(path);
            }
        }

        // Açılacak klasörün önizlemesi (göreli): tarih · madde · başlık.
        private string TargetRelative()
        {
            var record = journalNotePath.Length > 0 ? Path.GetDirectoryName(journalNotePath) ?? journalFolder : journalFolder;
            var item = journalItem == NewItemKey ? journalNewItemNo : journalItem;
            var name = MeasurementFiles.NameFor(JournalDate ?? DateTime.Today, item, JournalHeading);
            var baseFolder = MeasurementFiles.BaseFolder(record, KindOf(journalFolder) == RecordKind.Stock);
            return MeasurementFiles.Relative(record, Path.Combine(baseFolder, name)).Replace('/', '\\');
        }
    }
}
