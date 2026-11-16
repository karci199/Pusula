using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Pusula.Core.Testing;
using Pusula.Core.Tracking;
using Pusula.Wpf.Mvvm;
using Pusula.Wpf.Properties;

namespace Pusula.Wpf.ViewModels
{
    /// <summary>Doğrulamalar sekmesinde bir satır.</summary>
    public sealed class VerificationRow
    {
        /// <summary>Satırı maddeden kurar.</summary>
        public VerificationRow(VerificationItem item)
        {
            Item = item;
            var status = item.Status.Trim();
            StatusText = status.Length > 0 ? status : Resources.Verify_StatusPending;
            StatusKind = KindOf(status);
        }

        /// <summary>Durum seçenekleri sırayla: Bekliyor, Planlandı, Yapılıyor, Geçti, Kaldı, Tekrarlanacak, Kapsam dışı.</summary>
        public static string[] Statuses
        {
            get
            {
                return new[]
                {
                    Resources.Verify_StatusPending, Resources.Verify_StatusPlanned, Resources.Verify_StatusInProgress, Resources.Verify_StatusPassed,
                    Resources.Verify_StatusFailed, Resources.Verify_StatusRepeat, Resources.Verify_StatusNotApplicable,
                };
            }
        }

        /// <summary>Durum metninin türü (renk için): 0 bekliyor (ya da tanınmayan), 1 geçti, 2 kaldı, 3 yapılıyor, 4 tekrarlanacak, 5 kapsam dışı, 6 planlandı.</summary>
        public static int KindOf(string status)
        {
            var text = (status ?? string.Empty).Trim();
            var kinds = new[] { 0, 6, 3, 1, 2, 4, 5 };
            var names = Statuses;
            for (var i = 0; i < names.Length; i++)
            {
                if (string.Equals(text, names[i], StringComparison.CurrentCultureIgnoreCase))
                {
                    return kinds[i];
                }
            }

            return 0;
        }

        /// <summary>İlerlemeye sayılır mı (kapsam dışı sayılmaz).</summary>
        public bool Counts
        {
            get { return StatusKind != 5; }
        }

        /// <summary>Md'deki madde.</summary>
        public VerificationItem Item { get; }

        /// <summary>Sıra numarası.</summary>
        public int Number
        {
            get { return Item.Number; }
        }

        /// <summary>Madde metni.</summary>
        public string Text
        {
            get { return Item.Text; }
        }

        /// <summary>Gösterilen metin: madde ya da (henüz yazılmadıysa) "Maddeler sonra yazılacak".</summary>
        public string DisplayText
        {
            get { return Item.Text.Length > 0 ? Item.Text : Resources.Verify_TextLater; }
        }

        /// <summary>Madde metni henüz yazılmadı (yalnız alt grup var).</summary>
        public bool IsTextLater
        {
            get { return Item.Text.Length == 0; }
        }

        /// <summary>TP no.</summary>
        public string TpNo
        {
            get { return Item.TpNo; }
        }

        /// <summary>TR no.</summary>
        public string TrNo
        {
            get { return Item.TrNo; }
        }

        /// <summary>Alt grup.</summary>
        public string SubGroup
        {
            get { return Item.SubGroup; }
        }

        /// <summary>Altyapı.</summary>
        public string Facility
        {
            get { return Item.Facility; }
        }

        /// <summary>Altyapı yazılı mı (etiket gösterilir).</summary>
        public bool HasFacility
        {
            get { return Item.Facility.Length > 0; }
        }

        /// <summary>TG no.</summary>
        public string TgNo
        {
            get { return Item.TgNo; }
        }

        /// <summary>Durum metni (boşsa "Bekliyor").</summary>
        public string StatusText { get; }

        /// <summary>0 bekliyor, 1 geçti, 2 kaldı, 3 yapılıyor, 4 tekrarlanacak, 5 kapsam dışı, 6 planlandı (renk için).</summary>
        public int StatusKind { get; }
    }

    /// <summary>Doğrulamalar sekmesinde bir grup (İşlevsel, Özellik, ÇK/EMC, Diğer) ve maddeleri.</summary>
    public sealed class VerificationGroup
    {
        /// <summary>Grubu kurar.</summary>
        public VerificationGroup(string title, IList<VerificationRow> rows)
        {
            Title = title;
            Rows = rows;
        }

        /// <summary>Grup adı.</summary>
        public string Title { get; }

        /// <summary>Maddeler.</summary>
        public IList<VerificationRow> Rows { get; }

        /// <summary>"· 5 · 2 geçti".</summary>
        public string Summary
        {
            get
            {
                var passed = Rows.Count(r => r.StatusKind == 1);
                var text = "· " + Rows.Count.ToString(CultureInfo.CurrentCulture);
                return passed == 0 ? text : text + " · " + string.Format(CultureInfo.CurrentCulture, Resources.Verify_Passed, passed);
            }
        }

        /// <summary>Geçen maddelerin yüzdesi (ilerleme çubuğu; kapsam dışı sayılmaz).</summary>
        public double Progress
        {
            get
            {
                var counted = Rows.Count(r => r.Counts);
                return counted == 0 ? 0 : 100.0 * Rows.Count(r => r.StatusKind == 1) / counted;
            }
        }
    }

    /// <summary>
    /// Listeden seç ya da yeni yaz: seçenekler "—" (boş), var olan adlar ve en sonda "+ Yeni …"; son seçilince
    /// altında yazı kutusu açılır. Değer, seçili ad ya da yazılan yeni addır.
    /// </summary>
    public sealed class PickOrNew : ViewModelBase
    {
        private readonly string newLabel;
        private string selected;
        private string newText = string.Empty;

        /// <summary>Boş seçimle kurar; <paramref name="newLabel"/> listenin sonundaki "+ Yeni …" metnidir.</summary>
        public PickOrNew(string newLabel)
        {
            this.newLabel = newLabel;
            selected = Resources.Verify_None;
            Options.Add(Resources.Verify_None);
            Options.Add(newLabel);
        }

        /// <summary>Seçenekler.</summary>
        public ObservableCollection<string> Options { get; } = new ObservableCollection<string>();

        /// <summary>Seçili seçenek.</summary>
        public string Selected
        {
            get
            {
                return selected;
            }

            set
            {
                if (SetProperty(ref selected, value ?? Resources.Verify_None))
                {
                    OnPropertyChanged(nameof(IsNew));
                }
            }
        }

        /// <summary>"+ Yeni …" seçili mi (yazı kutusu görünür).</summary>
        public bool IsNew
        {
            get { return selected == newLabel; }
        }

        /// <summary>Yeni ad.</summary>
        public string NewText
        {
            get { return newText; }
            set { SetProperty(ref newText, value ?? string.Empty); }
        }

        /// <summary>Değer: yeni ad, seçili ad ya da boş.</summary>
        public string Value
        {
            get
            {
                if (IsNew)
                {
                    return NewText.Trim();
                }

                return selected == Resources.Verify_None ? string.Empty : selected;
            }

            set
            {
                var name = (value ?? string.Empty).Trim();
                var match = Options.Skip(1).Take(Math.Max(0, Options.Count - 2)).FirstOrDefault(o => string.Equals(o, name, StringComparison.InvariantCultureIgnoreCase));
                if (name.Length == 0)
                {
                    Selected = Resources.Verify_None;
                }
                else if (match != null)
                {
                    Selected = match;
                }
                else
                {
                    NewText = name;
                    Selected = newLabel;
                }
            }
        }

        /// <summary>Seçenekleri yeniler (seçim boşa döner).</summary>
        public void SetNames(IEnumerable<string> names)
        {
            Options.Clear();
            Options.Add(Resources.Verify_None);
            foreach (var name in names)
            {
                Options.Add(name);
            }

            Options.Add(newLabel);
            NewText = string.Empty;
            Selected = Resources.Verify_None;
        }
    }
    /// <summary>Seçmeli düğme (süzgeç ya da grup seçimi): metin ve işaret.</summary>
    public sealed class ChoiceOption : ViewModelBase
    {
        private readonly Action<ChoiceOption> changed;
        private bool isChecked;
        private string text;

        /// <summary>Seçeneği kurar; işaretlenince <paramref name="changed"/> çağrılır.</summary>
        public ChoiceOption(string key, string text, bool isChecked, Action<ChoiceOption> changed)
        {
            Key = key;
            this.text = text;
            this.isChecked = isChecked;
            this.changed = changed;
        }

        /// <summary>Anahtar (grup adı; "Hepsi" için boş).</summary>
        public string Key { get; }

        /// <summary>Görünen metin.</summary>
        public string Text
        {
            get { return text; }
            set { SetProperty(ref text, value); }
        }

        /// <summary>Seçili mi.</summary>
        public bool IsChecked
        {
            get
            {
                return isChecked;
            }

            set
            {
                if (SetProperty(ref isChecked, value) && value)
                {
                    changed(this);
                }
            }
        }
    }

    /// <summary>
    /// Stok sayfası · Doğrulamalar: <c>03_Testler\&lt;stok&gt;_Test_Gereksinimleri.md</c> tablosu gruplu gösterilir;
    /// "+ Madde" penceresi tabloya satır ekler, yeni altyapıyı ortak listeye (<c>_TAKIP\Altyapilar.md</c>) yazar.
    /// </summary>
    public sealed partial class MainViewModel
    {
        private List<VerificationItem> verificationItems = new List<VerificationItem>();
        private string verificationFilter = string.Empty;
        private string verificationStock = string.Empty;
        private bool isVerificationDialogOpen;
        private string newVerText = string.Empty;
        private string newVerTgNo = string.Empty;
        private string newVerTpNo = string.Empty;
        private string newVerTrNo = string.Empty;
        private string newVerStatus = string.Empty;
        private string verificationError = string.Empty;
        private bool keepVerificationOpen;
        private ICommand? addVerificationCommand;
        private ICommand? openVerificationFileCommand;
        private VerificationItem? editingVerification;
        private VerificationItem? pendingVerificationDelete;
        private bool isVerificationDeleteOpen;

        /// <summary>Gruplu maddeler (seçili süzgece göre).</summary>
        public ObservableCollection<VerificationGroup> VerificationGroups { get; } = new ObservableCollection<VerificationGroup>();

        /// <summary>Grup süzgeci: Hepsi · İşlevsel · Özellik · ÇK/EMC · Diğer (sayılarıyla).</summary>
        public ObservableCollection<ChoiceOption> VerificationFilters { get; } = new ObservableCollection<ChoiceOption>();

        /// <summary>Madde sayısı (sekme rozeti).</summary>
        public int VerificationCount
        {
            get { return verificationItems.Count; }
        }

        /// <summary>Sayı satırında: "Doğrulama 3/8" (kapsam dışı sayılmaz); madde yoksa boş.</summary>
        public string StockVerificationText
        {
            get
            {
                if (verificationItems.Count == 0)
                {
                    return string.Empty;
                }

                var kinds = verificationItems.Select(i => VerificationRow.KindOf(i.Status)).ToList();
                return string.Format(CultureInfo.CurrentCulture, Resources.Verify_Summary, kinds.Count(k => k == 1), kinds.Count(k => k != 5));
            }
        }

        /// <summary>Sayı satırındaki ilerleme çubuğu: geçen maddelerin yüzdesi (kapsam dışı sayılmaz).</summary>
        public double VerificationProgress
        {
            get
            {
                var kinds = verificationItems.Select(i => VerificationRow.KindOf(i.Status)).ToList();
                var counted = kinds.Count(k => k != 5);
                return counted == 0 ? 0 : 100.0 * kinds.Count(k => k == 1) / counted;
            }
        }

        /// <summary>Stokun test gereksinimleri dosyası var mı ("Dosyayı aç" düğmesi).</summary>
        public bool HasVerificationFile
        {
            get { return verificationStock.Length > 0 && File.Exists(VerificationList.PathFor(verificationStock)); }
        }

        /// <summary>"Dosyayı aç": test gereksinimleri md'sini not programında açar.</summary>
        public ICommand OpenVerificationFileCommand
        {
            get
            {
                return openVerificationFileCommand ?? (openVerificationFileCommand = new RelayCommand(() =>
                {
                    if (verificationStock.Length > 0)
                    {
                        OpenNoteOrFolder(VerificationList.PathFor(verificationStock), Path.Combine(verificationStock, VerificationList.FolderName));
                    }
                }));
            }
        }

        /// <summary>Pencere başlığı: yeni madde ya da düzenleme.</summary>
        public string VerificationDialogTitle
        {
            get { return editingVerification == null ? Resources.Verify_DialogTitle : Resources.Verify_EditTitle; }
        }

        /// <summary>Pencere onay düğmesi: Ekle ya da Kaydet.</summary>
        public string VerificationDialogButton
        {
            get { return editingVerification == null ? Resources.Dialog_Add : Resources.Verify_Save; }
        }

        /// <summary>Pencere düzenleme kipinde mi ("yenisini aç" gizlenir).</summary>
        public bool IsEditingVerification
        {
            get { return editingVerification != null; }
        }

        /// <summary>Silme onayı açık mı.</summary>
        public bool IsVerificationDeleteOpen
        {
            get { return isVerificationDeleteOpen; }
            set { SetDialogOpen(ref isVerificationDeleteOpen, value); }
        }

        /// <summary>Silme onayında gösterilen madde.</summary>
        public string VerificationDeleteText
        {
            get
            {
                return pendingVerificationDelete == null ? string.Empty
                    : string.Format(CultureInfo.CurrentCulture, Resources.Verify_DeleteText, pendingVerificationDelete.Number, pendingVerificationDelete.Text);
            }
        }

        /// <summary>Satırı düzenleme penceresinde açar (değerler doldurulur).</summary>
        public void EditVerification(VerificationRow row)
        {
            if (row == null || verificationStock.Length == 0)
            {
                return;
            }

            var item = row.Item;
            editingVerification = item;
            ResetVerificationForm(item.Group.Length > 0 ? item.Group : Resources.Verify_GroupOther);
            NewVerText = item.Text;
            NewVerSubGroup = item.SubGroup;
            NewVerFacility = item.Facility;
            NewVerTgNo = item.TgNo;
            NewVerTpNo = item.TpNo;
            NewVerTrNo = item.TrNo;
            NewVerStatus = item.Status.Length > 0 ? item.Status : Resources.Verify_StatusPending;
            NotifyVerificationDialog();
            IsVerificationDialogOpen = true;
        }

        /// <summary>Satırın durumunu değiştirir (yalnız o satırın durum hücresi yazılır).</summary>
        public void SetVerificationStatus(VerificationRow row, string status)
        {
            if (row == null || verificationStock.Length == 0 || string.Equals(row.Item.Status, status, StringComparison.CurrentCulture))
            {
                return;
            }

            var item = Copy(row.Item);
            item.Status = status ?? string.Empty;
            if (WriteVerification(VerificationList.Update(verificationStock, item)))
            {
                Done(string.Format(CultureInfo.CurrentCulture, Resources.Verify_StatusSet, item.Number, item.Status));
            }
        }

        /// <summary>Silmeden önce sorar.</summary>
        public void RequestDeleteVerification(VerificationRow row)
        {
            if (row == null)
            {
                return;
            }

            pendingVerificationDelete = row.Item;
            OnPropertyChanged(nameof(VerificationDeleteText));
            IsVerificationDeleteOpen = true;
        }

        /// <summary>Onaylanan maddeyi tablodan kaldırır.</summary>
        public void ConfirmDeleteVerification()
        {
            IsVerificationDeleteOpen = false;
            var item = pendingVerificationDelete;
            pendingVerificationDelete = null;
            if (item != null && verificationStock.Length > 0 && WriteVerification(VerificationList.Remove(verificationStock, item.Number)))
            {
                Done(string.Format(CultureInfo.CurrentCulture, Resources.Verify_Deleted, item.Number, item.Text));
            }
        }

        // Yazma sonucunu işler: liste yeniden okunur; hata ya da bulunamayan satır durum çubuğunda. Yazıldıysa true.
        private bool WriteVerification(string error)
        {
            LoadVerifications();
            if (error == "missing")
            {
                Status = Resources.Verify_Missing;
                return false;
            }

            if (error.Length > 0)
            {
                Status = string.Format(CultureInfo.CurrentCulture, Resources.Verify_WriteFailed, error);
                return false;
            }

            return true;
        }

        private static VerificationItem Copy(VerificationItem item)
        {
            var copy = new VerificationItem
            {
                Number = item.Number,
                Text = item.Text,
                Group = item.Group,
                SubGroup = item.SubGroup,
                Facility = item.Facility,
                TgNo = item.TgNo,
                TpNo = item.TpNo,
                TrNo = item.TrNo,
                Status = item.Status,
            };
            foreach (var pair in item.Extra)
            {
                copy.Extra[pair.Key] = pair.Value;
            }

            return copy;
        }

        private void NotifyVerificationDialog()
        {
            OnPropertyChanged(nameof(VerificationDialogTitle));
            OnPropertyChanged(nameof(VerificationDialogButton));
            OnPropertyChanged(nameof(IsEditingVerification));
            OnPropertyChanged(nameof(VerificationFileText));
        }

        // Birim notunun YAML'ına test gereksinimleri bağlantısı (alan yoksa) yazılır.
        private void LinkVerificationFromUnitNote()
        {
            var cover = Detail.Cover;
            if (cover.HasNote && string.Equals(cover.StockFolder, verificationStock, StringComparison.OrdinalIgnoreCase))
            {
                var error = VerificationList.LinkFromNote(cover.NoteFile, verificationStock);
                if (error.Length > 0)
                {
                    Status = string.Format(CultureInfo.CurrentCulture, Resources.Verify_WriteFailed, error);
                }
            }
        }

        /// <summary>Hiç madde yok (boş durum metni gösterilir).</summary>
        public bool IsVerificationEmpty
        {
            get { return verificationItems.Count == 0; }
        }

        /// <summary>"+ Madde": ekleme penceresini açar.</summary>
        public ICommand AddVerificationCommand
        {
            get { return addVerificationCommand ?? (addVerificationCommand = new RelayCommand(OpenNewVerification)); }
        }

        /// <summary>Ekleme penceresi açık mı.</summary>
        public bool IsVerificationDialogOpen
        {
            get { return isVerificationDialogOpen; }
            set { SetDialogOpen(ref isVerificationDialogOpen, value); }
        }

        /// <summary>Pencerede: madde metni (zorunlu).</summary>
        public string NewVerText
        {
            get { return newVerText; }
            set { SetProperty(ref newVerText, value); }
        }

        /// <summary>Pencerede: grup seçenekleri (biri seçili).</summary>
        public ObservableCollection<ChoiceOption> VerificationGroupChoices { get; } = new ObservableCollection<ChoiceOption>();

        /// <summary>Pencerede: alt grup (bu stokta kullanılanlardan seçilir ya da yeni yazılır).</summary>
        public PickOrNew SubGroupPick { get; } = new PickOrNew(Resources.Verify_NewSubGroup);

        /// <summary>Pencerede: altyapı (ortak listeden seçilir ya da yeni yazılır; yeni ad ortak listeye eklenir).</summary>
        public PickOrNew FacilityPick { get; } = new PickOrNew(Resources.Verify_NewFacility);

        /// <summary>Alt grup değeri (seçili ya da yeni yazılan).</summary>
        public string NewVerSubGroup
        {
            get { return SubGroupPick.Value; }
            set { SubGroupPick.Value = value; }
        }

        /// <summary>Altyapı değeri (seçili ya da yeni yazılan).</summary>
        public string NewVerFacility
        {
            get { return FacilityPick.Value; }
            set { FacilityPick.Value = value; }
        }

        /// <summary>Altyapı listesi (ortak liste + bu stokta kullanılanlar).</summary>
        public IList<string> FacilitySuggestions
        {
            get { return FacilityPick.Options.Skip(1).Take(Math.Max(0, FacilityPick.Options.Count - 2)).ToList(); }
        }

        /// <summary>Alt grup önerileri (bu stokta kullanılanlar).</summary>
        public IList<string> SubGroupSuggestions
        {
            get { return SubGroupPick.Options.Skip(1).Take(Math.Max(0, SubGroupPick.Options.Count - 2)).ToList(); }
        }
        /// <summary>Pencerede: TG no (isteğe bağlı).</summary>
        public string NewVerTgNo
        {
            get { return newVerTgNo; }
            set { SetProperty(ref newVerTgNo, value ?? string.Empty); }
        }

        /// <summary>Pencerede: TP no (isteğe bağlı).</summary>
        public string NewVerTpNo
        {
            get { return newVerTpNo; }
            set { SetProperty(ref newVerTpNo, value ?? string.Empty); }
        }

        /// <summary>Pencerede: TR no (isteğe bağlı).</summary>
        public string NewVerTrNo
        {
            get { return newVerTrNo; }
            set { SetProperty(ref newVerTrNo, value ?? string.Empty); }
        }

        /// <summary>Pencerede: durum.</summary>
        public string NewVerStatus
        {
            get { return newVerStatus; }
            set { SetProperty(ref newVerStatus, value ?? string.Empty); }
        }

        /// <summary>Durum seçenekleri: Bekliyor, Planlandı, Yapılıyor, Geçti, Kaldı, Tekrarlanacak, Kapsam dışı.</summary>
        public IList<string> VerificationStatuses
        {
            get { return VerificationRow.Statuses; }
        }

        /// <summary>Ekledikten sonra pencere yeni madde için açık kalsın.</summary>
        public bool KeepVerificationOpen
        {
            get { return keepVerificationOpen; }
            set { SetProperty(ref keepVerificationOpen, value); }
        }

        /// <summary>Pencerede hata metni.</summary>
        public string VerificationError
        {
            get { return verificationError; }
            set { SetProperty(ref verificationError, value); }
        }

        /// <summary>Pencere alt başlığı: maddenin yazılacağı dosya (stok klasörüne göre).</summary>
        public string VerificationFileText
        {
            get
            {
                if (verificationStock.Length == 0)
                {
                    return string.Empty;
                }

                var path = VerificationList.PathFor(verificationStock);
                return Path.GetFileName(verificationStock) + " · " + path.Substring(verificationStock.Length).TrimStart('\\');
            }
        }

        private static string[] KnownGroups()
        {
            return new[] { Resources.Verify_GroupFunctional, Resources.Verify_GroupFeature, Resources.Verify_GroupEmc, Resources.Verify_GroupOther };
        }

        // Stok değişince ya da madde eklenince dosyadan yeniden okunur.
        private void LoadVerifications()
        {
            var stock = selectedNode != null && selectedNode.Kind == FolderKind.Stock ? selectedNode.Path : string.Empty;
            if (!string.Equals(stock, verificationStock, StringComparison.OrdinalIgnoreCase))
            {
                verificationFilter = string.Empty;
            }

            verificationStock = stock;
            verificationItems = stock.Length == 0 ? new List<VerificationItem>() : VerificationList.Read(stock).ToList();

            // Süzgeç: Hepsi + dört grup (+ dosyada başka grup adı varsa o da).
            var groups = KnownGroups().ToList();
            foreach (var extra in verificationItems.Select(i => i.Group).Where(g => g.Length > 0 && !groups.Contains(g, StringComparer.CurrentCultureIgnoreCase)).Distinct())
            {
                groups.Add(extra);
            }

            if (verificationFilter.Length > 0 && !groups.Contains(verificationFilter, StringComparer.CurrentCultureIgnoreCase))
            {
                verificationFilter = string.Empty;
            }

            VerificationFilters.Clear();
            VerificationFilters.Add(new ChoiceOption(string.Empty, Resources.Home_RangeAll + " " + verificationItems.Count.ToString(CultureInfo.CurrentCulture),
                verificationFilter.Length == 0, OnVerificationFilter));
            foreach (var group in groups)
            {
                var count = verificationItems.Count(i => SameGroup(i, group));
                VerificationFilters.Add(new ChoiceOption(group, group + " " + count.ToString(CultureInfo.CurrentCulture),
                    string.Equals(group, verificationFilter, StringComparison.CurrentCultureIgnoreCase), OnVerificationFilter));
            }

            FillVerificationGroups();
            OnPropertyChanged(nameof(VerificationCount));
            OnPropertyChanged(nameof(StockVerificationText));
            OnPropertyChanged(nameof(VerificationProgress));
            OnPropertyChanged(nameof(IsVerificationEmpty));
            OnPropertyChanged(nameof(HasVerificationFile));
        }

        // Grubu boş madde "Diğer"e sayılır.
        private static bool SameGroup(VerificationItem item, string group)
        {
            var own = item.Group.Length > 0 ? item.Group : Resources.Verify_GroupOther;
            return string.Equals(own, group, StringComparison.CurrentCultureIgnoreCase);
        }

        private void OnVerificationFilter(ChoiceOption option)
        {
            verificationFilter = option.Key;
            foreach (var other in VerificationFilters.Where(o => o != option))
            {
                other.IsChecked = false;
            }

            FillVerificationGroups();
        }

        private void FillVerificationGroups()
        {
            VerificationGroups.Clear();
            foreach (var option in VerificationFilters.Where(o => o.Key.Length > 0))
            {
                if (verificationFilter.Length > 0 && !string.Equals(option.Key, verificationFilter, StringComparison.CurrentCultureIgnoreCase))
                {
                    continue;
                }

                var rows = verificationItems.Where(i => SameGroup(i, option.Key)).OrderBy(i => i.Number).Select(i => new VerificationRow(i)).ToList();
                if (rows.Count > 0)
                {
                    VerificationGroups.Add(new VerificationGroup(option.Key, rows));
                }
            }
        }

        private void OpenNewVerification()
        {
            if (verificationStock.Length == 0)
            {
                return;
            }

            var group = verificationFilter.Length > 0 ? verificationFilter : Resources.Verify_GroupFunctional;
            editingVerification = null;
            ResetVerificationForm(group);
            NotifyVerificationDialog();
            IsVerificationDialogOpen = true;
        }

        private void ResetVerificationForm(string group)
        {
            NewVerText = string.Empty;
            NewVerTgNo = string.Empty;
            NewVerTpNo = string.Empty;
            NewVerTrNo = string.Empty;
            NewVerStatus = Resources.Verify_StatusPending;
            VerificationError = string.Empty;

            VerificationGroupChoices.Clear();
            foreach (var name in KnownGroups())
            {
                VerificationGroupChoices.Add(new ChoiceOption(name, name, string.Equals(name, group, StringComparison.CurrentCultureIgnoreCase), OnVerificationGroupChoice));
            }

            if (!VerificationGroupChoices.Any(c => c.IsChecked))
            {
                VerificationGroupChoices[0].IsChecked = true;
            }

            // Alt grup: bu stokta kullanılanlar; altyapı: ortak liste + bu stokta kullanılanlar.
            SubGroupPick.SetNames(verificationItems.Select(i => i.SubGroup).Where(s => s.Length > 0).Distinct(StringComparer.CurrentCultureIgnoreCase).OrderBy(s => s, StringComparer.CurrentCulture));
            var facilities = FacilityList.Read(rootPath).ToList();
            foreach (var used in verificationItems.Select(i => i.Facility).Where(f => f.Length > 0))
            {
                if (!facilities.Contains(used, StringComparer.CurrentCultureIgnoreCase))
                {
                    facilities.Add(used);
                }
            }

            FacilityPick.SetNames(facilities);
        }

        private void OnVerificationGroupChoice(ChoiceOption option)
        {
            foreach (var other in VerificationGroupChoices.Where(o => o != option))
            {
                other.IsChecked = false;
            }
        }

        /// <summary>
        /// Pencereyi onaylar: maddeyi tabloya ekler, yeni altyapıyı ortak listeye yazar. Pencere kapanabilirse true;
        /// hata varsa ya da "yenisini aç" işaretliyse false (pencere yeniden açılır).
        /// </summary>
        public bool ConfirmNewVerification()
        {
            // Madde henüz belli değilse yalnız alt grup yazılabilir (örn. "Anten örüntü"); madde sonra doldurulur.
            var text = (NewVerText ?? string.Empty).Trim();
            if (text.Length == 0 && NewVerSubGroup.Trim().Length == 0)
            {
                VerificationError = Resources.Verify_TextOrSubGroup;
                return false;
            }

            if (verificationStock.Length == 0)
            {
                return true;
            }

            var group = VerificationGroupChoices.FirstOrDefault(c => c.IsChecked)?.Key ?? Resources.Verify_GroupFunctional;
            var item = new VerificationItem
            {
                Text = text,
                Group = group,
                SubGroup = NewVerSubGroup.Trim(),
                Facility = NewVerFacility.Trim(),
                TgNo = NewVerTgNo.Trim(),
                TpNo = NewVerTpNo.Trim(),
                TrNo = NewVerTrNo.Trim(),
                Status = NewVerStatus.Trim(),
            };

            if (editingVerification != null)
            {
                // Düzenleme: aynı numaralı satır yerinde değişir.
                item.Number = editingVerification.Number;
                var updateError = VerificationList.Update(verificationStock, item);
                if (updateError.Length > 0 && updateError != "missing")
                {
                    VerificationError = string.Format(CultureInfo.CurrentCulture, Resources.Verify_WriteFailed, updateError);
                    return false;
                }

                editingVerification = null;
                if (updateError.Length == 0 && item.Facility.Length > 0 && rootPath.Length > 0)
                {
                    FacilityList.Add(rootPath, item.Facility, Resources.Verify_FacilityTemplate);
                }

                if (WriteVerification(updateError))
                {
                    Done(string.Format(CultureInfo.CurrentCulture, Resources.Verify_Updated, item.Number, text.Length > 0 ? text : item.SubGroup));
                }

                return true;
            }

            var template = string.Format(CultureInfo.CurrentCulture, Resources.Verify_FileTemplate, Path.GetFileName(verificationStock));
            var error = VerificationList.Add(verificationStock, item, template);
            if (error.Length > 0)
            {
                VerificationError = string.Format(CultureInfo.CurrentCulture, Resources.Verify_WriteFailed, error);
                return false;
            }

            if (item.Facility.Length > 0 && rootPath.Length > 0)
            {
                FacilityList.Add(rootPath, item.Facility, Resources.Verify_FacilityTemplate);
            }

            LinkVerificationFromUnitNote();
            LoadVerifications();
            Done(string.Format(CultureInfo.CurrentCulture, Resources.Verify_Added, item.Number, text.Length > 0 ? text : item.SubGroup));
            if (KeepVerificationOpen)
            {
                // Grup, alt grup ve altyapı bir sonraki madde için kalır.
                var sub = item.SubGroup;
                var facility = item.Facility;
                ResetVerificationForm(group);
                NewVerSubGroup = sub;
                NewVerFacility = facility;
                return false;
            }

            return true;
        }
    }
}
