using Pusula.Wpf.Properties;

namespace Pusula.Wpf.ViewModels
{
    /// <summary>Olay çeşidinin görünen adı (<c>toplanti</c> → "Toplantı"); bilinmeyen çeşit baş harfi büyütülerek gösterilir.</summary>
    public static class EventKinds
    {
        /// <summary>Önem etiketinin rengi: Yüksek kırmızı, diğerleri nötr.</summary>
        public static string Tone(string importance)
        {
            return string.Equals(importance, Resources.Importance_High, System.StringComparison.OrdinalIgnoreCase)
                ? Pusula.Core.Types.StatusTone.Failed
                : Pusula.Core.Types.StatusTone.Neutral;
        }

        /// <summary>Günlük penceresindeki tür listesi.</summary>
        public static string[] JournalKinds
        {
            get
            {
                return new[]
                {
                    Resources.Event_Meeting, Resources.Event_Decision, Resources.Event_Delivery, Resources.Event_Sample,
                    Resources.Event_Shipment, Resources.Event_Revision, Resources.Event_Fault, Resources.QuickNote_Chip,
                };
            }
        }

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
                case "teslim":
                    return Resources.Event_Delivery;
                default:
                    return kind.Length == 0 ? kind : char.ToUpper(kind[0], new System.Globalization.CultureInfo("tr-TR")) + kind.Substring(1);
            }
        }
    }
}
