using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Pusula.Core.Tracking;
using Pusula.Wpf.Properties;

namespace Pusula.Wpf.ViewModels
{
    /// <summary>
    /// Ana sayfadaki proje kartı: renk, kod, ad, stok sayısı ve son değişiklik (tarih · stok).
    /// Disk okuması arka planda <see cref="Read"/> ile yapılır; kart yalnız gösterir.
    /// </summary>
    public sealed class ProjectCard
    {
        /// <summary>Kartta en çok bu kadar stok çipi gösterilir; fazlası "+N".</summary>
        public const int ChipLimit = 2;

        private ProjectCard(TreeNodeViewModel node, string code, string name, string color, int stockCount, DateTime? lastChange, string lastStock, IList<string> stockNames)
        {
            StockChips = stockNames.Take(ChipLimit).ToList();
            MoreText = stockNames.Count > ChipLimit ? "+" + (stockNames.Count - ChipLimit).ToString(CultureInfo.CurrentCulture) : string.Empty;
            Node = node;
            Code = code;
            Name = name;
            Color = color;
            StockCount = stockCount;
            LastChange = lastChange;
            LastStock = lastStock;
        }

        /// <summary>Ağaçtaki proje düğümü (karta tıklayınca seçilir).</summary>
        public TreeNodeViewModel Node { get; }

        /// <summary>Proje kodu (klasör adının ilk <c>_</c>'e kadarki kısmı), örn. 7000.</summary>
        public string Code { get; }

        /// <summary>Proje adı (proje.json, yoksa klasör adının kalanı), örn. MILKON.</summary>
        public string Name { get; }

        /// <summary>Projenin rengi (<c>#RRGGBB</c>); sol listedekiyle aynı.</summary>
        public string Color { get; }

        /// <summary>Kartta gösterilen stok numaraları (ağaçtaki sırayla, en çok <see cref="ChipLimit"/>).</summary>
        public IList<string> StockChips { get; }

        /// <summary>Gösterilmeyen stoklar: "+2"; yoksa boş.</summary>
        public string MoreText { get; }

        /// <summary>"+N" çipi var mı.</summary>
        public bool HasMore
        {
            get { return MoreText.Length > 0; }
        }

        /// <summary>Hiç stok çipi var mı.</summary>
        public bool HasChips
        {
            get { return StockChips.Count > 0; }
        }

        /// <summary>Stok sayısı.</summary>
        public int StockCount { get; }

        /// <summary>"4 stok".</summary>
        public string StockText
        {
            get { return string.Format(CultureInfo.CurrentCulture, Resources.Card_Stocks, StockCount); }
        }

        /// <summary>Projede en son değişen stokun zamanı; stok yoksa <c>null</c>.</summary>
        public DateTime? LastChange { get; }

        /// <summary>En son değişen stokun numarası; yoksa boş.</summary>
        public string LastStock { get; }

        /// <summary>"14.11.2026 · AB-7000-0002"; stok yoksa boş.</summary>
        public string LastText
        {
            get
            {
                if (!LastChange.HasValue)
                {
                    return string.Empty;
                }

                var date = LastChange.Value.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
                return LastStock.Length == 0 ? date : date + " · " + LastStock;
            }
        }

        /// <summary>Son değişiklik yazısı var mı.</summary>
        public bool HasLast
        {
            get { return LastChange.HasValue; }
        }

        /// <summary>
        /// Kartı diskten kurar (arka planda çağrılır): proje.json'dan ad ve renk, her stok klasöründe
        /// iki düzeye kadar klasör / dosya değişme zamanlarının en yenisi.
        /// </summary>
        public static ProjectCard Read(TreeNodeViewModel node, IList<TreeNodeViewModel> stocks)
        {
            var folderName = Path.GetFileName(node.Path.TrimEnd('\\'));
            var cut = folderName.IndexOf('_');
            var code = cut > 0 ? folderName.Substring(0, cut) : folderName;
            var info = JsonFile.TryRead<ProjectInfo>(TrackingFiles.ProjectFile(node.Path));
            var name = info != null && !string.IsNullOrWhiteSpace(info.Name) ? info.Name.Trim() : (cut > 0 ? folderName.Substring(cut + 1) : string.Empty);
            var color = ProjectColors.For(folderName, info?.Color);

            DateTime? last = null;
            var lastStock = string.Empty;
            foreach (var stock in stocks)
            {
                var time = NewestWrite(stock.Path, 2);
                if (time.HasValue && (!last.HasValue || time.Value > last.Value))
                {
                    last = time;
                    lastStock = stock.Title;
                }
            }

            return new ProjectCard(node, code, name, color, stocks.Count, last, lastStock, stocks.Select(s => s.Title).ToList());
        }

        // Klasörün, içindeki dosyaların ve depth düzeye kadar alt klasörlerin en yeni değişme zamanı.
        private static DateTime? NewestWrite(string folder, int depth)
        {
            try
            {
                var newest = Directory.GetLastWriteTime(folder);
                foreach (var file in Directory.EnumerateFiles(folder))
                {
                    var t = File.GetLastWriteTime(file);
                    if (t > newest)
                    {
                        newest = t;
                    }
                }

                if (depth > 0)
                {
                    foreach (var sub in Directory.EnumerateDirectories(folder))
                    {
                        var t = NewestWrite(sub, depth - 1);
                        if (t.HasValue && t.Value > newest)
                        {
                            newest = t.Value;
                        }
                    }
                }

                return newest;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                return null;
            }
        }
    }
}
