using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using DesignKit.Controls;
using Pusula.Core.Markdown;

namespace Pusula.Wpf
{
    /// <summary>
    /// Etiket kutusu: seçilen etiketler kutunun içinde renkli rozet olarak durur (× ile silinir), yanında yazma yeri
    /// vardır; <c>#</c> yazınca bu türde geçerli etiketler önerilir (<see cref="MentionAssist"/>, <c>Mode="tag"</c>).
    /// Öneri seçilince ya da boşluk / Enter basılınca yazılan etiket rozete döner; kutu boşken Backspace son rozeti siler.
    /// Değer <see cref="Tags"/>: <c>#a #b</c> metni (görünüm modeli bu metni okur / yazar).
    /// </summary>
    public sealed class TagBox : Border
    {
        /// <summary>Etiketler metni: <c>#SATIMO #faz_kararli</c>.</summary>
        public static readonly DependencyProperty TagsProperty = DependencyProperty.Register(
            nameof(Tags), typeof(string), typeof(TagBox),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (d, e) => ((TagBox)d).ShowTags()));

        /// <summary>Kutu boşken görünen ipucu.</summary>
        public static readonly DependencyProperty PlaceholderProperty = DependencyProperty.Register(
            nameof(Placeholder), typeof(string), typeof(TagBox), new PropertyMetadata(string.Empty, (d, e) => TextBoxHelper.SetPlaceholderText(((TagBox)d).input, (string)e.NewValue)));

        private readonly WrapPanel panel = new WrapPanel();
        private readonly TextBox input = new TextBox { MinWidth = 120, BorderThickness = new Thickness(0), Padding = new Thickness(2, 3, 2, 3) };
        private bool writing;

        /// <summary>Kutuyu kurar.</summary>
        public TagBox()
        {
            SetResourceReference(BackgroundProperty, "ControlFillColorDefaultBrush");
            SetResourceReference(BorderBrushProperty, "ControlStrokeColorDefaultBrush");
            SetResourceReference(CornerRadiusProperty, "ControlCornerRadius");
            BorderThickness = new Thickness(1);
            Padding = new Thickness(4, 2, 4, 2);
            MinHeight = 32;
            Cursor = Cursors.IBeam;
            input.Background = System.Windows.Media.Brushes.Transparent;

            // Yazma yeri sade: kendi çerçevesi / odak çizgisi yok (dış kutu yeter).
            input.Template = (ControlTemplate)System.Windows.Markup.XamlReader.Parse(
                "<ControlTemplate TargetType='TextBox' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>" +
                "<ScrollViewer x:Name='PART_ContentHost' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' Focusable='False' HorizontalScrollBarVisibility='Hidden' VerticalScrollBarVisibility='Hidden' />" +
                "</ControlTemplate>");
            input.SetResourceReference(TextBox.ForegroundProperty, "TextPrimaryBrush");
            input.SetResourceReference(TextBox.CaretBrushProperty, "TextPrimaryBrush");
            input.FocusVisualStyle = null;
            input.VerticalAlignment = VerticalAlignment.Center;
            MentionAssist.SetMode(input, "tag");
            panel.Children.Add(input);
            Child = panel;
            MouseLeftButtonDown += (s, e) =>
            {
                input.Focus();
                e.Handled = true;
            };
            input.TextChanged += Input_TextChanged;
            input.PreviewKeyDown += Input_PreviewKeyDown;
            input.LostKeyboardFocus += (s, e) => Commit(input.Text);
        }

        /// <summary>Etiketler metni.</summary>
        public string Tags
        {
            get { return (string)GetValue(TagsProperty); }
            set { SetValue(TagsProperty, value); }
        }

        /// <summary>Kutu boşken görünen ipucu.</summary>
        public string Placeholder
        {
            get { return (string)GetValue(PlaceholderProperty); }
            set { SetValue(PlaceholderProperty, value); }
        }

        /// <summary>Yazma yeri (testler için).</summary>
        public TextBox Input
        {
            get { return input; }
        }

        /// <summary>Gösterilen rozet sayısı (testler için).</summary>
        public int ChipCount
        {
            get { return panel.Children.Count - 1; }
        }

        // Öneri eklenince metin "#ad " olur: boşlukla biten tam etiketler rozete döner.
        private void Input_TextChanged(object sender, TextChangedEventArgs e)
        {
            var text = input.Text;
            if (text.Length > 0 && char.IsWhiteSpace(text[text.Length - 1]) && !MentionAssist.IsOpen(input))
            {
                Commit(text);
            }
        }

        private void Input_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (MentionAssist.IsOpen(input))
            {
                return;
            }

            if (e.Key == Key.Enter && input.Text.Trim().Length > 0)
            {
                Commit(input.Text);
                e.Handled = true;
            }
            else if (e.Key == Key.Back && input.Text.Length == 0)
            {
                var tags = MarkdownEditing.ParseTags(Tags);
                if (tags.Count > 0)
                {
                    tags.RemoveAt(tags.Count - 1);
                    Write(tags);
                }

                e.Handled = true;
            }
        }

        // Yazılanları (bir ya da birkaç etiket) rozete çevirir.
        private void Commit(string text)
        {
            var added = MarkdownEditing.ParseTags(text);
            if (added.Count == 0)
            {
                if (text.Length > 0 && text.Trim().Length == 0)
                {
                    input.Text = string.Empty;
                }

                return;
            }

            var tags = MarkdownEditing.ParseTags(Tags);
            foreach (var tag in added.Where(a => !tags.Contains(a, StringComparer.OrdinalIgnoreCase)))
            {
                tags.Add(tag);
            }

            input.Text = string.Empty;
            Write(tags);
        }

        private void Write(System.Collections.Generic.List<string> tags)
        {
            writing = true;
            try
            {
                Tags = MarkdownEditing.FormatTags(tags);
                GetBindingExpression(TagsProperty)?.UpdateSource();
            }
            finally
            {
                writing = false;
            }

            ShowTags();
        }

        private void ShowTags()
        {
            while (panel.Children.Count > 1)
            {
                panel.Children.RemoveAt(0);
            }

            var tags = MarkdownEditing.ParseTags(Tags);
            for (var i = 0; i < tags.Count; i++)
            {
                panel.Children.Insert(i, Chip(tags[i]));
            }

            if (!writing && !input.IsKeyboardFocused)
            {
                input.Text = string.Empty;
            }
        }

        // Renkli rozet: "#ad" ve × düğmesi.
        private UIElement Chip(string tag)
        {
            var text = new TextBlock { Text = "#" + tag, VerticalAlignment = VerticalAlignment.Center };
            text.SetResourceReference(TextBlock.ForegroundProperty, "AccentTextFillColorPrimaryBrush");
            var remove = new Button { Content = new FontIcon { Glyph = "", FontSize = 9 }, Padding = new Thickness(3), Margin = new Thickness(4, 0, 0, 0), MinWidth = 0, MinHeight = 0, Focusable = false, Cursor = Cursors.Hand };
            remove.SetResourceReference(StyleProperty, "SubtleButtonStyle");
            remove.Click += (s, e) =>
            {
                var tags = MarkdownEditing.ParseTags(Tags).Where(t => !string.Equals(t, tag, StringComparison.OrdinalIgnoreCase)).ToList();
                Write(tags);
            };
            var row = new StackPanel { Orientation = Orientation.Horizontal };
            row.Children.Add(text);
            row.Children.Add(remove);
            var chip = new Border { Child = row, Margin = new Thickness(0, 2, 6, 2), Padding = new Thickness(8, 1, 2, 1), BorderThickness = new Thickness(1), Cursor = Cursors.Arrow };
            chip.SetResourceReference(Border.BackgroundProperty, "SubtleFillColorSecondaryBrush");
            chip.SetResourceReference(Border.BorderBrushProperty, "AccentBrush");
            chip.SetResourceReference(Border.CornerRadiusProperty, "ControlCornerRadius");
            return chip;
        }
    }
}
