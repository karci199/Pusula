using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using DesignKit.Controls;
using Pusula.Core.Markdown;
using Pusula.Wpf.Markdown;
using Res = Pusula.Wpf.Properties.Resources;
using Pusula.Wpf.ViewModels;

namespace Pusula.Wpf
{
    /// <summary>
    /// md not penceresi: ekranın büyük kısmını kaplar; Düz / Yan yana / Görünüm; araç çubuğu, kısayollar, @ ve [[ önerileri,
    /// resim ekleme (dosya, yapıştırma, sürükle-bırak), görev kutusu, dış değişiklik uyarısı, kapatırken kaydetme sorusu.
    /// </summary>
    public partial class NoteWindow : ChromeWindow
    {
        private readonly NoteEditorViewModel viewModel;
        private readonly Func<string, bool> navigate;
        private readonly Action<string> openExternal;
        private readonly Action<string> openFolder;
        private readonly DispatcherTimer renderTimer;
        private readonly DispatcherTimer autoSaveTimer;
        private readonly DispatcherTimer externalTimer;
        private SuggestQuery? query;
        private bool allowClose;
        private bool fullScreen;
        private WindowState stateBeforeFullScreen;

        /// <summary>
        /// Pencereyi kurar. <paramref name="navigate"/> <c>[[hedef]]</c> tıklanınca çağrılır (bulduysa <c>true</c>);
        /// <paramref name="openExternal"/> notu ayarlardaki programda, <paramref name="openFolder"/> klasörü açar.
        /// </summary>
        public NoteWindow(NoteEditorViewModel viewModel, Func<string, bool> navigate, Action<string> openExternal, Action<string> openFolder)
        {
            InitializeComponent();
            this.viewModel = viewModel;
            this.navigate = navigate;
            this.openExternal = openExternal;
            this.openFolder = openFolder;
            DataContext = viewModel;

            renderTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
            renderTimer.Tick += (s, e) =>
            {
                renderTimer.Stop();
                RenderPreview();
                UpdateOutline();
            };
            autoSaveTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            autoSaveTimer.Tick += (s, e) =>
            {
                autoSaveTimer.Stop();
                if (viewModel.AutoSave && viewModel.IsDirty)
                {
                    viewModel.Save();
                }
            };
            externalTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            externalTimer.Tick += (s, e) => CheckExternal();
            externalTimer.Start();
            Activated += (s, e) => CheckExternal();

            viewModel.SettingsChanged += (s, e) => ApplySettings();
            foreach (var width in viewModel.ReadingWidths)
            {
                WidthBox.Items.Add(new ComboBoxItem { Content = width > 0 ? string.Format(Res.Note_WidthPx, width) : Res.Note_WidthFull, Tag = width });
            }

            WidthBox.SelectedItem = WidthBox.Items.OfType<ComboBoxItem>().FirstOrDefault(i => (double)i.Tag == viewModel.ReadingWidth);
            CommandManager.AddPreviewExecutedHandler(Editor, Editor_PreviewExecuted);
            HookScrollSync();
            Loaded += (s, e) =>
            {
                if (viewModel.StartMaximized)
                {
                    WindowState = WindowState.Maximized;
                }

                ApplySettings();
                ApplyOutline();
                Editor.Focus();
                Editor.CaretIndex = 0;
                viewModel.SetCaret(0);
            };
            Closing += NoteWindow_Closing;
            Closed += (s, e) =>
            {
                renderTimer.Stop();
                autoSaveTimer.Stop();
                externalTimer.Stop();
            };
        }

        /// <summary>Gösterilen notun görünüm modeli.</summary>
        public NoteEditorViewModel ViewModel
        {
            get { return viewModel; }
        }

        /// <summary>Son çizimin bilgisi (testler için): resimler ve bulunamayan dosyalar.</summary>
        public MarkdownRenderContext? LastRender { get; private set; }

        /// <summary>Görünümü hemen çizer (zamanlayıcıyı beklemeden).</summary>
        public void RenderPreview()
        {
            if (!viewModel.ShowsPreview)
            {
                return;
            }

            var scroll = FindScroll(Preview);
            var offset = scroll?.VerticalOffset ?? 0;
            var context = new MarkdownRenderContext
            {
                NoteFolder = viewModel.Folder,
                Root = viewModel.Root,
                FontSize = viewModel.PreviewSize,
                FontFamily = "Segoe UI",
                KeepLineBreaks = viewModel.LineBreaks,
                ShowFrontMatter = viewModel.FrontMatter,
                MaxImageWidth = Math.Max(240, Math.Min(viewModel.ReadingWidth > 0 ? viewModel.ReadingWidth : 1200, Preview.ActualWidth > 0 ? Preview.ActualWidth - 64 : 900)),
                WikiLinkClicked = target =>
                {
                    if (!navigate(target))
                    {
                        viewModel.Status = string.Format(Res.Note_LinkMissing, target);
                    }
                },
                UrlClicked = OpenShell,
                FileClicked = OpenShell,
                TaskToggled = line =>
                {
                    viewModel.ToggleTask(line);
                    RenderPreview();
                },
            };
            var document = MarkdownRenderer.Render(viewModel.Text, context);
            document.PagePadding = ReadingPadding(Preview.ActualWidth, 32);
            Preview.Document = document;
            LastRender = context;
            var info = new List<string>();
            if (context.Images.Count > 0)
            {
                info.Add(string.Format(Res.Note_Images, context.Images.Count));
            }

            if (context.MissingFiles.Count > 0)
            {
                info.Add(string.Format(Res.Note_MissingFiles, context.MissingFiles.Count));
            }

            RenderInfo.Text = string.Join(" · ", info);
            RenderInfo.ToolTip = context.MissingFiles.Count > 0 ? string.Join(Environment.NewLine, context.MissingFiles) : null;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                FindScroll(Preview)?.ScrollToVerticalOffset(offset);
                if (SyncActive)
                {
                    SyncPreviewToEditor();
                }
            }), DispatcherPriority.Loaded);
        }

        /// <summary>Biçim işlemi (araç çubuğu ve kısayollar): bold, italic, strike, highlight, code, heading, list, numbers, task, quote, link, image, table, callout, divider.</summary>
        public void ApplyFormat(string action)
        {
            var start = Editor.SelectionStart;
            var length = Editor.SelectionLength;
            var text = Editor.Text;
            EditResult? result = null;
            switch (action)
            {
                case "bold":
                    result = MarkdownEditing.Wrap(text, start, length, "**");
                    break;
                case "italic":
                    result = MarkdownEditing.Wrap(text, start, length, "*");
                    break;
                case "strike":
                    result = MarkdownEditing.Wrap(text, start, length, "~~");
                    break;
                case "highlight":
                    result = MarkdownEditing.Wrap(text, start, length, "==");
                    break;
                case "code":
                    result = MarkdownEditing.Wrap(text, start, length, "`");
                    break;
                case "heading":
                    result = MarkdownEditing.TogglePrefix(text, start, length, NextHeading(text, start));
                    break;
                case "list":
                    result = MarkdownEditing.TogglePrefix(text, start, length, "- ");
                    break;
                case "numbers":
                    result = MarkdownEditing.TogglePrefix(text, start, length, "1. ");
                    break;
                case "task":
                    result = MarkdownEditing.TogglePrefix(text, start, length, "- [ ] ");
                    break;
                case "quote":
                    result = MarkdownEditing.TogglePrefix(text, start, length, "> ");
                    break;
                case "link":
                    result = new EditResult(text.Insert(start, "@"), start + 1, 0);
                    break;
                case "table":
                    var header = Res.Note_TableHeader;
                    var table = "| " + header + " | " + header + " |" + Newline + "|---|---|" + Newline + "|  |  |" + Newline;
                    result = InsertBlock(text, start, table, 2);
                    break;
                case "callout":
                    result = InsertBlock(text, start, "> [!warning] " + Res.Note_CalloutTitle + Newline + "> ", 0);
                    break;
                case "divider":
                    result = InsertBlock(text, start, "---" + Newline, 0);
                    break;
                case "comment":
                    // HTML yorumu: Obsidian, VS Code ve Pusula görünümünde gizlenir.
                    result = MarkdownEditing.Wrap(text, start, length, "<!-- ", " -->");
                    break;
                case "image":
                    PickImage();
                    return;
            }

            if (result != null)
            {
                Apply(result);
            }
        }

        /// <summary>Metne dosya olarak resim ekler (kök dışındaysa notun Ekler klasörüne kopyalanır).</summary>
        public void InsertImageFile(string source)
        {
            var reference = ImageReference(source);
            if (reference.Length > 0)
            {
                InsertAtCaret("![[" + reference + "]]");
            }
        }

        /// <summary>Panodaki resmi notun Ekler klasörüne PNG olarak yazar ve ekler.</summary>
        public bool InsertImage(BitmapSource image)
        {
            try
            {
                var folder = Path.Combine(viewModel.Folder, "Ekler");
                Directory.CreateDirectory(folder);
                var target = UniquePath(Path.Combine(folder, "yapistirilan_" + DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture) + ".png"));
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(image));
                Pusula.Core.Scanning.DiskWatcher.MarkOwnWrite();
                using (var stream = File.Create(target))
                {
                    encoder.Save(stream);
                }

                InsertAtCaret("![[" + Relative(target) + "]]");
                return true;
            }
            catch (IOException ex)
            {
                viewModel.Status = ex.Message;
                return false;
            }
            catch (UnauthorizedAccessException ex)
            {
                viewModel.Status = ex.Message;
                return false;
            }
        }

        /// <summary>Öneri listesi açık mı (testler için).</summary>
        public bool IsSuggesting
        {
            get { return SuggestPopup.IsOpen; }
        }

        /// <summary>Öneri listesindeki öğeler (testler için).</summary>
        public IEnumerable<NoteSuggestion> SuggestionItems
        {
            get { return SuggestList.Items.OfType<NoteSuggestion>(); }
        }

        /// <summary>Seçili öneriyi yerleştirir (Enter / Tab / tıklama).</summary>
        public bool AcceptSuggestion()
        {
            var chosen = SuggestList.SelectedItem as NoteSuggestion;
            if (query == null || chosen == null)
            {
                return false;
            }

            var insert = chosen.Insert;
            if (query.IsLink && insert.StartsWith("@", StringComparison.Ordinal))
            {
                insert = "[[" + insert.Substring(1) + "]]";
            }

            var result = MarkdownEditing.ApplySuggestion(Editor.Text, Editor.CaretIndex, query, insert);
            CloseSuggestions();
            Apply(result);
            return true;
        }

        private string Newline
        {
            get { return Editor.Text.Contains("\n") && !Editor.Text.Contains("\r\n") ? "\n" : "\r\n"; }
        }

        // Düzenlemeyi uygular: metnin tamamı yeniden yazılmaz, yalnız değişen parça yerinde değiştirilir; böylece Ctrl+Z
        // geçmişi korunur ve işlem tek adımda geri alınır.
        private void Apply(EditResult result, bool focus = true)
        {
            var before = Editor.Text;
            var after = result.Text ?? string.Empty;
            if (!string.Equals(before, after, StringComparison.Ordinal))
            {
                var prefix = 0;
                var max = Math.Min(before.Length, after.Length);
                while (prefix < max && before[prefix] == after[prefix])
                {
                    prefix++;
                }

                var suffix = 0;
                while (suffix < max - prefix && before[before.Length - 1 - suffix] == after[after.Length - 1 - suffix])
                {
                    suffix++;
                }

                Editor.BeginChange();
                try
                {
                    Editor.Select(prefix, before.Length - prefix - suffix);
                    Editor.SelectedText = after.Substring(prefix, after.Length - prefix - suffix);
                }
                finally
                {
                    Editor.EndChange();
                }
            }

            var start = Math.Min(result.SelectionStart, Editor.Text.Length);
            Editor.Select(start, Math.Min(result.SelectionLength, Editor.Text.Length - start));
            if (focus)
            {
                Editor.Focus();
            }
        }

        private EditResult InsertBlock(string text, int caret, string block, int caretInBlock)
        {
            var lineStart = caret == 0 ? 0 : text.LastIndexOf('\n', caret - 1) + 1;
            var atLineStart = caret == lineStart && (caret >= text.Length || text[caret] == '\r' || text[caret] == '\n');
            var prefix = atLineStart ? string.Empty : Newline;
            var insert = prefix + block;
            var position = caret + prefix.Length + (caretInBlock > 0 ? caretInBlock : block.Length);
            return new EditResult(text.Insert(caret, insert), position, 0);
        }

        private static string NextHeading(string text, int caret)
        {
            var lineStart = caret == 0 ? 0 : text.LastIndexOf('\n', Math.Max(0, caret - 1)) + 1;
            var hashes = 0;
            while (lineStart + hashes < text.Length && text[lineStart + hashes] == '#')
            {
                hashes++;
            }

            // # → ## → ### → (yok) → #
            return hashes >= 3 ? new string('#', hashes) + " " : new string('#', hashes + 1) + " ";
        }

        private void InsertAtCaret(string insert)
        {
            var caret = Editor.SelectionStart;
            var text = Editor.Text.Remove(caret, Editor.SelectionLength);
            Apply(new EditResult(text.Insert(caret, insert), caret + insert.Length, 0));
        }

        private string ImageReference(string source)
        {
            try
            {
                var full = Path.GetFullPath(source);
                var root = viewModel.Root;
                if (root.Length > 0 && full.StartsWith(root.TrimEnd('\\') + "\\", StringComparison.OrdinalIgnoreCase))
                {
                    return Relative(full);
                }

                // Kök dışındaki dosya kopyalanır (taşınmaz); asıl dosyaya dokunulmaz.
                var folder = Path.Combine(viewModel.Folder, "Ekler");
                Directory.CreateDirectory(folder);
                var target = UniquePath(Path.Combine(folder, Path.GetFileName(full)));
                Pusula.Core.Scanning.DiskWatcher.MarkOwnWrite();
                File.Copy(full, target);
                viewModel.Status = string.Format(Res.Note_ImageCopied, Relative(target));
                return Relative(target);
            }
            catch (IOException ex)
            {
                viewModel.Status = ex.Message;
            }
            catch (UnauthorizedAccessException ex)
            {
                viewModel.Status = ex.Message;
            }
            catch (ArgumentException ex)
            {
                viewModel.Status = ex.Message;
            }

            return string.Empty;
        }

        // Notun klasörüne göre yol ("Ekler/a.png"); klasör dışındaysa köke göre.
        private string Relative(string full)
        {
            foreach (var baseFolder in new[] { viewModel.Folder, viewModel.Root })
            {
                if (baseFolder.Length > 0 && full.StartsWith(baseFolder.TrimEnd('\\') + "\\", StringComparison.OrdinalIgnoreCase))
                {
                    return full.Substring(baseFolder.TrimEnd('\\').Length + 1).Replace('\\', '/');
                }
            }

            return Path.GetFileName(full);
        }

        private static string UniquePath(string path)
        {
            if (!File.Exists(path))
            {
                return path;
            }

            var folder = Path.GetDirectoryName(path) ?? string.Empty;
            var name = Path.GetFileNameWithoutExtension(path);
            var extension = Path.GetExtension(path);
            for (var i = 2; ; i++)
            {
                var candidate = Path.Combine(folder, name + "_" + i.ToString(CultureInfo.InvariantCulture) + extension);
                if (!File.Exists(candidate))
                {
                    return candidate;
                }
            }
        }

        private void PickImage()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = Res.Note_ImagesFilter + "|*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.webp;*.tif;*.tiff",
                InitialDirectory = viewModel.Folder,
                Multiselect = true,
            };
            if (dialog.ShowDialog(this) == true)
            {
                foreach (var file in dialog.FileNames)
                {
                    InsertImageFile(file);
                    InsertAtCaret(Newline);
                }
            }
        }

        private static void OpenShell(string target)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(target) { UseShellExecute = true });
            }
            catch (System.ComponentModel.Win32Exception)
            {
            }
            catch (InvalidOperationException)
            {
            }
        }

        private void ApplySettings()
        {
            Editor.FontFamily = new FontFamily(viewModel.EditorFont);
            Editor.FontSize = viewModel.EditorSize;
            Editor.TextWrapping = viewModel.Wrap ? TextWrapping.Wrap : TextWrapping.NoWrap;
            ApplyView();
            RenderPreview();
        }

        private void ApplyView()
        {
            var edit = viewModel.ShowsEditor;
            var preview = viewModel.ShowsPreview;
            EditorColumn.Width = edit ? new GridLength(preview ? viewModel.SplitRatio : 1, GridUnitType.Star) : new GridLength(0);
            PreviewColumn.Width = preview ? new GridLength(edit ? 1 - viewModel.SplitRatio : 1, GridUnitType.Star) : new GridLength(0);
            Editor.Visibility = edit ? Visibility.Visible : Visibility.Collapsed;
            Preview.Visibility = preview ? Visibility.Visible : Visibility.Collapsed;
            Splitter.Visibility = edit && preview ? Visibility.Visible : Visibility.Collapsed;
            UpdateEditorPadding();
            if (preview)
            {
                RenderPreview();
            }
        }

        // Okuma genişliği: içerik ortada, iki yanda boşluk.
        private Thickness ReadingPadding(double available, double minimum)
        {
            var width = viewModel.ReadingWidth;
            var side = width > 0 && available > width + 2 * minimum ? (available - width) / 2 : minimum;
            return new Thickness(side, 24, side, 48);
        }

        private void UpdateEditorPadding()
        {
            var available = Editor.ActualWidth;
            if (!viewModel.ShowsPreview && available > 0)
            {
                var padding = ReadingPadding(available, 28);
                Editor.Padding = new Thickness(padding.Left, 20, padding.Right, 40);
            }
            else
            {
                Editor.Padding = new Thickness(28, 20, 28, 40);
            }
        }

        private void CheckExternal()
        {
            if (viewModel.CheckExternal())
            {
                RenderPreview();
            }
        }

        private void Format_Click(object sender, RoutedEventArgs e)
        {
            ApplyFormat((sender as FrameworkElement)?.Tag as string ?? string.Empty);
        }

        private void View_Click(object sender, RoutedEventArgs e)
        {
            viewModel.ViewMode = (sender as FrameworkElement)?.Tag as string ?? NoteEditorViewModel.ViewSplit;
            ApplyView();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            viewModel.Save();
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            SettingsPopup.IsOpen = !SettingsPopup.IsOpen;
        }

        private void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            openFolder(viewModel.Folder);
        }

        private void OpenExternal_Click(object sender, RoutedEventArgs e)
        {
            if (viewModel.IsDirty)
            {
                viewModel.Save();
            }

            openExternal(viewModel.Path);
        }

        private void FullScreen_Click(object sender, RoutedEventArgs e)
        {
            ToggleFullScreen();
        }

        private void ToggleFullScreen()
        {
            fullScreen = !fullScreen;
            if (fullScreen)
            {
                stateBeforeFullScreen = WindowState;
                WindowState = WindowState.Normal;
                WindowStyle = WindowStyle.None;
                WindowState = WindowState.Maximized;
            }
            else
            {
                WindowStyle = WindowStyle.SingleBorderWindow;
                WindowState = stateBeforeFullScreen;
            }
        }

        private void Reload_Click(object sender, RoutedEventArgs e)
        {
            viewModel.Load();
            RenderPreview();
        }

        private void KeepMine_Click(object sender, RoutedEventArgs e)
        {
            viewModel.KeepMine();
        }

        private void WidthBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (WidthBox.SelectedItem is ComboBoxItem item && IsLoaded)
            {
                viewModel.ReadingWidth = (double)item.Tag;
            }
        }

        private void Splitter_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            var total = EditorColumn.ActualWidth + PreviewColumn.ActualWidth;
            if (total > 0)
            {
                viewModel.SplitRatio = EditorColumn.ActualWidth / total;
            }
        }

        private void Preview_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (Preview.Document != null)
            {
                Preview.Document.PagePadding = ReadingPadding(Preview.ActualWidth, 32);
            }

            UpdateEditorPadding();
        }

        private void Editor_TextChanged(object sender, TextChangedEventArgs e)
        {
            renderTimer.Stop();
            renderTimer.Start();
            if (viewModel.AutoSave)
            {
                autoSaveTimer.Stop();
                autoSaveTimer.Start();
            }

            RefreshFindCount();

            // İmleç yerine oturduktan sonra bakılır (yapıştırma / programla yazmada seçim önce metni kapsar).
            Dispatcher.BeginInvoke(new Action(UpdateSuggestions), DispatcherPriority.Input);
        }

        private void Editor_SelectionChanged(object sender, RoutedEventArgs e)
        {
            viewModel.SetCaret(Editor.CaretIndex);
            HighlightOutline();
            if (SuggestPopup.IsOpen)
            {
                UpdateSuggestions();
            }
        }

        private void UpdateSuggestions()
        {
            // @ ve [[ ile kayıtlar; # ile etiketler (Obsidian etiketi, ana pencerenin bağlamındaki etiketler).
            var found = Editor.SelectionLength == 0 ? MarkdownEditing.FindQuery(Editor.Text, Editor.CaretIndex) : null;
            List<NoteSuggestion> items;
            if (found != null)
            {
                items = viewModel.Suggestions(found.Text);
            }
            else
            {
                // # tek başına öneri açmaz ("# Başlık" yazarken karışmasın); bir harf yazılınca açılır.
                found = Editor.SelectionLength == 0 && MentionAssist.TagProvider != null ? MarkdownEditing.FindTagQuery(Editor.Text, Editor.CaretIndex) : null;
                if (found == null || found.Text.Length == 0)
                {
                    CloseSuggestions();
                    return;
                }

                items = NoteSuggestion.Filter(MentionAssist.TagProvider!().ToList(), found.Text);
            }

            if (items.Count == 0)
            {
                CloseSuggestions();
                return;
            }

            query = found;
            SuggestList.ItemsSource = items;
            SuggestList.SelectedIndex = 0;
            var rect = Editor.GetRectFromCharacterIndex(Math.Min(Editor.CaretIndex, Editor.Text.Length));
            if (!rect.IsEmpty)
            {
                SuggestPopup.HorizontalOffset = rect.Left;
                SuggestPopup.VerticalOffset = rect.Bottom + 4;
            }

            SuggestPopup.IsOpen = true;
        }

        private void CloseSuggestions()
        {
            query = null;
            SuggestPopup.IsOpen = false;
        }

        private void SuggestList_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if ((e.OriginalSource as FrameworkElement)?.DataContext is NoteSuggestion item)
            {
                SuggestList.SelectedItem = item;
                AcceptSuggestion();
                e.Handled = true;
            }
        }

        private void Editor_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // AltGr, Windows'ta Ctrl+Alt olarak gelir (Türkçe klavyede # = AltGr+3, @ = AltGr+Q);
            // Alt basılıyken Ctrl kısayolu sayılmaz, yoksa # yazmak görünümü değiştirir.
            var alt = (Keyboard.Modifiers & ModifierKeys.Alt) != 0;
            var ctrl = !alt && (Keyboard.Modifiers & ModifierKeys.Control) != 0;
            var shift = (Keyboard.Modifiers & ModifierKeys.Shift) != 0;
            if (alt && (Keyboard.Modifiers & ModifierKeys.Control) != 0)
            {
                return;
            }

            if (SuggestPopup.IsOpen)
            {
                switch (e.Key)
                {
                    case Key.Down:
                        SuggestList.SelectedIndex = Math.Min(SuggestList.Items.Count - 1, SuggestList.SelectedIndex + 1);
                        SuggestList.ScrollIntoView(SuggestList.SelectedItem);
                        e.Handled = true;
                        return;
                    case Key.Up:
                        SuggestList.SelectedIndex = Math.Max(0, SuggestList.SelectedIndex - 1);
                        SuggestList.ScrollIntoView(SuggestList.SelectedItem);
                        e.Handled = true;
                        return;
                    case Key.Enter:
                    case Key.Tab:
                        e.Handled = AcceptSuggestion();
                        return;
                    case Key.Escape:
                        CloseSuggestions();
                        e.Handled = true;
                        return;
                }
            }

            // Alt+↑ / Alt+↓: satır taşı.
            var systemKey = e.Key == Key.System ? e.SystemKey : e.Key;
            if (alt && !ctrl && !shift && (systemKey == Key.Up || systemKey == Key.Down))
            {
                MoveLine(systemKey == Key.Up);
                e.Handled = true;
                return;
            }

            e.Handled = HandleKey(systemKey, ctrl, shift);
        }

        // Pencere kısayolları: odak nerede olursa olsun (yalnız görünüm açıkken, bul kutusundayken de) çalışır.
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var modifiers = Keyboard.Modifiers;
            if ((modifiers & ModifierKeys.Alt) != 0 && (modifiers & ModifierKeys.Control) != 0)
            {
                return;
            }

            var key = e.Key == Key.System ? e.SystemKey : e.Key;
            if (HandleGlobalKey(key, (modifiers & ModifierKeys.Control) != 0 && (modifiers & ModifierKeys.Alt) == 0, (modifiers & ModifierKeys.Shift) != 0))
            {
                e.Handled = true;
            }
        }

        /// <summary>Pencere kısayolunu işler (testler için açık); işlediyse <c>true</c>.</summary>
        public bool HandleGlobalKey(Key key, bool ctrl, bool shift)
        {
            if (ctrl && !shift)
            {
                switch (key)
                {
                    case Key.S:
                        viewModel.Save();
                        return true;
                    case Key.F:
                        OpenFind(false);
                        return true;
                    case Key.R:
                        OpenFind(true);
                        return true;
                    case Key.D1:
                    case Key.NumPad1:
                        SetView(NoteEditorViewModel.ViewEdit);
                        return true;
                    case Key.D2:
                    case Key.NumPad2:
                        SetView(NoteEditorViewModel.ViewSplit);
                        return true;
                    case Key.D3:
                    case Key.NumPad3:
                        SetView(NoteEditorViewModel.ViewPreview);
                        return true;
                    case Key.OemPlus:
                    case Key.Add:
                        viewModel.EditorSize += 1;
                        viewModel.PreviewSize += 1;
                        return true;
                    case Key.OemMinus:
                    case Key.Subtract:
                        viewModel.EditorSize -= 1;
                        viewModel.PreviewSize -= 1;
                        return true;
                }
            }

            if (ctrl && shift && key == Key.O)
            {
                ToggleOutline();
                return true;
            }

            if (key == Key.F3 && !ctrl)
            {
                if (!IsFindOpen)
                {
                    OpenFind(false);
                }
                else if (shift)
                {
                    FindPrevious();
                }
                else
                {
                    FindNext();
                }

                return true;
            }

            if (key == Key.F11)
            {
                ToggleFullScreen();
                return true;
            }

            if (key == Key.Escape && !SuggestPopup.IsOpen)
            {
                if (IsFindOpen)
                {
                    CloseFind();
                    return true;
                }

                if (fullScreen)
                {
                    ToggleFullScreen();
                    return true;
                }
            }

            return false;
        }

        /// <summary>Kısayolu işler (testler için açık): önce pencere kısayolları, sonra düz metin kısayolları; işlediyse <c>true</c>.</summary>
        public bool HandleKey(Key key, bool ctrl, bool shift)
        {
            if (HandleGlobalKey(key, ctrl, shift))
            {
                return true;
            }

            if (ctrl && !shift)
            {
                switch (key)
                {
                    case Key.B:
                        ApplyFormat("bold");
                        return true;
                    case Key.I:
                        ApplyFormat("italic");
                        return true;
                    case Key.E:
                        ApplyFormat("code");
                        return true;
                    case Key.L:
                        ApplyFormat("task");
                        return true;
                    case Key.K:
                        ApplyFormat("link");
                        return true;
                    case Key.H:
                        ApplyFormat("heading");
                        return true;
                    case Key.D:
                        Apply(MarkdownEditing.DuplicateLines(Editor.Text, Editor.SelectionStart, Editor.SelectionLength));
                        return true;
                }
            }

            if (key == Key.Back && !ctrl && !shift && viewModel.AutoPair && Editor.SelectionLength == 0)
            {
                var pair = MarkdownEditing.DeletePair(Editor.Text, Editor.CaretIndex);
                if (pair != null)
                {
                    Apply(pair);
                    return true;
                }
            }

            if (ctrl && shift)
            {
                switch (key)
                {
                    case Key.X:
                        ApplyFormat("strike");
                        return true;
                    case Key.C:
                        ApplyFormat("comment");
                        return true;
                    case Key.K:
                        Apply(MarkdownEditing.DeleteLines(Editor.Text, Editor.SelectionStart, Editor.SelectionLength));
                        return true;
                    case Key.T:
                        var table = MarkdownEditing.FormatTable(Editor.Text, Editor.CaretIndex);
                        if (table != null)
                        {
                            Apply(table);
                        }
                        else
                        {
                            viewModel.Status = Res.Note_NoTable;
                        }

                        return true;
                    case Key.H:
                        ApplyFormat("highlight");
                        return true;
                }
            }

            if (key == Key.Enter && !ctrl && !shift && Editor.SelectionLength == 0)
            {
                var result = MarkdownEditing.ContinueList(Editor.Text, Editor.CaretIndex);
                if (result != null)
                {
                    Apply(result);
                    return true;
                }
            }

            // Tablo içinde Tab / Shift+Tab: hizala ve sonraki / önceki hücre (son hücrede yeni satır).
            if (key == Key.Tab && !ctrl && Editor.SelectedText.IndexOf('\n') < 0)
            {
                var cell = MarkdownEditing.TableNextCell(Editor.Text, Editor.SelectionStart, shift);
                if (cell != null)
                {
                    Apply(cell);
                    return true;
                }
            }

            if (key == Key.Tab && !ctrl)
            {
                Apply(MarkdownEditing.Indent(Editor.Text, Editor.SelectionStart, Editor.SelectionLength, shift));
                return true;
            }

            return false;
        }

        private void SetView(string mode)
        {
            viewModel.ViewMode = mode;
            ApplyView();
        }

        private void Editor_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
            {
                viewModel.EditorSize += e.Delta > 0 ? 1 : -1;
                e.Handled = true;
            }
        }

        private void Preview_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
            {
                viewModel.PreviewSize += e.Delta > 0 ? 1 : -1;
                e.Handled = true;
            }
        }

        // Yapıştır: panoda resim ya da resim dosyası varsa Ekler klasörüne yazılıp ![[…]] eklenir.
        private void Editor_PreviewExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Command != ApplicationCommands.Paste)
            {
                return;
            }

            if (Clipboard.ContainsFileDropList())
            {
                var images = Clipboard.GetFileDropList().Cast<string>().Where(MarkdownNotes.IsImage).ToList();
                if (images.Count > 0)
                {
                    foreach (var file in images)
                    {
                        InsertImageFile(file);
                    }

                    e.Handled = true;
                    return;
                }
            }

            if (Clipboard.ContainsImage() && !Clipboard.ContainsText())
            {
                var image = Clipboard.GetImage();
                if (image != null && InsertImage(image))
                {
                    e.Handled = true;
                }

                return;
            }

            // Seçili metnin üzerine web adresi: [seçim](adres).
            if (Clipboard.ContainsText() && PasteText(Clipboard.GetText()))
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// Metin yapıştırır (testler için açık): seçim varken panodaki web adresi bağlantı olur (<c>[seçim](adres)</c>).
        /// Bağlantı yaptıysa <c>true</c>; değilse hiçbir şey yapmaz (normal yapıştırma sürer).
        /// </summary>
        public bool PasteText(string clipboard)
        {
            var link = MarkdownEditing.PasteLink(Editor.Text, Editor.SelectionStart, Editor.SelectionLength, clipboard);
            if (link == null)
            {
                return false;
            }

            Apply(link);
            return true;
        }

        /// <summary>Seçili satırları bir satır yukarı / aşağı taşır (Alt+↑ / Alt+↓).</summary>
        public void MoveLine(bool up)
        {
            var result = MarkdownEditing.MoveLines(Editor.Text, Editor.SelectionStart, Editor.SelectionLength, up);
            if (result != null)
            {
                Apply(result);
            }
        }

        /// <summary>Yazılan karakteri otomatik çift kurallarıyla işler (testler için açık); işlediyse <c>true</c>.</summary>
        public bool TypeChar(char typed)
        {
            if (!viewModel.AutoPair)
            {
                return false;
            }

            var result = MarkdownEditing.AutoPair(Editor.Text, Editor.SelectionStart, Editor.SelectionLength, typed);
            if (result == null)
            {
                return false;
            }

            Apply(result);
            return true;
        }

        private void Editor_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (e.Text.Length == 1 && TypeChar(e.Text[0]))
            {
                e.Handled = true;
            }
        }

        private void Editor_PreviewDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
                e.Handled = true;
            }
        }

        private void Editor_PreviewDrop(object sender, DragEventArgs e)
        {
            if (!(e.Data.GetData(DataFormats.FileDrop) is string[] files))
            {
                return;
            }

            foreach (var file in files)
            {
                if (MarkdownNotes.IsImage(file))
                {
                    InsertImageFile(file);
                }
                else
                {
                    var reference = Relative(file);
                    InsertAtCaret("[[" + reference + "]]");
                }

                InsertAtCaret(Newline);
            }

            e.Handled = true;
        }

        private void NoteWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (allowClose || !viewModel.IsDirty)
            {
                return;
            }

            e.Cancel = true;
            CloseText.Text = string.Format(Res.Note_CloseText, viewModel.FileName);
            CloseDialog.IsOpen = true;
        }

        private void CloseDialog_Save(object sender, RoutedEventArgs e)
        {
            if (viewModel.Save())
            {
                allowClose = true;
                Dispatcher.BeginInvoke(new Action(Close));
            }
        }

        private void CloseDialog_Discard(object sender, RoutedEventArgs e)
        {
            allowClose = true;
            Dispatcher.BeginInvoke(new Action(Close));
        }

        private static ScrollViewer? FindScroll(DependencyObject root)
        {
            if (root is ScrollViewer scroll)
            {
                return scroll;
            }

            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            {
                var found = FindScroll(VisualTreeHelper.GetChild(root, i));
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
