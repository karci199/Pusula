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

    /// <summary>Genel sekmesinde bir dikkat maddesi.</summary>
    public sealed class AlertItem
    {
        /// <summary>Maddeyi oluşturur.</summary>
        public AlertItem(string text, string tone)
        {
            Text = text;
            Tone = tone;
        }

        /// <summary>Metin.</summary>
        public string Text { get; }

        /// <summary>Renk anlamı.</summary>
        public string Tone { get; }

        /// <summary>Core dikkat maddesini metne çevirir.</summary>
        public static AlertItem From(TestAlert alert)
        {
            switch (alert.Kind)
            {
                case TestAlertKind.Open:
                    return new AlertItem(string.Format(Resources.Alert_Open, TestResultChip.SystemLabel(alert.Run!.System, alert.Run.IsQualification), alert.Run.SerialNo, alert.Run.Round), StatusTone.Waiting);
                case TestAlertKind.Failed:
                    return new AlertItem(string.Format(Resources.Alert_Failed, TestResultChip.SystemLabel(alert.Run!.System, alert.Run.IsQualification), alert.Run.SerialNo, alert.Run.Round), StatusTone.Failed);
                case TestAlertKind.JigRevision:
                    return new AlertItem(string.Format(Resources.Alert_JigRevision, alert.Jig, string.Join(" → ", alert.Revisions)), StatusTone.Neutral);
                default:
                    return new AlertItem(string.Format(Resources.Alert_Unknown, alert.Count), StatusTone.Neutral);
            }
        }
    }

    /// <summary>"Son sonuçlar" tablosunun bir satırı: SN ve sistem başına hücreler.</summary>
    public sealed class MatrixRow
    {
        /// <summary>Satırı oluşturur.</summary>
        public MatrixRow(string serial, IEnumerable<StatusChip> cells)
        {
            Serial = serial;
            Cells = cells.ToList();
        }

        /// <summary>Seri numarası.</summary>
        public string Serial { get; }

        /// <summary>Hücreler (sütun sırasıyla); ölçülmemişse "—".</summary>
        public List<StatusChip> Cells { get; }
    }

    /// <summary>Zaman çizelgesinde bir satır.</summary>
    public sealed class TimelineItem
    {
        /// <summary>Koşudan satır oluşturur.</summary>
        public TimelineItem(TestRun run)
        {
            DateText = run.Date == DateTime.MinValue ? "—" : Formats.ShortDate(run.Date.Date);
            Title = TestResultChip.SystemLabel(run.System, run.IsQualification) + " · " + run.SerialNo + " · T" + run.Round;
            Result = TestResultChip.For(run.Result);
            Summary = run.Summary;
            NotePath = run.NotePath;
            Folder = run.Folder;
        }

        /// <summary>Tarih.</summary>
        public string DateText { get; }

        /// <summary>"SATIMO · SN01 · T2".</summary>
        public string Title { get; }

        /// <summary>Sonuç (nokta rengi ve etiket).</summary>
        public StatusChip Result { get; }

        /// <summary>Kısa açıklama.</summary>
        public string Summary { get; }

        /// <summary>Açılacak not.</summary>
        public string NotePath { get; }

        /// <summary>Koşu klasörü.</summary>
        public string Folder { get; }
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
