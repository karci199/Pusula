using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Pusula.Core.Notes;
using Pusula.Wpf.Properties;

namespace Pusula.Wpf.ViewModels
{
    /// <summary>
    /// Kayıt penceresinde revizyon alanları: işlem "Revizyon" seçilince stok no (varsayılan bu kayıt), yeni rev (zorunlu)
    /// ve değişiklik no (isteğe bağlı) açılır. Önceki rev aynı stokun bir önceki revizyon kaydından bulunur; notta
    /// <c>stok: AB-7000-0001 · rev: A → B · değişiklik no: ECO-12</c> olarak yazılır.
    /// </summary>
    public sealed partial class MainViewModel
    {
        private string journalRevisionStock = string.Empty;
        private string journalRevisionNew = string.Empty;
        private string journalRevisionChangeNo = string.Empty;
        private string journalRevisionOld = string.Empty;
        private EventNote? journalRevisionEditing;

        /// <summary>Revizyon alanları görünür mü (yapıldı kaydı ve işlem Revizyon).</summary>
        public bool IsJournalRevision
        {
            get { return JournalDone && IsRevisionKind(journalKind); }
        }

        /// <summary>Revizyona giren stok numarası (varsayılan gösterilen kayıt).</summary>
        public string JournalRevisionStock
        {
            get
            {
                return journalRevisionStock;
            }

            set
            {
                if (SetProperty(ref journalRevisionStock, value ?? string.Empty))
                {
                    FindPreviousRevision();
                }
            }
        }

        /// <summary>Yeni revizyon (zorunlu).</summary>
        public string JournalRevisionNew
        {
            get
            {
                return journalRevisionNew;
            }

            set
            {
                if (SetProperty(ref journalRevisionNew, value ?? string.Empty))
                {
                    OnPropertyChanged(nameof(JournalPrimaryText));
                }
            }
        }

        /// <summary>Değişiklik numarası (ECR/ECO, iş emri); isteğe bağlı.</summary>
        public string JournalRevisionChangeNo
        {
            get { return journalRevisionChangeNo; }
            set { SetProperty(ref journalRevisionChangeNo, value ?? string.Empty); }
        }

        /// <summary>Yeni rev'in altındaki bilgi: "Önceki: A" ya da "İlk revizyon kaydı".</summary>
        public string JournalRevisionHint
        {
            get { return journalRevisionOld.Length > 0 ? string.Format(CultureInfo.CurrentCulture, Resources.Revision_Previous, journalRevisionOld) : Resources.Revision_First; }
        }

        /// <summary>İşlem Revizyon mu.</summary>
        public static bool IsRevisionKind(string kind)
        {
            var key = Journal.KindKey(kind ?? string.Empty);
            return key.Length > 0 && key == Journal.KindKey(Resources.Event_Revision);
        }

        // Revizyon seçiliyse yeni rev yazılmış olmalı.
        private bool JournalRevisionReady
        {
            get { return !IsJournalRevision || journalRevisionNew.Trim().Length > 0; }
        }

        // Pencere açılınca: düzenlemede kayıttan; yeni kayıtta stok no gösterilen kaydın adı.
        private void ResetJournalRevision(EventNote? note)
        {
            journalRevisionEditing = note;
            journalRevisionStock = note != null && note.RevisionNew.Length > 0 ? note.RevisionStock : Path.GetFileName(journalFolder.TrimEnd('\\'));
            JournalRevisionNew = note?.RevisionNew ?? string.Empty;
            JournalRevisionChangeNo = note?.RevisionChangeNo ?? string.Empty;
            if (note != null && note.RevisionNew.Length > 0)
            {
                journalRevisionOld = note.RevisionOld;
                OnPropertyChanged(nameof(JournalRevisionHint));
            }
            else
            {
                FindPreviousRevision();
            }

            OnPropertyChanged(nameof(JournalRevisionStock));
            OnRevisionChanged();
        }

        // Aynı notta aynı stokun en son revizyon kaydının yeni rev'i (düzenlenen kayıt hariç).
        private void FindPreviousRevision()
        {
            if (journalRevisionEditing != null && journalRevisionEditing.RevisionNew.Length > 0)
            {
                return;
            }

            var stock = journalRevisionStock.Trim();
            var previous = journalNotePath.Length == 0 || stock.Length == 0 ? null
                : Journal.Read(journalNotePath).Where(e => e.RevisionNew.Length > 0 && string.Equals(e.RevisionStock, stock, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(e => e.Date).FirstOrDefault();
            journalRevisionOld = previous?.RevisionNew ?? string.Empty;
            OnPropertyChanged(nameof(JournalRevisionHint));
        }

        // Kayda revizyon alanları yazılır.
        private void ApplyJournalRevision(JournalEntry entry)
        {
            if (!IsJournalRevision)
            {
                return;
            }

            entry.RevisionStock = journalRevisionStock.Trim();
            entry.RevisionNew = journalRevisionNew.Trim();
            entry.RevisionOld = journalRevisionOld.Trim();
            entry.RevisionChangeNo = journalRevisionChangeNo.Trim();
        }

        private void OnRevisionChanged()
        {
            OnPropertyChanged(nameof(IsJournalRevision));
            OnPropertyChanged(nameof(JournalPrimaryText));
        }
    }
}
