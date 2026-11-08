using Pusula.Wpf.Properties;

namespace Pusula.Wpf.ViewModels
{
    /// <summary>Olay çeşidinin görünen adı (<c>toplanti</c> → "Toplantı"); bilinmeyen çeşit baş harfi büyütülerek gösterilir.</summary>
    public static class EventKinds
    {
        /// <summary>Görünen ad.</summary>
        public static string Label(string kind)
        {
            switch (kind)
            {
                case "toplanti":
                    return Resources.Event_Meeting;
                case "karar":
                    return Resources.Event_Decision;
                case "numune":
                    return Resources.Event_Sample;
                case "sevkiyat":
                    return Resources.Event_Shipment;
                case "revizyon":
                    return Resources.Event_Revision;
                case "ariza":
                    return Resources.Event_Fault;
                case "not":
                    return Resources.QuickNote_Chip;
                default:
                    return kind.Length == 0 ? kind : char.ToUpper(kind[0], new System.Globalization.CultureInfo("tr-TR")) + kind.Substring(1);
            }
        }
    }
}
