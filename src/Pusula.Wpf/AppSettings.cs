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
            NoteApp = NoteOpener.Obsidian;
            TabOrder = new List<string>();
            Columns = new Dictionary<string, List<ColumnLayout>>();
        }

        /// <summary>Son seçilen projeler klasörü.</summary>
        [DataMember(Name = "rootPath", Order = 0)]
        public string RootPath { get; set; } = string.Empty;

        /// <summary>Seçili tema anahtarı (bkz. <see cref="Themes"/>); boşsa varsayılan.</summary>
        [DataMember(Name = "theme", Order = 1)]
        public string Theme { get; set; } = string.Empty;

        /// <summary>.md notlarını açan program: <c>obsidian</c>, <c>vscode</c> ya da <c>default</c> (bkz. <see cref="NoteOpener"/>).</summary>
        [DataMember(Name = "noteApp", Order = 2)]
        public string NoteApp { get; set; } = NoteOpener.Obsidian;

        /// <summary>Stok sekmelerinin sırası (sekme etiketleri: <c>tests</c>, <c>inventory:jig</c> …); boşsa varsayılan.</summary>
        [DataMember(Name = "tabOrder", Order = 3)]
        public List<string> TabOrder { get; set; } = new List<string>();

        /// <summary>Tabloların sütun sırası ve genişliği (tablo adı → sütunlar); yoksa varsayılan.</summary>
        [DataMember(Name = "columns", Order = 4)]
        public Dictionary<string, List<ColumnLayout>> Columns { get; set; } = new Dictionary<string, List<ColumnLayout>>();

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
    }
}