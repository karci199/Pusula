using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using Pusula.Core.Notes;
using Pusula.Wpf.Mvvm;
using Pusula.Wpf.Properties;

namespace Pusula.Wpf.ViewModels
{
    /// <summary>Malzeme talebinde düzenlenebilir bir kalem satırı.</summary>
    public sealed class RequestItemRow : ViewModelBase
    {
        private string stockNo = string.Empty;
        private string name = string.Empty;
        private string quantity = string.Empty;

        /// <summary>Stok numarası (isteğe bağlı).</summary>
        public string StockNo
        {
            get { return stockNo; }
            set { SetProperty(ref stockNo, value ?? string.Empty); }
        }

        /// <summary>Tanım.</summary>
        public string Name
        {
            get { return name; }
            set { SetProperty(ref name, value ?? string.Empty); }
        }

        /// <summary>Adet.</summary>
        public string Quantity
        {
            get { return quantity; }
            set { SetProperty(ref quantity, value ?? string.Empty); }
        }

        /// <summary>Boş mu (hiçbir hücre yazılmamış).</summary>
        public bool IsEmpty
        {
            get { return (stockNo + name + quantity).Trim().Length == 0; }
        }
    }

    /// <summary>
    /// Kayıt penceresinde talep alanları: işlem "Talep" seçilince talep türü (Mekanik, Kablaj, Cihaz, Malzeme, Test altyapı,
    /// Test destek, Diğer), talep no, kimden, istenen tarih ve türe göre ek alan (ilgili kayıt, kalemler, istenen) açılır.
    /// Talep açık başlar; kartta "Teslim alındı" ile kapanır ve çizelgeye bağlı bir teslim kaydı düşer.
    /// </summary>
    public sealed partial class MainViewModel
    {
        private string journalRequestType = string.Empty;
        private string journalRequestNo = string.Empty;
        private string journalRequestFrom = string.Empty;
        private string journalRequestDueText = string.Empty;
        private string journalRequestRecord = string.Empty;
        private string journalRequestDetail = string.Empty;
        private string journalRequestStatus = string.Empty;

        /// <summary>Talep türü anahtarları (ikon için) sırasıyla.</summary>
        public static readonly string[] RequestTypeKeys = { "mechanic", "cable", "device", "material", "infra", "support", "other" };

        /// <summary>Talep türü adları sırasıyla: Mekanik, Kablaj, Cihaz, Malzeme, Test altyapı, Test destek, Diğer.</summary>
        public static string[] RequestTypes
        {
            get
            {
                return new[]
                {
                    Resources.Request_Mechanic, Resources.Request_Cable, Resources.Request_Device, Resources.Request_Material,
                    Resources.Request_Infra, Resources.Request_Support, Resources.Request_Other,
                };
            }
        }

        /// <summary>Stok sayfası · Yapılacaklar: açık ya da gecikmiş talepler (istenen tarihe göre).</summary>
        public ObservableCollection<StockTimelineCard> OpenRequests { get; } = new ObservableCollection<StockTimelineCard>();

        /// <summary>Bekleyen talep var mı.</summary>
        public bool HasOpenRequests
        {
            get { return OpenRequests.Count > 0; }
        }

        /// <summary>Talep alanları görünür mü (yapıldı kaydı ve işlem Talep).</summary>
        public bool IsJournalRequest
        {
            get { return JournalDone && IsRequestKind(journalKind); }
        }

        /// <summary>Seçili talep türü.</summary>
        public string JournalRequestType
        {
            get
            {
                return journalRequestType;
            }

            set
            {
                if (SetProperty(ref journalRequestType, value ?? string.Empty))
                {
                    OnRequestChanged();
                }
            }
        }

        /// <summary>Talep türü kutuları (ikonlu); rozet = ikon anahtarı.</summary>
        public IList<ChoiceOption> JournalRequestTypeChoices
        {
            get { return RequestTypes.Select(t => new ChoiceOption(t, t, string.Equals(t, journalRequestType, StringComparison.CurrentCultureIgnoreCase), o => JournalRequestType = o.Key) { Badge = RequestIconKey(t) }).ToList(); }
        }

        /// <summary>Talep numarası (isteğe bağlı).</summary>
        public string JournalRequestNo
        {
            get { return journalRequestNo; }
            set { SetProperty(ref journalRequestNo, value ?? string.Empty); }
        }

        /// <summary>Talebin istendiği yer ya da kişiler (<c>@Ambar</c>).</summary>
        public string JournalRequestFrom
        {
            get { return journalRequestFrom; }
            set { SetProperty(ref journalRequestFrom, value ?? string.Empty); }
        }

        /// <summary>İstenen tarih (gg.aa.yyyy, boş olabilir).</summary>
        public string JournalRequestDueText
        {
            get
            {
                return journalRequestDueText;
            }

            set
            {
                if (SetProperty(ref journalRequestDueText, value ?? string.Empty))
                {
                    OnPropertyChanged(nameof(JournalRequestDueChoices));
                    OnPropertyChanged(nameof(JournalPrimaryText));
                }
            }
        }

        /// <summary>İstenen tarih kısayolları: Bugün · Yarın · Bu hafta · Gelecek hafta.</summary>
        public IList<ChoiceOption> JournalRequestDueChoices
        {
            get
            {
                var today = DateTime.Today;
                var friday = today.AddDays(((int)DayOfWeek.Friday - (int)today.DayOfWeek + 7) % 7);
                var choices = new[]
                {
                    Tuple.Create(Resources.Task_DueToday, today), Tuple.Create(Resources.Task_DueTomorrow, today.AddDays(1)),
                    Tuple.Create(Resources.Task_DueWeek, friday), Tuple.Create(Resources.Task_DueNext, today.AddDays(7)),
                };
                var current = ParseDay(journalRequestDueText);
                var matched = false;
                var result = new List<ChoiceOption>();
                foreach (var c in choices)
                {
                    var on = !matched && current.HasValue && current.Value == c.Item2;
                    matched = matched || on;
                    result.Add(new ChoiceOption(c.Item2.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture), c.Item1, on, o => JournalRequestDueText = o.Key));
                }

                return result;
            }
        }

        /// <summary>İlgili kayıt (JIG-…, KBL-…, cihaz); Mekanik, Kablaj, Cihaz talebinde.</summary>
        public string JournalRequestRecord
        {
            get { return journalRequestRecord; }
            set { SetProperty(ref journalRequestRecord, value ?? string.Empty); }
        }

        /// <summary>İstenenin anlatımı; Test altyapı ve Test destek talebinde.</summary>
        public string JournalRequestDetail
        {
            get { return journalRequestDetail; }
            set { SetProperty(ref journalRequestDetail, value ?? string.Empty); }
        }

        /// <summary>Malzeme kalemleri (son satır her zaman boş, yazınca yenisi eklenir).</summary>
        public ObservableCollection<RequestItemRow> JournalRequestItems { get; } = new ObservableCollection<RequestItemRow>();

        /// <summary>Türe göre ek alan: ilgili kayıt mı (Mekanik, Kablaj, Cihaz).</summary>
        public bool ShowsRequestRecord
        {
            get { return IsJournalRequest && RequestTypeIndex() >= 0 && RequestTypeIndex() <= 2; }
        }

        /// <summary>Türe göre ek alan: kalemler mi (Malzeme).</summary>
        public bool ShowsRequestItems
        {
            get { return IsJournalRequest && RequestTypeIndex() == 3; }
        }

        /// <summary>Türe göre ek alan: istenen anlatımı mı (Test altyapı, Test destek).</summary>
        public bool ShowsRequestDetail
        {
            get { return IsJournalRequest && (RequestTypeIndex() == 4 || RequestTypeIndex() == 5); }
        }

        /// <summary>İstenen anlatımının ipucu (test altyapıda testler, destekte kişi ve günler).</summary>
        public string JournalRequestDetailHint
        {
            get { return RequestTypeIndex() == 5 ? Resources.Request_SupportHint : Resources.Request_InfraHint; }
        }

        /// <summary>İşlem Talep mi.</summary>
        public static bool IsRequestKind(string kind)
        {
            var key = Journal.KindKey(kind ?? string.Empty);
            return key.Length > 0 && key == Journal.KindKey(Resources.Event_Request);
        }

        /// <summary>Talep türünün ikon anahtarı (<c>mechanic</c>, <c>material</c> …); tanınmazsa <c>other</c>.</summary>
        public static string RequestIconKey(string type)
        {
            var names = RequestTypes;
            for (var i = 0; i < names.Length; i++)
            {
                if (string.Equals((type ?? string.Empty).Trim(), names[i], StringComparison.CurrentCultureIgnoreCase))
                {
                    return RequestTypeKeys[i];
                }
            }

            return "other";
        }

        /// <summary>Kalem satırı ekler (son satır boş değilse).</summary>
        public void AddRequestItemRow()
        {
            if (JournalRequestItems.Count == 0 || !JournalRequestItems[JournalRequestItems.Count - 1].IsEmpty)
            {
                JournalRequestItems.Add(new RequestItemRow());
            }
        }

        /// <summary>Kalem satırını çıkarır (en az bir boş satır kalır).</summary>
        public void RemoveRequestItemRow(RequestItemRow row)
        {
            JournalRequestItems.Remove(row);
            AddRequestItemRow();
        }

        /// <summary>
        /// Açık talebi kapatır: talebin durumu "Teslim alındı" olur ve Günlük'e bağlı bir teslim kaydı eklenir
        /// ("Teslim alındı · başlık", talep no ve kimden aynen). Yazıldıysa <c>true</c>.
        /// </summary>
        public bool MarkRequestDelivered(EventNote note)
        {
            if (note == null || !note.IsJournal || note.RequestType.Length == 0 || IsRequestClosed(note.RequestStatus))
            {
                return false;
            }

            var entry = Journal.EntryOf(note);
            entry.RequestStatus = Resources.Request_StatusDelivered;
            var error = Journal.Replace(note.Path, note.SourceLine, note.SourceOccurrence, entry);
            if (error.Length == 0)
            {
                var now = DateTime.Now;
                var delivered = new JournalEntry
                {
                    Date = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0),
                    Kind = Resources.Event_Delivery,
                    Title = note.Title,
                    Person = CurrentUser,
                    Body = string.Format(CultureInfo.CurrentCulture, Resources.Request_DeliveredBody, note.RequestType, note.Date.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)),
                };
                delivered.Related.AddRange(note.Related);
                delivered.Tags.AddRange(note.Tags);
                if (note.RequestNo.Length > 0)
                {
                    delivered.Body += " · " + note.RequestNo;
                }

                error = Journal.Add(note.Path, delivered);
            }

            if (error.Length > 0)
            {
                Status = string.Format(Resources.Error_Io, error);
                return false;
            }

            Done(string.Format(CultureInfo.CurrentCulture, Resources.Request_Delivered, note.Title), note.Path);
            RefreshShowing(Path.GetDirectoryName(note.Path) ?? string.Empty);
            return true;
        }

        /// <summary>Durum kapalı mı (Teslim alındı ya da İptal).</summary>
        public static bool IsRequestClosed(string status)
        {
            var s = (status ?? string.Empty).Trim();
            return string.Equals(s, Resources.Request_StatusDelivered, StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(s, Resources.Request_StatusCancelled, StringComparison.CurrentCultureIgnoreCase);
        }

        // Pencere açılınca: düzenlemede kayıttan, yeni kayıtta boş.
        private void ResetJournalRequest(EventNote? note)
        {
            journalRequestType = note?.RequestType ?? string.Empty;
            JournalRequestNo = note?.RequestNo ?? string.Empty;
            JournalRequestFrom = note != null ? string.Join(" ", note.RequestFrom.Select(p => "@" + p)) : string.Empty;
            JournalRequestDueText = note?.RequestDue?.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) ?? string.Empty;
            JournalRequestRecord = note != null && note.RequestType.Length > 0 ? string.Join(" ", note.Related) : string.Empty;
            JournalRequestDetail = note?.RequestDetail ?? string.Empty;
            journalRequestStatus = note?.RequestStatus ?? string.Empty;
            JournalRequestItems.Clear();
            foreach (var item in note?.RequestItems ?? new List<RequestItem>())
            {
                JournalRequestItems.Add(new RequestItemRow { StockNo = item.StockNo, Name = item.Name, Quantity = item.Quantity });
            }

            AddRequestItemRow();
            OnRequestChanged();
        }

        // Kayda talep alanları yazılır; tür seçilmediyse "Diğer". Yeni talep "Açık" başlar.
        private bool ApplyJournalRequest(JournalEntry entry)
        {
            if (!IsJournalRequest)
            {
                return true;
            }

            DateTime? due = null;
            if (journalRequestDueText.Trim().Length > 0)
            {
                due = ParseDay(journalRequestDueText);
                if (!due.HasValue)
                {
                    Status = Resources.Task_BadDate;
                    return false;
                }
            }

            entry.RequestType = journalRequestType.Length > 0 ? journalRequestType : Resources.Request_Other;
            entry.RequestNo = journalRequestNo.Trim();
            entry.RequestFrom.AddRange(journalRequestFrom.Split(new[] { ' ', ',', ';', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim().TrimStart('@', '#')).Where(p => p.Length > 0).Distinct(StringComparer.CurrentCultureIgnoreCase));
            entry.RequestDue = due;
            entry.RequestStatus = journalRequestStatus.Length > 0 ? journalRequestStatus : Resources.Request_StatusOpen;
            var index = RequestTypeIndex();
            if (index >= 0 && index <= 2)
            {
                entry.Related.Clear();
                entry.Related.AddRange(journalRequestRecord.Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(r => r.Trim().Trim('[', ']')).Where(r => r.Length > 0));
            }

            if (index == 3)
            {
                entry.RequestItems.AddRange(JournalRequestItems.Where(r => !r.IsEmpty).Select(r => new RequestItem { StockNo = r.StockNo.Trim(), Name = r.Name.Trim(), Quantity = r.Quantity.Trim() }));
            }

            if (index == 4 || index == 5)
            {
                entry.RequestDetail = journalRequestDetail.Trim();
            }

            return true;
        }

        private int RequestTypeIndex()
        {
            return Array.FindIndex(RequestTypes, t => string.Equals(t, journalRequestType, StringComparison.CurrentCultureIgnoreCase));
        }

        private void OnRequestChanged()
        {
            OnPropertyChanged(nameof(IsJournalRequest));
            OnPropertyChanged(nameof(JournalRequestType));
            OnPropertyChanged(nameof(JournalRequestTypeChoices));
            OnPropertyChanged(nameof(ShowsRequestRecord));
            OnPropertyChanged(nameof(ShowsRequestItems));
            OnPropertyChanged(nameof(ShowsRequestDetail));
            OnPropertyChanged(nameof(JournalRequestDetailHint));
        }
    }
}
