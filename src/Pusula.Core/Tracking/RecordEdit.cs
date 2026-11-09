using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Pusula.Core.Inventory;

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

        /// <summary>Envanter kaydının adını, alt türünü, kısa açıklamasını ve alan değerlerini değiştirir. Hata metni; başarıda boş.</summary>
        public static string EditInventory(string folder, string name, string subType, string note, IDictionary<string, string>? fields = null)
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
