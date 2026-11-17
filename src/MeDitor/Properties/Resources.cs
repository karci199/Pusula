using System.Globalization;
using System.Resources;

namespace MeDitor.Properties
{
    /// <summary>
    /// Arayüz metinlerine tür güvenli erişim (<c>Resources.resx</c>).
    /// Komut satırı MSBuild'de de derlensin diye elle yazılmıştır; resx'e anahtar eklenince buraya da eklenir.
    /// </summary>
    public static class Resources
    {
        private static ResourceManager? resourceManager;
        private static CultureInfo? resourceCulture;

        /// <summary>Bu sınıfın kullandığı <see cref="System.Resources.ResourceManager"/>.</summary>
        public static ResourceManager ResourceManager
        {
            get
            {
                if (resourceManager == null)
                {
                    resourceManager = new ResourceManager("MeDitor.Properties.Resources", typeof(Resources).Assembly);
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

        /// <summary>Diyaloğun vazgeç düğmesi.</summary>
        public static string Dialog_Cancel { get { return Get("Dialog_Cancel"); } }

        /// <summary>Kaydet düğmesi.</summary>
        public static string Edit_Save { get { return Get("Edit_Save"); } }

        /// <summary>Ayar.</summary>
        public static string Note_AutoPair { get { return Get("Note_AutoPair"); } }

        /// <summary>Ayar: yazmayı bırakınca 2 sn sonra.</summary>
        public static string Note_AutoSave { get { return Get("Note_AutoSave"); } }

        /// <summary>Araç çubuğu.</summary>
        public static string Note_Bold { get { return Get("Note_Bold"); } }

        /// <summary>Araç çubuğu.</summary>
        public static string Note_Callout { get { return Get("Note_Callout"); } }

        /// <summary>Eklenen uyarı kutusunun başlığı.</summary>
        public static string Note_CalloutTitle { get { return Get("Note_CalloutTitle"); } }

        /// <summary>Durum: imleç.</summary>
        public static string Note_Caret { get { return Get("Note_Caret"); } }

        /// <summary>Kapatırken: değişikliği at.</summary>
        public static string Note_CloseDiscard { get { return Get("Note_CloseDiscard"); } }

        /// <summary>Kapatırken soru.</summary>
        public static string Note_CloseText { get { return Get("Note_CloseText"); } }

        /// <summary>Kapatırken soru başlığı.</summary>
        public static string Note_CloseTitle { get { return Get("Note_CloseTitle"); } }

        /// <summary>Araç çubuğu.</summary>
        public static string Note_Code { get { return Get("Note_Code"); } }

        /// <summary>Araç çubuğu.</summary>
        public static string Note_Comment { get { return Get("Note_Comment"); } }

        /// <summary>Araç çubuğu.</summary>
        public static string Note_Divider { get { return Get("Note_Divider"); } }

        /// <summary>Ayar.</summary>
        public static string Note_EditorFont { get { return Get("Note_EditorFont"); } }

        /// <summary>Ayar.</summary>
        public static string Note_EditorSize { get { return Get("Note_EditorSize"); } }

        /// <summary>Dış değişiklik uyarısı.</summary>
        public static string Note_ExternalChanged { get { return Get("Note_ExternalChanged"); } }

        /// <summary>Bul çubuğu.</summary>
        public static string Note_FindClose { get { return Get("Note_FindClose"); } }

        /// <summary>Eşleşme sırası / toplam.</summary>
        public static string Note_FindCount { get { return Get("Note_FindCount"); } }

        /// <summary>Büyük / küçük harf düğmesindeki yazı.</summary>
        public static string Note_FindGlyphCase { get { return Get("Note_FindGlyphCase"); } }

        /// <summary>Düzenli ifade düğmesindeki yazı.</summary>
        public static string Note_FindGlyphRegex { get { return Get("Note_FindGlyphRegex"); } }

        /// <summary>Tam kelime düğmesindeki yazı.</summary>
        public static string Note_FindGlyphWord { get { return Get("Note_FindGlyphWord"); } }

        /// <summary>Bul seçeneği.</summary>
        public static string Note_FindMatchCase { get { return Get("Note_FindMatchCase"); } }

        /// <summary>Bul çubuğu.</summary>
        public static string Note_FindNext { get { return Get("Note_FindNext"); } }

        /// <summary>Eşleşme yok.</summary>
        public static string Note_FindNone { get { return Get("Note_FindNone"); } }

        /// <summary>Bul kutusu yer tutucu.</summary>
        public static string Note_FindPlaceholder { get { return Get("Note_FindPlaceholder"); } }

        /// <summary>Bul çubuğu.</summary>
        public static string Note_FindPrevious { get { return Get("Note_FindPrevious"); } }

        /// <summary>Bul seçeneği.</summary>
        public static string Note_FindRegex { get { return Get("Note_FindRegex"); } }

        /// <summary>Bul çubuğu.</summary>
        public static string Note_FindToggleReplace { get { return Get("Note_FindToggleReplace"); } }

        /// <summary>Toplam eşleşme.</summary>
        public static string Note_FindTotal { get { return Get("Note_FindTotal"); } }

        /// <summary>Bul seçeneği.</summary>
        public static string Note_FindWholeWord { get { return Get("Note_FindWholeWord"); } }

        /// <summary>Ayar.</summary>
        public static string Note_FrontMatter { get { return Get("Note_FrontMatter"); } }

        /// <summary>Araç çubuğu.</summary>
        public static string Note_FullScreen { get { return Get("Note_FullScreen"); } }

        /// <summary>Başlık düğmesindeki harf.</summary>
        public static string Note_GlyphHeading { get { return Get("Note_GlyphHeading"); } }

        /// <summary>Üstü çizili düğmesindeki harf.</summary>
        public static string Note_GlyphStrike { get { return Get("Note_GlyphStrike"); } }

        /// <summary>Araç çubuğu.</summary>
        public static string Note_Heading { get { return Get("Note_Heading"); } }

        /// <summary>Araç çubuğu.</summary>
        public static string Note_Highlight { get { return Get("Note_Highlight"); } }

        /// <summary>Dış değişiklik: yok say.</summary>
        public static string Note_KeepMine { get { return Get("Note_KeepMine"); } }

        /// <summary>Ayar.</summary>
        public static string Note_LineBreaks { get { return Get("Note_LineBreaks"); } }

        /// <summary>Araç çubuğu.</summary>
        public static string Note_Link { get { return Get("Note_Link"); } }

        /// <summary>Durum: bağlantı hedefi yok.</summary>
        public static string Note_LinkMissing { get { return Get("Note_LinkMissing"); } }

        /// <summary>Araç çubuğu.</summary>
        public static string Note_List { get { return Get("Note_List"); } }

        /// <summary>Durum: görünümde bulunamayan resim / dosya.</summary>
        public static string Note_MissingFiles { get { return Get("Note_MissingFiles"); } }

        /// <summary>Tablo yokken durum çubuğu.</summary>
        public static string Note_NoTable { get { return Get("Note_NoTable"); } }

        /// <summary>Araç çubuğu.</summary>
        public static string Note_NumberList { get { return Get("Note_NumberList"); } }

        /// <summary>Notu dışarıda aç.</summary>
        public static string Note_OpenExternal { get { return Get("Note_OpenExternal"); } }

        /// <summary>Notun klasörünü aç.</summary>
        public static string Note_OpenFolder { get { return Get("Note_OpenFolder"); } }

        /// <summary>Başlık paneli başlığı.</summary>
        public static string Note_Outline { get { return Get("Note_Outline"); } }

        /// <summary>Başlık paneli başlığı ve sayı.</summary>
        public static string Note_OutlineCount { get { return Get("Note_OutlineCount"); } }

        /// <summary>Başlık yokken.</summary>
        public static string Note_OutlineEmpty { get { return Get("Note_OutlineEmpty"); } }

        /// <summary>Araç çubuğu.</summary>
        public static string Note_OutlineToggle { get { return Get("Note_OutlineToggle"); } }

        /// <summary>Ayar.</summary>
        public static string Note_PreviewSize { get { return Get("Note_PreviewSize"); } }

        /// <summary>Araç çubuğu.</summary>
        public static string Note_Quote { get { return Get("Note_Quote"); } }

        /// <summary>Dış değişiklik: yeniden yükle.</summary>
        public static string Note_Reload { get { return Get("Note_Reload"); } }

        /// <summary>Değiştir düğmesi.</summary>
        public static string Note_Replace { get { return Get("Note_Replace"); } }

        /// <summary>Tümünü değiştir düğmesi.</summary>
        public static string Note_ReplaceAll { get { return Get("Note_ReplaceAll"); } }

        /// <summary>Tümünü değiştir ipucu.</summary>
        public static string Note_ReplaceAllHint { get { return Get("Note_ReplaceAllHint"); } }

        /// <summary>Tümünü değiştir sonucu.</summary>
        public static string Note_ReplacedCount { get { return Get("Note_ReplacedCount"); } }

        /// <summary>Değiştir ipucu.</summary>
        public static string Note_ReplaceHint { get { return Get("Note_ReplaceHint"); } }

        /// <summary>Değiştir kutusu yer tutucu.</summary>
        public static string Note_ReplacePlaceholder { get { return Get("Note_ReplacePlaceholder"); } }

        /// <summary>Kaydet düğmesi.</summary>
        public static string Note_Save { get { return Get("Note_Save"); } }

        /// <summary>Durum: kayıt saati.</summary>
        public static string Note_Saved { get { return Get("Note_Saved"); } }

        /// <summary>Durum.</summary>
        public static string Note_SaveError { get { return Get("Note_SaveError"); } }

        /// <summary>Ayarlar düğmesi.</summary>
        public static string Note_Settings { get { return Get("Note_Settings"); } }

        /// <summary>Ayar.</summary>
        public static string Note_StartMaximized { get { return Get("Note_StartMaximized"); } }

        /// <summary>Araç çubuğu.</summary>
        public static string Note_Strike { get { return Get("Note_Strike"); } }

        /// <summary>Ayar.</summary>
        public static string Note_SyncScroll { get { return Get("Note_SyncScroll"); } }

        /// <summary>Araç çubuğu.</summary>
        public static string Note_Table { get { return Get("Note_Table"); } }

        /// <summary>Eklenen tablo şablonunun başlık hücresi.</summary>
        public static string Note_TableHeader { get { return Get("Note_TableHeader"); } }

        /// <summary>Araç çubuğu.</summary>
        public static string Note_Task { get { return Get("Note_Task"); } }

        /// <summary>Durum.</summary>
        public static string Note_Unsaved { get { return Get("Note_Unsaved"); } }

        /// <summary>Görünüm: yalnız md metni.</summary>
        public static string Note_ViewEdit { get { return Get("Note_ViewEdit"); } }

        /// <summary>Görünüm: yalnız biçimli görünüm.</summary>
        public static string Note_ViewPreview { get { return Get("Note_ViewPreview"); } }

        /// <summary>Görünüm: metin ve görünüm yan yana.</summary>
        public static string Note_ViewSplit { get { return Get("Note_ViewSplit"); } }

        /// <summary>Ayar.</summary>
        public static string Note_Width { get { return Get("Note_Width"); } }

        /// <summary>Ayar seçeneği.</summary>
        public static string Note_WidthFull { get { return Get("Note_WidthFull"); } }

        /// <summary>Okuma genişliği seçeneği.</summary>
        public static string Note_WidthPx { get { return Get("Note_WidthPx"); } }

        /// <summary>Not penceresi başlığı: dosya adı ve kaydedilmemiş işareti.</summary>
        public static string Note_WindowTitle { get { return Get("Note_WindowTitle"); } }

        /// <summary>Durum: kelime sayısı.</summary>
        public static string Note_Words { get { return Get("Note_Words"); } }

        /// <summary>Ayar.</summary>
        public static string Note_Wrap { get { return Get("Note_Wrap"); } }

        /// <summary>Düzenli ifade hatası.</summary>
        public static string Note_FindInvalid { get { return Get("Note_FindInvalid"); } }

        /// <summary>Araç çubuğu.</summary>
        public static string Note_Image { get { return Get("Note_Image"); } }

        /// <summary>Durum.</summary>
        public static string Note_ImageCopied { get { return Get("Note_ImageCopied"); } }

        /// <summary>Durum: görünümdeki resim sayısı.</summary>
        public static string Note_Images { get { return Get("Note_Images"); } }

        /// <summary>Resim seçme penceresinin süzgeci.</summary>
        public static string Note_ImagesFilter { get { return Get("Note_ImagesFilter"); } }

        /// <summary>Araç çubuğu.</summary>
        public static string Note_Italic { get { return Get("Note_Italic"); } }

        /// <summary>Uygulama adı.</summary>
        public static string App_Name { get { return Get("App_Name"); } }

        /// <summary>Pencere başlığı; {0} seçili sekme.</summary>
        public static string Main_WindowTitle { get { return Get("Main_WindowTitle"); } }

        /// <summary>Kaydedilmemiş notun adı.</summary>
        public static string Main_Untitled { get { return Get("Main_Untitled"); } }

        /// <summary>Menü: Dosya.</summary>
        public static string Main_File { get { return Get("Main_File"); } }

        /// <summary>Yeni adsız not.</summary>
        public static string Main_New { get { return Get("Main_New"); } }

        /// <summary>Dosya aç.</summary>
        public static string Main_Open { get { return Get("Main_Open"); } }

        /// <summary>Kenar çubuğunda klasör aç.</summary>
        public static string Main_OpenFolder { get { return Get("Main_OpenFolder"); } }

        /// <summary>Son açılan dosyalar.</summary>
        public static string Main_Recent { get { return Get("Main_Recent"); } }

        /// <summary>Son açılanlar boş.</summary>
        public static string Main_RecentEmpty { get { return Get("Main_RecentEmpty"); } }

        /// <summary>Son açılanları temizle.</summary>
        public static string Main_RecentClear { get { return Get("Main_RecentClear"); } }

        /// <summary>Kaydet.</summary>
        public static string Main_Save { get { return Get("Main_Save"); } }

        /// <summary>Farklı kaydet.</summary>
        public static string Main_SaveAs { get { return Get("Main_SaveAs"); } }

        /// <summary>Bütün sekmeleri kaydet.</summary>
        public static string Main_SaveAll { get { return Get("Main_SaveAll"); } }

        /// <summary>HTML dışa aktarma.</summary>
        public static string Main_ExportHtml { get { return Get("Main_ExportHtml"); } }

        /// <summary>Yazdırma (tarayıcıda; PDF olarak da kaydedilir).</summary>
        public static string Main_Print { get { return Get("Main_Print"); } }

        /// <summary>Sekmeyi kapat.</summary>
        public static string Main_CloseTab { get { return Get("Main_CloseTab"); } }

        /// <summary>Uygulamadan çık.</summary>
        public static string Main_Exit { get { return Get("Main_Exit"); } }

        /// <summary>Menü: Görünüm.</summary>
        public static string Main_View { get { return Get("Main_View"); } }

        /// <summary>Kenar çubuğunu göster / gizle.</summary>
        public static string Main_Sidebar { get { return Get("Main_Sidebar"); } }

        /// <summary>Tema seçimi.</summary>
        public static string Main_Theme { get { return Get("Main_Theme"); } }

        /// <summary>Kenar çubuğu başlığı (klasör yok).</summary>
        public static string Main_NoFolder { get { return Get("Main_NoFolder"); } }

        /// <summary>Kenar çubuğu boşken açıklama.</summary>
        public static string Main_NoFolderHint { get { return Get("Main_NoFolderHint"); } }

        /// <summary>Kenar çubuğunu yenile.</summary>
        public static string Main_RefreshFolder { get { return Get("Main_RefreshFolder"); } }

        /// <summary>Kenar çubuğundaki klasörü kapat.</summary>
        public static string Main_CloseFolder { get { return Get("Main_CloseFolder"); } }

        /// <summary>Hiç sekme yokken açıklama.</summary>
        public static string Main_EmptyText { get { return Get("Main_EmptyText"); } }

        /// <summary>Aç / kaydet penceresi süzgeci.</summary>
        public static string Main_MdFilter { get { return Get("Main_MdFilter"); } }

        /// <summary>Dışa aktarma süzgeci.</summary>
        public static string Main_HtmlFilter { get { return Get("Main_HtmlFilter"); } }

        /// <summary>Dışa aktarma bitti; {0} dosya.</summary>
        public static string Main_Exported { get { return Get("Main_Exported"); } }

        /// <summary>Öneri türü: not.</summary>
        public static string Main_KindNote { get { return Get("Main_KindNote"); } }

        /// <summary>Öneri türü: etiket.</summary>
        public static string Main_KindTag { get { return Get("Main_KindTag"); } }

        /// <summary>Kısayol metni.</summary>
        public static string Gesture_New { get { return Get("Gesture_New"); } }

        /// <summary>Kısayol metni.</summary>
        public static string Gesture_Open { get { return Get("Gesture_Open"); } }

        /// <summary>Kısayol metni (sekme yokken).</summary>
        public static string Gesture_OpenFolder { get { return Get("Gesture_OpenFolder"); } }

        /// <summary>Kısayol metni.</summary>
        public static string Gesture_Save { get { return Get("Gesture_Save"); } }

        /// <summary>Kısayol metni.</summary>
        public static string Gesture_SaveAs { get { return Get("Gesture_SaveAs"); } }

        /// <summary>Kısayol metni.</summary>
        public static string Gesture_Print { get { return Get("Gesture_Print"); } }

        /// <summary>Kısayol metni.</summary>
        public static string Gesture_CloseTab { get { return Get("Gesture_CloseTab"); } }

        /// <summary>Kısayol metni.</summary>
        public static string Gesture_Sidebar { get { return Get("Gesture_Sidebar"); } }

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

        private static string Get(string name)
        {
            return ResourceManager.GetString(name, resourceCulture) ?? name;
        }
    }
}