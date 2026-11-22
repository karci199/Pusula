using System;
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
