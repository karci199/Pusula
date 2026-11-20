using System;
using System.Collections.Generic;
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

    /// <summary>
    /// Olay (işlem) türleri: görünen ad, ikon, renk. Hazır türlerin ardından kullanıcının eklediği türler gelir
    /// (<see cref="SetCustom"/>, kökte <c>_TAKIP\Islemler.md</c>). Bilinmeyen tür baş harfi büyütülerek ve nötr renkte gösterilir.
    /// </summary>
    public static class EventKinds
    {
        /// <summary>Yeni türde seçilebilen ikonlar.</summary>
        public static readonly string[] Glyphs = { "", "", "", "", "", "", "", "", "", "", "", "" };

        /// <summary>Yeni türde seçilebilen renk tonları.</summary>
        public static readonly string[] Tones = { StatusTone.Active, StatusTone.Done, StatusTone.Waiting, StatusTone.Failed, StatusTone.Neutral, Purple, Orange };

        /// <summary>Mor ton (toplantı, karar); yalnız işlem türlerinde.</summary>
        public const string Purple = "purple";

        /// <summary>Turuncu ton (hazırlık); yalnız işlem türlerinde.</summary>
        public const string Orange = "orange";

        private static EventKindOption[]? all;
        private static EventKindOption[]? builtIn;
        private static EventKindOption[] custom = new EventKindOption[0];

        /// <summary>Bütün türler: hazırlar ("+ Kayıt" ızgarasındaki sırayla) ve eklenenler.</summary>
        public static EventKindOption[] All
        {
            get { return all ?? (all = BuiltIn().Concat(custom).ToArray()); }
        }

        /// <summary>Kullanıcının eklediği türleri ayarlar (hazır türlerle aynı adda olanlar atlanır).</summary>
        public static void SetCustom(IEnumerable<CustomEventKind> kinds)
        {
            var known = BuiltIn();
            custom = (kinds ?? Enumerable.Empty<CustomEventKind>())
                .Where(k => k.Name.Trim().Length > 0 && known.All(b => b.Key != Journal.KindKey(k.Name)))
                .Select(k => new EventKindOption(k.Name.Trim(), GlyphFromCode(k.Glyph), Tones.Contains(k.Tone) ? k.Tone : StatusTone.Neutral))
                .ToArray();
            all = null;
        }

        /// <summary>İkon karakterinin kodu (<c>E8A5</c>); dosyaya bu yazılır.</summary>
        public static string GlyphCode(string glyph)
        {
            return string.IsNullOrEmpty(glyph) ? string.Empty : ((int)glyph[0]).ToString("X4", System.Globalization.CultureInfo.InvariantCulture);
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

        /// <summary>Türün rengi (önemden bağımsız; önem kartta ayrı şerit ve rozetle gösterilir).</summary>
        public static string ToneFor(string kind, string importance)
        {
            return Find(kind)?.Tone ?? StatusTone.Neutral;
        }

        private static EventKindOption[]? importances;

        /// <summary>Önem seçenekleri, ikon ve renkle: Düşük (gri, aşağı), Normal (mavi, çizgi), Önemli (sarı, ünlem), Kritik (kırmızı, uyarı).</summary>
        public static EventKindOption[] Importances
        {
            get
            {
                return importances ?? (importances = new[]
                {
                    new EventKindOption(Resources.Importance_Low, "", StatusTone.Neutral),
                    new EventKindOption(Resources.Importance_Normal, "", StatusTone.Active),
                    new EventKindOption(Resources.Importance_High, "", StatusTone.Waiting),
                    new EventKindOption(Resources.Importance_Critical, "", StatusTone.Failed),
                });
            }
        }

        /// <summary>Önemin ikonu (Önemli ünlem, Kritik uyarı); bilinmeyende boş.</summary>
        public static string ImportanceGlyph(int rank)
        {
            return rank == 2 ? Importances[3].Glyph : rank == 1 ? Importances[2].Glyph : string.Empty;
        }

        /// <summary>Önem düzeyi: 0 normal, 1 önemli (eski "Yüksek" de), 2 kritik.</summary>
        public static int ImportanceRank(string importance)
        {
            var value = (importance ?? string.Empty).Trim();
            if (string.Equals(value, Resources.Importance_Critical, StringComparison.CurrentCultureIgnoreCase))
            {
                return 2;
            }

            return string.Equals(value, Resources.Importance_High, StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(value, Resources.Importance_Legacy, StringComparison.CurrentCultureIgnoreCase) ? 1 : 0;
        }

        private static EventKindOption[] BuiltIn()
        {
            return builtIn ?? (builtIn = new[]
            {
                new EventKindOption(Resources.Event_Preparation, "", Orange),
                new EventKindOption(Resources.Event_Measurement, "\uE9D9", StatusTone.Active),
                new EventKindOption(Resources.Event_TestStart, "", StatusTone.Active),
                new EventKindOption(Resources.Event_TestEnd, "", StatusTone.Done),
                new EventKindOption(Resources.Event_Meeting, "", Purple),
                new EventKindOption(Resources.Event_Decision, "", Purple),
                new EventKindOption(Resources.Event_Issue, "", StatusTone.Failed),
                new EventKindOption(Resources.Event_Solution, "", StatusTone.Done),
                new EventKindOption(Resources.Event_Delivery, "", StatusTone.Neutral),
                new EventKindOption(Resources.QuickNote_Chip, "", StatusTone.Neutral),
                new EventKindOption(Resources.Event_ToProduction, "", StatusTone.Done),
                new EventKindOption(Resources.Event_Repair, "", StatusTone.Waiting),
                new EventKindOption(Resources.Event_Fault, "", StatusTone.Failed),
                new EventKindOption(Resources.Event_Revision, "", StatusTone.Waiting),
                new EventKindOption(Resources.Event_Sample, "", StatusTone.Neutral),
                new EventKindOption(Resources.Event_Shipment, "", StatusTone.Neutral),
            });
        }

        private static string GlyphFromCode(string code)
        {
            int value;
            return int.TryParse((code ?? string.Empty).Trim(), System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out value) && value > 0
                ? ((char)value).ToString() : Glyphs[0];
        }
    }
}
