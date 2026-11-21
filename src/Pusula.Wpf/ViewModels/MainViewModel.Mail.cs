using System;
using System.Globalization;
using System.IO;
using Pusula.Core.Notes;
using Pusula.Wpf.Properties;

namespace Pusula.Wpf.ViewModels
{
    /// <summary>
    /// Toplantı kaydına mail (.msg): ortam Outlook iken mail dosyası seçilir; kayıtla birlikte kaydın klasöründeki
    /// <c>Diger\</c> klasörüne <c>tarih_başlık.msg</c> adıyla <b>kopyalanır</b> (asıl dosyaya dokunulmaz, üzerine yazılmaz)
    /// ve notta <c>mail: Diger/….msg</c> olarak tutulur. Pusula maili okumaz; kartta ✉ ile Outlook'ta açılır.
    /// </summary>
    public sealed partial class MainViewModel
    {
        /// <summary>Mail dosyalarının konduğu klasörün adı (kayıt klasörünün en dışında).</summary>
        public const string MailFolderName = "Diger";

        private string journalMailSource = string.Empty;
        private string journalMailRelative = string.Empty;

        /// <summary>Mail alanı görünür mü (toplantı ve ortam Outlook).</summary>
        public bool IsJournalMailVisible
        {
            get { return IsJournalMeeting && string.Equals(journalSetting, Resources.Setting_Email, StringComparison.CurrentCultureIgnoreCase); }
        }

        /// <summary>Seçilen (ya da kayıtlı) mail dosyasının adı; yoksa "Seçilmedi".</summary>
        public string JournalMailText
        {
            get
            {
                if (journalMailSource.Length > 0)
                {
                    return Path.GetFileName(journalMailSource);
                }

                return journalMailRelative.Length > 0 ? Path.GetFileName(journalMailRelative.Replace('/', '\\')) : Resources.Journal_MailNone;
            }
        }

        /// <summary>Mail dosyası seçildi ya da kayıtlı mı.</summary>
        public bool HasJournalMail
        {
            get { return journalMailSource.Length > 0 || journalMailRelative.Length > 0; }
        }

        /// <summary>Mail dosyası seçer (yalnız .msg); dosya Ekle'de kopyalanır. Kabul edildiyse <c>true</c>.</summary>
        public bool SetJournalMail(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path) || !string.Equals(Path.GetExtension(path), ".msg", StringComparison.OrdinalIgnoreCase))
            {
                Status = Resources.Journal_MailOnlyMsg;
                return false;
            }

            journalMailSource = path;
            OnMailChanged();
            return true;
        }

        /// <summary>Seçilen maili kaldırır (dosyaya dokunulmaz).</summary>
        public void ClearJournalMail()
        {
            journalMailSource = string.Empty;
            journalMailRelative = string.Empty;
            OnMailChanged();
        }

        /// <summary>Kayda bağlı maili varsayılan programda (Outlook) açar.</summary>
        public void OpenMail(EventNote note)
        {
            if (note == null || note.Mail.Length == 0)
            {
                return;
            }

            var path = MeasurementFiles.Resolve(Path.GetDirectoryName(note.Path) ?? string.Empty, note.Mail);
            if (!File.Exists(path))
            {
                Status = string.Format(CultureInfo.CurrentCulture, Resources.Journal_MailMissing, Path.GetFileName(path));
                return;
            }

            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                Status = string.Format(Resources.Error_Io, ex.Message);
            }
        }

        // Pencere açılınca: düzenlemede kaydın maili korunur.
        private void ResetJournalMail(EventNote? note)
        {
            journalMailSource = string.Empty;
            journalMailRelative = note?.Mail ?? string.Empty;
            OnMailChanged();
        }

        // Kayıt yazılmadan önce: seçilen mail Diger klasörüne kopyalanır, yolu kayda yazılır. Hata olursa false.
        private bool CopyJournalMail(JournalEntry entry)
        {
            entry.Mail = journalMailRelative;
            if (!IsJournalMailVisible || journalMailSource.Length == 0)
            {
                return true;
            }

            var record = Path.GetDirectoryName(journalNotePath) ?? journalFolder;
            try
            {
                var folder = Path.Combine(record, MailFolderName);
                Directory.CreateDirectory(folder);
                var name = MeasurementFiles.NameFor(entry.Date, string.Empty, entry.Title);
                var target = Path.Combine(folder, name + ".msg");
                for (var n = 2; File.Exists(target); n++)
                {
                    target = Path.Combine(folder, name + "_" + n.ToString(CultureInfo.InvariantCulture) + ".msg");
                }

                File.Copy(journalMailSource, target, false);
                entry.Mail = MeasurementFiles.Relative(record, target);
                journalMailSource = string.Empty;
                return true;
            }
            catch (IOException ex)
            {
                Status = string.Format(Resources.Error_Io, ex.Message);
                return false;
            }
            catch (UnauthorizedAccessException ex)
            {
                Status = string.Format(Resources.Error_Io, ex.Message);
                return false;
            }
        }

        private void OnMailChanged()
        {
            OnPropertyChanged(nameof(IsJournalMailVisible));
            OnPropertyChanged(nameof(JournalMailText));
            OnPropertyChanged(nameof(HasJournalMail));
        }
    }
}
