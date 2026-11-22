using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security;
using System.Text;

namespace Pusula.Core.Office
{
    /// <summary>
    /// Kütüphanesiz xlsx yazıcı: tek sayfa, ilk satır kalın başlık, metin hücreleri (satır içi metin), sütun genişliği
    /// içeriğe göre, başlık satırı dondurulmuş. Dosya atomik yazılır (geçici dosya + yeniden adlandırma).
    /// </summary>
    public static class XlsxWriter
    {
        /// <summary>Satırları yazar (ilk satır başlık). Hata metni ya da boş döndürür.</summary>
        public static string Write(string path, string sheetName, IList<string[]> rows)
        {
            var temp = path + ".tmp-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            try
            {
                Pusula.Core.Scanning.DiskWatcher.MarkOwnWrite();
                using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write))
                using (var zip = new ZipArchive(stream, ZipArchiveMode.Create))
                {
                    Put(zip, "[Content_Types].xml", "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/></Types>");
                    Put(zip, "_rels/.rels", "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>");
                    Put(zip, "xl/workbook.xml", "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"" + SheetName(sheetName) + "\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>");
                    Put(zip, "xl/_rels/workbook.xml.rels", "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/><Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/></Relationships>");
                    // Stil 0: normal (üste hizalı, kaydırmalı); 1: kalın başlık, gri dolgu.
                    Put(zip, "xl/styles.xml", "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><fonts count=\"2\"><font><sz val=\"11\"/><name val=\"Calibri\"/></font><font><b/><sz val=\"11\"/><name val=\"Calibri\"/></font></fonts><fills count=\"3\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill><fill><patternFill patternType=\"solid\"><fgColor rgb=\"FFD9D9D9\"/><bgColor indexed=\"64\"/></patternFill></fill></fills><borders count=\"1\"><border><left/><right/><top/><bottom/><diagonal/></border></borders><cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs><cellXfs count=\"2\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyAlignment=\"1\"><alignment vertical=\"top\" wrapText=\"1\"/></xf><xf numFmtId=\"0\" fontId=\"1\" fillId=\"2\" borderId=\"0\" xfId=\"0\" applyFont=\"1\" applyFill=\"1\"/></cellXfs></styleSheet>");
                    Put(zip, "xl/worksheets/sheet1.xml", Sheet(rows ?? new List<string[]>()));
                }

                Pusula.Core.Scanning.DiskWatcher.MarkOwnWrite();
                if (File.Exists(path))
                {
                    File.Replace(temp, path, null);
                }
                else
                {
                    File.Move(temp, path);
                }

                return string.Empty;
            }
            catch (IOException ex)
            {
                TryDelete(temp);
                return ex.Message;
            }
            catch (UnauthorizedAccessException ex)
            {
                TryDelete(temp);
                return ex.Message;
            }
        }

        private static string Sheet(IList<string[]> rows)
        {
            var columns = rows.Select(r => r.Length).DefaultIfEmpty(0).Max();
            var sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">");
            sb.Append("<sheetViews><sheetView workbookViewId=\"0\"><pane ySplit=\"1\" topLeftCell=\"A2\" activePane=\"bottomLeft\" state=\"frozen\"/></sheetView></sheetViews>");
            if (columns > 0)
            {
                sb.Append("<cols>");
                for (var c = 0; c < columns; c++)
                {
                    var longest = rows.Select(r => c < r.Length ? (r[c] ?? string.Empty).Length : 0).DefaultIfEmpty(0).Max();
                    var width = Math.Max(8, Math.Min(60, longest + 2));
                    sb.Append("<col min=\"").Append(c + 1).Append("\" max=\"").Append(c + 1).Append("\" width=\"").Append(width.ToString(CultureInfo.InvariantCulture)).Append("\" customWidth=\"1\"/>");
                }

                sb.Append("</cols>");
            }

            sb.Append("<sheetData>");
            for (var r = 0; r < rows.Count; r++)
            {
                sb.Append("<row r=\"").Append(r + 1).Append("\">");
                for (var c = 0; c < rows[r].Length; c++)
                {
                    var text = rows[r][c] ?? string.Empty;
                    if (text.Length == 0)
                    {
                        continue;
                    }

                    sb.Append("<c r=\"").Append(ColumnName(c)).Append(r + 1).Append("\" t=\"inlineStr\" s=\"").Append(r == 0 ? 1 : 0).Append("\"><is><t xml:space=\"preserve\">")
                        .Append(SecurityElement.Escape(CellSafe(text))).Append("</t></is></c>");
                }

                sb.Append("</row>");
            }

            sb.Append("</sheetData></worksheet>");
            return sb.ToString();
        }

        // Hücre metni: XML'de geçersiz denetim karakterleri (DOORS metnindeki \v gibi) atılır, Excel dosyayı bozuk saymasın;
        // Excel'in hücre sınırı 32 767 karakter.
        private static string CellSafe(string text)
        {
            var sb = new StringBuilder(text.Length);
            foreach (var ch in text)
            {
                if (ch >= 0x20 || ch == '\t' || ch == '\n' || ch == '\r')
                {
                    if (ch != '￾' && ch != '￿')
                    {
                        sb.Append(ch);
                    }
                }
            }

            return sb.Length > 32767 ? sb.ToString(0, 32767) : sb.ToString();
        }

        // 0 → A, 25 → Z, 26 → AA.
        private static string ColumnName(int index)
        {
            var name = string.Empty;
            index++;
            while (index > 0)
            {
                var m = (index - 1) % 26;
                name = (char)('A' + m) + name;
                index = (index - 1) / 26;
            }

            return name;
        }

        // Excel sayfa adı: en çok 31 karakter, : \ / ? * [ ] yok.
        private static string SheetName(string name)
        {
            var clean = new string((name ?? "Sayfa1").Where(ch => ":\\/?*[]".IndexOf(ch) < 0).ToArray()).Trim();
            if (clean.Length == 0)
            {
                clean = "Sayfa1";
            }

            return SecurityElement.Escape(clean.Length > 31 ? clean.Substring(0, 31) : clean);
        }

        private static void Put(ZipArchive zip, string name, string content)
        {
            var entry = zip.CreateEntry(name);
            using (var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false)))
            {
                writer.Write(content);
            }
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
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
