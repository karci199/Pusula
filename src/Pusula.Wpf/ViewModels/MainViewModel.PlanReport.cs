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

    /// <summary>Plan ayrıntısında bağlı gereksinim: numara, başlık, kısa not ve durum.</summary>
    public sealed class PlanRequirementDetail
    {
        /// <summary>Ayrıntıyı kurar.</summary>
        public PlanRequirementDetail(string key, VerificationItem? item)
        {
            Key = key;
            Text = item != null ? (item.Text.Length > 0 ? item.Text : item.Requirement) : Resources.Verify_LinkMissing;
            Description = item != null ? (item.Text.Length > 0 ? item.Description : string.Empty) : string.Empty;
            var status = item != null ? item.EffectiveStatus : string.Empty;
            StatusText = status.Length > 0 ? status : Resources.Verify_StatusPending;
            StatusKind = VerificationRow.KindOf(status);
        }

        /// <summary>Gereksinim no (TG no).</summary>
        public string Key { get; }

        /// <summary>Başlık (gereksinim bulunamazsa uyarı).</summary>
        public string Text { get; }

        /// <summary>Kısa not (ölçüt).</summary>
        public string Description { get; }

        /// <summary>Durum.</summary>
        public string StatusText { get; }

        /// <summary>Durum türü (renk).</summary>
        public int StatusKind { get; }
    }

    /// <summary>Plan sekmesinde bir satır: plan maddesi, doğruladığı gereksinimler ve bağlı raporlar; tıklanınca ayrıntı açılır.</summary>
    public sealed class PlanRow : ViewModelBase
    {
        private bool isExpanded;

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

        /// <summary>Satırın sol şeridi: 0 yok, 2 kırmızı (raporu Kaldı), 4 sarı (bağı eksik).</summary>
        public int Stripe { get; set; }

        /// <summary>Ayrıntı açık mı.</summary>
        public bool IsExpanded
        {
            get { return isExpanded; }
            set { SetProperty(ref isExpanded, value); }
        }

        /// <summary>Ayrıntı: bağlı gereksinimler (numara, başlık, kısa not, durum).</summary>
        public IList<PlanRequirementDetail> RequirementDetails { get; set; } = new List<PlanRequirementDetail>();

        /// <summary>Ayrıntı: bağlı rapor maddeleri (sonuç, tarih, birim, klasör).</summary>
        public IList<ReportRow> ReportDetails { get; set; } = new List<ReportRow>();

        /// <summary>Ayrıntı: md'de "### TP no" başlığı altına elle yazılan metin (salt okunur).</summary>
        public string DetailText { get; set; } = string.Empty;

        /// <summary>Elle yazılmış ayrıntı var mı.</summary>
        public bool HasDetailText
        {
            get { return DetailText.Length > 0; }
        }

        /// <summary>Bağlı gereksinimlerin standartları ("810G 509.5, 461G RE102"); planda ayrı seçilmez, gereksinimden okunur.</summary>
        public string StandardText { get; set; } = string.Empty;

        /// <summary>Standart çipi görünür mü.</summary>
        public bool HasStandard
        {
            get { return StandardText.Length > 0; }
        }

        /// <summary>Bağlı rapor var mı.</summary>
        public bool HasReportDetails
        {
            get { return ReportDetails.Count > 0; }
        }
    }

    /// <summary>Rapor ayrıntısında bağlı plan: numara, açıklama, yöntem · altyapı.</summary>
    public sealed class ReportPlanDetail
    {
        /// <summary>Ayrıntıyı kurar (plan tabloda yoksa uyarı metni).</summary>
        public ReportPlanDetail(string key, PlanItem? plan)
        {
            Key = key;
            Text = plan != null ? (plan.Description.Length > 0 ? plan.Description : Resources.Verify_None) : Resources.Verify_LinkMissing;
            Method = plan == null ? string.Empty : string.Join(" · ", new[] { plan.Method, plan.Facility }.Where(s => s.Length > 0));
        }

        /// <summary>TP no.</summary>
        public string Key { get; }

        /// <summary>Açıklama.</summary>
        public string Text { get; }

        /// <summary>Yöntem · altyapı.</summary>
        public string Method { get; }
    }

    /// <summary>Rapor sekmesinde bir satır: rapor maddesi, sonuç, bağlı planlar ve (plan üzerinden) gereksinimler; tıklanınca ayrıntı açılır.</summary>
    public sealed class ReportRow : ViewModelBase
    {
        private bool isExpanded;

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

        /// <summary>Tarih.</summary>
        public string Date
        {
            get { return Item.Date; }
        }

        /// <summary>Birim (seri no).</summary>
        public string Unit
        {
            get { return Item.Unit; }
        }

        /// <summary>Bağlı plan maddeleri (yoksa tek sarı "plan yok" çipi).</summary>
        public IList<LinkChip> Plans { get; }

        /// <summary>Plan üzerinden gereksinimler (hesaplanır).</summary>
        public IList<LinkChip> Requirements { get; }

        /// <summary>Ayrıntı açık mı.</summary>
        public bool IsExpanded
        {
            get { return isExpanded; }
            set { SetProperty(ref isExpanded, value); }
        }

        /// <summary>Ayrıntı: bağlı planlar (numara, açıklama, yöntem · altyapı).</summary>
        public IList<ReportPlanDetail> PlanDetails { get; set; } = new List<ReportPlanDetail>();

        /// <summary>Ayrıntı: planlar üzerinden gereksinimler (numara, başlık ya da metin, durum).</summary>
        public IList<PlanRequirementDetail> RequirementDetails { get; set; } = new List<PlanRequirementDetail>();

        /// <summary>Ayrıntı: md'de "### TR no" başlığı altına elle yazılan metin (salt okunur).</summary>
        public string DetailText { get; set; } = string.Empty;

        /// <summary>Elle yazılmış ayrıntı var mı.</summary>
        public bool HasDetailText
        {
            get { return DetailText.Length > 0; }
        }

        /// <summary>Bağlı plan var mı.</summary>
        public bool HasPlanDetails
        {
            get { return PlanDetails.Count > 0; }
        }

        /// <summary>Satırın sol şeridi: 0 yok, 2 kırmızı (Kaldı), 4 sarı (planı ya da sonucu yok).</summary>
        public int Stripe { get; set; }
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
            BuildCoverageParts();
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
            set
            {
                if (SetProperty(ref showRequirementReports, value))
                {
                    VerificationColumns.ShowReport = value;
                }
            }
        }

        /// <summary>Rapor görünümünde Gereksinimler sütunu açık mı (varsayılan kapalı: bağlı planlar görünür).</summary>
        public bool ShowReportRequirements
        {
            get { return showReportRequirements; }
            set
            {
                if (SetProperty(ref showReportRequirements, value))
                {
                    VerificationColumns.ShowReportRequirements = value;
                }
            }
        }

        /// <summary>Hiçbir plan maddesinde olmayan gereksinimler.</summary>
        public IList<string> RequirementsWithoutPlan { get; private set; } = new List<string>();

        /// <summary>Raporu olmayan plan maddeleri.</summary>
        public IList<string> PlansWithoutReport { get; private set; } = new List<string>();

        /// <summary>Hiçbir plan maddesine bağlı olmayan rapor maddeleri.</summary>
        public IList<string> ReportsWithoutPlan { get; private set; } = new List<string>();

        /// <summary>Seçili görünümün kapsama uyarısı (parçaların birleşimi); eksik yoksa boş.</summary>
        public string VerCoverageText
        {
            get { return string.Join(" · ", CoverageParts.Select(p => p.Text)); }
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

        // Eski düzenli dosyayı bir kez yeni düzene geçirir; gereksinimler okunmadan önce çağrılır (yoksa bellekte eski satırlar kalır).
        private void RetireLegacyVerification()
        {
            var retireError = string.Empty;
            if (verificationStock.Length > 0 && PlanReportList.RetireLegacy(verificationStock, out retireError))
            {
                Status = Resources.Verify_Migrated;
            }
            else if (verificationStock.Length > 0 && retireError.Length > 0)
            {
                Status = string.Format(CultureInfo.CurrentCulture, Resources.Verify_WriteFailed, retireError);
            }
        }

        // Plan ve rapor tablolarını okur; satırları ve kapsamayı kurar.
        private void LoadPlanReports()
        {
            verificationPlans = verificationStock.Length == 0 ? new List<PlanItem>() : PlanReportList.ReadPlans(verificationStock);
            verificationReports = verificationStock.Length == 0 ? new List<ReportItem>() : PlanReportList.ReadReports(verificationStock);
            var keys = verificationItems.Select(VerificationList.KeyOf).ToList();
            var tps = verificationPlans.Select(p => p.TpNo).ToList();

            RequirementsWithoutPlan = verificationItems.Where(i => !IsOutOfScope(i)).Select(VerificationList.KeyOf).Where(k => !verificationPlans.Any(p => p.Requirements.Contains(k, StringComparer.OrdinalIgnoreCase))).ToList();
            PlansWithoutReport = tps.Where(tp => tp.Length > 0 && !verificationReports.Any(r => r.Plans.Contains(tp, StringComparer.OrdinalIgnoreCase))).ToList();
            ReportsWithoutPlan = verificationReports.Where(r => !r.Plans.Any(p => tps.Contains(p, StringComparer.OrdinalIgnoreCase))).Select(r => r.TrNo).ToList();

            allPlanRows = new List<PlanRow>();
            foreach (var plan in verificationPlans)
            {
                var reqs = plan.Requirements.Select(r => new LinkChip(r, keys.Contains(r, StringComparer.OrdinalIgnoreCase) ? 0 : 4)).ToList();
                if (reqs.Count == 0)
                {
                    reqs.Add(new LinkChip(Resources.Verify_NoRequirement, 4));
                }

                var reports = ReportsOf(new[] { plan.TpNo });
                var reportState = PlanReportState(plan);
                allPlanRows.Add(new PlanRow(plan, reqs, reports.Count > 0 ? reports : new List<LinkChip> { new LinkChip(Resources.Verify_NoReport, 4) })
                {
                    Stripe = reportState == 2 ? 2 : (reportState == 0 || reportState == 3 || PlanReqState(plan) == 0 ? 4 : 0),
                });
            }

            allReportRows = new List<ReportRow>();
            foreach (var report in verificationReports)
            {
                var plans = report.Plans.Select(p => new LinkChip(p, tps.Contains(p, StringComparer.OrdinalIgnoreCase) ? 5 : 4)).ToList();
                var reqs = verificationPlans.Where(p => report.Plans.Contains(p.TpNo, StringComparer.OrdinalIgnoreCase))
                    .SelectMany(p => p.Requirements).Distinct(StringComparer.OrdinalIgnoreCase).Select(r => new LinkChip(r, 0)).ToList();
                var result = ReportResultState(report);
                allReportRows.Add(new ReportRow(report, plans.Count > 0 ? plans : new List<LinkChip> { new LinkChip(Resources.Verify_NoPlan, 4) }, reqs)
                {
                    Stripe = result == 2 ? 2 : (result == 0 || ReportPlanState(report) == 0 ? 4 : 0),
                });
            }

            // Plan ayrıntısı: gereksinimler, raporlar ve md'deki elle yazılan ayrıntı.
            var details = verificationStock.Length == 0 ? new Dictionary<string, string>() : PlanReportList.ReadDetails(verificationStock, PlanReportList.PlanHeading);
            foreach (var row in allPlanRows)
            {
                row.RequirementDetails = row.Item.Requirements.Select(k => new PlanRequirementDetail(k, verificationItems.FirstOrDefault(i => string.Equals(VerificationList.KeyOf(i), k, StringComparison.OrdinalIgnoreCase)))).ToList();
                row.StandardText = string.Join(", ", row.Item.Requirements.Select(k => verificationItems.FirstOrDefault(i => string.Equals(VerificationList.KeyOf(i), k, StringComparison.OrdinalIgnoreCase)))
                    .Where(i => i != null).Select(i => StandardShort(i!)).Where(s => s.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase));
                row.ReportDetails = allReportRows.Where(r => r.Item.Plans.Contains(row.TpNo, StringComparer.OrdinalIgnoreCase)).ToList();
                string text;
                row.DetailText = details.TryGetValue(row.TpNo, out text) ? text : string.Empty;
            }

            // Rapor ayrıntısı: planlar, planlar üzerinden gereksinimler, md'deki elle yazılan ayrıntı.
            var reportDetails = verificationStock.Length == 0 ? new Dictionary<string, string>() : PlanReportList.ReadDetails(verificationStock, PlanReportList.ReportHeading);
            foreach (var row in allReportRows)
            {
                var plans = row.Item.Plans.Select(k => verificationPlans.FirstOrDefault(p => string.Equals(p.TpNo, k, StringComparison.OrdinalIgnoreCase))).ToList();
                row.PlanDetails = row.Item.Plans.Select((k, i) => new ReportPlanDetail(k, plans[i])).ToList();
                row.RequirementDetails = plans.Where(p => p != null).SelectMany(p => p!.Requirements).Distinct(StringComparer.OrdinalIgnoreCase)
                    .Select(k => new PlanRequirementDetail(k, verificationItems.FirstOrDefault(i => string.Equals(VerificationList.KeyOf(i), k, StringComparison.OrdinalIgnoreCase)))).ToList();
                string text;
                row.DetailText = reportDetails.TryGetValue(row.TrNo, out text) ? text : string.Empty;
            }

            UpdateLinkCounts();
            FilterPlanRows();
            FilterReportRows();

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

        /// <summary>Gereksinimin bağlı plan maddeleri (açılan satırdaki Test planı kartı).</summary>
        public IList<PlanRow> PlanRowsOf(VerificationItem item)
        {
            var key = VerificationList.KeyOf(item);
            return allPlanRows.Where(p => p.Item.Requirements.Contains(key, StringComparer.OrdinalIgnoreCase)).ToList();
        }

        /// <summary>Gereksinimin planları üzerinden bağlı rapor maddeleri (açılan satırdaki Test raporu kartı).</summary>
        public IList<ReportRow> ReportRowsOf(VerificationItem item)
        {
            var tps = PlanRowsOf(item).Select(p => p.TpNo).ToList();
            return allReportRows.Where(r => r.Item.Plans.Any(p => tps.Contains(p, StringComparer.OrdinalIgnoreCase))).ToList();
        }

        /// <summary>
        /// Bağlardan hesaplanan gereksinim durumu (kod karar verir): planı yoksa Bekliyor; bağlı bir rapor Kaldı ise Kaldı;
        /// her planının raporu var ve hepsi Geçti ise Geçti; değilse Planlandı. Elle verilen durum ayrı tutulur, bunu ezmez.
        /// </summary>
        public static string StatusFromLinks(IList<PlanItem> plans, IList<ReportItem> reports)
        {
            if (plans.Count == 0)
            {
                return Resources.Verify_StatusPending;
            }

            var results = reports.Select(r => ResultKindOf(r.Result)).ToList();
            if (results.Contains(2))
            {
                return Resources.Verify_StatusFailed;
            }

            var everyPlanReported = plans.All(p => reports.Any(r => r.Plans.Contains(p.TpNo, StringComparer.OrdinalIgnoreCase)));
            return results.Count > 0 && everyPlanReported && results.All(k => k == 1) ? Resources.Verify_StatusPassed : Resources.Verify_StatusPlanned;
        }

        /// <summary>
        /// Seçilen durumdan elle durum: boş, Bekliyor ya da hesaplananla aynıysa boş (hesaplanan geçerli olur);
        /// değilse seçilen.
        /// </summary>
        public static string ManualStatusFor(string picked, string computed)
        {
            var p = (picked ?? string.Empty).Trim();
            return IsDefaultStatus(p) || string.Equals(p, (computed ?? string.Empty).Trim(), StringComparison.CurrentCultureIgnoreCase) ? string.Empty : p;
        }

        // Bekliyor (boş da sayılır): elle verilmiş sayılmaz.
        private static bool IsDefaultStatus(string status)
        {
            var kind = VerificationRow.KindOf(status);
            return status.Trim().Length == 0 || kind == 0;
        }
        /// <summary>
        /// Gereksinim satırlarının Durum sütununu bağlarla eşitler (<see cref="StatusFromLinks"/>). Yalnız değişen satırlar yazılır. Değişen satır sayısı.
        /// </summary>
        private int SyncRequirementLinks()
        {
            if (verificationStock.Length == 0)
            {
                return 0;
            }

            var plans = PlanReportList.ReadPlans(verificationStock);
            var reports = PlanReportList.ReadReports(verificationStock);
            var changed = 0;
            // Eski dosya (Elle durum sütunu yok): Durum'da Bekliyor dışında, hesaplanandan farklı bir durum elle
            // verilmiştir; Elle durum'a taşınır (kaybolmaz).
            var firstTime = !VerificationList.HasManualStatusColumn(verificationStock);
            foreach (var item in VerificationList.Read(verificationStock))
            {
                var key = VerificationList.KeyOf(item);
                var mine = plans.Where(p => p.Requirements.Contains(key, StringComparer.OrdinalIgnoreCase)).ToList();
                var tps = mine.Select(p => p.TpNo).ToList();
                var myReports = reports.Where(r => r.Plans.Any(p => tps.Contains(p, StringComparer.OrdinalIgnoreCase))).ToList();
                var status = StatusFromLinks(mine, myReports);
                var manual = item.ManualStatus.Trim();
                if (firstTime && manual.Length == 0 && item.Status.Trim() != status && !IsDefaultStatus(item.Status))
                {
                    manual = item.Status.Trim();
                }

                // Elle durum hesaplananla aynı olunca kalkar (iş bitti, yeniden otomatik).
                if (string.Equals(manual, status, StringComparison.CurrentCultureIgnoreCase))
                {
                    manual = string.Empty;
                }

                if (status == item.Status && manual == item.ManualStatus)
                {
                    continue;
                }

                item.Status = status;
                item.ManualStatus = manual;
                if (VerificationList.Update(verificationStock, item).Length == 0)
                {
                    changed++;
                }
            }

            return changed;
        }
    }
}
