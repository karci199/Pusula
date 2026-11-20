using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Input;
using Pusula.Core.Notes;
using Pusula.Core.Tracking;
using Pusula.Core.Types;
using Pusula.Wpf.Mvvm;
using Pusula.Wpf.Properties;

namespace Pusula.Wpf.ViewModels
{
    /// <summary>Kartın altındaki etiket hapı: <c>#SATIMO</c> (mavi) ya da <c>@AliDemirci</c> (mor).</summary>
    public sealed class TimelineTag
    {
        /// <summary>Etiketi kurar.</summary>
        public TimelineTag(string text)
        {
            Text = text;
            IsPerson = text.StartsWith("@", StringComparison.Ordinal);
            if (IsPerson)
            {
                // "@Ali_Demirci" → "Ali Demirci", baş harfler "AD", renk isme göre sabit.
                Name = text.Substring(1).Replace('_', ' ').Trim();
                var words = Name.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                Initials = words.Length == 0 ? "?" : (words.Length == 1 ? words[0].Substring(0, 1) : words[0].Substring(0, 1) + words[words.Length - 1].Substring(0, 1)).ToUpper(CultureInfo.GetCultureInfo("tr-TR"));
                var hash = 0;
                foreach (var ch in Name.ToLower(CultureInfo.GetCultureInfo("tr-TR")))
                {
                    hash = unchecked((hash * 31) + ch);
                }

                AvatarColor = ProjectColors.Palette[Math.Abs(hash % ProjectColors.Palette.Length)];
            }
        }

        /// <summary>Kişide görünen ad ("Ali Demirci"); etikette boş.</summary>
        public string Name { get; } = string.Empty;

        /// <summary>Kişinin baş harfleri ("AD").</summary>
        public string Initials { get; } = string.Empty;

        /// <summary>Kişi yuvarlağının rengi (isme göre sabit).</summary>
        public string AvatarColor { get; } = string.Empty;

        /// <summary>Görünen metin (# ya da @ ile).</summary>
        public string Text { get; }

        /// <summary>Kişi etiketi mi.</summary>
        public bool IsPerson { get; }
    }

    /// <summary>
    /// Stok sayfası zaman çizelgesinde bir kart: işlem (ikon, renk), başlık, saat, açıklama, etiketler ve önem; yapılacak
    /// kartta onay kutusu ve son tarih. Kaynak, günlük olayı / test koşusu / not (<see cref="Item"/>) ya da açık iştir (<see cref="Task"/>).
    /// </summary>
    public sealed class StockTimelineCard : ViewModelBase
    {
        private bool isExpanded;

        private static readonly Regex Mention = new Regex(@"(?<![\w@])@([\p{L}][\p{L}\p{N}_\.]*)", RegexOptions.Compiled);

        /// <summary>Çizelge kaydından kartı kurar.</summary>
        public StockTimelineCard(TimelineItem item)
        {
            Item = item;
            var journal = item.Journal;
            var kind = journal != null ? EventKinds.Find(journal.Kind) : null;
            KindLabel = journal != null ? EventKinds.Label(journal.Kind) : item.Result.Name;
            Glyph = kind?.Glyph ?? (journal != null ? "" : "");
            Tone = kind?.Tone ?? item.Result.Tone;
            Title = item.Title;
            Description = item.Summary;
            FullDescription = (journal != null && journal.Body.Trim().Length > 0 ? journal.Body : item.Summary ?? string.Empty).Trim();

            // Günlük kaydında kartta "#### Ayrıntı" başlığından öncesi görünür; ayrıntı MD düğmesiyle notta okunur.
            if (journal != null && journal.Body.Trim().Length > 0)
            {
                FullDescription = Journal.SummaryOf(journal.Body);
                Description = Regex.Replace(FullDescription, @"\s+", " ");
            }
            When = item.SortDate;
            TimeText = item.SortDate.TimeOfDay == TimeSpan.Zero ? Resources.Timeline_AllDay : item.SortDate.ToString("HH:mm", CultureInfo.InvariantCulture);
            ImportanceRank = journal == null ? 0 : EventKinds.ImportanceRank(journal.Importance);
            ImportanceText = ImportanceRank == 2 ? Resources.Importance_Critical : ImportanceRank == 1 ? Resources.Importance_High : string.Empty;
            var tags = journal == null ? new List<string>() : journal.Tags.Select(t => "#" + t).ToList();
            var body = journal == null ? item.Summary : journal.Body;
            foreach (Match m in Mention.Matches(body ?? string.Empty))
            {
                tags.Add("@" + m.Groups[1].Value);
            }

            if (journal != null)
            {
                // Toplantı: katılımcılar kişi hapı, ortam ikonlu çip, kararlar ayrı kutu.
                tags.InsertRange(0, journal.Participants.Concat(journal.DecidedBy).Select(p => "@" + p));
                Setting = journal.Setting;
                SettingIcon = MainViewModel.SettingIconKey(journal.Setting);
                Decisions = journal.Decisions;
            }

            Tags = tags.Distinct(StringComparer.CurrentCultureIgnoreCase).Select(t => new TimelineTag(t)).ToList();

            // Ölçüm: bağlı madde ve sonuç tek rozette ("↳ TG-1 · Kaldı", madde yoksa "Bilgi").
            var result = journal?.Result.Trim() ?? string.Empty;
            var linked = journal?.Item.Trim() ?? string.Empty;
            ResultKind = result.Length == 0 && linked.Length == 0 ? 0
                : string.Equals(result, Resources.Verify_StatusPassed, StringComparison.CurrentCultureIgnoreCase) ? 1
                : string.Equals(result, Resources.Verify_StatusFailed, StringComparison.CurrentCultureIgnoreCase) ? 2 : 3;
            ResultText = linked.Length > 0 ? "↳ " + linked + (result.Length > 0 ? " · " + result : string.Empty) : result;

            // Talep: tür çipi, durum (açık · istenen tarih / gecikti / teslim alındı), kalemler ya da istenen.
            if (journal != null && journal.RequestType.Length > 0)
            {
                RequestType = journal.RequestType;
                RequestIcon = MainViewModel.RequestIconKey(journal.RequestType);
                var closed = MainViewModel.IsRequestClosed(journal.RequestStatus);
                var late = !closed && journal.RequestDue.HasValue && journal.RequestDue.Value.Date < DateTime.Today;
                RequestStatusKind = closed ? 3 : late ? 2 : 1;
                var status = journal.RequestStatus.Length > 0 ? journal.RequestStatus : Resources.Request_StatusOpen;
                RequestStatusText = closed || !journal.RequestDue.HasValue ? status
                    : status + " · " + journal.RequestDue.Value.ToString("dd.MM", CultureInfo.InvariantCulture) + (late ? " · " + Resources.Request_DueLate : string.Empty);
                var lines = journal.RequestItems.Select(i => string.Join(" · ", new[] { i.StockNo, i.Name, i.Quantity }.Where(s => s.Trim().Length > 0))).ToList();
                if (journal.RequestDetail.Length > 0)
                {
                    lines.Add(journal.RequestDetail);
                }

                RequestLines = string.Join(Environment.NewLine, lines);
                RequestMeta = string.Join(" · ", new[] { journal.RequestNo, string.Join(" ", journal.RequestFrom.Select(p => p.Replace('_', ' '))) }.Where(s => s.Length > 0));
            }

            // Ölçüm klasörü: kartta "📁 sayı" düğmesi.
            if (journal != null && journal.Folder.Trim().Length > 0)
            {
                FolderPath = MeasurementFiles.Resolve(System.IO.Path.GetDirectoryName(journal.Path) ?? string.Empty, journal.Folder);
                FileCount = MeasurementFiles.CountFiles(FolderPath);
            }
        }

        /// <summary>Talep türü ("Malzeme"); talep değilse boş.</summary>
        public string RequestType { get; } = string.Empty;

        /// <summary>Talep türünün ikon anahtarı.</summary>
        public string RequestIcon { get; } = string.Empty;

        /// <summary>Talep mi.</summary>
        public bool IsRequest
        {
            get { return RequestType.Length > 0; }
        }

        /// <summary>Talep durumu metni ("Açık · 25.11", "Açık · 18.11 · gecikti", "Teslim alındı").</summary>
        public string RequestStatusText { get; } = string.Empty;

        /// <summary>Talep durumu: 0 talep değil, 1 açık, 2 gecikmiş, 3 kapalı.</summary>
        public int RequestStatusKind { get; }

        /// <summary>Açık (ya da gecikmiş) talep mi: "Teslim alındı" düğmesi görünür.</summary>
        public bool IsRequestOpen
        {
            get { return RequestStatusKind == 1 || RequestStatusKind == 2; }
        }

        /// <summary>Kalemler ya da istenen (satır satır).</summary>
        public string RequestLines { get; } = string.Empty;

        /// <summary>Kalem ya da istenen var mı.</summary>
        public bool HasRequestLines
        {
            get { return RequestLines.Length > 0; }
        }

        /// <summary>Talep no ve kimden ("AMB-2026-1182 · Ambar").</summary>
        public string RequestMeta { get; } = string.Empty;

        /// <summary>Talep no / kimden var mı.</summary>
        public bool HasRequestMeta
        {
            get { return RequestMeta.Length > 0; }
        }

        /// <summary>Toplantı ortamı ("Teams"); yoksa boş.</summary>
        public string Setting { get; } = string.Empty;

        /// <summary>Ortamın ikon anahtarı (<c>teams</c>, <c>face</c> …); tanınmazsa boş.</summary>
        public string SettingIcon { get; } = string.Empty;

        /// <summary>Ortam çipi görünür mü.</summary>
        public bool HasSetting
        {
            get { return Setting.Length > 0; }
        }

        /// <summary>Toplantı kararları; yoksa boş.</summary>
        public string Decisions { get; } = string.Empty;

        /// <summary>Karar var mı.</summary>
        public bool HasDecisions
        {
            get { return Decisions.Length > 0; }
        }

        /// <summary>Ölçüm dosyalarının klasörü (tam yol); yoksa boş.</summary>
        public string FolderPath { get; } = string.Empty;

        /// <summary>Ölçüm klasöründeki dosya sayısı.</summary>
        public int FileCount { get; }

        /// <summary>Klasör düğmesi görünür mü.</summary>
        public bool HasFolder
        {
            get { return FolderPath.Length > 0; }
        }

        /// <summary>Günlük kaydı mı (dosya eklenebilir, düzenlenebilir).</summary>
        public bool IsJournal
        {
            get { return Item?.Journal != null; }
        }

        /// <summary>Ölçüm rozeti: 0 yok, 1 geçti, 2 kaldı, 3 bilgi / sonuçsuz.</summary>
        public int ResultKind { get; }

        /// <summary>Ölçüm rozetinin metni ("↳ TG-1 · Kaldı", "Bilgi"); yoksa boş.</summary>
        public string ResultText { get; } = string.Empty;

        /// <summary>Ölçüm rozeti görünür mü.</summary>
        public bool HasResult
        {
            get { return ResultKind > 0; }
        }

        /// <summary>Açık işten (yapılacak) kartı kurar; metin "İşlem: başlık" ise işlem çipi olur.</summary>
        public StockTimelineCard(OpenTaskRow task)
        {
            Task = task;
            IsPlanned = true;
            var text = task.Text;
            var colon = text.IndexOf(": ", StringComparison.Ordinal);
            var kind = colon > 0 ? EventKinds.Find(text.Substring(0, colon)) : null;
            if (kind != null)
            {
                text = text.Substring(colon + 2);
            }

            KindLabel = kind?.Label ?? Resources.Timeline_Planned;
            Glyph = kind?.Glyph ?? "";
            Tone = kind?.Tone ?? StatusTone.Neutral;
            // Etiketler satırın kendisinden (iş metni ayrıştırılırken etiketler atılmış olabilir).
            var tags = Regex.Matches(task.Task.RawLine + " " + text, @"(?<![\w#&])#([\p{L}][\p{L}\p{N}_/\-]*)").Cast<Match>().Select(m => "#" + m.Groups[1].Value).Distinct(StringComparer.CurrentCultureIgnoreCase).ToList();
            Title = Regex.Replace(text, @"\s*(?<![\w#])#[\p{L}\p{N}_/\-]+", string.Empty).Trim();
            Description = task.Task.Description.Replace(Environment.NewLine, " ");
            FullDescription = task.Task.Description;
            When = task.Task.Due ?? DateTime.MaxValue;
            TimeText = task.DueText;
            IsLate = task.IsLate;
            var people = task.Task.People.Count > 0 ? task.Task.People : (task.PersonName.Length > 0 ? new List<string> { task.PersonName } : new List<string>());
            var all = people.Select(p => "@" + p.Replace(' ', '_')).Concat(tags).ToList();
            if (task.Task.LinkTarget.Length > 0)
            {
                all.Add("↳ " + task.Task.LinkTarget);
            }

            Tags = all.Select(t => new TimelineTag(t)).ToList();
        }

        /// <summary>Çizelge kaydı (yapılacakta <c>null</c>).</summary>
        public TimelineItem? Item { get; }

        /// <summary>Açık iş (yalnız yapılacakta).</summary>
        public OpenTaskRow? Task { get; }

        /// <summary>Yapılacak mı (kesik çizgili kart, onay kutusu).</summary>
        public bool IsPlanned { get; }

        /// <summary>Tarihi geçmiş yapılacak.</summary>
        public bool IsLate { get; }

        /// <summary>İşlem adı.</summary>
        public string KindLabel { get; }

        /// <summary>İşlem ikonu.</summary>
        public string Glyph { get; }

        /// <summary>İşlem renk tonu (<see cref="StatusTone"/>).</summary>
        public string Tone { get; }

        /// <summary>Başlık.</summary>
        public string Title { get; }

        /// <summary>Açıklama (kısaltılmış).</summary>
        public string Description { get; }

        /// <summary>Açıklamanın tamamı (kart açılınca görünür).</summary>
        public string FullDescription { get; } = string.Empty;

        /// <summary>Kart açık mı (tıklayınca): açıklama tek satır yerine tam gösterilir.</summary>
        public bool IsExpanded
        {
            get { return isExpanded; }
            set { SetProperty(ref isExpanded, value); }
        }

        /// <summary>Açıklama var mı.</summary>
        public bool HasDescription
        {
            get { return Description.Length > 0; }
        }

        /// <summary>Sıralama zamanı.</summary>
        public DateTime When { get; }

        /// <summary>Saat ("14:20", "Gün boyu") ya da yapılacakta son tarih.</summary>
        public string TimeText { get; }

        /// <summary>Önem: 0 normal, 1 önemli, 2 kritik.</summary>
        public int ImportanceRank { get; }

        /// <summary>Önem rozeti ("Önemli", "Kritik"); normalde boş.</summary>
        public string ImportanceText { get; } = string.Empty;

        /// <summary>Önem rozetinin ikonu (Önemli ünlem, Kritik uyarı).</summary>
        public string ImportanceGlyph
        {
            get { return EventKinds.ImportanceGlyph(ImportanceRank); }
        }

        /// <summary>Önem rozeti görünür mü.</summary>
        public bool HasImportance
        {
            get { return ImportanceRank > 0; }
        }

        /// <summary>Etiket hapları.</summary>
        public IList<TimelineTag> Tags { get; }

        /// <summary>Etiket var mı.</summary>
        public bool HasTags
        {
            get { return Tags.Count > 0; }
        }

        /// <summary>Projenin rengi (ray ve nötr işlemin noktası).</summary>
        public string ProjectColor { get; set; } = ProjectColors.Palette[2];

        /// <summary>Kaydın stoku ("AB-7000-0002"); yalnız ana sayfa Günlük'te dolu.</summary>
        public string Source { get; set; } = string.Empty;

        /// <summary>Stokun klasörü (ana sayfada çift tıklayınca açılır).</summary>
        public string SourcePath { get; set; } = string.Empty;

        /// <summary>Stok numarası görünür mü.</summary>
        public bool HasSource
        {
            get { return Source.Length > 0; }
        }
    }

    /// <summary>Zaman çizelgesinde bir gün (ya da "Yapılacak" bölümü) ve kartları.</summary>
    public sealed class StockTimelineDay
    {
        /// <summary>Günü kurar.</summary>
        public StockTimelineDay(string title, string subtitle, IList<StockTimelineCard> cards)
        {
            Title = title;
            Subtitle = subtitle;
            Cards = cards;
            RailColor = cards.Count > 0 ? cards[0].ProjectColor : ProjectColors.Palette[2];
        }

        /// <summary>Gün başlığı satırındaki rayın rengi (ilk kartın projesi).</summary>
        public string RailColor { get; }

        /// <summary>Başlık ("Bugün", "Dün", "Yapılacak").</summary>
        public string Title { get; }

        /// <summary>Yanındaki soluk yazı ("18.11 Çar", "· 2").</summary>
        public string Subtitle { get; }

        /// <summary>Kartlar.</summary>
        public IList<StockTimelineCard> Cards { get; }
    }

    /// <summary>
    /// Stok sayfası zaman çizelgesi: süzgeç (Hepsi · Yapılan · Yapılacak · Önemli) ve "+ Kayıt" penceresinin yeni alanları
    /// (yapıldı / yapılacak, ayrı tarih ve saat, işlem ızgarası, yeni işlem türü).
    /// </summary>
    public sealed partial class MainViewModel
    {
        private string timelineFilter = "all";
        private bool journalPlanned;
        private string journalDayText = string.Empty;
        private string journalTimeText = string.Empty;
        private bool isNewKindOpen;
        private string newKindName = string.Empty;
        private string newKindGlyph = EventKinds.Glyphs[0];
        private string newKindTone = StatusTone.Active;
        private ICommand? addKindCommand;
        private ICommand? toggleNewKindCommand;

        /// <summary>Süzgeç: hepsi.</summary>
        public bool IsTimelineAll
        {
            get { return timelineFilter == "all"; }
            set { SetTimelineFilter(value, "all"); }
        }

        /// <summary>Süzgeç: yalnız yapılanlar.</summary>
        public bool IsTimelineDone
        {
            get { return timelineFilter == "done"; }
            set { SetTimelineFilter(value, "done"); }
        }

        /// <summary>Süzgeç: yalnız yapılacaklar.</summary>
        public bool IsTimelinePlanned
        {
            get { return timelineFilter == "planned"; }
            set { SetTimelineFilter(value, "planned"); }
        }

        /// <summary>Süzgeç: önemli ve kritik kayıtlar.</summary>
        public bool IsTimelineImportant
        {
            get { return timelineFilter == "important"; }
            set { SetTimelineFilter(value, "important"); }
        }

        /// <summary>Pencerede: yapılacak mı (Yapılacaklar bölümüne iş olarak yazılır; açıklama ve önem yazılmaz).</summary>
        public bool JournalPlanned
        {
            get
            {
                return journalPlanned;
            }

            set
            {
                if (SetProperty(ref journalPlanned, value))
                {
                    OnPropertyChanged(nameof(JournalDone));
                    OnPropertyChanged(nameof(JournalPrimaryText));
                    OnPropertyChanged(nameof(JournalRecordHint));
                    OnPropertyChanged(nameof(IsJournalMeasurement));
                    OnPropertyChanged(nameof(IsJournalNewItem));
                    OnMeetingChanged();
                    OnDecisionChanged();
                    OnRequestChanged();
                }
            }
        }

        /// <summary>Kayıt türünün altındaki açıklama: olay çizelgeye işlenir; yapılacak iş listeye eklenir, bitince çizelgeye düşer.</summary>
        public string JournalRecordHint
        {
            get { return journalPlanned ? Resources.Journal_RecordHintPlanned : Resources.Journal_RecordHintDone; }
        }

        /// <summary>Pencerede: yapıldı mı (Günlük'e olay).</summary>
        public bool JournalDone
        {
            get { return !journalPlanned; }
            set { JournalPlanned = !value; }
        }

        /// <summary>Düzenlemede "Yapılacak" seçilemez (olay iş olmaz).</summary>
        public bool CanPlanJournal
        {
            get { return editing == null; }
        }

        /// <summary>Tarih kutusu (gün.ay.yıl), elle yazılır.</summary>
        public string JournalDayText
        {
            get
            {
                return journalDayText;
            }

            set
            {
                if (SetProperty(ref journalDayText, value ?? string.Empty))
                {
                    JournalDateText = (journalDayText.Trim() + " " + journalTimeText.Trim()).Trim();
                }
            }
        }

        /// <summary>Saat kutusu (ss:dd), elle yazılır; boşsa gün boyu.</summary>
        public string JournalTimeText
        {
            get
            {
                return journalTimeText;
            }

            set
            {
                if (SetProperty(ref journalTimeText, value ?? string.Empty))
                {
                    JournalDateText = (journalDayText.Trim() + " " + journalTimeText.Trim()).Trim();
                }
            }
        }

        /// <summary>Yeni işlem türü formu açık mı.</summary>
        public bool IsNewKindOpen
        {
            get { return isNewKindOpen; }
            set { SetProperty(ref isNewKindOpen, value); }
        }

        /// <summary>Yeni işlem türünün adı.</summary>
        public string NewKindName
        {
            get { return newKindName; }
            set { SetProperty(ref newKindName, value ?? string.Empty); }
        }

        /// <summary>Yeni işlem türünün ikonu.</summary>
        public string NewKindGlyph
        {
            get { return newKindGlyph; }
            set { SetProperty(ref newKindGlyph, value ?? EventKinds.Glyphs[0]); }
        }

        /// <summary>Yeni işlem türünün rengi.</summary>
        public string NewKindTone
        {
            get { return newKindTone; }
            set { SetProperty(ref newKindTone, value ?? StatusTone.Neutral); }
        }

        /// <summary>Seçilebilen ikonlar.</summary>
        public string[] NewKindGlyphs
        {
            get { return EventKinds.Glyphs; }
        }

        /// <summary>Seçilebilen renk tonları.</summary>
        public string[] NewKindTones
        {
            get { return EventKinds.Tones; }
        }

        /// <summary>"+ Yeni işlem": formu açar / kapar.</summary>
        public ICommand ToggleNewKindCommand
        {
            get { return toggleNewKindCommand ?? (toggleNewKindCommand = new RelayCommand(() => IsNewKindOpen = !IsNewKindOpen)); }
        }

        /// <summary>Yeni işlem türünü ortak listeye (<c>_TAKIP\Islemler.md</c>) ekler ve seçer.</summary>
        public ICommand AddKindCommand
        {
            get { return addKindCommand ?? (addKindCommand = new RelayCommand(() => AddKind())); }
        }

        /// <summary>Yeni işlem türünü ekler (ad boşsa bir şey yapmaz); eklendiyse <c>true</c>.</summary>
        public bool AddKind()
        {
            var name = newKindName.Trim();
            if (name.Length == 0)
            {
                return false;
            }

            var error = EventKindList.Add(rootPath, new CustomEventKind { Name = name, Glyph = EventKinds.GlyphCode(newKindGlyph), Tone = newKindTone }, Resources.Timeline_KindsTemplate);
            if (error.Length > 0)
            {
                Status = string.Format(Resources.Error_Io, error);
                return false;
            }

            ReloadKinds();
            JournalKind = name;
            NewKindName = string.Empty;
            IsNewKindOpen = false;
            return true;
        }

        // Ortak listedeki eklenmiş türler okunur; ızgara yenilenir.
        private void ReloadKinds()
        {
            EventKinds.SetCustom(EventKindList.Read(rootPath));
            OnPropertyChanged(nameof(JournalKinds));
            OnPropertyChanged(nameof(JournalKindOption));
        }

        private void SetTimelineFilter(bool on, string key)
        {
            if (!on || timelineFilter == key)
            {
                return;
            }

            timelineFilter = key;
            OnPropertyChanged(nameof(IsTimelineAll));
            OnPropertyChanged(nameof(IsTimelineDone));
            OnPropertyChanged(nameof(IsTimelinePlanned));
            OnPropertyChanged(nameof(IsTimelineImportant));
            FilterStockTimeline();
        }

        // Pencere açılırken: tarih ve saat şu an, yapıldı seçili, yeni tür formu kapalı.
        private void ResetJournalExtras(DateTime when, bool planned)
        {
            journalDayText = string.Empty;
            journalTimeText = string.Empty;
            JournalDayText = when.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
            JournalTimeText = when.TimeOfDay == TimeSpan.Zero ? string.Empty : when.ToString("HH:mm", CultureInfo.InvariantCulture);
            JournalDate = ParseDate(JournalDateText);
            JournalPlanned = planned;
            IsNewKindOpen = false;
            NewKindName = string.Empty;
            OnPropertyChanged(nameof(CanPlanJournal));
        }

        // Yapılacak: ana notun Yapılacaklar bölümüne iş satırı ("İşlem: başlık #etiket", son tarih).
        private bool AddPlannedJournal()
        {
            var title = JournalHeading.Trim();
            var tags = JournalTags.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(t => "#" + t.TrimStart('#')).Where(t => t.Length > 1).Distinct(StringComparer.OrdinalIgnoreCase);
            var text = (JournalKind.Trim().Length > 0 ? JournalKind.Trim() + ": " : string.Empty) + title + string.Concat(tags.Select(t => " " + t));
            var error = TaskList.Add(journalNotePath, text, string.Empty, JournalDate?.Date);
            if (error.Length > 0)
            {
                Status = string.Format(Resources.Error_Io, error);
                return false;
            }

            Done(string.Format(Resources.Task_Added, title), journalNotePath);
            RefreshShowing(journalFolder);
            return true;
        }

        // Zaman çizelgesinin kartları: yapılacaklar (açık işler) üstte, sonra seçili aralıktaki kayıtlar gün gün.
        private void BuildStockTimeline(DateTime? start)
        {
            var today = DateTime.Today;
            var culture = new CultureInfo("tr-TR");
            var project = selectedNode == null ? null : ProjectOf(selectedNode.Path);
            var color = string.IsNullOrEmpty(project?.ProjectColor) ? ProjectColors.Palette[2] : project!.ProjectColor;
            StockDays.Clear();

            // Bekleyen talepler (Yapılacaklar sekmesinde): açık ya da gecikmiş talepler, istenen tarihe göre.
            OpenRequests.Clear();
            foreach (var card in Detail.Timeline.Where(t => t.Journal != null && t.Journal.RequestType.Length > 0 && !IsRequestClosed(t.Journal.RequestStatus))
                .OrderBy(t => t.Journal!.RequestDue ?? DateTime.MaxValue).Select(t => new StockTimelineCard(t) { ProjectColor = color }))
            {
                OpenRequests.Add(card);
            }

            OnPropertyChanged(nameof(HasOpenRequests));
            if (timelineFilter == "all" || timelineFilter == "planned")
            {
                var planned = Detail.OpenTasks.Where(t => !t.IsDone).OrderBy(t => t.Task.Due ?? DateTime.MaxValue).Select(t => new StockTimelineCard(t) { ProjectColor = color }).ToList();
                if (planned.Count > 0)
                {
                    StockDays.Add(new StockTimelineDay(Resources.Timeline_Planned, "· " + planned.Count.ToString(CultureInfo.CurrentCulture), planned));
                }
            }

            if (timelineFilter == "planned")
            {
                return;
            }

            var cards = Detail.Timeline
                .Where(t => !start.HasValue || t.SortDate.Date >= start.Value)
                .OrderByDescending(t => t.SortDate)
                .Select(t => new StockTimelineCard(t) { ProjectColor = color })
                .Where(c => timelineFilter != "important" || c.ImportanceRank > 0);
            foreach (var day in GroupDays(cards))
            {
                StockDays.Add(day);
            }
        }

        // Kartları günlere ayırır: "Bugün · 18.11 Çar", "Dün", "12.11.2026".
        private static IEnumerable<StockTimelineDay> GroupDays(IEnumerable<StockTimelineCard> cards)
        {
            var today = DateTime.Today;
            var culture = new CultureInfo("tr-TR");
            foreach (var day in cards.GroupBy(c => c.When.Date))
            {
                var title = day.Key == today ? Resources.Day_Today
                    : day.Key == today.AddDays(-1) ? Resources.Day_Yesterday
                    : day.Key.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
                var sub = day.Key.ToString("dd.MM ddd", culture);
                yield return new StockTimelineDay(title, title == sub ? string.Empty : "· " + sub, day.ToList());
            }
        }
    }
}
