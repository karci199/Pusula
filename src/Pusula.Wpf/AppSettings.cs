using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using Pusula.Core.Tracking;

namespace Pusula.Wpf
{
    /// <summary>
    /// Kullanıcıya özel uygulama ayarları: <c>%LOCALAPPDATA%\Pusula\ayarlar.json</c>.
    /// </summary>
    [DataContract]
    public sealed class AppSettings
    {
        /// <summary>Serileştirici kurucuyu çağırmaz; dosyada olmayan alanlar boş değerle başlar.</summary>
        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
            RootPath = string.Empty;
            Theme = string.Empty;
            NoteApp = NoteOpener.Pusula;
            TabOrder = new List<string>();
            Columns = new Dictionary<string, List<ColumnLayout>>();
        }

        /// <summary>Son seçilen projeler klasörü.</summary>
        [DataMember(Name = "rootPath", Order = 0)]
        public string RootPath { get; set; } = string.Empty;

        /// <summary>Seçili tema anahtarı (bkz. <see cref="Themes"/>); boşsa varsayılan.</summary>
        [DataMember(Name = "theme", Order = 1)]
        public string Theme { get; set; } = string.Empty;

        /// <summary>.md notlarını açan program: <c>pusula</c> (yerleşik), <c>obsidian</c>, <c>vscode</c> ya da <c>default</c> (bkz. <see cref="NoteOpener"/>).</summary>
        [DataMember(Name = "noteApp", Order = 2)]
        public string NoteApp { get; set; } = NoteOpener.Pusula;

        /// <summary>Stok sekmelerinin sırası (sekme etiketleri: <c>tests</c>, <c>inventory:jig</c> …); boşsa varsayılan.</summary>
        [DataMember(Name = "tabOrder", Order = 3)]
        public List<string> TabOrder { get; set; } = new List<string>();

        /// <summary>Tabloların sütun sırası ve genişliği (tablo adı → sütunlar); yoksa varsayılan.</summary>
        [DataMember(Name = "columns", Order = 4)]
        public Dictionary<string, List<ColumnLayout>> Columns { get; set; } = new Dictionary<string, List<ColumnLayout>>();

        /// <summary>Ana pencerenin son yeri ve boyutu; yoksa varsayılan (ortada, 1380×860).</summary>
        [DataMember(Name = "window", Order = 5, EmitDefaultValue = false)]
        public WindowBounds? Window { get; set; }

        /// <summary>md not penceresinin ayarları (görünüm, yazı tipi, genişlik …); yoksa varsayılan.</summary>
        [DataMember(Name = "noteEditor", Order = 6, EmitDefaultValue = false)]
        public NoteEditorSettings? NoteEditor { get; set; }

        /// <summary>Ayar dosyasının yerini değiştiren ortam değişkeni (testler gerçek ayarlara dokunmasın diye).</summary>
        public const string PathVariable = "PUSULA_AYARLAR";

        private static string FilePath
        {
            get
            {
                var custom = Environment.GetEnvironmentVariable(PathVariable);
                if (!string.IsNullOrEmpty(custom))
                {
                    return custom!;
                }

                var folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                return Path.Combine(folder, "Pusula", "ayarlar.json");
            }
        }

        /// <summary>Ayarları okur; dosya yoksa ya da bozuksa varsayılanı döner.</summary>
        public static AppSettings Load()
        {
            return JsonFile.TryRead<AppSettings>(FilePath) ?? new AppSettings();
        }

        /// <summary>Ayarları yazar. Yazılamazsa sessizce geçer; ayar kaybı uygulamayı durdurmamalı.</summary>
        public void Save()
        {
            try
            {
                JsonFile.WriteAtomic(FilePath, this);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    /// <summary>md not penceresinin ayarları.</summary>
    [DataContract]
    public sealed class NoteEditorSettings
    {
        /// <summary>Görünüm: <c>edit</c> (düz), <c>split</c> (yan yana), <c>preview</c> (görünüm).</summary>
        [DataMember(Name = "view", Order = 0)]
        public string View { get; set; } = "split";

        /// <summary>Düz görünüm yazı tipi.</summary>
        [DataMember(Name = "editorFont", Order = 1)]
        public string EditorFont { get; set; } = "Cascadia Mono";

        /// <summary>Düz görünüm yazı boyutu.</summary>
        [DataMember(Name = "editorSize", Order = 2)]
        public double EditorSize { get; set; } = 14;

        /// <summary>Görünüm yazı boyutu.</summary>
        [DataMember(Name = "previewSize", Order = 3)]
        public double PreviewSize { get; set; } = 15;

        /// <summary>Okuma genişliği (piksel); 0 tam genişlik.</summary>
        [DataMember(Name = "readingWidth", Order = 4)]
        public double ReadingWidth { get; set; } = 860;

        /// <summary>Düz görünümde uzun satırlar kaydırılsın mı.</summary>
        [DataMember(Name = "wrap", Order = 5)]
        public bool Wrap { get; set; } = true;

        /// <summary>Tek satır sonu görünümde yeni satır mı (Obsidian varsayılanı).</summary>
        [DataMember(Name = "lineBreaks", Order = 6)]
        public bool LineBreaks { get; set; } = true;

        /// <summary>YAML başlığı görünümde özellik kutusu olarak gösterilsin mi.</summary>
        [DataMember(Name = "frontMatter", Order = 7)]
        public bool FrontMatter { get; set; } = true;

        /// <summary>Yazmayı bırakınca 2 sn sonra kendiliğinden kaydet.</summary>
        [DataMember(Name = "autoSave", Order = 8)]
        public bool AutoSave { get; set; }

        /// <summary>Pencere tam ekran (ekranı kaplar) açılsın mı.</summary>
        [DataMember(Name = "maximized", Order = 9)]
        public bool Maximized { get; set; } = true;

        /// <summary>Yan yana görünümde düz bölümün oranı (0.2–0.8).</summary>
        [DataMember(Name = "splitRatio", Order = 10)]
        public double SplitRatio { get; set; } = 0.5;
    }

    /// <summary>Bir tablo sütununun kayıtlı yeri ve genişliği.</summary>
    [DataContract]
    public sealed class ColumnLayout
    {
        /// <summary>Sütunun tasarımdaki sırası (kimliği).</summary>
        [DataMember(Name = "index", Order = 0)]
        public int Index { get; set; }

        /// <summary>Genişlik (piksel).</summary>
        [DataMember(Name = "width", Order = 1)]
        public double Width { get; set; }

        /// <summary>Sütunun anahtarı (başlığın Tag'i); araya sütun eklense de doğru sütunu bulur. Eski kayıtlarda boş.</summary>
        [DataMember(Name = "key", Order = 2, EmitDefaultValue = false)]
        public string? Key { get; set; }
    }

    /// <summary>Pencerenin yeri ve boyutu (mantıksal piksel).</summary>
    [DataContract]
    public sealed class WindowBounds
    {
        /// <summary>Sol.</summary>
        [DataMember(Name = "left", Order = 0)]
        public double Left { get; set; }

        /// <summary>Üst.</summary>
        [DataMember(Name = "top", Order = 1)]
        public double Top { get; set; }

        /// <summary>Genişlik.</summary>
        [DataMember(Name = "width", Order = 2)]
        public double Width { get; set; }

        /// <summary>Yükseklik.</summary>
        [DataMember(Name = "height", Order = 3)]
        public double Height { get; set; }

        /// <summary>Tam ekran (büyütülmüş) mü.</summary>
        [DataMember(Name = "maximized", Order = 4)]
        public bool Maximized { get; set; }
    }
}