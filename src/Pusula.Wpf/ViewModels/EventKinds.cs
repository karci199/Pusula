using System;
using System.Linq;
using Pusula.Core.Notes;
using Pusula.Core.Types;
using Pusula.Wpf.Properties;

namespace Pusula.Wpf.ViewModels
{
    /// <summary>Olay türü: anahtar, görünen ad, ikon ve renk (etiket tonu).</summary>
    public sealed class EventKindOption
    {
        /// <summary>Seçeneği kurar; anahtar addan türetilir ("Test başladı" → <c>test basladi</c>).</summary>
        public EventKindOption(string label, string glyph, string tone)
        {
            Label = label;
            Key = Journal.KindKey(label);
            Glyph = glyph;
            Tone = tone;
        }

        /// <summary>Anahtar (karşılaştırma için).</summary>
        public string Key { get; }

        /// <summary>Görünen ad.</summary>
        public string Label { get; }

        /// <summary>Segoe Fluent ikon.</summary>
        public string Glyph { get; }

        /// <summary>Etiket tonu (<see cref="StatusTone"/>).</summary>
        public string Tone { get; }

        /// <summary>Görünen ad.</summary>
        public override string ToString()
        {
            return Label;
        }
    }

    /// <summary>Olay türleri: görünen ad, ikon, renk. Bilinmeyen tür baş harfi büyütülerek ve nötr renkte gösterilir.</summary>
    public static class EventKinds
    {
        private static EventKindOption[]? all;

        /// <summary>Bütün türler ("+ Olay" listesindeki sırayla).</summary>
        public static EventKindOption[] All
        {
            get
            {
                return all ?? (all = new[]
                {
                    new EventKindOption(Resources.Event_Meeting, "", StatusTone.Active),
                    new EventKindOption(Resources.Event_Decision, "", StatusTone.Waiting),
                    new EventKindOption(Resources.Event_TestStart, "", StatusTone.Active),
                    new EventKindOption(Resources.Event_TestEnd, "", StatusTone.Done),
                    new EventKindOption(Resources.Event_Delivery, "", StatusTone.Neutral),
                    new EventKindOption(Resources.Event_ToProduction, "", StatusTone.Done),
                    new EventKindOption(Resources.Event_Repair, "", StatusTone.Waiting),
                    new EventKindOption(Resources.Event_Fault, "", StatusTone.Failed),
                    new EventKindOption(Resources.Event_Revision, "", StatusTone.Waiting),
                    new EventKindOption(Resources.Event_Sample, "", StatusTone.Neutral),
                    new EventKindOption(Resources.Event_Shipment, "", StatusTone.Neutral),
                    new EventKindOption(Resources.QuickNote_Chip, "", StatusTone.Neutral),
                });
            }
        }

        /// <summary>Anahtarı ya da adı verilen tür; tanınmıyorsa <c>null</c>.</summary>
        public static EventKindOption? Find(string kind)
        {
            var key = Journal.KindKey(kind);
            if (key == "teslim")
            {
                key = Journal.KindKey(Resources.Event_Delivery);
            }

            return All.FirstOrDefault(o => o.Key == key);
        }

        /// <summary>Revizyon türü mü (çizelgede Revizyon süzgecine girer).</summary>
        public static bool IsRevision(string kind)
        {
            return Journal.KindKey(kind) == Journal.KindKey(Resources.Event_Revision);
        }

        /// <summary>Görünen ad.</summary>
        public static string Label(string kind)
        {
            var known = Find(kind);
            if (known != null)
            {
                return known.Label;
            }

            return kind.Length == 0 ? kind : char.ToUpper(kind[0], new System.Globalization.CultureInfo("tr-TR")) + kind.Substring(1);
        }

        /// <summary>Etiket tonu: önemi Yüksek olan kırmızı; değilse türün rengi.</summary>
        public static string ToneFor(string kind, string importance)
        {
            if (string.Equals(importance, Resources.Importance_High, StringComparison.OrdinalIgnoreCase))
            {
                return StatusTone.Failed;
            }

            return Find(kind)?.Tone ?? StatusTone.Neutral;
        }
    }
}
