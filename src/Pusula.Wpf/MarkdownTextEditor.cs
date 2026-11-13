using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace Pusula.Wpf
{
    /// <summary>
    /// md düzenleyicisi: AvalonEdit (MIT, <c>Dependencies\AvalonEdit</c>) üstüne TextBox'la aynı adlı üyeler. md
    /// renklendirme (<see cref="MarkdownColorizer"/>), satır numarası, geçerli satır vurgusu; renkler temadan.
    /// Metin <see cref="BoundText"/> ile görünüm modeline bağlanır. Satır numaraları TextBox'taki gibi 0'dan başlar
    /// (<see cref="ScrollToLine"/>, <see cref="GetLineIndexFromCharacterIndex"/> …).
    /// </summary>
    public sealed class MarkdownTextEditor : TextEditor
    {
        /// <summary>Görünüm modeline bağlanan metin (iki yönlü).</summary>
        public static readonly DependencyProperty BoundTextProperty = DependencyProperty.Register(
            nameof(BoundText), typeof(string), typeof(MarkdownTextEditor),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnBoundTextChanged));

        private bool syncing;

        /// <summary>Düzenleyiciyi kurar.</summary>
        public MarkdownTextEditor()
        {
            ShowLineNumbers = true;
            Options.HighlightCurrentLine = true;
            Options.EnableHyperlinks = false;
            Options.EnableEmailHyperlinks = false;
            Options.AllowScrollBelowDocument = false;
            Options.ConvertTabsToSpaces = false;
            Options.EnableRectangularSelection = true;
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            BorderThickness = new Thickness(0);
            SetResourceReference(ForegroundProperty, "TextPrimaryBrush");
            Background = Brushes.Transparent;
            SetResourceReference(LineNumbersForegroundProperty, "TextTertiaryBrush");
            TextArea.SetResourceReference(ICSharpCode.AvalonEdit.Editing.TextArea.SelectionBrushProperty, "AccentFillColorTertiaryBrush");
            TextArea.SelectionForeground = null;
            TextArea.SelectionBorder = null;
            TextArea.SelectionCornerRadius = 2;
            TextArea.TextView.SetResourceReference(TextView.CurrentLineBackgroundProperty, "SubtleFillColorSecondaryBrush");
            TextArea.TextView.CurrentLineBorder = null;
            TextArea.Caret.CaretBrush = null;
            TextArea.SetResourceReference(Control.ForegroundProperty, "TextPrimaryBrush");
            TextArea.TextView.LineTransformers.Add(new MarkdownColorizer());
            TextArea.TextView.Margin = new Thickness(8, 0, 0, 0);

            TextChanged += (s, e) =>
            {
                if (syncing)
                {
                    return;
                }

                syncing = true;
                try
                {
                    SetCurrentValue(BoundTextProperty, Text);
                }
                finally
                {
                    syncing = false;
                }
            };
            TextArea.SelectionChanged += (s, e) => RaiseSelectionChanged();
            TextArea.Caret.PositionChanged += (s, e) => RaiseSelectionChanged();

            // Tema değişince renkler yeniden okunur (renklendirici fırçaları her çizimde temadan alır).
            IsVisibleChanged += (s, e) => TextArea.TextView.Redraw();
        }

        /// <summary>Seçim ya da imleç değişti (TextBox'taki SelectionChanged gibi).</summary>
        public event RoutedEventHandler? SelectionChanged;

        /// <summary>Görünüm modeline bağlanan metin.</summary>
        public string BoundText
        {
            get { return (string)GetValue(BoundTextProperty); }
            set { SetValue(BoundTextProperty, value); }
        }

        /// <summary>İmlecin yeri (TextBox.CaretIndex karşılığı).</summary>
        public int CaretIndex
        {
            get { return CaretOffset; }
            set { CaretOffset = Math.Max(0, Math.Min(value, Document.TextLength)); }
        }

        /// <summary>Satır kaydırma (TextBox.TextWrapping karşılığı).</summary>
        public TextWrapping TextWrapping
        {
            get { return WordWrap ? TextWrapping.Wrap : TextWrapping.NoWrap; }
            set { WordWrap = value != TextWrapping.NoWrap; }
        }

        /// <summary>Satırı görünür yapar (0'dan başlayan satır numarası).</summary>
        public new void ScrollToLine(int lineIndex)
        {
            base.ScrollToLine(Math.Max(1, Math.Min(lineIndex + 1, Document.LineCount)));
        }

        /// <summary>Konumun satırı (0'dan).</summary>
        public int GetLineIndexFromCharacterIndex(int index)
        {
            return Document.GetLineByOffset(Math.Max(0, Math.Min(index, Document.TextLength))).LineNumber - 1;
        }

        /// <summary>Satırın ilk konumu (satır 0'dan).</summary>
        public int GetCharacterIndexFromLineIndex(int lineIndex)
        {
            return Document.GetLineByNumber(Math.Max(1, Math.Min(lineIndex + 1, Document.LineCount))).Offset;
        }

        /// <summary>Görünen ilk satır (0'dan).</summary>
        public int GetFirstVisibleLineIndex()
        {
            var view = TextArea.TextView;
            view.EnsureVisualLines();
            var line = view.GetDocumentLineByVisualTop(view.ScrollOffset.Y);
            return line == null ? 0 : line.LineNumber - 1;
        }

        /// <summary>Görünen son satır (0'dan).</summary>
        public int GetLastVisibleLineIndex()
        {
            var view = TextArea.TextView;
            view.EnsureVisualLines();
            var line = view.GetDocumentLineByVisualTop(view.ScrollOffset.Y + Math.Max(0, view.ActualHeight - 1));
            return line == null ? Document.LineCount - 1 : line.LineNumber - 1;
        }

        /// <summary>Konumdaki karakterin dikdörtgeni, düzenleyiciye göre (TextBox.GetRectFromCharacterIndex gibi); çizilmemişse boş.</summary>
        public Rect GetRectFromCharacterIndex(int index)
        {
            var view = TextArea.TextView;
            if (!view.IsLoaded || view.ActualHeight <= 0)
            {
                return Rect.Empty;
            }

            view.EnsureVisualLines();
            var location = Document.GetLocation(Math.Max(0, Math.Min(index, Document.TextLength)));
            var position = new TextViewPosition(location);
            var top = view.GetVisualPosition(position, VisualYPosition.LineTop);
            var bottom = view.GetVisualPosition(position, VisualYPosition.LineBottom);
            var origin = view.TranslatePoint(new Point(top.X - view.ScrollOffset.X, top.Y - view.ScrollOffset.Y), this);
            return new Rect(origin, new Size(1, Math.Max(1, bottom.Y - top.Y)));
        }

        private void RaiseSelectionChanged()
        {
            SelectionChanged?.Invoke(this, new RoutedEventArgs());
        }

        private static void OnBoundTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var editor = (MarkdownTextEditor)d;
            var text = (string)e.NewValue ?? string.Empty;
            if (editor.syncing || string.Equals(editor.Text, text, StringComparison.Ordinal))
            {
                return;
            }

            editor.syncing = true;
            try
            {
                editor.Text = text;
            }
            finally
            {
                editor.syncing = false;
            }
        }
    }
}