using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;

namespace Pusula.Core.Office
{
    /// <summary>
    /// Kütüphanesiz xlsx okuyucu: ilk çalışma sayfasının hücrelerini metin olarak okur (paylaşılan metinler, satır içi
    /// metinler, sayılar). Biçim, formül, birleştirilmiş hücre yok sayılır. Dosyaya yazmaz; Excel'de açıkken de okunur.
    /// </summary>
    public static class XlsxReader
    {
        private static readonly XNamespace Main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        private static readonly XNamespace Rel = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        private static readonly XNamespace PackageRel = "http://schemas.openxmlformats.org/package/2006/relationships";

        /// <summary>İlk sayfanın satırları; her satır sütun sırasıyla hücre metinleri (boş hücreler boş metin).</summary>
        public static List<List<string>> ReadFirstSheet(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            using (var zip = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                var shared = ReadSharedStrings(zip);
                var sheet = zip.GetEntry(FirstSheetPath(zip)) ?? throw new InvalidDataException("sheet");
                XDocument doc;
                using (var s = sheet.Open())
                {
                    doc = XDocument.Load(s);
                }

                var rows = new List<List<string>>();
                foreach (var row in doc.Descendants(Main + "row"))
                {
                    var cells = new List<string>();
                    var next = 0;
                    foreach (var cell in row.Elements(Main + "c"))
                    {
                        var col = ColumnIndex((string?)cell.Attribute("r"), next);
                        while (cells.Count < col)
                        {
                            cells.Add(string.Empty);
                        }

                        cells.Add(CellText(cell, shared));
                        next = col + 1;
                    }

                    // Excel boş satırları atlayabilir: satır numarasına göre boş satır eklenir.
                    int number;
                    if (int.TryParse((string?)row.Attribute("r"), NumberStyles.None, CultureInfo.InvariantCulture, out number))
                    {
                        while (rows.Count < number - 1)
                        {
                            rows.Add(new List<string>());
                        }
                    }

                    rows.Add(cells);
                }

                return rows;
            }
        }

        private static string FirstSheetPath(ZipArchive zip)
        {
            var workbook = Load(zip, "xl/workbook.xml");
            var rels = Load(zip, "xl/_rels/workbook.xml.rels");
            var first = workbook?.Descendants(Main + "sheet").FirstOrDefault();
            var id = (string?)first?.Attribute(Rel + "id");
            var target = rels?.Descendants(PackageRel + "Relationship").FirstOrDefault(r => (string?)r.Attribute("Id") == id)?.Attribute("Target")?.Value;
            if (string.IsNullOrEmpty(target))
            {
                return "xl/worksheets/sheet1.xml";
            }

            return target!.StartsWith("/", StringComparison.Ordinal) ? target.Substring(1) : "xl/" + target;
        }

        private static List<string> ReadSharedStrings(ZipArchive zip)
        {
            var doc = Load(zip, "xl/sharedStrings.xml");
            if (doc == null)
            {
                return new List<string>();
            }

            return doc.Root!.Elements(Main + "si").Select(si => string.Concat(si.Descendants(Main + "t").Select(t => t.Value))).ToList();
        }

        private static XDocument? Load(ZipArchive zip, string name)
        {
            var entry = zip.GetEntry(name);
            if (entry == null)
            {
                return null;
            }

            using (var s = entry.Open())
            {
                return XDocument.Load(s);
            }
        }

        private static string CellText(XElement cell, List<string> shared)
        {
            var type = (string?)cell.Attribute("t");
            if (type == "inlineStr")
            {
                return string.Concat(cell.Descendants(Main + "t").Select(t => t.Value)).Trim();
            }

            var value = cell.Element(Main + "v")?.Value ?? string.Empty;
            int index;
            if (type == "s" && int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out index))
            {
                return index >= 0 && index < shared.Count ? shared[index].Trim() : string.Empty;
            }

            // Sayı hücresi: Excel ikili kayan nokta yazar (3.1 → 3.1000000000000001); 15 anlamlı basamakla Excel'de görüneni verir.
            double number;
            if ((type == null || type == "n") && value.IndexOf('E') < 0 && value.IndexOf('e') < 0
                && double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out number))
            {
                return number.ToString("G15", CultureInfo.InvariantCulture);
            }

            return value.Trim();
        }

        // "C12" → 2; adres yoksa sıradaki sütun.
        private static int ColumnIndex(string? reference, int fallback)
        {
            if (string.IsNullOrEmpty(reference))
            {
                return fallback;
            }

            var col = 0;
            foreach (var ch in reference!)
            {
                if (ch < 'A' || ch > 'Z')
                {
                    break;
                }

                col = col * 26 + (ch - 'A' + 1);
            }

            return col == 0 ? fallback : col - 1;
        }
    }
}
