using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;

namespace Pusula.Core.Tracking
{
    /// <summary>
    /// Küçük JSON dosyalarını okur ve atomik olarak yazar (geçici dosya + yeniden adlandırma).
    /// .NET Framework'ün kendi serileştiricisini kullanır; ek paket gerekmez.
    /// </summary>
    public static class JsonFile
    {
        /// <summary>
        /// Dosyayı okur. Dosya yoksa ya da bozuksa <c>null</c> döner, hata fırlatmaz.
        /// </summary>
        public static T? TryRead<T>(string path) where T : class
        {
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                using (var stream = File.OpenRead(path))
                {
                    var serializer = new DataContractJsonSerializer(typeof(T));
                    return serializer.ReadObject(stream) as T;
                }
            }
            catch (IOException)
            {
                return null;
            }
            catch (System.Runtime.Serialization.SerializationException)
            {
                return null;
            }
        }

        /// <summary>
        /// Değeri girintili JSON olarak yazar. Önce geçici dosyaya yazılır, sonra yerine taşınır;
        /// yazma yarıda kesilirse eski dosya bozulmaz.
        /// </summary>
        public static void WriteAtomic<T>(string path, T value)
        {
            var folder = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(folder))
            {
                Directory.CreateDirectory(folder);
            }

            Pusula.Core.Scanning.DiskWatcher.MarkOwnWrite();
            var tempPath = path + ".tmp";
            using (var stream = File.Create(tempPath))
            using (var writer = JsonReaderWriterFactory.CreateJsonWriter(stream, new UTF8Encoding(false), true, true, "  "))
            {
                var serializer = new DataContractJsonSerializer(typeof(T));
                serializer.WriteObject(writer, value);
                writer.Flush();
            }

            if (File.Exists(path))
            {
                File.Replace(tempPath, path, null);
            }
            else
            {
                File.Move(tempPath, path);
            }
        }

        /// <summary>
        /// Oku – değiştir – atomik yaz (25.11.2026; dört yerde tekrar eden kalıp). Dosya okunamazsa ya da
        /// <paramref name="change"/> <c>false</c> dönerse yazılmaz. Başarıda boş, aksi hâlde hata metni döner.
        /// </summary>
        public static string Update<T>(string path, System.Func<T, bool> change) where T : class
        {
            var value = TryRead<T>(path);
            if (value == null)
            {
                return "okunamadı: " + path;
            }

            if (!change(value))
            {
                return string.Empty;
            }

            try
            {
                WriteAtomic(path, value);
                return string.Empty;
            }
            catch (IOException ex)
            {
                return ex.Message;
            }
            catch (System.UnauthorizedAccessException ex)
            {
                return ex.Message;
            }
        }
    }
}
