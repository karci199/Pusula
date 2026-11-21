using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Pusula.Core.Notes;
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

        /// <summary>Maddeye bağlı ölçüm kayıtları (Günlük'te <c>madde:</c> bu madde), yeniden eskiye.</summary>
        public IList<VerificationAttempt> Attempts { get; set; } = new List<VerificationAttempt>();

        /// <summary>Deneme var mı.</summary>
        public bool HasAttempts
        {
            get { return Attempts.Count > 0; }
        }

        /// <summary>Deneme sayısı metni ("3 deneme"); yoksa boş.</summary>
        public string AttemptCountText
        {
            get { return Attempts.Count > 0 ? string.Format(CultureInfo.CurrentCulture, Resources.Verify_AttemptCount, Attempts.Count) : string.Empty; }
        }

        /// <summary>Açık mı (tam gereksinim ve plan / rapor özeti görünür).</summary>
        public bool IsExpanded
        {
            get { return isExpanded; }
            set
            {
                if (SetProperty(ref isExpanded, value))
                {
                    OnPropertyChanged(nameof(ShowsDescriptionLine));
                }
            }
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
            get { return Item.TgNo.Length > 0 ? Item.TgNo : Item.Source; }
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

        /// <summary>Başlığın altındaki tek satır: kısa not, yoksa gereksinim metni (tabloda kırpılır).</summary>
        public string Description
        {
            get { return Item.Description.Length > 0 ? Item.Description : Item.Requirement; }
        }

        /// <summary>Başlığın altındaki tek satır görünür mü (satır açıkken gizli: tam metin açılan kısımda).</summary>
        public bool ShowsDescriptionLine
        {
            get { return HasDescription && !isExpanded; }
        }

        /// <summary>Başlığın altında satır var mı.</summary>
        public bool HasDescription
        {
            get { return Description.Length > 0; }
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

        /// <summary>Kaynak satırı var mı.</summary>
        public bool HasSourceLine
        {
            get { return SourceLine.Length > 0; }
        }

        /// <summary>Tür: Temel, Türetilmiş ya da "—".</summary>
        public string KindText
        {
            get { return Item.Kind; }
        }

        /// <summary>Türün rengi: 1 temel, 2 türetilmiş, 0 boş.</summary>
        public int KindTone
        {
            get
            {
                return string.Equals(Item.Kind, Resources.Verify_KindBasic, StringComparison.CurrentCultureIgnoreCase) ? 1
                    : string.Equals(Item.Kind, Resources.Verify_KindDerived, StringComparison.CurrentCultureIgnoreCase) ? 2 : 0;
            }
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
            get { return Item.TpNo; }
        }

        /// <summary>Test raporu madde no; yoksa "—".</summary>
        public string TrText
        {
            get { return Item.TrNo; }
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

        /// <summary>Test sonucu seçenekleri (rapor): Geçti · Kaldı · Hazırlanmadı.</summary>
        public static string[] Results
        {
            get { return new[] { Resources.Verify_StatusPassed, Resources.Verify_StatusFailed, Resources.Verify_ResultNotReady }; }
        }

        /// <summary>Raporu var mı (TR no, sonuç, açıklama, yer ya da not yazılı); yoksa kartta "Rapor ekle".</summary>
        public bool HasReport
        {
            get { return Item.TrNo.Length > 0 || Item.TrResult.Length > 0 || Item.TrDescription.Length > 0 || Item.ResultPath.Length > 0 || Item.TrNote.Length > 0; }
        }

        /// <summary>Rapordaki sonuç.</summary>
        public string ReportResult
        {
            get { return Item.TrResult; }
        }

        /// <summary>Sonucun rengi: 1 geçti, 2 kaldı, 3 hazırlanmadı, 0 boş.</summary>
        public int ReportResultKind
        {
            get
            {
                if (string.Equals(Item.TrResult, Resources.Verify_StatusPassed, StringComparison.CurrentCultureIgnoreCase))
                {
                    return 1;
                }

                if (string.Equals(Item.TrResult, Resources.Verify_StatusFailed, StringComparison.CurrentCultureIgnoreCase))
                {
                    return 2;
                }

                return Item.TrResult.Length > 0 ? 3 : 0;
            }
        }

        /// <summary>Sonuç yazılı mı.</summary>
        public bool HasReportResult
        {
            get { return Item.TrResult.Length > 0; }
        }

        /// <summary>Rapordaki açıklama.</summary>
        public string ReportDescription
        {
            get { return Item.TrDescription; }
        }

        /// <summary>Rapor açıklaması yazılı mı.</summary>
        public bool HasReportDescription
        {
            get { return Item.TrDescription.Length > 0; }
        }

        /// <summary>Rapor notu.</summary>
        public string ReportNote
        {
            get { return Item.TrNote; }
        }

        /// <summary>Rapor notu yazılı mı.</summary>
        public bool HasReportNote
        {
            get { return Item.TrNote.Length > 0; }
        }

        /// <summary>Sonucun yeri (md'de yazıldığı gibi).</summary>
        public string ResultPath
        {
            get { return Item.ResultPath; }
        }

        /// <summary>Sonucun yeri yazılı mı.</summary>
        public bool HasResultPath
        {
            get { return Item.ResultPath.Length > 0; }
        }

        /// <summary>Planı var mı (TP no, plan açıklaması, yöntem ya da plan notu yazılı); yoksa kartta "Test planı ekle".</summary>
        public bool HasPlan
        {
            get { return Item.TpNo.Length > 0 || Item.TpDescription.Length > 0 || Item.Method.Length > 0 || Item.TpNote.Length > 0; }
        }

        /// <summary>Plandaki açıklama.</summary>
        public string PlanDescription
        {
            get { return Item.TpDescription; }
        }

        /// <summary>Plan notu.</summary>
        public string PlanNote
        {
            get { return Item.TpNote; }
        }

        /// <summary>Plan açıklaması yazılı mı.</summary>
        public bool HasPlanDescription
        {
            get { return Item.TpDescription.Length > 0; }
        }

        /// <summary>Plan notu yazılı mı.</summary>
        public bool HasPlanNote
        {
            get { return Item.TpNote.Length > 0; }
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

                return parts.Count == 0 ? (HasPlan ? string.Empty : Resources.Verify_PlanEmpty) : string.Join(" · ", parts);
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

    /// <summary>Doğrulamalar sekmesinde durum süzgecinin bir seçeneği: bir durum, o anki gruptaki sayısıyla; işaretlenebilir (çoklu seçim).</summary>
    public sealed class VerificationStatusFilterOption : Pusula.Wpf.Mvvm.ViewModelBase
    {
        private readonly Action<VerificationStatusFilterOption> changed;
        private bool isChecked;

        /// <summary>Seçeneği kurar; işaret değişince <paramref name="changed"/> çağrılır.</summary>
        public VerificationStatusFilterOption(string key, int count, bool isChecked, Action<VerificationStatusFilterOption> changed)
        {
            Key = key;
            Count = count;
            Kind = VerificationRow.KindOf(key);
            this.isChecked = isChecked;
            this.changed = changed;
        }

        /// <summary>Durum adı.</summary>
        public string Key { get; }

        /// <summary>Görünen ad (durum adı).</summary>
        public string Text
        {
            get { return Key; }
        }

        /// <summary>Bu durumdaki madde sayısı (o anki grupta).</summary>
        public int Count { get; }

        /// <summary>Hiç madde yok mu (listede soluk).</summary>
        public bool IsEmpty
        {
            get { return Count == 0; }
        }

        /// <summary>Durum türü (renk için, bkz. <see cref="VerificationRow.KindOf"/>).</summary>
        public int Kind { get; }

        /// <summary>Tümü seçeneği mi (artık yok; şablon uyumu için her zaman <c>false</c>).</summary>
        public bool IsAll
        {
            get { return false; }
        }

        /// <summary>İşaretli mi; değişince tablo yeniden süzülür.</summary>
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

        /// <inheritdoc />
        public override string ToString()
        {
            return Text;
        }
    }
    /// <summary>Doğrulama satırı açılınca görünen bir deneme: maddeye bağlı Günlük kaydı (tarih, sonuç, başlık, etiketler).</summary>
    public sealed class VerificationAttempt
    {
        /// <summary>Günlük kaydından denemeyi kurar.</summary>
        public VerificationAttempt(EventNote note)
        {
            Note = note;
            DateText = note.Date.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
            // Sonucu olmayan kayıt (karar gibi) işlem adıyla görünür.
            Result = note.Result.Trim().Length > 0 ? note.Result.Trim() : (note.KindLabel.Trim().Length > 0 ? note.KindLabel.Trim() : Resources.Journal_ResultInfo);
            ResultKind = string.Equals(Result, Resources.Verify_StatusPassed, StringComparison.CurrentCultureIgnoreCase) ? 1
                : string.Equals(Result, Resources.Verify_StatusFailed, StringComparison.CurrentCultureIgnoreCase) ? 2 : 3;
            Title = note.Title;
            TagText = string.Join(" ", note.Tags.Select(t => "#" + t));
            FolderPath = MeasurementFiles.Resolve(Path.GetDirectoryName(note.Path) ?? string.Empty, note.Folder);
        }

        /// <summary>Ölçüm dosyalarının klasörü; yoksa boş.</summary>
        public string FolderPath { get; }

        /// <summary>Klasör düğmesi görünür mü.</summary>
        public bool HasFolder
        {
            get { return FolderPath.Length > 0; }
        }

        /// <summary>Günlük kaydı.</summary>
        public EventNote Note { get; }

        /// <summary>Tarih (gg.aa.yyyy).</summary>
        public string DateText { get; }

        /// <summary>Sonuç metni (Geçti, Kaldı, Bilgi).</summary>
        public string Result { get; }

        /// <summary>Sonucun rengi: 1 geçti, 2 kaldı, 3 bilgi.</summary>
        public int ResultKind { get; }

        /// <summary>Kaydın başlığı.</summary>
        public string Title { get; }

        /// <summary>Etiketler ("#SN-003 #VNA").</summary>
        public string TagText { get; }
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

    /// <summary>
    /// Doğrulama tablosunun sütun genişlikleri (TG no, tür, yöntem, test planı, test raporu, durum); gereksinim sütunu kalanı
    /// doldurur. Başlıktaki ayırıcı sürüklenince değişir, ayarlara (<c>columns["verification"]</c>) kaydedilir.
    /// </summary>
    public sealed class VerificationColumns : ViewModelBase
    {
        /// <summary>Sütun anahtarları (sırasıyla).</summary>
        public static readonly string[] Keys = { "tg", "kind", "method", "plan", "report", "status" };

        private static readonly double[] Defaults = { 104, 150, 110, 104, 104, 170 };
        private const double MinWidth = 56;
        private const double MaxWidth = 600;
        private readonly double[] widths = (double[])Defaults.Clone();

        /// <summary>TG no sütunu.</summary>
        public GridLength Tg
        {
            get { return new GridLength(widths[0]); }
        }

        /// <summary>Tür sütunu.</summary>
        public GridLength Kind
        {
            get { return new GridLength(widths[1]); }
        }

        /// <summary>Yöntem sütunu.</summary>
        public GridLength Method
        {
            get { return new GridLength(widths[2]); }
        }

        /// <summary>Test planı sütunu.</summary>
        public GridLength Plan
        {
            get { return new GridLength(widths[3]); }
        }

        /// <summary>Test raporu sütunu.</summary>
        public GridLength Report
        {
            get { return new GridLength(widths[4]); }
        }

        /// <summary>Durum sütunu.</summary>
        public GridLength Status
        {
            get { return new GridLength(widths[5]); }
        }

        /// <summary>Sütunu <paramref name="delta"/> kadar genişletir (eksi daraltır); sınırlar içinde kalır.</summary>
        public void Resize(string key, double delta)
        {
            var i = Array.IndexOf(Keys, key);
            if (i < 0)
            {
                return;
            }

            widths[i] = Math.Max(MinWidth, Math.Min(MaxWidth, widths[i] + delta));
            OnPropertyChanged(string.Empty);
        }

        /// <summary>Bütün sütunları varsayılan genişliğe döndürür.</summary>
        public void Reset()
        {
            Array.Copy(Defaults, widths, widths.Length);
            OnPropertyChanged(string.Empty);
        }

        /// <summary>Kayıtlı genişlikleri uygular (anahtarı tanınmayanlar atlanır).</summary>
        public void Load(IEnumerable<ColumnLayout>? saved)
        {
            foreach (var column in saved ?? Enumerable.Empty<ColumnLayout>())
            {
                var i = Array.IndexOf(Keys, column.Key ?? string.Empty);
                if (i >= 0 && column.Width > 0)
                {
                    widths[i] = Math.Max(MinWidth, Math.Min(MaxWidth, column.Width));
                }
            }

            OnPropertyChanged(string.Empty);
        }

        /// <summary>Kaydedilecek genişlikler.</summary>
        public List<ColumnLayout> ToLayout()
        {
            return Keys.Select((k, i) => new ColumnLayout { Key = k, Index = i, Width = widths[i] }).ToList();
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

            // Liste temizlenince açılır kutu seçimi kaybeder; aynı değer olsa da yeniden bildirilir ("—" görünsün).
            selected = Resources.Verify_None;
            OnPropertyChanged(nameof(Selected));
            OnPropertyChanged(nameof(IsNew));
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
        private List<EventNote> verificationAttempts = new List<EventNote>();
        private string verificationFilter = string.Empty;
        private string verificationStock = string.Empty;
        private bool isVerificationDialogOpen;
        private string newVerText = string.Empty;
        private string newVerSource = string.Empty;
        private string newVerDescription = string.Empty;
        private string newVerRequirement = string.Empty;
        private string newVerKind = string.Empty;
        private string newVerParent = string.Empty;
        private string newVerMethod = string.Empty;
        private string newVerTgNo = string.Empty;
        private string newVerTpNo = string.Empty;
        private string newVerTrNo = string.Empty;
        private string newVerStatus = string.Empty;
        private string verificationError = string.Empty;
        private ICommand? addVerificationCommand;
        private VerificationColumns? verificationColumns;
        private ICommand? openVerificationFileCommand;
        private VerificationItem? editingVerification;
        private VerificationItem? pendingVerificationDelete;
        private bool isVerificationDeleteOpen;

        /// <summary>Tablonun sütun genişlikleri (ilk kullanımda ayarlardan okunur).</summary>
        public VerificationColumns VerificationColumns
        {
            get
            {
                if (verificationColumns == null)
                {
                    verificationColumns = new VerificationColumns();
                    List<ColumnLayout>? saved;
                    if (Settings.Columns.TryGetValue(VerificationColumnsKey, out saved))
                    {
                        verificationColumns.Load(saved);
                    }
                }

                return verificationColumns;
            }
        }

        /// <summary>Sütun genişliklerini ayarlara yazar (sürükleme bitince).</summary>
        public void SaveVerificationColumns()
        {
            Settings.Columns[VerificationColumnsKey] = VerificationColumns.ToLayout();
            Settings.Save();
        }

        private const string VerificationColumnsKey = "verification";

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
                        OpenInPusula(VerificationList.PathFor(verificationStock));
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
            // TG no ve kaynak doküman tek kutuda: TG no yoksa doküman no gösterilir (kaydedince TG no olur).
            NewVerTgNo = item.TgNo.Length > 0 ? item.TgNo : item.Source;
            NewVerSource = string.Empty;
            NewVerRequirement = item.Requirement.Length > 0 ? item.Requirement : item.Description;
            NewVerDescription = item.Requirement.Length > 0 ? item.Description : string.Empty;
            NewVerKind = item.Kind;
            NewVerParent = item.Parent;
            NewVerMethod = item.Method;
            NewVerSubGroup = item.SubGroup;
            NewVerFacility = item.Facility;
            NewVerTpNo = item.TpNo;
            NewVerTrNo = item.TrNo;
            NewVerStatus = item.Status.Length > 0 ? item.Status : Resources.Verify_StatusPending;
            NotifyVerificationDialog();
            IsVerificationDialogOpen = true;
        }

        #region Test raporu penceresi (satırdaki Test raporu kartı)

        private VerificationItem? reportItem;
        private bool isVerReportOpen;
        private string reportTrNo = string.Empty;
        private string reportDescription = string.Empty;
        private string reportResult = string.Empty;
        private string reportPath = string.Empty;
        private string reportNote = string.Empty;
        private string reportError = string.Empty;

        /// <summary>Test raporu penceresi açık mı.</summary>
        public bool IsVerReportOpen
        {
            get { return isVerReportOpen; }
            set { SetDialogOpen(ref isVerReportOpen, value); }
        }

        /// <summary>Pencere başlığı: "Test raporu · DGÖ-1".</summary>
        public string VerReportTitle
        {
            get { return reportItem == null ? string.Empty : string.Format(CultureInfo.CurrentCulture, Resources.Verify_ReportTitle, VerificationList.KeyOf(reportItem)); }
        }

        /// <summary>Test sonucu seçenekleri: Geçti · Kaldı · Hazırlanmadı.</summary>
        public IList<string> VerificationResults
        {
            get { return VerificationRow.Results; }
        }

        /// <summary>Rapor madde no (TR).</summary>
        public string ReportTrNo
        {
            get { return reportTrNo; }
            set { SetProperty(ref reportTrNo, value ?? string.Empty); }
        }

        /// <summary>Rapordaki açıklama.</summary>
        public string ReportDescription
        {
            get { return reportDescription; }
            set { SetProperty(ref reportDescription, value ?? string.Empty); }
        }

        /// <summary>Test sonucu (Geçti · Kaldı · Hazırlanmadı); boş olabilir.</summary>
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

        /// <summary>Rapor notu (ölçülen değer, kaldıysa bildirim no).</summary>
        public string ReportNote
        {
            get { return reportNote; }
            set { SetProperty(ref reportNote, value ?? string.Empty); }
        }

        /// <summary>Penceredeki hata; boşsa gizli.</summary>
        public string ReportError
        {
            get { return reportError; }
            private set { SetProperty(ref reportError, value); }
        }

        /// <summary>Satırın test raporunu düzenleme penceresinde açar (yoksa boş: "Rapor ekle").</summary>
        public void OpenVerReport(VerificationRow row)
        {
            if (row == null || verificationStock.Length == 0)
            {
                return;
            }

            reportItem = row.Item;
            ReportTrNo = reportItem.TrNo;
            ReportDescription = reportItem.TrDescription;
            ReportResult = reportItem.TrResult;
            ReportPath = FullResultPath(verificationStock, reportItem.ResultPath);
            ReportNote = reportItem.TrNote;
            ReportError = string.Empty;
            OnPropertyChanged(nameof(VerReportTitle));
            IsVerReportOpen = true;
        }

        /// <summary>
        /// Raporu satıra yazar (TR no, açıklama, sonuç, sonucun yeri, not). Sonuç Geçti ya da Kaldı ise maddenin durumu da
        /// o olur; Hazırlanmadı ya da boşsa durum değişmez. Sonucun yeri stok klasörünün içindeyse göreli yazılır.
        /// </summary>
        public bool ConfirmVerReport()
        {
            if (reportItem == null || verificationStock.Length == 0)
            {
                return true;
            }

            var item = VerificationList.Read(verificationStock).FirstOrDefault(v => v.Number == reportItem.Number);
            if (item == null)
            {
                ReportError = Resources.Verify_Missing;
                return false;
            }

            item.TrNo = ReportTrNo.Trim();
            item.TrDescription = ReportDescription.Trim();
            item.TrResult = ReportResult.Trim();
            item.ResultPath = RelativeResultPath(verificationStock, ReportPath.Trim());
            item.TrNote = ReportNote.Trim();
            if (string.Equals(item.TrResult, Resources.Verify_StatusPassed, StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(item.TrResult, Resources.Verify_StatusFailed, StringComparison.CurrentCultureIgnoreCase))
            {
                item.Status = item.TrResult;
            }

            var error = VerificationList.Update(verificationStock, item);
            if (error.Length > 0 && error != "missing")
            {
                ReportError = string.Format(CultureInfo.CurrentCulture, Resources.Verify_WriteFailed, error);
                return false;
            }

            WriteVerificationSections(item);
            var number = item.Number;
            reportItem = null;
            if (WriteVerification(error))
            {
                Status = string.Format(CultureInfo.CurrentCulture, Resources.Verify_ReportSaved, VerificationList.KeyOf(item));
                var row = VerificationGroups.SelectMany(g => g.Rows).FirstOrDefault(r => r.Item.Number == number);
                if (row != null)
                {
                    row.IsExpanded = true;
                }
            }

            return true;
        }

        // Md'deki okunur bölümler (## Plan / ## Sonuç altında madde başlığı); yazılamazsa durum çubuğunda söylenir.
        private void WriteVerificationSections(VerificationItem item)
        {
            if (verificationStock.Length == 0 || item == null)
            {
                return;
            }

            var order = VerificationList.Read(verificationStock);
            var error = VerificationList.WriteSections(verificationStock, item, order);
            if (error.Length > 0 && error != "missing")
            {
                Status = string.Format(CultureInfo.CurrentCulture, Resources.Verify_WriteFailed, error);
            }
        }

        /// <summary>Satırdaki "Aç": sonucun yerini (klasör ya da dosya) açar.</summary>
        public void OpenResultPath(VerificationRow row)
        {
            if (row == null || verificationStock.Length == 0 || row.Item.ResultPath.Length == 0)
            {
                return;
            }

            var full = FullResultPath(verificationStock, row.Item.ResultPath);
            if (Directory.Exists(full) || File.Exists(full))
            {
                OpenPath(full);
            }
            else
            {
                Status = string.Format(CultureInfo.CurrentCulture, Resources.Verify_ResultPathMissing, row.Item.ResultPath);
            }
        }

        /// <summary>Md'deki yol (göreli ya da tam) → tam yol.</summary>
        public static string FullResultPath(string stockFolder, string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            return Path.IsPathRooted(path) ? path : Path.Combine(stockFolder, path);
        }

        /// <summary>Tam yol stok klasörünün içindeyse göreli (03_Testler\…), değilse olduğu gibi.</summary>
        public static string RelativeResultPath(string stockFolder, string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            var root = stockFolder.TrimEnd('\\') + "\\";
            return path.StartsWith(root, StringComparison.OrdinalIgnoreCase) ? path.Substring(root.Length) : path;
        }

        #endregion
        #region Test planı penceresi (satırdaki Test planı kartı)

        private VerificationItem? planItem;
        private bool isVerPlanOpen;
        private string planTpNo = string.Empty;
        private string planDescription = string.Empty;
        private string planMethod = string.Empty;
        private string planNote = string.Empty;
        private string planError = string.Empty;

        /// <summary>Test planı penceresi açık mı.</summary>
        public bool IsVerPlanOpen
        {
            get { return isVerPlanOpen; }
            set { SetDialogOpen(ref isVerPlanOpen, value); }
        }

        /// <summary>Pencere başlığı: "Test planı · DGÖ-1".</summary>
        public string VerPlanTitle
        {
            get { return planItem == null ? string.Empty : string.Format(CultureInfo.CurrentCulture, Resources.Verify_PlanTitle, VerificationList.KeyOf(planItem)); }
        }

        /// <summary>Plan madde no (TP).</summary>
        public string PlanTpNo
        {
            get { return planTpNo; }
            set { SetProperty(ref planTpNo, value ?? string.Empty); }
        }

        /// <summary>Plandaki açıklama.</summary>
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

        /// <summary>Plan notu (jig, SN, dikkat edilecekler).</summary>
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

        /// <summary>Satırın test planını düzenleme penceresinde açar (yoksa boş: "Test planı ekle").</summary>
        public void OpenVerPlan(VerificationRow row)
        {
            if (row == null || verificationStock.Length == 0)
            {
                return;
            }

            planItem = row.Item;
            PlanTpNo = planItem.TpNo;
            PlanDescription = planItem.TpDescription;
            PlanMethod = planItem.Method;
            var facilities = FacilityList.Read(rootPath).ToList();
            foreach (var used in verificationItems.Select(i => i.Facility).Where(f => f.Length > 0))
            {
                if (!facilities.Contains(used, StringComparer.CurrentCultureIgnoreCase))
                {
                    facilities.Add(used);
                }
            }

            PlanFacilityPick.SetNames(facilities);
            PlanFacilityPick.Value = planItem.Facility;
            PlanNote = planItem.TpNote;
            PlanError = string.Empty;
            OnPropertyChanged(nameof(VerPlanTitle));
            IsVerPlanOpen = true;
        }

        /// <summary>
        /// Planı satıra yazar (TP no, açıklama, yöntem, altyapı, not; diskteki güncel satırın üstüne). Durum Bekliyor ya da
        /// boşsa Planlandı olur; daha ilerideyse dokunulmaz. Başarısızsa <c>false</c> ve hata metni.
        /// </summary>
        public bool ConfirmVerPlan()
        {
            if (planItem == null || verificationStock.Length == 0)
            {
                return true;
            }

            var item = VerificationList.Read(verificationStock).FirstOrDefault(v => v.Number == planItem.Number);
            if (item == null)
            {
                PlanError = Resources.Verify_Missing;
                return false;
            }

            item.TpNo = PlanTpNo.Trim();
            item.TpDescription = PlanDescription.Trim();
            item.Method = planMethod.Trim();
            item.Facility = PlanFacilityPick.Value.Trim();
            item.TpNote = PlanNote.Trim();
            var planned = item.TpNo.Length > 0 || item.TpDescription.Length > 0 || item.Method.Length > 0;
            if (planned && VerificationRow.KindOf(item.Status) == 0)
            {
                item.Status = Resources.Verify_StatusPlanned;
            }

            var error = VerificationList.Update(verificationStock, item);
            if (error.Length > 0 && error != "missing")
            {
                PlanError = string.Format(CultureInfo.CurrentCulture, Resources.Verify_WriteFailed, error);
                return false;
            }

            if (error.Length == 0 && item.Facility.Length > 0 && rootPath.Length > 0)
            {
                FacilityList.Add(rootPath, item.Facility, Resources.Verify_FacilityTemplate);
            }

            WriteVerificationSections(item);
            var number = item.Number;
            planItem = null;
            if (WriteVerification(error))
            {
                Status = string.Format(CultureInfo.CurrentCulture, Resources.Verify_PlanSaved, VerificationList.KeyOf(item));
                // Satır açık kalsın: kart yeni planla görünür.
                var row = VerificationGroups.SelectMany(g => g.Rows).FirstOrDefault(r => r.Item.Number == number);
                if (row != null)
                {
                    row.IsExpanded = true;
                }
            }

            return true;
        }

        #endregion

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
                TpDescription = item.TpDescription,
                TpNote = item.TpNote,
                TrDescription = item.TrDescription,
                TrNote = item.TrNote,
                TrResult = item.TrResult,
                ResultPath = item.ResultPath,
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
            verificationAttempts = stock.Length == 0 ? new List<EventNote>() : Journal.Read(TaskNotePath(stock)).Where(e => e.Item.Trim().Length > 0).OrderByDescending(e => e.Date).ToList();

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

            BuildStatusFilters();
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

            BuildStatusFilters();
            FillVerificationGroups();
        }

        private readonly HashSet<string> verificationStatusFilter = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);
        private string verificationSort = string.Empty;

        /// <summary>Durum süzgeci (çoklu seçim): her durum, o anki grup seçimindeki sayısıyla. Hiçbiri işaretli değilse hepsi görünür.</summary>
        public ObservableCollection<VerificationStatusFilterOption> VerificationStatusFilters { get; } = new ObservableCollection<VerificationStatusFilterOption>();

        /// <summary>Durum kutusunun yazısı: "Hepsi", tek durumun adı ya da "2 durum" (stoklar arasında seçim hatırlanır).</summary>
        public string VerificationStatusFilterText
        {
            get
            {
                if (verificationStatusFilter.Count == 0)
                {
                    return Resources.Home_RangeAll;
                }

                return verificationStatusFilter.Count == 1
                    ? verificationStatusFilter.First()
                    : string.Format(CultureInfo.CurrentCulture, Resources.Verify_FilterStatusCount, verificationStatusFilter.Count);
            }
        }

        /// <summary>Durum süzgecinde seçim var mı (kutu vurgulu, Temizle görünür).</summary>
        public bool HasVerificationStatusFilter
        {
            get { return verificationStatusFilter.Count > 0; }
        }

        /// <summary>Görünen madde sayısı: "3 / 9 madde".</summary>
        public string VerificationShownText
        {
            get
            {
                var shown = VerificationGroups.Sum(g => g.Rows.Count);
                return string.Format(CultureInfo.CurrentCulture, Resources.Verify_Shown, shown, verificationItems.Count);
            }
        }

        /// <summary>Durum süzgecini temizler (hepsi görünür).</summary>
        public void ClearVerificationStatusFilter()
        {
            verificationStatusFilter.Clear();
            BuildStatusFilters();
            FillVerificationGroups();
        }

        private void OnStatusFilterChanged(VerificationStatusFilterOption option)
        {
            if (option.IsChecked)
            {
                verificationStatusFilter.Add(option.Key);
            }
            else
            {
                verificationStatusFilter.Remove(option.Key);
            }

            OnPropertyChanged(nameof(VerificationStatusFilterText));
            OnPropertyChanged(nameof(HasVerificationStatusFilter));
            FillVerificationGroups();
        }
        /// <summary>Sıralama seçenekleri: Sıra no · TG no · Durum · Başlık (grup içinde).</summary>
        public IList<string> VerificationSorts
        {
            get { return new[] { Resources.Verify_SortNumber, Resources.Verify_SortTg, Resources.Verify_SortStatus, Resources.Verify_SortTitle }; }
        }

        /// <summary>Seçili sıralama (stoklar arasında hatırlanır).</summary>
        public string VerificationSort
        {
            get { return verificationSort.Length == 0 ? Resources.Verify_SortNumber : verificationSort; }
            set
            {
                if (string.IsNullOrEmpty(value) || value == VerificationSort)
                {
                    return;
                }

                verificationSort = value;
                OnPropertyChanged();
                FillVerificationGroups();
            }
        }

        // Durum süzgeci seçenekleri: sayılar o anki grup seçimine göre; işaretler korunur.
        private void BuildStatusFilters()
        {
            var inGroup = verificationItems.Where(i => verificationFilter.Length == 0 || SameGroup(i, verificationFilter)).ToList();
            VerificationStatusFilters.Clear();
            foreach (var status in VerificationRow.Statuses)
            {
                var kind = VerificationRow.KindOf(status);
                VerificationStatusFilters.Add(new VerificationStatusFilterOption(status, inGroup.Count(i => VerificationRow.KindOf(i.Status) == kind), verificationStatusFilter.Contains(status), OnStatusFilterChanged));
            }

            OnPropertyChanged(nameof(VerificationStatusFilterText));
            OnPropertyChanged(nameof(HasVerificationStatusFilter));
        }
        // "DGÖ-2" < "DGÖ-10": rakam dizileri sayı olarak, geri kalanı büyük/küçük harf duyarsız karşılaştırılır.
        private static int CompareNatural(string a, string b)
        {
            int i = 0, j = 0;
            while (i < a.Length && j < b.Length)
            {
                if (char.IsDigit(a[i]) && char.IsDigit(b[j]))
                {
                    var si = i;
                    var sj = j;
                    while (i < a.Length && char.IsDigit(a[i]))
                    {
                        i++;
                    }

                    while (j < b.Length && char.IsDigit(b[j]))
                    {
                        j++;
                    }

                    var na = a.Substring(si, i - si).TrimStart('0');
                    var nb = b.Substring(sj, j - sj).TrimStart('0');
                    var c = na.Length != nb.Length ? na.Length.CompareTo(nb.Length) : string.CompareOrdinal(na, nb);
                    if (c != 0)
                    {
                        return c;
                    }
                }
                else
                {
                    var c = string.Compare(a[i].ToString(), b[j].ToString(), StringComparison.CurrentCultureIgnoreCase);
                    if (c != 0)
                    {
                        return c;
                    }

                    i++;
                    j++;
                }
            }

            return (a.Length - i).CompareTo(b.Length - j);
        }
        // Durum sırası (Durum'a göre sıralamada): önce dikkat isteyenler, sonda bitenler.
        private static int StatusOrder(string status)
        {
            var order = new[] { 2, 4, 0, 6, 7, 3, 8, 9, 1, 5 };
            var index = Array.IndexOf(order, VerificationRow.KindOf(status));
            return index < 0 ? order.Length : index;
        }

        private IEnumerable<VerificationItem> SortVerification(IEnumerable<VerificationItem> items)
        {
            var sort = VerificationSort;
            if (sort == Resources.Verify_SortTg)
            {
                return items.OrderBy(i => i.TgNo.Length == 0 ? 1 : 0).ThenBy(i => i.TgNo, Comparer<string>.Create(CompareNatural)).ThenBy(i => i.Number);
            }

            if (sort == Resources.Verify_SortStatus)
            {
                return items.OrderBy(i => StatusOrder(i.Status)).ThenBy(i => i.Number);
            }

            if (sort == Resources.Verify_SortTitle)
            {
                return items.OrderBy(i => i.Text.Length > 0 ? i.Text : i.SubGroup, StringComparer.CurrentCultureIgnoreCase).ThenBy(i => i.Number);
            }

            return items.OrderBy(i => i.Number);
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

                var kinds = new HashSet<int>(verificationStatusFilter.Select(VerificationRow.KindOf));
                var rows = SortVerification(verificationItems.Where(i => SameGroup(i, option.Key) && (kinds.Count == 0 || kinds.Contains(VerificationRow.KindOf(i.Status))))).Select(i => new VerificationRow(i) { SourceLine = SourceLineOf(i), Attempts = AttemptsOf(i) }).ToList();
                if (rows.Count > 0)
                {
                    VerificationGroups.Add(new VerificationGroup(option.Key, Math.Max(0, option.Tone), rows));
                }
            }

            OnPropertyChanged(nameof(VerificationShownText));
        }

        // Maddeye bağlı ölçüm kayıtları (Günlük'te madde: anahtarı), yeniden eskiye.
        private IList<VerificationAttempt> AttemptsOf(VerificationItem item)
        {
            var key = VerificationList.KeyOf(item);
            return verificationAttempts.Where(e => string.Equals(e.Item.Trim(), key, StringComparison.OrdinalIgnoreCase)).Select(e => new VerificationAttempt(e)).ToList();
        }

        // Açılan satırın kaynak satırı: doküman · TG no · alt grup · bağlı türetilmiş maddeler.
        private string SourceLineOf(VerificationItem item)
        {
            var parts = new List<string>();
            if (item.TgNo.Length > 0)
            {
                parts.Add(item.TgNo);
            }
            else if (item.Source.Length > 0)
            {
                parts.Add(item.Source);
            }

            if (item.SubGroup.Length > 0)
            {
                parts.Add(item.SubGroup);
            }

            var children = item.TgNo.Length == 0 ? new List<string>() : verificationItems
                .Where(i => string.Equals(i.Parent, item.TgNo, StringComparison.OrdinalIgnoreCase))
                .Select(i => i.Text.Length > 0 ? i.Text : i.TgNo).ToList();
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
                VerificationGroupChoices.Add(new ChoiceOption(name, name, string.Equals(name, group, StringComparison.CurrentCultureIgnoreCase), OnVerificationGroupChoice)
                {
                    Tone = Math.Min(VerificationGroupChoices.Count, 3),
                });
            }

            if (!VerificationGroupChoices.Any(c => c.IsChecked))
            {
                VerificationGroupChoices[0].IsChecked = true;
            }

            OnPropertyChanged(nameof(NewVerGroupChoice));

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

        /// <summary>Seçili grup (açılır liste; renkli noktayla).</summary>
        public ChoiceOption? NewVerGroupChoice
        {
            get
            {
                return VerificationGroupChoices.FirstOrDefault(c => c.IsChecked);
            }

            set
            {
                if (value != null)
                {
                    value.IsChecked = true;
                }
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
            // Plan / rapor açıklama ve notu pencerede yok: düzenlemede diskteki güncel satırdan korunur (Obsidian'da değişmiş olabilir).
            var onDisk = editingVerification == null ? null
                : VerificationList.Read(verificationStock).FirstOrDefault(v => v.Number == editingVerification.Number) ?? editingVerification;
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
                TpDescription = onDisk?.TpDescription ?? string.Empty,
                TpNote = onDisk?.TpNote ?? string.Empty,
                TrDescription = onDisk?.TrDescription ?? string.Empty,
                TrNote = onDisk?.TrNote ?? string.Empty,
                TrResult = onDisk?.TrResult ?? string.Empty,
                ResultPath = onDisk?.ResultPath ?? string.Empty,
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
                if (updateError.Length == 0)
                {
                    WriteVerificationSections(item);
                }

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
            Done(string.Format(CultureInfo.CurrentCulture, Resources.Verify_Added, item.Number, text.Length > 0 ? text : item.SubGroup), VerificationList.PathFor(verificationStock));
            return true;
        }
    }
}
