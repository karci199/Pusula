using System;
using System.Linq;
using Pusula.Core.Notes;
using Pusula.Wpf.Properties;

namespace Pusula.Wpf.ViewModels
{
    /// <summary>
    /// Kayıt penceresinde karar alanları: işlem "Karar" seçilince karar veren (@) ve isteğe bağlı ilgili madde açılır.
    /// Madde listesi ölçümdekiyle aynıdır; karar maddenin durumunu değiştirmez, doğrulama satırında deneme listesinde görünür.
    /// </summary>
    public sealed partial class MainViewModel
    {
        private string journalDecidedBy = string.Empty;

        /// <summary>Karar alanları görünür mü (yapıldı kaydı ve işlem Karar).</summary>
        public bool IsJournalDecision
        {
            get { return JournalDone && IsDecisionKind(journalKind); }
        }

        /// <summary>Kararı verenler (<c>@Ayşe_Kaya</c>).</summary>
        public string JournalDecidedBy
        {
            get { return journalDecidedBy; }
            set { SetProperty(ref journalDecidedBy, value ?? string.Empty); }
        }

        /// <summary>İşlem Karar mı.</summary>
        public static bool IsDecisionKind(string kind)
        {
            var key = Journal.KindKey(kind ?? string.Empty);
            return key.Length > 0 && key == Journal.KindKey(Resources.Event_Decision);
        }

        // Pencere açılınca: düzenlemede kayıttan, yeni kayıtta boş.
        private void ResetJournalDecision(EventNote? note)
        {
            JournalDecidedBy = note != null ? string.Join(" ", note.DecidedBy.Select(p => "@" + p)) : string.Empty;
            OnDecisionChanged();
        }

        // Kayda karar alanları yazılır: karar veren ve (seçildiyse) madde; sonuç yazılmaz.
        private void ApplyJournalDecision(JournalEntry entry)
        {
            if (!IsJournalDecision)
            {
                return;
            }

            entry.DecidedBy.AddRange(journalDecidedBy.Split(new[] { ' ', ',', ';', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim().TrimStart('@', '#')).Where(p => p.Length > 0).Distinct(StringComparer.CurrentCultureIgnoreCase));
            entry.Item = journalItem == NewItemKey ? string.Empty : journalItem;
            entry.Result = string.Empty;
        }

        private void OnDecisionChanged()
        {
            OnPropertyChanged(nameof(IsJournalDecision));
        }
    }
}
