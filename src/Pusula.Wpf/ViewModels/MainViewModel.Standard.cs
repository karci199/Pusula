using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Pusula.Core.Testing;
using Pusula.Wpf.Properties;

namespace Pusula.Wpf.ViewModels
{
    /// <summary>Gereksinim penceresi: grup ÇK / EMC iken standart, yöntem, prosedür (elle girilir).</summary>
    public sealed partial class MainViewModel
    {
        private string newVerStandard = string.Empty;
        private string newVerStandardMethod = string.Empty;
        private string newVerProcedure = string.Empty;
        private IList<TestStandard> standards = new List<TestStandard>();

        /// <summary>Standart kısmı görünür mü: grup ÇK ya da EMC (ya da zaten standart yazılı).</summary>
        public bool IsNewVerStandard
        {
            get
            {
                var group = NewVerGroupChoice;
                return (group != null && (string.Equals(group.Key, Resources.Verify_GroupEmc, StringComparison.CurrentCultureIgnoreCase) || string.Equals(group.Key, Resources.Verify_GroupEmcOnly, StringComparison.CurrentCultureIgnoreCase)))
                    || newVerStandard.Length > 0;
            }
        }

        /// <summary>Standart adları (ortak liste + bu stokta kullanılanlar).</summary>
        public ObservableCollection<string> StandardNames { get; } = new ObservableCollection<string>();

        /// <summary>Seçili standardın yöntemleri.</summary>
        public ObservableCollection<string> StandardMethodOptions { get; } = new ObservableCollection<string>();

        /// <summary>Pencerede: standart, örn. "MIL-STD-810G".</summary>
        public string NewVerStandard
        {
            get { return newVerStandard; }
            set
            {
                if (SetProperty(ref newVerStandard, (value ?? string.Empty).Trim()))
                {
                    FillStandardMethods();
                    OnPropertyChanged(nameof(IsNewVerStandard));
                }
            }
        }

        /// <summary>Pencerede: standardın yöntemi, örn. "509.5 · Tuz sisi".</summary>
        public string NewVerStandardMethod
        {
            get { return newVerStandardMethod; }
            set { SetProperty(ref newVerStandardMethod, (value ?? string.Empty).Trim()); }
        }

        /// <summary>Pencerede: prosedür, örn. "I".</summary>
        public string NewVerProcedure
        {
            get { return newVerProcedure; }
            set { SetProperty(ref newVerProcedure, value ?? string.Empty); }
        }

        // Pencere açılırken: ortak liste okunur, alanlar maddeden (yoksa boş) doldurulur.
        private void ResetStandardFields(VerificationItem? item)
        {
            standards = StandardList.Read(rootPath);
            StandardNames.Clear();
            foreach (var name in standards.Select(s => s.Name).Concat(verificationItems.Select(i => i.Standard)).Where(s => s.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                StandardNames.Add(name);
            }

            newVerStandard = item != null ? item.Standard : string.Empty;
            OnPropertyChanged(nameof(NewVerStandard));
            FillStandardMethods();
            NewVerStandardMethod = item != null ? item.StandardMethod : string.Empty;
            NewVerProcedure = item != null ? item.Procedure : string.Empty;
            OnPropertyChanged(nameof(IsNewVerStandard));
        }

        private void FillStandardMethods()
        {
            StandardMethodOptions.Clear();
            var std = standards.FirstOrDefault(s => string.Equals(s.Name, newVerStandard, StringComparison.OrdinalIgnoreCase));
            var used = verificationItems.Where(i => string.Equals(i.Standard, newVerStandard, StringComparison.OrdinalIgnoreCase)).Select(i => i.StandardMethod);
            foreach (var method in (std != null ? std.Methods : new List<string>()).Concat(used).Where(m => m.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                StandardMethodOptions.Add(method);
            }
        }

        // Kaydederken: maddeye yazılır; standart kısmı gizliyse (grup ÇK / EMC değil ve standart yok) boş kalır.
        private void ApplyStandardFields(VerificationItem item)
        {
            var show = IsNewVerStandard;
            item.Standard = show ? newVerStandard.Trim() : string.Empty;
            item.StandardMethod = show && item.Standard.Length > 0 ? newVerStandardMethod.Trim() : string.Empty;
            item.Procedure = show && item.Standard.Length > 0 ? newVerProcedure.Trim() : string.Empty;
        }

        // Listede olmayan standart / yöntem ortak listeye eklenir (dosya yoksa başlangıç listesiyle oluşur).
        private void RememberStandard(VerificationItem item)
        {
            if (item.Standard.Length > 0 && rootPath.Length > 0)
            {
                StandardList.Add(rootPath, item.Standard, item.StandardMethod, Resources.Standard_FileHeader);
            }
        }

        /// <summary>Gereksinimin standart özeti: "810G 509.5 · I" (yoksa boş).</summary>
        public static string StandardShort(VerificationItem item)
        {
            if (item == null || item.Standard.Trim().Length == 0)
            {
                return string.Empty;
            }

            var text = StandardList.ShortText(item.Standard, item.StandardMethod);
            return item.Procedure.Trim().Length > 0 ? text + " · " + item.Procedure.Trim() : text;
        }

        /// <summary>Gereksinimin standardı uzun: "MIL-STD-810G 509.5 · Tuz sisi, Prosedür I" (yoksa boş).</summary>
        public static string StandardLong(VerificationItem item)
        {
            if (item == null || item.Standard.Trim().Length == 0)
            {
                return string.Empty;
            }

            var text = item.Standard.Trim() + (item.StandardMethod.Trim().Length > 0 ? " " + item.StandardMethod.Trim() : string.Empty);
            return item.Procedure.Trim().Length > 0 ? text + ", " + string.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.Standard_Procedure, item.Procedure.Trim()) : text;
        }
    }
}
