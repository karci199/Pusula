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
    /// <summary>Doğrulamalar sekmesinde bir satır (madde); tıklanınca açılır, tam gereksinim ve plan / rapor özeti görünür.</summary>
    public sealed class VerificationRow : ViewModelBase
    {
        private bool isExpanded;

        /// <summary>Satırı maddeden kurar.</summary>
        public VerificationRow(VerificationItem item)
        {
            Item = item;
            var status = item.Status.Trim();
            StatusText = status.Length > 0 ? status : Resources.Verify_StatusPending;
            StatusKind = KindOf(status);
        }

        /// <summary>
        /// Durum seçenekleri sırayla: Bekliyor, Planlandı, Hazırlık yapılıyor, Yapılıyor, Değerlendiriliyor, Geçti, Kaldı,
        /// Şartlı kabul, Tekrarlanacak, Kapsam dışı.
        /// </summary>
        public static string[] Statuses
        {
            get
            {
                return new[]
                {
                    Resources.Verify_StatusPending, Resources.Verify_StatusPlanned, Resources.Verify_StatusPreparing,
                    Resources.Verify_StatusInProgress, Resources.Verify_StatusEvaluating, Resources.Verify_StatusPassed,
                    Resources.Verify_StatusFailed, Resources.Verify_StatusConditional, Resources.Verify_StatusRepeat,
                    Resources.Verify_StatusNotApplicable,
                };
            }
        }

        /// <summary>Doğrulama yöntemleri: Test, Muayene, Belge, Gösterim, Diğer.</summary>
        public static string[] Methods
        {
            get { return new[] { Resources.Verify_MethodTest, Resources.Verify_MethodInspection, Resources.Verify_MethodDocument, Resources.Verify_MethodDemonstration, Resources.Verify_MethodOther }; }
        }

        /// <summary>
        /// Durum metninin türü (renk için): 0 bekliyor (ya da tanınmayan), 1 geçti, 2 kaldı, 3 yapılıyor, 4 tekrarlanacak,
        /// 5 kapsam dışı, 6 planlandı, 7 hazırlık yapılıyor, 8 değerlendiriliyor, 9 şartlı kabul.
        /// </summary>
        public static int KindOf(string status)
        {
            var text = (status ?? string.Empty).Trim();
            var kinds = new[] { 0, 6, 7, 3, 8, 1, 2, 9, 4, 5 };
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

        /// <summary>Durum geçti sayılır mı (geçti ya da şartlı kabul).</summary>
        public static bool IsPassedKind(int kind)
        {
            return kind == 1 || kind == 9;
        }

        /// <summary>Md'deki madde.</summary>
        public VerificationItem Item { get; }

        /// <summary>Açık mı (tam gereksinim ve plan / rapor özeti görünür).</summary>
        public bool IsExpanded
        {
            get { return isExpanded; }
            set { SetProperty(ref isExpanded, value); }
        }

        /// <summary>İlerlemeye sayılır mı (kapsam dışı sayılmaz).</summary>
        public bool Counts
        {
            get { return StatusKind != 5; }
        }

        /// <summary>Geçti ya da şartlı kabul.</summary>
        public bool IsPassed
        {
            get { return IsPassedKind(StatusKind); }
        }

        /// <summary>Sıra numarası.</summary>
        public int Number
        {
            get { return Item.Number; }
        }

        /// <summary>Kimlik sütunu: TG no; yoksa kaynak doküman; o da yoksa "—".</summary>
        public string Id
        {
            get { return Item.TgNo.Length > 0 ? Item.TgNo : Item.Source.Length > 0 ? Item.Source : Resources.Verify_None; }
        }

        /// <summary>TG no yok (kimlik sütununda soluk doküman adı gösterilir).</summary>
        public bool IsSourceOnly
        {
            get { return Item.TgNo.Length == 0; }
        }

        /// <summary>Başlık.</summary>
        public string Text
        {
            get { return Item.Text; }
        }

        /// <summary>Gösterilen başlık: başlık ya da (henüz yazılmadıysa) "Maddeler sonra yazılacak".</summary>
        public string DisplayText
        {
            get { return Item.Text.Length > 0 ? Item.Text : Item.SubGroup.Length > 0 ? Item.SubGroup + " · " + Resources.Verify_TextLater : Resources.Verify_TextLater; }
        }

        /// <summary>Başlık henüz yazılmadı.</summary>
        public bool IsTextLater
        {
            get { return Item.Text.Length == 0; }
        }

        /// <summary>Kısa not.</summary>
        public string Description
        {
            get { return Item.Description; }
        }

        /// <summary>Kısa not var mı.</summary>
        public bool HasDescription
        {
            get { return Item.Description.Length > 0; }
        }

        /// <summary>Tam gereksinim; yazılmadıysa kısa not.</summary>
        public string Requirement
        {
            get { return Item.Requirement.Length > 0 ? Item.Requirement : Item.Description; }
        }

        /// <summary>Açılan satırda gösterilecek gereksinim metni var mı.</summary>
        public bool HasRequirement
        {
            get { return Requirement.Length > 0; }
        }

        /// <summary>Açılan satırın kaynak satırı: "AB-7000-0002_TG_AA · TG-4 · Anten örüntü" (boş olanlar atlanır).</summary>
        public string SourceLine { get; set; } = string.Empty;

        /// <summary>Tür: Temel, Türetilmiş ya da "—".</summary>
        public string KindText
        {
            get { return Item.Kind.Length > 0 ? Item.Kind : Resources.Verify_None; }
        }

        /// <summary>Türetilmişse bağlı temel madde ("← TG-1"); yoksa boş.</summary>
        public string ParentText
        {
            get { return Item.Parent.Length > 0 ? "← " + Item.Parent : string.Empty; }
        }

        /// <summary>Bağlı temel madde yazılı mı.</summary>
        public bool HasParent
        {
            get { return Item.Parent.Length > 0; }
        }

        /// <summary>Yöntem.</summary>
        public string Method
        {
            get { return Item.Method; }
        }

        /// <summary>Yöntem yazılı mı.</summary>
        public bool HasMethod
        {
            get { return Item.Method.Length > 0; }
        }

        /// <summary>Yöntemin rengi: 1 test, 2 muayene, 3 belge, 4 gösterim, 0 diğer.</summary>
        public int MethodTone
        {
            get
            {
                var methods = Methods;
                for (var i = 0; i < 4; i++)
                {
                    if (string.Equals(Item.Method, methods[i], StringComparison.CurrentCultureIgnoreCase))
                    {
                        return i + 1;
                    }
                }

                return 0;
            }
        }

        /// <summary>Test planı madde no; yoksa "—".</summary>
        public string TpText
        {
            get { return Item.TpNo.Length > 0 ? Item.TpNo : Resources.Verify_None; }
        }

        /// <summary>Test raporu madde no; yoksa "—".</summary>
        public string TrText
        {
            get { return Item.TrNo.Length > 0 ? Item.TrNo : Resources.Verify_None; }
        }

        /// <summary>Plan madde no yazılı mı.</summary>
        public bool HasTp
        {
            get { return Item.TpNo.Length > 0; }
        }

        /// <summary>Rapor madde no yazılı mı.</summary>
        public bool HasTr
        {
            get { return Item.TrNo.Length > 0; }
        }

        /// <summary>Açılan satırda plan özeti: "TP-5.2 · Altyapı SATIMO" (boş olanlar atlanır).</summary>
        public string PlanLine
        {
            get
            {
                var parts = new List<string>();
                if (Item.Method.Length > 0)
                {
                    parts.Add(Item.Method);
                }

                if (Item.Facility.Length > 0)
                {
                    parts.Add(string.Format(CultureInfo.CurrentCulture, Resources.Verify_PlanFacility, Item.Facility));
                }

                return parts.Count == 0 ? Resources.Verify_PlanEmpty : string.Join(" · ", parts);
            }
        }

        /// <summary>Açılan satırda rapor özeti: durum ya da "Henüz sonuç yok".</summary>
        public string ReportLine
        {
            get { return Item.TrNo.Length > 0 || IsPassed || StatusKind == 2 ? StatusText : Resources.Verify_ReportEmpty; }
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

        /// <summary>Altyapı yazılı mı.</summary>
        public bool HasFacility
        {
            get { return Item.Facility.Length > 0; }
        }

        /// <summary>TG no.</summary>
        public string TgNo
        {
            get { return Item.TgNo; }
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

        /// <summary>Durum metni (boşsa "Bekliyor").</summary>
        public string StatusText { get; }

        /// <summary>Durum türü (bkz. <see cref="KindOf"/>).</summary>
        public int StatusKind { get; }
    }

    /// <summary>Doğrulamalar sekmesinde bir grup (İşlevsel, Özellik, ÇK/EMC, Diğer) ve maddeleri.</summary>
    public sealed class VerificationGroup
    {
        /// <summary>Çubuğun genişliği (piksel).</summary>
        public const double BarWidth = 90;

        /// <summary>Grubu kurar; <paramref name="tone"/> grubun renk sırasıdır (0 işlevsel, 1 özellik, 2 ÇK/EMC, 3 diğer).</summary>
        public VerificationGroup(string title, int tone, IList<VerificationRow> rows)
        {
            Title = title;
            Tone = tone;
            Rows = rows;
        }

        /// <summary>Grup adı.</summary>
        public string Title { get; }

        /// <summary>Renk sırası.</summary>
        public int Tone { get; }

        /// <summary>Maddeler.</summary>
        public IList<VerificationRow> Rows { get; }

        /// <summary>"4 madde · 2 geçti · 1 kaldı".</summary>
        public string Summary
        {
            get
            {
                var text = string.Format(CultureInfo.CurrentCulture, Resources.Verify_GroupCount, Rows.Count);
                var passed = Rows.Count(r => r.IsPassed);
                var failed = Rows.Count(r => r.StatusKind == 2);
                if (passed > 0)
                {
                    text += " · " + string.Format(CultureInfo.CurrentCulture, Resources.Verify_Passed, passed);
                }

                if (failed > 0)
                {
                    text += " · " + string.Format(CultureInfo.CurrentCulture, Resources.Verify_Failed, failed);
                }

                return text;
            }
        }

        /// <summary>Geçen maddelerin yüzdesi (kapsam dışı sayılmaz).</summary>
        public double Progress
        {
            get
            {
                var counted = Rows.Count(r => r.Counts);
                return counted == 0 ? 0 : 100.0 * Rows.Count(r => r.IsPassed) / counted;
            }
        }

        /// <summary>Çubukta yeşil (geçen) kısmın genişliği.</summary>
        public double PassedWidth
        {
            get
            {
                var counted = Rows.Count(r => r.Counts);
                return counted == 0 ? 0 : BarWidth * Rows.Count(r => r.IsPassed) / counted;
            }
        }

        /// <summary>Çubukta kırmızı (kalan) kısmın genişliği.</summary>
        public double FailedWidth
        {
            get
            {
                var counted = Rows.Count(r => r.Counts);
                return counted == 0 ? 0 : BarWidth * Rows.Count(r => r.StatusKind == 2) / counted;
            }
        }
    }

    /// <summary>Doğrulamalar sekmesinin üstündeki doküman kartı: test gereksinimi, tasarımcı dokümanı, test planı, test raporu.</summary>
    public sealed class VerificationDocument
    {
        /// <summary>Kartı kurar.</summary>
        public VerificationDocument(string mark, string label, string name, int tone)
        {
            Mark = mark;
            Label = label;
            Name = name.Length > 0 ? name : Resources.Verify_DocNone;
            Tone = tone;
            State = name.Length > 0 ? VerificationList.DocumentState(name) : -1;
        }

        /// <summary>Kısaltma (TG, W, TP, TR).</summary>
        public string Mark { get; }

        /// <summary>Doküman türü.</summary>
        public string Label { get; }

        /// <summary>Doküman numarası ya da "belirtilmedi".</summary>
        public string Name { get; }

        /// <summary>Renk sırası (0 TG, 1 tasarımcı, 2 TP, 3 TR).</summary>
        public int Tone { get; }

        /// <summary>-1 yazılmadı, 0 bilinmiyor, 1 yayınlandı, 2 hazırlanıyor.</summary>
        public int State { get; }

        /// <summary>"✓ yayınlandı" / "hazırlanıyor" / boş.</summary>
        public string StateText
        {
            get { return State == 1 ? Resources.Verify_DocPublished : State == 2 ? Resources.Verify_DocDraft : string.Empty; }
        }
    }

    /// <summary>Doküman penceresinde bir alan: tür, açıklama, numara.</summary>
    public sealed class DocumentField : ViewModelBase
    {
        private string value;

        /// <summary>Alanı kurar.</summary>
        public DocumentField(string label, string hint, string value)
        {
            Label = label;
            Hint = hint;
            this.value = value ?? string.Empty;
        }

        /// <summary>Doküman türü.</summary>
        public string Label { get; }

        /// <summary>Örnek / açıklama (yer tutucu).</summary>
        public string Hint { get; }

        /// <summary>Doküman numarası.</summary>
        public string Value
        {
            get { return value; }
            set { SetProperty(ref this.value, value ?? string.Empty); }
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

        /// <summary>Yanındaki sayı rozeti (süzgeçte madde sayısı); yoksa boş.</summary>
        public string Badge { get; set; } = string.Empty;

        /// <summary>Renk sırası (süzgeçteki grup noktası: 0 işlevsel, 1 özellik, 2 ÇK/EMC, 3 diğer; -1 nokta yok).</summary>
        public int Tone { get; set; } = -1;

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
        private string[] verificationDocs = { string.Empty, string.Empty, string.Empty, string.Empty };
        private bool isVerificationDialogOpen;
        private string newVerText = string.Empty;
        private string newVerSource = string.Empty;
        private string newVerDescription = string.Empty;
        private string newVerRequirement = string.Empty;
        private string newVerKind = string.Empty;
        private string newVerParent = string.Empty;
        private string newVerMethod = string.Empty;
        private bool isDocumentsDialogOpen;
        private ICommand? editDocumentsCommand;
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
                return string.Format(CultureInfo.CurrentCulture, Resources.Verify_Summary, kinds.Count(VerificationRow.IsPassedKind), kinds.Count(k => k != 5));
            }
        }

        /// <summary>Sayı satırındaki ilerleme çubuğu: geçen maddelerin yüzdesi (kapsam dışı sayılmaz).</summary>
        public double VerificationProgress
        {
            get
            {
                var kinds = verificationItems.Select(i => VerificationRow.KindOf(i.Status)).ToList();
                var counted = kinds.Count(k => k != 5);
                return counted == 0 ? 0 : 100.0 * kinds.Count(VerificationRow.IsPassedKind) / counted;
            }
        }

        /// <summary>Dokümanlar: test gereksinimi (DOORS), tasarımcı dokümanı, test planı, test raporu (dosyanın YAML başlığından).</summary>
        public ObservableCollection<VerificationDocument> VerificationDocuments { get; } = new ObservableCollection<VerificationDocument>();

        /// <summary>Doküman şeridi görünür mü (en az biri yazılıysa).</summary>
        public bool HasVerificationDocuments
        {
            get { return VerificationDocuments.Any(d => d.State >= 0); }
        }

        /// <summary>"Dokümanlar": doküman numaralarını düzenleme penceresini açar.</summary>
        public ICommand EditDocumentsCommand
        {
            get { return editDocumentsCommand ?? (editDocumentsCommand = new RelayCommand(OpenDocumentsDialog)); }
        }

        /// <summary>Doküman numaraları penceresi açık mı.</summary>
        public bool IsDocumentsDialogOpen
        {
            get { return isDocumentsDialogOpen; }
            set { SetDialogOpen(ref isDocumentsDialogOpen, value); }
        }

        /// <summary>Penceredeki doküman numaraları (test gereksinimi, tasarımcı dokümanı, test planı, test raporu).</summary>
        public ObservableCollection<DocumentField> DocumentFields { get; } = new ObservableCollection<DocumentField>();

        /// <summary>Doküman penceresini doldurup açar.</summary>
        public void OpenDocumentsDialog()
        {
            if (verificationStock.Length == 0)
            {
                return;
            }

            var values = VerificationList.ReadDocuments(verificationStock);
            var labels = new[] { Resources.Verify_DocRequirement, Resources.Verify_DocDesigner, Resources.Verify_DocPlan, Resources.Verify_DocReport };
            var hints = new[] { Resources.Verify_DocRequirementHint, Resources.Verify_DocDesignerHint, Resources.Verify_DocPlanHint, Resources.Verify_DocReportHint };
            DocumentFields.Clear();
            for (var i = 0; i < labels.Length; i++)
            {
                DocumentFields.Add(new DocumentField(labels[i], hints[i], values[i]));
            }

            IsDocumentsDialogOpen = true;
        }

        /// <summary>Doküman numaralarını dosyanın YAML başlığına yazar (dosya yoksa önce şablondan açılır).</summary>
        public void ConfirmDocuments()
        {
            IsDocumentsDialogOpen = false;
            if (verificationStock.Length == 0)
            {
                return;
            }

            var template = string.Format(CultureInfo.CurrentCulture, Resources.Verify_FileTemplate, Path.GetFileName(verificationStock));
            var error = VerificationList.EnsureFile(verificationStock, template);
            if (error.Length == 0)
            {
                error = VerificationList.WriteDocuments(verificationStock, DocumentFields.Select(f => f.Value).ToList());
            }

            if (error.Length > 0)
            {
                Status = string.Format(CultureInfo.CurrentCulture, Resources.Verify_WriteFailed, error);
                return;
            }

            LinkVerificationFromUnitNote();
            LoadVerifications();
            Done(Resources.Verify_DocsSaved);
        }

        /// <summary>Pencerede: kaynak doküman (TG no yoksa; örn. tasarımcı dokümanı).</summary>
        public string NewVerSource
        {
            get { return newVerSource; }
            set { SetProperty(ref newVerSource, value ?? string.Empty); }
        }

        /// <summary>Pencerede: kısa not (ölçüt / sınır).</summary>
        public string NewVerDescription
        {
            get { return newVerDescription; }
            set { SetProperty(ref newVerDescription, value ?? string.Empty); }
        }

        /// <summary>Pencerede: tam gereksinim metni.</summary>
        public string NewVerRequirement
        {
            get { return newVerRequirement; }
            set { SetProperty(ref newVerRequirement, value ?? string.Empty); }
        }

        /// <summary>Tür seçenekleri: —, Temel, Türetilmiş.</summary>
        public IList<string> VerificationKinds
        {
            get { return new[] { Resources.Verify_None, Resources.Verify_KindBasic, Resources.Verify_KindDerived }; }
        }

        /// <summary>Pencerede: tür ("—" boş).</summary>
        public string NewVerKind
        {
            get
            {
                return newVerKind.Length == 0 ? Resources.Verify_None : newVerKind;
            }

            set
            {
                var v = value ?? string.Empty;
                if (SetProperty(ref newVerKind, v == Resources.Verify_None ? string.Empty : v))
                {
                    OnPropertyChanged(nameof(IsNewVerDerived));
                }
            }
        }

        /// <summary>Türetilmiş seçili mi (bağlı temel madde seçilir).</summary>
        public bool IsNewVerDerived
        {
            get { return string.Equals(newVerKind, Resources.Verify_KindDerived, StringComparison.CurrentCultureIgnoreCase); }
        }

        /// <summary>Bağlı temel madde seçenekleri: "—" ve bu stoktaki TG no'lar.</summary>
        public ObservableCollection<string> VerificationParents { get; } = new ObservableCollection<string>();

        /// <summary>Pencerede: bağlı temel madde ("—" boş).</summary>
        public string NewVerParent
        {
            get { return newVerParent.Length == 0 ? Resources.Verify_None : newVerParent; }
            set { var v = value ?? string.Empty; SetProperty(ref newVerParent, v == Resources.Verify_None ? string.Empty : v); }
        }

        /// <summary>Yöntem seçenekleri: —, Test, Muayene, Belge, Gösterim, Diğer.</summary>
        public IList<string> VerificationMethods
        {
            get { return new[] { Resources.Verify_None }.Concat(VerificationRow.Methods).ToList(); }
        }

        /// <summary>Pencerede: yöntem ("—" boş).</summary>
        public string NewVerMethod
        {
            get { return newVerMethod.Length == 0 ? Resources.Verify_None : newVerMethod; }
            set { var v = value ?? string.Empty; SetProperty(ref newVerMethod, v == Resources.Verify_None ? string.Empty : v); }
        }

        /// <summary>Satırı açar / kapar (tam gereksinim ve plan / rapor özeti).</summary>
        public void ToggleVerification(VerificationRow row)
        {
            if (row != null)
            {
                row.IsExpanded = !row.IsExpanded;
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
            NewVerSource = item.Source;
            NewVerDescription = item.Description;
            NewVerRequirement = item.Requirement;
            NewVerKind = item.Kind;
            NewVerParent = item.Parent;
            NewVerMethod = item.Method;
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
                Description = item.Description,
                Requirement = item.Requirement,
                Source = item.Source,
                Kind = item.Kind,
                Parent = item.Parent,
                Method = item.Method,
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
            VerificationFilters.Add(new ChoiceOption(string.Empty, Resources.Home_RangeAll, verificationFilter.Length == 0, OnVerificationFilter)
            {
                Badge = verificationItems.Count.ToString(CultureInfo.CurrentCulture),
            });
            for (var g = 0; g < groups.Count; g++)
            {
                var group = groups[g];
                var count = verificationItems.Count(i => SameGroup(i, group));
                VerificationFilters.Add(new ChoiceOption(group, group, string.Equals(group, verificationFilter, StringComparison.CurrentCultureIgnoreCase), OnVerificationFilter)
                {
                    Badge = count.ToString(CultureInfo.CurrentCulture),
                    Tone = Math.Min(g, 3),
                });
            }

            // Dokümanlar (YAML): TG, tasarımcı, TP, TR.
            var docs = stock.Length == 0 ? new string[4] { string.Empty, string.Empty, string.Empty, string.Empty } : VerificationList.ReadDocuments(stock);
            VerificationDocuments.Clear();
            VerificationDocuments.Add(new VerificationDocument("TG", Resources.Verify_DocRequirement, docs[0], 0));
            VerificationDocuments.Add(new VerificationDocument("W", Resources.Verify_DocDesigner, docs[1], 1));
            VerificationDocuments.Add(new VerificationDocument("TP", Resources.Verify_DocPlan, docs[2], 2));
            VerificationDocuments.Add(new VerificationDocument("TR", Resources.Verify_DocReport, docs[3], 3));
            verificationDocs = docs;
            OnPropertyChanged(nameof(HasVerificationDocuments));

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

                var rows = verificationItems.Where(i => SameGroup(i, option.Key)).OrderBy(i => i.Number).Select(i => new VerificationRow(i) { SourceLine = SourceLineOf(i) }).ToList();
                if (rows.Count > 0)
                {
                    VerificationGroups.Add(new VerificationGroup(option.Key, Math.Max(0, option.Tone), rows));
                }
            }
        }

        // Açılan satırın kaynak satırı: doküman · TG no · alt grup · bağlı türetilmiş maddeler.
        private string SourceLineOf(VerificationItem item)
        {
            var parts = new List<string>();
            var doc = item.TgNo.Length > 0 ? verificationDocs[0] : item.Source.Length > 0 ? item.Source : verificationDocs[1];
            if (doc.Length > 0)
            {
                parts.Add(doc);
            }

            if (item.TgNo.Length > 0)
            {
                parts.Add(item.TgNo);
            }

            if (item.SubGroup.Length > 0)
            {
                parts.Add(item.SubGroup);
            }

            var children = item.TgNo.Length == 0 ? new List<string>() : verificationItems
                .Where(i => string.Equals(i.Parent, item.TgNo, StringComparison.OrdinalIgnoreCase))
                .Select(i => i.TgNo.Length > 0 ? i.TgNo : i.Text).ToList();
            if (children.Count > 0)
            {
                parts.Add(string.Format(CultureInfo.CurrentCulture, Resources.Verify_Children, string.Join(", ", children)));
            }

            return string.Join(" · ", parts);
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
            NewVerSource = string.Empty;
            NewVerDescription = string.Empty;
            NewVerRequirement = string.Empty;
            NewVerKind = string.Empty;
            NewVerParent = string.Empty;
            NewVerMethod = string.Empty;
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

            // Bağlı temel madde: bu stoktaki (temel ya da türü yazılmamış) TG no'lar.
            VerificationParents.Clear();
            VerificationParents.Add(Resources.Verify_None);
            foreach (var tg in verificationItems.Where(i => i.TgNo.Length > 0 && !string.Equals(i.Kind, Resources.Verify_KindDerived, StringComparison.CurrentCultureIgnoreCase)).Select(i => i.TgNo).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                VerificationParents.Add(tg);
            }
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
            if (text.Length == 0 && NewVerSubGroup.Trim().Length == 0 && NewVerTgNo.Trim().Length == 0)
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
                Description = newVerDescription.Trim(),
                Requirement = newVerRequirement.Trim(),
                Source = NewVerTgNo.Trim().Length > 0 ? string.Empty : newVerSource.Trim(),
                Kind = newVerKind.Trim(),
                Parent = IsNewVerDerived ? newVerParent.Trim() : string.Empty,
                Method = newVerMethod.Trim(),
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
