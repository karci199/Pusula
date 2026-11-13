using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Pusula.Core.Markdown;

namespace Pusula.Wpf
{
    /// <summary>
    /// Yan yana görünümde birlikte kaydırma: başlıklar çapa olur (düz metindeki başlığın yeri ↔ görünümdeki yeri), aradaki
    /// kısım orantıyla eşlenir; böylece resim ve tablolar kaymaya yol açmaz. Görünüm henüz güncel değilse (başlık sayısı
    /// tutmuyorsa) toplam orana göre eşlenir. Ayardan kapatılabilir.
    /// </summary>
    public partial class NoteWindow
    {
        private double expectedEditorOffset = double.NaN;
        private double expectedPreviewOffset = double.NaN;

        /// <summary>Kaydırma eşitlemesini kurar (yapıcıdan çağrılır).</summary>
        private void HookScrollSync()
        {
            Editor.AddHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler(Editor_ScrollChanged));
            Preview.AddHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler(Preview_ScrollChanged));
        }

        /// <summary>Düz metnin kaydırmasını görünüme uygular (testler için açık).</summary>
        public void SyncPreviewToEditor()
        {
            var scroll = FindScroll(Preview);
            if (scroll == null)
            {
                return;
            }

            var anchors = ScrollAnchors(scroll);
            var target = anchors == null
                ? Ratio(Editor.VerticalOffset, Editor.ExtentHeight - Editor.ViewportHeight) * Math.Max(0, scroll.ExtentHeight - scroll.ViewportHeight)
                : Map(anchors, Editor.VerticalOffset, true);
            target = Math.Max(0, Math.Min(target, scroll.ScrollableHeight));
            if (Math.Abs(target - scroll.VerticalOffset) >= 1)
            {
                expectedPreviewOffset = target;
                scroll.ScrollToVerticalOffset(target);
            }
        }

        /// <summary>Görünümün kaydırmasını düz metne uygular (testler için açık).</summary>
        public void SyncEditorToPreview()
        {
            var scroll = FindScroll(Preview);
            if (scroll == null)
            {
                return;
            }

            var anchors = ScrollAnchors(scroll);
            var target = anchors == null
                ? Ratio(scroll.VerticalOffset, scroll.ScrollableHeight) * Math.Max(0, Editor.ExtentHeight - Editor.ViewportHeight)
                : Map(anchors, scroll.VerticalOffset, false);
            target = Math.Max(0, Math.Min(target, Math.Max(0, Editor.ExtentHeight - Editor.ViewportHeight)));
            if (Math.Abs(target - Editor.VerticalOffset) >= 1)
            {
                expectedEditorOffset = target;
                Editor.ScrollToVerticalOffset(target);
            }
        }

        private bool SyncActive
        {
            get { return viewModel.SyncScroll && viewModel.ShowsEditor && viewModel.ShowsPreview; }
        }

        private void Editor_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.VerticalChange == 0 && e.ExtentHeightChange == 0)
            {
                return;
            }

            // Eşitlemenin kendi kaydırması geri yansımasın.
            if (!double.IsNaN(expectedEditorOffset) && Math.Abs(Editor.VerticalOffset - expectedEditorOffset) < 2)
            {
                expectedEditorOffset = double.NaN;
                return;
            }

            expectedEditorOffset = double.NaN;
            if (e.VerticalChange != 0)
            {
                HighlightOutlineAtTop();
            }

            if (SyncActive)
            {
                SyncPreviewToEditor();
            }
        }

        private void Preview_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.VerticalChange == 0)
            {
                return;
            }

            var scroll = FindScroll(Preview);
            if (scroll != null && !double.IsNaN(expectedPreviewOffset) && Math.Abs(scroll.VerticalOffset - expectedPreviewOffset) < 2)
            {
                expectedPreviewOffset = double.NaN;
                return;
            }

            expectedPreviewOffset = double.NaN;

            // Yalnız kullanıcının görünümde kaydırması (fare üstündeyken) düz metni oynatır; yeniden çizimdeki geri yükleme oynatmaz.
            if (SyncActive && Preview.IsMouseOver)
            {
                SyncEditorToPreview();
            }
        }

        // Çapalar: (düz metin konumu, görünüm konumu) çiftleri, baştan sona artan. Başlık sayısı tutmazsa null.
        private List<Point>? ScrollAnchors(ScrollViewer scroll)
        {
            var render = LastRender;
            var document = Preview.Document;
            if (render == null || document == null)
            {
                return null;
            }

            var outline = MarkdownNotes.Outline(Editor.Text);
            if (outline.Count == 0 || outline.Count != render.Headings.Count)
            {
                return null;
            }

            var docTop = document.ContentStart.GetCharacterRect(LogicalDirection.Forward);
            if (docTop.IsEmpty)
            {
                return null;
            }

            var anchors = new List<Point> { new Point(0, 0) };
            for (var i = 0; i < outline.Count; i++)
            {
                var editorRect = Editor.GetRectFromCharacterIndex(Math.Min(outline[i].Start, Editor.Text.Length));
                var previewRect = render.Headings[i].ContentStart.GetCharacterRect(LogicalDirection.Forward);
                if (editorRect.IsEmpty || previewRect.IsEmpty)
                {
                    continue;
                }

                // Başlık, üst kenardan biraz aşağıda dursun (iki tarafta da aynı boşluk).
                var editorY = editorRect.Top + Editor.VerticalOffset - Editor.Padding.Top;
                var previewY = previewRect.Top - docTop.Top + document.PagePadding.Top - 12;
                var last = anchors[anchors.Count - 1];
                if (editorY > last.X && previewY > last.Y)
                {
                    anchors.Add(new Point(editorY, previewY));
                }
            }

            var end = new Point(Math.Max(0, Editor.ExtentHeight - Editor.ViewportHeight), scroll.ScrollableHeight);
            var tail = anchors[anchors.Count - 1];
            if (end.X > tail.X && end.Y > tail.Y)
            {
                anchors.Add(end);
            }

            return anchors.Count >= 2 ? anchors : null;
        }

        // Çapalar arasında doğrusal eşleme (fromEditor: düz metin → görünüm, değilse tersi).
        private static double Map(List<Point> anchors, double value, bool fromEditor)
        {
            for (var i = 0; i < anchors.Count - 1; i++)
            {
                var a = anchors[i];
                var b = anchors[i + 1];
                var from0 = fromEditor ? a.X : a.Y;
                var from1 = fromEditor ? b.X : b.Y;
                var to0 = fromEditor ? a.Y : a.X;
                var to1 = fromEditor ? b.Y : b.X;
                if (value <= from1 || i == anchors.Count - 2)
                {
                    return to0 + Ratio(value - from0, from1 - from0) * (to1 - to0);
                }
            }

            return 0;
        }

        private static double Ratio(double value, double total)
        {
            return total <= 0 ? 0 : Math.Max(0, Math.Min(1, value / total));
        }
    }
}
