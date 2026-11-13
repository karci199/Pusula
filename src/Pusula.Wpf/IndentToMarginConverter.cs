using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Pusula.Wpf
{
    /// <summary>
    /// Sayı (piksel) → sol kenar boşluğu (başlık panelinde düzeye göre girinti).
    /// </summary>
    public sealed class IndentToMarginConverter : IValueConverter
    {
        /// <inheritdoc />
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return new Thickness(value is double left ? left : 0, 2, 0, 2);
        }

        /// <inheritdoc />
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is Thickness thickness ? thickness.Left : 0d;
        }
    }
}
