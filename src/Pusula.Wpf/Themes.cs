using System;
using System.Windows;
using System.Windows.Media;
using Pusula.Wpf.Properties;

namespace Pusula.Wpf
{
    /// <summary>
    /// Bir temanın tohumu: zemin, vurgu ve metin rengi. Kalan renk token'ları (kart, kontrol, kenarlık, ikincil metin …)
    /// bunlardan türetilir (KIWI'deki temalarla aynı).
    /// </summary>
    public sealed class Palette
    {
        /// <summary>Paleti kurar; renkler <c>#RRGGBB</c> biçiminde.</summary>
        public Palette(string key, bool dark, string background, string accent, string text)
        {
            Key = key;
            Dark = dark;
            Background = (Color)ColorConverter.ConvertFromString(background);
            Accent = (Color)ColorConverter.ConvertFromString(accent);
            Text = (Color)ColorConverter.ConvertFromString(text);
        }

        /// <summary>Ayarlarda saklanan kalıcı anahtar (ör. <c>FluentDark</c>).</summary>
        public string Key { get; }

        /// <summary>Görünen ad (<c>Theme_</c> + anahtar kaynağından).</summary>
        public string Name
        {
            get
            {
                var name = Resources.ResourceManager.GetString("Theme_" + Key, Resources.Culture);
                return string.IsNullOrEmpty(name) ? Key : name;
            }
        }

        /// <summary>Koyu tema mı.</summary>
        public bool Dark { get; }

        /// <summary>Zemin rengi.</summary>
        public Color Background { get; }

        /// <summary>Vurgu rengi.</summary>
        public Color Accent { get; }

        /// <summary>Metin rengi.</summary>
        public Color Text { get; }

        /// <summary>Görünen ad.</summary>
        public override string ToString()
        {
            return Name;
        }
    }

    /// <summary>DesignKit'in renk token'larını bir <see cref="Palette"/>'ten üretir ve çalışırken temayı değiştirir.</summary>
    public static class Themes
    {
        private const string Root = "pack://application:,,,/DesignKit.Wpf;component/";

        private static ResourceDictionary? current;

        /// <summary>Varsayılan tema anahtarı.</summary>
        public const string DefaultKey = "FluentDark";

        /// <summary>Şu an uygulanan tema (henüz uygulanmadıysa <c>null</c>).</summary>
        public static Palette? Current { get; private set; }

        /// <summary>Bütün temalar. Yeni tema sona eklenir.</summary>
        public static readonly Palette[] All =
        {
            new Palette("FluentLight", false, "#F3F3F3", "#005FB8", "#1A1A1A"),
            new Palette("FluentDark", true, "#202020", "#60CDFF", "#FFFFFF"),
            new Palette("Night", true, "#18202B", "#5AB4FF", "#E8EEF5"),
            new Palette("Amber", true, "#1F1D1B", "#F0A33E", "#F2EEE9"),
            new Palette("Forest", true, "#18211C", "#4CC38A", "#E6F0EA"),
            new Palette("Plum", true, "#1F1A26", "#B392F0", "#EEE9F5"),
            new Palette("Glacier", false, "#EDF2F7", "#1E6FB8", "#17212B"),
            new Palette("Sand", false, "#F4F0E8", "#A85B18", "#2A2420"),
            new Palette("Mint", false, "#EEF5F1", "#157F5B", "#18241E"),
        };

        /// <summary>Anahtarı verilen temayı bulur; yoksa varsayılanı döner.</summary>
        public static Palette Find(string? key)
        {
            foreach (var palette in All)
            {
                if (string.Equals(palette.Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    return palette;
                }
            }

            return All[1];
        }

        /// <summary>Temayı uygular: önceki tema sözlüğü çıkarılır, yenisi eklenir; <c>DynamicResource</c>'lar yeniden çözülür.</summary>
        public static void Apply(Palette palette)
        {
            var dicts = Application.Current.Resources.MergedDictionaries;
            for (var i = dicts.Count - 1; i >= 0; i--)
            {
                var source = dicts[i].Source != null ? dicts[i].Source.OriginalString : string.Empty;
                if (ReferenceEquals(dicts[i], current) || source.IndexOf("/Themes/", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    dicts.RemoveAt(i);
                }
            }

            current = Build(palette);
            dicts.Add(current);
            Current = palette;
        }

        private static Color Mix(Color a, Color b, double t)
        {
            return Color.FromArgb(
                255,
                (byte)Math.Round(a.R + (b.R - a.R) * t),
                (byte)Math.Round(a.G + (b.G - a.G) * t),
                (byte)Math.Round(a.B + (b.B - a.B) * t));
        }

        private static double Luminance(Color c)
        {
            return (0.2126 * c.R + 0.7152 * c.G + 0.0722 * c.B) / 255;
        }

        // Paletten tam bir DesignKit tema sözlüğü: Type, Spacing, Motion, Elevation + üretilmiş renkler + Semantic fırçalar.
        private static ResourceDictionary Build(Palette p)
        {
            var theme = new ResourceDictionary();
            foreach (var axis in new[] { "Type", "Spacing", "Motion", "Elevation" })
            {
                theme.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri(Root + "Themes/Fluent/Fluent." + axis + ".xaml") });
            }

            var colors = new ResourceDictionary { Source = new Uri(Root + "Themes/Fluent/Fluent.Colors." + (p.Dark ? "Dark" : "Light") + ".xaml") };
            var white = Colors.White;
            var black = Colors.Black;
            var bg = p.Background;
            var text = p.Text;

            void Set(string key, Color value)
            {
                colors[key] = value;
            }

            if (p.Dark)
            {
                Set("SolidBackgroundFillColorBase", bg);
                Set("SolidBackgroundFillColorSecondary", Mix(bg, black, 0.22));
                Set("SolidBackgroundFillColorTertiary", Mix(bg, white, 0.05));
                Set("SolidBackgroundFillColorQuarternary", Mix(bg, white, 0.08));
                Set("SolidBackgroundFillColorBaseAlt", Mix(bg, black, 0.35));
                Set("LayerFillColorDefault", Mix(bg, white, 0.05));
                Set("LayerFillColorAlt", Mix(bg, white, 0.08));
                Set("CardBackgroundFillColorDefault", Mix(bg, white, 0.07));
                Set("CardBackgroundFillColorSecondary", Mix(bg, white, 0.10));
                Set("ControlFillColorDefault", Mix(bg, white, 0.10));
                Set("ControlFillColorSecondary", Mix(bg, white, 0.14));
                Set("ControlFillColorTertiary", Mix(bg, white, 0.07));
                Set("ControlFillColorInputActive", Mix(bg, black, 0.30));
            }
            else
            {
                Set("SolidBackgroundFillColorBase", bg);
                Set("SolidBackgroundFillColorSecondary", Mix(bg, black, 0.04));
                Set("SolidBackgroundFillColorTertiary", Mix(bg, white, 0.55));
                Set("SolidBackgroundFillColorQuarternary", Mix(bg, black, 0.07));
                Set("SolidBackgroundFillColorBaseAlt", Mix(bg, black, 0.06));
                Set("LayerFillColorDefault", Mix(bg, white, 0.80));
                Set("LayerFillColorAlt", Mix(bg, white, 0.95));
                Set("CardBackgroundFillColorDefault", Mix(bg, white, 0.85));
                Set("CardBackgroundFillColorSecondary", Mix(bg, white, 0.55));
                Set("ControlFillColorDefault", Mix(bg, white, 0.92));
                Set("ControlFillColorSecondary", Mix(bg, white, 0.60));
                Set("ControlFillColorTertiary", Mix(bg, white, 0.40));
                Set("ControlFillColorInputActive", white);
            }

            Set("ControlStrokeColorDefault", Mix(bg, text, 0.16));
            Set("ControlStrokeColorSecondary", Mix(bg, text, 0.10));
            Set("ControlStrongStrokeColorDefault", Mix(bg, text, 0.55));
            Set("CardStrokeColorDefault", Mix(bg, text, p.Dark ? 0.12 : 0.10));
            Set("DividerStrokeColorDefault", Mix(bg, text, 0.12));
            Set("SurfaceStrokeColorDefault", Mix(bg, text, 0.20));
            Set("TextFillColorPrimary", text);
            Set("TextFillColorSecondary", Mix(text, bg, 0.28));
            Set("TextFillColorTertiary", Mix(text, bg, 0.48));
            Set("TextFillColorDisabled", Mix(text, bg, 0.64));

            var accent = p.Accent;
            Set("SystemAccentColor", accent);

            // Üzerine gelme ve seçili satır rengi vurguya doğru boyalı (kitin varsayılanı neredeyse görünmüyor).
            Set("SubtleFillColorSecondary", Mix(Mix(bg, white, p.Dark ? 0.04 : 0.55), accent, p.Dark ? 0.26 : 0.18));
            Set("SubtleFillColorTertiary", Mix(Mix(bg, white, p.Dark ? 0.04 : 0.55), accent, p.Dark ? 0.36 : 0.28));
            Set("AccentFillColorDefault", accent);
            Set("AccentFillColorSecondary", Mix(accent, bg, 0.12));
            Set("AccentFillColorTertiary", Mix(accent, bg, 0.26));
            Set("AccentTextFillColorPrimary", p.Dark ? Mix(accent, white, 0.25) : Mix(accent, black, 0.18));
            Set("TextOnAccentFillColorPrimary", Luminance(accent) > 0.58 ? black : white);

            theme.MergedDictionaries.Add(colors);
            theme.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri(Root + "Contract/Semantic.xaml") });

            // Proje simgesi: açık temada koyu sarı, koyu temada açık sarı.
            theme["Pusula.ProjectIconBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(p.Dark ? "#E8B04B" : "#B07A12"));
            return theme;
        }
    }
}
