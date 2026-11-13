using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using Pusula.Core.Scanning;

namespace Pusula.Core.Testing
{
    /// <summary>
    /// Bir stokun test koşularını okur. Koşu = <c>&lt;SİSTEM&gt;\&lt;SN&gt;_T&lt;tur&gt;</c> klasörü
    /// (<c>_P&lt;tur&gt;</c> da kabul edilir). Aranan yerler:
    /// <c>03_Test_Sonuclari</c> (geliştirme) ve <c>04_Kalifikasyon\03_Test_Sonuclari</c> (kalifikasyon).
    /// Sonuç sırasıyla: <c>test.md</c> YAML'ı → KIWI <c>_kosu.md</c> → bilinmiyor.
    /// </summary>
    public static class TestRunReader
    {
        /// <summary>Koşu klasörü adı: SN01_T1, SNP1_T2, SN01_P1 …</summary>
        private static readonly Regex RunFolder = new Regex(@"^(SN[A-Za-z0-9]*)_[TtPp](\d+)$", RegexOptions.CultureInvariant);

        private static readonly Regex KiwiResult = new Regex(@"Genel sonuç:\s*\**\s*([^\*\r\n]+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static readonly Regex KiwiStart = new Regex(@"\|\s*Başlangıç\s*\|\s*([0-9T:\-\+]+)", RegexOptions.CultureInvariant);

        private static readonly Regex KiwiPerson = new Regex(@"\|\s*Kişi\s*\|\s*([^|\r\n]+?)\s*\|", RegexOptions.CultureInvariant);

        private static readonly Regex KiwiPlan = new Regex(@"\|\s*Plan\s*\|\s*([^·|\r\n]+)", RegexOptions.CultureInvariant);

        /// <summary>Geliştirme testlerinin klasörü (stoka göre).</summary>
        public const string DevelopmentFolder = "03_Test_Sonuclari";

        /// <summary>Kalifikasyon testlerinin klasörü (stoka göre).</summary>
        public static readonly string QualificationFolder = Path.Combine("04_Kalifikasyon", "03_Test_Sonuclari");

        /// <summary>Stokun bütün koşuları, en yeni üstte.</summary>
        public static List<TestRun> ReadStock(string stockFolder)
        {
            var runs = new List<TestRun>();
            ReadArea(Path.Combine(stockFolder, DevelopmentFolder), false, runs);
            ReadArea(Path.Combine(stockFolder, QualificationFolder), true, runs);
            return runs.OrderByDescending(r => r.Date).ThenBy(r => r.System).ThenBy(r => r.SerialNo).ToList();
        }

        private static void ReadArea(string area, bool qualification, List<TestRun> runs)
        {
            foreach (var systemFolder in SafeDirectories(area))
            {
                var system = Path.GetFileName(systemFolder);
                if (system.StartsWith("_", StringComparison.Ordinal))
                {
                    continue;
                }

                foreach (var runFolder in SafeDirectories(systemFolder))
                {
                    var match = RunFolder.Match(Path.GetFileName(runFolder));
                    if (!match.Success)
                    {
                        continue;
                    }

                    var run = new TestRun
                    {
                        Folder = runFolder,
                        System = system,
                        SerialNo = match.Groups[1].Value.ToUpperInvariant(),
                        Round = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture),
                        IsQualification = qualification,
                        Date = EarliestWrite(runFolder),
                    };

                    if (!ReadNote(run) && !ReadKiwi(run, false))
                    {
                        run.Source = TestRunSource.Folder;
                    }

                    if (run.AnalysisDate == DateTime.MinValue)
                    {
                        run.AnalysisDate = run.Date;
                    }

                    run.Photo = FirstPhoto(runFolder);

                    runs.Add(run);
                }
            }
        }

        // test.md: YAML başlığı (sonuc, tarih, kisi, plan, jig) + gövdeden kısa açıklama.
        private static bool ReadNote(TestRun run)
        {
            var path = Path.Combine(run.Folder, "test.md");
            if (!Fs.FileExists(path))
            {
                return false;
            }

            string body;
            var fields = FrontMatter.Parse(SafeRead(path), out body);
            run.NotePath = path;
            run.Source = TestRunSource.Note;
            run.ResultText = Get(fields, "sonuc", "sonuç", "result");
            run.Result = ParseResult(run.ResultText);
            run.Person = Get(fields, "kisi", "kişi", "person");
            run.Plan = Get(fields, "plan");
            run.Jig = Get(fields, "jig");
            run.Summary = SummaryOf(body);
            run.Body = body.Trim();

            DateTime date;
            if (DateTime.TryParseExact(Get(fields, "tarih", "date"), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            {
                run.Date = date;
            }

            // Analiz testten sonra yapıldıysa ayrı tarih; yoksa test tarihi sayılır.
            if (DateTime.TryParseExact(Get(fields, "analiz_tarihi", "analiz"), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            {
                run.AnalysisDate = date;
            }

            run.AnalysisNote = Get(fields, "analiz_notu");

            // Notta sonuç yoksa KIWI koşusundan tamamlanır.
            if (run.Result == TestResult.Unknown)
            {
                var notePath = run.NotePath;
                if (ReadKiwi(run, true))
                {
                    run.NotePath = notePath;
                    run.Source = TestRunSource.Note;
                }
            }

            return true;
        }

        // KIWI: koşu klasörünün altında herhangi bir _kosu.md (en yenisi). Nottan çağrılırsa notun tarihi korunur.
        private static bool ReadKiwi(TestRun run, bool fromNote)
        {
            string[] files;
            try
            {
                files = Fs.GetFiles(run.Folder, "_kosu.md", SearchOption.AllDirectories);
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }

            var path = files.OrderByDescending(f => Fs.GetLastWriteTime(f)).FirstOrDefault();
            if (path == null)
            {
                return false;
            }

            var text = SafeRead(path);
            var result = KiwiResult.Match(text);
            if (!result.Success)
            {
                return false;
            }

            run.ResultText = result.Groups[1].Value.Trim();
            run.Result = ParseResult(run.ResultText);
            run.Source = TestRunSource.Kiwi;
            if (string.IsNullOrEmpty(run.NotePath))
            {
                run.NotePath = path;
            }

            var start = KiwiStart.Match(text);
            DateTimeOffset when;
            if (!fromNote && start.Success && DateTimeOffset.TryParse(start.Groups[1].Value, CultureInfo.InvariantCulture, DateTimeStyles.None, out when))
            {
                run.Date = when.LocalDateTime;
            }

            if (string.IsNullOrEmpty(run.Person))
            {
                var person = KiwiPerson.Match(text);
                run.Person = person.Success ? person.Groups[1].Value.Trim() : string.Empty;
            }

            if (string.IsNullOrEmpty(run.Plan))
            {
                var plan = KiwiPlan.Match(text);
                run.Plan = plan.Success ? plan.Groups[1].Value.Trim() : string.Empty;
            }

            if (string.IsNullOrEmpty(run.Summary))
            {
                run.Summary = "KIWI";
            }

            return true;
        }

        /// <summary>
        /// Koşunun analiz tarihini ve kısa notunu <c>test.md</c>'nin YAML başlığına yazar (gövdeye dokunmaz). Not yoksa
        /// test tarihiyle oluşturulur. Analiz tarihi test tarihiyle aynıysa ya da verilmemişse <c>analiz_tarihi</c> kaldırılır.
        /// Hata metni; başarıda boş.
        /// </summary>
        public static string SetAnalysis(string runFolder, DateTime testDate, DateTime? analysisDate, string note)
        {
            var path = Path.Combine(runFolder, "test.md");
            if (!File.Exists(path))
            {
                var fields = new List<KeyValuePair<string, string>>();
                if (testDate != DateTime.MinValue)
                {
                    fields.Add(new KeyValuePair<string, string>("tarih", testDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
                }

                if (Notes.NoteFile.Create(path, fields, Path.GetFileName(runFolder)).Length == 0)
                {
                    return path;
                }
            }

            var separate = analysisDate.HasValue && (testDate == DateTime.MinValue || analysisDate.Value.Date != testDate.Date);
            var values = new Dictionary<string, string>
            {
                { "analiz_tarihi", separate ? analysisDate!.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : string.Empty },
                { "analiz_notu", note ?? string.Empty },
            };
            return Notes.Journal.SetFrontMatter(path, values);
        }

        /// <summary>
        /// Sonuç metnini toleranslı çevirir: Geçti/geçti/GECTI/passed → Passed; Kaldı/failed → Failed;
        /// Devam/sürüyor → InProgress; diğerleri Unknown.
        /// </summary>
        public static TestResult ParseResult(string text)
        {
            var value = Fold(text);
            if (value.StartsWith("gect", StringComparison.Ordinal) || value.StartsWith("pass", StringComparison.Ordinal) || value == "ok" || value.StartsWith("tamam", StringComparison.Ordinal))
            {
                return TestResult.Passed;
            }

            if (value.StartsWith("kald", StringComparison.Ordinal) || value.StartsWith("fail", StringComparison.Ordinal) || value.StartsWith("red", StringComparison.Ordinal))
            {
                return TestResult.Failed;
            }

            if (value.StartsWith("devam", StringComparison.Ordinal) || value.StartsWith("sur", StringComparison.Ordinal) || value.StartsWith("in progress", StringComparison.Ordinal) || value.StartsWith("bekl", StringComparison.Ordinal))
            {
                return TestResult.InProgress;
            }

            return TestResult.Unknown;
        }

        // "**Sonuç: Kaldı.** Yan lob …" satırındaki açıklama; yoksa başlık olmayan ilk dolu satır.
        private static string SummaryOf(string body)
        {
            var lines = body.Replace("\r\n", "\n").Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).ToList();
            foreach (var line in lines)
            {
                if (line.StartsWith("**Sonuç", StringComparison.OrdinalIgnoreCase))
                {
                    var close = line.IndexOf("**", 2, StringComparison.Ordinal);
                    var rest = close > 0 ? line.Substring(close + 2).Trim() : string.Empty;
                    if (rest.Length > 0)
                    {
                        return rest;
                    }
                }
            }

            var first = lines.FirstOrDefault(l => !l.StartsWith("#", StringComparison.Ordinal) && !l.StartsWith("![[", StringComparison.Ordinal) && !l.StartsWith("**Sonuç", StringComparison.OrdinalIgnoreCase));
            return (first ?? string.Empty).TrimStart('-', ' ');
        }

        private static readonly string[] PhotoExtensions = { ".png", ".jpg", ".jpeg", ".bmp" };

        /// <summary>Koşunun ilk fotoğrafı: önce <c>Foto\</c>, sonra koşu klasörünün kendisi; ada göre ilk.</summary>
        public static string FirstPhoto(string runFolder)
        {
            foreach (var folder in new[] { Path.Combine(runFolder, "Foto"), runFolder })
            {
                try
                {
                    if (!Fs.DirectoryExists(folder))
                    {
                        continue;
                    }

                    var photo = Fs.GetFiles(folder)
                        .Where(f => PhotoExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                        .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                        .FirstOrDefault();
                    if (photo != null)
                    {
                        return photo;
                    }
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }

            return string.Empty;
        }

        private static string Get(Dictionary<string, string> fields, params string[] keys)
        {
            foreach (var key in keys)
            {
                string value;
                if (fields.TryGetValue(key, out value) && value.Length > 0)
                {
                    return value;
                }
            }

            return string.Empty;
        }

        // Küçük harf, Türkçe karaktersiz, noktalamasız: "Geçti." → "gecti".
        private static string Fold(string text)
        {
            var builder = new StringBuilder();
            foreach (var c in (text ?? string.Empty).Trim().ToLower(CultureInfo.GetCultureInfo("tr-TR")))
            {
                switch (c)
                {
                    case 'ç': builder.Append('c'); break;
                    case 'ğ': builder.Append('g'); break;
                    case 'ı': builder.Append('i'); break;
                    case 'ö': builder.Append('o'); break;
                    case 'ş': builder.Append('s'); break;
                    case 'ü': builder.Append('u'); break;
                    case '.': break;
                    case '!': break;
                    default: builder.Append(c); break;
                }
            }

            return builder.ToString();
        }

        private static DateTime EarliestWrite(string folder)
        {
            try
            {
                var times = Fs.GetFiles(folder, "*", SearchOption.AllDirectories).Where(f => !Scanning.FileRules.IsJunkFile(f)).Select(Fs.GetLastWriteTime).ToList();
                return times.Count > 0 ? times.Min() : Directory.GetLastWriteTime(folder); // klasörün kendi tarihi taramada yok; yalnız boş klasörde
            }
            catch (IOException)
            {
                return DateTime.MinValue;
            }
            catch (UnauthorizedAccessException)
            {
                return DateTime.MinValue;
            }
        }

        private static string SafeRead(string path)
        {
            try
            {
                return Fs.ReadAllText(path);
            }
            catch (IOException)
            {
                return string.Empty;
            }
            catch (UnauthorizedAccessException)
            {
                return string.Empty;
            }
        }

        private static IEnumerable<string> SafeDirectories(string folder)
        {
            try
            {
                return Fs.DirectoryExists(folder)
                    ? Fs.GetDirectories(folder).OrderBy(d => d, StringComparer.OrdinalIgnoreCase).ToList()
                    : new List<string>();
            }
            catch (IOException)
            {
                return new List<string>();
            }
            catch (UnauthorizedAccessException)
            {
                return new List<string>();
            }
        }
    }
}
