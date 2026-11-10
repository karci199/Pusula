using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Pusula.Core.Markdown;
using Res = Pusula.Wpf.Properties.Resources;

namespace Pusula.Wpf
{
    /// <summary>
    /// Not penceresinin başlık paneli (Ctrl+Shift+O): notun başlıkları düzeye göre girintili; tıklanınca düz metinde ve
    /// görünümde o başlığa gidilir; imlecin bulunduğu başlık seçili görünür. Açık / kapalı ayarlarda kalır.
    /// </summary>
    public partial class NoteWindow
    {
        private IList<OutlineItem> outlineItems = new List<OutlineItem>();
        private bool syncingOutline;

        /// <summary>Başlık paneli açık mı.</summary>
        public bool IsOutlineOpen
        {
            get { return OutlinePanel.Visibility == Visibility.Visible; }
        }

        /// <summary>Paneldeki başlıklar (testler için).</summary>
        public IList<OutlineItem> OutlineItems
        {
            get { return outlineItems; }
        }

        /// <summary>Başlık panelini açar / kapatır (ayar kaydedilir).</summary>
        public void ToggleOutline()
        {
            viewModel.ShowOutline = !viewModel.ShowOutline;
            ApplyOutline();
        }

        /// <summary>
        /// Sıradaki başlığa gider: düz metinde imleç başlığın başına konur, görünümde başlık en üste kaydırılır.
        /// </summary>
        public void GoToHeading(int index)
        {
            if (index < 0 || index >= outlineItems.Count)
            {
                return;
            }

            var item = outlineItems[index];
            if (viewModel.ShowsEditor)
            {
                // Başlık düz metinde de en üste gelir (birlikte kaydırmada iki taraf aynı yerde durur).
                var start = Math.Min(item.Start, Editor.Text.Length);
                Editor.Select(start, 0);
                Editor.ScrollToLine(Editor.GetLineIndexFromCharacterIndex(start));
                var rect = Editor.GetRectFromCharacterIndex(start);
                if (!rect.IsEmpty)
                {
                    Editor.ScrollToVerticalOffset(Math.Max(0, Editor.VerticalOffset + rect.Top - Editor.Padding.Top));
                }

                Editor.Focus();
            }

            if (viewModel.ShowsPreview && LastRender != null && index < LastRender.Headings.Count)
            {
                ScrollPreviewTo(LastRender.Headings[index]);
            }
        }

        private void ApplyOutline()
        {
            var open = viewModel.ShowOutline;
            OutlinePanel.Visibility = open ? Visibility.Visible : Visibility.Collapsed;
            OutlineToggle.IsChecked = open;
            UpdateOutline();
        }

        // Metin değişince (çizim zamanlayıcısıyla) yeniden çıkarılır.
        private void UpdateOutline()
        {
            if (!IsOutlineOpen)
            {
                return;
            }

            outlineItems = MarkdownNotes.Outline(Editor.Text);
            syncingOutline = true;
            try
            {
                OutlineList.ItemsSource = outlineItems;
            }
            finally
            {
                syncingOutline = false;
            }

            OutlineEmpty.Visibility = outlineItems.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            OutlineHeader.Text = outlineItems.Count == 0 ? Res.Note_Outline : string.Format(Res.Note_OutlineCount, outlineItems.Count);
            HighlightOutline();
        }

        // İmlecin bulunduğu başlık seçili görünür (gezinme tetiklenmez).
        private void HighlightOutline()
        {
            HighlightOutlineAt(Editor.CaretIndex);
        }

        // Kaydırınca: imleç ekrandaysa onun başlığı, değilse düz metinde en üstte görünen satırın başlığı seçili görünür.
        private void HighlightOutlineAtTop()
        {
            var first = Editor.GetFirstVisibleLineIndex();
            var last = Editor.GetLastVisibleLineIndex();
            if (first < 0)
            {
                return;
            }

            var caretLine = Editor.GetLineIndexFromCharacterIndex(Math.Min(Editor.CaretIndex, Editor.Text.Length));
            HighlightOutlineAt(caretLine >= first && caretLine <= last ? Editor.CaretIndex : Editor.GetCharacterIndexFromLineIndex(first));
        }

        private void HighlightOutlineAt(int position)
        {
            if (!IsOutlineOpen || outlineItems.Count == 0)
            {
                return;
            }

            var current = outlineItems.LastOrDefault(i => i.Start <= position) ?? outlineItems[0];
            if (ReferenceEquals(OutlineList.SelectedItem, current))
            {
                return;
            }

            syncingOutline = true;
            try
            {
                OutlineList.SelectedItem = current;
                OutlineList.ScrollIntoView(current);
            }
            finally
            {
                syncingOutline = false;
            }
        }

        // Görünümde paragrafı üste getirir (BringIntoView yalnız görünür yapar, üste almaz).
        private void ScrollPreviewTo(Paragraph paragraph)
        {
            var scroll = FindScroll(Preview);
            if (scroll == null)
            {
                paragraph.BringIntoView();
                return;
            }

            // Konum, belgenin başına göre fark olarak alınır (koordinatın görünüme mi belgeye mi göre olduğundan bağımsız).
            var rect = paragraph.ContentStart.GetCharacterRect(LogicalDirection.Forward);
            var top = Preview.Document?.ContentStart.GetCharacterRect(LogicalDirection.Forward) ?? Rect.Empty;
            if (rect.IsEmpty || top.IsEmpty)
            {
                paragraph.BringIntoView();
                return;
            }

            scroll.ScrollToVerticalOffset(Math.Max(0, rect.Top - top.Top + (Preview.Document?.PagePadding.Top ?? 0) - 12));
        }

        private void OutlineList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!syncingOutline && OutlineList.SelectedIndex >= 0)
            {
                GoToHeading(OutlineList.SelectedIndex);
            }
        }

        private void OutlineToggle_Click(object sender, RoutedEventArgs e)
        {
            ToggleOutline();
        }

        private void OutlineClose_Click(object sender, RoutedEventArgs e)
        {
            if (viewModel.ShowOutline)
            {
                ToggleOutline();
            }
        }
    }
}
