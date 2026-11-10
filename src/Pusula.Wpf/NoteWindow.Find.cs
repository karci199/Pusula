using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Pusula.Core.Markdown;
using Pusula.Wpf.ViewModels;
using Res = Pusula.Wpf.Properties.Resources;

namespace Pusula.Wpf
{
    /// <summary>
    /// Not penceresinde bul / değiştir: Ctrl+F bul, Ctrl+R değiştir, F3 / Shift+F3 sonraki / önceki, Enter / Shift+Enter
    /// kutuda sonraki / önceki, Esc kapatır. Büyük / küçük harf, tam kelime ve düzenli ifade seçenekleri ayarlarda kalır.
    /// </summary>
    public partial class NoteWindow
    {
        private SearchResult? findResult;

        /// <summary>Bul çubuğu açık mı.</summary>
        public bool IsFindOpen
        {
            get { return FindBar.Visibility == Visibility.Visible; }
        }

        /// <summary>Son aramanın sonucu (testler için).</summary>
        public SearchResult? FindResult
        {
            get { return findResult; }
        }

        /// <summary>
        /// Bul çubuğunu açar (<paramref name="replace"/> ise değiştir satırıyla). Düz metinde tek satırlık bir seçim varsa
        /// aranan metin o olur. Yalnız görünüm açıksa yan yana görünüme geçilir.
        /// </summary>
        public void OpenFind(bool replace)
        {
            if (!viewModel.ShowsEditor)
            {
                SetView(NoteEditorViewModel.ViewSplit);
            }

            // Seçim zaten şu anki aramanın bir eşleşmesiyse (ör. bulduktan sonra Ctrl+R) aranan metin korunur.
            var selected = Editor.SelectedText;
            var isMatch = IsFindOpen && Refresh().IndexOf(Editor.SelectionStart, Editor.SelectionLength) >= 0;
            if (!isMatch && selected.Length > 0 && selected.IndexOf('\n') < 0)
            {
                FindBox.Text = selected;
            }

            FindBar.Visibility = Visibility.Visible;
            SetReplaceVisible(replace || ReplaceRow.Visibility == Visibility.Visible && !string.IsNullOrEmpty(ReplaceBox.Text));
            FindBox.Focus();
            FindBox.SelectAll();
            Search(Editor.SelectionStart);
        }

        /// <summary>Bul çubuğunu kapatır, düz metne döner (seçim kalır).</summary>
        public void CloseFind()
        {
            FindBar.Visibility = Visibility.Collapsed;
            findResult = null;
            Editor.Focus();
        }

        /// <summary>Sonraki eşleşmeyi seçer (sona gelince başa döner).</summary>
        public void FindNext()
        {
            Step(true);
        }

        /// <summary>Önceki eşleşmeyi seçer (baştaysa sona döner).</summary>
        public void FindPrevious()
        {
            Step(false);
        }

        /// <summary>Seçili eşleşmeyi değiştirir ve sonrakine geçer; seçim bir eşleşme değilse önce ilk eşleşmeyi seçer.</summary>
        public void ReplaceCurrent()
        {
            var result = Refresh();
            var at = result.IndexOf(Editor.SelectionStart, Editor.SelectionLength);
            if (at < 0)
            {
                Step(true);
                return;
            }

            var edit = TextSearch.Replace(Editor.Text, result.Matches[at], FindBox.Text, ReplaceBox.Text, viewModel.FindOptions);
            if (edit == null)
            {
                return;
            }

            Apply(new EditResult(edit.Text, edit.SelectionStart + edit.SelectionLength, 0), false);
            var after = Refresh();
            Select(after, after.IndexFrom(Editor.CaretIndex));
        }

        /// <summary>Bütün eşleşmeleri değiştirir (tek geri alma adımı); değişen sayıyı verir.</summary>
        public int ReplaceEverything()
        {
            int count;
            var text = TextSearch.ReplaceAll(Editor.Text, FindBox.Text, ReplaceBox.Text, viewModel.FindOptions, out count);
            if (count > 0)
            {
                Apply(new EditResult(text, Math.Min(Editor.SelectionStart, text.Length), 0), false);
            }

            Refresh();
            FindCount.Text = string.Format(Res.Note_ReplacedCount, count);
            return count;
        }

        // Düz metin değişince sayaç güncellenir (seçim yerinde kalır).
        private void RefreshFindCount()
        {
            if (IsFindOpen)
            {
                var result = Refresh();
                ShowCount(result, result.IndexOf(Editor.SelectionStart, Editor.SelectionLength));
            }
        }

        private void Search(int from)
        {
            var result = Refresh();
            Select(result, result.IndexFrom(from));
        }

        private void Step(bool forward)
        {
            var result = Refresh();
            if (result.Matches.Count == 0)
            {
                ShowCount(result, -1);
                return;
            }

            var at = result.IndexOf(Editor.SelectionStart, Editor.SelectionLength);
            int next;
            if (at >= 0)
            {
                next = forward ? (at + 1) % result.Matches.Count : (at - 1 + result.Matches.Count) % result.Matches.Count;
            }
            else
            {
                next = forward ? result.IndexFrom(Editor.SelectionStart + Editor.SelectionLength) : result.IndexBefore(Editor.SelectionStart);
            }

            Select(result, next);
        }

        private SearchResult Refresh()
        {
            findResult = TextSearch.FindAll(Editor.Text, FindBox.Text, viewModel.FindOptions);
            return findResult;
        }

        private void Select(SearchResult result, int index)
        {
            if (index >= 0 && index < result.Matches.Count)
            {
                var match = result.Matches[index];
                Editor.Select(match.Start, match.Length);
                RevealInEditor(match.Start);
            }

            ShowCount(result, index);
        }

        // Konumu düz metinde görünür yapar: satır ekranın üçte birine gelir.
        private void RevealInEditor(int position)
        {
            var line = Editor.GetLineIndexFromCharacterIndex(Math.Min(position, Editor.Text.Length));
            if (line < 0)
            {
                return;
            }

            var first = Editor.GetFirstVisibleLineIndex();
            var last = Editor.GetLastVisibleLineIndex();
            if (first >= 0 && last >= first && line > first && line < last)
            {
                return;
            }

            Editor.ScrollToLine(line);
            var rect = Editor.GetRectFromCharacterIndex(Math.Min(position, Editor.Text.Length));
            if (!rect.IsEmpty)
            {
                Editor.ScrollToVerticalOffset(Math.Max(0, Editor.VerticalOffset + rect.Top - Editor.ActualHeight / 3));
            }
        }

        private void ShowCount(SearchResult result, int index)
        {
            FindCount.ClearValue(ForegroundProperty);
            if (string.IsNullOrEmpty(FindBox.Text))
            {
                FindCount.Text = string.Empty;
            }
            else if (result.Error.Length > 0)
            {
                FindCount.Text = Res.Note_FindInvalid;
                FindCount.ToolTip = result.Error;
                FindCount.SetResourceReference(ForegroundProperty, "SystemFillColorCriticalBrush");
                return;
            }
            else if (result.Matches.Count == 0)
            {
                FindCount.Text = Res.Note_FindNone;
                FindCount.SetResourceReference(ForegroundProperty, "SystemFillColorCautionBrush");
            }
            else
            {
                FindCount.Text = index >= 0
                    ? string.Format(Res.Note_FindCount, index + 1, result.Matches.Count)
                    : string.Format(Res.Note_FindTotal, result.Matches.Count);
            }

            FindCount.ToolTip = null;
        }

        private void SetReplaceVisible(bool visible)
        {
            ReplaceRow.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
            ReplaceToggleIcon.Glyph = visible ? "" : "";
        }

        private void FindBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (IsFindOpen)
            {
                // Yazdıkça: seçimin başından itibaren ilk eşleşme (imleç ileri kaçmaz).
                Search(Editor.SelectionStart);
            }
        }

        private void FindOption_Click(object sender, RoutedEventArgs e)
        {
            Search(Editor.SelectionStart);
        }

        private void FindBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Step((Keyboard.Modifiers & ModifierKeys.Shift) == 0);
                e.Handled = true;
            }
        }

        private void ReplaceBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
                {
                    ReplaceEverything();
                }
                else
                {
                    ReplaceCurrent();
                }

                e.Handled = true;
            }
        }

        private void ReplaceToggle_Click(object sender, RoutedEventArgs e)
        {
            SetReplaceVisible(ReplaceRow.Visibility != Visibility.Visible);
            if (ReplaceRow.Visibility == Visibility.Visible)
            {
                ReplaceBox.Focus();
            }
        }

        private void FindPrevious_Click(object sender, RoutedEventArgs e)
        {
            FindPrevious();
        }

        private void FindNext_Click(object sender, RoutedEventArgs e)
        {
            FindNext();
        }

        private void FindClose_Click(object sender, RoutedEventArgs e)
        {
            CloseFind();
        }

        private void ReplaceOne_Click(object sender, RoutedEventArgs e)
        {
            ReplaceCurrent();
        }

        private void ReplaceAll_Click(object sender, RoutedEventArgs e)
        {
            ReplaceEverything();
        }
    }
}
