using System;
using System.Globalization;

namespace Pusula.Core.Tracking
{
    /// <summary>
    /// Projenin sol listedeki rengi: <c>proje.json</c>'da elle verilmişse o, yoksa proje kodundan türetilir.
    /// Rastgele değildir; aynı kod her bilgisayarda ve her açılışta aynı rengi alır.
    /// </summary>
    public static class ProjectColors
    {
        /// <summary>Hem koyu hem açık temada okunan orta tonlar.</summary>
        public static readonly string[] Palette =
        {
            "#E8B04B", // amber
            "#4A9FE0", // mavi
            "#5DB55A", // yeşil
            "#E0709A", // pembe
            "#9B7FE6", // mor
            "#E8794F", // turuncu
            "#3BB3A6", // camgöbeği
            "#B8935A", // toprak
        };

        /// <summary>
        /// Projenin rengi (<c>#RRGGBB</c>). <paramref name="stored"/> geçerli bir renkse o döner;
        /// değilse klasör adının kod kısmından (ilk <c>_</c>'e kadar) paletten bir renk seçilir.
        /// </summary>
        /// <param name="folderName">Proje klasörünün adı, örn. <c>7000_MILKON</c>.</param>
        /// <param name="stored"><c>proje.json</c>'daki renk; boş olabilir.</param>
        public static string For(string folderName, string? stored)
        {
            if (IsColor(stored))
            {
                return stored!.Trim();
            }

            var name = folderName ?? string.Empty;
            var cut = name.IndexOf('_');
            var code = (cut > 0 ? name.Substring(0, cut) : name).Trim().ToUpperInvariant();
            return Palette[(int)(Hash(code) % (uint)Palette.Length)];
        }

        // FNV-1a: string.GetHashCode sürümden sürüme değişebilir, bu değişmez.
        private static uint Hash(string text)
        {
            var hash = 2166136261u;
            foreach (var c in text)
            {
                hash ^= c;
                hash *= 16777619u;
            }

            return hash;
        }

        private static bool IsColor(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var text = value!.Trim();
            int ignored;
            return text.Length == 7 && text[0] == '#'
                && int.TryParse(text.Substring(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ignored);
        }
    }
}
