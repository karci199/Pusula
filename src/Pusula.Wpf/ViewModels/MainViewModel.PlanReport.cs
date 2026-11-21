using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using Pusula.Core.Testing;
using Pusula.Wpf.Mvvm;
using Pusula.Wpf.Properties;

namespace Pusula.Wpf.ViewModels
{
    /// <summary>Bağ çipi: plan / rapor / gereksinim numarası ve rengi.</summary>
    public sealed class LinkChip
    {
        /// <summary>Çipi kurar.</summary>
        public LinkChip(string text, int kind)
        {
            Text = text;
            Kind = kind;
        }

        /// <summary>Numara ("4.1", "DGÖ-1", "5.2 Kaldı").</summary>
        public string Text { get; }

        /// <summary>Renk: 0 gereksinim (mavi), 5 plan (mor), 1 geçti, 2 kaldı, 3 hazırlanmadı / sonuçsuz, 4 eksik (sarı uyarı).</summary>
        public int Kind { get; }
    }

    /// <summary>Plan sekmesinde bir satır: plan maddesi, doğruladığı gereksinimler ve bağlı raporlar.</summary>
    public sealed class PlanRow
    {
        /// <summary>Satırı kurar.</summary>
        public PlanRow(PlanItem item, IList<LinkChip> requirements, IList<LinkChip> reports)
        {
            Item = item;
            Requirements = requirements;
            Reports = reports;
        }

        /// <summary>Md'deki plan maddesi.</summary>
        public PlanItem Item { get; }

        /// <summary>TP no.</summary>
        public string TpNo
        {
            get { return Item.TpNo; }
        }

        /// <summary>Açıklama.</summary>
        public string Description
        {
            get { return Item.Description; }
        }

        /// <summary>Not.</summary>
        public string Note
        {
            get { return Item.Note; }
        }

        /// <summary>Not yazılı mı.</summary>
        public bool HasNote
        {
            get { return Item.Note.Length > 0; }
        }

        /// <summary>Yöntem.</summary>
        public string Method
        {
            get { return Item.Method; }
        }

        /// <summary>Yöntemin rengi (bkz. <see cref="VerificationRow.MethodTone"/>).</summary>
        public int MethodTone
        {
            get { return VerificationRow.ToneOfMethod(Item.Method); }
        }

        /// <summary>Altyapı.</summary>
        public string Facility
        {
            get { return Item.Facility; }
        }

        /// <summary>Doğruladığı gereksinimler (yoksa tek sarı "gereksinim yok" çipi).</summary>
        public IList<LinkChip> Requirements { get; }

        /// <summary>Bağlı rapor maddeleri ve sonuçları (yoksa tek sarı "rapor yok" çipi).</summary>
        public IList<LinkChip> Reports { get; }
    }

    /// <summary>Rapor sekmesinde bir satır: rapor maddesi, sonuç, bağlı planlar ve (plan üzerinden) gereksinimler.</summary>
    public sealed class ReportRow
    {
        /// <summary>Satırı kurar.</summary>
        public ReportRow(ReportItem item, IList<LinkChip> plans, IList<LinkChip> requirements)
        {
            Item = item;
            Plans = plans;
            Requirements = requirements;
        }

        /// <summary>Md'deki rapor maddesi.</summary>
        public ReportItem Item { get; }

        /// <summary>TR no.</summary>
        public string TrNo
        {
            get { return Item.TrNo; }
        }

        /// <summary>Açıklama.</summary>
        public string Description
        {
            get { return Item.Description; }
        }

        /// <summary>Not.</summary>
        public string Note
        {
            get { return Item.Note; }
        }

        /// <summary>Not yazılı mı.</summary>
        public bool HasNote
        {
            get { return Item.Note.Length > 0; }
        }

        /// <summary>Sonuç (Geçti · Kaldı · Hazırlanmadı); boşsa "—".</summary>
        public string Result
        {
            get { return Item.Result.Length > 0 ? Item.Result : Resources.Verify_None; }
        }

        /// <summary>Sonucun rengi: 1 geçti, 2 kaldı, 3 diğer / boş.</summary>
        public int ResultKind
        {
            get { return MainViewModel.ResultKindOf(Item.Result); }
        }

        /// <summary>Sonucun yeri.</summary>
        public string ResultPath
        {
            get { return Item.ResultPath; }
        }

        /// <summary>Sonucun yeri yazılı mı.</summary>
        public bool HasResultPath
        {
            get { return Item.ResultPath.Length > 0; }
        }

        /// <summary>Bağlı plan maddeleri (yoksa tek sarı "plan yok" çipi).</summary>
        public IList<LinkChip> Plans { get; }

        /// <summary>Plan üzerinden gereksinimler (hesaplanır).</summary>
        public IList<LinkChip> Requirements { get; }
    }

    /// <summary>Ana pencere: doğrulama sekmesinin Plan ve Rapor görünümleri, bağlar ve kapsama.</summary>
    public sealed partial class MainViewModel
    {
        private List<PlanItem> verificationPlans = new List<PlanItem>();
        private List<ReportItem> verificationReports = new List<ReportItem>();
        private int verificationView;
        private bool showRequirementReports;
        private bool showReportRequirements;

        /// <summary>Plan sekmesinin satırları.</summary>
        public ObservableCollection<PlanRow> PlanRows { get; } = new ObservableCollection<PlanRow>();

        /// <summary>Rapor sekmesinin satırları.</summary>
        public ObservableCollection<ReportRow> ReportRows { get; } = new ObservableCollection<ReportRow>();

        /// <summary>Gereksinimler görünümü seçili mi (varsayılan).</summary>
        public bool IsVerReqView
        {
            get { return verificationView == 0; }
            set { SetVerificationView(value, 0); }
        }

        /// <summary>Plan görünümü seçili mi.</summary>
        public bool IsVerPlanView
        {
            get { return verificationView == 1; }
            set { SetVerificationView(value, 1); }
        }

        /// <summary>Rapor görünümü seçili mi.</summary>
        public bool IsVerReportView
        {
            get { return verificationView == 2; }
            set { SetVerificationView(value, 2); }
        }

        private void SetVerificationView(bool on, int view)
        {
            if (!on || verificationView == view)
            {
                return;
            }

            verificationView = view;
            OnPropertyChanged(nameof(IsVerReqView));
            OnPropertyChanged(nameof(IsVerPlanView));
            OnPropertyChanged(nameof(IsVerReportView));
            OnPropertyChanged(nameof(VerCoverageText));
            OnPropertyChanged(nameof(HasVerCoverageWarning));
        }

        /// <summary>Plan maddesi sayısı (sekme rozeti).</summary>
        public int PlanCount
        {
            get { return verificationPlans.Count; }
        }

        /// <summary>Rapor maddesi sayısı (sekme rozeti).</summary>
        public int ReportCount
        {
            get { return verificationReports.Count; }
        }

        /// <summary>Gereksinimler görünümünde Rapor sütunu açık mı (varsayılan kapalı: bağlı planlar görünür).</summary>
        public bool ShowRequirementReports
        {
            get { return showRequirementReports; }
            set { SetProperty(ref showRequirementReports, value); }
        }

        /// <summary>Rapor görünümünde Gereksinimler sütunu açık mı (varsayılan kapalı: bağlı planlar görünür).</summary>
        public bool ShowReportRequirements
        {
            get { return showReportRequirements; }
            set { SetProperty(ref showReportRequirements, value); }
        }

        /// <summary>Hiçbir plan maddesinde olmayan gereksinimler.</summary>
        public IList<string> RequirementsWithoutPlan { get; private set; } = new List<string>();

        /// <summary>Raporu olmayan plan maddeleri.</summary>
        public IList<string> PlansWithoutReport { get; private set; } = new List<string>();

        /// <summary>Hiçbir plan maddesine bağlı olmayan rapor maddeleri.</summary>
        public IList<string> ReportsWithoutPlan { get; private set; } = new List<string>();

        /// <summary>Seçili görünümün kapsama uyarısı; eksik yoksa boş.</summary>
        public string VerCoverageText
        {
            get
            {
                var parts = new List<string>();
                if (verificationView != 2 && RequirementsWithoutPlan.Count > 0)
                {
                    parts.Add(string.Format(CultureInfo.CurrentCulture, Resources.Verify_CoverageNoPlan, RequirementsWithoutPlan.Count, string.Join(", ", RequirementsWithoutPlan)));
                }

                if (verificationView != 0 && PlansWithoutReport.Count > 0)
                {
                    parts.Add(string.Format(CultureInfo.CurrentCulture, Resources.Verify_CoverageNoReport, PlansWithoutReport.Count, string.Join(", ", PlansWithoutReport)));
                }

                if (verificationView == 2 && ReportsWithoutPlan.Count > 0)
                {
                    parts.Add(string.Format(CultureInfo.CurrentCulture, Resources.Verify_CoverageReportNoPlan, ReportsWithoutPlan.Count, string.Join(", ", ReportsWithoutPlan)));
                }

                return string.Join(" · ", parts);
            }
        }

        /// <summary>Kapsama uyarısı var mı.</summary>
        public bool HasVerCoverageWarning
        {
            get { return VerCoverageText.Length > 0; }
        }

        /// <summary>Sonuç metninin rengi: 1 geçti, 2 kaldı, 3 diğer ya da boş.</summary>
        public static int ResultKindOf(string result)
        {
            if (string.Equals(result, Resources.Verify_StatusPassed, StringComparison.CurrentCultureIgnoreCase))
            {
                return 1;
            }

            return string.Equals(result, Resources.Verify_StatusFailed, StringComparison.CurrentCultureIgnoreCase) ? 2 : 3;
        }

        // Plan ve rapor tablolarını okur (tablo yoksa bir kez gereksinim satırlarından taşınır); satırları ve kapsamayı kurar.
        private void LoadPlanReports()
        {
            if (verificationStock.Length > 0 && PlanReportList.MigrateFromRequirements(verificationStock, verificationItems))
            {
                Status = Resources.Verify_Migrated;
            }

            verificationPlans = verificationStock.Length == 0 ? new List<PlanItem>() : PlanReportList.ReadPlans(verificationStock);
            verificationReports = verificationStock.Length == 0 ? new List<ReportItem>() : PlanReportList.ReadReports(verificationStock);
            var keys = verificationItems.Select(VerificationList.KeyOf).ToList();
            var tps = verificationPlans.Select(p => p.TpNo).ToList();

            RequirementsWithoutPlan = keys.Where(k => !verificationPlans.Any(p => p.Requirements.Contains(k, StringComparer.OrdinalIgnoreCase))).ToList();
            PlansWithoutReport = tps.Where(tp => tp.Length > 0 && !verificationReports.Any(r => r.Plans.Contains(tp, StringComparer.OrdinalIgnoreCase))).ToList();
            ReportsWithoutPlan = verificationReports.Where(r => !r.Plans.Any(p => tps.Contains(p, StringComparer.OrdinalIgnoreCase))).Select(r => r.TrNo).ToList();

            PlanRows.Clear();
            foreach (var plan in verificationPlans)
            {
                var reqs = plan.Requirements.Select(r => new LinkChip(r, keys.Contains(r, StringComparer.OrdinalIgnoreCase) ? 0 : 4)).ToList();
                if (reqs.Count == 0)
                {
                    reqs.Add(new LinkChip(Resources.Verify_NoRequirement, 4));
                }

                var reports = ReportsOf(new[] { plan.TpNo });
                PlanRows.Add(new PlanRow(plan, reqs, reports.Count > 0 ? reports : new List<LinkChip> { new LinkChip(Resources.Verify_NoReport, 4) }));
            }

            ReportRows.Clear();
            foreach (var report in verificationReports)
            {
                var plans = report.Plans.Select(p => new LinkChip(p, tps.Contains(p, StringComparer.OrdinalIgnoreCase) ? 5 : 4)).ToList();
                var reqs = verificationPlans.Where(p => report.Plans.Contains(p.TpNo, StringComparer.OrdinalIgnoreCase))
                    .SelectMany(p => p.Requirements).Distinct(StringComparer.OrdinalIgnoreCase).Select(r => new LinkChip(r, 0)).ToList();
                ReportRows.Add(new ReportRow(report, plans.Count > 0 ? plans : new List<LinkChip> { new LinkChip(Resources.Verify_NoPlan, 4) }, reqs));
            }

            OnPropertyChanged(nameof(PlanCount));
            OnPropertyChanged(nameof(ReportCount));
            OnPropertyChanged(nameof(RequirementsWithoutPlan));
            OnPropertyChanged(nameof(PlansWithoutReport));
            OnPropertyChanged(nameof(ReportsWithoutPlan));
            OnPropertyChanged(nameof(VerCoverageText));
            OnPropertyChanged(nameof(HasVerCoverageWarning));
        }

        // Verilen plan maddelerine bağlı raporlar, sonuçlarıyla çip ("5.1" Geçti rengi).
        private List<LinkChip> ReportsOf(IEnumerable<string> tps)
        {
            var list = tps.ToList();
            return verificationReports.Where(r => r.Plans.Any(p => list.Contains(p, StringComparer.OrdinalIgnoreCase)))
                .Select(r => new LinkChip(r.TrNo, ResultKindOf(r.Result))).ToList();
        }

        /// <summary>Gereksinimin bağlı plan maddeleri (Gereksinimler görünümünde Plan sütunu; yoksa sarı "plan yok").</summary>
        public IList<LinkChip> PlansOfRequirement(VerificationItem item)
        {
            var key = VerificationList.KeyOf(item);
            var chips = verificationPlans.Where(p => p.Requirements.Contains(key, StringComparer.OrdinalIgnoreCase)).Select(p => new LinkChip(p.TpNo, 5)).ToList();
            return chips.Count > 0 ? chips : new List<LinkChip> { new LinkChip(Resources.Verify_NoPlan, 4) };
        }

        /// <summary>Gereksinimin (planları üzerinden) bağlı raporları; plan var rapor yoksa sarı "rapor yok", plan yoksa boş.</summary>
        public IList<LinkChip> ReportsOfRequirement(VerificationItem item)
        {
            var key = VerificationList.KeyOf(item);
            var tps = verificationPlans.Where(p => p.Requirements.Contains(key, StringComparer.OrdinalIgnoreCase)).Select(p => p.TpNo).ToList();
            if (tps.Count == 0)
            {
                return new List<LinkChip>();
            }

            var chips = ReportsOf(tps);
            return chips.Count > 0 ? chips : new List<LinkChip> { new LinkChip(Resources.Verify_NoReport, 4) };
        }
    }
}
