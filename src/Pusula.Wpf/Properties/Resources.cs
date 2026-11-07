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

        #region Kök klasör

        /// <summary>Kök klasör alanı boşken görünen metin.</summary>
        public static string Root_Placeholder { get { return Get("Root_Placeholder"); } }

        /// <summary>Kök klasör seçme düğmesi.</summary>
        public static string Root_Browse { get { return Get("Root_Browse"); } }

        /// <summary>Klasör seçme penceresinin başlığı.</summary>
        public static string Root_DialogTitle { get { return Get("Root_DialogTitle"); } }

        #endregion

        #region Ağaç

        /// <summary>Ağaç bölümünün başlığı.</summary>
        public static string Tree_Header { get { return Get("Tree_Header"); } }

        /// <summary>Kök klasör seçilmemişken ağaç alanında görünen metin.</summary>
        public static string Tree_NoRoot { get { return Get("Tree_NoRoot"); } }

        /// <summary>Ekle düğmesinin ipucu.</summary>
        public static string Button_Add { get { return Get("Button_Add"); } }

        /// <summary>Yenile düğmesinin ipucu.</summary>
        public static string Button_Refresh { get { return Get("Button_Refresh"); } }

        /// <summary>Proje türünün adı.</summary>
        public static string Kind_Project { get { return Get("Kind_Project"); } }

        /// <summary>Stok türünün adı.</summary>
        public static string Kind_Stock { get { return Get("Kind_Stock"); } }

        /// <summary>İşaretsiz klasörün tür adı.</summary>
        public static string Kind_Plain { get { return Get("Kind_Plain"); } }

        #endregion

        #region Menü

        /// <summary>Yeni proje menü öğesi.</summary>
        public static string Menu_NewProject { get { return Get("Menu_NewProject"); } }

        /// <summary>Yeni stok menü öğesi (hedef proje yokken).</summary>
        public static string Menu_NewStock { get { return Get("Menu_NewStock"); } }

        /// <summary>Yeni stok menü öğesi; {0} hedef proje.</summary>
        public static string Menu_NewStockFor { get { return Get("Menu_NewStockFor"); } }

        /// <summary>Klasörü Windows Gezgini'nde açar.</summary>
        public static string Menu_OpenFolder { get { return Get("Menu_OpenFolder"); } }

        /// <summary>Yenile menü öğesi.</summary>
        public static string Menu_Refresh { get { return Get("Menu_Refresh"); } }

        /// <summary>Yeni proje kısayolu.</summary>
        public static string Gesture_NewProject { get { return Get("Gesture_NewProject"); } }

        /// <summary>Yeni stok kısayolu.</summary>
        public static string Gesture_NewStock { get { return Get("Gesture_NewStock"); } }

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

        /// <summary>Yeni stok diyaloğunun başlığı; {0} proje.</summary>
        public static string Dialog_NewStockTitleFor { get { return Get("Dialog_NewStockTitleFor"); } }

        /// <summary>Diyaloğun oluştur düğmesi.</summary>
        public static string Dialog_Create { get { return Get("Dialog_Create"); } }

        /// <summary>Diyaloğun vazgeç düğmesi.</summary>
        public static string Dialog_Cancel { get { return Get("Dialog_Cancel"); } }

        /// <summary>Proje numarası alanı.</summary>
        public static string Field_ProjectNo { get { return Get("Field_ProjectNo"); } }

        /// <summary>Proje adı alanı.</summary>
        public static string Field_ProjectName { get { return Get("Field_ProjectName"); } }

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

        #region Şablon

        /// <summary>Şablon seçimi alanı.</summary>
        public static string Field_Template { get { return Get("Field_Template"); } }

        /// <summary>Modül onay kutularının başlığı.</summary>
        public static string Field_Modules { get { return Get("Field_Modules"); } }

        /// <summary>Çekirdek klasörlerin bilgi metni; {0} liste.</summary>
        public static string Dialog_AlwaysCreated { get { return Get("Dialog_AlwaysCreated"); } }

        /// <summary>Stokta zaten bulunan modülün etiketi.</summary>
        public static string Module_Exists { get { return Get("Module_Exists"); } }

        /// <summary>Modül ekle menü öğesi.</summary>
        public static string Menu_AddModules { get { return Get("Menu_AddModules"); } }

        /// <summary>Modül ekle diyaloğunun başlığı; {0} stok.</summary>
        public static string Dialog_AddModulesTitle { get { return Get("Dialog_AddModulesTitle"); } }

        /// <summary>Modül ekle diyaloğunun ekle düğmesi.</summary>
        public static string Dialog_Add { get { return Get("Dialog_Add"); } }

        /// <summary>Modül eklenince; {0} sayı, {1} stok.</summary>
        public static string Status_ModulesAdded { get { return Get("Status_ModulesAdded"); } }

        /// <summary>Hiç yeni modül işaretlenmeden ekle denince.</summary>
        public static string Status_NothingToAdd { get { return Get("Status_NothingToAdd"); } }

        /// <summary>Stok seçilmeden modül ekle istenince.</summary>
        public static string Status_SelectStockFirst { get { return Get("Status_SelectStockFirst"); } }

        /// <summary>Şablon düzenleyici kaydedilince.</summary>
        public static string Status_TemplatesSaved { get { return Get("Status_TemplatesSaved"); } }

        /// <summary>Şablon düzenleyiciyi açan düğme ve menü öğesi.</summary>
        public static string Button_EditTemplates { get { return Get("Button_EditTemplates"); } }

        #endregion

        #region Düzenleyici

        /// <summary>Şablon düzenleyici başlığı.</summary>
        public static string Editor_Title { get { return Get("Editor_Title"); } }

        /// <summary>Şablon dosyası bilgisi; {0} yol.</summary>
        public static string Editor_SharedNote { get { return Get("Editor_SharedNote"); } }

        /// <summary>Modül listesi başlığı.</summary>
        public static string Editor_Modules { get { return Get("Editor_Modules"); } }

        /// <summary>Modül ekle düğmesi.</summary>
        public static string Editor_AddModule { get { return Get("Editor_AddModule"); } }

        /// <summary>Modül silme düğmesi.</summary>
        public static string Editor_DeleteModule { get { return Get("Editor_DeleteModule"); } }

        /// <summary>Her stokta açılan modülün etiketi.</summary>
        public static string Editor_CoreBadge { get { return Get("Editor_CoreBadge"); } }

        /// <summary>Modül adı alanı.</summary>
        public static string Editor_Name { get { return Get("Editor_Name"); } }

        /// <summary>Modül klasörü alanı (numara ve ad).</summary>
        public static string Editor_Folder { get { return Get("Editor_Folder"); } }

        /// <summary>Her stokta açılır seçeneği.</summary>
        public static string Editor_Core { get { return Get("Editor_Core"); } }

        /// <summary>Her stokta seçeneğinin açıklaması.</summary>
        public static string Editor_CoreHint { get { return Get("Editor_CoreHint"); } }

        /// <summary>Modülün işaretli geldiği şablonlar.</summary>
        public static string Editor_InTemplates { get { return Get("Editor_InTemplates"); } }

        /// <summary>Alt klasör ağacının başlığı.</summary>
        public static string Editor_SubFolders { get { return Get("Editor_SubFolders"); } }

        /// <summary>Modül klasörünün altına klasör ekler.</summary>
        public static string Editor_AddSubFolder { get { return Get("Editor_AddSubFolder"); } }

        /// <summary>Düğümün altına klasör ekler.</summary>
        public static string Editor_AddChild { get { return Get("Editor_AddChild"); } }

        /// <summary>Sil düğmesinin ipucu.</summary>
        public static string Editor_Delete { get { return Get("Editor_Delete"); } }

        /// <summary>Silme açıklaması.</summary>
        public static string Editor_DeleteHint { get { return Get("Editor_DeleteHint"); } }

        /// <summary>Şablon listesi başlığı.</summary>
        public static string Editor_Templates { get { return Get("Editor_Templates"); } }

        /// <summary>Şablon ekle düğmesi.</summary>
        public static string Editor_AddTemplate { get { return Get("Editor_AddTemplate"); } }

        /// <summary>Şablon bölümünün açıklaması.</summary>
        public static string Editor_TemplatesHint { get { return Get("Editor_TemplatesHint"); } }

        /// <summary>Yeni modülün varsayılan adı.</summary>
        public static string Editor_NewModule { get { return Get("Editor_NewModule"); } }

        /// <summary>Yeni şablonun varsayılan adı.</summary>
        public static string Editor_NewTemplate { get { return Get("Editor_NewTemplate"); } }

        /// <summary>Yeni klasörün varsayılan adı.</summary>
        public static string Editor_NewFolder { get { return Get("Editor_NewFolder"); } }

        /// <summary>Kaydet düğmesi.</summary>
        public static string Editor_Save { get { return Get("Editor_Save"); } }

        /// <summary>Boş modül adı.</summary>
        public static string Editor_ErrorModuleName { get { return Get("Editor_ErrorModuleName"); } }

        /// <summary>Geçersiz modül klasörü; {0} ad.</summary>
        public static string Editor_ErrorFolder { get { return Get("Editor_ErrorFolder"); } }

        /// <summary>Tekrarlı modül klasörü; {0} ad.</summary>
        public static string Editor_ErrorDuplicateFolder { get { return Get("Editor_ErrorDuplicateFolder"); } }

        /// <summary>Geçersiz alt klasör; {0} modül, {1} ad.</summary>
        public static string Editor_ErrorSubFolder { get { return Get("Editor_ErrorSubFolder"); } }

        /// <summary>Geçersiz şablon adı.</summary>
        public static string Editor_ErrorTemplateName { get { return Get("Editor_ErrorTemplateName"); } }

        #endregion
    }
}
