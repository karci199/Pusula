using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Pusula.Core.Notes;

namespace Pusula.Core.Testing
{
    /// <summary>Test planındaki bir madde: TP no, açıklama, yöntem, altyapı, doğruladığı gereksinimler, not.</summary>
    public sealed class PlanItem
    {
        /// <summary>Plan madde numarası (TP no), örn. "4.1".</summary>
        public string TpNo { get; set; } = string.Empty;

        /// <summary>Açıklama, örn. "SATIMO hüzme ölçümü".</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>Doğrulama yöntemi: Test, Muayene, Belge, Gösterim, Diğer.</summary>
        public string Method { get; set; } = string.Empty;

        /// <summary>Altyapı, örn. "SATIMO".</summary>
        public string Facility { get; set; } = string.Empty;

        /// <summary>Doğruladığı gereksinimler (TG no; TG no yoksa "No 5").</summary>
        public List<string> Requirements { get; set; } = new List<string>();

        /// <summary>Not (jig, SN, dikkat edilecekler).</summary>
        public string Note { get; set; } = string.Empty;

        /// <summary>Tabloda tanınmayan sütunların değerleri; yazarken korunur.</summary>
        public Dictionary<string, string> Extra { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Test raporundaki bir madde: TR no, açıklama, sonuç, bağlı plan maddeleri, sonucun yeri, not.</summary>
    public sealed class ReportItem
    {
        /// <summary>Rapor madde numarası (TR no), örn. "5.1".</summary>
        public string TrNo { get; set; } = string.Empty;

        /// <summary>Açıklama, örn. "Hüzme ölçüm sonuçları".</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>Sonuç: Geçti, Kaldı ya da Hazırlanmadı.</summary>
        public string Result { get; set; } = string.Empty;

        /// <summary>Bağlı plan maddeleri (TP no).</summary>
        public List<string> Plans { get; set; } = new List<string>();

        /// <summary>Sonucun yeri: ölçüm klasörü ya da dosya (stok klasörüne göre göreli ya da tam yol).</summary>
        public string ResultPath { get; set; } = string.Empty;

        /// <summary>Not (ölçülen değer, kaldıysa bildirim no).</summary>
        public string Note { get; set; } = string.Empty;

        /// <summary>Ölçüm / rapor tarihi (gg.aa.yyyy olarak yazılır); boş olabilir.</summary>
        public string Date { get; set; } = string.Empty;

        /// <summary>Ölçülen birim (seri no), örn. "SN01"; boş olabilir.</summary>
        public string Unit { get; set; } = string.Empty;

        /// <summary>Tabloda tanınmayan sütunların değerleri; yazarken korunur.</summary>
        public Dictionary<string, string> Extra { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Doğrulama dosyasındaki (<c>03_Testler\&lt;stok&gt;_Test_Gereksinimleri.md</c>) Plan ve Rapor tabloları (21.11.2026):
    /// <c>## Plan</c> altındaki ilk tablo plan maddeleri, <c>## Rapor</c> (eski dosyalarda <c>## Sonuç</c>) altındaki ilk tablo
    /// rapor maddeleridir. Bağlar numarayla tutulur ("DGÖ-1, DGÖ-2"; "4.1, 4.2"). Uygulama yalnız bu iki tabloyu yazar;
    /// bölümdeki başka satırlara (elle yazılan ayrıntı) dokunmaz. Sütunlar başlıktan tanınır, tanınmayanlar korunur.
    /// </summary>
    public static class PlanReportList
    {
        /// <summary>Plan bölümünün başlığı.</summary>
        public const string PlanHeading = "Plan";

        /// <summary>Rapor bölümünün başlığı (eski dosyalarda <see cref="LegacyReportHeading"/>).</summary>
        public const string ReportHeading = "Rapor";

        /// <summary>Rapor bölümünün eski başlığı.</summary>
        public const string LegacyReportHeading = "Sonuç";

        /// <summary>Plan tablosunun başlıkları.</summary>
        public static readonly string[] PlanHeaders = { "TP no", "Açıklama", "Yöntem", "Altyapı", "Gereksinimler", "Not" };

        /// <summary>Rapor tablosunun başlıkları.</summary>
        public static readonly string[] ReportHeaders = { "TR no", "Açıklama", "Plan", "Tarih", "Birim", "Sonuç", "Sonuç yeri", "Not" };

        /// <summary>Plan maddelerini okur; dosya, bölüm ya da tablo yoksa boş liste.</summary>
        public static List<PlanItem> ReadPlans(string stockFolder)
        {
            var result = new List<PlanItem>();
            foreach (var row in ReadRows(stockFolder, new[] { PlanHeading }, PlanHeaders))
            {
                var item = new PlanItem
                {
                    TpNo = Cell(row, 0),
                    Description = Cell(row, 1),
                    Method = Cell(row, 2),
                    Facility = Cell(row, 3),
                    Requirements = SplitList(Cell(row, 4)),
                    Note = Cell(row, 5),
                };
                CopyExtra(row, item.Extra, PlanHeaders);
                if (item.TpNo.Length > 0 || item.Description.Length > 0)
                {
                    result.Add(item);
                }
            }

            return result;
        }

        /// <summary>Rapor maddelerini okur; dosya, bölüm ya da tablo yoksa boş liste.</summary>
        public static List<ReportItem> ReadReports(string stockFolder)
        {
            var result = new List<ReportItem>();
            foreach (var row in ReadRows(stockFolder, new[] { ReportHeading, LegacyReportHeading }, ReportHeaders))
            {
                var item = new ReportItem
                {
                    TrNo = Cell(row, 0),
                    Description = Cell(row, 1),
                    Plans = SplitList(Cell(row, 2)),
                    Date = Cell(row, 3),
                    Unit = Cell(row, 4),
                    Result = Cell(row, 5),
                    ResultPath = Cell(row, 6),
                    Note = Cell(row, 7),
                };
                CopyExtra(row, item.Extra, ReportHeaders);
                if (item.TrNo.Length > 0 || item.Description.Length > 0)
                {
                    result.Add(item);
                }
            }

            return result;
        }

        /// <summary>Plan tablosunu verilen maddelerle yazar (bölüm ya da tablo yoksa açılır). Hata metni ya da boş döndürür.</summary>
        public static string SavePlans(string stockFolder, IList<PlanItem> items)
        {
            var rows = (items ?? new List<PlanItem>()).Select(i => Tuple.Create(
                new[] { i.TpNo, i.Description, i.Method, i.Facility, string.Join(", ", i.Requirements), i.Note }, i.Extra)).ToList();
            return WriteRows(stockFolder, new[] { PlanHeading }, PlanHeaders, rows);
        }

        /// <summary>Rapor tablosunu verilen maddelerle yazar (bölüm ya da tablo yoksa açılır). Hata metni ya da boş döndürür.</summary>
        public static string SaveReports(string stockFolder, IList<ReportItem> items)
        {
            var rows = (items ?? new List<ReportItem>()).Select(i => Tuple.Create(
                new[] { i.TrNo, i.Description, string.Join(", ", i.Plans), i.Date, i.Unit, i.Result, i.ResultPath, i.Note }, i.Extra)).ToList();
            return WriteRows(stockFolder, new[] { ReportHeading, LegacyReportHeading }, ReportHeaders, rows);
        }

        /// <summary>
        /// Taşıma (21.11.2026 öncesi dosyalar): plan / rapor tablosu yoksa gereksinim satırlarındaki TP / TR bilgisinden kurar.
        /// Aynı TP no'lu satırlar tek plan maddesinde birleşir (gereksinimleri toplanır); aynı TR no'lu satırlar tek rapor
        /// maddesinde (planları toplanır). Gereksinim tablosuna dokunulmaz. Bir şey yazıldıysa true.
        /// </summary>
        public static bool MigrateFromRequirements(string stockFolder, IList<VerificationItem> requirements)
        {
            if (requirements == null || requirements.Count == 0 || HasTable(stockFolder, new[] { PlanHeading }) || ReadPlans(stockFolder).Count > 0)
            {
                return false;
            }

            var plans = new List<PlanItem>();
            foreach (var group in requirements.Where(r => r.TpNo.Trim().Length > 0).GroupBy(r => r.TpNo.Trim(), StringComparer.OrdinalIgnoreCase))
            {
                var first = group.First();
                plans.Add(new PlanItem
                {
                    TpNo = group.Key,
                    Description = group.Select(r => r.TpDescription).FirstOrDefault(s => s.Length > 0) ?? string.Empty,
                    Method = group.Select(r => r.Method).FirstOrDefault(s => s.Length > 0) ?? string.Empty,
                    Facility = group.Select(r => r.Facility).FirstOrDefault(s => s.Length > 0) ?? string.Empty,
                    Requirements = group.Select(VerificationList.KeyOf).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                    Note = first.TpNote,
                });
            }

            var reports = new List<ReportItem>();
            foreach (var group in requirements.Where(r => r.TrNo.Trim().Length > 0).GroupBy(r => r.TrNo.Trim(), StringComparer.OrdinalIgnoreCase))
            {
                reports.Add(new ReportItem
                {
                    TrNo = group.Key,
                    Description = group.Select(r => r.TrDescription).FirstOrDefault(s => s.Length > 0) ?? string.Empty,
                    // Sonuç sütunu boşsa satırın durumu Geçti / Kaldı ise o alınır (eski dosyalarda sonuç durumda tutuluyordu).
                    Result = group.Select(r => r.TrResult).FirstOrDefault(s => s.Length > 0)
                        ?? group.Select(r => r.Status).FirstOrDefault(s => s == "Geçti" || s == "Kaldı") ?? string.Empty,
                    Plans = group.Select(r => r.TpNo.Trim()).Where(s => s.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                    ResultPath = group.Select(r => r.ResultPath).FirstOrDefault(s => s.Length > 0) ?? string.Empty,
                    Note = group.Select(r => r.TrNote).FirstOrDefault(s => s.Length > 0) ?? string.Empty,
                });
            }

            if (plans.Count == 0 && reports.Count == 0)
            {
                return false;
            }

            plans = plans.OrderBy(p => p.TpNo, Comparer<string>.Create(CompareNo)).ToList();
            reports = reports.OrderBy(r => r.TrNo, Comparer<string>.Create(CompareNo)).ToList();

            var error = SavePlans(stockFolder, plans);
            if (error.Length == 0 && reports.Count > 0 && ReadReports(stockFolder).Count == 0)
            {
                error = SaveReports(stockFolder, reports);
            }

            return error.Length == 0;
        }

        /// <summary>Eski düzenin yedeğinin konduğu klasör (stok klasöründe).</summary>
        public const string BackupFolder = "_ARSIV";

        /// <summary>
        /// Eski düzenden yeni düzene geçiş (22.11.2026 kullanıcı kararı). Gereksinim tablosundaki TP / TR sütunlarının
        /// bilgisi önce Plan / Rapor tablolarına aktarılır (tablo yoksa kurulur; varsa eksik madde, bağ ve boş alanlar
        /// tamamlanır, dolu alanlara dokunulmaz); sonra sütunlar kalkar, <c>## Sonuç</c> → <c>## Rapor</c>, eski işaretli
        /// madde bölümleri kalkar. Değişiklikten önce dosyanın kopyası <c>_ARSIV\&lt;ad&gt;_&lt;zaman&gt;.md.yedek</c> olarak
        /// alınır (Obsidian'da görünmez). Bir şey değiştiyse true; hata olursa <paramref name="error"/> dolar.
        /// </summary>
        public static bool RetireLegacy(string stockFolder, out string error)
        {
            error = string.Empty;
            var path = VerificationList.PathFor(stockFolder);
            var lines = Lines(stockFolder);
            if (lines == null)
            {
                return false;
            }

            var probe = new List<string>(lines);
            if (!VerificationList.RetireLegacyLines(probe))
            {
                return false;
            }

            try
            {
                var backupDir = Path.Combine(stockFolder, BackupFolder);
                Directory.CreateDirectory(backupDir);
                var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss", System.Globalization.CultureInfo.InvariantCulture);
                var backup = Path.Combine(backupDir, Path.GetFileNameWithoutExtension(path) + "_" + stamp + ".md.yedek");
                if (!File.Exists(backup))
                {
                    File.Copy(path, backup);
                }
            }
            catch (IOException ex)
            {
                error = ex.Message;
                return false;
            }
            catch (UnauthorizedAccessException ex)
            {
                error = ex.Message;
                return false;
            }

            // 1) Bilgiyi Plan / Rapor tablolarına aktar.
            var requirements = VerificationList.Read(stockFolder);
            if (VerificationList.HasLegacyColumns(lines))
            {
                MigrateFromRequirements(stockFolder, requirements);
                error = MergeLegacy(stockFolder, requirements);
                if (error.Length > 0)
                {
                    return false;
                }
            }

            // 2) Sütunları, başlığı, eski bölümleri yeni düzene çevir (dosya yeniden okunur: tablolar yazılmış olabilir).
            try
            {
                var text = File.ReadAllText(path);
                var newline = text.Contains("\r\n") ? "\r\n" : "\n";
                var now = text.Replace("\r\n", "\n").Split('\n').ToList();
                if (!VerificationList.RetireLegacyLines(now))
                {
                    return false;
                }

                error = VerificationList.WriteAtomic(path, string.Join(newline, now), Journal.HasBom(path));
                return error.Length == 0;
            }
            catch (IOException ex)
            {
                error = ex.Message;
                return false;
            }
            catch (UnauthorizedAccessException ex)
            {
                error = ex.Message;
                return false;
            }
        }

        // Plan / Rapor tabloları varken eski sütunlardaki bilgiyi tamamlar: eksik madde eklenir, eksik bağ eklenir, boş alan doldurulur.
        private static string MergeLegacy(string stockFolder, IList<VerificationItem> requirements)
        {
            var plans = ReadPlans(stockFolder);
            var reports = ReadReports(stockFolder);
            var plansChanged = false;
            var reportsChanged = false;
            foreach (var req in requirements)
            {
                var key = VerificationList.KeyOf(req);
                var tps = SplitList(req.TpNo);
                foreach (var tp in tps)
                {
                    var plan = plans.FirstOrDefault(p => string.Equals(p.TpNo, tp, StringComparison.OrdinalIgnoreCase));
                    if (plan == null)
                    {
                        plan = new PlanItem { TpNo = tp };
                        plans.Add(plan);
                        plansChanged = true;
                    }

                    if (!plan.Requirements.Contains(key, StringComparer.OrdinalIgnoreCase))
                    {
                        plan.Requirements.Add(key);
                        plansChanged = true;
                    }

                    if (tps.Count == 1)
                    {
                        plansChanged |= Fill(() => plan.Description, v => plan.Description = v, req.TpDescription);
                        plansChanged |= Fill(() => plan.Method, v => plan.Method = v, req.Method);
                        plansChanged |= Fill(() => plan.Facility, v => plan.Facility = v, req.Facility);
                        plansChanged |= Fill(() => plan.Note, v => plan.Note = v, req.TpNote);
                    }
                }

                var trs = SplitList(req.TrNo);
                foreach (var tr in trs)
                {
                    var report = reports.FirstOrDefault(r => string.Equals(r.TrNo, tr, StringComparison.OrdinalIgnoreCase));
                    if (report == null)
                    {
                        report = new ReportItem { TrNo = tr };
                        reports.Add(report);
                        reportsChanged = true;
                    }

                    // Raporun hiç planı yoksa gereksinimin planlarına bağlanır.
                    if (report.Plans.Count == 0 && tps.Count > 0)
                    {
                        report.Plans.AddRange(tps);
                        reportsChanged = true;
                    }

                    if (trs.Count == 1)
                    {
                        reportsChanged |= Fill(() => report.Description, v => report.Description = v, req.TrDescription);
                        reportsChanged |= Fill(() => report.Result, v => report.Result = v, req.TrResult);
                        reportsChanged |= Fill(() => report.ResultPath, v => report.ResultPath = v, req.ResultPath);
                        reportsChanged |= Fill(() => report.Note, v => report.Note = v, req.TrNote);
                    }
                }
            }

            var error = string.Empty;
            if (plansChanged)
            {
                error = SavePlans(stockFolder, plans.OrderBy(p => p.TpNo, Comparer<string>.Create(CompareNo)).ToList());
            }

            if (error.Length == 0 && reportsChanged)
            {
                error = SaveReports(stockFolder, reports.OrderBy(r => r.TrNo, Comparer<string>.Create(CompareNo)).ToList());
            }

            return error;
        }

        // Alan boşsa değeri yazar; yazdıysa true.
        private static bool Fill(Func<string> get, Action<string> set, string value)
        {
            if (get().Trim().Length > 0 || string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            set(value.Trim());
            return true;
        }

        /// <summary>Madde numaralarını karşılaştırır: "4.2" &lt; "4.10" (nokta, tire ya da boşlukla ayrılan sayılar sayı olarak).</summary>
        public static int CompareNo(string a, string b)
        {
            var pa = (a ?? string.Empty).Split('.', '-', ' ');
            var pb = (b ?? string.Empty).Split('.', '-', ' ');
            for (var i = 0; i < Math.Min(pa.Length, pb.Length); i++)
            {
                int na, nb;
                var c = int.TryParse(pa[i], out na) && int.TryParse(pb[i], out nb)
                    ? na.CompareTo(nb)
                    : string.Compare(pa[i], pb[i], StringComparison.OrdinalIgnoreCase);
                if (c != 0)
                {
                    return c;
                }
            }

            return pa.Length.CompareTo(pb.Length);
        }

        /// <summary>
        /// Sıradaki madde numarası: en büyük numaranın son sayısı bir artar ("4.4" → "4.5", "TP-9" → "TP-10").
        /// Liste boşsa ya da son parça sayı değilse <paramref name="first"/>.
        /// </summary>
        public static string NextNo(IEnumerable<string> numbers, string first)
        {
            var last = (numbers ?? Enumerable.Empty<string>()).Where(n => !string.IsNullOrWhiteSpace(n))
                .OrderBy(n => n, Comparer<string>.Create(CompareNo)).LastOrDefault();
            if (last == null)
            {
                return first;
            }

            var end = last.Length;
            var start = end;
            while (start > 0 && char.IsDigit(last[start - 1]))
            {
                start--;
            }

            int value;
            if (start == end || !int.TryParse(last.Substring(start, end - start), out value))
            {
                return first;
            }

            return last.Substring(0, start) + (value + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>Bağ listesinde bir numarayı yenisiyle değiştirir (yeni boşsa çıkarır). Değiştiyse true.</summary>
        public static bool RenameLink(List<string> links, string oldNo, string newNo)
        {
            var index = links.FindIndex(l => string.Equals(l, oldNo, StringComparison.OrdinalIgnoreCase));
            if (index < 0 || string.Equals(links[index], newNo, StringComparison.Ordinal))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(newNo) || links.Any(l => string.Equals(l, newNo, StringComparison.OrdinalIgnoreCase)))
            {
                links.RemoveAt(index);
            }
            else
            {
                links[index] = newNo.Trim();
            }

            return true;
        }

        /// <summary>"DGÖ-1, DGÖ-2" → ["DGÖ-1", "DGÖ-2"] (virgül ya da noktalı virgül; boşlar atlanır, tekrarsız).</summary>
        public static List<string> SplitList(string text)
        {
            return (text ?? string.Empty).Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim()).Where(s => s.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static string Cell(Dictionary<int, string> row, int known)
        {
            string value;
            return row.TryGetValue(known, out value) ? value : string.Empty;
        }

        private static void CopyExtra(Dictionary<int, string> row, Dictionary<string, string> extra, string[] headers)
        {
            string names;
            if (!row.TryGetValue(ExtraNamesKey, out names) || names.Length == 0)
            {
                return;
            }

            var list = names.Split('\u0001');
            for (var i = 0; i < list.Length; i++)
            {
                string value;
                if (row.TryGetValue(ExtraBase - i, out value))
                {
                    extra[list[i]] = value;
                }
            }
        }

        private const int ExtraNamesKey = -1;
        private const int ExtraBase = -1000;
        private static string Escape(string value)
        {
            return (value ?? string.Empty).Replace("\r", " ").Replace("\n", " ").Replace("|", "\\|").Trim();
        }

        private static int KnownIndex(string header, string[] headers)
        {
            var h = Norm(header);
            for (var i = 0; i < headers.Length; i++)
            {
                if (Norm(headers[i]) == h)
                {
                    return i;
                }
            }

            return -1;
        }

        private static string Norm(string text)
        {
            return new string((text ?? string.Empty).ToLowerInvariant().Where(c => !char.IsWhiteSpace(c) && c != '.' && c != '_' && c != '-').ToArray());
        }

        // Bölüm: "## <başlık>" satırından sonraki "## " ya da "# " satırına kadar.
        private static bool FindSection(List<string> lines, string[] headings, out int start, out int end)
        {
            start = -1;
            end = lines.Count;
            foreach (var heading in headings)
            {
                start = lines.FindIndex(l => l.StartsWith("## ", StringComparison.Ordinal)
                    && string.Equals(l.Substring(3).Trim(), heading, StringComparison.CurrentCultureIgnoreCase));
                if (start >= 0)
                {
                    break;
                }
            }

            if (start < 0)
            {
                return false;
            }

            for (var i = start + 1; i < lines.Count; i++)
            {
                if (lines[i].StartsWith("## ", StringComparison.Ordinal) || lines[i].StartsWith("# ", StringComparison.Ordinal))
                {
                    end = i;
                    break;
                }
            }

            return true;
        }

        // Bölümdeki ilk tablo: (başlık satırı, bitiş) ya da (-1, -1).
        private static void FindTable(List<string> lines, int start, int end, out int tableStart, out int tableEnd)
        {
            tableStart = -1;
            tableEnd = -1;
            for (var i = start + 1; i + 1 < end; i++)
            {
                if (IsRow(lines[i]) && IsSeparator(lines[i + 1]))
                {
                    tableStart = i;
                    tableEnd = i + 2;
                    while (tableEnd < end && IsRow(lines[tableEnd]))
                    {
                        tableEnd++;
                    }

                    return;
                }
            }
        }

        private static bool HasTable(string stockFolder, string[] headings)
        {
            var lines = Lines(stockFolder);
            int start, end, ts, te;
            if (lines == null || !FindSection(lines, headings, out start, out end))
            {
                return false;
            }

            FindTable(lines, start, end, out ts, out te);
            return ts >= 0;
        }

        private static List<string>? Lines(string stockFolder)
        {
            var path = VerificationList.PathFor(stockFolder);
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                return File.ReadAllText(path).Replace("\r\n", "\n").Split('\n').ToList();
            }
            catch (IOException)
            {
                return null;
            }
            catch (UnauthorizedAccessException)
            {
                return null;
            }
        }

        // Satırlar: bilinen sütun sırası → değer; tanınmayan sütunlar ExtraBase - n ile, adları ExtraNamesKey'de.
        private static List<Dictionary<int, string>> ReadRows(string stockFolder, string[] headings, string[] headers)
        {
            var rows = new List<Dictionary<int, string>>();
            var lines = Lines(stockFolder);
            int start, end, ts, te;
            if (lines == null || !FindSection(lines, headings, out start, out end))
            {
                return rows;
            }

            FindTable(lines, start, end, out ts, out te);
            if (ts < 0)
            {
                return rows;
            }

            var head = SplitRow(lines[ts]);
            var keys = head.Select(h => KnownIndex(h, headers)).ToArray();
            var extras = new List<string>();
            for (var c = 0; c < keys.Length; c++)
            {
                if (keys[c] < 0 && head[c].Length > 0)
                {
                    keys[c] = ExtraBase - extras.Count;
                    extras.Add(head[c]);
                }
            }

            for (var i = ts + 2; i < te; i++)
            {
                var cells = SplitRow(lines[i]);
                var row = new Dictionary<int, string> { { ExtraNamesKey, string.Join("\u0001", extras) } };
                for (var c = 0; c < keys.Length; c++)
                {
                    if (keys[c] != -1)
                    {
                        row[keys[c]] = c < cells.Count ? cells[c] : string.Empty;
                    }
                }

                rows.Add(row);
            }

            return rows;
        }

        private static string WriteRows(string stockFolder, string[] headings, string[] headers, List<Tuple<string[], Dictionary<string, string>>> items)
        {
            var path = VerificationList.PathFor(stockFolder);
            try
            {
                if (!File.Exists(path))
                {
                    return "missing";
                }

                var text = File.ReadAllText(path);
                var newline = text.Contains("\r\n") ? "\r\n" : "\n";
                var lines = text.Replace("\r\n", "\n").Split('\n').ToList();

                // Ek sütunlar: var olan tablodakiler sırasıyla, sonra maddelerde olup tabloda olmayanlar.
                var order = new List<string>();
                int start, end, ts = -1, te = -1;
                if (FindSection(lines, headings, out start, out end))
                {
                    FindTable(lines, start, end, out ts, out te);
                    if (ts >= 0)
                    {
                        order.AddRange(SplitRow(lines[ts]).Where(h => h.Length > 0 && KnownIndex(h, headers) < 0));
                    }
                }

                foreach (var name in items.SelectMany(r => r.Item2.Keys))
                {
                    if (!order.Contains(name, StringComparer.OrdinalIgnoreCase))
                    {
                        order.Add(name);
                    }
                }

                var head = "| " + string.Join(" | ", headers.Concat(order)) + " |";
                var sep = "|" + string.Join("|", Enumerable.Repeat("---", headers.Length + order.Count)) + "|";
                var table = new List<string> { head, sep };
                foreach (var item in items)
                {
                    var cells = item.Item1.Select(Escape).ToList();
                    foreach (var name in order)
                    {
                        string v;
                        cells.Add(Escape(item.Item2.TryGetValue(name, out v) ? v : string.Empty));
                    }

                    table.Add("| " + string.Join(" | ", cells) + " |");
                }
                if (start < 0)
                {
                    while (lines.Count > 0 && lines[lines.Count - 1].Trim().Length == 0)
                    {
                        lines.RemoveAt(lines.Count - 1);
                    }

                    lines.Add(string.Empty);
                    lines.Add("## " + headings[0]);
                    lines.Add(string.Empty);
                    lines.AddRange(table);
                    lines.Add(string.Empty);
                }
                else if (ts >= 0)
                {
                    lines.RemoveRange(ts, te - ts);
                    lines.InsertRange(ts, table);
                }
                else
                {
                    // Bölüm var, tablo yok: başlığın hemen altına (bir boş satırla).
                    var at = start + 1;
                    var block = new List<string> { string.Empty };
                    block.AddRange(table);
                    if (at < lines.Count && lines[at].Trim().Length > 0)
                    {
                        block.Add(string.Empty);
                    }

                    lines.InsertRange(at, block);
                }

                return VerificationList.WriteAtomic(path, string.Join(newline, lines), Journal.HasBom(path));
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

        private static bool IsRow(string line)
        {
            return line.TrimStart().StartsWith("|", StringComparison.Ordinal);
        }

        private static bool IsSeparator(string line)
        {
            var t = line.Trim();
            return t.StartsWith("|", StringComparison.Ordinal) && t.Contains("-") && t.All(c => c == '|' || c == '-' || c == ':' || c == ' ');
        }

        private static List<string> SplitRow(string line)
        {
            var cells = new List<string>();
            var t = line.Trim();
            if (t.StartsWith("|", StringComparison.Ordinal))
            {
                t = t.Substring(1);
            }

            if (t.EndsWith("|", StringComparison.Ordinal) && !t.EndsWith("\\|", StringComparison.Ordinal))
            {
                t = t.Substring(0, t.Length - 1);
            }

            var cell = new StringBuilder();
            for (var i = 0; i < t.Length; i++)
            {
                if (t[i] == '\\' && i + 1 < t.Length && t[i + 1] == '|')
                {
                    cell.Append('|');
                    i++;
                }
                else if (t[i] == '|')
                {
                    cells.Add(cell.ToString().Trim());
                    cell.Clear();
                }
                else
                {
                    cell.Append(t[i]);
                }
            }

            cells.Add(cell.ToString().Trim());
            return cells;
        }
    }
}
