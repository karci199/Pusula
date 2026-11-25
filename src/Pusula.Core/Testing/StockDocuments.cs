using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Pusula.Core.Inventory;
using Pusula.Core.Notes;
using Pusula.Core.Templates;

namespace Pusula.Core.Testing
{
    /// <summary>Stoktaki bir dokümanın dosya adından okunan durumu.</summary>
    public enum DocumentState
    {
        /// <summary>Doküman klasöründe bu koda uyan dosya yok (başka yerde olabilir).</summary>
        NotHere,

        /// <summary>En son revizyon prototip (P1–P9) ve taslak.</summary>
        PrototypeDraft,

        /// <summary>En son revizyon prototip ve aktarılmış.</summary>
        PrototypeReleased,

        /// <summary>En son revizyon harfli (AA …) ve taslak; daha önce harfli revizyon aktarılmamış.</summary>
        Draft,

        /// <summary>En son revizyon harfli ve aktarılmış.</summary>
        Released,

        /// <summary>Harfli bir revizyon aktarılmış, daha yeni bir revizyon taslakta (değişiklik bekliyor).</summary>
        ChangePending,
    }

    /// <summary>Bir dokümanın stoktaki durumu.</summary>
    public sealed class StockDocument
    {
        /// <summary>Tanım (kod, ad).</summary>
        public DocumentDefinition Definition { get; internal set; } = new DocumentDefinition();

        /// <summary>Durum.</summary>
        public DocumentState State { get; internal set; }

        /// <summary>En son revizyon ("BA", "P2"); dosya yoksa boş.</summary>
        public string Revision { get; internal set; } = string.Empty;

        /// <summary>Aktarılmış en son revizyon; yoksa boş.</summary>
        public string ReleasedRevision { get; internal set; } = string.Empty;

        /// <summary>En son revizyonun dosyası ya da klasörü; yoksa boş.</summary>
        public string Path { get; internal set; } = string.Empty;

        /// <summary>Harfli (resmî) bir revizyon aktarılmış mı (261 ile doğrulanan madde sarıdan çıkar).</summary>
        public bool IsOfficial { get; internal set; }
    }

    /// <summary>
    /// Stok dokümanları (25.11.2026, kullanıcı kararı). Dosyalar <c>01_Ortak\Dokumanlar</c> altında (eski stokta
    /// <c>01_Dokumanlar</c>), ad düzeni <c>&lt;stok&gt;_&lt;kod&gt;_&lt;rev&gt;[_TASLAK]</c>: revizyon P1–P9 (prototip) ya da iki harf
    /// (AA, AB … BA); sıra P1 &lt; … &lt; P9 &lt; AA &lt; AB &lt; … &lt; BA. <c>_TASLAK</c> eki hazırlanıyor, eksiz ad aktarıldı demektir.
    /// Çok dosyalı doküman (kaynak kod) aynı adda klasör olabilir. Durum yalnız okunur; dosyaya ya da zaman çizelgesine yazılmaz.
    /// </summary>
    public static class StockDocuments
    {
        /// <summary>Taslak eki.</summary>
        public const string DraftSuffix = "_TASLAK";

        /// <summary>Dokümanların durduğu klasör (<c>01_Ortak\Dokumanlar</c>; henüz açılmamış olabilir). Eski stokta <c>01_Dokumanlar</c>.</summary>
        public static string FolderOf(string stockFolder)
        {
            var common = RecordContents.FindFolder(stockFolder, StockCover.CommonLabel);
            if (common.Length > 0)
            {
                var documents = RecordContents.FindFolder(common, TemplateSet.DocumentsFolder);
                return documents.Length > 0 ? documents : System.IO.Path.Combine(common, TemplateSet.DocumentsFolder);
            }

            var legacy = RecordContents.FindFolder(stockFolder, TemplateSet.DocumentsFolder);
            return legacy.Length > 0 ? legacy : System.IO.Path.Combine(stockFolder, "01_Ortak", TemplateSet.DocumentsFolder);
        }

        /// <summary>Tanım bu stokta aranır mı (kodu dolu, önek uyuyor).</summary>
        public static bool AppliesTo(DocumentDefinition definition, string stockNo)
        {
            return definition.Code.Trim().Length > 0
                && (definition.StockPrefix.Trim().Length == 0 || stockNo.StartsWith(definition.StockPrefix.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>Stokta aranan dokümanların durumları (tanım sırasıyla).</summary>
        public static List<StockDocument> Read(string stockFolder, string stockNo, IEnumerable<DocumentDefinition> definitions)
        {
            var wanted = definitions.Where(d => AppliesTo(d, stockNo)).ToList();
            var names = new List<string>();
            var folder = FolderOf(stockFolder);
            try
            {
                if (wanted.Count > 0 && Directory.Exists(folder))
                {
                    names.AddRange(Directory.EnumerateFileSystemEntries(folder));
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }

            return wanted.Select(d => StateOf(d, stockNo, names)).ToList();
        }

        /// <summary>Verilen dosya / klasör yollarından bir dokümanın durumu.</summary>
        public static StockDocument StateOf(DocumentDefinition definition, string stockNo, IEnumerable<string> paths)
        {
            var found = new List<Tuple<int, bool, string, string>>();
            foreach (var path in paths ?? Enumerable.Empty<string>())
            {
                string revision;
                bool draft;
                if (TryParse(System.IO.Path.GetFileName(path), stockNo, definition.Code.Trim(), out revision, out draft))
                {
                    found.Add(Tuple.Create(RankOf(revision), draft, revision, path));
                }
            }

            var result = new StockDocument { Definition = definition };
            if (found.Count == 0)
            {
                result.State = DocumentState.NotHere;
                return result;
            }

            // Aynı revizyonun hem taslağı hem aktarılmışı varsa aktarılmış sayılır.
            var latest = found.OrderByDescending(f => f.Item1).ThenBy(f => f.Item2).First();
            var released = found.Where(f => !f.Item2).OrderByDescending(f => f.Item1).FirstOrDefault();
            var officialReleased = found.Where(f => !f.Item2 && !IsPrototype(f.Item3)).OrderByDescending(f => f.Item1).FirstOrDefault();
            result.Revision = latest.Item3;
            result.Path = latest.Item4;
            result.ReleasedRevision = released?.Item3 ?? string.Empty;
            result.IsOfficial = officialReleased != null;
            if (!latest.Item2)
            {
                result.State = IsPrototype(latest.Item3) ? DocumentState.PrototypeReleased : DocumentState.Released;
            }
            else if (officialReleased != null)
            {
                result.ReleasedRevision = officialReleased.Item3;
                result.State = DocumentState.ChangePending;
            }
            else
            {
                result.State = IsPrototype(latest.Item3) ? DocumentState.PrototypeDraft : DocumentState.Draft;
            }

            return result;
        }

        /// <summary>
        /// Ad <c>&lt;stok&gt;_&lt;kod&gt;_&lt;rev&gt;[_TASLAK][.uzantı]</c> düzenine uyuyorsa revizyonu ve taslak olup olmadığını verir
        /// (büyük / küçük harf duyarsız).
        /// </summary>
        public static bool TryParse(string name, string stockNo, string code, out string revision, out bool draft)
        {
            revision = string.Empty;
            draft = false;
            if (string.IsNullOrEmpty(name) || stockNo.Length == 0 || code.Length == 0)
            {
                return false;
            }

            var pattern = "^" + Regex.Escape(stockNo) + "_" + Regex.Escape(code) + "_(?<rev>P[1-9]|[A-Z]{2})(?<draft>" + DraftSuffix + ")?(\\.[^.]+)?$";
            var match = Regex.Match(name, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (!match.Success)
            {
                return false;
            }

            revision = match.Groups["rev"].Value.ToUpperInvariant();
            draft = match.Groups["draft"].Success;
            return true;
        }

        /// <summary>Revizyon sırası: P1–P9 → 1–9, AA → 100, AB → 101 … (bilinmeyen 0).</summary>
        public static int RankOf(string revision)
        {
            var rev = (revision ?? string.Empty).ToUpperInvariant();
            if (rev.Length == 2 && rev[0] == 'P' && rev[1] >= '1' && rev[1] <= '9')
            {
                return rev[1] - '0';
            }

            if (rev.Length == 2 && rev[0] >= 'A' && rev[0] <= 'Z' && rev[1] >= 'A' && rev[1] <= 'Z')
            {
                return 100 + ((rev[0] - 'A') * 26) + (rev[1] - 'A');
            }

            return 0;
        }

        /// <summary>Prototip revizyonu mu (P1–P9).</summary>
        public static bool IsPrototype(string revision)
        {
            var rank = RankOf(revision);
            return rank >= 1 && rank <= 9;
        }
    }
}
