using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Pusula.Wpf
{
    /// <summary>Sayıdan görünürlük: sıfırdan büyükse görünür; <see cref="WhenZero"/> ise tersi (boş liste yazısı için).</summary>
    public sealed class CountToVisibilityConverter : IValueConverter
    {
        /// <summary>Sıfırken görünür olsun mu (boş liste ipucu).</summary>
        public bool WhenZero { get; set; }

        /// <summary>Sayıyı görünürlüğe çevirir.</summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var count = value is int n ? n : 0;
            return (count > 0) != WhenZero ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>Geri çevrilmez.</summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
