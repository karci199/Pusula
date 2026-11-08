using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using Pusula.Core.Inventory;

namespace Pusula.Core.Tracking
{
    /// <summary>Uygulamadan yazılan kısa not: tarih, kişi, metin. Proje, stok ve envanter kaydının bilgi dosyasında durur.</summary>
    [DataContract]
    public sealed class QuickNote
    {
        private const string DateFormat = "yyyy-MM-dd HH:mm";

        /// <summary>Serileştirici kurucuyu çağırmaz; dosyada olmayan alanlar boş değerle başlar.</summary>
        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
            Date = string.Empty;
            Person = string.Empty;
            Text = string.Empty;
        }

        /// <summary>Tarih ve saat (yyyy-MM-dd HH:mm).</summary>
        [DataMember(Name = "date", Order = 0)]
        public string Date { get; set; } = string.Empty;

        /// <summary>Yazan kişi.</summary>
        [DataMember(Name = "person", Order = 1)]
        public string Person { get; set; } = string.Empty;

        /// <summary>Not metni.</summary>
        [DataMember(Name = "text", Order = 2)]
        public string Text { get; set; } = string.Empty;

        /// <summary><see cref="Date"/> tarih olarak; okunamazsa <c>null</c>.</summary>
        public DateTime? DateValue
        {
            get
            {
                DateTime value;
                return DateTime.TryParseExact(Date, new[] { DateFormat, "yyyy-MM-dd" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out value)
                    ? value
                    : (DateTime?)null;
            }
        }

        /// <summary>Şimdiki zamanla not oluşturur.</summary>
        public static QuickNote Create(string text, string person, DateTime when)
        {
            return new QuickNote
            {
                Date = when.ToString(DateFormat, CultureInfo.InvariantCulture),
                Person = person ?? string.Empty,
                Text = (text ?? string.Empty).Trim(),
            };
        }
    }

    /// <summary>Kısa notu ilgili bilgi dosyasına ekler (diğer alanlara dokunmaz, atomik yazar).</summary>
    public static class QuickNotes
    {
        /// <summary>Projeye not ekler (<c>_TAKIP\proje.json</c>).</summary>
        public static bool AddToProject(string folder, QuickNote note)
        {
            return Add<ProjectInfo>(TrackingFiles.ProjectFile(folder), note, info => info.Notes);
        }

        /// <summary>Stoka not ekler (<c>_TAKIP\stok.json</c>).</summary>
        public static bool AddToStock(string folder, QuickNote note)
        {
            return Add<StockInfo>(TrackingFiles.StockFile(folder), note, info => info.Notes);
        }

        /// <summary>Envanter kaydına not ekler (<c>_TAKIP\kayit.json</c>).</summary>
        public static bool AddToRecord(string folder, QuickNote note)
        {
            return Add<InventoryRecord>(InventoryService.RecordFile(folder), note, record => record.Notes);
        }

        private static bool Add<T>(string path, QuickNote note, Func<T, List<QuickNote>> notes) where T : class
        {
            if (note.Text.Length == 0)
            {
                return false;
            }

            var info = JsonFile.TryRead<T>(path);
            if (info == null)
            {
                return false;
            }

            notes(info).Add(note);
            try
            {
                JsonFile.WriteAtomic(path, info);
                return true;
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }
    }
}
