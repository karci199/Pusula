using System;
using System.Collections.Generic;
using System.Linq;
using Pusula.Core.Scanning;
using Pusula.Core.Testing;
using Pusula.Core.Types;
using Pusula.Wpf.Mvvm;
using Pusula.Wpf.Properties;

namespace Pusula.Wpf.ViewModels
{
    /// <summary>Test sonucunu etikete çevirir (metin + renk).</summary>
    public static class TestResultChip
    {
        /// <summary>Sonucun etiketi; <paramref name="suffix"/> varsa sonuna eklenir ("Geçti · T2").</summary>
        public static StatusChip For(TestResult result, string suffix = "")
        {
            string text;
            string tone;
            switch (result)
            {
                case TestResult.Passed:
                    text = Resources.Result_Passed;
                    tone = StatusTone.Done;
                    break;
                case TestResult.Failed:
                    text = Resources.Result_Failed;
                    tone = StatusTone.Failed;
                    break;
                case TestResult.InProgress:
                    text = Resources.Result_InProgress;
                    tone = StatusTone.Waiting;
                    break;
                default:
                    text = Resources.Result_Unknown;
                    tone = StatusTone.Neutral;
                    break;
            }

            return new StatusChip(suffix.Length == 0 ? text : text + " · " + suffix, tone);
        }

        /// <summary>Sistem adı; kalifikasyonsa "(kal.)" eklenir.</summary>
        public static string SystemLabel(string system, bool qualification)
        {
            return qualification ? string.Format(Resources.Test_QualificationSystem, system) : system;
        }
    }

    /// <summary>Testler / Kalifikasyon tablosunun bir satırı.</summary>
    public sealed class TestRunRow
    {
        /// <summary>Koşudan satır oluşturur.</summary>
        public TestRunRow(TestRun run)
        {
            Serial = run.SerialNo;
            Round = run.Round;
            RoundText = "T" + run.Round;
            System = run.System;
            Result = TestResultChip.For(run.Result);
            ResultName = Result.Name;
            Date = run.Date;
            DateText = run.Date == DateTime.MinValue ? "—" : run.Date.ToString(Resources.Format_Date);
            Person = run.Person.Length == 0 ? "—" : run.Person;
            Summary = run.Summary;
            NotePath = run.NotePath;
            Folder = run.Folder;
        }

        /// <summary>Seri numarası.</summary>
        public string Serial { get; }

        /// <summary>Tur (sıralama için sayı).</summary>
        public int Round { get; }

        /// <summary>Tur metni ("T2").</summary>
        public string RoundText { get; }

        /// <summary>Sistem.</summary>
        public string System { get; }

        /// <summary>Sonuç etiketi.</summary>
        public StatusChip Result { get; }

        /// <summary>Sonuç adı (sıralama için).</summary>
        public string ResultName { get; }

        /// <summary>Tarih (sıralama için).</summary>
        public DateTime Date { get; }

        /// <summary>Tarih metni.</summary>
        public string DateText { get; }

        /// <summary>Kişi.</summary>
        public string Person { get; }

        /// <summary>Kısa açıklama.</summary>
        public string Summary { get; }

        /// <summary>Açılacak not (boş olabilir).</summary>
        public string NotePath { get; }

        /// <summary>Koşu klasörü.</summary>
        public string Folder { get; }
    }

    /// <summary>Genel sekmesinde bir dikkat kartı: ikon, başlık, açıklama, renk.</summary>
    public sealed class AlertItem
    {
        /// <summary>Kartı oluşturur.</summary>
        public AlertItem(string glyph, string title, string detail, string tone)
        {
            Glyph = glyph;
            Title = title;
            Detail = detail;
            Tone = tone;
        }

        /// <summary>İkon.</summary>
        public string Glyph { get; }

        /// <summary>Başlık ("Açık test").</summary>
        public string Title { get; }

        /// <summary>Açıklama ("ÇK (kal.) · SN01 · T1 devam ediyor").</summary>
        public string Detail { get; }

        /// <summary>Renk anlamı.</summary>
        public string Tone { get; }

        /// <summary>Core dikkat maddesini karta çevirir.</summary>
        public static AlertItem From(TestAlert alert)
        {
            switch (alert.Kind)
            {
                case TestAlertKind.Open:
                    return new AlertItem(AppIcons.Clock, Resources.Alert_OpenTitle, RunText(alert.Run!) + " " + Resources.Alert_OpenSuffix, StatusTone.Waiting);
                case TestAlertKind.Failed:
                    return new AlertItem(AppIcons.Error, Resources.Alert_FailedTitle, RunText(alert.Run!), StatusTone.Failed);
                case TestAlertKind.JigRevision:
                    return new AlertItem(AppIcons.Repair, Resources.Alert_JigTitle, alert.Jig + " · Rev " + string.Join(" → ", alert.Revisions), StatusTone.Neutral);
                default:
                    return new AlertItem(AppIcons.Help, Resources.Alert_UnknownTitle, string.Format(Resources.Alert_UnknownDetail, alert.Count), StatusTone.Neutral);
            }
        }

        private static string RunText(TestRun run)
        {
            return TestResultChip.SystemLabel(run.System, run.IsQualification) + " · " + run.SerialNo + " · T" + run.Round;
        }
    }

    /// <summary>Genel sekmesinin üstündeki özet kartı.</summary>
    public sealed class TestKpi
    {
        /// <summary>Kartı oluşturur.</summary>
        public TestKpi(string label, string value, string tone)
        {
            Label = label;
            Value = value;
            Tone = tone;
        }

        /// <summary>Etiket.</summary>
        public string Label { get; }

        /// <summary>Değer.</summary>
        public string Value { get; }

        /// <summary>Değerin rengi (done yeşil, failed kırmızı, diğerleri normal).</summary>
        public string Tone { get; }
    }

    /// <summary>"Son sonuçlar" tablosunun bir satırı: SN ve sistem başına hücreler.</summary>
    public sealed class MatrixRow
    {
        /// <summary>Satırı oluşturur; <paramref name="summary"/> "3 tur · son 07.11.2026".</summary>
        public MatrixRow(string serial, IEnumerable<MatrixCell> cells, string summary)
        {
            Serial = serial;
            Cells = cells.ToList();
            Summary = summary;
            IsPrototype = serial.StartsWith("SNP", StringComparison.OrdinalIgnoreCase);
            KindLabel = IsPrototype ? Resources.Serial_Prototype : Resources.Serial_Production;
        }

        /// <summary>Seri numarası.</summary>
        public string Serial { get; }

        /// <summary>Prototip mi (SNP ile başlar); değilse üretim birimi (SN).</summary>
        public bool IsPrototype { get; }

        /// <summary>"Prototip" / "Üretim".</summary>
        public string KindLabel { get; }

        /// <summary>Tur sayısı ve son test tarihi (ipucu).</summary>
        public string Summary { get; }

        /// <summary>Yalnız tur sayısı ("3 tur").</summary>
        public string RunCountText
        {
            get { return Summary.Split(new[] { " · " }, StringSplitOptions.None)[0]; }
        }

        /// <summary>Hücreler (sütun sırasıyla); ölçülmemişse "—".</summary>
        public List<MatrixCell> Cells { get; }
    }

    /// <summary>Seri numaraları tablosunda bir hücre: o SN'nin o sistemdeki son turu; tıklanınca notu açılır.</summary>
    public sealed class MatrixCell
    {
        /// <summary>Hücreyi kurar (koşu yoksa <paramref name="run"/> boş).</summary>
        public MatrixCell(StatusChip chip, TestRun? run)
        {
            Chip = chip;
            NotePath = run?.NotePath ?? string.Empty;
            Folder = run?.Folder ?? string.Empty;
        }

        /// <summary>Sonuç etiketi ("Geçti · T2").</summary>
        public StatusChip Chip { get; }

        /// <summary>Turun notu; yoksa boş.</summary>
        public string NotePath { get; }

        /// <summary>Turun klasörü; koşu yoksa boş.</summary>
        public string Folder { get; }

        /// <summary>Koşu var mı (tıklanabilir).</summary>
        public bool HasRun
        {
            get { return Folder.Length > 0; }
        }
    }

    /// <summary>
    /// Zaman çizelgesinde bir kart. Kapalıyken tek satır özet; tıklanınca notun metni, plan, jig ve ilk fotoğraf açılır.
    /// </summary>
    public sealed class TimelineItem : ViewModelBase
    {
        private bool isExpanded;

        /// <summary>Koşudan kart oluşturur.</summary>
        public TimelineItem(TestRun run)
        {
            Title = TestResultChip.SystemLabel(run.System, run.IsQualification) + " · " + run.SerialNo + " · T" + run.Round;
            Result = TestResultChip.For(run.Result);
            Summary = run.Summary;
            var date = run.Date == DateTime.MinValue ? "—" : run.Date.ToString(Resources.Format_Date);
            Meta = run.Person.Length == 0 ? date : date + " · " + run.Person;
            var extra = new List<string>();
            if (run.Plan.Length > 0)
            {
                extra.Add(Resources.Timeline_Plan + ": " + run.Plan);
            }

            if (run.Jig.Length > 0)
            {
                extra.Add(Resources.Timeline_Jig + ": " + run.Jig);
            }

            var body = MarkdownText.Simplify(run.Body, run.Summary);
            Detail = string.Join(Environment.NewLine, extra.Concat(body.Length > 0 ? new[] { body } : new string[0]));
            Photo = run.Photo;
            NotePath = run.NotePath;
            Folder = run.Folder;
            SortDate = run.Date;
        }

        /// <summary>Uygulamadan yazılan kısa nottan kart oluşturur; <paramref name="folder"/> stok klasörüdür.</summary>
        public TimelineItem(Pusula.Core.Tracking.QuickNote note, string folder)
        {
            Title = Resources.QuickNote_Title;
            Result = new StatusChip(Resources.QuickNote_Chip, Pusula.Core.Types.StatusTone.Neutral);
            Summary = note.Text;
            var date = note.DateValue.HasValue ? note.DateValue.Value.ToString(Resources.Format_DateTime) : note.Date;
            Meta = note.Person.Length == 0 ? date : date + " · " + note.Person;
            Detail = string.Empty;
            Photo = string.Empty;
            NotePath = string.Empty;
            Folder = folder;
            SortDate = note.DateValue ?? DateTime.MinValue;
            IsQuickNote = true;
        }

        /// <summary>Obsidian olay notundan kart oluşturur (toplantı, karar …); tıklayınca not açılır.</summary>
        public TimelineItem(Pusula.Core.Notes.EventNote note)
        {
            Journal = note.IsJournal ? note : null;
            Title = note.Title;
            Result = new StatusChip(EventKinds.Label(note.Kind), EventKinds.ToneFor(note.Kind, note.Importance));
            Summary = note.Excerpt;
            var date = note.Date.TimeOfDay == TimeSpan.Zero ? note.Date.ToString(Resources.Format_Date) : note.Date.ToString(Resources.Format_DateTime);
            Meta = note.Person.Length == 0 ? date : date + " · " + note.Person;
            Detail = string.Empty;
            Photo = string.Empty;
            NotePath = note.Path;
            Folder = System.IO.Path.GetDirectoryName(note.Path) ?? string.Empty;
            SortDate = note.Date;
            var links = note.Related.Concat(note.Tags.Select(t => "#" + t)).ToList();
            if (links.Count > 0)
            {
                Detail = string.Join(" · ", links);
            }
        }

        /// <summary>Sıralama tarihi (koşu tarihi ya da notun zamanı).</summary>
        public DateTime SortDate { get; }

        /// <summary>Uygulamadan yazılmış kısa not mu (koşu değil).</summary>
        public bool IsQuickNote { get; }

        /// <summary>Notun Günlük bölümündeki olay (düzenlenebilir / silinebilir); değilse <c>null</c>.</summary>
        public Pusula.Core.Notes.EventNote? Journal { get; }

        /// <summary>Günlük olayı mı (sağ tıkla düzenle / sil).</summary>
        public bool IsJournal
        {
            get { return Journal != null; }
        }

        /// <summary>"SATIMO · SN01 · T2".</summary>
        public string Title { get; }

        /// <summary>Sonuç (nokta rengi ve etiket).</summary>
        public StatusChip Result { get; }

        /// <summary>Tek satır özet.</summary>
        public string Summary { get; }

        /// <summary>"07.11.2026 · katan".</summary>
        public string Meta { get; }

        /// <summary>Açılınca görünen metin (plan, jig, notun sadeleştirilmiş gövdesi).</summary>
        public string Detail { get; }

        /// <summary>İlk fotoğraf; yoksa boş.</summary>
        public string Photo { get; }

        /// <summary>Fotoğraf var mı.</summary>
        public bool HasPhoto
        {
            get { return Photo.Length > 0; }
        }

        /// <summary>Açılınca gösterilecek bir şey var mı.</summary>
        public bool HasDetail
        {
            get { return Detail.Length > 0 || HasPhoto; }
        }

        /// <summary>Kart açık mı.</summary>
        public bool IsExpanded
        {
            get { return isExpanded; }
            set { SetProperty(ref isExpanded, value); }
        }

        /// <summary>Açılacak not.</summary>
        public string NotePath { get; }

        /// <summary>Koşu klasörü.</summary>
        public string Folder { get; }
    }

    /// <summary>Notun markdown gövdesini kartta göstermek için sadeleştirir.</summary>
    public static class MarkdownText
    {
        /// <summary>
        /// Başlık işaretlerini, kalın yazıyı, resim gömmelerini ve viki bağlarını sadeleştirir; özetle aynı satırı ve
        /// "Sonuç:" satırını atlar; en çok 10 satır döner.
        /// </summary>
        public static string Simplify(string markdown, string summary)
        {
            var lines = new List<string>();
            foreach (var raw in (markdown ?? string.Empty).Replace("\r\n", "\n").Split('\n'))
            {
                var line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("![[", StringComparison.Ordinal) || line.StartsWith("**Sonuç", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (line.StartsWith("# ", StringComparison.Ordinal))
                {
                    continue;
                }

                line = line.TrimStart('#', ' ');
                line = line.Replace("**", string.Empty).Replace("`", string.Empty);
                line = System.Text.RegularExpressions.Regex.Replace(line, @"\[\[([^\]|]+)(\|[^\]]+)?\]\]", "$1");
                line = line.Replace("- [ ]", "☐").Replace("- [x]", "☑");
                line = System.Text.RegularExpressions.Regex.Replace(line, @"\s#\w+", string.Empty);
                if (line == summary)
                {
                    continue;
                }

                lines.Add(line.StartsWith("- ", StringComparison.Ordinal) ? "• " + line.Substring(2) : line);
                if (lines.Count == 10)
                {
                    break;
                }
            }

            return string.Join(Environment.NewLine, lines);
        }
    }

    /// <summary>Klasörler sekmesindeki ağacın bir düğümü.</summary>
    public sealed class FolderTreeItem : ViewModelBase
    {
        private bool isExpanded;

        /// <summary>Core düğümünden oluşturur; ilk iki düzey açık gelir.</summary>
        public FolderTreeItem(FolderTreeNode node, int depth)
        {
            Name = node.Name;
            Path = node.Path;
            CountText = node.FileCount == 0 ? Resources.Row_Empty : string.Format(Resources.Tree_FileCount, node.FileCount);
            IsDim = node.IsSpecial || node.FileCount == 0;
            isExpanded = depth < 1 && node.FileCount > 0;
            Children = node.Children.Select(c => new FolderTreeItem(c, depth + 1)).ToList();
        }

        /// <summary>Klasör adı.</summary>
        public string Name { get; }

        /// <summary>Tam yol.</summary>
        public string Path { get; }

        /// <summary>"12 dosya" ya da "boş".</summary>
        public string CountText { get; }

        /// <summary>Soluk gösterilir (boş ya da özel klasör).</summary>
        public bool IsDim { get; }

        /// <summary>Açık mı.</summary>
        public bool IsExpanded
        {
            get { return isExpanded; }
            set { SetProperty(ref isExpanded, value); }
        }

        /// <summary>Alt klasörler.</summary>
        public List<FolderTreeItem> Children { get; }
    }
}
