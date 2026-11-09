using System;
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
        }

        /// <summary>Son seçilen projeler klasörü.</summary>
        [DataMember(Name = "rootPath", Order = 0)]
        public string RootPath { get; set; } = string.Empty;

        /// <summary>Seçili tema anahtarı (bkz. <see cref="Themes"/>); boşsa varsayılan.</summary>
        [DataMember(Name = "theme", Order = 1)]
        public string Theme { get; set; } = string.Empty;

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
}
