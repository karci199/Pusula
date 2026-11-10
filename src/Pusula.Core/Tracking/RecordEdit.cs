using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Pusula.Core.Inventory;
using Pusula.Core.Types;

namespace Pusula.Core.Tracking
{
    /// <summary>Takip edilen kaydın çeşidi.</summary>
    public enum RecordKind
    {
        /// <summary>Proje (<c>_TAKIP\proje.json</c>).</summary>
        Project,

        /// <summary>Stok (<c>_TAKIP\stok.json</c>).</summary>
        Stock,

        /// <summary>Envanter kaydı: jig, kablo … (<c>_TAKIP\kayit.json</c>).</summary>
        Inventory,
    }

    /// <summary>
    /// Proje, stok ve envanter kayıtları için ortak düzenleme ve takipten çıkarma. Yalnızca takip dosyası değişir;
    /// klasöre, notlara ve belgelere dokunulmaz. Numara (klasör adı) burada değiştirilmez.
    /// </summary>
    public static class RecordEdit
    {
        /// <summary>Takipten çıkarılan dosyanın adına eklenen ek (<c>stok.json.kaldirildi-20261109-1930</c>).</summary>
        public const string RemovedSuffix = ".kaldirildi-";

        /// <summary>Kaydın takip dosyası.</summary>
        public static string TrackingFile(string folder, RecordKind kind)
        {
            switch (kind)
            {
                case RecordKind.Project:
                    return TrackingFiles.ProjectFile(folder);
                case RecordKind.Stock:
                    return TrackingFiles.StockFile(folder);
                default:
                    return InventoryService.RecordFile(folder);
            }
        }

        /// <summary>Projenin adını değiştirir. Hata metni; başarıda boş.</summary>
        public static string EditProject(string folder, string name)
        {
            var path = TrackingFiles.ProjectFile(folder);
            var info = JsonFile.TryRead<ProjectInfo>(path);
            if (info == null)
            {
                return path;
            }

            info.Name = (name ?? string.Empty).Trim();
            return Write(path, info);
        }

        /// <summary>Stokun adını ve şablonunu değiştirir. Hata metni; başarıda boş.</summary>
        public static string EditStock(string folder, string name, string template)
        {
            var path = TrackingFiles.StockFile(folder);
            var info = JsonFile.TryRead<StockInfo>(path);
            if (info == null)
            {
                return path;
            }

            info.Name = (name ?? string.Empty).Trim();
            info.Template = (template ?? string.Empty).Trim();
            return Write(path, info);
        }

        /// <summary>Envanter kaydının adını, alt türünü, kısa açıklamasını, alan değerlerini ve etiketlerini (verildiyse) değiştirir. Hata metni; başarıda boş.</summary>
        public static string EditInventory(string folder, string name, string subType, string note, IDictionary<string, string>? fields = null, IEnumerable<string>? tags = null)
        {
            var path = InventoryService.RecordFile(folder);
            var record = JsonFile.TryRead<InventoryRecord>(path);
            if (record == null)
            {
                return path;
            }

            record.Name = (name ?? string.Empty).Trim();
            record.SubType = (subType ?? string.Empty).Trim();
            record.Note = (note ?? string.Empty).Trim();
            if (fields != null)
            {
                record.Fields = new Dictionary<string, string>();
                foreach (var pair in fields)
                {
                    if (!string.IsNullOrWhiteSpace(pair.Value))
                    {
                        record.Fields[pair.Key] = pair.Value.Trim();
                    }
                }
            }

            if (tags != null)
            {
                record.Tags = tags.Select(TagDefinition.NameFrom).Where(t => t.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            }

            return Write(path, record);
        }

        /// <summary>
        /// Takipten çıkarır: takip dosyası silinmez, <c>…json.kaldirildi-&lt;zaman&gt;</c> olarak yeniden adlandırılır
        /// (geri almak için eski adına çevirmek yeter). Klasör ve içindekiler olduğu gibi kalır; klasör ağaçta
        /// işaretsiz görünür. Hata metni; başarıda boş.
        /// </summary>
        public static string Untrack(string folder, RecordKind kind, DateTime now)
        {
            var path = TrackingFile(folder, kind);
            if (!File.Exists(path))
            {
                return path;
            }

            var target = path + RemovedSuffix + now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            try
            {
                Pusula.Core.Scanning.DiskWatcher.MarkOwnWrite();
                File.Move(path, target);
                return string.Empty;
            }
            catch (IOException ex)
            {
                return ex.Message;
            }
            catch (UnauthorizedAccessException ex)
            {
                return ex.Message;
            }
        }

        /// <summary>Kökteki "silinenler" klasörünün adı (alt çizgiyle başladığı için ağaçta görünmez, sayılmaz).</summary>
        public const string TrashFolderName = "_SILINENLER";

        /// <summary>Silinenlerin kaydı (her satır: zaman, kişi, eski yol, yeni yol).</summary>
        public const string TrashLogName = "silinenler.log";

        /// <summary>
        /// "Siler": klasörü kalıcı silmez, kökteki <c>_SILINENLER\&lt;zaman&gt;\&lt;kökten göreli yol&gt;</c> altına taşır ve
        /// <c>_SILINENLER\silinenler.log</c> dosyasına bir satır ekler. Geri almak için klasörü eski yerine taşımak yeter.
        /// Kökün dışındaki, kökün kendisi ya da zaten silinenlerdeki klasörler reddedilir. Hata metni; başarıda boş.
        /// </summary>
        public static string MoveToTrash(string root, string folder, DateTime now, string person, out string target)
        {
            target = string.Empty;
            var fullRoot = Path.GetFullPath(root).TrimEnd('\\', '/');
            var fullFolder = Path.GetFullPath(folder).TrimEnd('\\', '/');
            var trash = Path.Combine(fullRoot, TrashFolderName);
            if (!fullFolder.StartsWith(fullRoot + "\\", StringComparison.OrdinalIgnoreCase)
                || fullFolder.StartsWith(trash + "\\", StringComparison.OrdinalIgnoreCase)
                || !Directory.Exists(fullFolder))
            {
                return fullFolder;
            }

            var relative = fullFolder.Substring(fullRoot.Length + 1);
            var stamp = now.ToString("yyyy-MM-dd_HHmmss", CultureInfo.InvariantCulture);
            target = Path.Combine(trash, stamp, relative);
            var suffix = 2;
            while (Directory.Exists(target) || File.Exists(target))
            {
                target = Path.Combine(trash, stamp + "_" + suffix.ToString(CultureInfo.InvariantCulture), relative);
                suffix++;
            }

            try
            {
                Pusula.Core.Scanning.DiskWatcher.MarkOwnWrite();
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                Directory.Move(fullFolder, target);
            }
            catch (IOException ex)
            {
                return ex.Message;
            }
            catch (UnauthorizedAccessException ex)
            {
                return ex.Message;
            }

            try
            {
                var line = now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + " | " + person + " | " + fullFolder + " | " + target + Environment.NewLine;
                File.AppendAllText(Path.Combine(trash, TrashLogName), line, new System.Text.UTF8Encoding(true));
            }
            catch (IOException)
            {
                // Kayıt yazılamasa da taşıma yapıldı; klasör yerinde durur.
            }
            catch (UnauthorizedAccessException)
            {
            }

            return string.Empty;
        }

        private static string Write<T>(string path, T value)
        {
            try
            {
                JsonFile.WriteAtomic(path, value);
                return string.Empty;
            }
            catch (IOException ex)
            {
                return ex.Message;
            }
            catch (UnauthorizedAccessException ex)
            {
                return ex.Message;
            }
        }
    }
}
