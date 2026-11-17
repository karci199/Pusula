using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace MeDitor
{
    /// <summary>
    /// Kullanıcıya özel ayarlar: <c>%LOCALAPPDATA%\MeDitor\ayarlar.json</c>. Düzenleyici ayarları, tema, son açılan
    /// dosyalar, son klasör ve açık sekmeler (oturum).
    /// </summary>
    [DataContract]
    public sealed class AppSettings
    {
        /// <summary>Son açılanlar listesinin en fazla uzunluğu.</summary>
        public const int RecentLimit = 15;

        /// <summary>Serileştirici kurucuyu çağırmaz; dosyada olmayan alanlar varsayılanla başlar.</summary>
        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
            Theme = string.Empty;
            Recent = new List<string>();
            Folder = string.Empty;
            Session = new List<string>();
            ActiveFile = string.Empty;
            ShowSidebar = true;
            SidebarWidth = 260;
        }

        /// <summary>Düzenleyici ayarları (görünüm, yazı tipi, genişlik …); yoksa varsayılan.</summary>
        [DataMember(Name = "editor", Order = 0, EmitDefaultValue = false)]
        public EditorSettings? Editor { get; set; }

        /// <summary>Seçili tema anahtarı (bkz. <see cref="Themes"/>); boşsa varsayılan.</summary>
        [DataMember(Name = "theme", Order = 1)]
        public string Theme { get; set; } = string.Empty;

        /// <summary>Son açılan dosyalar (en yeni başta).</summary>
        [DataMember(Name = "recent", Order = 2)]
        public List<string> Recent { get; set; } = new List<string>();

        /// <summary>Kenar çubuğunda açık klasör; yoksa boş.</summary>
        [DataMember(Name = "folder", Order = 3)]
        public string Folder { get; set; } = string.Empty;

        /// <summary>Kapanışta açık olan sekmeler (yeniden açılınca geri gelir).</summary>
        [DataMember(Name = "session", Order = 4)]
        public List<string> Session { get; set; } = new List<string>();

        /// <summary>Kapanışta seçili sekmenin dosyası.</summary>
        [DataMember(Name = "activeFile", Order = 5)]
        public string ActiveFile { get; set; } = string.Empty;

        /// <summary>Kenar çubuğu görünür mü.</summary>
        [DataMember(Name = "showSidebar", Order = 6)]
        public bool ShowSidebar { get; set; } = true;

        /// <summary>Kenar çubuğu genişliği (piksel).</summary>
        [DataMember(Name = "sidebarWidth", Order = 7)]
        public double SidebarWidth { get; set; } = 260;

        /// <summary>Ayar dosyasının yolu.</summary>
        public static string FilePath
        {
            get
            {
                // Testler kendi geçici ayar dosyasını verir (MEDITOR_AYARLAR); kullanıcının ayarlarına dokunulmaz.
                var test = Environment.GetEnvironmentVariable("MEDITOR_AYARLAR");
                return string.IsNullOrEmpty(test) ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MeDitor", "ayarlar.json") : test!;
            }
        }

        /// <summary>Ayarları okur; dosya yoksa ya da bozuksa varsayılan.</summary>
        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    using (var stream = File.OpenRead(FilePath))
                    {
                        var loaded = new DataContractJsonSerializer(typeof(AppSettings)).ReadObject(stream) as AppSettings;
                        if (loaded != null)
                        {
                            return loaded;
                        }
                    }
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (SerializationException)
            {
            }

            return new AppSettings();
        }

        /// <summary>Ayarları atomik yazar (geçici dosya + yeniden adlandırma); yazılamazsa sessizce geçer.</summary>
        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
                var temp = FilePath + ".tmp";
                using (var stream = File.Create(temp))
                {
                    using (var writer = JsonReaderWriterFactory.CreateJsonWriter(stream, Encoding.UTF8, true, true, "  "))
                    {
                        new DataContractJsonSerializer(typeof(AppSettings)).WriteObject(writer, this);
                    }
                }

                if (File.Exists(FilePath))
                {
                    File.Replace(temp, FilePath, null);
                }
                else
                {
                    File.Move(temp, FilePath);
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        /// <summary>Dosyayı son açılanların başına alır (tekrarsız, en fazla <see cref="RecentLimit"/>).</summary>
        public void AddRecent(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            Recent.RemoveAll(p => string.Equals(p, path, StringComparison.OrdinalIgnoreCase));
            Recent.Insert(0, path);
            if (Recent.Count > RecentLimit)
            {
                Recent = Recent.Take(RecentLimit).ToList();
            }
        }
    }
    /// <summary>Düzenleyicinin ayarları.</summary>
    [DataContract]
    public sealed class EditorSettings
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

        /// <summary>Aramada büyük / küçük harf ayrılsın mı.</summary>
        [DataMember(Name = "findMatchCase", Order = 11)]
        public bool FindMatchCase { get; set; }

        /// <summary>Aramada yalnız tam kelime.</summary>
        [DataMember(Name = "findWholeWord", Order = 12)]
        public bool FindWholeWord { get; set; }

        /// <summary>Aramada düzenli ifade.</summary>
        [DataMember(Name = "findRegex", Order = 13)]
        public bool FindRegex { get; set; }

        /// <summary>Yan yana görünümde iki taraf birlikte kaysın mı.</summary>
        [DataMember(Name = "syncScroll", Order = 14)]
        public bool SyncScroll { get; set; } = true;

        /// <summary>Başlık paneli (içindekiler) açık mı.</summary>
        [DataMember(Name = "showOutline", Order = 15)]
        public bool ShowOutline { get; set; }

        /// <summary>Parantez, tırnak ve işaret çiftleri kendiliğinden kapansın mı.</summary>
        [DataMember(Name = "autoPair", Order = 16)]
        public bool AutoPair { get; set; } = true;

        // Eski ayar dosyasında olmayan alanlar varsayılanla gelsin (serileştirici yapıcıyı çalıştırmaz).
        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
            View = "split";
            EditorFont = "Cascadia Mono";
            EditorSize = 14;
            PreviewSize = 15;
            ReadingWidth = 860;
            Wrap = true;
            LineBreaks = true;
            FrontMatter = true;
            Maximized = true;
            SplitRatio = 0.5;
            SyncScroll = true;
            AutoPair = true;
        }
    }
}
