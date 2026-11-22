using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using DesignKit.Controls;

namespace Pusula.Wpf
{
    /// <summary>
    /// Aramalı seçim kutusu: düğmede seçili değer (yoksa ipucu); açılınca yazarak süzülen liste. Listede olmayan yazı
    /// "+ Ekle" ile seçilebilir (<see cref="AllowNew"/>); "Temizle" seçimi boşaltır. Enter ilk eşleşeni (yoksa yeniyi) seçer,
    /// Esc kapatır.
    /// </summary>
    public partial class SearchPickBox : UserControl
    {
        /// <summary>Seçenekler (metin).</summary>
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(SearchPickBox), new PropertyMetadata(null, OnItemsChanged));

        /// <summary>Seçili değer (iki yönlü).</summary>
        public static readonly DependencyProperty SelectedValueProperty =
            DependencyProperty.Register(nameof(SelectedValue), typeof(string), typeof(SearchPickBox),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedChanged));

        /// <summary>Boşken düğmede görünen ipucu.</summary>
        public static readonly DependencyProperty PlaceholderProperty =
            DependencyProperty.Register(nameof(Placeholder), typeof(string), typeof(SearchPickBox), new PropertyMetadata(string.Empty, OnSelectedChanged));

        /// <summary>Listede olmayan yazı seçilebilir mi.</summary>
        public static readonly DependencyProperty AllowNewProperty =
            DependencyProperty.Register(nameof(AllowNew), typeof(bool), typeof(SearchPickBox), new PropertyMetadata(true));

        /// <summary>Kutuyu kurar.</summary>
        public SearchPickBox()
        {
            InitializeComponent();
            TextBoxHelper.SetPlaceholderText(Search, Properties.Resources.SearchPick_Placeholder);
            ClearText.Text = Properties.Resources.SearchPick_Clear;
            RefreshShown();
        }

        /// <summary>Seçenekler (metin).</summary>
        public IEnumerable? ItemsSource
        {
            get { return (IEnumerable?)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        /// <summary>Seçili değer.</summary>
        public string SelectedValue
        {
            get { return (string)GetValue(SelectedValueProperty) ?? string.Empty; }
            set { SetValue(SelectedValueProperty, value); }
        }

        /// <summary>Boşken düğmede görünen ipucu.</summary>
        public string Placeholder
        {
            get { return (string)GetValue(PlaceholderProperty) ?? string.Empty; }
            set { SetValue(PlaceholderProperty, value); }
        }

        /// <summary>Listede olmayan yazı seçilebilir mi.</summary>
        public bool AllowNew
        {
            get { return (bool)GetValue(AllowNewProperty); }
            set { SetValue(AllowNewProperty, value); }
        }

        /// <summary>Açılır liste açık mı (test ve klavye için).</summary>
        public bool IsDropOpen
        {
            get { return DropPopup.IsOpen; }
            set { Button.IsChecked = value; }
        }

        /// <summary>Arama metni (test için).</summary>
        public string SearchText
        {
            get { return Search.Text; }
            set { Search.Text = value ?? string.Empty; }
        }

        /// <summary>Şu an listede görünen seçenekler (test için).</summary>
        public IList<string> ShownItems
        {
            get { return (List.ItemsSource as IEnumerable<string> ?? Enumerable.Empty<string>()).ToList(); }
        }

        /// <summary>Enter'ın yaptığını yapar: ilk eşleşeni, yoksa (izin varsa) yazılanı seçer.</summary>
        public void PickFirst()
        {
            var first = ShownItems.FirstOrDefault();
            if (first != null)
            {
                Choose(first);
            }
            else if (AllowNew && Search.Text.Trim().Length > 0)
            {
                Choose(Search.Text.Trim());
            }
        }

        private static void OnItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var box = (SearchPickBox)d;
            if (e.OldValue is INotifyCollectionChanged oldList)
            {
                oldList.CollectionChanged -= box.OnSourceChanged;
            }

            if (e.NewValue is INotifyCollectionChanged newList)
            {
                newList.CollectionChanged += box.OnSourceChanged;
            }

            box.Filter();
        }

        private void OnSourceChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            Filter();
        }

        private static void OnSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((SearchPickBox)d).RefreshShown();
        }

        private void RefreshShown()
        {
            var value = SelectedValue;
            ShownText.Text = value.Length > 0 ? value : Placeholder;
            ShownText.SetResourceReference(TextBlock.ForegroundProperty, value.Length > 0 ? "TextFillColorPrimaryBrush" : "TextTertiaryBrush");
            ClearButton.Visibility = value.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        // Liste: arama metnini numara ya da adın herhangi bir yerinde içerenler; "+ Ekle" yalnız birebir eşleşme yoksa.
        private void Filter()
        {
            var text = Search.Text.Trim();
            var all = (ItemsSource ?? Enumerable.Empty<object>()).Cast<object>().Select(o => Convert.ToString(o, CultureInfo.CurrentCulture) ?? string.Empty).Where(s => s.Length > 0).ToList();
            List.ItemsSource = text.Length == 0 ? all : all.Where(s => s.IndexOf(text, StringComparison.CurrentCultureIgnoreCase) >= 0).ToList();
            var exact = all.Any(s => string.Equals(s, text, StringComparison.CurrentCultureIgnoreCase));
            AddButton.Visibility = AllowNew && text.Length > 0 && !exact ? Visibility.Visible : Visibility.Collapsed;
            AddLabel.Text = string.Format(CultureInfo.CurrentCulture, Properties.Resources.SearchPick_Add, text);
        }

        private void Choose(string value)
        {
            SelectedValue = value;
            Button.IsChecked = false;
        }

        private void Drop_Opened(object? sender, EventArgs e)
        {
            DropBorder.Width = Math.Max(ActualWidth, 260);
            Search.Text = string.Empty;
            Filter();
            Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() => Keyboard.Focus(Search)));
        }

        private void Search_TextChanged(object sender, TextChangedEventArgs e)
        {
            Filter();
        }

        private void Search_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                PickFirst();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                Button.IsChecked = false;
                e.Handled = true;
            }
        }

        private void Item_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is string value)
            {
                Choose(value);
            }
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            Choose(Search.Text.Trim());
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            Choose(string.Empty);
        }
    }
}
