using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Pusula.Core.Notes;
using Pusula.Core.Scanning;

namespace Pusula.Core.Testing
{
    /// <summary>Doğrulama listesinde bir madde (md tablosunda bir satır).</summary>
    public sealed class VerificationItem
    {
        /// <summary>Sıra numarası (# sütunu).</summary>
        public int Number { get; set; }

        /// <summary>Başlık (kısa ad, örn. "Yanca hüzme genişliği"); henüz bilinmiyorsa boş (alt grup yazılıdır, sonra doldurulur).</summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>Kısa not: ölçüt ya da sınırın tek satırlık özeti (örn. "Azimutta −3 dB genişliği 60° ± 5°").</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>Tam gereksinim metni (satır açılınca gösterilir); isteğe bağlı.</summary>
        public string Requirement { get; set; } = string.Empty;

        /// <summary>Kaynak doküman: TG no yoksa (tasarımcı dokümanı gibi) maddenin geldiği dokümanın numarası.</summary>
        public string Source { get; set; } = string.Empty;

        /// <summary>Tür: Temel (şartnamede yazan) ya da Türetilmiş (tasarımcının temel için koyduğu); boş olabilir.</summary>
        public string Kind { get; set; } = string.Empty;

        /// <summary>Türetilmiş maddenin bağlı olduğu temel madde (TG no), örn. "TG-1".</summary>
        public string Parent { get; set; } = string.Empty;

        /// <summary>Doğrulama yöntemi: Test, Muayene, Belge, Gösterim, Diğer (test planında belirlenir).</summary>
        public string Method { get; set; } = string.Empty;

        /// <summary>Grup: İşlevsel, Özellik, ÇK/EMC, Diğer.</summary>
        public string Group { get; set; } = string.Empty;

        /// <summary>Alt grup (isteğe bağlı), örn. "Anten örüntü".</summary>
        public string SubGroup { get; set; } = string.Empty;

        /// <summary>Altyapı (isteğe bağlı), örn. "SATIMO".</summary>
        public string Facility { get; set; } = string.Empty;

        /// <summary>Test gereksinimi dokümanındaki madde numarası (TG-1); tasarımcı dokümanından gelen maddede boş olabilir.</summary>
        public string TgNo { get; set; } = string.Empty;

        /// <summary>Test planındaki madde numarası (TP-5.1); isteğe bağlı.</summary>
        public string TpNo { get; set; } = string.Empty;

        /// <summary>Test planındaki maddenin açıklaması (örn. "SATIMO hüzme ölçümü"); isteğe bağlı.</summary>
        public string TpDescription { get; set; } = string.Empty;

        /// <summary>Test planı notu (jig, SN, dikkat edilecekler); isteğe bağlı.</summary>
        public string TpNote { get; set; } = string.Empty;

        /// <summary>Test raporundaki madde numarası (TR-5.1); isteğe bağlı.</summary>
        public string TrNo { get; set; } = string.Empty;

        /// <summary>Test raporundaki maddenin açıklaması; isteğe bağlı.</summary>
        public string TrDescription { get; set; } = string.Empty;

        /// <summary>Test raporu notu (ölçülen değer, kaldıysa bildirim no); isteğe bağlı.</summary>
        public string TrNote { get; set; } = string.Empty;

        /// <summary>Test raporundaki sonuç: Geçti, Kaldı ya da Hazırlanmadı; boş olabilir.</summary>
        public string TrResult { get; set; } = string.Empty;

        /// <summary>Sonucun yeri: ölçüm klasörü ya da dosya (stok klasörüne göre göreli ya da tam yol); boş olabilir.</summary>
        public string ResultPath { get; set; } = string.Empty;

        /// <summary>Durum metni: Bekliyor, Planlandı, Hazırlık yapılıyor, Yapılıyor, Değerlendiriliyor, Geçti, Kaldı, Şartlı kabul, Tekrarlanacak, Kapsam dışı.</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>Tabloda uygulamanın tanımadığı sütunların değerleri (başlık → değer); yazarken korunur.</summary>
        public Dictionary<string, string> Extra { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Stokun doğrulama listesi: <c>03_Testler\&lt;stok&gt;_Test_Gereksinimleri.md</c> içindeki ilk tablo.
    /// Sütunlar başlıklarından tanınır: # · TG no · kaynak · başlık · kısa not · gereksinim · grup · alt grup · tür · bağlı ·
    /// yöntem · altyapı · TP no · TP açıklama · TP not · TR no · TR açıklama · TR not · TR sonuç · sonuç yeri · durum (sıra serbest, tanınmayan sütunlar korunur). Eski tablolarda eksik sütunlar
    /// ilk yazımda eklenir. Uygulama tabloya satır
    /// ekler, satırı değiştirir ya da kaldırır (16.11.2026 kullanıcı kararı); dosyanın geri kalanına dokunmaz.
    /// </summary>
    public static class VerificationList
    {
        /// <summary>Doğrulama dosyasının durduğu klasör.</summary>
        public const string FolderName = "03_Testler";

        /// <summary>Dosya adının stok numarasından sonraki kısmı.</summary>
        public const string FileSuffix = "_Test_Gereksinimleri";

        /// <summary>Birim notunun YAML başlığında doğrulama dosyasına bağlantıyı tutan alan.</summary>
        public const string LinkKey = "test_gereksinimleri";

        // Sütun anahtarları (tablodaki tanınan sütunlar).
        private const int ColNumber = 0;
        private const int ColTg = 1;
        private const int ColSource = 2;
        private const int ColText = 3;
        private const int ColDescription = 4;
        private const int ColRequirement = 5;
        private const int ColGroup = 6;
        private const int ColSubGroup = 7;
        private const int ColKind = 8;
        private const int ColParent = 9;
        private const int ColMethod = 10;
        private const int ColFacility = 11;
        private const int ColTp = 12;
        private const int ColTpDescription = 13;
        private const int ColTpNote = 14;
        private const int ColTr = 15;
        private const int ColTrDescription = 16;
        private const int ColTrNote = 17;
        private const int ColTrResult = 18;
        private const int ColResultPath = 19;
        private const int ColStatus = 20;
        private const int KnownCount = 21;

        /// <summary>Yeni ya da yükseltilen tablonun başlıkları (sırasıyla).</summary>
        public static readonly string[] Headers = { "#", "TG no", "Kaynak", "Başlık", "Kısa not", "Gereksinim", "Grup", "Alt grup", "Tür", "Bağlı", "Yöntem", "Altyapı", "TP no", "TP açıklama", "TP not", "TR no", "TR açıklama", "TR not", "TR sonuç", "Sonuç yeri", "Durum" };

        // Eski (16.11.2026 öncesi) 7 sütunlu tablonun sırası; başlık tanınmazsa kullanılır.
        private static readonly int[] LegacyOrder = { ColNumber, ColText, ColGroup, ColSubGroup, ColFacility, ColTg, ColStatus };

        /// <summary>
        /// Stokun doğrulama dosyası: klasörde adı <c>_Test_Gereksinimleri</c> içeren bir md varsa o (revizyon ekli olabilir),
        /// yoksa <c>&lt;stok&gt;_Test_Gereksinimleri.md</c> (henüz oluşmamış olabilir).
        /// </summary>
        public static string PathFor(string stockFolder)
        {
            var folder = Path.Combine(stockFolder, FolderName);
            if (Directory.Exists(folder))
            {
                var existing = Directory.EnumerateFiles(folder, "*.md")
                    .Where(f => Path.GetFileName(f).IndexOf(FileSuffix, StringComparison.OrdinalIgnoreCase) >= 0)
                    .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                    .FirstOrDefault();
                if (existing != null)
                {
                    return existing;
                }
            }

            return Path.Combine(folder, Path.GetFileName(stockFolder.TrimEnd('\\', '/')) + FileSuffix + ".md");
        }

        /// <summary>Resmî olmayan (tasarımcı dokümanı, sözlü istek) maddelerin TG no öneki: <c>EK-1</c>, <c>EK-2</c> …</summary>
        public const string ExtraPrefix = "EK-";

        /// <summary>
        /// Maddenin kayıtlarda kullanılan anahtarı: TG no; yoksa <c>No 5</c> (sıra numarasıyla). Günlükte
        /// <c>madde: TG-1</c> olarak yazılır.
        /// </summary>
        public static string KeyOf(VerificationItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            return item.TgNo.Trim().Length > 0 ? item.TgNo.Trim() : "No " + item.Number.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>Sıradaki ek madde numarası (<c>EK-</c> ile başlayan en büyük numara + 1).</summary>
        public static string NextExtraNo(IEnumerable<VerificationItem> items)
        {
            var max = 0;
            foreach (var item in items ?? Enumerable.Empty<VerificationItem>())
            {
                int n;
                var tg = item.TgNo.Trim();
                if (tg.StartsWith(ExtraPrefix, StringComparison.OrdinalIgnoreCase)
                    && int.TryParse(tg.Substring(ExtraPrefix.Length), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out n) && n > max)
                {
                    max = n;
                }
            }

            return ExtraPrefix + (max + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>Maddeleri okur; dosya ya da tablo yoksa boş liste. Metni de alt grubu da boş satırlar atlanır.</summary>
        public static IList<VerificationItem> Read(string stockFolder)
        {
            var items = new List<VerificationItem>();
            var path = PathFor(stockFolder);
            if (!File.Exists(path))
            {
                return items;
            }

            string text;
            try
            {
                text = File.ReadAllText(path);
            }
            catch (IOException)
            {
                return items;
            }
            catch (UnauthorizedAccessException)
            {
                return items;
            }

            var table = Table.Parse(text.Replace("\r\n", "\n").Split('\n').ToList());
            if (table == null)
            {
                return items;
            }

            for (var i = table.Start + 2; i < table.End; i++)
            {
                var item = table.ItemOf(table.Lines[i], items.Count + 1);
                if (item.Text.Length > 0 || item.SubGroup.Length > 0 || item.TgNo.Length > 0)
                {
                    items.Add(item);
                }
            }

            return items;
        }

        /// <summary>
        /// Tablonun sonuna madde ekler; numarayı (en büyük + 1) kendisi verir. Dosya yoksa <paramref name="newFileTemplate"/>
        /// ile oluşturulur (içinde başlık satırı ve ayraçtan oluşan boş tablo olmalı). Hata metni ya da boş döndürür.
        /// </summary>
        public static string Add(string stockFolder, VerificationItem item, string newFileTemplate)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            var path = PathFor(stockFolder);
            try
            {
                var exists = File.Exists(path);
                var text = exists ? File.ReadAllText(path) : (newFileTemplate ?? string.Empty);
                var newline = text.Contains("\r\n") || !exists ? "\r\n" : "\n";
                var lines = text.Replace("\r\n", "\n").Split('\n').ToList();

                var table = Table.Parse(lines);
                if (table == null)
                {
                    // Tablo yoksa yeni başlıklı boş tablo dosyanın sonuna eklenir.
                    while (lines.Count > 0 && lines[lines.Count - 1].Trim().Length == 0)
                    {
                        lines.RemoveAt(lines.Count - 1);
                    }

                    lines.Add(string.Empty);
                    lines.Add(HeaderLine(Headers));
                    lines.Add(SeparatorLine(Headers.Length));
                    lines.Add(string.Empty);
                    table = Table.Parse(lines)!;
                }

                table.Upgrade();
                var max = 0;
                for (var i = table.Start + 2; i < table.End; i++)
                {
                    int n;
                    if (int.TryParse(table.CellOf(table.Lines[i], ColNumber), out n) && n > max)
                    {
                        max = n;
                    }
                }

                item.Number = max + 1;
                table.Lines.Insert(table.End, table.RowOf(item));

                Directory.CreateDirectory(Path.GetDirectoryName(path) ?? stockFolder);
                return WriteAtomic(path, string.Join(newline, table.Lines), exists && Journal.HasBom(path));
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

        /// <summary>
        /// Numarası <see cref="VerificationItem.Number"/> olan satırı verilen maddeyle değiştirir (numara korunur).
        /// Satır bulunamazsa <c>"missing"</c>, hata olursa hata metni, başarıda boş döndürür.
        /// </summary>
        public static string Update(string stockFolder, VerificationItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            return EditRow(stockFolder, item.Number, item);
        }

        /// <summary>
        /// Numarası verilen satırı tablodan kaldırır; diğer satırların numaraları değişmez (TG / not bağları bozulmasın).
        /// Satır bulunamazsa <c>"missing"</c>, hata olursa hata metni, başarıda boş döndürür.
        /// </summary>
        public static string Remove(string stockFolder, int number)
        {
            return EditRow(stockFolder, number, null);
        }

        /// <summary>
        /// Birim notunun YAML başlığına doğrulama dosyasının bağlantısını (<c>test_gereksinimleri: "[[…]]"</c>) yazar.
        /// Alan zaten varsa (elle yazılmış olsa da) dokunulmaz; not ya da doğrulama dosyası yoksa bir şey yapmaz.
        /// Gövdeye dokunulmaz. Hata metni ya da boş döndürür.
        /// </summary>
        public static string LinkFromNote(string notePath, string stockFolder)
        {
            var path = PathFor(stockFolder);
            if (string.IsNullOrEmpty(notePath) || !File.Exists(notePath) || !File.Exists(path))
            {
                return string.Empty;
            }

            try
            {
                string body;
                if (FrontMatter.Parse(File.ReadAllText(notePath), out body).ContainsKey(LinkKey))
                {
                    return string.Empty;
                }
            }
            catch (IOException ex)
            {
                return ex.Message;
            }
            catch (UnauthorizedAccessException ex)
            {
                return ex.Message;
            }

            var link = "[[" + Path.GetFileNameWithoutExtension(path) + "]]";
            return Journal.SetFrontMatter(notePath, new Dictionary<string, string> { { LinkKey, link } });
        }

        /// <summary>Plan bölümünün başlığı (dosyada <c>## Plan</c>).</summary>
        public const string PlanHeading = "Plan";

        /// <summary>Sonuç bölümünün başlığı (dosyada <c>## Sonuç</c>).</summary>
        public const string ResultHeading = "Sonuç";

        /// <summary>
        /// Maddenin okunur bölümlerini yazar (21.11.2026 kullanıcı kararı): <c>## Plan</c> ve <c>## Sonuç</c> altında
        /// <c>### &lt;TG no&gt; · &lt;başlık&gt;</c> başlığı ve <c>%% pusula:plan TG %%</c> … <c>%% pusula:son %%</c> işaretleri
        /// arasında plan / rapor bilgisi. Yalnız işaretlerin arası (ve bölümün başlık satırı) yazılır; altına elle yazılan
        /// ayrıntıya dokunulmaz. Bölüm yoksa ve yazılacak bilgi varsa madde sırasıyla eklenir; <c>## Plan</c> /
        /// <c>## Sonuç</c> başlığı yoksa dosyanın sonuna eklenir. Hata metni ya da boş döndürür.
        /// </summary>
        public static string WriteSections(string stockFolder, VerificationItem item, IList<VerificationItem> order)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            var path = PathFor(stockFolder);
            try
            {
                if (!File.Exists(path))
                {
                    return "missing";
                }

                var text = File.ReadAllText(path);
                var newline = text.Contains("\r\n") ? "\r\n" : "\n";
                var lines = text.Replace("\r\n", "\n").Split('\n').ToList();
                var keys = (order ?? new List<VerificationItem>()).Select(KeyOf).ToList();
                var changed = WriteSection(lines, PlanHeading, "plan", item, PlanLines(item), keys);
                changed |= WriteSection(lines, ResultHeading, "sonuc", item, ResultLines(item), keys);
                if (!changed)
                {
                    return string.Empty;
                }

                return WriteAtomic(path, string.Join(newline, lines), Journal.HasBom(path));
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

        /// <summary>Silinen madde bölümünün yerine yazılan satır (altında elle yazılmış ayrıntı varsa bölüm kalır).</summary>
        public const string RemovedLine = "- *Madde tablodan silindi.*";

        /// <summary>
        /// Madde tablodan silinince okunur bölümlerini toplar: bölümün altında elle yazılmış bir şey yoksa başlık ve işaretli
        /// kısım kaldırılır; varsa elle yazılan korunur, işaretli kısma yalnız <see cref="RemovedLine"/> yazılır.
        /// Hata metni ya da boş döndürür.
        /// </summary>
        public static string RemoveSections(string stockFolder, string key)
        {
            return EditSections(stockFolder, lines =>
            {
                var changed = false;
                foreach (var kind in new[] { "plan", "sonuc" })
                {
                    var at = lines.FindIndex(l => string.Equals(l.Trim(), StartMarker(kind, key), StringComparison.Ordinal));
                    if (at < 0)
                    {
                        continue;
                    }

                    var close = lines.FindIndex(at + 1, l => string.Equals(l.Trim(), EndMarker, StringComparison.Ordinal));
                    if (close < 0)
                    {
                        continue;
                    }

                    // Bölümün sonu: sonraki başlık (### ya da ##) ya da dosya sonu; arada boş olmayan satır = elle yazılmış.
                    var next = close + 1;
                    while (next < lines.Count && !lines[next].StartsWith("#", StringComparison.Ordinal))
                    {
                        next++;
                    }

                    var hasFree = lines.Skip(close + 1).Take(next - close - 1).Any(l => l.Trim().Length > 0);
                    var heading = at > 0 && lines[at - 1].StartsWith("### ", StringComparison.Ordinal) ? at - 1 : at;
                    if (hasFree)
                    {
                        lines.RemoveRange(at + 1, close - at - 1);
                        lines.Insert(at + 1, RemovedLine);
                    }
                    else
                    {
                        var from = heading > 0 && lines[heading - 1].Trim().Length == 0 ? heading - 1 : heading;
                        lines.RemoveRange(from, close - from + 1);
                    }

                    changed = true;
                }

                return changed;
            });
        }

        /// <summary>
        /// Maddenin TG no'su değişince okunur bölümlerinin işaretlerini yeni anahtara çevirir (elle yazılan korunur;
        /// başlık bir sonraki yazımda güncellenir). Yeni anahtarla bölüm zaten varsa dokunulmaz. Hata metni ya da boş döndürür.
        /// </summary>
        public static string RenameSections(string stockFolder, string oldKey, string newKey)
        {
            if (string.Equals(oldKey, newKey, StringComparison.Ordinal))
            {
                return string.Empty;
            }

            return EditSections(stockFolder, lines =>
            {
                var changed = false;
                foreach (var kind in new[] { "plan", "sonuc" })
                {
                    var at = lines.FindIndex(l => string.Equals(l.Trim(), StartMarker(kind, oldKey), StringComparison.Ordinal));
                    if (at < 0 || lines.Any(l => string.Equals(l.Trim(), StartMarker(kind, newKey), StringComparison.Ordinal)))
                    {
                        continue;
                    }

                    lines[at] = StartMarker(kind, newKey);
                    changed = true;
                }

                return changed;
            });
        }

        // Dosyayı satır satır düzenler (satır sonları, BOM korunur); işlev değişiklik yaptıysa atomik yazılır.
        private static string EditSections(string stockFolder, Func<List<string>, bool> edit)
        {
            var path = PathFor(stockFolder);
            try
            {
                if (!File.Exists(path))
                {
                    return string.Empty;
                }

                var text = File.ReadAllText(path);
                var newline = text.Contains("\r\n") ? "\r\n" : "\n";
                var lines = text.Replace("\r\n", "\n").Split('\n').ToList();
                return edit(lines) ? WriteAtomic(path, string.Join(newline, lines), Journal.HasBom(path)) : string.Empty;
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

        /// <summary>Plan bölümünün satırları: TP · açıklama, yöntem · altyapı, not (boş olanlar atlanır).</summary>
        public static List<string> PlanLines(VerificationItem item)
        {
            var lines = new List<string>();
            var tp = Join(" · ", item.TpNo, item.TpDescription);
            if (tp.Length > 0)
            {
                lines.Add("- **TP:** " + tp);
            }

            var parts = new List<string>();
            if (item.Method.Length > 0)
            {
                parts.Add("**Yöntem:** " + item.Method);
            }

            if (item.Facility.Length > 0)
            {
                parts.Add("**Altyapı:** " + item.Facility);
            }

            if (parts.Count > 0)
            {
                lines.Add("- " + string.Join(" · ", parts));
            }

            if (item.TpNote.Length > 0)
            {
                lines.Add("- **Not:** " + item.TpNote);
            }

            return lines;
        }

        /// <summary>Sonuç bölümünün satırları: TR · açıklama, sonuç, yer, not (boş olanlar atlanır).</summary>
        public static List<string> ResultLines(VerificationItem item)
        {
            var lines = new List<string>();
            var tr = Join(" · ", item.TrNo, item.TrDescription);
            if (tr.Length > 0)
            {
                lines.Add("- **TR:** " + tr);
            }

            if (item.TrResult.Length > 0)
            {
                lines.Add("- **Sonuç:** " + item.TrResult);
            }

            if (item.ResultPath.Length > 0)
            {
                lines.Add("- **Yer:** `" + item.ResultPath + "`");
            }

            if (item.TrNote.Length > 0)
            {
                lines.Add("- **Not:** " + item.TrNote);
            }

            return lines;
        }

        private static string Join(string separator, params string[] parts)
        {
            return string.Join(separator, parts.Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p.Trim()));
        }

        private const string EndMarker = "%% pusula:son %%";

        private static string StartMarker(string kind, string key)
        {
            return "%% pusula:" + kind + " " + key + " %%";
        }

        // Bölüm başlığı ("## Plan") ve sonu (sonraki "## " ya da dosya sonu).
        private static void FindSection(List<string> lines, string heading, out int start, out int end)
        {
            start = lines.FindIndex(l => l.StartsWith("## ", StringComparison.Ordinal)
                && string.Equals(l.Substring(3).Trim(), heading, StringComparison.CurrentCultureIgnoreCase));
            end = lines.Count;
            if (start < 0)
            {
                return;
            }

            for (var i = start + 1; i < lines.Count; i++)
            {
                if (lines[i].StartsWith("## ", StringComparison.Ordinal) || lines[i].StartsWith("# ", StringComparison.Ordinal))
                {
                    end = i;
                    break;
                }
            }
        }

        // Bir bölümde maddenin işaretli kısmını yazar; değişiklik olduysa true.
        private static bool WriteSection(List<string> lines, string heading, string kind, VerificationItem item, List<string> body, List<string> order)
        {
            var key = KeyOf(item);
            var title = "### " + key + (item.Text.Trim().Length > 0 ? " · " + item.Text.Trim() : string.Empty);
            var marker = StartMarker(kind, key);
            int start, end;
            FindSection(lines, heading, out start, out end);
            var at = start < 0 ? -1 : lines.FindIndex(start, end - start, l => string.Equals(l.Trim(), marker, StringComparison.Ordinal));
            if (at >= 0)
            {
                var close = lines.FindIndex(at + 1, end - at - 1, l => string.Equals(l.Trim(), EndMarker, StringComparison.Ordinal));
                if (close < 0)
                {
                    // Bitiş işareti silinmişse dokunulmaz (elle yazılanı ezmemek için).
                    return false;
                }

                var old = lines.GetRange(at + 1, close - at - 1);
                var headingChanged = at > 0 && lines[at - 1].StartsWith("### ", StringComparison.Ordinal) && lines[at - 1] != title;
                if (old.SequenceEqual(body) && !headingChanged)
                {
                    return false;
                }

                lines.RemoveRange(at + 1, close - at - 1);
                lines.InsertRange(at + 1, body);
                if (headingChanged)
                {
                    lines[at - 1] = title;
                }

                return true;
            }

            if (body.Count == 0)
            {
                return false;
            }

            if (start < 0)
            {
                while (lines.Count > 0 && lines[lines.Count - 1].Trim().Length == 0)
                {
                    lines.RemoveAt(lines.Count - 1);
                }

                lines.Add(string.Empty);
                lines.Add("## " + heading);
                lines.Add(string.Empty);
                FindSection(lines, heading, out start, out end);
            }

            // Madde sırasıyla: sıradaki maddelerden ilkinin bölüm başlığından önce; yoksa bölümün sonuna.
            var mine = order.FindIndex(k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase));
            var insertAt = -1;
            if (mine >= 0)
            {
                for (var i = start + 1; i < end && insertAt < 0; i++)
                {
                    var t = lines[i].Trim();
                    if (!t.StartsWith("%% pusula:" + kind + " ", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    var other = t.Substring(("%% pusula:" + kind + " ").Length).TrimEnd('%', ' ');
                    var index = order.FindIndex(k => string.Equals(k, other, StringComparison.OrdinalIgnoreCase));
                    if (index > mine)
                    {
                        insertAt = i > 0 && lines[i - 1].StartsWith("### ", StringComparison.Ordinal) ? i - 1 : i;
                    }
                }
            }

            if (insertAt < 0)
            {
                insertAt = end;
                while (insertAt - 1 > start && lines[insertAt - 1].Trim().Length == 0)
                {
                    insertAt--;
                }
            }

            // Önünde zaten boş satır varsa ikinci boş satır eklenmez.
            var block = insertAt > 0 && lines[insertAt - 1].Trim().Length == 0 ? new List<string> { title, marker } : new List<string> { string.Empty, title, marker };
            block.AddRange(body);
            block.Add(EndMarker);
            if (insertAt < lines.Count && lines[insertAt].Trim().Length > 0)
            {
                block.Add(string.Empty);
            }

            lines.InsertRange(insertAt, block);
            return true;
        }

        // Numarası verilen satırı değiştirir (item) ya da kaldırır (null); dosyanın geri kalanı, satır sonları ve BOM korunur.
        private static string EditRow(string stockFolder, int number, VerificationItem? item)
        {
            var path = PathFor(stockFolder);
            try
            {
                if (!File.Exists(path))
                {
                    return "missing";
                }

                var text = File.ReadAllText(path);
                var newline = text.Contains("\r\n") ? "\r\n" : "\n";
                var table = Table.Parse(text.Replace("\r\n", "\n").Split('\n').ToList());
                if (table == null)
                {
                    return "missing";
                }

                for (var i = table.Start + 2; i < table.End; i++)
                {
                    int n;
                    if (int.TryParse(table.CellOf(table.Lines[i], ColNumber), out n) && n == number)
                    {
                        if (item == null)
                        {
                            table.Lines.RemoveAt(i);
                        }
                        else
                        {
                            // Tanınmayan sütunların değerleri satırdan korunur.
                            var old = table.ItemOf(table.Lines[i], number);
                            foreach (var pair in old.Extra.Where(p => !item.Extra.ContainsKey(p.Key)))
                            {
                                item.Extra[pair.Key] = pair.Value;
                            }

                            table.Upgrade();
                            table.Lines[i] = table.RowOf(item);
                        }

                        return WriteAtomic(path, string.Join(newline, table.Lines), Journal.HasBom(path));
                    }
                }

                return "missing";
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

        private static string HeaderLine(IEnumerable<string> headers)
        {
            return "| " + string.Join(" | ", headers.Select(Escape)) + " |";
        }

        private static string SeparatorLine(int count)
        {
            return "|" + string.Join("|", Enumerable.Repeat("---", count)) + "|";
        }

        // Başlığı sütun anahtarına çevirir; tanınmazsa -1.
        private static int KeyOf(string header)
        {
            var h = new string((header ?? string.Empty).ToLowerInvariant().Where(c => !char.IsWhiteSpace(c) && c != '.' && c != '_' && c != '-').ToArray());
            switch (h)
            {
                case "#":
                case "no":
                case "sıra":
                    return ColNumber;
                case "başlık":
                case "baslik":
                case "doğrulamamaddesi":
                case "madde":
                case "metin":
                    return ColText;
                case "kaynak":
                case "doküman":
                    return ColSource;
                case "kısanot":
                case "kisanot":
                case "açıklama":
                case "not":
                    return ColDescription;
                case "gereksinim":
                case "tamgereksinim":
                    return ColRequirement;
                case "tür":
                case "tur":
                    return ColKind;
                case "bağlı":
                case "bagli":
                case "temel":
                    return ColParent;
                case "yöntem":
                case "yontem":
                case "doğrulamayöntemi":
                    return ColMethod;
                case "grup":
                    return ColGroup;
                case "altgrup":
                case "alt":
                    return ColSubGroup;
                case "altyapı":
                case "altyapi":
                    return ColFacility;
                case "tgno":
                case "tg":
                    return ColTg;
                case "tpno":
                case "tp":
                    return ColTp;
                case "trno":
                case "tr":
                    return ColTr;
                case "tpaçıklama":
                case "tpaciklama":
                    return ColTpDescription;
                case "tpnot":
                    return ColTpNote;
                case "traçıklama":
                case "traciklama":
                    return ColTrDescription;
                case "trnot":
                    return ColTrNote;
                case "trsonuç":
                case "trsonuc":
                case "testsonucu":
                    return ColTrResult;
                case "sonuçyeri":
                case "sonucyeri":
                    return ColResultPath;
                case "durum":
                    return ColStatus;
                default:
                    return -1;
            }
        }

        // Dosyadaki ilk tablo ve sütun eşlemesi.
        private sealed class Table
        {
            private Table(List<string> lines, int start, int end)
            {
                Lines = lines;
                Start = start;
                End = end;
            }

            public List<string> Lines { get; }

            public int Start { get; }

            public int End { get; private set; }

            // Sütun başlıkları ve her sütunun anahtarı (-1: tanınmayan).
            public List<string> HeaderCells { get; private set; } = new List<string>();

            public int[] Keys { get; private set; } = new int[0];

            // İlk tablo: "|" ile başlayan satır + ardından "|---" ayracı; tablo, "|" ile başlayan satırlar bitene kadar.
            public static Table? Parse(List<string> lines)
            {
                for (var i = 0; i + 1 < lines.Count; i++)
                {
                    if (IsRow(lines[i]) && IsSeparator(lines[i + 1]))
                    {
                        var end = i + 2;
                        while (end < lines.Count && IsRow(lines[end]))
                        {
                            end++;
                        }

                        var table = new Table(lines, i, end);
                        table.ReadHeader();
                        return table;
                    }
                }

                return null;
            }

            private void ReadHeader()
            {
                HeaderCells = SplitRow(Lines[Start]);
                Keys = HeaderCells.Select(KeyOf).ToArray();

                // Başlık tanınmıyorsa (ya da # / madde yoksa) eski 7 sütunlu sıra.
                var known = Keys.Where(k => k >= 0).Distinct().Count();
                if (known < 3 || !Keys.Contains(ColNumber) || !Keys.Contains(ColText))
                {
                    Keys = HeaderCells.Select((c, i) => i < LegacyOrder.Length ? LegacyOrder[i] : -1).ToArray();
                }

                // Aynı anahtar iki sütunda olursa ilki geçerli.
                var seen = new HashSet<int>();
                for (var i = 0; i < Keys.Length; i++)
                {
                    if (Keys[i] >= 0 && !seen.Add(Keys[i]))
                    {
                        Keys[i] = -1;
                    }
                }
            }

            public string CellOf(string line, int key)
            {
                var index = Array.IndexOf(Keys, key);
                var cells = SplitRow(line);
                return index >= 0 && index < cells.Count ? cells[index] : string.Empty;
            }

            public VerificationItem ItemOf(string line, int fallbackNumber)
            {
                var cells = SplitRow(line);
                var values = new string[KnownCount];
                var item = new VerificationItem();
                for (var i = 0; i < Keys.Length; i++)
                {
                    var value = i < cells.Count ? cells[i] : string.Empty;
                    if (Keys[i] >= 0)
                    {
                        values[Keys[i]] = value;
                    }
                    else if (HeaderCells[i].Length > 0)
                    {
                        item.Extra[HeaderCells[i]] = value;
                    }
                }

                int number;
                item.Number = int.TryParse(values[ColNumber], out number) ? number : fallbackNumber;
                item.Text = values[ColText] ?? string.Empty;
                item.Description = values[ColDescription] ?? string.Empty;
                item.Requirement = values[ColRequirement] ?? string.Empty;
                item.Source = values[ColSource] ?? string.Empty;
                item.Kind = values[ColKind] ?? string.Empty;
                item.Parent = values[ColParent] ?? string.Empty;
                item.Method = values[ColMethod] ?? string.Empty;
                item.Group = values[ColGroup] ?? string.Empty;
                item.SubGroup = values[ColSubGroup] ?? string.Empty;
                item.Facility = values[ColFacility] ?? string.Empty;
                item.TgNo = values[ColTg] ?? string.Empty;
                item.TpNo = values[ColTp] ?? string.Empty;
                item.TrNo = values[ColTr] ?? string.Empty;
                item.TpDescription = values[ColTpDescription] ?? string.Empty;
                item.TpNote = values[ColTpNote] ?? string.Empty;
                item.TrDescription = values[ColTrDescription] ?? string.Empty;
                item.TrNote = values[ColTrNote] ?? string.Empty;
                item.TrResult = values[ColTrResult] ?? string.Empty;
                item.ResultPath = values[ColResultPath] ?? string.Empty;
                item.Status = values[ColStatus] ?? string.Empty;
                return item;
            }

            public string RowOf(VerificationItem item)
            {
                var cells = new List<string>();
                for (var i = 0; i < Keys.Length; i++)
                {
                    string value;
                    switch (Keys[i])
                    {
                        case ColNumber:
                            value = item.Number.ToString(System.Globalization.CultureInfo.InvariantCulture);
                            break;
                        case ColText:
                            value = item.Text;
                            break;
                        case ColDescription:
                            value = item.Description;
                            break;
                        case ColRequirement:
                            value = item.Requirement;
                            break;
                        case ColSource:
                            value = item.Source;
                            break;
                        case ColKind:
                            value = item.Kind;
                            break;
                        case ColParent:
                            value = item.Parent;
                            break;
                        case ColMethod:
                            value = item.Method;
                            break;
                        case ColGroup:
                            value = item.Group;
                            break;
                        case ColSubGroup:
                            value = item.SubGroup;
                            break;
                        case ColFacility:
                            value = item.Facility;
                            break;
                        case ColTg:
                            value = item.TgNo;
                            break;
                        case ColTp:
                            value = item.TpNo;
                            break;
                        case ColTr:
                            value = item.TrNo;
                            break;
                        case ColTpDescription:
                            value = item.TpDescription;
                            break;
                        case ColTpNote:
                            value = item.TpNote;
                            break;
                        case ColTrDescription:
                            value = item.TrDescription;
                            break;
                        case ColTrNote:
                            value = item.TrNote;
                            break;
                        case ColTrResult:
                            value = item.TrResult;
                            break;
                        case ColResultPath:
                            value = item.ResultPath;
                            break;
                        case ColStatus:
                            value = item.Status;
                            break;
                        default:
                            item.Extra.TryGetValue(HeaderCells[i], out value);
                            break;
                    }

                    cells.Add(Escape(value ?? string.Empty));
                }

                return "| " + string.Join(" | ", cells) + " |";
            }

            /// <summary>Eksik tanınan sütun varsa tablo yeni başlıklarla (tanınmayan sütunlar sonda) yeniden yazılır.</summary>
            public void Upgrade()
            {
                if (Enumerable.Range(0, KnownCount).All(k => Keys.Contains(k)))
                {
                    return;
                }

                var items = new List<VerificationItem>();
                for (var i = Start + 2; i < End; i++)
                {
                    items.Add(ItemOf(Lines[i], items.Count + 1));
                }

                var extras = HeaderCells.Where((h, i) => Keys[i] < 0 && h.Length > 0).ToList();
                var headers = Headers.Concat(extras).ToList();
                var rows = new List<string> { HeaderLine(headers), SeparatorLine(headers.Count) };
                HeaderCells = headers;
                Keys = Enumerable.Range(0, KnownCount).Concat(Enumerable.Repeat(-1, extras.Count)).ToArray();
                rows.AddRange(items.Select(RowOf));

                Lines.RemoveRange(Start, End - Start);
                Lines.InsertRange(Start, rows);
                End = Start + rows.Count;
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

        // "| a | b \| c |" → ["a", "b | c"]; kaçışlı "\|" hücreyi bölmez.
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

        private static string Escape(string value)
        {
            return (value ?? string.Empty).Replace("\r", " ").Replace("\n", " ").Replace("|", "\\|").Trim();
        }

        internal static string WriteAtomic(string path, string content, bool bom)
        {
            var temp = path + ".tmp-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            DiskWatcher.MarkOwnWrite();
            File.WriteAllText(temp, content, new UTF8Encoding(bom));
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
    }

    /// <summary>
    /// Altyapı listesi (SATIMO, Yankısız oda, VNA …): tüm projelerde ortak, kökte <c>_TAKIP\Altyapilar.md</c>.
    /// Her altyapı bir madde satırı (<c>- SATIMO</c>); Obsidian'dan elle de eklenebilir.
    /// </summary>
    public static class FacilityList
    {
        /// <summary>Listenin kökteki yolu.</summary>
        public static string PathFor(string root)
        {
            return Path.Combine(root, "_TAKIP", "Altyapilar.md");
        }

        /// <summary>Altyapı adları (dosyadaki sırayla, tekrarsız); dosya yoksa boş.</summary>
        public static IList<string> Read(string root)
        {
            var names = new List<string>();
            var path = PathFor(root);
            if (!File.Exists(path))
            {
                return names;
            }

            try
            {
                foreach (var line in File.ReadAllLines(path))
                {
                    var name = NameOf(line);
                    if (name.Length > 0 && !names.Contains(name, StringComparer.InvariantCultureIgnoreCase))
                    {
                        names.Add(name);
                    }
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }

            return names;
        }

        /// <summary>
        /// Listede yoksa altyapıyı sona ekler; dosya yoksa <paramref name="newFileTemplate"/> ile oluşturur.
        /// Hata metni ya da boş döndürür.
        /// </summary>
        public static string Add(string root, string name, string newFileTemplate)
        {
            name = (name ?? string.Empty).Trim();
            if (name.Length == 0 || Read(root).Contains(name, StringComparer.InvariantCultureIgnoreCase))
            {
                return string.Empty;
            }

            var path = PathFor(root);
            try
            {
                var exists = File.Exists(path);
                var text = exists ? File.ReadAllText(path) : (newFileTemplate ?? string.Empty);
                var newline = text.Contains("\r\n") || !exists ? "\r\n" : "\n";
                text = text.TrimEnd('\r', '\n', ' ', '\t');
                text = text + newline + (text.Length == 0 || IsListLine(LastLine(text)) ? string.Empty : newline) + "- " + name + newline;
                Directory.CreateDirectory(Path.GetDirectoryName(path) ?? root);
                return VerificationList.WriteAtomic(path, text.TrimStart('\r', '\n'), exists && Journal.HasBom(path));
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

        private static string LastLine(string text)
        {
            var i = text.LastIndexOf('\n');
            return i < 0 ? text : text.Substring(i + 1);
        }

        private static bool IsListLine(string line)
        {
            var t = line.TrimStart();
            return t.StartsWith("- ", StringComparison.Ordinal) || t.StartsWith("* ", StringComparison.Ordinal);
        }

        // "- [[SATIMO]]" → "SATIMO"; "- VNA — 2. kat" → "VNA — 2. kat" (açıklama da addır).
        private static string NameOf(string line)
        {
            if (!IsListLine(line))
            {
                return string.Empty;
            }

            var t = line.TrimStart().Substring(2).Trim();
            if (t.StartsWith("[ ]", StringComparison.Ordinal) || t.StartsWith("[x]", StringComparison.OrdinalIgnoreCase))
            {
                t = t.Substring(3).Trim();
            }

            return t.Replace("[[", string.Empty).Replace("]]", string.Empty).Trim();
        }
    }
}
