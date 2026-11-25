using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using Pusula.Core.Inventory;
using Pusula.Core.Tracking;
using Pusula.Wpf.Properties;

namespace Pusula.Wpf.ViewModels
{
    /// <summary>Stok sayfasında bileşen / kullanıldığı yer çipi: numara · ad; tıklanınca o stok açılır.</summary>
    public sealed class StockLinkChip
    {
        /// <summary>Çipi kurar.</summary>
        public StockLinkChip(string number, string name, string path)
        {
            Number = number;
            Name = name;
            Path = path;
            Label = name.Length > 0 ? number + " · " + name : number;
        }

        /// <summary>Stok numarası.</summary>
        public string Number { get; }

        /// <summary>Stok adı (boş olabilir).</summary>
        public string Name { get; }

        /// <summary>Stok klasörü; ağaçta bulunamazsa boş.</summary>
        public string Path { get; }

        /// <summary>"AB-7000-0003 · DA400 Döner Anten".</summary>
        public string Label { get; }

        /// <summary>Stok ağaçta yok (silinmiş / taşınmış numara).</summary>
        public bool IsMissing
        {
            get { return Path.Length == 0; }
        }
    }

    /// <summary>
    /// Alt / üst stok (25.11.2026, kullanıcı kararı): bileşenler üst stokta girilir (<c>stok.json</c> → <c>components</c>),
    /// alt stokta "Kullanıldığı yerler" bütün stokların listelerinden hesaplanır. Döngü kurulamaz; klasörler ve ağaç değişmez.
    /// </summary>
    public sealed partial class MainViewModel
    {
        private bool isAddingComponent;

        /// <summary>Seçili stokun bileşenleri.</summary>
        public ObservableCollection<StockLinkChip> StockComponentChips { get; } = new ObservableCollection<StockLinkChip>();

        /// <summary>Seçili stokun kullanıldığı yerler (onu bileşen olarak listeleyen stoklar).</summary>
        public ObservableCollection<StockLinkChip> StockUsedInChips { get; } = new ObservableCollection<StockLinkChip>();

        /// <summary>"Kullanıldığı yerler" satırı görünür mü.</summary>
        public bool HasStockUsedIn
        {
            get { return StockUsedInChips.Count > 0; }
        }

        /// <summary>"+ Bileşen" seçenekleri: eklenebilecek stoklar (kendisi, bileşenleri ve üstleri hariç).</summary>
        public ObservableCollection<LevelOption> ComponentOptions { get; } = new ObservableCollection<LevelOption>();

        /// <summary>Bileşen seçme kutusu açık mı.</summary>
        public bool IsAddingComponent
        {
            get { return isAddingComponent; }
            set { SetProperty(ref isAddingComponent, value); }
        }

        /// <summary>Kutudan seçilen stok bileşen olarak eklenir, kutu kapanır.</summary>
        public LevelOption? ComponentChoice
        {
            get { return null; }
            set
            {
                if (value != null && AddComponent(value.Owner))
                {
                    IsAddingComponent = false;
                }

                OnPropertyChanged();
            }
        }

        /// <summary>Seçili stoka bileşen ekler (klasör yolu ya da stok numarası). Döngü kurulacaksa eklenmez.</summary>
        public bool AddComponent(string stockPathOrNumber)
        {
            var stock = selectedNode != null && selectedNode.Kind == FolderKind.Stock ? selectedNode : null;
            if (stock == null || string.IsNullOrWhiteSpace(stockPathOrNumber))
            {
                return false;
            }

            var number = stockPathOrNumber.IndexOf('\\') >= 0 ? Path.GetFileName(stockPathOrNumber.TrimEnd('\\')) : stockPathOrNumber.Trim();
            var map = ReadComponentMap();
            if (!StockComponents.CanAdd(stock.Title, number, map))
            {
                Status = string.Format(CultureInfo.CurrentCulture, Resources.Stock_ComponentCycle, number, stock.Title);
                return false;
            }

            if (!StockComponents.Set(stock.Path, number, true))
            {
                Status = Resources.StockInv_LinkFailed;
                return false;
            }

            Status = string.Format(CultureInfo.CurrentCulture, Resources.Stock_ComponentAdded, number, stock.Title);
            LoadStockLinks();
            return true;
        }

        /// <summary>Seçili stoktan bileşeni kaldırır (yalnız bağ; klasörlere dokunulmaz).</summary>
        public bool RemoveComponent(string number)
        {
            var stock = selectedNode != null && selectedNode.Kind == FolderKind.Stock ? selectedNode : null;
            if (stock == null || !StockComponents.Set(stock.Path, number, false))
            {
                Status = Resources.StockInv_LinkFailed;
                return false;
            }

            Status = string.Format(CultureInfo.CurrentCulture, Resources.Stock_ComponentRemoved, number, stock.Title);
            LoadStockLinks();
            return true;
        }

        // Stok sayfası açılınca (ve bağ değişince): bileşenler, kullanıldığı yerler, eklenebilecekler.
        private void LoadStockLinks()
        {
            StockComponentChips.Clear();
            StockUsedInChips.Clear();
            ComponentOptions.Clear();
            IsAddingComponent = false;
            var stock = selectedNode != null && selectedNode.Kind == FolderKind.Stock ? selectedNode : null;
            if (stock != null)
            {
                var nodes = AllStockNodes().ToList();
                var names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var paths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var map = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                foreach (var node in nodes)
                {
                    var info = JsonFile.TryRead<StockInfo>(TrackingFiles.StockFile(node.Path));
                    names[node.Title] = info?.Name ?? string.Empty;
                    paths[node.Title] = node.Path;
                    map[node.Title] = info?.Components ?? new List<string>();
                }

                Func<string, StockLinkChip> chip = n =>
                {
                    string name, path;
                    return new StockLinkChip(n, names.TryGetValue(n, out name) ? name : string.Empty, paths.TryGetValue(n, out path) ? path : string.Empty);
                };
                List<string> own;
                foreach (var number in map.TryGetValue(stock.Title, out own) ? own : new List<string>())
                {
                    StockComponentChips.Add(chip(number));
                }

                foreach (var parent in map.Where(p => p.Value.Contains(stock.Title, StringComparer.OrdinalIgnoreCase)).Select(p => p.Key).OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
                {
                    StockUsedInChips.Add(chip(parent));
                }

                foreach (var node in nodes.Where(n => StockComponents.CanAdd(stock.Title, n.Title, map)))
                {
                    ComponentOptions.Add(new LevelOption(node.Title, InventoryLevel.Stock, node.Path, names[node.Title]));
                }
            }

            OnPropertyChanged(nameof(HasStockUsedIn));
        }

        // Ağaçtaki bütün stokların bileşen listeleri (numara → bileşenler).
        private Dictionary<string, List<string>> ReadComponentMap()
        {
            var map = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var node in AllStockNodes())
            {
                map[node.Title] = JsonFile.TryRead<StockInfo>(TrackingFiles.StockFile(node.Path))?.Components ?? new List<string>();
            }

            return map;
        }
    }
}
