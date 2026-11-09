using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Pusula.Wpf
{
    /// <summary>Ana pencere: stok sekmelerinin sırası (sürükle-bırak) ve tablo sütunlarının sırası / genişliği (hatırlanır).</summary>
    public partial class MainWindow
    {
        /// <summary>Sütun genişliğinin alt sınırı (piksel).</summary>
        public const double MinColumnWidth = 40;

        /// <summary>Sütun genişliğinin üst sınırı (piksel).</summary>
        public const double MaxColumnWidth = 600;

        private const string GeneralTabTag = "general";

        private readonly List<TableLayout> tables = new List<TableLayout>();
        private Point dragStart;
        private TabItem? dragTab;

        // Pencere açılınca: tablolar kaydedilen düzene getirilir, sekme sürükleme bağlanır; kapanırken düzen yazılır.
        private void SetUpLayout()
        {
            tables.Add(new TableLayout("stocks", ProjectStockList));
            tables.Add(new TableLayout("inventory", InventoryList));
            tables.Add(new TableLayout("runs", DevRunsList));
            tables.Add(new TableLayout("qualification", QualRunsList));
            foreach (var table in tables)
            {
                table.Apply(viewModel.Settings);
                table.Moved += (s, e) => SaveLayout();
            }

            StockTabs.AllowDrop = true;
            StockTabs.PreviewMouseLeftButtonDown += StockTabs_PreviewMouseLeftButtonDown;
            StockTabs.PreviewMouseMove += StockTabs_PreviewMouseMove;
            StockTabs.Drop += StockTabs_Drop;
            ApplyTabOrder();
            Closing += (s, e) => SaveLayout();
        }

        /// <summary>Sekme ve sütun düzenini varsayılana döndürür (Ayarlar'daki düğme).</summary>
        internal void ResetLayout()
        {
            viewModel.Settings.TabOrder.Clear();
            viewModel.Settings.Columns.Clear();
            foreach (var table in tables)
            {
                table.Reset();
            }

            ApplyTabOrder();
            viewModel.Settings.Save();
        }

        private void SaveLayout()
        {
            viewModel.Settings.TabOrder = StockTabs.Items.OfType<TabItem>().Select(t => t.Tag as string ?? string.Empty).Where(t => t.Length > 0 && t != GeneralTabTag).ToList();
            foreach (var table in tables)
            {
                table.Store(viewModel.Settings);
            }

            viewModel.Settings.Save();
        }

        // Kayıtlı sıra uygulanır: Genel hep ilk; kayıtta olmayan sekmeler (yeni tür) yerlerini korur, sona doğru kalır.
        private void ApplyTabOrder()
        {
            var order = viewModel.Settings.TabOrder;
            var tabs = StockTabs.Items.OfType<TabItem>().ToList();
            var selected = StockTabs.SelectedItem;
            var sorted = tabs
                .Select((tab, index) => new { tab, index, tag = tab.Tag as string ?? string.Empty })
                .OrderBy(x => x.tag == GeneralTabTag ? -1 : 0)
                .ThenBy(x => order.Count == 0 ? DefaultRank(x.tag) * 100 + x.index : (order.IndexOf(x.tag) >= 0 ? order.IndexOf(x.tag) : order.Count + x.index))
                .Select(x => x.tab)
                .ToList();
            if (sorted.SequenceEqual(tabs))
            {
                return;
            }

            rebuildingTabs = true;
            try
            {
                StockTabs.Items.Clear();
                foreach (var tab in sorted)
                {
                    StockTabs.Items.Add(tab);
                }

                StockTabs.SelectedItem = selected ?? sorted.FirstOrDefault();
            }
            finally
            {
                rebuildingTabs = false;
            }
        }

        // Varsayılan sıra: Genel · Testler · Kalifikasyon · envanter türleri · Klasörler.
        private static int DefaultRank(string tag)
        {
            switch (tag)
            {
                case GeneralTabTag:
                    return 0;
                case "tests":
                    return 1;
                case "qualification":
                    return 2;
                case "folders":
                    return 9;
                default:
                    return tag.StartsWith(InventoryTabTag, StringComparison.Ordinal) ? 3 : 8;
            }
        }

        private static TabItem? TabUnder(DependencyObject? source)
        {
            while (source != null && !(source is TabItem))
            {
                source = source is Visual || source is System.Windows.Media.Media3D.Visual3D ? VisualTreeHelper.GetParent(source) : LogicalTreeHelper.GetParent(source);
            }

            return source as TabItem;
        }

        private void StockTabs_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var tab = TabUnder(e.OriginalSource as DependencyObject);
            // Yalnız sekme başlığından sürüklenir (içerikten değil); Genel sabit.
            dragTab = tab != null && tab.Parent == StockTabs && tab.Tag as string != GeneralTabTag && IsOnHeader(tab, e) ? tab : null;
            dragStart = e.GetPosition(StockTabs);
        }

        private static bool IsOnHeader(TabItem tab, MouseEventArgs e)
        {
            var position = e.GetPosition(tab);
            return position.Y >= 0 && position.Y <= tab.ActualHeight && position.X >= 0 && position.X <= tab.ActualWidth;
        }

        private void StockTabs_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (dragTab == null || e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            var now = e.GetPosition(StockTabs);
            if (Math.Abs(now.X - dragStart.X) < SystemParameters.MinimumHorizontalDragDistance)
            {
                return;
            }

            var tab = dragTab;
            dragTab = null;
            DragDrop.DoDragDrop(tab, tab, DragDropEffects.Move);
        }

        private void StockTabs_Drop(object sender, DragEventArgs e)
        {
            var source = e.Data.GetData(typeof(TabItem)) as TabItem;
            var target = TabUnder(e.OriginalSource as DependencyObject);
            MoveTab(source, target);
        }

        /// <summary>Sekmeyi hedef sekmenin yerine taşır ve sırayı kaydeder (Genel'in önüne geçemez).</summary>
        internal void MoveTab(TabItem? source, TabItem? target)
        {
            if (source == null || target == null || ReferenceEquals(source, target) || source.Tag as string == GeneralTabTag)
            {
                return;
            }

            var index = Math.Max(1, StockTabs.Items.IndexOf(target));
            rebuildingTabs = true;
            try
            {
                StockTabs.Items.Remove(source);
                StockTabs.Items.Insert(Math.Min(index, StockTabs.Items.Count), source);
                StockTabs.SelectedItem = source;
            }
            finally
            {
                rebuildingTabs = false;
            }

            SaveLayout();
        }

        /// <summary>Bir tablonun sütunları: tasarımdaki sıra ve genişlik saklanır, kayıtlı düzen uygulanır, genişlik sınırlanır.</summary>
        private sealed class TableLayout
        {
            private readonly string key;
            private readonly GridView view;
            private readonly List<GridViewColumn> original;
            private readonly List<double> defaults;
            private bool applying;

            public TableLayout(string key, ListView list)
            {
                this.key = key;
                view = (GridView)list.View;
                original = view.Columns.ToList();
                defaults = original.Select(c => c.Width).ToList();
                view.Columns.CollectionChanged += Columns_CollectionChanged;
                var width = DependencyPropertyDescriptor.FromProperty(GridViewColumn.WidthProperty, typeof(GridViewColumn));
                foreach (var column in original)
                {
                    width.AddValueChanged(column, Column_WidthChanged);
                }
            }

            /// <summary>Sütun sürüklenip yeri değişti.</summary>
            public event EventHandler? Moved;

            public void Apply(AppSettings settings)
            {
                List<ColumnLayout>? saved;
                if (!settings.Columns.TryGetValue(key, out saved) || saved == null || saved.Count == 0)
                {
                    return;
                }

                applying = true;
                try
                {
                    var order = saved.Where(s => s.Index >= 0 && s.Index < original.Count).Select(s => s.Index).Distinct().ToList();
                    order.AddRange(Enumerable.Range(0, original.Count).Where(i => !order.Contains(i)));
                    for (var target = 0; target < order.Count; target++)
                    {
                        var column = original[order[target]];
                        var current = view.Columns.IndexOf(column);
                        if (current != target)
                        {
                            view.Columns.Move(current, target);
                        }
                    }

                    foreach (var item in saved.Where(s => s.Index >= 0 && s.Index < original.Count && s.Width > 0))
                    {
                        original[item.Index].Width = Clamp(item.Width);
                    }
                }
                finally
                {
                    applying = false;
                }
            }

            public void Store(AppSettings settings)
            {
                var changed = view.Columns.Select((c, i) => original.IndexOf(c) != i || !Same(c.Width, defaults[original.IndexOf(c)])).Any(x => x);
                if (!changed)
                {
                    settings.Columns.Remove(key);
                    return;
                }

                settings.Columns[key] = view.Columns.Select(c => new ColumnLayout { Index = original.IndexOf(c), Width = double.IsNaN(c.Width) ? 0 : Math.Round(c.Width) }).ToList();
            }

            public void Reset()
            {
                applying = true;
                try
                {
                    for (var target = 0; target < original.Count; target++)
                    {
                        var current = view.Columns.IndexOf(original[target]);
                        if (current != target)
                        {
                            view.Columns.Move(current, target);
                        }

                        original[target].Width = defaults[target];
                    }
                }
                finally
                {
                    applying = false;
                }
            }

            private static bool Same(double a, double b)
            {
                return (double.IsNaN(a) && double.IsNaN(b)) || Math.Abs(a - b) < 0.5;
            }

            private static double Clamp(double width)
            {
                return Math.Max(MinColumnWidth, Math.Min(MaxColumnWidth, width));
            }

            private void Column_WidthChanged(object? sender, EventArgs e)
            {
                if (applying || !(sender is GridViewColumn column) || double.IsNaN(column.Width))
                {
                    return;
                }

                var clamped = Clamp(column.Width);
                if (!Same(clamped, column.Width))
                {
                    applying = true;
                    column.Width = clamped;
                    applying = false;
                }
            }

            private void Columns_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
            {
                if (!applying && e.Action == NotifyCollectionChangedAction.Move)
                {
                    Moved?.Invoke(this, EventArgs.Empty);
                }
            }
        }
    }
}
