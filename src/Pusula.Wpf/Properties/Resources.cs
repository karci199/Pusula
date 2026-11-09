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

        /// <summary>Modül adı alanı.</summary>
        public static string Editor_Name { get { return Get("Editor_Name"); } }

        /// <summary>Modül klasörü alanı (numara ve ad).</summary>
        public static string Editor_Folder { get { return Get("Editor_Folder"); } }

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

        /// <summary>Şablona modül ekleme düğmesi.</summary>
        public static string Editor_AddModule { get { return Get("Editor_AddModule"); } }

        /// <summary>Şablon silme düğmesi.</summary>
        public static string Editor_DeleteTemplate { get { return Get("Editor_DeleteTemplate"); } }

        /// <summary>Şablon seçili değilken.</summary>
        public static string Editor_SelectTemplate { get { return Get("Editor_SelectTemplate"); } }

        /// <summary>Modül düzenle düğmesinin ipucu.</summary>
        public static string Editor_EditModule { get { return Get("Editor_EditModule"); } }

        /// <summary>Modülü şablondan çıkaran düğmenin ipucu.</summary>
        public static string Editor_RemoveFromTemplate { get { return Get("Editor_RemoveFromTemplate"); } }

        /// <summary>Seçili modülün kullanım bilgisi; {0} ad, {1} sayı.</summary>
        public static string Editor_ModuleUsage { get { return Get("Editor_ModuleUsage"); } }

        /// <summary>Modülün alt klasör sayısı.</summary>
        public static string Editor_SubFolderCount { get { return Get("Editor_SubFolderCount"); } }

        /// <summary>Modül ekle menüsü boşken.</summary>
        public static string Editor_NoMoreModules { get { return Get("Editor_NoMoreModules"); } }

        /// <summary>Modül ekle menüsünün son öğesi.</summary>
        public static string Editor_CreateModule { get { return Get("Editor_CreateModule"); } }

        /// <summary>Modül penceresindeki alt klasör başlığı.</summary>
        public static string Editor_SubFoldersOptional { get { return Get("Editor_SubFoldersOptional"); } }

        #endregion

        #region Modül penceresi

        /// <summary>Yeni modül penceresinin başlığı.</summary>
        public static string ModuleDialog_NewTitle { get { return Get("ModuleDialog_NewTitle"); } }

        /// <summary>Modül düzenleme penceresinin başlığı.</summary>
        public static string ModuleDialog_EditTitle { get { return Get("ModuleDialog_EditTitle"); } }

        /// <summary>Oluşacak klasör adı; {0} klasör.</summary>
        public static string ModuleDialog_FolderPreview { get { return Get("ModuleDialog_FolderPreview"); } }

        /// <summary>Modül düzenlemesini uygulayan düğme.</summary>
        public static string ModuleDialog_Apply { get { return Get("ModuleDialog_Apply"); } }

        /// <summary>Modül penceresinin açıklaması.</summary>
        public static string ModuleDialog_Hint { get { return Get("ModuleDialog_Hint"); } }

        #endregion

        #region Özet

        /// <summary>Özet hesaplanırken.</summary>
        public static string Detail_Loading { get { return Get("Detail_Loading"); } }

        /// <summary>Proje özetinin notu; {0} dosya sayısı.</summary>
        public static string Detail_General { get { return Get("Detail_General"); } }

        /// <summary>Stok başlığı üstü; {0} şablon, {1} proje.</summary>
        public static string Detail_StockSubtitle { get { return Get("Detail_StockSubtitle"); } }

        /// <summary>Şablonsuz stok başlığı üstü; {0} proje.</summary>
        public static string Detail_StockSubtitleNoTemplate { get { return Get("Detail_StockSubtitleNoTemplate"); } }

        /// <summary>Stok tablosunun notu.</summary>
        public static string Detail_RowHint { get { return Get("Detail_RowHint"); } }

        /// <summary>Kutucuk: stok sayısı.</summary>
        public static string Tile_Stocks { get { return Get("Tile_Stocks"); } }

        /// <summary>Kutucuk: dosya sayısı.</summary>
        public static string Tile_Files { get { return Get("Tile_Files"); } }

        /// <summary>Kutucuk: toplam boyut.</summary>
        public static string Tile_Size { get { return Get("Tile_Size"); } }

        /// <summary>Kutucuk: son değişiklik tarihi.</summary>
        public static string Tile_LastChange { get { return Get("Tile_LastChange"); } }

        /// <summary>Kutucuk: son değişen dosya.</summary>
        public static string Tile_LastFile { get { return Get("Tile_LastFile"); } }

        /// <summary>Sütun: stok.</summary>
        public static string Col_Stock { get { return Get("Col_Stock"); } }

        /// <summary>Sütun: şablon.</summary>
        public static string Col_Template { get { return Get("Col_Template"); } }

        /// <summary>Sütun: dosya sayısı.</summary>
        public static string Col_Files { get { return Get("Col_Files"); } }

        /// <summary>Sütun: boyut.</summary>
        public static string Col_Size { get { return Get("Col_Size"); } }

        /// <summary>Sütun: son değişiklik.</summary>
        public static string Col_LastChange { get { return Get("Col_LastChange"); } }

        /// <summary>Sütun: klasör.</summary>
        public static string Col_Folder { get { return Get("Col_Folder"); } }

        /// <summary>Sütun: son değişen dosya.</summary>
        public static string Col_LastFile { get { return Get("Col_LastFile"); } }

        /// <summary>Dosyası olmayan klasör.</summary>
        public static string Row_Empty { get { return Get("Row_Empty"); } }

        /// <summary>Şablonda olup diskte olmayan klasör.</summary>
        public static string Row_Missing { get { return Get("Row_Missing"); } }

        /// <summary>Alt çizgiyle başlayan özel klasör.</summary>
        public static string Row_Special { get { return Get("Row_Special"); } }

        #endregion

        #region Biçim

        /// <summary>Boyut: bayt.</summary>
        public static string Size_Bytes { get { return Get("Size_Bytes"); } }

        /// <summary>Boyut: KB.</summary>
        public static string Size_KB { get { return Get("Size_KB"); } }

        /// <summary>Boyut: MB.</summary>
        public static string Size_MB { get { return Get("Size_MB"); } }

        /// <summary>Boyut: GB.</summary>
        public static string Size_GB { get { return Get("Size_GB"); } }

        /// <summary>Tarih ve saat biçimi.</summary>
        public static string Format_DateTime { get { return Get("Format_DateTime"); } }

        /// <summary>Tarih biçimi.</summary>
        public static string Format_Date { get { return Get("Format_Date"); } }

        /// <summary>Bugünkü tarih; {0} saat.</summary>
        public static string Format_Today { get { return Get("Format_Today"); } }

        #endregion

        #region Durum

        /// <summary>Sütun: durum.</summary>
        public static string Col_Status { get { return Get("Col_Status"); } }

        /// <summary>Durumu verilmemiş stoklar (dağılımda).</summary>
        public static string Status_None { get { return Get("Status_None"); } }

        /// <summary>Durumun verildiği tarih; {0} tarih.</summary>
        public static string Status_Since { get { return Get("Status_Since"); } }

        /// <summary>Durum etiketinin ipucu.</summary>
        public static string Status_ChangeHint { get { return Get("Status_ChangeHint"); } }

        /// <summary>Durum değişince alt çubuk; {0} stok, {1} durum.</summary>
        public static string Status_StatusChanged { get { return Get("Status_StatusChanged"); } }

        /// <summary>Durum notu alanı.</summary>
        public static string Field_StatusNote { get { return Get("Field_StatusNote"); } }

        /// <summary>Biçime uymayan stok numarası; {0} biçimler.</summary>
        public static string Error_StockNumberFormat { get { return Get("Error_StockNumberFormat"); } }

        #endregion

        #region Biçim

        /// <summary>Saatsiz bugünkü tarih.</summary>
        public static string Format_TodayOnly { get { return Get("Format_TodayOnly"); } }

        #endregion

        #region Durum

        /// <summary>Durum bugün verildiyse.</summary>
        public static string Status_SinceToday { get { return Get("Status_SinceToday"); } }

        #endregion

        #region İşaretle

        /// <summary>Elle açılmış klasörü proje yapar.</summary>
        public static string Menu_MarkAsProject { get { return Get("Menu_MarkAsProject"); } }

        /// <summary>Elle açılmış klasörü stok yapar.</summary>
        public static string Menu_MarkAsStock { get { return Get("Menu_MarkAsStock"); } }

        /// <summary>Diyalog başlığı; {0} klasör.</summary>
        public static string Mark_ProjectTitle { get { return Get("Mark_ProjectTitle"); } }

        /// <summary>Diyalog başlığı; {0} klasör.</summary>
        public static string Mark_StockTitle { get { return Get("Mark_StockTitle"); } }

        /// <summary>İşaretleme açıklaması.</summary>
        public static string Mark_Hint { get { return Get("Mark_Hint"); } }

        /// <summary>İşaretle düğmesi.</summary>
        public static string Mark_Confirm { get { return Get("Mark_Confirm"); } }

        /// <summary>Proje işaretlenince; {0} yol.</summary>
        public static string Status_MarkedProject { get { return Get("Status_MarkedProject"); } }

        /// <summary>Stok işaretlenince; {0} yol.</summary>
        public static string Status_MarkedStock { get { return Get("Status_MarkedStock"); } }

        /// <summary>Zaten işaretli klasör.</summary>
        public static string Error_AlreadyMarked { get { return Get("Error_AlreadyMarked"); } }

        #endregion

        #region Menü

        /// <summary>Yenile kısayolu.</summary>
        public static string Gesture_Refresh { get { return Get("Gesture_Refresh"); } }

        #endregion

        #region Sekmeler

        /// <summary>Sekme: genel.</summary>
        public static string Tab_General { get { return Get("Tab_General"); } }

        /// <summary>Sekme: testler; {0} koşu sayısı.</summary>
        public static string Tab_Tests { get { return Get("Tab_Tests"); } }

        /// <summary>Sekme: kalifikasyon; {0} koşu sayısı.</summary>
        public static string Tab_Qualification { get { return Get("Tab_Qualification"); } }

        /// <summary>Sekme: klasörler.</summary>
        public static string Tab_Folders { get { return Get("Tab_Folders"); } }

        /// <summary>Genel: dikkat başlığı.</summary>
        public static string General_Attention { get { return Get("General_Attention"); } }

        /// <summary>Genel: dikkat maddesi yokken.</summary>
        public static string General_NoAttention { get { return Get("General_NoAttention"); } }

        /// <summary>Genel: son sonuçlar başlığı.</summary>
        public static string General_Latest { get { return Get("General_Latest"); } }

        /// <summary>Genel: olayların akışı (testler, notlar, Obsidian olay notları).</summary>
        public static string General_Timeline { get { return Get("General_Timeline"); } }

        /// <summary>Genel: koşu yokken.</summary>
        public static string General_NoRuns { get { return Get("General_NoRuns"); } }

        /// <summary>Klasörler sekmesinin notu.</summary>
        public static string Folders_Hint { get { return Get("Folders_Hint"); } }

        #endregion

        #region Testler

        /// <summary>Sütun: seri numarası.</summary>
        public static string Col_Serial { get { return Get("Col_Serial"); } }

        /// <summary>Sütun: tur.</summary>
        public static string Col_Round { get { return Get("Col_Round"); } }

        /// <summary>Sütun: sistem.</summary>
        public static string Col_System { get { return Get("Col_System"); } }

        /// <summary>Sütun: sonuç.</summary>
        public static string Col_Result { get { return Get("Col_Result"); } }

        /// <summary>Sütun: tarih.</summary>
        public static string Col_Date { get { return Get("Col_Date"); } }

        /// <summary>Sütun: kişi.</summary>
        public static string Col_Person { get { return Get("Col_Person"); } }

        /// <summary>Sütun: not.</summary>
        public static string Col_Note { get { return Get("Col_Note"); } }

        /// <summary>Sonuç: geçti.</summary>
        public static string Result_Passed { get { return Get("Result_Passed"); } }

        /// <summary>Sonuç: kaldı.</summary>
        public static string Result_Failed { get { return Get("Result_Failed"); } }

        /// <summary>Sonuç: devam ediyor.</summary>
        public static string Result_InProgress { get { return Get("Result_InProgress"); } }

        /// <summary>Sonuç girilmemiş.</summary>
        public static string Result_Unknown { get { return Get("Result_Unknown"); } }

        /// <summary>Kalifikasyon sistemi; {0} sistem.</summary>
        public static string Test_QualificationSystem { get { return Get("Test_QualificationSystem"); } }

        /// <summary>Ağaçta dosya sayısı.</summary>
        public static string Tree_FileCount { get { return Get("Tree_FileCount"); } }

        #endregion

        #region Genel

        /// <summary>Özet kartı: koşu sayısı.</summary>
        public static string Kpi_Runs { get { return Get("Kpi_Runs"); } }

        /// <summary>Özet kartı: geçen.</summary>
        public static string Kpi_Passed { get { return Get("Kpi_Passed"); } }

        /// <summary>Özet kartı: kalan.</summary>
        public static string Kpi_Failed { get { return Get("Kpi_Failed"); } }

        /// <summary>Özet kartı: son koşu.</summary>
        public static string Kpi_Last { get { return Get("Kpi_Last"); } }

        /// <summary>Dikkat kartı başlığı: açık test.</summary>
        public static string Alert_OpenTitle { get { return Get("Alert_OpenTitle"); } }

        /// <summary>Açık test açıklamasının sonu.</summary>
        public static string Alert_OpenSuffix { get { return Get("Alert_OpenSuffix"); } }

        /// <summary>Dikkat kartı başlığı: kalan son tur.</summary>
        public static string Alert_FailedTitle { get { return Get("Alert_FailedTitle"); } }

        /// <summary>Dikkat kartı başlığı: jig revizyonu.</summary>
        public static string Alert_JigTitle { get { return Get("Alert_JigTitle"); } }

        /// <summary>Dikkat kartı başlığı: sonucu olmayan koşular.</summary>
        public static string Alert_UnknownTitle { get { return Get("Alert_UnknownTitle"); } }

        /// <summary>Sonucu olmayan koşu sayısı.</summary>
        public static string Alert_UnknownDetail { get { return Get("Alert_UnknownDetail"); } }

        /// <summary>Açılan kartta plan satırı.</summary>
        public static string Timeline_Plan { get { return Get("Timeline_Plan"); } }

        /// <summary>Açılan kartta jig satırı.</summary>
        public static string Timeline_Jig { get { return Get("Timeline_Jig"); } }

        /// <summary>Koşunun notunu açar.</summary>
        public static string Run_OpenNote { get { return Get("Run_OpenNote"); } }

        /// <summary>Koşunun klasörünü açar.</summary>
        public static string Run_OpenFolder { get { return Get("Run_OpenFolder"); } }

        /// <summary>Koşunun Ham klasörünü açar.</summary>
        public static string Run_OpenRaw { get { return Get("Run_OpenRaw"); } }

        /// <summary>Koşunun Foto klasörünü açar.</summary>
        public static string Run_OpenPhotos { get { return Get("Run_OpenPhotos"); } }

        #endregion

        #region Biçim

        /// <summary>Gün.ay biçimi.</summary>
        public static string Format_DayMonth { get { return Get("Format_DayMonth"); } }

        #endregion

        #region Envanter

        /// <summary>Ağaçta envanter bölümü.</summary>
        public static string Tree_Inventory { get { return Get("Tree_Inventory"); } }

        /// <summary>Yeni kayıt menüsü ve diyalog başlığı; {0} tür adı.</summary>
        public static string Inv_NewTitle { get { return Get("Inv_NewTitle"); } }

        /// <summary>Kayıt açılınca; {0} klasör.</summary>
        public static string Inv_Created { get { return Get("Inv_Created"); } }

        /// <summary>Durum olayı diyaloğu; {0} yeni durum.</summary>
        public static string Inv_EventTitle { get { return Get("Inv_EventTitle"); } }

        /// <summary>Alan: numara.</summary>
        public static string Inv_Number { get { return Get("Inv_Number"); } }

        /// <summary>Alan: tanım.</summary>
        public static string Inv_Name { get { return Get("Inv_Name"); } }

        /// <summary>Alan: tür.</summary>
        public static string Inv_Type { get { return Get("Inv_Type"); } }

        /// <summary>Alan: alt tür.</summary>
        public static string Inv_SubType { get { return Get("Inv_SubType"); } }

        /// <summary>Alan: not.</summary>
        public static string Inv_Note { get { return Get("Inv_Note"); } }

        /// <summary>Alan: düzey.</summary>
        public static string Inv_Level { get { return Get("Inv_Level"); } }

        /// <summary>Düzey: genel.</summary>
        public static string Inv_LevelGeneral { get { return Get("Inv_LevelGeneral"); } }

        /// <summary>Düzey: proje; {0} proje.</summary>
        public static string Inv_LevelProject { get { return Get("Inv_LevelProject"); } }

        /// <summary>Düzey: stok; {0} stok.</summary>
        public static string Inv_LevelStock { get { return Get("Inv_LevelStock"); } }

        /// <summary>Bilgi: talep tarihi ve kişi.</summary>
        public static string Inv_Requested { get { return Get("Inv_Requested"); } }

        /// <summary>Bilgi: hazırlayan kişi.</summary>
        public static string Inv_MadeBy { get { return Get("Inv_MadeBy"); } }

        /// <summary>Bilgi: teslim tarihi.</summary>
        public static string Inv_Delivered { get { return Get("Inv_Delivered"); } }

        /// <summary>Alan: konum.</summary>
        public static string Inv_Location { get { return Get("Inv_Location"); } }

        /// <summary>Alan: tarih.</summary>
        public static string Inv_Date { get { return Get("Inv_Date"); } }

        /// <summary>Alan: kişi.</summary>
        public static string Inv_Person { get { return Get("Inv_Person"); } }

        /// <summary>Kayıt sayfası: bilgiler başlığı.</summary>
        public static string Inv_Facts { get { return Get("Inv_Facts"); } }

        /// <summary>Kayıt sayfası: geçmiş başlığı.</summary>
        public static string Inv_History { get { return Get("Inv_History"); } }

        /// <summary>Ağaçta kaydı olmayan klasörün eki.</summary>
        public static string Inv_Unmarked { get { return Get("Inv_Unmarked"); } }

        /// <summary>Sağ tık: elle açılmış klasörü kayıt yapar; {0} tür adı.</summary>
        public static string Inv_MarkAs { get { return Get("Inv_MarkAs"); } }

        /// <summary>İşaretlemede varsayılan durum (listede varsa).</summary>
        public static string Inv_DefaultMarkStatus { get { return Get("Inv_DefaultMarkStatus"); } }

        /// <summary>Yeni kayıt diyaloğunun notu.</summary>
        public static string Inv_TemplateHint { get { return Get("Inv_TemplateHint"); } }

        /// <summary>Kayıt sayfası: alt klasör kutucukları başlığı.</summary>
        public static string Inv_Files { get { return Get("Inv_Files"); } }

        /// <summary>Kutucukta dosyası olmayan klasör.</summary>
        public static string Inv_FolderEmpty { get { return Get("Inv_FolderEmpty"); } }

        /// <summary>Kutucuk: dosya sayısı ve son revizyon.</summary>
        public static string Inv_FilesWithRevision { get { return Get("Inv_FilesWithRevision"); } }

        /// <summary>Kayıt sayfası: not kartı başlığı.</summary>
        public static string Inv_NoteTitle { get { return Get("Inv_NoteTitle"); } }

        /// <summary>Notun gövdesi boşsa.</summary>
        public static string Inv_NoteEmpty { get { return Get("Inv_NoteEmpty"); } }

        /// <summary>Klasörde md notu yoksa.</summary>
        public static string Inv_NoNote { get { return Get("Inv_NoNote"); } }

        /// <summary>Notu Obsidian'da (varsayılan uygulamada) açar.</summary>
        public static string Inv_OpenNote { get { return Get("Inv_OpenNote"); } }

        /// <summary>Kaydın md notunu YAML bloğuyla oluşturur.</summary>
        public static string Inv_CreateNote { get { return Get("Inv_CreateNote"); } }

        /// <summary>Durum çubuğu.</summary>
        public static string Inv_NoteFailed { get { return Get("Inv_NoteFailed"); } }

        #endregion

        #region Stok envanteri

        /// <summary>Genel sekmesinde özet başlığı.</summary>
        public static string StockInv_Section { get { return Get("StockInv_Section"); } }

        /// <summary>Özet: sıra dışı durumdaki kayıt sayısı.</summary>
        public static string StockInv_Attention { get { return Get("StockInv_Attention"); } }

        /// <summary>Özet: bulunamayan bağ sayısı.</summary>
        public static string StockInv_Missing { get { return Get("StockInv_Missing"); } }

        /// <summary>Bağlı numaranın kaydı yok.</summary>
        public static string Inv_LinkMissing { get { return Get("Inv_LinkMissing"); } }

        /// <summary>Dikkat kartı; {0} tür adı.</summary>
        public static string Alert_InvMissingTitle { get { return Get("Alert_InvMissingTitle"); } }

        /// <summary>Genel / proje kaydını stoka bağlar.</summary>
        public static string StockInv_AddExisting { get { return Get("StockInv_AddExisting"); } }

        /// <summary>İpucu.</summary>
        public static string StockInv_AddHint { get { return Get("StockInv_AddHint"); } }

        /// <summary>Stok düzeyinde yeni kayıt açar.</summary>
        public static string StockInv_New { get { return Get("StockInv_New"); } }

        /// <summary>İpucu.</summary>
        public static string StockInv_NewHint { get { return Get("StockInv_NewHint"); } }

        /// <summary>Ekle menüsü boşsa.</summary>
        public static string StockInv_NoCandidates { get { return Get("StockInv_NoCandidates"); } }

        /// <summary>Boş bölüm ipucu.</summary>
        public static string StockInv_Empty { get { return Get("StockInv_Empty"); } }

        /// <summary>Satır menüsü.</summary>
        public static string StockInv_OpenRecord { get { return Get("StockInv_OpenRecord"); } }

        /// <summary>Satır menüsü; kayda dokunmaz.</summary>
        public static string StockInv_Unlink { get { return Get("StockInv_Unlink"); } }

        /// <summary>Durum çubuğu.</summary>
        public static string StockInv_Linked { get { return Get("StockInv_Linked"); } }

        /// <summary>Durum çubuğu.</summary>
        public static string StockInv_Unlinked { get { return Get("StockInv_Unlinked"); } }

        /// <summary>Durum çubuğu.</summary>
        public static string StockInv_LinkFailed { get { return Get("StockInv_LinkFailed"); } }

        #endregion

        #region Türler

        /// <summary>Ayarlar düğmesi ipucu.</summary>
        public static string Button_Settings { get { return Get("Button_Settings"); } }

        /// <summary>Türler penceresini açar.</summary>
        public static string Button_EditTypes { get { return Get("Button_EditTypes"); } }

        /// <summary>Durum çubuğu.</summary>
        public static string Status_TypesSaved { get { return Get("Status_TypesSaved"); } }

        /// <summary>Pencere başlığı.</summary>
        public static string Types_Title { get { return Get("Types_Title"); } }

        /// <summary>Sol liste başlığı.</summary>
        public static string Types_List { get { return Get("Types_List"); } }

        /// <summary>Yeni tür.</summary>
        public static string Types_Add { get { return Get("Types_Add"); } }

        /// <summary>Sol alt ipucu.</summary>
        public static string Types_ListHint { get { return Get("Types_ListHint"); } }

        /// <summary>Yeni türün başlangıç adı.</summary>
        public static string Types_NewName { get { return Get("Types_NewName"); } }

        /// <summary>Yeni türün başlangıç çoğul adı.</summary>
        public static string Types_NewPlural { get { return Get("Types_NewPlural"); } }

        /// <summary>Tür adı.</summary>
        public static string Types_Name { get { return Get("Types_Name"); } }

        /// <summary>Çoğul ad.</summary>
        public static string Types_Plural { get { return Get("Types_Plural"); } }

        /// <summary>Simge.</summary>
        public static string Types_Icon { get { return Get("Types_Icon"); } }

        /// <summary>Renk.</summary>
        public static string Types_Color { get { return Get("Types_Color"); } }

        /// <summary>Bölüm başlığı.</summary>
        public static string Types_Formats { get { return Get("Types_Formats"); } }

        /// <summary>Sütun.</summary>
        public static string Types_Pattern { get { return Get("Types_Pattern"); } }

        /// <summary>Sütun.</summary>
        public static string Types_Label { get { return Get("Types_Label"); } }

        /// <summary>Numara deneme kutusu.</summary>
        public static string Types_Try { get { return Get("Types_Try"); } }

        /// <summary>Numara uyan biçim; {0} etiket.</summary>
        public static string Types_TryMatch { get { return Get("Types_TryMatch"); } }

        /// <summary>Numara uymuyor.</summary>
        public static string Types_TryNoMatch { get { return Get("Types_TryNoMatch"); } }

        /// <summary>Bölüm başlığı.</summary>
        public static string Types_SubTypes { get { return Get("Types_SubTypes"); } }

        /// <summary>Sütun.</summary>
        public static string Types_SubType { get { return Get("Types_SubType"); } }

        /// <summary>Bölüm başlığı.</summary>
        public static string Types_RecordFolders { get { return Get("Types_RecordFolders"); } }

        /// <summary>Sütun.</summary>
        public static string Types_Folder { get { return Get("Types_Folder"); } }

        /// <summary>Bölüm başlığı.</summary>
        public static string Types_Fields { get { return Get("Types_Fields"); } }

        /// <summary>Sütun.</summary>
        public static string Types_FieldName { get { return Get("Types_FieldName"); } }

        /// <summary>Sütun.</summary>
        public static string Types_FieldKind { get { return Get("Types_FieldKind"); } }

        /// <summary>Sütun.</summary>
        public static string Types_FieldUnit { get { return Get("Types_FieldUnit"); } }

        /// <summary>Sütun.</summary>
        public static string Types_FieldChoices { get { return Get("Types_FieldChoices"); } }

        /// <summary>Sütun.</summary>
        public static string Types_FieldSubTypes { get { return Get("Types_FieldSubTypes"); } }

        /// <summary>Alan ipucu.</summary>
        public static string Types_FieldsHint { get { return Get("Types_FieldsHint"); } }

        /// <summary>Bölüm başlığı.</summary>
        public static string Types_Statuses { get { return Get("Types_Statuses"); } }

        /// <summary>Sütun.</summary>
        public static string Types_StatusName { get { return Get("Types_StatusName"); } }

        /// <summary>Sütun.</summary>
        public static string Types_StatusTone { get { return Get("Types_StatusTone"); } }

        /// <summary>Sütun.</summary>
        public static string Types_StatusSide { get { return Get("Types_StatusSide"); } }

        /// <summary>Sütun.</summary>
        public static string Types_StatusNote { get { return Get("Types_StatusNote"); } }

        /// <summary>Durum ipucu.</summary>
        public static string Types_StatusesHint { get { return Get("Types_StatusesHint"); } }

        /// <summary>Bölüm başlığı.</summary>
        public static string Types_LevelFolders { get { return Get("Types_LevelFolders"); } }

        /// <summary>İpucu.</summary>
        public static string Types_LevelFoldersHint { get { return Get("Types_LevelFoldersHint"); } }

        /// <summary>Hata.</summary>
        public static string Types_ErrorName { get { return Get("Types_ErrorName"); } }

        /// <summary>Hata.</summary>
        public static string Types_ErrorPattern { get { return Get("Types_ErrorPattern"); } }

        /// <summary>Hata.</summary>
        public static string Types_ErrorField { get { return Get("Types_ErrorField"); } }

        /// <summary>Hata.</summary>
        public static string Types_ErrorStatus { get { return Get("Types_ErrorStatus"); } }

        /// <summary>Hata.</summary>
        public static string Types_ErrorDuplicate { get { return Get("Types_ErrorDuplicate"); } }

        /// <summary>Hata.</summary>
        public static string Types_ErrorSave { get { return Get("Types_ErrorSave"); } }

        /// <summary>Alan çeşidi.</summary>
        public static string FieldKind_Text { get { return Get("FieldKind_Text"); } }

        /// <summary>Alan çeşidi.</summary>
        public static string FieldKind_Number { get { return Get("FieldKind_Number"); } }

        /// <summary>Alan çeşidi.</summary>
        public static string FieldKind_YesNo { get { return Get("FieldKind_YesNo"); } }

        /// <summary>Alan çeşidi.</summary>
        public static string FieldKind_Choice { get { return Get("FieldKind_Choice"); } }

        /// <summary>Durum rengi.</summary>
        public static string Tone_Neutral { get { return Get("Tone_Neutral"); } }

        /// <summary>Durum rengi.</summary>
        public static string Tone_Waiting { get { return Get("Tone_Waiting"); } }

        /// <summary>Durum rengi.</summary>
        public static string Tone_Active { get { return Get("Tone_Active"); } }

        /// <summary>Durum rengi.</summary>
        public static string Tone_Done { get { return Get("Tone_Done"); } }

        /// <summary>Durum rengi.</summary>
        public static string Tone_Warning { get { return Get("Tone_Warning"); } }

        /// <summary>Durum rengi.</summary>
        public static string Tone_Failed { get { return Get("Tone_Failed"); } }

        #endregion

        #region Stok

        /// <summary>Kapak kartı.</summary>
        public static string Stock_NoNote { get { return Get("Stock_NoNote"); } }

        /// <summary>Kapak kartı: fotoğraf klasörü düğmesi.</summary>
        public static string Stock_Photos { get { return Get("Stock_Photos"); } }

        /// <summary>Kapak kartı: doküman klasörü düğmesi.</summary>
        public static string Stock_Documents { get { return Get("Stock_Documents"); } }

        #endregion

        #region Süzgeç

        /// <summary>Ağaç arama kutusu.</summary>
        public static string Tree_Search { get { return Get("Tree_Search"); } }

        /// <summary>Envanter listesi arama kutusu.</summary>
        public static string Filter_Search { get { return Get("Filter_Search"); } }

        /// <summary>Süzgeçte her şey.</summary>
        public static string Filter_All { get { return Get("Filter_All"); } }

        /// <summary>Liste sayısı.</summary>
        public static string Filter_Count { get { return Get("Filter_Count"); } }

        /// <summary>Süzülmüş liste sayısı.</summary>
        public static string Filter_CountOf { get { return Get("Filter_CountOf"); } }

        #endregion

        #region Kısa not

        /// <summary>Zaman çizelgesi / geçmişte not etiketi.</summary>
        public static string QuickNote_Chip { get { return Get("QuickNote_Chip"); } }

        /// <summary>Kısa not düğmesi.</summary>
        public static string QuickNote_Add { get { return Get("QuickNote_Add"); } }

        /// <summary>Tablo sağ tık menüsü.</summary>
        public static string QuickNote_AddMenu { get { return Get("QuickNote_AddMenu"); } }

        /// <summary>Not kutusu ipucu.</summary>
        public static string QuickNote_Hint { get { return Get("QuickNote_Hint"); } }

        /// <summary>Proje sayfası kart başlığı.</summary>
        public static string QuickNote_Section { get { return Get("QuickNote_Section"); } }

        /// <summary>Diyalog başlığı.</summary>
        public static string QuickNote_DialogTitle { get { return Get("QuickNote_DialogTitle"); } }

        /// <summary>Durum çubuğu.</summary>
        public static string QuickNote_Added { get { return Get("QuickNote_Added"); } }

        /// <summary>Tablo sağ tık: sayfasına git.</summary>
        public static string Row_Open { get { return Get("Row_Open"); } }

        /// <summary>Zaman çizelgesinde notun başlığı.</summary>
        public static string QuickNote_Title { get { return Get("QuickNote_Title"); } }

        #endregion

        #region Olay

        /// <summary>olay: toplanti</summary>
        public static string Event_Meeting { get { return Get("Event_Meeting"); } }

        /// <summary>olay: karar</summary>
        public static string Event_Decision { get { return Get("Event_Decision"); } }

        /// <summary>olay: numune</summary>
        public static string Event_Sample { get { return Get("Event_Sample"); } }

        /// <summary>olay: sevkiyat</summary>
        public static string Event_Shipment { get { return Get("Event_Shipment"); } }

        /// <summary>olay: revizyon</summary>
        public static string Event_Revision { get { return Get("Event_Revision"); } }

        /// <summary>olay: ariza</summary>
        public static string Event_Fault { get { return Get("Event_Fault"); } }

        #endregion

        #region Türler

        /// <summary>Hata.</summary>
        public static string Types_ErrorFolder { get { return Get("Types_ErrorFolder"); } }

        #endregion

        #region Dosya türleri

        /// <summary>Ayarlar menüsü.</summary>
        public static string Button_EditFileTypes { get { return Get("Button_EditFileTypes"); } }

        /// <summary>Pencere başlığı.</summary>
        public static string FileTypes_Title { get { return Get("FileTypes_Title"); } }

        /// <summary>Pencere ipucu.</summary>
        public static string FileTypes_Hint { get { return Get("FileTypes_Hint"); } }

        /// <summary>Sütun.</summary>
        public static string FileTypes_Name { get { return Get("FileTypes_Name"); } }

        /// <summary>Sütun.</summary>
        public static string FileTypes_Extensions { get { return Get("FileTypes_Extensions"); } }

        /// <summary>Sütun.</summary>
        public static string FileTypes_Group { get { return Get("FileTypes_Group"); } }

        /// <summary>Hata.</summary>
        public static string FileTypes_ErrorRow { get { return Get("FileTypes_ErrorRow"); } }

        /// <summary>Hata.</summary>
        public static string FileTypes_ErrorDuplicate { get { return Get("FileTypes_ErrorDuplicate"); } }

        /// <summary>Tanımsız uzantılar.</summary>
        public static string FileType_Other { get { return Get("FileType_Other"); } }

        #endregion

        #region Ayarlar

        /// <summary>Ayarlar penceresinin başlığı.</summary>
        public static string Settings_Title { get { return Get("Settings_Title"); } }

        /// <summary>Kök klasör başlığı.</summary>
        public static string Settings_Root { get { return Get("Settings_Root"); } }

        /// <summary>Kök klasör açıklaması.</summary>
        public static string Settings_RootHint { get { return Get("Settings_RootHint"); } }

        /// <summary>Tema başlığı.</summary>
        public static string Settings_Theme { get { return Get("Settings_Theme"); } }

        /// <summary>Şablon, tür ve dosya türü düğmelerinin başlığı.</summary>
        public static string Settings_Definitions { get { return Get("Settings_Definitions"); } }

        /// <summary>Kapat düğmesi.</summary>
        public static string Settings_Close { get { return Get("Settings_Close"); } }

        /// <summary>Tema adı.</summary>
        public static string Theme_FluentLight { get { return Get("Theme_FluentLight"); } }

        /// <summary>Tema adı.</summary>
        public static string Theme_FluentDark { get { return Get("Theme_FluentDark"); } }

        /// <summary>Tema adı.</summary>
        public static string Theme_Night { get { return Get("Theme_Night"); } }

        /// <summary>Tema adı.</summary>
        public static string Theme_Amber { get { return Get("Theme_Amber"); } }

        /// <summary>Tema adı.</summary>
        public static string Theme_Forest { get { return Get("Theme_Forest"); } }

        /// <summary>Tema adı.</summary>
        public static string Theme_Plum { get { return Get("Theme_Plum"); } }

        /// <summary>Tema adı.</summary>
        public static string Theme_Glacier { get { return Get("Theme_Glacier"); } }

        /// <summary>Tema adı.</summary>
        public static string Theme_Sand { get { return Get("Theme_Sand"); } }

        /// <summary>Tema adı.</summary>
        public static string Theme_Mint { get { return Get("Theme_Mint"); } }

        #endregion

        #region Kapak

        /// <summary>Kapak fotoğrafının köşesindeki düğme.</summary>
        public static string Cover_Open { get { return Get("Cover_Open"); } }

        #endregion

        #region Düzenle

        /// <summary>Ağaç ve tablo menüsü.</summary>
        public static string Menu_Edit { get { return Get("Menu_Edit"); } }

        /// <summary>Ağaç ve tablo menüsü.</summary>
        public static string Menu_Untrack { get { return Get("Menu_Untrack"); } }

        /// <summary>Düzenleme diyaloğu başlığı.</summary>
        public static string Edit_Title { get { return Get("Edit_Title"); } }

        /// <summary>Salt okunur numara.</summary>
        public static string Edit_Number { get { return Get("Edit_Number"); } }

        /// <summary>Numara açıklaması.</summary>
        public static string Edit_NumberHint { get { return Get("Edit_NumberHint"); } }

        /// <summary>Ad alanı.</summary>
        public static string Edit_Name { get { return Get("Edit_Name"); } }

        /// <summary>Stok şablonu alanı.</summary>
        public static string Edit_Template { get { return Get("Edit_Template"); } }

        /// <summary>Envanter alt türü alanı.</summary>
        public static string Edit_SubType { get { return Get("Edit_SubType"); } }

        /// <summary>Envanter kısa açıklaması.</summary>
        public static string Edit_Note { get { return Get("Edit_Note"); } }

        /// <summary>Kaydet düğmesi.</summary>
        public static string Edit_Save { get { return Get("Edit_Save"); } }

        /// <summary>Durum çubuğu.</summary>
        public static string Edit_Saved { get { return Get("Edit_Saved"); } }

        /// <summary>Onay başlığı.</summary>
        public static string Untrack_Title { get { return Get("Untrack_Title"); } }

        /// <summary>Onay metni.</summary>
        public static string Untrack_Text { get { return Get("Untrack_Text"); } }

        /// <summary>Onay düğmesi.</summary>
        public static string Untrack_Confirm { get { return Get("Untrack_Confirm"); } }

        /// <summary>Durum çubuğu.</summary>
        public static string Untrack_Done { get { return Get("Untrack_Done"); } }

        #endregion

        #region Sil

        /// <summary>Ağaç ve tablo menüsü.</summary>
        public static string Menu_Delete { get { return Get("Menu_Delete"); } }

        /// <summary>Silme onayı başlığı.</summary>
        public static string Delete_Title { get { return Get("Delete_Title"); } }

        /// <summary>Silme onayı metni.</summary>
        public static string Delete_Text { get { return Get("Delete_Text"); } }

        /// <summary>Onay kutusu ipucu.</summary>
        public static string Delete_TypeHint { get { return Get("Delete_TypeHint"); } }

        /// <summary>Sil düğmesi (ad doğru yazılınca görünür).</summary>
        public static string Delete_Confirm { get { return Get("Delete_Confirm"); } }

        /// <summary>Durum çubuğu.</summary>
        public static string Delete_NotTyped { get { return Get("Delete_NotTyped"); } }

        /// <summary>Durum çubuğu.</summary>
        public static string Delete_Done { get { return Get("Delete_Done"); } }

        /// <summary>Ayarlar düğmesi.</summary>
        public static string Settings_Trash { get { return Get("Settings_Trash"); } }

        #endregion

        #region Ayarlar

        /// <summary>Ayarlar başlığı.</summary>
        public static string Settings_NoteApp { get { return Get("Settings_NoteApp"); } }

        /// <summary>Ayarlar açıklaması.</summary>
        public static string Settings_NoteAppHint { get { return Get("Settings_NoteAppHint"); } }

        /// <summary>Seçenek.</summary>
        public static string NoteApp_Obsidian { get { return Get("NoteApp_Obsidian"); } }

        /// <summary>Seçenek.</summary>
        public static string NoteApp_VsCode { get { return Get("NoteApp_VsCode"); } }

        /// <summary>Seçenek.</summary>
        public static string NoteApp_Default { get { return Get("NoteApp_Default"); } }

        /// <summary>Durum çubuğu.</summary>
        public static string NoteApp_Missing { get { return Get("NoteApp_Missing"); } }

        #endregion

        #region Genel

        /// <summary>Stok Genel sayfası: md notlarındaki - [ ] maddeleri.</summary>
        public static string General_OpenTasks { get { return Get("General_OpenTasks"); } }

        #endregion

        #region Plan

        /// <summary>Plan türü.</summary>
        public static string Plan_Kiwi { get { return Get("Plan_Kiwi"); } }

        /// <summary>Adım sayısı.</summary>
        public static string Plan_Steps { get { return Get("Plan_Steps"); } }

        /// <summary>Limit sayısı.</summary>
        public static string Plan_Limits { get { return Get("Plan_Limits"); } }

        /// <summary>Plan revizyonu.</summary>
        public static string Plan_Revision { get { return Get("Plan_Revision"); } }

        /// <summary>Plana bağlı koşu sayısı.</summary>
        public static string Plan_Runs { get { return Get("Plan_Runs"); } }

        /// <summary>Koşu yok.</summary>
        public static string Plan_NoRuns { get { return Get("Plan_NoRuns"); } }

        /// <summary>Düğme ipucu.</summary>
        public static string Plan_Open { get { return Get("Plan_Open"); } }

        #endregion
    }
}
