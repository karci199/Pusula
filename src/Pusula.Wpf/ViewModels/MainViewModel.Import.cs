using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Pusula.Core.Testing;
using Pusula.Wpf.Properties;

namespace Pusula.Wpf.ViewModels
{
    /// <summary>Doğrulamalar: Excel'den gereksinim içe aktarma (deneme).</summary>
    public sealed partial class MainViewModel
    {
        private RequirementImportResult? pendingImport;
        private bool isImportOpen;
        private string importText = string.Empty;

        /// <summary>İçe aktarma onay penceresi açık mı.</summary>
        public bool IsImportOpen
        {
            get { return isImportOpen; }
            set { SetProperty(ref isImportOpen, value); }
        }

        /// <summary>Onay penceresindeki özet ("12 madde, 3 başlık okundu: 9 yeni, 2 güncellenecek, 1 aynı").</summary>
        public string ImportText
        {
            get { return importText; }
            private set { SetProperty(ref importText, value); }
        }

        /// <summary>Excel'i okur, özeti hazırlar ve onay penceresini açar (henüz bir şey yazılmaz).</summary>
        public void PrepareImport(string path)
        {
            if (verificationStock.Length == 0 || string.IsNullOrEmpty(path))
            {
                return;
            }

            var result = RequirementImport.ReadExcel(path);
            if (result.Error.Length > 0 || result.Items.Count == 0)
            {
                Status = result.Error == "header" || result.Items.Count == 0 ? Resources.Import_NoHeader : string.Format(CultureInfo.CurrentCulture, Resources.Verify_WriteFailed, result.Error);
                return;
            }

            var existing = VerificationList.Read(verificationStock);
            var updates = result.Items.Count(i => i.TgNo.Length > 0 && existing.Any(e => string.Equals(e.TgNo.Trim(), i.TgNo, StringComparison.OrdinalIgnoreCase)));
            pendingImport = result;
            ImportText = string.Format(CultureInfo.CurrentCulture, Resources.Import_Summary, result.Items.Count, result.Headings.Count, result.Items.Count - updates, updates);
            IsImportOpen = true;
        }

        /// <summary>Dışa aktarma için önerilen dosya adı: "AB-7000-0002_Test_Plani.xlsx" (rapor için _Test_Raporu).</summary>
        public string ExportFileName(bool report)
        {
            var stock = verificationStock.Length > 0 ? System.IO.Path.GetFileName(verificationStock.TrimEnd('\\')) : string.Empty;
            return stock + (report ? Resources.Export_ReportSuffix : Resources.Export_PlanSuffix) + ".xlsx";
        }

        /// <summary>Dışa aktarma için önerilen klasör: stokun 03_Testler klasörü (yoksa stok klasörü).</summary>
        public string ExportFolder
        {
            get
            {
                var folder = System.IO.Path.Combine(verificationStock, VerificationList.FolderName);
                return System.IO.Directory.Exists(folder) ? folder : verificationStock;
            }
        }

        /// <summary>
        /// Test planını Excel'e yazar (DOORS'a kopyala-yapıştır için): TP no, açıklama, yöntem, altyapı, gereksinimler,
        /// gereksinim metinleri, not, test raporu. Bağı eksik satırlar "—" ile belli olur.
        /// </summary>
        public bool ExportPlans(string path)
        {
            var rows = new List<string[]>
            {
                new[] { Resources.Verify_PlanNo, Resources.Verify_ColDescription, Resources.Verify_ColMethod, Resources.Verify_FieldFacility, Resources.Verify_ColRequirements, Resources.Export_RequirementTexts, Resources.Export_Standard, Resources.Verify_PlanNote, Resources.Verify_ViewReport },
            };
            foreach (var plan in verificationPlans)
            {
                var texts = plan.Requirements.Select(k => verificationItems.FirstOrDefault(i => string.Equals(VerificationList.KeyOf(i), k, StringComparison.OrdinalIgnoreCase)))
                    .Where(i => i != null).Select(i => VerificationList.KeyOf(i!) + ": " + (i!.Requirement.Length > 0 ? i.Requirement : i.Text));
                var stds = plan.Requirements.Select(k => verificationItems.FirstOrDefault(i => string.Equals(VerificationList.KeyOf(i), k, StringComparison.OrdinalIgnoreCase)))
                    .Where(i => i != null).Select(i => StandardLong(i!)).Where(s => s.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase);
                var reports = verificationReports.Where(r => r.Plans.Contains(plan.TpNo, StringComparer.OrdinalIgnoreCase))
                    .Select(r => r.TrNo + (r.Result.Length > 0 ? " " + r.Result : string.Empty)).ToList();
                rows.Add(new[]
                {
                    plan.TpNo, plan.Description, plan.Method, plan.Facility,
                    plan.Requirements.Count > 0 ? string.Join(", ", plan.Requirements) : Resources.Verify_None,
                    string.Join("\n", texts), string.Join("\n", stds), plan.Note,
                    reports.Count > 0 ? string.Join(", ", reports) : Resources.Verify_None,
                });
            }

            return FinishExport(Core.Office.XlsxWriter.Write(path, Resources.Verify_ViewPlan, rows), path, rows.Count - 1);
        }

        /// <summary>Test raporunu Excel'e yazar: TR no, açıklama, plan, gereksinimler (plan üzerinden), tarih, birim, sonuç, sonucun yeri, not.</summary>
        public bool ExportReports(string path)
        {
            var rows = new List<string[]>
            {
                new[] { Resources.Verify_ReportNo, Resources.Verify_ColDescription, Resources.Verify_ViewPlan, Resources.Verify_ColRequirements, Resources.Verify_ColDate, Resources.Verify_ColUnit, Resources.Verify_ColResult, Resources.Verify_ColResultPath, Resources.Verify_PlanNote },
            };
            foreach (var report in verificationReports)
            {
                var reqs = verificationPlans.Where(p => report.Plans.Contains(p.TpNo, StringComparer.OrdinalIgnoreCase))
                    .SelectMany(p => p.Requirements).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                rows.Add(new[]
                {
                    report.TrNo, report.Description,
                    report.Plans.Count > 0 ? string.Join(", ", report.Plans) : Resources.Verify_None,
                    reqs.Count > 0 ? string.Join(", ", reqs) : Resources.Verify_None,
                    report.Date, report.Unit, report.Result.Length > 0 ? report.Result : Resources.Verify_None, report.ResultPath, report.Note,
                });
            }

            return FinishExport(Core.Office.XlsxWriter.Write(path, Resources.Verify_ViewReport, rows), path, rows.Count - 1);
        }

        private bool FinishExport(string error, string path, int count)
        {
            Status = error.Length > 0
                ? string.Format(CultureInfo.CurrentCulture, Resources.Verify_WriteFailed, error)
                : string.Format(CultureInfo.CurrentCulture, Resources.Export_Done, count, System.IO.Path.GetFileName(path));
            return error.Length == 0;
        }

        /// <summary>Onaylanan içe aktarmayı yazar.</summary>
        public void ConfirmImport()
        {
            IsImportOpen = false;
            var import = pendingImport;
            pendingImport = null;
            if (import == null || verificationStock.Length == 0)
            {
                return;
            }

            string error;
            var template = string.Format(CultureInfo.CurrentCulture, Resources.Verify_FileTemplate, System.IO.Path.GetFileName(verificationStock));
            var counts = RequirementImport.Apply(verificationStock, import.Items, Resources.Verify_GroupOther, template, out error);
            LoadVerifications();
            Status = error.Length > 0
                ? string.Format(CultureInfo.CurrentCulture, Resources.Verify_WriteFailed, error)
                : string.Format(CultureInfo.CurrentCulture, Resources.Import_Done, counts.Item1, counts.Item2, counts.Item3);
        }
    }
}
