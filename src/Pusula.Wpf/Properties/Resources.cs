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

        #region Araç çubuğu

        /// <summary>Kök klasör alanı boşken görünen metin.</summary>
        public static string Root_Placeholder { get { return Get("Root_Placeholder"); } }

        /// <summary>Kök klasör seçme düğmesi.</summary>
        public static string Root_Browse { get { return Get("Root_Browse"); } }

        /// <summary>Klasör seçme penceresinin başlığı.</summary>
        public static string Root_DialogTitle { get { return Get("Root_DialogTitle"); } }

        /// <summary>Yeni proje düğmesi.</summary>
        public static string Button_NewProject { get { return Get("Button_NewProject"); } }

        /// <summary>Yeni stok düğmesi.</summary>
        public static string Button_NewStock { get { return Get("Button_NewStock"); } }

        /// <summary>Yenile düğmesinin ipucu.</summary>
        public static string Button_Refresh { get { return Get("Button_Refresh"); } }

        #endregion

        #region Ağaç

        /// <summary>Kök klasör seçilmemişken ağaç alanında görünen metin.</summary>
        public static string Tree_NoRoot { get { return Get("Tree_NoRoot"); } }

        /// <summary>Proje türünün adı.</summary>
        public static string Kind_Project { get { return Get("Kind_Project"); } }

        /// <summary>Stok türünün adı.</summary>
        public static string Kind_Stock { get { return Get("Kind_Stock"); } }

        /// <summary>İşaretsiz klasörün tür adı.</summary>
        public static string Kind_Plain { get { return Get("Kind_Plain"); } }

        #endregion

        #region İçerik

        /// <summary>Hiçbir düğüm seçilmemişken sağ panelin başlığı.</summary>
        public static string Content_EmptyTitle { get { return Get("Content_EmptyTitle"); } }

        /// <summary>Hiçbir düğüm seçilmemişken sağ panelde görünen ipucu.</summary>
        public static string Content_EmptyHint { get { return Get("Content_EmptyHint"); } }

        #endregion

        #region Diyalog

        /// <summary>Yeni proje diyaloğunun başlığı.</summary>
        public static string Dialog_NewProjectTitle { get { return Get("Dialog_NewProjectTitle"); } }

        /// <summary>Yeni stok diyaloğunun başlığı.</summary>
        public static string Dialog_NewStockTitle { get { return Get("Dialog_NewStockTitle"); } }

        /// <summary>Diyaloğun oluştur düğmesi.</summary>
        public static string Dialog_Create { get { return Get("Dialog_Create"); } }

        /// <summary>Diyaloğun vazgeç düğmesi.</summary>
        public static string Dialog_Cancel { get { return Get("Dialog_Cancel"); } }

        /// <summary>Proje numarası alanı.</summary>
        public static string Field_ProjectNo { get { return Get("Field_ProjectNo"); } }

        /// <summary>Proje adı alanı.</summary>
        public static string Field_ProjectName { get { return Get("Field_ProjectName"); } }

        /// <summary>Stokun ekleneceği proje.</summary>
        public static string Field_ParentProject { get { return Get("Field_ParentProject"); } }

        /// <summary>Stok numarası alanı.</summary>
        public static string Field_StockNo { get { return Get("Field_StockNo"); } }

        /// <summary>Stok adı alanı (isteğe bağlı).</summary>
        public static string Field_StockName { get { return Get("Field_StockName"); } }

        #endregion

        #region Durum

        /// <summary>Kök seçilmeden yeni proje istenince.</summary>
        public static string Status_SelectRootFirst { get { return Get("Status_SelectRootFirst"); } }

        /// <summary>Proje seçilmeden yeni stok istenince.</summary>
        public static string Status_SelectProjectFirst { get { return Get("Status_SelectProjectFirst"); } }

        /// <summary>Proje oluşunca; {0} klasör yolu.</summary>
        public static string Status_ProjectCreated { get { return Get("Status_ProjectCreated"); } }

        /// <summary>Stok oluşunca; {0} klasör yolu.</summary>
        public static string Status_StockCreated { get { return Get("Status_StockCreated"); } }

        #endregion

        #region Hata

        /// <summary>Üst klasör yok.</summary>
        public static string Error_ParentMissing { get { return Get("Error_ParentMissing"); } }

        /// <summary>Numara boş.</summary>
        public static string Error_NumberEmpty { get { return Get("Error_NumberEmpty"); } }

        /// <summary>Ad boş.</summary>
        public static string Error_NameEmpty { get { return Get("Error_NameEmpty"); } }

        /// <summary>Geçersiz karakter.</summary>
        public static string Error_InvalidCharacters { get { return Get("Error_InvalidCharacters"); } }

        /// <summary>Klasör zaten var; {0} yol.</summary>
        public static string Error_AlreadyExists { get { return Get("Error_AlreadyExists"); } }

        /// <summary>Disk hatası; {0} sistem mesajı.</summary>
        public static string Error_Io { get { return Get("Error_Io"); } }

        #endregion
    }
}
