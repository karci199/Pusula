using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Pusula.Wpf
{
    /// <summary>
    /// <c>true</c> → Collapsed, <c>false</c> → Visible ("boş" ipuçlarını göstermek için).
    /// </summary>
    public sealed class InverseBooleanToVisibilityConverter : IValueConverter
    {
        /// <inheritdoc />
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool flag && flag ? Visibility.Collapsed : Visibility.Visible;
        }

        /// <inheritdoc />
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is Visibility visibility && visibility != Visibility.Visible;
        }
    }
}
