using System.Globalization;
using System.Resources;

namespace Pusula.Wpf.Properties
{
    /// <summary>
    /// Arayüz metinlerine tür güvenli erişim (<c>Resources.resx</c>).
    /// Komut satırı MSBuild'de de derlensin diye elle yazılmıştır; resx'e anahtar eklenince buraya da eklenir.
    /// </summary>
    public static class Resources
    {
        #region Altyapı

        private static ResourceManager? resourceManager;
        private static CultureInfo? resourceCulture;

        /// <summary>Bu sınıfın kullandığı <see cref="System.Resources.ResourceManager"/>.</summary>
        public static ResourceManager ResourceManager
        {
            get
            {
                if (resourceManager == null)
                {
                    resourceManager = new ResourceManager("Pusula.Wpf.Properties.Resources", typeof(Resources).Assembly);
                }

                return resourceManager;
            }
        }

        /// <summary>Tüm aramalarda kullanılacak kültürü değiştirir.</summary>
        public static CultureInfo? Culture
        {
            get { return resourceCulture; }
            set { resourceCulture = value; }
        }

        private static string Get(string name)
        {
            return ResourceManager.GetString(name, resourceCulture) ?? name;
        }

        #endregion

        #region Pencere

        /// <summary>Ana pencerenin başlık metni.</summary>
        public static string Window_Title { get { return Get("Window_Title"); } }

        #endregion

        #region Ağaç

        /// <summary>Kök klasör seçilmemişken ağaç alanında görünen metin.</summary>
        public static string Tree_NoRoot { get { return Get("Tree_NoRoot"); } }

        #endregion

        #region İçerik

        /// <summary>Hiçbir düğüm seçilmemişken sağ panelin başlığı.</summary>
        public static string Content_EmptyTitle { get { return Get("Content_EmptyTitle"); } }

        /// <summary>Hiçbir düğüm seçilmemişken sağ panelde görünen ipucu.</summary>
        public static string Content_EmptyHint { get { return Get("Content_EmptyHint"); } }

        #endregion
    }
}
