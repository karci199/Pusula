using System;
using System.Globalization;
using System.Windows.Data;

namespace Pusula.Wpf
{
    /// <summary>true → false, false → true. Açılır kutunun düğmesi, kutu açıkken tıklanamaz olsun diye (ikinci tık kapatır, yeniden açmaz).</summary>
    public sealed class InverseBooleanConverter : IValueConverter
    {
        /// <summary>Değeri tersine çevirir.</summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(value is bool b && b);
        }

        /// <summary>Değeri tersine çevirir.</summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(value is bool b && b);
        }
    }
}
