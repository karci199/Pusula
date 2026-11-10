using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using Pusula.Core.Markdown;
using Pusula.Wpf.Mvvm;
using Pusula.Wpf.Properties;

namespace Pusula.Wpf.ViewModels
{
    /// <summary>@ / [[ listesinde bir öneri: jig, kablo, stok, proje, kişi ya da not.</summary>
    public sealed class NoteSuggestion
    {
        /// <summary>Öneriyi kurar.</summary>
        public NoteSuggestion(string key, string title, string kind, string glyph, string insert)
        {
            Key = key;
            Title = title;
            Kind = kind;
            Glyph = glyph;
            Insert = insert;
            Search = Fold(key + " " + title);
        }

        /// <summary>Numara ya da ad.</summary>
        public string Key { get; }

        /// <summary>Tanım.</summary>
        public string Title { get; }

        /// <summary>Tür adı (Jig, Kablo, Stok, Kişi …).</summary>
        public string Kind { get; }

        /// <summary>İkon.</summary>
        public string Glyph { get; }

        /// <summary>Metne yazılacak (<c>[[JIG-1000-0009]]</c>, <c>@Ali Veli</c>).</summary>
        public string Insert { get; }

        /// <summary>Aramada kullanılan sadeleşmiş metin.</summary>
        public string Search { get; }

        /// <summary>Küçük harf, Türkçe harfler sadeleşmiş.</summary>
        public static string Fold(string text)
        {
            var lower = (text ?? string.Empty).ToLower(CultureInfo.GetCultureInfo("tr-TR"));
            return lower.Replace('ç', 'c').Replace('ğ', 'g').Replace('ı', 'i').Replace('ö', 'o').Replace('ş', 's').Replace('ü', 'u');
        }
    }

    /// <summary>
    /// md not penceresi: metin, kaydedilme durumu, görünüm (düz / yan yana / görünüm), ayarlar, öneriler, görev kutusu,
    /// dış değişiklik. Kaydetme atomiktir ve notun kodlamasını korur; Pusula nota kendinden bir şey eklemez.
    /// </summary>
    public sealed class NoteEditorViewModel : ViewModelBase
    {
        /// <summary>Düz görünüm.</summary>
        public const string ViewEdit = "edit";

        /// <summary>Yan yana.</summary>
        public const string ViewSplit = "split";

        /// <summary>Yalnız görünüm.</summary>
        public const string ViewPreview = "preview";

        private readonly NoteEditorSettings settings;
        private readonly Action saveSettings;
        private readonly Func<IEnumerable<NoteSuggestion>> suggestionSource;
        private List<NoteSuggestion>? suggestionCache;
        private string text = string.Empty;
        private string savedText = string.Empty;
        private string status = string.Empty;
        private string caretText = string.Empty;
        private string lastSaved = string.Empty;
        private bool isExternallyChanged;
        private DateTime fileTime;

        /// <summary>Notu okur.</summary>
        public NoteEditorViewModel(string path, string root, NoteEditorSettings settings, Action saveSettings, Func<IEnumerable<NoteSuggestion>> suggestionSource)
        {
            Path = path;
            Root = root ?? string.Empty;
            this.settings = settings;
            this.saveSettings = saveSettings;
            this.suggestionSource = suggestionSource;
            Load();
        }

        /// <summary>Notun yolu.</summary>
        public string Path { get; }

        /// <summary>Kök (resim ve bağlantı araması için).</summary>
        public string Root { get; }

        /// <summary>Notun klasörü.</summary>
        public string Folder
        {
            get { return System.IO.Path.GetDirectoryName(Path) ?? string.Empty; }
        }

        /// <summary>Dosya adı (uzantısız).</summary>
        public string FileName
        {
            get { return System.IO.Path.GetFileNameWithoutExtension(Path); }
        }

        /// <summary>Klasörün köke göre yolu.</summary>
        public string RelativeFolder
        {
            get
            {
                var folder = Folder;
                return Root.Length > 0 && folder.StartsWith(Root, StringComparison.OrdinalIgnoreCase) ? folder.Substring(Root.Length).TrimStart('\\') : folder;
            }
        }

        /// <summary>Pencere başlığı ("JIG-1000-0009 •" kaydedilmemişse).</summary>
        public string WindowTitle
        {
            get { return string.Format(Resources.Note_WindowTitle, FileName, IsDirty ? " •" : string.Empty); }
        }

        /// <summary>Metin.</summary>
        public string Text
        {
            get { return text; }
            set
            {
                if (SetProperty(ref text, value ?? string.Empty))
                {
                    NotifyDirty();
                    OnPropertyChanged(nameof(WordsText));
                }
            }
        }

        /// <summary>Bu pencerede kaç kez kaydedildi (kapanınca ana pencere yenilensin mi diye).</summary>
        public int SaveCount { get; private set; }

        /// <summary>Kaydedilmemiş değişiklik var mı.</summary>
        public bool IsDirty
        {
            get { return !string.Equals(text, savedText, StringComparison.Ordinal); }
        }

        /// <summary>"Kaydedildi 23:14" ya da "Kaydedilmemiş değişiklik".</summary>
        public string SaveText
        {
            get { return IsDirty ? Resources.Note_Unsaved : lastSaved; }
        }

        /// <summary>"123 kelime".</summary>
        public string WordsText
        {
            get { return string.Format(Resources.Note_Words, MarkdownNotes.WordCount(text)); }
        }

        /// <summary>"Satır 4, Sütun 10".</summary>
        public string CaretText
        {
            get { return caretText; }
            private set { SetProperty(ref caretText, value); }
        }

        /// <summary>Son ileti (hata, bulunamayan bağlantı …).</summary>
        public string Status
        {
            get { return status; }
            set { SetProperty(ref status, value ?? string.Empty); }
        }

        /// <summary>Dosya dışarıda değişti (uyarı şeridi).</summary>
        public bool IsExternallyChanged
        {
            get { return isExternallyChanged; }
            private set { SetProperty(ref isExternallyChanged, value); }
        }

        /// <summary>Görünüm (<see cref="ViewEdit"/>, <see cref="ViewSplit"/>, <see cref="ViewPreview"/>).</summary>
        public string ViewMode
        {
            get { return settings.View == ViewEdit || settings.View == ViewPreview ? settings.View : ViewSplit; }
            set
            {
                if (settings.View != value)
                {
                    settings.View = value;
                    SaveSettings();
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsEditMode));
                    OnPropertyChanged(nameof(IsSplitMode));
                    OnPropertyChanged(nameof(IsPreviewMode));
                    OnPropertyChanged(nameof(ShowsEditor));
                    OnPropertyChanged(nameof(ShowsPreview));
                }
            }
        }

        /// <summary>Düz görünüm seçili.</summary>
        public bool IsEditMode
        {
            get { return ViewMode == ViewEdit; }
        }

        /// <summary>Yan yana seçili.</summary>
        public bool IsSplitMode
        {
            get { return ViewMode == ViewSplit; }
        }

        /// <summary>Görünüm seçili.</summary>
        public bool IsPreviewMode
        {
            get { return ViewMode == ViewPreview; }
        }

        /// <summary>Metin kutusu görünür mü.</summary>
        public bool ShowsEditor
        {
            get { return ViewMode != ViewPreview; }
        }

        /// <summary>Görünüm görünür mü.</summary>
        public bool ShowsPreview
        {
            get { return ViewMode != ViewEdit; }
        }

        /// <summary>Düz görünüm yazı tipleri.</summary>
        public string[] EditorFonts { get; } = { "Cascadia Mono", "Consolas", "Segoe UI", "Calibri", "Georgia" };

        /// <summary>Okuma genişlikleri (0 tam).</summary>
        public double[] ReadingWidths { get; } = { 680, 860, 1040, 1280, 0 };

        /// <summary>Düz görünüm yazı tipi.</summary>
        public string EditorFont
        {
            get { return settings.EditorFont; }
            set { Set(() => settings.EditorFont = value ?? "Consolas"); }
        }

        /// <summary>Düz görünüm yazı boyutu (10–28).</summary>
        public double EditorSize
        {
            get { return settings.EditorSize; }
            set { Set(() => settings.EditorSize = Math.Max(10, Math.Min(28, Math.Round(value)))); }
        }

        /// <summary>Görünüm yazı boyutu (11–28).</summary>
        public double PreviewSize
        {
            get { return settings.PreviewSize; }
            set { Set(() => settings.PreviewSize = Math.Max(11, Math.Min(28, Math.Round(value)))); }
        }

        /// <summary>Okuma genişliği (0 tam).</summary>
        public double ReadingWidth
        {
            get { return settings.ReadingWidth; }
            set { Set(() => settings.ReadingWidth = Math.Max(0, value)); }
        }

        /// <summary>Uzun satırları kaydır.</summary>
        public bool Wrap
        {
            get { return settings.Wrap; }
            set { Set(() => settings.Wrap = value); }
        }

        /// <summary>Tek satır sonu yeni satır.</summary>
        public bool LineBreaks
        {
            get { return settings.LineBreaks; }
            set { Set(() => settings.LineBreaks = value); }
        }

        /// <summary>YAML özelliklerini göster.</summary>
        public bool FrontMatter
        {
            get { return settings.FrontMatter; }
            set { Set(() => settings.FrontMatter = value); }
        }

        /// <summary>Otomatik kaydet.</summary>
        public bool AutoSave
        {
            get { return settings.AutoSave; }
            set { Set(() => settings.AutoSave = value); }
        }

        /// <summary>Ekranı kaplayarak aç.</summary>
        public bool StartMaximized
        {
            get { return settings.Maximized; }
            set { Set(() => settings.Maximized = value); }
        }

        /// <summary>Yan yana oranı.</summary>
        public double SplitRatio
        {
            get { return settings.SplitRatio; }
            set { Set(() => settings.SplitRatio = Math.Max(0.2, Math.Min(0.8, value))); }
        }

        /// <summary>Aramada büyük / küçük harf.</summary>
        public bool FindMatchCase
        {
            get { return settings.FindMatchCase; }
            set { SetQuiet(() => settings.FindMatchCase = value); }
        }

        /// <summary>Aramada tam kelime.</summary>
        public bool FindWholeWord
        {
            get { return settings.FindWholeWord; }
            set { SetQuiet(() => settings.FindWholeWord = value); }
        }

        /// <summary>Aramada düzenli ifade.</summary>
        public bool FindRegex
        {
            get { return settings.FindRegex; }
            set { SetQuiet(() => settings.FindRegex = value); }
        }

        /// <summary>Arama seçenekleri (şu anki ayarlarla).</summary>
        public SearchOptions FindOptions
        {
            get { return new SearchOptions { MatchCase = FindMatchCase, WholeWord = FindWholeWord, UseRegex = FindRegex }; }
        }

        /// <summary>Yan yana görünümde birlikte kaydır.</summary>
        public bool SyncScroll
        {
            get { return settings.SyncScroll; }
            set { SetQuiet(() => settings.SyncScroll = value); }
        }

        /// <summary>Başlık paneli açık mı.</summary>
        public bool ShowOutline
        {
            get { return settings.ShowOutline; }
            set { SetQuiet(() => settings.ShowOutline = value); }
        }

        /// <summary>Parantez ve işaret çiftlerini kendiliğinden kapat.</summary>
        public bool AutoPair
        {
            get { return settings.AutoPair; }
            set { SetQuiet(() => settings.AutoPair = value); }
        }

        /// <summary>Ayar değişince görünüm yeniden çizilsin (pencere dinler).</summary>
        public event EventHandler? SettingsChanged;

        /// <summary>İmleç değişti.</summary>
        public void SetCaret(int caret)
        {
            var position = MarkdownEditing.LineColumn(text, caret);
            CaretText = string.Format(Resources.Note_Caret, position.Item1, position.Item2);
        }

        /// <summary>Kaydeder. Başarılıysa <c>true</c>.</summary>
        public bool Save()
        {
            var error = MarkdownNotes.Save(Path, text);
            if (error.Length > 0)
            {
                Status = string.Format(Resources.Note_SaveError, error);
                return false;
            }

            savedText = text;
            SaveCount++;
            fileTime = SafeTime();
            IsExternallyChanged = false;
            lastSaved = string.Format(Resources.Note_Saved, DateTime.Now.ToString("HH:mm", CultureInfo.InvariantCulture));
            Status = string.Empty;
            NotifyDirty();
            return true;
        }

        /// <summary>Diskteki hali okur (değişiklikler atılır).</summary>
        public void Load()
        {
            try
            {
                var read = File.Exists(Path) ? File.ReadAllText(Path) : string.Empty;
                savedText = read;
                text = read;
                fileTime = SafeTime();
                IsExternallyChanged = false;
                lastSaved = string.Empty;
                OnPropertyChanged(nameof(Text));
                OnPropertyChanged(nameof(WordsText));
                NotifyDirty();
            }
            catch (IOException ex)
            {
                Status = ex.Message;
            }
            catch (UnauthorizedAccessException ex)
            {
                Status = ex.Message;
            }
        }

        /// <summary>
        /// Dosya dışarıda değişti mi bakar: değiştiyse ve kaydedilmemiş değişiklik yoksa sessizce yeniden yükler (<c>true</c>);
        /// varsa uyarı şeridini açar.
        /// </summary>
        public bool CheckExternal()
        {
            var time = SafeTime();
            if (time == DateTime.MinValue || time == fileTime)
            {
                return false;
            }

            string disk;
            try
            {
                disk = File.ReadAllText(Path);
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }

            fileTime = time;
            if (string.Equals(disk, savedText, StringComparison.Ordinal))
            {
                return false;
            }

            if (!IsDirty)
            {
                Load();
                return true;
            }

            IsExternallyChanged = true;
            return false;
        }

        /// <summary>Dış değişikliği yok sayar (benimki kalır; kaydedince diskteki ezilir).</summary>
        public void KeepMine()
        {
            IsExternallyChanged = false;
        }

        /// <summary>Görünümde tıklanan görev kutusunu metinde çevirir.</summary>
        public void ToggleTask(int line)
        {
            Text = MarkdownNotes.ToggleTask(text, line);
        }

        /// <summary>Sorguya uyan öneriler (en çok 12): numara / ad başı eşleşenler önce.</summary>
        public List<NoteSuggestion> Suggestions(string query)
        {
            var all = suggestionCache ?? (suggestionCache = suggestionSource().ToList());
            var q = NoteSuggestion.Fold(query ?? string.Empty).Trim();
            return all
                .Where(s => q.Length == 0 || s.Search.Contains(q))
                .OrderBy(s => q.Length > 0 && NoteSuggestion.Fold(s.Key).StartsWith(q, StringComparison.Ordinal) ? 0 : 1)
                .ThenBy(s => s.Key, StringComparer.OrdinalIgnoreCase)
                .Take(12)
                .ToList();
        }

        private void Set(Action change)
        {
            change();
            SaveSettings();
            OnPropertyChanged(string.Empty);
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }

        // Görünümü yeniden çizdirmeyen ayar (arama seçenekleri, panel): yalnız kaydedilir.
        private void SetQuiet(Action change)
        {
            change();
            SaveSettings();
            OnPropertyChanged(string.Empty);
        }

        private void SaveSettings()
        {
            saveSettings();
        }

        private void NotifyDirty()
        {
            OnPropertyChanged(nameof(IsDirty));
            OnPropertyChanged(nameof(SaveText));
            OnPropertyChanged(nameof(WindowTitle));
        }

        private DateTime SafeTime()
        {
            try
            {
                return File.Exists(Path) ? File.GetLastWriteTimeUtc(Path) : DateTime.MinValue;
            }
            catch (IOException)
            {
                return DateTime.MinValue;
            }
            catch (UnauthorizedAccessException)
            {
                return DateTime.MinValue;
            }
        }
    }
}
