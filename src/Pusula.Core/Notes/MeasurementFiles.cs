using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Pusula.Core.Notes
{
    /// <summary>
    /// Ölçüm kaydının klasörü: <c>03_Testler\Olcumler\2026-11-20_DGO-1_Yanca_Huzme_Genisligi\</c> (stokta; diğer
    /// kayıtlarda <c>Olcumler\…</c>). Pusula yalnız boş klasörü açar; içine dosyaları kullanıcı koyar, içi serbesttir.
    /// Aynı ad varsa <c>_2</c> eklenir. Klasörün yolu Günlük'te <c>klasör: 03_Testler/Olcumler/…</c> olarak kayda yazılır.
    /// </summary>
    public static class MeasurementFiles
    {
        /// <summary>Ölçüm klasörlerinin toplandığı klasörün adı.</summary>
        public const string FolderName = "Olcumler";

        /// <summary>Klasör adının en fazla uzunluğu (tarih dahil).</summary>
        public const int MaxNameLength = 60;

        /// <summary>Kaydın ölçüm klasörlerinin üst klasörü: stokta <c>03_Testler\Olcumler</c>, diğerlerinde <c>Olcumler</c>.</summary>
        public static string BaseFolder(string recordFolder, bool isStock)
        {
            return isStock
                ? Path.Combine(recordFolder, Testing.VerificationList.FolderName, FolderName)
                : Path.Combine(recordFolder, FolderName);
        }

        /// <summary>Klasör adı: <c>yyyy-MM-dd_madde_başlık</c> (madde yoksa yazılmaz); Türkçe harfler sadeleşir, boşluk <c>_</c>.</summary>
        public static string NameFor(DateTime date, string item, string title)
        {
            var name = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var plainItem = Plain(item);
            if (plainItem.Length > 0)
            {
                name += "_" + plainItem;
            }

            var plainTitle = Plain(title);
            if (plainTitle.Length > 0)
            {
                name += "_" + plainTitle;
            }

            if (name.Length > MaxNameLength)
            {
                name = name.Substring(0, MaxNameLength).TrimEnd('_', '-', '.');
            }

            return name;
        }

        /// <summary>Metni dosya adına uygun düz ASCII'ye çevirir (ğ→g, ö→o …; boşluk ve geçersiz karakterler <c>_</c>).</summary>
        public static string Plain(string text)
        {
            var map = new Dictionary<char, string>
            {
                { 'ç', "c" }, { 'Ç', "C" }, { 'ğ', "g" }, { 'Ğ', "G" }, { 'ı', "i" }, { 'İ', "I" },
                { 'ö', "o" }, { 'Ö', "O" }, { 'ş', "s" }, { 'Ş', "S" }, { 'ü', "u" }, { 'Ü', "U" },
            };
            var sb = new StringBuilder();
            foreach (var ch in (text ?? string.Empty).Trim())
            {
                string mapped;
                if (map.TryGetValue(ch, out mapped))
                {
                    sb.Append(mapped);
                }
                else if ((ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z') || (ch >= '0' && ch <= '9') || ch == '-' || ch == '.')
                {
                    sb.Append(ch);
                }
                else if (sb.Length > 0 && sb[sb.Length - 1] != '_')
                {
                    sb.Append('_');
                }
            }

            return sb.ToString().Trim('_', '.');
        }

        /// <summary>Üst klasörde bu adla henüz olmayan klasör yolu (varsa <c>_2</c>, <c>_3</c> …). Klasörü açmaz.</summary>
        public static string UniqueFolder(string baseFolder, string name)
        {
            var path = Path.Combine(baseFolder, name);
            for (var n = 2; Directory.Exists(path) || File.Exists(path); n++)
            {
                path = Path.Combine(baseFolder, name + "_" + n.ToString(CultureInfo.InvariantCulture));
            }

            return path;
        }

        /// <summary>Boş ölçüm klasörünü açar (adı çakışırsa <c>_2</c> …). Açılan yol; hata olursa boş ve <paramref name="error"/> dolu.</summary>
        public static string Create(string baseFolder, string name, out string error)
        {
            error = string.Empty;
            try
            {
                var path = UniqueFolder(baseFolder, name);
                Directory.CreateDirectory(path);
                return path;
            }
            catch (IOException ex)
            {
                error = ex.Message;
            }
            catch (UnauthorizedAccessException ex)
            {
                error = ex.Message;
            }
            catch (ArgumentException ex)
            {
                error = ex.Message;
            }
            catch (NotSupportedException ex)
            {
                error = ex.Message;
            }

            return string.Empty;
        }

        /// <summary>Klasördeki (alt klasörler dahil) dosya sayısı; klasör yoksa 0.</summary>
        public static int CountFiles(string folder)
        {
            try
            {
                return Directory.Exists(folder) ? Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories).Count() : 0;
            }
            catch (IOException)
            {
                return 0;
            }
            catch (UnauthorizedAccessException)
            {
                return 0;
            }
        }

        /// <summary>Kayıt klasörüne göre göreli yol, <c>/</c> ile (notta <c>klasör:</c> değeri).</summary>
        public static string Relative(string recordFolder, string path)
        {
            var root = Path.GetFullPath(recordFolder).TrimEnd('\\') + "\\";
            var full = Path.GetFullPath(path);
            var relative = full.StartsWith(root, StringComparison.OrdinalIgnoreCase) ? full.Substring(root.Length) : full;
            return relative.Replace('\\', '/');
        }

        /// <summary>Notta yazılı <c>klasör:</c> değerinin tam yolu (kayıt klasörüne göre); boşsa boş.</summary>
        public static string Resolve(string recordFolder, string relative)
        {
            var value = (relative ?? string.Empty).Trim();
            if (value.Length == 0)
            {
                return string.Empty;
            }

            var native = value.Replace('/', '\\');
            return Path.IsPathRooted(native) ? native : Path.Combine(recordFolder, native);
        }
    }
}
