using System;
using System.Collections.Generic;
using System.Linq;
using Pusula.Core.Notes;
using Pusula.Wpf.Properties;

namespace Pusula.Wpf.ViewModels
{
    /// <summary>
    /// Kayıt penceresinde toplantı alanları: işlem "Toplantı" seçilince katılımcılar (@), ortam (Yüz yüze, Teams, Skype,
    /// Zoom, Telefon) ve kararlar açılır; açıklama "Notlar" olur. Kararlar notta <c>#### Kararlar</c> başlığının altına yazılır.
    /// </summary>
    public sealed partial class MainViewModel
    {
        private string journalParticipants = string.Empty;
        private string journalSetting = string.Empty;
        private string journalDecisions = string.Empty;

        /// <summary>Toplantı alanları görünür mü (yapıldı kaydı ve işlem Toplantı).</summary>
        public bool IsJournalMeeting
        {
            get { return JournalDone && IsMeetingKind(journalKind); }
        }

        /// <summary>Açıklama alanının adı: toplantıda "Notlar", diğerlerinde "Açıklama".</summary>
        public string JournalBodyLabel
        {
            get { return IsJournalMeeting ? Resources.Journal_Notes : Resources.Journal_Body; }
        }

        /// <summary>Katılımcılar (<c>@katan @Ali_Demirci</c>); açılışta kullanıcının kendisi.</summary>
        public string JournalParticipants
        {
            get { return journalParticipants; }
            set { SetProperty(ref journalParticipants, value ?? string.Empty); }
        }

        /// <summary>Seçili ortam (boş = seçilmedi).</summary>
        public string JournalSetting
        {
            get
            {
                return journalSetting;
            }

            set
            {
                if (SetProperty(ref journalSetting, value ?? string.Empty))
                {
                    OnPropertyChanged(nameof(JournalSettingChoices));
                    OnMailChanged();
                }
            }
        }

        /// <summary>Ortam seçenekleri (ikonlu kutular); seçili olan işaretli.</summary>
        public IList<ChoiceOption> JournalSettingChoices
        {
            get { return MeetingSettings.Select(s => new ChoiceOption(s, s, string.Equals(s, journalSetting, StringComparison.CurrentCultureIgnoreCase), o => JournalSetting = o.Key) { Badge = SettingIconKey(s) }).ToList(); }
        }

        /// <summary>Kararlar (serbest metin).</summary>
        public string JournalDecisions
        {
            get { return journalDecisions; }
            set { SetProperty(ref journalDecisions, value ?? string.Empty); }
        }

        /// <summary>Ortam adları sırasıyla: Yüz yüze, Teams, Skype, Zoom, Telefon, E-posta.</summary>
        public static string[] MeetingSettings
        {
            get { return new[] { Resources.Setting_FaceToFace, Resources.Setting_Teams, Resources.Setting_Skype, Resources.Setting_Zoom, Resources.Setting_Phone, Resources.Setting_Email }; }
        }

        /// <summary>Ortamın ikon anahtarı: <c>face</c>, <c>teams</c>, <c>skype</c>, <c>zoom</c>, <c>phone</c>; tanınmazsa boş.</summary>
        public static string SettingIconKey(string setting)
        {
            var keys = new[] { "face", "teams", "skype", "zoom", "phone", "mail" };
            var names = MeetingSettings;
            for (var i = 0; i < names.Length; i++)
            {
                if (string.Equals((setting ?? string.Empty).Trim(), names[i], StringComparison.CurrentCultureIgnoreCase))
                {
                    return keys[i];
                }
            }

            return string.Empty;
        }

        /// <summary>İşlem Toplantı mı.</summary>
        public static bool IsMeetingKind(string kind)
        {
            var key = Journal.KindKey(kind ?? string.Empty);
            return key.Length > 0 && key == Journal.KindKey(Resources.Event_Meeting);
        }

        // Pencere açılınca: yeni kayıtta katılımcı kullanıcının kendisi; düzenlemede kayıttan.
        private void ResetJournalMeeting(EventNote? note)
        {
            JournalParticipants = note != null
                ? string.Join(" ", note.Participants.Select(p => "@" + p))
                : (CurrentUser.Length > 0 ? "@" + CurrentUser.Replace(' ', '_') : string.Empty);
            JournalSetting = note?.Setting ?? string.Empty;
            JournalDecisions = note?.Decisions ?? string.Empty;
            OnMeetingChanged();
        }

        // Kayda toplantı alanları yazılır (toplantı değilse boş kalır).
        private void ApplyJournalMeeting(JournalEntry entry)
        {
            if (!IsJournalMeeting)
            {
                return;
            }

            entry.Participants.AddRange(journalParticipants.Split(new[] { ' ', ',', ';', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim().TrimStart('@', '#')).Where(p => p.Length > 0).Distinct(StringComparer.CurrentCultureIgnoreCase));
            entry.Setting = journalSetting.Trim();
            entry.Decisions = journalDecisions.Trim();
        }

        private void OnMeetingChanged()
        {
            OnPropertyChanged(nameof(IsJournalMeeting));
            OnPropertyChanged(nameof(JournalBodyLabel));
            OnMailChanged();
        }
    }
}
