using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Threading;
using Pusula.Core.Markdown;
using Pusula.Wpf.ViewModels;

namespace Pusula.Wpf
{
    /// <summary>
    /// Herhangi bir metin kutusuna @ / [[ önerisi takar (WhatsApp'taki gibi): <c>@</c> ya da <c>[[</c> yazınca kayıtlı jig,
    /// kablo, yazılım, stok, proje ve kişiler listelenir; ↑ ↓ ile seçilir, Enter / Tab ile eklenir, Esc kapatır.
    /// <c>Mode="link"</c> md'ye giden kutularda <c>[[NUMARA]]</c> / <c>@Kişi</c> ekler; <c>Mode="plain"</c> (kısa notlar)
    /// yalnız numarayı ya da kişi adını ekler. Öneri kaynağı <see cref="Provider"/> (ana pencere verir).
    /// </summary>
    public static class MentionAssist
    {
        /// <summary>Ekleme biçimi: <c>link</c> ya da <c>plain</c>; boşsa kapalı.</summary>
        public static readonly DependencyProperty ModeProperty = DependencyProperty.RegisterAttached(
            "Mode", typeof(string), typeof(MentionAssist), new PropertyMetadata(string.Empty, OnModeChanged));

        private static readonly DependencyProperty StateProperty = DependencyProperty.RegisterAttached(
            "State", typeof(AssistState), typeof(MentionAssist), new PropertyMetadata(null));

        /// <summary>Bütün öneriler (ana pencere kurar); yoksa öneri açılmaz.</summary>
        public static Func<IEnumerable<NoteSuggestion>>? Provider { get; set; }

        /// <summary>Ekleme biçimini okur.</summary>
        public static string GetMode(DependencyObject element)
        {
            return (string)element.GetValue(ModeProperty);
        }

        /// <summary>Ekleme biçimini yazar.</summary>
        public static void SetMode(DependencyObject element, string value)
        {
            element.SetValue(ModeProperty, value);
        }

        /// <summary>Kutunun öneri listesi açık mı (testler için).</summary>
        public static bool IsOpen(TextBox box)
        {
            var state = (AssistState?)box.GetValue(StateProperty);
            return state != null && state.Popup.IsOpen;
        }

        /// <summary>Kutuda listelenen öneriler (testler için).</summary>
        public static IList<NoteSuggestion> Items(TextBox box)
        {
            var state = (AssistState?)box.GetValue(StateProperty);
            return state?.List.ItemsSource as IList<NoteSuggestion> ?? new List<NoteSuggestion>();
        }

        /// <summary>Kutunun açılır listesi (testler için: görüntü almak); takılı değilse <c>null</c>.</summary>
        public static Popup? PopupOf(TextBox box)
        {
            return ((AssistState?)box.GetValue(StateProperty))?.Popup;
        }

        /// <summary>İmleçteki sorguya göre listeyi hemen günceller (testler için; normalde yazdıkça kendiliğinden).</summary>
        public static void Update(TextBox box)
        {
            var state = (AssistState?)box.GetValue(StateProperty);
            state?.Update();
        }

        /// <summary>Seçili öneriyi ekler (testler için; normalde Enter / Tab / tıklama). Eklediyse <c>true</c>.</summary>
        public static bool Accept(TextBox box)
        {
            var state = (AssistState?)box.GetValue(StateProperty);
            return state != null && state.Accept();
        }

        private static void OnModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var box = d as TextBox;
            if (box == null || box.GetValue(StateProperty) != null || string.IsNullOrEmpty(e.NewValue as string))
            {
                return;
            }

            box.SetValue(StateProperty, new AssistState(box));
        }

        // Bir kutunun öneri durumu: açılır liste, sorgu, o açılıştaki öneriler.
        private sealed class AssistState
        {
            private const string ItemTemplateXaml =
                "<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:c='clr-namespace:DesignKit.Controls;assembly=DesignKit.Wpf'>" +
                "<Grid Margin='2,3'><Grid.ColumnDefinitions><ColumnDefinition Width='24'/><ColumnDefinition Width='Auto'/><ColumnDefinition Width='*'/><ColumnDefinition Width='Auto'/></Grid.ColumnDefinitions>" +
                "<c:FontIcon Glyph='{Binding Glyph}' FontSize='14' Foreground='{DynamicResource TextSecondaryBrush}' VerticalAlignment='Center'/>" +
                "<TextBlock Grid.Column='1' FontWeight='SemiBold' Text='{Binding Key}' VerticalAlignment='Center'/>" +
                "<TextBlock Grid.Column='2' Margin='10,0,0,0' Foreground='{DynamicResource TextSecondaryBrush}' TextTrimming='CharacterEllipsis' Text='{Binding Title}' VerticalAlignment='Center'/>" +
                "<TextBlock Grid.Column='3' Margin='10,0,4,0' Style='{DynamicResource CaptionTextBlockStyle}' Foreground='{DynamicResource TextTertiaryBrush}' Text='{Binding Kind}' VerticalAlignment='Center'/>" +
                "</Grid></DataTemplate>";

            private static DataTemplate? itemTemplate;
            private readonly TextBox box;
            private SuggestQuery? query;
            private List<NoteSuggestion>? all;

            public AssistState(TextBox box)
            {
                this.box = box;
                List = new ListBox
                {
                    BorderThickness = new Thickness(0),
                    Background = System.Windows.Media.Brushes.Transparent,
                    Focusable = false,
                    ItemTemplate = itemTemplate ?? (itemTemplate = (DataTemplate)XamlReader.Parse(ItemTemplateXaml)),
                };
                ScrollViewer.SetHorizontalScrollBarVisibility(List, ScrollBarVisibility.Disabled);
                List.PreviewMouseLeftButtonUp += (s, e) =>
                {
                    if ((e.OriginalSource as FrameworkElement)?.DataContext is NoteSuggestion item)
                    {
                        List.SelectedItem = item;
                        Accept();
                        e.Handled = true;
                    }
                };

                var border = new Border { BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), Padding = new Thickness(4), MinWidth = 340, MaxHeight = 320, Child = List };
                border.SetResourceReference(Border.BackgroundProperty, "SolidBackgroundFillColorTertiaryBrush");
                border.SetResourceReference(Border.BorderBrushProperty, "CardStrokeColorDefaultBrush");
                Popup = new Popup
                {
                    PlacementTarget = box,
                    Placement = PlacementMode.Relative,
                    AllowsTransparency = true,
                    StaysOpen = true,
                    Child = border,
                };

                box.TextChanged += (s, e) => box.Dispatcher.BeginInvoke(new Action(Update), DispatcherPriority.Input);
                box.PreviewKeyDown += Box_PreviewKeyDown;
                box.LostKeyboardFocus += (s, e) => Close();
                box.IsVisibleChanged += (s, e) =>
                {
                    if (!box.IsVisible)
                    {
                        Close();
                    }
                };
            }

            public Popup Popup { get; }

            public ListBox List { get; }

            public void Update()
            {
                // Kaynak: kutunun penceresindeki ana görünüm modeli (birden çok pencerede doğru kök); yoksa genel kaynak.
                // Mode="tag": # ile etiket önerisi (açık pencerenin türü ve alt türünde geçerli olanlar).
                var main = Window.GetWindow(box)?.DataContext as MainViewModel;
                // Mode="person": # ya da @ ile kişi önerisi (Talep eden gibi); seçilince yalnız ad yazılır.
                var isTag = string.Equals(GetMode(box), "tag", StringComparison.OrdinalIgnoreCase);
                var isPerson = string.Equals(GetMode(box), "person", StringComparison.OrdinalIgnoreCase);
                Func<IEnumerable<NoteSuggestion>>? source = main == null ? (isTag || isPerson ? null : Provider)
                    : isTag ? main.TagSuggestions : isPerson ? main.PersonSuggestions : (Func<IEnumerable<NoteSuggestion>>)main.NoteSuggestions;
                var found = box.SelectionLength == 0
                    ? (isTag ? MarkdownEditing.FindTagQuery(box.Text, box.CaretIndex)
                        : isPerson ? MarkdownEditing.FindTagQuery(box.Text, box.CaretIndex) ?? MarkdownEditing.FindQuery(box.Text, box.CaretIndex)
                        : MarkdownEditing.FindQuery(box.Text, box.CaretIndex))
                    : null;
                if (found == null || source == null)
                {
                    Close();
                    return;
                }

                // Öneriler açılışta bir kez toplanır; liste kapanana dek aynı küme süzülür.
                if (all == null)
                {
                    all = source().ToList();
                }

                var items = NoteSuggestion.Filter(all, found.Text);
                if (items.Count == 0)
                {
                    Close();
                    return;
                }

                query = found;
                List.ItemsSource = items;
                List.SelectedIndex = 0;
                var rect = box.GetRectFromCharacterIndex(Math.Min(box.CaretIndex, box.Text.Length));
                Popup.HorizontalOffset = rect.IsEmpty ? 0 : rect.Left;
                Popup.VerticalOffset = rect.IsEmpty ? box.ActualHeight : rect.Bottom + 4;
                Popup.IsOpen = true;
            }

            public bool Accept()
            {
                var item = List.SelectedItem as NoteSuggestion;
                if (item == null || query == null)
                {
                    return false;
                }

                var insert = string.Equals(GetMode(box), "plain", StringComparison.OrdinalIgnoreCase) || string.Equals(GetMode(box), "person", StringComparison.OrdinalIgnoreCase) ? item.Key : item.Insert;
                var result = MarkdownEditing.ApplySuggestion(box.Text, box.CaretIndex, query, insert);
                var at = result.SelectionStart;
                if (at >= result.Text.Length || !char.IsWhiteSpace(result.Text[at]))
                {
                    // Yazmaya devam edilebilsin diye arkasına bir boşluk.
                    result = new EditResult(result.Text.Insert(at, " "), at + 1, 0);
                }

                Close();

                // Yalnız değişen parça değişir (Ctrl+Z geçmişi korunur).
                var before = box.Text;
                var after = result.Text;
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

                box.Select(prefix, before.Length - prefix - suffix);
                box.SelectedText = after.Substring(prefix, after.Length - prefix - suffix);
                box.Select(Math.Min(result.SelectionStart, box.Text.Length), 0);
                return true;
            }

            private void Close()
            {
                query = null;
                all = null;
                Popup.IsOpen = false;
            }

            private void Box_PreviewKeyDown(object sender, KeyEventArgs e)
            {
                if (!Popup.IsOpen)
                {
                    return;
                }

                switch (e.Key)
                {
                    case Key.Down:
                        List.SelectedIndex = Math.Min(List.Items.Count - 1, List.SelectedIndex + 1);
                        List.ScrollIntoView(List.SelectedItem);
                        e.Handled = true;
                        break;
                    case Key.Up:
                        List.SelectedIndex = Math.Max(0, List.SelectedIndex - 1);
                        List.ScrollIntoView(List.SelectedItem);
                        e.Handled = true;
                        break;
                    case Key.Enter:
                    case Key.Tab:
                        e.Handled = Accept();
                        break;
                    case Key.Escape:
                        Close();
                        e.Handled = true;
                        break;
                }
            }
        }
    }
}
