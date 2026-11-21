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
    /// <summary>Bağ süzgecinde bir hap düğme: "Var 6", "Kaldı 1" …</summary>
    public sealed class LinkPill : ViewModelBase
    {
        private readonly Action<LinkPill> changed;
        private bool isChecked;
        private int count;

        /// <summary>Hapı kurar.</summary>
        public LinkPill(int state, string text, int tone, Action<LinkPill> changed)
        {
            State = state;
            Text = text;
            Tone = tone;
            this.changed = changed;
        }

        /// <summary>Bağ durumu: 0 yok, 1 var / geçti, 2 kaldı.</summary>
        public int State { get; }

        /// <summary>Metin.</summary>
        public string Text { get; }

        /// <summary>Nokta rengi: -1 yok, 1 yeşil, 2 kırmızı, 4 sarı.</summary>
        public int Tone { get; }

        /// <summary>Bu duruma uyan madde sayısı.</summary>
        public int Count
        {
            get { return count; }
            set
            {
                if (SetProperty(ref count, value))
                {
                    OnPropertyChanged(nameof(IsZero));
                }
            }
        }

        /// <summary>Sayı sıfır mı (hap soluk).</summary>
        public bool IsZero
        {
            get { return count == 0; }
        }

        /// <summary>Seçili mi.</summary>
        public bool IsChecked
        {
            get { return isChecked; }
            set
            {
                if (SetProperty(ref isChecked, value))
                {
                    changed(this);
                }
            }
        }

        /// <summary>Seçimi süzgeci tetiklemeden değiştirir.</summary>
        public void SetQuietly(bool value)
        {
            if (isChecked != value)
            {
                isChecked = value;
                OnPropertyChanged(nameof(IsChecked));
            }
        }
    }

    /// <summary>Kapsama uyarısının tıklanabilir bir parçası: metin ve uyguladığı süzgeç.</summary>
    public sealed class CoveragePart : ViewModelBase
    {
        private bool isActive;

        /// <summary>Parçayı kurar: <paramref name="first"/> birinci grubun, <paramref name="second"/> ikinci grubun seçimi (-1 seçim yok).</summary>
        public CoveragePart(string text, int first, int second)
        {
            Text = text;
            First = first;
            Second = second;
        }

        /// <summary>Metin.</summary>
        public string Text { get; }

        /// <summary>Birinci grupta yakılacak durum (-1 yok).</summary>
        public int First { get; }

        /// <summary>İkinci grupta yakılacak durum (-1 yok).</summary>
        public int Second { get; }

        /// <summary>Şu anki süzgeç bu parçanınki mi (koyu görünür).</summary>
        public bool IsActive
        {
            get { return isActive; }
            set { SetProperty(ref isActive, value); }
        }
    }

    /// <summary>Ana pencere: doğrulama sekmelerinde bağ süzgeci (hap düğmeler), tıklanabilir kapsama uyarısı ve arama.</summary>
    public sealed partial class MainViewModel
    {
        private List<PlanRow> allPlanRows = new List<PlanRow>();
        private List<ReportRow> allReportRows = new List<ReportRow>();
        private string planListSearch = string.Empty;
        private string reportListSearch = string.Empty;

        /// <summary>Gereksinimler: Test planı [Var · Yok].</summary>
        public ObservableCollection<LinkPill> ReqPlanPills { get; } = new ObservableCollection<LinkPill>();

        /// <summary>Gereksinimler: Test raporu [Geçti · Kaldı · Yok].</summary>
        public ObservableCollection<LinkPill> ReqReportPills { get; } = new ObservableCollection<LinkPill>();

        /// <summary>Test planı: Gereksinim [Var · Yok].</summary>
        public ObservableCollection<LinkPill> PlanReqPills { get; } = new ObservableCollection<LinkPill>();

        /// <summary>Test planı: Test raporu [Geçti · Kaldı · Yok].</summary>
        public ObservableCollection<LinkPill> PlanReportPills { get; } = new ObservableCollection<LinkPill>();

        /// <summary>Test raporu: Test planı [Var · Yok].</summary>
        public ObservableCollection<LinkPill> ReportPlanPills { get; } = new ObservableCollection<LinkPill>();

        /// <summary>Test raporu: Sonuç [Geçti · Kaldı · Yok].</summary>
        public ObservableCollection<LinkPill> ReportResultPills { get; } = new ObservableCollection<LinkPill>();

        /// <summary>Seçili görünümün kapsama uyarısı parçaları (tıklanınca süzer).</summary>
        public ObservableCollection<CoveragePart> CoverageParts { get; } = new ObservableCollection<CoveragePart>();

        /// <summary>Gereksinimlerde bağ süzgeci açık mı (Temizle görünür).</summary>
        public bool HasReqLinkFilter
        {
            get { return AnyChecked(ReqPlanPills) || AnyChecked(ReqReportPills); }
        }

        /// <summary>Test planında süzgeç ya da arama var mı.</summary>
        public bool HasPlanLinkFilter
        {
            get { return AnyChecked(PlanReqPills) || AnyChecked(PlanReportPills) || planListSearch.Trim().Length > 0; }
        }

        /// <summary>Test raporunda süzgeç ya da arama var mı.</summary>
        public bool HasReportLinkFilter
        {
            get { return AnyChecked(ReportPlanPills) || AnyChecked(ReportResultPills) || reportListSearch.Trim().Length > 0; }
        }

        /// <summary>Test planı listesinde arama (TP no, açıklama, gereksinim).</summary>
        public string PlanListSearch
        {
            get { return planListSearch; }
            set
            {
                if (SetProperty(ref planListSearch, value ?? string.Empty))
                {
                    FilterPlanRows();
                }
            }
        }

        /// <summary>Test raporu listesinde arama (TR no, açıklama, plan).</summary>
        public string ReportListSearch
        {
            get { return reportListSearch; }
            set
            {
                if (SetProperty(ref reportListSearch, value ?? string.Empty))
                {
                    FilterReportRows();
                }
            }
        }

        /// <summary>Test planı sekmesinde görünen / toplam: "3 / 7 madde".</summary>
        public string PlanShownText
        {
            get { return string.Format(CultureInfo.CurrentCulture, Resources.Verify_Shown, PlanRows.Count, allPlanRows.Count); }
        }

        /// <summary>Test raporu sekmesinde görünen / toplam.</summary>
        public string ReportShownText
        {
            get { return string.Format(CultureInfo.CurrentCulture, Resources.Verify_Shown, ReportRows.Count, allReportRows.Count); }
        }

        /// <summary>Gereksinimin plan bağı: 1 var, 0 yok.</summary>
        public int ReqPlanState(VerificationItem item)
        {
            var key = VerificationList.KeyOf(item);
            return verificationPlans.Any(p => p.Requirements.Contains(key, StringComparer.OrdinalIgnoreCase)) ? 1 : 0;
        }

        /// <summary>
        /// Gereksinimin rapor durumu (planları üzerinden): 2 bir raporu Kaldı; 1 her planı raporlu ve hepsi Geçti; 0 yok
        /// (rapor yok, sonucu boş ya da Hazırlanmadı, ya da raporsuz planı var).
        /// </summary>
        public int ReqReportState(VerificationItem item)
        {
            var key = VerificationList.KeyOf(item);
            var plans = verificationPlans.Where(p => p.Requirements.Contains(key, StringComparer.OrdinalIgnoreCase)).ToList();
            var tps = plans.Select(p => p.TpNo).ToList();
            var reports = verificationReports.Where(r => r.Plans.Any(p => tps.Contains(p, StringComparer.OrdinalIgnoreCase))).ToList();
            return CombinedResult(reports, plans.All(p => reports.Any(r => r.Plans.Contains(p.TpNo, StringComparer.OrdinalIgnoreCase))));
        }

        /// <summary>Plan maddesinin gereksinim bağı: 1 tabloda olan en az bir gereksinime bağlı, 0 değil.</summary>
        public int PlanReqState(PlanItem plan)
        {
            var keys = verificationItems.Select(VerificationList.KeyOf).ToList();
            return plan.Requirements.Any(r => keys.Contains(r, StringComparer.OrdinalIgnoreCase)) ? 1 : 0;
        }

        /// <summary>Plan maddesinin rapor durumu: 2 bir raporu Kaldı, 1 raporlu ve hepsi Geçti, 0 yok.</summary>
        public int PlanReportState(PlanItem plan)
        {
            return CombinedResult(verificationReports.Where(r => r.Plans.Contains(plan.TpNo, StringComparer.OrdinalIgnoreCase)).ToList(), true);
        }

        /// <summary>Rapor maddesinin plan bağı: 1 tabloda olan en az bir plana bağlı, 0 değil.</summary>
        public int ReportPlanState(ReportItem report)
        {
            return report.Plans.Any(p => verificationPlans.Any(x => string.Equals(x.TpNo, p, StringComparison.OrdinalIgnoreCase))) ? 1 : 0;
        }

        /// <summary>Rapor maddesinin sonucu: 1 Geçti, 2 Kaldı, 0 yok (boş ya da Hazırlanmadı).</summary>
        public static int ReportResultState(ReportItem report)
        {
            var kind = ResultKindOf(report.Result);
            return kind == 3 ? 0 : kind;
        }

        // Birden çok rapordan tek sonuç: biri Kaldı → 2; hepsi Geçti (ve eksik yok) → 1; değilse 0.
        private static int CombinedResult(IList<ReportItem> reports, bool complete)
        {
            var results = reports.Select(ReportResultState).ToList();
            if (results.Contains(2))
            {
                return 2;
            }

            return results.Count > 0 && complete && results.All(r => r == 1) ? 1 : 0;
        }

        /// <summary>Uyarının parçasına tıklanınca: o sekmenin hap düğmeleri parçanınkine ayarlanır.</summary>
        public void ApplyCoveragePart(CoveragePart part)
        {
            if (part == null)
            {
                return;
            }

            var groups = PillGroupsOfView();
            SetPills(groups.Item1, part.First);
            SetPills(groups.Item2, part.Second);
            if (verificationView == 1)
            {
                planListSearch = string.Empty;
                OnPropertyChanged(nameof(PlanListSearch));
            }
            else if (verificationView == 2)
            {
                reportListSearch = string.Empty;
                OnPropertyChanged(nameof(ReportListSearch));
            }

            RefilterView();
        }

        /// <summary>Seçili sekmenin bağ süzgecini ve aramasını temizler.</summary>
        public void ClearLinkFilter()
        {
            var groups = PillGroupsOfView();
            SetPills(groups.Item1, -1);
            SetPills(groups.Item2, -1);
            planListSearch = verificationView == 1 ? string.Empty : planListSearch;
            reportListSearch = verificationView == 2 ? string.Empty : reportListSearch;
            OnPropertyChanged(nameof(PlanListSearch));
            OnPropertyChanged(nameof(ReportListSearch));
            RefilterView();
        }

        private Tuple<ObservableCollection<LinkPill>, ObservableCollection<LinkPill>> PillGroupsOfView()
        {
            if (verificationView == 1)
            {
                return Tuple.Create(PlanReqPills, PlanReportPills);
            }

            return verificationView == 2 ? Tuple.Create(ReportPlanPills, ReportResultPills) : Tuple.Create(ReqPlanPills, ReqReportPills);
        }

        private static void SetPills(IEnumerable<LinkPill> pills, int state)
        {
            foreach (var pill in pills)
            {
                pill.SetQuietly(pill.State == state);
            }
        }

        private void RefilterView()
        {
            if (verificationView == 1)
            {
                FilterPlanRows();
            }
            else if (verificationView == 2)
            {
                FilterReportRows();
            }
            else
            {
                FillVerificationGroups();
            }
        }

        private static bool AnyChecked(IEnumerable<LinkPill> pills)
        {
            return pills.Any(p => p.IsChecked);
        }

        // Grupta seçim yoksa hepsi geçer; varsa durum seçililerden biri olmalı.
        private static bool PassGroup(IEnumerable<LinkPill> pills, int state)
        {
            var on = pills.Where(p => p.IsChecked).ToList();
            return on.Count == 0 || on.Any(p => p.State == state);
        }

        /// <summary>Gereksinim bağ süzgecinden geçiyor mu.</summary>
        public bool PassesReqLinks(VerificationItem item)
        {
            return PassGroup(ReqPlanPills, ReqPlanState(item)) && PassGroup(ReqReportPills, ReqReportState(item));
        }

        // Hap düğmeler bir kez kurulur; sayılar her yüklemede güncellenir, seçimler stok değişince sıfırlanır.
        private void EnsurePills()
        {
            if (ReqPlanPills.Count > 0)
            {
                return;
            }

            AddHave(ReqPlanPills, OnReqPill);
            AddResults(ReqReportPills, OnReqPill);
            AddHave(PlanReqPills, OnPlanPill);
            AddResults(PlanReportPills, OnPlanPill);
            AddHave(ReportPlanPills, OnReportPill);
            AddResults(ReportResultPills, OnReportPill);
        }

        private static void AddHave(ObservableCollection<LinkPill> pills, Action<LinkPill> changed)
        {
            pills.Add(new LinkPill(1, Resources.Verify_LinkHave, -1, changed));
            pills.Add(new LinkPill(0, Resources.Verify_LinkNone, 4, changed));
        }

        private static void AddResults(ObservableCollection<LinkPill> pills, Action<LinkPill> changed)
        {
            pills.Add(new LinkPill(1, Resources.Verify_StatusPassed, 1, changed));
            pills.Add(new LinkPill(2, Resources.Verify_StatusFailed, 2, changed));
            pills.Add(new LinkPill(0, Resources.Verify_LinkNone, 4, changed));
        }

        private void OnReqPill(LinkPill pill)
        {
            FillVerificationGroups();
        }

        private void OnPlanPill(LinkPill pill)
        {
            FilterPlanRows();
        }

        private void OnReportPill(LinkPill pill)
        {
            FilterReportRows();
        }

        // Yeni stokta bütün bağ süzgeçleri ve aramalar temizlenir.
        private void ResetLinkFilters()
        {
            EnsurePills();
            foreach (var group in new[] { ReqPlanPills, ReqReportPills, PlanReqPills, PlanReportPills, ReportPlanPills, ReportResultPills })
            {
                SetPills(group, -1);
            }

            planListSearch = string.Empty;
            reportListSearch = string.Empty;
            OnPropertyChanged(nameof(PlanListSearch));
            OnPropertyChanged(nameof(ReportListSearch));
        }

        // Hap sayıları ve uyarı parçaları (LoadPlanReports sonunda).
        private void UpdateLinkCounts()
        {
            EnsurePills();
            var reqPlan = verificationItems.Select(ReqPlanState).ToList();
            var reqReport = verificationItems.Select(ReqReportState).ToList();
            var planReq = verificationPlans.Select(PlanReqState).ToList();
            var planReport = verificationPlans.Select(PlanReportState).ToList();
            var reportPlan = verificationReports.Select(ReportPlanState).ToList();
            var reportResult = verificationReports.Select(ReportResultState).ToList();
            Count(ReqPlanPills, reqPlan);
            Count(ReqReportPills, reqReport);
            Count(PlanReqPills, planReq);
            Count(PlanReportPills, planReport);
            Count(ReportPlanPills, reportPlan);
            Count(ReportResultPills, reportResult);

            RequirementsWithoutReport = verificationItems.Where(i => ReqPlanState(i) == 1 && ReqReportState(i) == 0).Select(VerificationList.KeyOf).ToList();
            PlansWithoutRequirement = verificationPlans.Where(p => PlanReqState(p) == 0).Select(p => p.TpNo).ToList();
            PlansWithoutResult = verificationPlans.Where(p => PlanReportState(p) == 0).Select(p => p.TpNo).ToList();
            ReportsWithoutResult = verificationReports.Where(r => ReportResultState(r) == 0).Select(r => r.TrNo).ToList();
            BuildCoverageParts();
        }

        private static void Count(IEnumerable<LinkPill> pills, IList<int> states)
        {
            foreach (var pill in pills)
            {
                pill.Count = states.Count(s => s == pill.State);
            }
        }

        /// <summary>Planı var ama raporu yok (ya da sonuçsuz) gereksinimler.</summary>
        public IList<string> RequirementsWithoutReport { get; private set; } = new List<string>();

        /// <summary>Hiçbir gereksinime bağlı olmayan plan maddeleri.</summary>
        public IList<string> PlansWithoutRequirement { get; private set; } = new List<string>();

        /// <summary>Raporu olmayan ya da sonuçsuz plan maddeleri.</summary>
        public IList<string> PlansWithoutResult { get; private set; } = new List<string>();

        /// <summary>Sonucu boş ya da Hazırlanmadı olan rapor maddeleri.</summary>
        public IList<string> ReportsWithoutResult { get; private set; } = new List<string>();

        // Seçili görünüme göre uyarı parçaları.
        private void BuildCoverageParts()
        {
            CoverageParts.Clear();
            if (verificationView == 1)
            {
                AddPart(PlansWithoutRequirement, Resources.Verify_CoveragePlanNoReq, 0, -1);
                AddPart(PlansWithoutResult, Resources.Verify_CoverageNoReport, -1, 0);
            }
            else if (verificationView == 2)
            {
                AddPart(ReportsWithoutPlan, Resources.Verify_CoverageReportNoPlan, 0, -1);
                AddPart(ReportsWithoutResult, Resources.Verify_CoverageReportNoResult, -1, 0);
            }
            else
            {
                AddPart(RequirementsWithoutPlan, Resources.Verify_CoverageNoPlan, 0, -1);
                AddPart(RequirementsWithoutReport, Resources.Verify_CoverageReqNoReport, 1, 0);
            }

            MarkActiveParts();
            OnPropertyChanged(nameof(VerCoverageText));
            OnPropertyChanged(nameof(HasVerCoverageWarning));
        }

        private void AddPart(IList<string> keys, string format, int first, int second)
        {
            if (keys.Count > 0)
            {
                CoverageParts.Add(new CoveragePart(string.Format(CultureInfo.CurrentCulture, format, keys.Count, string.Join(", ", keys)), first, second));
            }
        }

        // Uyarı parçası, süzgeç tam onunkiyse koyu görünür.
        private void MarkActiveParts()
        {
            var groups = PillGroupsOfView();
            foreach (var part in CoverageParts)
            {
                part.IsActive = Matches(groups.Item1, part.First) && Matches(groups.Item2, part.Second);
            }

            OnPropertyChanged(nameof(HasReqLinkFilter));
            OnPropertyChanged(nameof(HasPlanLinkFilter));
            OnPropertyChanged(nameof(HasReportLinkFilter));
        }

        private static bool Matches(IEnumerable<LinkPill> pills, int state)
        {
            var on = pills.Where(p => p.IsChecked).Select(p => p.State).ToList();
            return state < 0 ? on.Count == 0 : on.Count == 1 && on[0] == state;
        }

        // Test planı satırları: arama + iki grup.
        private void FilterPlanRows()
        {
            var text = planListSearch.Trim();
            PlanRows.Clear();
            foreach (var row in allPlanRows)
            {
                var item = row.Item;
                if (text.Length > 0
                    && item.TpNo.IndexOf(text, StringComparison.CurrentCultureIgnoreCase) < 0
                    && item.Description.IndexOf(text, StringComparison.CurrentCultureIgnoreCase) < 0
                    && !item.Requirements.Any(r => r.IndexOf(text, StringComparison.CurrentCultureIgnoreCase) >= 0))
                {
                    continue;
                }

                if (PassGroup(PlanReqPills, PlanReqState(item)) && PassGroup(PlanReportPills, PlanReportState(item)))
                {
                    PlanRows.Add(row);
                }
            }

            MarkActiveParts();
            OnPropertyChanged(nameof(PlanShownText));
        }

        // Test raporu satırları: arama + iki grup.
        private void FilterReportRows()
        {
            var text = reportListSearch.Trim();
            ReportRows.Clear();
            foreach (var row in allReportRows)
            {
                var item = row.Item;
                if (text.Length > 0
                    && item.TrNo.IndexOf(text, StringComparison.CurrentCultureIgnoreCase) < 0
                    && item.Description.IndexOf(text, StringComparison.CurrentCultureIgnoreCase) < 0
                    && !item.Plans.Any(p => p.IndexOf(text, StringComparison.CurrentCultureIgnoreCase) >= 0))
                {
                    continue;
                }

                if (PassGroup(ReportPlanPills, ReportPlanState(item)) && PassGroup(ReportResultPills, ReportResultState(item)))
                {
                    ReportRows.Add(row);
                }
            }

            MarkActiveParts();
            OnPropertyChanged(nameof(ReportShownText));
        }
    }
}
