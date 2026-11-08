using System.Text;

namespace Pusula.Wpf.ViewModels
{
    /// <summary>
    /// Arama karşılaştırması: büyük/küçük harf ve Türkçe i/ı/İ/I farkı gözetilmez ("satimo" = "SATIMO", "milkon" = "MILKON").
    /// </summary>
    public static class SearchMatch
    {
        /// <summary><paramref name="text"/> içinde <paramref name="part"/> geçiyor mu.</summary>
        public static bool Contains(string text, string part)
        {
            return Fold(text).Contains(Fold(part));
        }

        private static string Fold(string value)
        {
            var builder = new StringBuilder(value?.Length ?? 0);
            foreach (var c in value ?? string.Empty)
            {
                switch (c)
                {
                    case 'ı':
                    case 'İ':
                    case 'I':
                        builder.Append('i');
                        break;
                    default:
                        builder.Append(char.ToLowerInvariant(c));
                        break;
                }
            }

            return builder.ToString();
        }
    }
}
