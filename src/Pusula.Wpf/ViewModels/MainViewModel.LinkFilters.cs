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

    /// <summary>Bağ açılır kutusu: iki başlıklı çoklu seçim (ör. Test planı: Var/Yok, Test raporu: Geçti/Kaldı/Yok).</summary>
    public sealed class LinkFilterBox : ViewModelBase
    {
        /// <summary>Kutuyu iki grupla kurar.</summary>
        public LinkFilterBox(string firstTitle, ObservableCollection<LinkPill> first, string secondTitle, ObservableCollection<LinkPill> second)
        {
            FirstTitle = firstTitle;
            First = first;
            SecondTitle = secondTitle;
            Second = second;
        }

        /// <summary>Birinci grubun başlığı.</summary>
        public string FirstTitle { get; }

        /// <summary>Birinci grubun seçenekleri.</summary>
        public ObservableCollection<LinkPill> First { get; }

        /// <summary>İkinci grubun başlığı.</summary>
        public string SecondTitle { get; }

        /// <summary>İkinci grubun seçenekleri.</summary>
        public ObservableCollection<LinkPill> Second { get; }

        /// <summary>Seçim var mı (kutu vurgulu).</summary>
        public bool IsActive
        {
            get { return First.Concat(Second).Any(p => p.IsChecked); }
        }

        /// <summary>Kapalı kutunun yazısı: "Hepsi", tek seçimde "Test planı: Yok", birden çokta "2 seçim".</summary>
        public string Text
        {
            get
            {
                var on = First.Where(p => p.IsChecked).Select(p => FirstTitle + ": " + p.Text)
                    .Concat(Second.Where(p => p.IsChecked).Select(p => SecondTitle + ": " + p.Text)).ToList();
                if (on.Count == 0)
                {
                    return Resources.Home_RangeAll;
                }

                return on.Count == 1 ? on[0] : string.Format(CultureInfo.CurrentCulture, Resources.Verify_LinkSelections, on.Count);
            }
        }

        /// <summary>Yazıyı ve vurguyu yeniler.</summary>
        public void Refresh()
        {
            OnPropertyChanged(nameof(Text));
            OnPropertyChanged(nameof(IsActive));
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
        private string requirementSearch = string.Empty;
        private string planSort = string.Empty;
        private string reportSort = string.Empty;
        private LinkFilterBox? reqLinkBox;
        private LinkFilterBox? planLinkBox;
        private LinkFilterBox? reportLinkBox;

        /// <summary>Gereksinimler: Bağ kutusu (Test planı · Test raporu).</summary>
        public LinkFilterBox ReqLinkBox
        {
            get { return reqLinkBox ?? (reqLinkBox = new LinkFilterBox(Resources.Verify_PillPlan, ReqPlanPills, Resources.Verify_PillReport, ReqReportPills)); }
        }

        /// <summary>Test planı: Bağ kutusu (Gereksinim · Test raporu).</summary>
        public LinkFilterBox PlanLinkBox
        {
            get { return planLinkBox ?? (planLinkBox = new LinkFilterBox(Resources.Verify_PillRequirement, PlanReqPills, Resources.Verify_PillReport, PlanReportPills)); }
        }

        /// <summary>Test raporu: Bağ kutusu (Test planı · Sonuç).</summary>
        public LinkFilterBox ReportLinkBox
        {
            get { return reportLinkBox ?? (reportLinkBox = new LinkFilterBox(Resources.Verify_PillPlan, ReportPlanPills, Resources.Verify_PillResult, ReportResultPills)); }
        }

        /// <summary>Gereksinimlerde arama (TG no, başlık, kısa not, alt grup).</summary>
        public string RequirementSearch
        {
            get { return requirementSearch; }
            set
            {
                if (SetProperty(ref requirementSearch, value ?? string.Empty))
                {
                    FillVerificationGroups();
                }
            }
        }

        /// <summary>Test planı sıralama seçenekleri: TP no · Bağ durumu.</summary>
        public IList<string> PlanSorts
        {
            get { return new[] { Resources.Verify_SortTp, Resources.Verify_SortLink }; }
        }

        /// <summary>Test planı sıralaması.</summary>
        public string PlanSort
        {
            get { return planSort.Length == 0 ? Resources.Verify_SortTp : planSort; }
            set
            {
                if (!string.IsNullOrEmpty(value) && value != PlanSort)
                {
                    planSort = value;
                    OnPropertyChanged();
                    FilterPlanRows();
                }
            }
        }

        /// <summary>Test raporu sıralama seçenekleri: TR no · Sonuç.</summary>
        public IList<string> ReportSorts
        {
            get { return new[] { Resources.Verify_SortTr, Resources.Verify_SortResult }; }
        }

        /// <summary>Test raporu sıralaması.</summary>
        public string ReportSort
        {
            get { return reportSort.Length == 0 ? Resources.Verify_SortTr : reportSort; }
            set
            {
                if (!string.IsNullOrEmpty(value) && value != ReportSort)
                {
                    reportSort = value;
                    OnPropertyChanged();
                    FilterReportRows();
                }
            }
        }

        /// <summary>Gereksinim aramadan geçiyor mu.</summary>
        public bool PassesRequirementSearch(VerificationItem item)
        {
            var text = requirementSearch.Trim();
            return text.Length == 0
                || VerificationList.KeyOf(item).IndexOf(text, StringComparison.CurrentCultureIgnoreCase) >= 0
                || item.Text.IndexOf(text, StringComparison.CurrentCultureIgnoreCase) >= 0
                || item.Description.IndexOf(text, StringComparison.CurrentCultureIgnoreCase) >= 0
                || item.SubGroup.IndexOf(text, StringComparison.CurrentCultureIgnoreCase) >= 0;
        }

        // Kapsam dışı gereksinim ve 261 ile doğrulanan gereksinim (25.11.2026) bağ sayılarına, plan / rapor uyarısına ve bağ süzgecine girmez.
        private static bool IsOutOfScope(VerificationItem item)
        {
            return VerificationRow.KindOf(item.EffectiveStatus) == 5 || IsDocumentVerified(item);
        }

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
            get { return AnyChecked(ReqPlanPills) || AnyChecked(ReqReportPills) || requirementSearch.Trim().Length > 0 || HasVerificationStatusFilter; }
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
        /// Gereksinimin rapor durumu (planları üzerinden): 2 bir raporu Kaldı; 1 her planı raporlu ve hepsi Geçti; 3 her planı
        /// raporlu ama bir raporun sonucu girilmemiş (boş ya da Hazırlanmadı); 0 yok (rapor yok ya da raporsuz planı var).
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

        /// <summary>Plan maddesinin rapor durumu: 2 bir raporu Kaldı, 1 raporlu ve hepsi Geçti, 3 raporlu ama sonucu girilmemiş, 0 raporu yok.</summary>
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

        // Birden çok rapordan tek sonuç: biri Kaldı → 2; rapor yok ya da raporsuz plan var → 0; hepsi Geçti → 1; değilse
        // (raporlu ama sonucu girilmemiş) → 3.
        private static int CombinedResult(IList<ReportItem> reports, bool complete)
        {
            var results = reports.Select(ReportResultState).ToList();
            if (results.Contains(2))
            {
                return 2;
            }

            if (results.Count == 0 || !complete)
            {
                return 0;
            }

            return results.All(r => r == 1) ? 1 : 3;
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
            if (verificationView == 0)
            {
                requirementSearch = string.Empty;
                OnPropertyChanged(nameof(RequirementSearch));
                verificationStatusFilter.Clear();
                BuildStatusFilters();
            }

            RefilterView();
        }

        /// <summary>Bağ kutusundaki "Temizle": yalnız o kutunun seçimleri kalkar (arama ve durum kalır).</summary>
        public void ClearLinkBox(LinkFilterBox box)
        {
            if (box == null)
            {
                return;
            }

            SetPills(box.First, -1);
            SetPills(box.Second, -1);
            if (box == PlanLinkBox)
            {
                FilterPlanRows();
            }
            else if (box == ReportLinkBox)
            {
                FilterReportRows();
            }
            else
            {
                FillVerificationGroups();
            }
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
            if (IsOutOfScope(item) && (AnyChecked(ReqPlanPills) || AnyChecked(ReqReportPills)))
            {
                return false;
            }

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
            ReqReportPills.Insert(2, new LinkPill(3, Resources.Verify_PillNoResult, 4, OnReqPill));
            AddHave(PlanReqPills, OnPlanPill);
            AddResults(PlanReportPills, OnPlanPill);
            PlanReportPills.Insert(2, new LinkPill(3, Resources.Verify_PillNoResult, 4, OnPlanPill));
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
            requirementSearch = string.Empty;
            OnPropertyChanged(nameof(PlanListSearch));
            OnPropertyChanged(nameof(ReportListSearch));
            OnPropertyChanged(nameof(RequirementSearch));
        }

        // Hap sayıları ve uyarı parçaları (LoadPlanReports sonunda).
        private void UpdateLinkCounts()
        {
            EnsurePills();
            var inScope = verificationItems.Where(i => !IsOutOfScope(i)).ToList();
            var reqPlan = inScope.Select(ReqPlanState).ToList();
            var reqReport = inScope.Select(ReqReportState).ToList();
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

            RequirementsWithoutReport = inScope.Where(i => ReqPlanState(i) == 1 && ReqReportState(i) == 0).Select(VerificationList.KeyOf).ToList();
            RequirementsWithoutResult = inScope.Where(i => ReqReportState(i) == 3).Select(VerificationList.KeyOf).ToList();
            PlansWithoutResultValue = verificationPlans.Where(p => PlanReportState(p) == 3).Select(p => p.TpNo).ToList();
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

        /// <summary>Planı var ama raporu olmayan (en az bir planı raporsuz) gereksinimler.</summary>
        public IList<string> RequirementsWithoutReport { get; private set; } = new List<string>();

        /// <summary>Her planı raporlu ama bir raporunun sonucu girilmemiş gereksinimler.</summary>
        public IList<string> RequirementsWithoutResult { get; private set; } = new List<string>();

        /// <summary>Raporu olan ama sonucu girilmemiş plan maddeleri.</summary>
        public IList<string> PlansWithoutResultValue { get; private set; } = new List<string>();

        /// <summary>Hiçbir gereksinime bağlı olmayan plan maddeleri.</summary>
        public IList<string> PlansWithoutRequirement { get; private set; } = new List<string>();

        /// <summary>Raporu olmayan plan maddeleri.</summary>
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
                AddPart(PlansWithoutResultValue, Resources.Verify_CoveragePlanNoResult, -1, 3);
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
                AddPart(RequirementsWithoutResult, Resources.Verify_CoverageReqNoResult, 1, 3);
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
            ReqLinkBox.Refresh();
            PlanLinkBox.Refresh();
            ReportLinkBox.Refresh();
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
            IEnumerable<PlanRow> ordered = allPlanRows;
            if (PlanSort == Resources.Verify_SortLink)
            {
                // Önce bağı eksikler (sarı), sonra Kaldı (kırmızı), sonra tamam olanlar; kendi içinde TP no.
                ordered = allPlanRows.OrderBy(r => r.Stripe == 4 ? 0 : (r.Stripe == 2 ? 1 : 2)).ThenBy(r => r.TpNo, Comparer<string>.Create(PlanReportList.CompareNo));
            }

            foreach (var row in ordered)
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
            IEnumerable<ReportRow> ordered = allReportRows;
            if (ReportSort == Resources.Verify_SortResult)
            {
                // Önce Kaldı, sonra sonuçsuz, sonra Geçti; kendi içinde TR no.
                ordered = allReportRows.OrderBy(r => ReportResultState(r.Item) == 2 ? 0 : (ReportResultState(r.Item) == 0 ? 1 : 2)).ThenBy(r => r.TrNo, Comparer<string>.Create(PlanReportList.CompareNo));
            }

            foreach (var row in ordered)
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
