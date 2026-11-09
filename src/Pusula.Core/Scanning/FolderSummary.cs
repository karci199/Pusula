using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Pusula.Core.Templates;
using Pusula.Core.Tracking;

namespace Pusula.Core.Scanning
{
    /// <summary>Özet tablosunun bir satırı: bir alt klasör ve istatistiği.</summary>
    public sealed class SummaryRow
    {
        /// <summary>Satırı oluşturur.</summary>
        public SummaryRow(string name, string path, string template, FolderStats stats, bool isSpecial, bool isMissing, string status = "")
        {
            Name = name;
            Path = path;
            Template = template;
            Stats = stats;
            IsSpecial = isSpecial;
            IsMissing = isMissing;
            Status = status ?? string.Empty;
        }

        /// <summary>Klasör adı.</summary>
        public string Name { get; }

        /// <summary>Tam yol.</summary>
        public string Path { get; }

        /// <summary>Stok satırında şablon adı; diğerlerinde boş.</summary>
        public string Template { get; }

        /// <summary>İstatistik.</summary>
        public FolderStats Stats { get; }

        /// <summary><c>_</c> ile başlayan özel klasör (toplamlara girmez).</summary>
        public bool IsSpecial { get; }

        /// <summary>Şablonda var ama diskte yok.</summary>
        public bool IsMissing { get; }

        /// <summary>Stok satırında ana durum; diğerlerinde boş.</summary>
        public string Status { get; }
    }

    /// <summary>Bir proje ya da stok için özet: satırlar ve toplam.</summary>
    public sealed class FolderSummary
    {
        /// <summary>Özeti oluşturur.</summary>
        public FolderSummary(IReadOnlyList<SummaryRow> rows, FolderStats total, int stockCount, string template, FolderStats general, StockInfo? stock = null)
        {
            Rows = rows;
            Total = total;
            StockCount = stockCount;
            Template = template;
            General = general;
            Stock = stock;
        }

        /// <summary>Satırlar (projede stoklar, stokta klasörler).</summary>
        public IReadOnlyList<SummaryRow> Rows { get; }

        /// <summary>Özel klasörler hariç toplam.</summary>
        public FolderStats Total { get; }

        /// <summary>Projedeki stok sayısı (stok özetinde 0).</summary>
        public int StockCount { get; }

        /// <summary>Stok özetinde stokun şablonu; projede boş.</summary>
        public string Template { get; }

        /// <summary>Projede proje geneli klasörlerin toplamı; stokta boş.</summary>
        public FolderStats General { get; }

        /// <summary>Stok özetinde <c>stok.json</c> içeriği (durum dahil); projede <c>null</c>.</summary>
        public StockInfo? Stock { get; }

        /// <summary>
        /// Proje özeti: içindeki her stok için istatistik ve şablon; proje geneli klasörler ayrı toplanır.
        /// </summary>
        public static FolderSummary ForProject(string projectFolder, IEnumerable<string> projectFolders, CancellationToken cancellation)
        {
            var rows = new List<SummaryRow>();
            var total = FolderStats.Empty;
            var general = FolderStats.Empty;
            var generalNames = new HashSet<string>(projectFolders, StringComparer.OrdinalIgnoreCase);

            foreach (var child in SafeDirectories(projectFolder))
            {
                var name = Path.GetFileName(child);
                if (FolderStats.IsAppFolder(name))
                {
                    continue;
                }

                var stats = FolderStats.Compute(child, cancellation);
                if (generalNames.Contains(name))
                {
                    general = general.Add(stats);
                    total = total.Add(stats);
                    continue;
                }

                var special = name.StartsWith("_", StringComparison.Ordinal);
                if (!special)
                {
                    total = total.Add(stats);
                }

                if (FolderReader.KindOf(child) == FolderKind.Stock)
                {
                    var info = JsonFile.TryRead<StockInfo>(TrackingFiles.StockFile(child));
                    rows.Add(new SummaryRow(name, child, info?.Template ?? string.Empty, stats, special, false, info?.Status ?? string.Empty));
                }
            }

            total = total.Add(FilesDirectlyIn(projectFolder));
            return new FolderSummary(rows, total, rows.Count, string.Empty, general);
        }

        /// <summary>
        /// Stok özeti: üst düzey her klasör için istatistik. Stokun şablonunda olup diskte olmayan
        /// klasörler "eksik" satırı olarak eklenir.
        /// </summary>
        public static FolderSummary ForStock(string stockFolder, TemplateSet templates, CancellationToken cancellation)
        {
            var info = JsonFile.TryRead<StockInfo>(TrackingFiles.StockFile(stockFolder));
            var template = info?.Template ?? string.Empty;
            var rows = new List<SummaryRow>();
            var total = FilesDirectlyIn(stockFolder);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var child in SafeDirectories(stockFolder))
            {
                var name = Path.GetFileName(child);
                if (FolderStats.IsAppFolder(name))
                {
                    continue;
                }

                seen.Add(name);
                var stats = FolderStats.Compute(child, cancellation);
                var special = name.StartsWith("_", StringComparison.Ordinal);
                if (!special)
                {
                    total = total.Add(stats);
                }

                rows.Add(new SummaryRow(name, child, string.Empty, stats, special, false));
            }

            var preset = templates.Templates.FirstOrDefault(t => string.Equals(t.Name, template, StringComparison.OrdinalIgnoreCase));
            if (preset != null)
            {
                var keys = new HashSet<string>(preset.Modules, StringComparer.OrdinalIgnoreCase);
                foreach (var module in templates.Modules.Where(m => keys.Contains(m.Key)))
                {
                    if (!seen.Contains(module.Folder))
                    {
                        rows.Add(new SummaryRow(module.Folder, Path.Combine(stockFolder, module.Folder), string.Empty, FolderStats.Empty, false, true));
                    }
                }
            }

            rows = rows
                .OrderBy(r => r.Name.StartsWith("_", StringComparison.Ordinal))
                .ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
            return new FolderSummary(rows, total, 0, template, FolderStats.Empty, info);
        }

        private static IEnumerable<string> SafeDirectories(string folder)
        {
            try
            {
                return Directory.GetDirectories(folder).OrderBy(d => d, StringComparer.OrdinalIgnoreCase).ToList();
            }
            catch (IOException)
            {
                return Enumerable.Empty<string>();
            }
            catch (UnauthorizedAccessException)
            {
                return Enumerable.Empty<string>();
            }
        }

        private static FolderStats FilesDirectlyIn(string folder)
        {
            var result = FolderStats.Empty;
            try
            {
                foreach (var file in new DirectoryInfo(folder).GetFiles().Where(f => !FileRules.IsJunkFile(f.Name)))
                {
                    result = result.Add(new FolderStats(1, file.Length, file.FullName, file.LastWriteTime));
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }

            return result;
        }
    }
}
