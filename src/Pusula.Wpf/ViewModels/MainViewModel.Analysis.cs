using System;
using Pusula.Core.Testing;
using Pusula.Wpf.Properties;

namespace Pusula.Wpf.ViewModels
{
    /// <summary>Test koşusunun analiz tarihi ve kısa notu (test.md YAML'ına yazılır).</summary>
    public sealed partial class MainViewModel
    {
        private bool isAnalysisOpen;
        private string analysisTarget = string.Empty;
        private string analysisDateText = string.Empty;
        private string analysisNote = string.Empty;
        private string analysisError = string.Empty;
        private TestRunRow? analysisRun;

        /// <summary>Analiz penceresi açık mı.</summary>
        public bool IsAnalysisOpen
        {
            get { return isAnalysisOpen; }
            set { SetDialogOpen(ref isAnalysisOpen, value); }
        }

        /// <summary>Alt başlık: "SN01 · SATIMO · T2 · test 03.11.2026".</summary>
        public string AnalysisTarget
        {
            get { return analysisTarget; }
            private set { SetProperty(ref analysisTarget, value); }
        }

        /// <summary>Analiz tarihi (gg.aa.yyyy); başta test tarihi.</summary>
        public string AnalysisDateText
        {
            get { return analysisDateText; }
            set
            {
                if (SetProperty(ref analysisDateText, value ?? string.Empty))
                {
                    AnalysisError = string.Empty;
                }
            }
        }

        /// <summary>Kısa analiz notu.</summary>
        public string AnalysisNote
        {
            get { return analysisNote; }
            set { SetProperty(ref analysisNote, value ?? string.Empty); }
        }

        /// <summary>Hata (tarih okunamadı, yazılamadı).</summary>
        public string AnalysisError
        {
            get { return analysisError; }
            private set { SetProperty(ref analysisError, value); }
        }

        /// <summary>Koşunun analiz penceresini açar (tarih başta test tarihi ya da kayıtlı analiz tarihi).</summary>
        public bool OpenAnalysis(TestRunRow run)
        {
            if (run == null || run.Folder.Length == 0)
            {
                return false;
            }

            analysisRun = run;
            AnalysisTarget = string.Format(Resources.Analysis_Target, run.Title, run.DateText);
            var date = run.AnalysisDate != DateTime.MinValue ? run.AnalysisDate : run.Date;
            AnalysisDateText = date == DateTime.MinValue ? string.Empty : date.ToString("dd.MM.yyyy", System.Globalization.CultureInfo.InvariantCulture);
            AnalysisNote = run.AnalysisNote;
            AnalysisError = string.Empty;
            IsAnalysisOpen = true;
            return true;
        }

        /// <summary>Kaydeder: tarih boşsa ya da test günüyse ayrı analiz tarihi kaldırılır.</summary>
        public bool ConfirmAnalysis()
        {
            var run = analysisRun;
            if (run == null)
            {
                IsAnalysisOpen = false;
                return false;
            }

            DateTime? date = null;
            if (AnalysisDateText.Trim().Length > 0)
            {
                date = ParseDate(AnalysisDateText);
                if (!date.HasValue)
                {
                    AnalysisError = Resources.Analysis_BadDate;
                    return false;
                }
            }

            var error = TestRunReader.SetAnalysis(run.Folder, run.Date, date, AnalysisNote);
            if (error.Length > 0)
            {
                AnalysisError = string.Format(Resources.Error_Io, error);
                Status = AnalysisError;
                return false;
            }

            IsAnalysisOpen = false;
            analysisRun = null;
            Status = string.Format(Resources.Analysis_Saved, run.Title);
            RefreshShowing(selectedNode?.Path ?? run.Folder);
            return true;
        }
    }
}
