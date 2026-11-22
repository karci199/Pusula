using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using Pusula.Core.Testing;
using Pusula.Wpf.Mvvm;
using Pusula.Wpf.Properties;

namespace Pusula.Wpf.ViewModels
{
    /// <summary>Çoklu seçim listesinde bir seçenek: numara, açıklama, sağda bilgi (bağlı olduğu diğer maddeler).</summary>
    public sealed class LinkOption : ViewModelBase
    {
        private readonly Action changed;
        private bool isChecked;
        private bool isVisible = true;

        /// <summary>Seçeneği kurar.</summary>
        public LinkOption(string key, string text, string badge, bool isWarning, bool isChecked, Action changed)
        {
            Key = key;
            Text = text;
            Badge = badge;
            IsWarning = isWarning;
            this.isChecked = isChecked;
            this.changed = changed;
        }

        /// <summary>Numara (TG no / TP no); md'ye bu yazılır.</summary>
        public string Key { get; }

        /// <summary>Açıklama.</summary>
        public string Text { get; }

        /// <summary>Sağdaki bilgi ("4.1, 4.3" ya da "plan yok").</summary>
        public string Badge { get; }

        /// <summary>Bilgi uyarı rengiyle mi (bağı yok).</summary>
        public bool IsWarning { get; }

        /// <summary>Seçili mi.</summary>
        public bool IsChecked
        {
            get { return isChecked; }
            set
            {
                if (SetProperty(ref isChecked, value))
                {
                    changed();
                }
            }
        }

        /// <summary>Arama süzgecinden geçiyor mu.</summary>
        public bool IsVisible
        {
            get { return isVisible; }
            set { SetProperty(ref isVisible, value); }
        }
    }

    /// <summary>Ana pencere: plan maddesi ve rapor maddesi pencereleri (ekle, düzenle, sil) ve bağların eşitlenmesi.</summary>
    public sealed partial class MainViewModel
    {
        #region Plan maddesi penceresi

        private PlanItem? editingPlan;
        private bool isVerPlanOpen;
        private string planTpNo = string.Empty;
        private string planDescription = string.Empty;
        private string planMethod = string.Empty;
        private string planNote = string.Empty;
        private string planError = string.Empty;
        private string planSearch = string.Empty;

        /// <summary>Plan maddesi penceresi açık mı.</summary>
        public bool IsVerPlanOpen
        {
            get { return isVerPlanOpen; }
            set { SetDialogOpen(ref isVerPlanOpen, value); }
        }

        /// <summary>Pencere başlığı: "Plan maddesi" ya da "Plan maddesi · 4.1".</summary>
        public string VerPlanTitle
        {
            get { return editingPlan == null ? Resources.Verify_PlanItemTitle : string.Format(CultureInfo.CurrentCulture, Resources.Verify_PlanItemTitleFor, editingPlan.TpNo); }
        }

        /// <summary>Penceredeki Sil düğmesinin metni; yeni maddede boş (düğme gizli).</summary>
        public string VerPlanDeleteText
        {
            get { return editingPlan == null ? string.Empty : Resources.Verify_Delete; }
        }

        /// <summary>TP no.</summary>
        public string PlanTpNo
        {
            get { return planTpNo; }
            set { SetProperty(ref planTpNo, value ?? string.Empty); }
        }

        /// <summary>Açıklama.</summary>
        public string PlanDescription
        {
            get { return planDescription; }
            set { SetProperty(ref planDescription, value ?? string.Empty); }
        }

        /// <summary>Doğrulama yöntemi ("—" boş).</summary>
        public string PlanMethod
        {
            get { return planMethod.Length == 0 ? Resources.Verify_None : planMethod; }
            set { var v = value ?? string.Empty; SetProperty(ref planMethod, v == Resources.Verify_None ? string.Empty : v); }
        }

        /// <summary>Altyapı seçimi (ortak liste; yenisi yazılabilir).</summary>
        public PickOrNew PlanFacilityPick { get; } = new PickOrNew(Resources.Verify_NewFacility);

        /// <summary>Not (jig, SN, dikkat edilecekler).</summary>
        public string PlanNote
        {
            get { return planNote; }
            set { SetProperty(ref planNote, value ?? string.Empty); }
        }

        /// <summary>Penceredeki hata; boşsa gizli.</summary>
        public string PlanError
        {
            get { return planError; }
            private set { SetProperty(ref planError, value); }
        }

        /// <summary>Gereksinim seçenekleri (çoklu seçim; sağda bağlı olduğu diğer planlar).</summary>
        public ObservableCollection<LinkOption> PlanRequirementOptions { get; } = new ObservableCollection<LinkOption>();

        /// <summary>Gereksinim listesinde arama (numara ya da başlık).</summary>
        public string PlanSearch
        {
            get { return planSearch; }
            set
            {
                if (SetProperty(ref planSearch, value ?? string.Empty))
                {
                    FilterOptions(PlanRequirementOptions, planSearch);
                }
            }
        }

        /// <summary>"Gereksinimler · 2 seçili: DGÖ-1, DGÖ-2".</summary>
        public string PlanSelectedText
        {
            get { return SelectedText(PlanRequirementOptions); }
        }

        /// <summary>Plan sekmesindeki "+ Plan maddesi": boş pencere, sıradaki TP no önerilir.</summary>
        public void NewPlanItem()
        {
            OpenPlanDialog(null, null);
        }

        /// <summary>Plan sekmesindeki satır: maddeyi düzenleme penceresinde açar.</summary>
        public void EditPlanItem(PlanItem plan)
        {
            if (plan != null)
            {
                OpenPlanDialog(plan, null);
            }
        }

        /// <summary>Gereksinim satırındaki Test planı kartı "+": bu gereksinim seçili yeni plan maddesi.</summary>
        public void OpenVerPlan(VerificationRow row)
        {
            if (row != null)
            {
                OpenPlanDialog(null, VerificationList.KeyOf(row.Item));
            }
        }

        private void OpenPlanDialog(PlanItem? plan, string? requirement)
        {
            if (verificationStock.Length == 0)
            {
                return;
            }

            editingPlan = plan;
            PlanTpNo = plan != null ? plan.TpNo : PlanReportList.NextNo(verificationPlans.Select(p => p.TpNo), "1");
            PlanDescription = plan != null ? plan.Description : string.Empty;
            PlanMethod = plan != null ? plan.Method : string.Empty;
            var facilities = FacilityList.Read(rootPath).ToList();
            foreach (var used in verificationPlans.Select(p => p.Facility).Concat(verificationItems.Select(i => i.Facility)).Where(f => f.Length > 0))
            {
                if (!facilities.Contains(used, StringComparer.CurrentCultureIgnoreCase))
                {
                    facilities.Add(used);
                }
            }

            PlanFacilityPick.SetNames(facilities);
            PlanFacilityPick.Value = plan != null ? plan.Facility : string.Empty;
            PlanNote = plan != null ? plan.Note : string.Empty;
            PlanError = string.Empty;

            var selected = plan != null ? plan.Requirements : (requirement != null ? new List<string> { requirement } : new List<string>());
            PlanRequirementOptions.Clear();
            foreach (var item in verificationItems)
            {
                var key = VerificationList.KeyOf(item);
                var others = verificationPlans.Where(p => p != plan && p.Requirements.Contains(key, StringComparer.OrdinalIgnoreCase)).Select(p => p.TpNo).ToList();
                PlanRequirementOptions.Add(new LinkOption(key, item.Text, others.Count > 0 ? string.Join(", ", others) : Resources.Verify_NoPlan, others.Count == 0,
                    selected.Contains(key, StringComparer.OrdinalIgnoreCase), () => OnPropertyChanged(nameof(PlanSelectedText))));
            }

            // Tabloda olup gereksinim listesinde olmayan bağlar da görünsün (silinmesin diye seçili kalır).
            foreach (var missing in selected.Where(s => !PlanRequirementOptions.Any(o => string.Equals(o.Key, s, StringComparison.OrdinalIgnoreCase))).ToList())
            {
                PlanRequirementOptions.Add(new LinkOption(missing, Resources.Verify_LinkMissing, string.Empty, true, true, () => OnPropertyChanged(nameof(PlanSelectedText))));
            }

            planSearch = string.Empty;
            OnPropertyChanged(nameof(PlanSearch));
            OnPropertyChanged(nameof(PlanSelectedText));
            OnPropertyChanged(nameof(VerPlanTitle));
            OnPropertyChanged(nameof(VerPlanDeleteText));
            IsVerPlanOpen = true;
        }

        /// <summary>
        /// Plan maddesini <c>## Plan</c> tablosuna yazar (yeni ya da yerinde). TP no boş ya da başka maddede varsa hata.
        /// TP no değiştiyse rapor tablosundaki bağlar da yeni numaraya geçer. Sonra gereksinim durumları eşitlenir.
        /// </summary>
        public bool ConfirmVerPlan()
        {
            if (verificationStock.Length == 0)
            {
                return true;
            }

            var tp = PlanTpNo.Trim();
            if (tp.Length == 0)
            {
                PlanError = Resources.Verify_TpRequired;
                return false;
            }

            var plans = PlanReportList.ReadPlans(verificationStock);
            var oldNo = editingPlan != null ? editingPlan.TpNo : string.Empty;
            var index = editingPlan != null ? plans.FindIndex(p => string.Equals(p.TpNo, oldNo, StringComparison.OrdinalIgnoreCase)) : -1;
            if (plans.Where((p, i) => i != index).Any(p => string.Equals(p.TpNo, tp, StringComparison.OrdinalIgnoreCase)))
            {
                PlanError = string.Format(CultureInfo.CurrentCulture, Resources.Verify_NoDuplicate, tp);
                return false;
            }

            var item = index >= 0 ? plans[index] : new PlanItem();
            item.TpNo = tp;
            item.Description = PlanDescription.Trim();
            item.Method = planMethod.Trim();
            item.Facility = PlanFacilityPick.Value.Trim();
            item.Requirements = PlanRequirementOptions.Where(o => o.IsChecked).Select(o => o.Key).ToList();
            item.Note = PlanNote.Trim();
            if (index < 0)
            {
                plans.Add(item);
                plans = plans.OrderBy(p => p.TpNo, Comparer<string>.Create(PlanReportList.CompareNo)).ToList();
            }

            var error = PlanReportList.SavePlans(verificationStock, plans);
            if (error.Length == 0 && oldNo.Length > 0 && !string.Equals(oldNo, tp, StringComparison.Ordinal))
            {
                var reports = PlanReportList.ReadReports(verificationStock);
                if (reports.Count(r => PlanReportList.RenameLink(r.Plans, oldNo, tp)) > 0)
                {
                    error = PlanReportList.SaveReports(verificationStock, reports);
                }
            }

            if (error.Length > 0)
            {
                PlanError = string.Format(CultureInfo.CurrentCulture, Resources.Verify_WriteFailed, error);
                return false;
            }

            if (item.Facility.Length > 0 && rootPath.Length > 0)
            {
                FacilityList.Add(rootPath, item.Facility, Resources.Verify_FacilityTemplate);
            }

            editingPlan = null;
            AfterLinkWrite(string.Format(CultureInfo.CurrentCulture, Resources.Verify_PlanItemSaved, tp));
            return true;
        }

        /// <summary>Penceredeki Sil: onay sorar.</summary>
        public void RequestDeletePlanItem()
        {
            if (editingPlan == null)
            {
                return;
            }

            pendingLinkDelete = editingPlan;
            VerLinkDeleteText = string.Format(CultureInfo.CurrentCulture, Resources.Verify_DeletePlanText, editingPlan.TpNo);
            editingPlan = null;
            IsVerLinkDeleteOpen = true;
        }

        #endregion
        #region Rapor maddesi penceresi

        private ReportItem? editingReport;
        private bool isVerReportOpen;
        private string reportTrNo = string.Empty;
        private string reportDescription = string.Empty;
        private string reportResult = string.Empty;
        private string reportPath = string.Empty;
        private string reportNote = string.Empty;
        private string reportDate = string.Empty;
        private string reportUnit = string.Empty;
        private string reportError = string.Empty;
        private string reportSearch = string.Empty;

        /// <summary>Rapor maddesi penceresi açık mı.</summary>
        public bool IsVerReportOpen
        {
            get { return isVerReportOpen; }
            set { SetDialogOpen(ref isVerReportOpen, value); }
        }

        /// <summary>Pencere başlığı: "Rapor maddesi" ya da "Rapor maddesi · 5.1".</summary>
        public string VerReportTitle
        {
            get { return editingReport == null ? Resources.Verify_ReportItemTitle : string.Format(CultureInfo.CurrentCulture, Resources.Verify_ReportItemTitleFor, editingReport.TrNo); }
        }

        /// <summary>Penceredeki Sil düğmesinin metni; yeni maddede boş (düğme gizli).</summary>
        public string VerReportDeleteText
        {
            get { return editingReport == null ? string.Empty : Resources.Verify_Delete; }
        }

        /// <summary>Test sonucu seçenekleri: Geçti · Kaldı · Hazırlanmadı.</summary>
        public IList<string> VerificationResults
        {
            get { return VerificationRow.Results; }
        }

        /// <summary>TR no.</summary>
        public string ReportTrNo
        {
            get { return reportTrNo; }
            set { SetProperty(ref reportTrNo, value ?? string.Empty); }
        }

        /// <summary>Açıklama.</summary>
        public string ReportDescription
        {
            get { return reportDescription; }
            set { SetProperty(ref reportDescription, value ?? string.Empty); }
        }

        /// <summary>Sonuç (Geçti · Kaldı · Hazırlanmadı); boş olabilir.</summary>
        public string ReportResult
        {
            get { return reportResult; }
            set { SetProperty(ref reportResult, value ?? string.Empty); }
        }

        /// <summary>Sonucun yeri: tam yol (penceredeki seçici); kaydedince stok içindeyse göreli yazılır.</summary>
        public string ReportPath
        {
            get { return reportPath; }
            set { SetProperty(ref reportPath, value ?? string.Empty); }
        }

        /// <summary>Not (ölçülen değer, kaldıysa bildirim no).</summary>
        public string ReportNote
        {
            get { return reportNote; }
            set { SetProperty(ref reportNote, value ?? string.Empty); }
        }

        /// <summary>Tarih (gg.aa.yyyy); yeni maddede bugün.</summary>
        public string ReportDate
        {
            get { return reportDate; }
            set { SetProperty(ref reportDate, value ?? string.Empty); }
        }

        /// <summary>Birim (seri no), örn. "SN01".</summary>
        public string ReportUnit
        {
            get { return reportUnit; }
            set { SetProperty(ref reportUnit, value ?? string.Empty); }
        }

        /// <summary>Penceredeki hata; boşsa gizli.</summary>
        public string ReportError
        {
            get { return reportError; }
            private set { SetProperty(ref reportError, value); }
        }

        /// <summary>Plan maddesi seçenekleri (çoklu seçim; sağda kapsadığı gereksinimler).</summary>
        public ObservableCollection<LinkOption> ReportPlanOptions { get; } = new ObservableCollection<LinkOption>();

        /// <summary>Plan listesinde arama (numara ya da açıklama).</summary>
        public string ReportSearch
        {
            get { return reportSearch; }
            set
            {
                if (SetProperty(ref reportSearch, value ?? string.Empty))
                {
                    FilterOptions(ReportPlanOptions, reportSearch);
                }
            }
        }

        /// <summary>"2 seçili: 4.1, 4.3".</summary>
        public string ReportSelectedText
        {
            get { return SelectedText(ReportPlanOptions); }
        }

        /// <summary>Rapor sekmesindeki "+ Rapor maddesi": boş pencere, sıradaki TR no önerilir.</summary>
        public void NewReportItem()
        {
            OpenReportDialog(null, new List<string>());
        }

        /// <summary>Rapor sekmesindeki satır: maddeyi düzenleme penceresinde açar.</summary>
        public void EditReportItem(ReportItem report)
        {
            if (report != null)
            {
                OpenReportDialog(report, report.Plans);
            }
        }

        /// <summary>Gereksinim satırındaki Test raporu kartı "+": gereksinimin planları seçili yeni rapor maddesi.</summary>
        public void OpenVerReport(VerificationRow row)
        {
            if (row != null)
            {
                OpenReportDialog(null, PlanRowsOf(row.Item).Select(p => p.TpNo).ToList());
            }
        }

        private void OpenReportDialog(ReportItem? report, IList<string> selected)
        {
            if (verificationStock.Length == 0)
            {
                return;
            }

            editingReport = report;
            ReportTrNo = report != null ? report.TrNo : PlanReportList.NextNo(verificationReports.Select(r => r.TrNo), "1");
            ReportDescription = report != null ? report.Description : string.Empty;
            ReportResult = report != null ? report.Result : string.Empty;
            ReportPath = report != null ? FullResultPath(verificationStock, report.ResultPath) : string.Empty;
            ReportNote = report != null ? report.Note : string.Empty;
            ReportDate = report != null ? report.Date : DateTime.Today.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
            ReportUnit = report != null ? report.Unit : string.Empty;
            ReportError = string.Empty;

            ReportPlanOptions.Clear();
            foreach (var plan in verificationPlans)
            {
                var reqs = plan.Requirements.Count > 0 ? string.Join(", ", plan.Requirements) : Resources.Verify_NoRequirement;
                ReportPlanOptions.Add(new LinkOption(plan.TpNo, plan.Description, reqs, plan.Requirements.Count == 0,
                    selected.Contains(plan.TpNo, StringComparer.OrdinalIgnoreCase), () => OnPropertyChanged(nameof(ReportSelectedText))));
            }

            foreach (var missing in selected.Where(s => !ReportPlanOptions.Any(o => string.Equals(o.Key, s, StringComparison.OrdinalIgnoreCase))).ToList())
            {
                ReportPlanOptions.Add(new LinkOption(missing, Resources.Verify_LinkMissing, string.Empty, true, true, () => OnPropertyChanged(nameof(ReportSelectedText))));
            }

            reportSearch = string.Empty;
            OnPropertyChanged(nameof(ReportSearch));
            OnPropertyChanged(nameof(ReportSelectedText));
            OnPropertyChanged(nameof(VerReportTitle));
            OnPropertyChanged(nameof(VerReportDeleteText));
            IsVerReportOpen = true;
        }

        /// <summary>
        /// Rapor maddesini <c>## Rapor</c> tablosuna yazar (yeni ya da yerinde). TR no boş ya da başka maddede varsa hata.
        /// Sonucun yeri stok klasörünün içindeyse göreli yazılır. Sonra gereksinim durumları eşitlenir.
        /// </summary>
        public bool ConfirmVerReport()
        {
            if (verificationStock.Length == 0)
            {
                return true;
            }

            var tr = ReportTrNo.Trim();
            if (tr.Length == 0)
            {
                ReportError = Resources.Verify_TrRequired;
                return false;
            }

            var reports = PlanReportList.ReadReports(verificationStock);
            var index = editingReport != null ? reports.FindIndex(r => string.Equals(r.TrNo, editingReport.TrNo, StringComparison.OrdinalIgnoreCase)) : -1;
            if (reports.Where((r, i) => i != index).Any(r => string.Equals(r.TrNo, tr, StringComparison.OrdinalIgnoreCase)))
            {
                ReportError = string.Format(CultureInfo.CurrentCulture, Resources.Verify_NoDuplicate, tr);
                return false;
            }

            var item = index >= 0 ? reports[index] : new ReportItem();
            item.TrNo = tr;
            item.Description = ReportDescription.Trim();
            item.Result = ReportResult.Trim();
            item.Plans = ReportPlanOptions.Where(o => o.IsChecked).Select(o => o.Key).ToList();
            item.ResultPath = RelativeResultPath(verificationStock, ReportPath.Trim());
            item.Note = ReportNote.Trim();
            item.Date = ReportDate.Trim();
            item.Unit = ReportUnit.Trim();
            if (index < 0)
            {
                reports.Add(item);
                reports = reports.OrderBy(r => r.TrNo, Comparer<string>.Create(PlanReportList.CompareNo)).ToList();
            }

            var error = PlanReportList.SaveReports(verificationStock, reports);
            if (error.Length > 0)
            {
                ReportError = string.Format(CultureInfo.CurrentCulture, Resources.Verify_WriteFailed, error);
                return false;
            }

            editingReport = null;
            AfterLinkWrite(string.Format(CultureInfo.CurrentCulture, Resources.Verify_ReportItemSaved, tr));
            return true;
        }

        /// <summary>Penceredeki Sil: onay sorar.</summary>
        public void RequestDeleteReportItem()
        {
            if (editingReport == null)
            {
                return;
            }

            pendingLinkDelete = editingReport;
            VerLinkDeleteText = string.Format(CultureInfo.CurrentCulture, Resources.Verify_DeleteReportText, editingReport.TrNo);
            editingReport = null;
            IsVerLinkDeleteOpen = true;
        }

        /// <summary>Rapor maddesinin sonucunun yerini (klasör ya da dosya) açar.</summary>
        public void OpenReportPath(ReportItem report)
        {
            if (report == null || verificationStock.Length == 0 || report.ResultPath.Length == 0)
            {
                return;
            }

            var full = FullResultPath(verificationStock, report.ResultPath);
            if (Directory.Exists(full) || File.Exists(full))
            {
                OpenPath(full);
            }
            else
            {
                Status = string.Format(CultureInfo.CurrentCulture, Resources.Verify_ResultPathMissing, report.ResultPath);
            }
        }

        #endregion
        #region Silme onayı ve ortak

        private object? pendingLinkDelete;
        private bool isVerLinkDeleteOpen;
        private string verLinkDeleteText = string.Empty;

        /// <summary>Plan / rapor maddesi silme onayı açık mı.</summary>
        public bool IsVerLinkDeleteOpen
        {
            get { return isVerLinkDeleteOpen; }
            set { SetDialogOpen(ref isVerLinkDeleteOpen, value); }
        }

        /// <summary>Onay metni.</summary>
        public string VerLinkDeleteText
        {
            get { return verLinkDeleteText; }
            private set { SetProperty(ref verLinkDeleteText, value); }
        }

        /// <summary>
        /// Onaylanan plan ya da rapor maddesini kendi tablosundan kaldırır. Bağlı maddelerdeki numara silinmez; orada
        /// "plan yok" / eksik bağ olarak görünür. Sonra gereksinim durumları eşitlenir.
        /// </summary>
        public void ConfirmDeleteVerLink()
        {
            IsVerLinkDeleteOpen = false;
            var target = pendingLinkDelete;
            pendingLinkDelete = null;
            if (verificationStock.Length == 0 || target == null)
            {
                return;
            }

            string error;
            string done;
            var plan = target as PlanItem;
            if (plan != null)
            {
                var plans = PlanReportList.ReadPlans(verificationStock);
                plans.RemoveAll(p => string.Equals(p.TpNo, plan.TpNo, StringComparison.OrdinalIgnoreCase));
                error = PlanReportList.SavePlans(verificationStock, plans);
                done = string.Format(CultureInfo.CurrentCulture, Resources.Verify_PlanItemDeleted, plan.TpNo);
            }
            else
            {
                var report = (ReportItem)target;
                var reports = PlanReportList.ReadReports(verificationStock);
                reports.RemoveAll(r => string.Equals(r.TrNo, report.TrNo, StringComparison.OrdinalIgnoreCase));
                error = PlanReportList.SaveReports(verificationStock, reports);
                done = string.Format(CultureInfo.CurrentCulture, Resources.Verify_ReportItemDeleted, report.TrNo);
            }

            if (error.Length > 0)
            {
                Status = string.Format(CultureInfo.CurrentCulture, Resources.Verify_WriteFailed, error);
                return;
            }

            AfterLinkWrite(done);
        }

        // Plan / rapor yazıldıktan sonra: gereksinim satırları eşitlenir, liste yeniden okunur, durum çubuğunda sonuç.
        private void AfterLinkWrite(string done)
        {
            var changed = SyncRequirementLinks();
            LoadVerifications();
            Status = changed > 0 ? done + " · " + string.Format(CultureInfo.CurrentCulture, Resources.Verify_StatusSynced, changed) : done;
        }

        private static void FilterOptions(IEnumerable<LinkOption> options, string search)
        {
            var text = (search ?? string.Empty).Trim();
            foreach (var option in options)
            {
                option.IsVisible = text.Length == 0
                    || option.Key.IndexOf(text, StringComparison.CurrentCultureIgnoreCase) >= 0
                    || option.Text.IndexOf(text, StringComparison.CurrentCultureIgnoreCase) >= 0;
            }
        }

        private static string SelectedText(IEnumerable<LinkOption> options)
        {
            var keys = options.Where(o => o.IsChecked).Select(o => o.Key).ToList();
            return keys.Count == 0
                ? Resources.Verify_SelectedNone
                : string.Format(CultureInfo.CurrentCulture, Resources.Verify_SelectedCount, keys.Count, string.Join(", ", keys));
        }

        #endregion
    }
}
