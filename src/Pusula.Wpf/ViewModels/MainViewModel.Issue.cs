using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using Pusula.Core.Notes;
using Pusula.Wpf.Properties;

namespace Pusula.Wpf.ViewModels
{
    /// <summary>
    /// Kayıt penceresinde sorun ve çözüm alanları. Sorun: isteğe bağlı ilgili madde, birim (seri no) ve durum (Açık / Çözüldü).
    /// Çözüm: "Çözdüğü sorun" — Yok (doğrudan çözüm) ya da stokun açık sorunlarından biri; bağlanınca sorun "Çözüldü" olur.
    /// </summary>
    public sealed partial class MainViewModel
    {
        private string journalIssueUnit = string.Empty;
        private string journalIssueStatus = string.Empty;
        private string journalIssueStatusText = string.Empty;
        private string journalResolves = string.Empty;

        /// <summary>Stok sayfası · Yapılacaklar: açık sorunlar (eskiden yeniye).</summary>
        public ObservableCollection<StockTimelineCard> OpenIssues { get; } = new ObservableCollection<StockTimelineCard>();

        /// <summary>Açık sorun var mı.</summary>
        public bool HasOpenIssues
        {
            get { return OpenIssues.Count > 0; }
        }

        /// <summary>Ana sayfa · Yapılacaklar: bütün stokların açık sorunları (eskiden yeniye; kartta stok no).</summary>
        public ObservableCollection<StockTimelineCard> HomeOpenIssues { get; } = new ObservableCollection<StockTimelineCard>();

        /// <summary>Ana sayfada açık sorun var mı.</summary>
        public bool HasHomeOpenIssues
        {
            get { return HomeOpenIssues.Count > 0; }
        }

        /// <summary>Ana sayfadaki açık sorunlar başlığı ("Açık sorunlar · 3").</summary>
        public string HomeOpenIssuesTitle
        {
            get { return Resources.Issue_OpenTitle + " · " + HomeOpenIssues.Count.ToString(CultureInfo.CurrentCulture); }
        }

        // Ana sayfa kartlarından açık sorunlar.
        private void FillHomeOpenIssues(IEnumerable<StockTimelineCard> events)
        {
            HomeOpenIssues.Clear();
            foreach (var card in events.Where(c => c.IsIssueOpen).OrderBy(c => c.When))
            {
                HomeOpenIssues.Add(card);
            }

            OnPropertyChanged(nameof(HasHomeOpenIssues));
            OnPropertyChanged(nameof(HomeOpenIssuesTitle));
        }

        /// <summary>Sorun alanları görünür mü (yapıldı kaydı ve işlem Sorun).</summary>
        public bool IsJournalIssue
        {
            get { return JournalDone && IsIssueKind(journalKind); }
        }

        /// <summary>Çözüm alanları görünür mü (yapıldı kaydı ve işlem Çözüm).</summary>
        public bool IsJournalSolution
        {
            get { return JournalDone && IsSolutionKind(journalKind); }
        }

        /// <summary>Sorunun görüldüğü birim (seri no); isteğe bağlı.</summary>
        public string JournalIssueUnit
        {
            get { return journalIssueUnit; }
            set { SetProperty(ref journalIssueUnit, value ?? string.Empty); }
        }

        /// <summary>Sorun durumu: <c>Açık</c> ya da <c>Çözüldü</c>.</summary>
        public string JournalIssueStatus
        {
            get
            {
                return journalIssueStatus;
            }

            set
            {
                if (SetProperty(ref journalIssueStatus, value ?? string.Empty))
                {
                    OnPropertyChanged(nameof(JournalIssueStatusChoices));
                }
            }
        }

        /// <summary>Durum seçenekleri (Açık · Çözüldü).</summary>
        public IList<ChoiceOption> JournalIssueStatusChoices
        {
            get
            {
                return new[] { Resources.Issue_Open, Resources.Issue_Solved }
                    .Select(s => new ChoiceOption(s, s, string.Equals(s, journalIssueStatus, StringComparison.CurrentCultureIgnoreCase), o => JournalIssueStatus = o.Key)).ToList();
            }
        }

        /// <summary>Çözdüğü sorun seçenekleri: — Yok ve stokun açık sorunları (rozet "Açık").</summary>
        public ObservableCollection<ChoiceOption> JournalSolutionOptions { get; } = new ObservableCollection<ChoiceOption>();

        /// <summary>Seçili çözdüğü sorun (açılır liste).</summary>
        public ChoiceOption? JournalSolutionOption
        {
            get { return JournalSolutionOptions.FirstOrDefault(o => o.Key == journalResolves) ?? JournalSolutionOptions.FirstOrDefault(); }
            set
            {
                // Liste yeniden doldurulurken kutu seçimi boşaltır; bu, bağı silmez.
                if (value != null)
                {
                    JournalResolves = value.Key;
                }
            }
        }

        /// <summary>Çözdüğü sorunun anahtarı (<see cref="Journal.IssueKey"/>); boş = doğrudan çözüm.</summary>
        public string JournalResolves
        {
            get
            {
                return journalResolves;
            }

            set
            {
                if (SetProperty(ref journalResolves, value ?? string.Empty))
                {
                    OnPropertyChanged(nameof(JournalSolutionOption));
                }
            }
        }

        /// <summary>İşlem Sorun mu.</summary>
        public static bool IsIssueKind(string kind)
        {
            var key = Journal.KindKey(kind ?? string.Empty);
            return key.Length > 0 && key == Journal.KindKey(Resources.Event_Issue);
        }

        /// <summary>İşlem Çözüm mü.</summary>
        public static bool IsSolutionKind(string kind)
        {
            var key = Journal.KindKey(kind ?? string.Empty);
            return key.Length > 0 && key == Journal.KindKey(Resources.Event_Solution);
        }

        /// <summary>Sorun durumu çözüldü mü (<c>Çözüldü …</c>).</summary>
        public static bool IsIssueSolved(string status)
        {
            return (status ?? string.Empty).Trim().StartsWith(Resources.Issue_Solved, StringComparison.CurrentCultureIgnoreCase);
        }

        /// <summary>Açık sorunun kartından "✓ Çözüm ekle": kayıt penceresini Çözüm ve bu sorun seçili açar.</summary>
        public bool OpenSolutionFor(EventNote issue)
        {
            return OpenSolutionFor(issue, string.Empty);
        }

        /// <summary>
        /// Açık sorun için Çözüm penceresini açar; <paramref name="folder"/> verilirse (ana sayfa) kayıt o stokun notuna,
        /// boşsa gösterilen sayfaya yazılır.
        /// </summary>
        public bool OpenSolutionFor(EventNote issue, string folder)
        {
            if (issue == null || !(folder.Length > 0 ? OpenJournal(folder) : OpenJournalHere()))
            {
                return false;
            }

            JournalKind = Resources.Event_Solution;
            JournalResolves = Journal.IssueKey(issue.Date, issue.Title);
            return true;
        }

        // Pencere açılınca: düzenlemede kayıttan, yeni kayıtta boş (sorun Açık başlar); açık sorunlar notta okunur.
        private void ResetJournalIssue(EventNote? note)
        {
            var issue = note != null && IsIssueKind(note.KindLabel);
            JournalIssueUnit = note?.IssueUnit ?? string.Empty;
            journalIssueStatusText = issue ? note!.RequestStatus : string.Empty;
            JournalIssueStatus = issue && IsIssueSolved(note!.RequestStatus) ? Resources.Issue_Solved : Resources.Issue_Open;
            var resolves = note?.Resolves ?? string.Empty;
            JournalSolutionOptions.Clear();
            JournalSolutionOptions.Add(new ChoiceOption(string.Empty, Resources.Solution_None, false, o => { }));
            foreach (var open in ReadOpenIssues(journalNotePath))
            {
                var info = new[] { open.Date.ToString("dd.MM", CultureInfo.InvariantCulture), open.Item, open.IssueUnit }.Where(s => s.Length > 0);
                JournalSolutionOptions.Add(new ChoiceOption(Journal.IssueKey(open.Date, open.Title), open.Title + " · " + string.Join(" · ", info), false, o => { })
                {
                    Badge = Resources.Issue_Open,
                });
            }

            // Düzenlenen çözümün sorunu artık kapalıysa da listede kalır.
            if (resolves.Length > 0 && JournalSolutionOptions.All(o => o.Key != resolves))
            {
                JournalSolutionOptions.Add(new ChoiceOption(resolves, resolves, false, o => { }));
            }

            journalResolves = resolves;
            OnIssueChanged();
            OnPropertyChanged(nameof(JournalResolves));
        }

        // Kayda sorun / çözüm alanları yazılır.
        private void ApplyJournalIssue(JournalEntry entry)
        {
            if (IsJournalIssue)
            {
                entry.Item = journalItem == NewItemKey ? string.Empty : journalItem;
                entry.Result = string.Empty;
                entry.IssueUnit = journalIssueUnit.Trim();
                if (journalIssueStatus == Resources.Issue_Solved)
                {
                    entry.IssueStatus = IsIssueSolved(journalIssueStatusText) ? journalIssueStatusText : Resources.Issue_Solved + " " + DateTime.Today.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
                }
                else
                {
                    entry.IssueStatus = Resources.Issue_Open;
                }
            }

            if (IsJournalSolution)
            {
                entry.Resolves = journalResolves;
            }
        }

        // Çözüm yazıldıktan sonra bağlı sorun "Çözüldü <çözüm tarihi>" olur (yalnız o sorunun bilgi satırı değişir).
        private void CloseResolvedIssue(JournalEntry entry)
        {
            if (entry.Resolves.Length == 0)
            {
                return;
            }

            var issue = ReadOpenIssues(journalNotePath).FirstOrDefault(n => Journal.IssueKey(n.Date, n.Title) == entry.Resolves);
            if (issue == null)
            {
                return;
            }

            var closed = Journal.EntryOf(issue);
            closed.IssueStatus = Resources.Issue_Solved + " " + entry.Date.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
            var error = Journal.Replace(issue.Path, issue.SourceLine, closed);
            if (error.Length > 0)
            {
                Status = string.Format(Resources.Error_Io, error);
            }
        }

        // Notun açık sorunları (eskiden yeniye).
        private static List<EventNote> ReadOpenIssues(string notePath)
        {
            if (string.IsNullOrEmpty(notePath) || !File.Exists(notePath))
            {
                return new List<EventNote>();
            }

            return Journal.Read(notePath).Where(n => IsIssueKind(n.KindLabel) && !IsIssueSolved(n.RequestStatus)).OrderBy(n => n.Date).ToList();
        }

        private void OnIssueChanged()
        {
            OnPropertyChanged(nameof(IsJournalIssue));
            OnPropertyChanged(nameof(IsJournalSolution));
            OnPropertyChanged(nameof(JournalIssueStatusChoices));
            OnPropertyChanged(nameof(JournalSolutionOption));
        }
    }
}
