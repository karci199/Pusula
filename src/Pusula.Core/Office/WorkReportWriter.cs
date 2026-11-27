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
    /// İş raporunu biçimli xlsx olarak yazar (kütüphanesiz). İki sayfa:
    /// <list type="bullet">
    /// <item>İşler: başlık, özet kutuları (Excel süzgecine göre sayar), tarih · gün · proje · stok · tür · yapılan iş · durum
    /// tablosu; başlık satırında süzgeç (proje / stok / tür seçilir), gün gün açık-koyu zemin, durum renkli.</item>
    /// <item>Özet: proje ve stok başına kayıt sayısı (İşler'den formülle), son kayıt, türler.</item>
    /// </list>
    /// Dosya atomik yazılır (geçici dosya + yeniden adlandırma).
    /// </summary>
    public static class WorkReportWriter
    {
        private const int HeaderRow = 10;
        private const int FirstRow = 11;
        private const int MaxKindTiles = 4;

        // Hücre biçimleri (styles.xml cellXfs sırası).
        private const int SDefault = 0;
        private const int STitle = 1;
        private const int SPeriod = 2;
        private const int SSubtitle = 3;
        private const int SHint = 4;
        private const int STileLabel = 5;
        private const int STileNumber = 6;
        private const int SSection = 7;
        private const int SHeader = 8;
        private const int SFooter = 9;
        private const int SHeaderCenter = 10;

        // Tablo hücreleri: 11'den başlar; her biri düz ve zeminli iki sürüm (zeminli = +BandOffset).
        private const int SDate = 11;
        private const int SDateDim = 12;
        private const int SDay = 13;
        private const int SDayDim = 14;
        private const int SText = 15;
        private const int SMuted = 16;
        private const int SMono = 17;
        private const int SStatus = 18;
        private const int SBold = 19;
        private const int SNumber = 20;
        private const int BandOffset = 10;

        /// <summary>Raporu yazar. Hata metni ya da boş döndürür.</summary>
        public static string Write(string path, WorkReportLabels labels, IList<WorkReportRow> rows)
        {
            var temp = path + ".tmp-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            var list = WorkReport.Sorted(rows ?? new List<WorkReportRow>());
            var workSheet = XlsxWriter.SheetName(labels.SheetWork);
            var summarySheet = XlsxWriter.SheetName(labels.SheetSummary);
            var last = FirstRow + Math.Max(list.Count, 1) - 1;
            try
            {
                Scanning.DiskWatcher.MarkOwnWrite();
                using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write))
                using (var zip = new ZipArchive(stream, ZipArchiveMode.Create))
                {
                    XlsxWriter.Put(zip, "[Content_Types].xml", Xml + "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/worksheets/sheet2.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/></Types>");
                    XlsxWriter.Put(zip, "_rels/.rels", Xml + "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>");
                    XlsxWriter.Put(zip, "xl/workbook.xml", Xml + "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets>"
                        + "<sheet name=\"" + workSheet + "\" sheetId=\"1\" r:id=\"rId1\"/><sheet name=\"" + summarySheet + "\" sheetId=\"2\" r:id=\"rId2\"/></sheets><definedNames>"
                        + "<definedName name=\"_xlnm._FilterDatabase\" localSheetId=\"0\" hidden=\"1\">" + SecurityElement.Escape(Quote(labels.SheetWork)) + "!$B$" + HeaderRow + ":$H$" + last + "</definedName>"
                        + "<definedName name=\"_xlnm.Print_Titles\" localSheetId=\"0\">" + SecurityElement.Escape(Quote(labels.SheetWork)) + "!$" + HeaderRow + ":$" + HeaderRow + "</definedName>"
                        + "</definedNames><calcPr calcId=\"191029\" fullCalcOnLoad=\"1\"/></workbook>");
                    XlsxWriter.Put(zip, "xl/_rels/workbook.xml.rels", Xml + "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/><Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet2.xml\"/><Relationship Id=\"rId3\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/></Relationships>");
                    XlsxWriter.Put(zip, "xl/styles.xml", Styles());
                    XlsxWriter.Put(zip, "xl/worksheets/sheet1.xml", WorkSheet(labels, list, last));
                    XlsxWriter.Put(zip, "xl/worksheets/sheet2.xml", SummarySheet(labels, list, last));
                }

                Scanning.DiskWatcher.MarkOwnWrite();
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
                XlsxWriter.TryDelete(temp);
                return ex.Message;
            }
            catch (UnauthorizedAccessException ex)
            {
                XlsxWriter.TryDelete(temp);
                return ex.Message;
            }
        }

        private const string Xml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>";

        // ---- İşler sayfası ----

        private static string WorkSheet(WorkReportLabels labels, List<WorkReportRow> rows, int last)
        {
            var s = new SheetBuilder();
            s.Row(2, 30).Text("B", 2, labels.Title, STitle).Text("H", 2, labels.PeriodTag, SPeriod);
            s.Row(3, 18).Text("B", 3, labels.Subtitle, SSubtitle);
            s.Row(5, 14).Text("B", 5, labels.FilterHint, SHint);

            // Özet kutuları: Kayıt + en sık türler; hepsi Excel süzgecine göre (gizli satırlar sayılmaz).
            var kinds = rows.Where(r => r.Kind.Length > 0).GroupBy(r => r.Kind, StringComparer.CurrentCultureIgnoreCase)
                .OrderByDescending(g => g.Count()).ThenBy(g => g.Key, StringComparer.CurrentCultureIgnoreCase).Take(MaxKindTiles).Select(g => g.Key).ToList();
            var tiles = new List<Tuple<string, string>> { Tuple.Create(labels.Records, "SUBTOTAL(103,$G$" + FirstRow + ":$G$" + last + ")") };
            foreach (var kind in kinds)
            {
                tiles.Add(Tuple.Create(kind, "SUMPRODUCT(SUBTOTAL(103,OFFSET($F$" + FirstRow + ",ROW($F$" + FirstRow + ":$F$" + last + ")-ROW($F$" + FirstRow + "),0))*($F$" + FirstRow + ":$F$" + last + "=\"" + kind.Replace("\"", "\"\"") + "\"))"));
            }

            var tileColumns = new[] { "B", "D", "E", "F", "G" };
            s.Row(6, 18);
            s.Row(7, 34);
            for (var i = 0; i < tileColumns.Length; i++)
            {
                var col = tileColumns[i];
                if (i < tiles.Count)
                {
                    s.Text(col, 6, tiles[i].Item1, STileLabel);
                    if (rows.Count == 0)
                    {
                        s.Number(col, 7, 0, STileNumber);
                    }
                    else
                    {
                        s.Formula(col, 7, tiles[i].Item2, STileNumber);
                    }
                }
                else
                {
                    s.Text(col, 6, string.Empty, STileLabel).Text(col, 7, string.Empty, STileNumber);
                }
            }

            s.Text("C", 6, string.Empty, STileLabel).Text("C", 7, string.Empty, STileNumber);
            s.Merge("B6:C6").Merge("B7:C7");

            s.Row(9, 20).Text("B", 9, labels.WorkSection, SSection);
            s.Row(HeaderRow, 18);
            var cols = new[] { "B", "C", "D", "E", "F", "G", "H" };
            for (var c = 0; c < cols.Length; c++)
            {
                s.Text(cols[c], HeaderRow, c < labels.Columns.Length ? labels.Columns[c] : string.Empty, c == 1 || c == 6 ? SHeaderCenter : SHeader);
            }

            if (rows.Count == 0)
            {
                s.Row(FirstRow, 22).Text("G", FirstRow, labels.Empty, SMuted);
            }

            var band = false;
            DateTime? previous = null;
            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                var r = FirstRow + i;
                var newDay = !previous.HasValue || previous.Value != row.Date.Date;
                if (newDay)
                {
                    band = !band;
                }

                previous = row.Date.Date;
                var o = band ? 0 : BandOffset;
                s.Row(r, 21);
                s.Number("B", r, row.Date.Date.ToOADate(), (newDay ? SDate : SDateDim) + o);
                s.Number("C", r, row.Date.Date.ToOADate(), (newDay ? SDay : SDayDim) + o);
                s.Text("D", r, row.Project, SText + o);
                s.Text("E", r, row.Stock, SMono + o);
                s.Text("F", r, row.Kind, SMuted + o);
                s.Text("G", r, row.Work, SText + o);
                s.Text("H", r, row.Status, SStatus + o);
            }

            var footer = last + 2;
            s.Row(footer, 14).Text("B", footer, labels.Footer, SFooter);

            var cf = new StringBuilder();
            var priority = 1;
            AddRules(cf, labels.GoodWords, 0, last, ref priority);
            AddRules(cf, labels.BadWords, 1, last, ref priority);
            AddRules(cf, labels.WaitWords, 2, last, ref priority);
            var cfXml = cf.Length > 0 ? "<conditionalFormatting sqref=\"H" + FirstRow + ":H" + last + "\">" + cf + "</conditionalFormatting>" : string.Empty;

            return s.Build(
                new[] { 2.0, 12.5, 6, 20, 15, 19, 60, 16, 2 },
                "<pane ySplit=\"" + HeaderRow + "\" topLeftCell=\"A" + FirstRow + "\" activePane=\"bottomLeft\" state=\"frozen\"/><selection pane=\"bottomLeft\" activeCell=\"B" + FirstRow + "\" sqref=\"B" + FirstRow + "\"/>",
                "<autoFilter ref=\"B" + HeaderRow + ":H" + last + "\"/>",
                cfXml);
        }

        // Durum sözcüğü geçen hücreyi renklendirir (Geçti yeşil, Kaldı kırmızı, Açık sarı).
        private static void AddRules(StringBuilder sb, IEnumerable<string> words, int dxf, int last, ref int priority)
        {
            foreach (var word in words.Where(w => !string.IsNullOrWhiteSpace(w)))
            {
                var text = SecurityElement.Escape(word);
                sb.Append("<cfRule type=\"containsText\" dxfId=\"").Append(dxf).Append("\" priority=\"").Append(priority++).Append("\" operator=\"containsText\" text=\"").Append(text).Append("\">")
                    .Append("<formula>NOT(ISERROR(SEARCH(\"").Append(SecurityElement.Escape(word.Replace("\"", "\"\""))).Append("\",H").Append(FirstRow).Append(")))</formula></cfRule>");
            }
        }

        // ---- Özet sayfası ----

        private static string SummarySheet(WorkReportLabels labels, List<WorkReportRow> rows, int last)
        {
            var s = new SheetBuilder();
            s.Row(2, 26).Text("B", 2, labels.SummarySection, STitle);
            s.Row(3, 18).Text("B", 3, labels.Subtitle, SSubtitle);
            const int head = 5;
            s.Row(head, 18);
            var cols = new[] { "B", "C", "D", "E", "F", "G" };
            for (var c = 0; c < cols.Length; c++)
            {
                s.Text(cols[c], head, c < labels.SummaryColumns.Length ? labels.SummaryColumns[c] : string.Empty, c == 3 ? SHeaderCenter : SHeader);
            }

            var work = Quote(labels.SheetWork);
            var groups = rows.GroupBy(r => r.Project + "\u0001" + r.Stock, StringComparer.CurrentCultureIgnoreCase)
                .Select(g => g.ToList())
                .OrderBy(g => g[0].Project, StringComparer.CurrentCultureIgnoreCase).ThenBy(g => g[0].Stock, StringComparer.CurrentCultureIgnoreCase).ToList();
            var r0 = head + 1;
            string? previousProject = null;
            var band = false;
            for (var i = 0; i < groups.Count; i++)
            {
                var g = groups[i];
                var first = g[0];
                var r = r0 + i;
                var newProject = previousProject == null || !string.Equals(previousProject, first.Project, StringComparison.CurrentCultureIgnoreCase);
                if (newProject)
                {
                    band = !band;
                }

                previousProject = first.Project;
                var o = band ? 0 : BandOffset;
                var kinds = string.Join(" · ", g.Where(x => x.Kind.Length > 0).GroupBy(x => x.Kind, StringComparer.CurrentCultureIgnoreCase)
                    .OrderByDescending(k => k.Count()).Select(k => k.Key + " " + k.Count().ToString(CultureInfo.InvariantCulture)));
                s.Row(r, 21);
                s.Text("B", r, first.Project, (newProject ? SBold : SText) + o);
                s.Text("C", r, first.Stock, SMono + o);
                s.Text("D", r, g.Select(x => x.StockName).FirstOrDefault(n => n.Length > 0) ?? string.Empty, SMuted + o);
                s.Formula("E", r, "COUNTIFS(" + work + "!$D$" + FirstRow + ":$D$" + last + ",B" + r + "," + work + "!$E$" + FirstRow + ":$E$" + last + ",C" + r + ")", SNumber + o);
                s.Number("F", r, g.Max(x => x.Date).Date.ToOADate(), SDate + o);
                s.Text("G", r, kinds, SMuted + o);
            }

            var end = r0 + Math.Max(groups.Count, 1) - 1;
            if (groups.Count == 0)
            {
                s.Row(r0, 22).Text("B", r0, labels.Empty, SMuted);
            }

            return s.Build(new[] { 2.0, 20, 15, 28, 9, 12.5, 48, 2 },
                "<pane ySplit=\"" + head + "\" topLeftCell=\"A" + r0 + "\" activePane=\"bottomLeft\" state=\"frozen\"/>",
                "<autoFilter ref=\"B" + head + ":G" + end + "\"/>",
                string.Empty);
        }

        // Sayfa adı formülde tırnaklı: 'İşler'.
        private static string Quote(string sheet)
        {
            return "'" + XlsxWriter.CleanSheetName(sheet).Replace("'", "''") + "'";
        }

        // ---- Biçimler ----

        private static string Styles()
        {
            const string ink = "FF1F2937";
            const string muted = "FF6B7280";
            const string dim = "FFCBD5E1";
            const string accent = "FF0D9488";
            var fonts = new[]
            {
                Font(10, ink),                    // 0 normal
                Font(18, ink, bold: true),        // 1 başlık
                Font(11, accent, bold: true),     // 2 bölüm / dönem
                Font(10, muted),                  // 3 soluk
                Font(8, muted, italic: true),     // 4 açıklama
                Font(9, muted),                   // 5 kutu etiketi
                Font(20, ink, bold: true),        // 6 kutu sayısı
                Font(9, muted, bold: true),       // 7 tablo başlığı
                Font(10, dim),                    // 8 tekrar eden tarih
                Font(10, ink, name: "Consolas"),  // 9 stok no
                Font(10, ink, bold: true),        // 10 kalın
            };
            var fills = new[]
            {
                "<fill><patternFill patternType=\"none\"/></fill>",
                "<fill><patternFill patternType=\"gray125\"/></fill>",
                Fill("FFF0FDFA"), // 2 kutu
                Fill("FFF8FAFC"), // 3 gün zemini
            };
            var borders = new[]
            {
                "<border><left/><right/><top/><bottom/><diagonal/></border>",
                "<border><left/><right/><top/><bottom style=\"thin\"><color rgb=\"FFE5E7EB\"/></bottom><diagonal/></border>",
                "<border><left/><right/><top/><bottom style=\"medium\"><color rgb=\"" + ink + "\"/></bottom><diagonal/></border>",
                "<border><left/><right/><top style=\"thick\"><color rgb=\"" + accent + "\"/></top><bottom/><diagonal/></border>",
            };

            const string left = "<alignment vertical=\"center\"/>";
            const string center = "<alignment horizontal=\"center\" vertical=\"center\"/>";
            const string indent = "<alignment horizontal=\"left\" vertical=\"center\" indent=\"1\"/>";
            const string right = "<alignment horizontal=\"right\" vertical=\"center\"/>";
            var xfs = new List<string>
            {
                Xf(0, 0, 0, 0, null),            // 0
                Xf(0, 1, 0, 0, left),            // 1 başlık
                Xf(0, 2, 0, 0, right),           // 2 dönem
                Xf(0, 3, 0, 0, left),            // 3 alt başlık
                Xf(0, 4, 0, 0, left),            // 4 açıklama
                Xf(0, 5, 2, 3, indent),          // 5 kutu etiketi
                Xf(0, 6, 2, 0, indent),          // 6 kutu sayısı
                Xf(0, 2, 0, 0, left),            // 7 bölüm
                Xf(0, 7, 0, 2, left),            // 8 tablo başlığı
                Xf(0, 4, 0, 0, left),            // 9 alt not
                Xf(0, 7, 0, 2, center),          // 10 tablo başlığı ortalı
            };
            for (var band = 0; band < 2; band++)
            {
                var fill = band == 0 ? 3 : 0;
                xfs.Add(Xf(164, 0, fill, 1, left));    // 11 / 21 tarih
                xfs.Add(Xf(164, 8, fill, 1, left));    // 12 / 22 tarih (aynı gün)
                xfs.Add(Xf(165, 0, fill, 1, center));  // 13 / 23 gün
                xfs.Add(Xf(165, 8, fill, 1, center));  // 14 / 24 gün (aynı gün)
                xfs.Add(Xf(0, 0, fill, 1, left));      // 15 / 25 metin
                xfs.Add(Xf(0, 3, fill, 1, left));      // 16 / 26 soluk metin
                xfs.Add(Xf(0, 9, fill, 1, left));      // 17 / 27 stok no
                xfs.Add(Xf(0, 10, fill, 1, center));   // 18 / 28 durum
                xfs.Add(Xf(0, 10, fill, 1, left));     // 19 / 29 kalın
                xfs.Add(Xf(1, 0, fill, 1, center));    // 20 / 30 sayı
            }

            var dxfs = new[]
            {
                Dxf("FF166534", "FFDCFCE7"), // 0 iyi
                Dxf("FF991B1B", "FFFEE2E2"), // 1 kötü
                Dxf("FF92400E", "FFFEF3C7"), // 2 bekliyor
            };

            return Xml + "<styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">"
                + "<numFmts count=\"2\"><numFmt numFmtId=\"164\" formatCode=\"dd\\.mm\\.yyyy\"/><numFmt numFmtId=\"165\" formatCode=\"[$-41F]ddd\"/></numFmts>"
                + "<fonts count=\"" + fonts.Length + "\">" + string.Concat(fonts) + "</fonts>"
                + "<fills count=\"" + fills.Length + "\">" + string.Concat(fills) + "</fills>"
                + "<borders count=\"" + borders.Length + "\">" + string.Concat(borders) + "</borders>"
                + "<cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs>"
                + "<cellXfs count=\"" + xfs.Count + "\">" + string.Concat(xfs) + "</cellXfs>"
                + "<cellStyles count=\"1\"><cellStyle name=\"Normal\" xfId=\"0\" builtinId=\"0\"/></cellStyles>"
                + "<dxfs count=\"" + dxfs.Length + "\">" + string.Concat(dxfs) + "</dxfs>"
                + "</styleSheet>";
        }

        private static string Font(int size, string color, bool bold = false, bool italic = false, string name = "Arial")
        {
            return "<font>" + (bold ? "<b/>" : string.Empty) + (italic ? "<i/>" : string.Empty) + "<sz val=\"" + size.ToString(CultureInfo.InvariantCulture)
                + "\"/><color rgb=\"" + color + "\"/><name val=\"" + name + "\"/></font>";
        }

        private static string Fill(string color)
        {
            return "<fill><patternFill patternType=\"solid\"><fgColor rgb=\"" + color + "\"/><bgColor indexed=\"64\"/></patternFill></fill>";
        }

        private static string Dxf(string font, string fill)
        {
            return "<dxf><font><b/><color rgb=\"" + font + "\"/></font><fill><patternFill patternType=\"solid\"><bgColor rgb=\"" + fill + "\"/></patternFill></fill></dxf>";
        }

        private static string Xf(int numFmt, int font, int fill, int border, string? alignment)
        {
            return "<xf numFmtId=\"" + numFmt.ToString(CultureInfo.InvariantCulture) + "\" fontId=\"" + font.ToString(CultureInfo.InvariantCulture)
                + "\" fillId=\"" + fill.ToString(CultureInfo.InvariantCulture) + "\" borderId=\"" + border.ToString(CultureInfo.InvariantCulture) + "\" xfId=\"0\""
                + (numFmt != 0 ? " applyNumberFormat=\"1\"" : string.Empty) + " applyFont=\"1\" applyFill=\"1\" applyBorder=\"1\""
                + (alignment != null ? " applyAlignment=\"1\">" + alignment + "</xf>" : "/>");
        }

        // Satır satır hücre toplayan küçük yardımcı; hücreler sütun sırasıyla yazılır.
        private sealed class SheetBuilder
        {
            private readonly SortedDictionary<int, SortedDictionary<int, string>> cells = new SortedDictionary<int, SortedDictionary<int, string>>();
            private readonly Dictionary<int, double> heights = new Dictionary<int, double>();
            private readonly List<string> merges = new List<string>();

            public SheetBuilder Row(int row, double height)
            {
                heights[row] = height;
                return this;
            }

            public SheetBuilder Text(string col, int row, string text, int style)
            {
                var value = text ?? string.Empty;
                var xml = value.Length == 0
                    ? "<c r=\"" + col + row + "\" s=\"" + style + "\"/>"
                    : "<c r=\"" + col + row + "\" s=\"" + style + "\" t=\"inlineStr\"><is><t xml:space=\"preserve\">" + SecurityElement.Escape(XlsxWriter.CellSafe(value)) + "</t></is></c>";
                return Put(col, row, xml);
            }

            public SheetBuilder Number(string col, int row, double value, int style)
            {
                return Put(col, row, "<c r=\"" + col + row + "\" s=\"" + style + "\"><v>" + value.ToString("R", CultureInfo.InvariantCulture) + "</v></c>");
            }

            public SheetBuilder Formula(string col, int row, string formula, int style)
            {
                return Put(col, row, "<c r=\"" + col + row + "\" s=\"" + style + "\"><f>" + SecurityElement.Escape(formula) + "</f></c>");
            }

            public SheetBuilder Merge(string range)
            {
                merges.Add(range);
                return this;
            }

            public string Build(double[] widths, string pane, string autoFilter, string conditional)
            {
                var sb = new StringBuilder();
                sb.Append(Xml).Append("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">");
                sb.Append("<sheetPr><pageSetUpPr fitToPage=\"1\"/></sheetPr>");
                sb.Append("<sheetViews><sheetView showGridLines=\"0\" workbookViewId=\"0\">").Append(pane).Append("</sheetView></sheetViews>");
                sb.Append("<sheetFormatPr defaultRowHeight=\"15\"/><cols>");
                for (var i = 0; i < widths.Length; i++)
                {
                    sb.Append("<col min=\"").Append(i + 1).Append("\" max=\"").Append(i + 1).Append("\" width=\"")
                        .Append(widths[i].ToString(CultureInfo.InvariantCulture)).Append("\" customWidth=\"1\"/>");
                }

                sb.Append("</cols><sheetData>");
                foreach (var r in cells.Keys.Union(heights.Keys).OrderBy(k => k))
                {
                    sb.Append("<row r=\"").Append(r).Append('"');
                    double h;
                    if (heights.TryGetValue(r, out h))
                    {
                        sb.Append(" ht=\"").Append(h.ToString(CultureInfo.InvariantCulture)).Append("\" customHeight=\"1\"");
                    }

                    sb.Append('>');
                    SortedDictionary<int, string> row;
                    if (cells.TryGetValue(r, out row))
                    {
                        foreach (var c in row.Values)
                        {
                            sb.Append(c);
                        }
                    }

                    sb.Append("</row>");
                }

                sb.Append("</sheetData>").Append(autoFilter);
                if (merges.Count > 0)
                {
                    sb.Append("<mergeCells count=\"").Append(merges.Count).Append("\">");
                    foreach (var m in merges)
                    {
                        sb.Append("<mergeCell ref=\"").Append(m).Append("\"/>");
                    }

                    sb.Append("</mergeCells>");
                }

                sb.Append(conditional);
                sb.Append("<pageMargins left=\"0.4\" right=\"0.4\" top=\"0.5\" bottom=\"0.5\" header=\"0.3\" footer=\"0.3\"/>");
                sb.Append("<pageSetup paperSize=\"9\" orientation=\"landscape\" fitToWidth=\"1\" fitToHeight=\"0\"/>");
                sb.Append("</worksheet>");
                return sb.ToString();
            }

            private SheetBuilder Put(string col, int row, string xml)
            {
                SortedDictionary<int, string> line;
                if (!cells.TryGetValue(row, out line))
                {
                    line = new SortedDictionary<int, string>();
                    cells[row] = line;
                }

                line[ColumnIndex(col)] = xml;
                return this;
            }

            private static int ColumnIndex(string col)
            {
                var n = 0;
                foreach (var ch in col)
                {
                    n = (n * 26) + (ch - 'A' + 1);
                }

                return n;
            }
        }
    }
}
