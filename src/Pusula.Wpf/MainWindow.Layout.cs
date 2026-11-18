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
    /// <summary>Ana pencere: pencere yeri ve tablo sütunlarının sırası / genişliği (hatırlanır).</summary>
    public partial class MainWindow
    {
        /// <summary>Sütun genişliğinin alt sınırı (piksel).</summary>
        public const double MinColumnWidth = 40;

        /// <summary>Sütun genişliğinin üst sınırı (piksel).</summary>
        public const double MaxColumnWidth = 600;

        private readonly List<TableLayout> tables = new List<TableLayout>();

        // Pencere açılınca: tablolar kaydedilen düzene getirilir; kapanırken düzen yazılır.
        private void SetUpLayout()
        {
            tables.Add(new TableLayout("inventory", InventoryList));
            foreach (var table in tables)
            {
                table.Apply(viewModel.Settings);
                table.Moved += (s, e) => SaveLayout();
            }

            ApplyWindowBounds();
            Closing += (s, e) =>
            {
                StoreWindowBounds();
                SaveLayout();
            };
        }

        // Son yer ve boyut: ekranlardan birinin içinde kalıyorsa uygulanır (çıkarılmış ikinci ekran gibi durumlarda ortada açılır);
        // kayıt yoksa ekranı kaplar.
        private void ApplyWindowBounds()
        {
            var saved = viewModel.Settings.Window;
            if (saved == null || saved.Width < MinWidth || saved.Height < MinHeight)
            {
                // İlk açılışta (kayıtlı yer yoksa) ekranı kaplar; sonra son hâli (küçültülmüş boyut ya da tam ekran) hatırlanır.
                // Testlerde (App başlatılmadan) pencere ekran dışında kalsın diye uygulanmaz.
                if (App.Started)
                {
                    WindowState = WindowState.Maximized;
                }

                return;
            }

            var screen = new Rect(SystemParameters.VirtualScreenLeft, SystemParameters.VirtualScreenTop, SystemParameters.VirtualScreenWidth, SystemParameters.VirtualScreenHeight);
            var bounds = new Rect(saved.Left, saved.Top, saved.Width, saved.Height);
            if (!screen.IntersectsWith(bounds) || bounds.Left < screen.Left - 50 || bounds.Top < screen.Top - 10)
            {
                Width = Math.Min(saved.Width, SystemParameters.WorkArea.Width);
                Height = Math.Min(saved.Height, SystemParameters.WorkArea.Height);
                return;
            }

            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = saved.Left;
            Top = saved.Top;
            Width = saved.Width;
            Height = saved.Height;
            if (saved.Maximized)
            {
                WindowState = WindowState.Maximized;
            }
        }

        private void StoreWindowBounds()
        {
            var bounds = WindowState == WindowState.Normal ? new Rect(Left, Top, ActualWidth, ActualHeight) : RestoreBounds;
            if (bounds.IsEmpty || bounds.Width < MinWidth)
            {
                return;
            }

            viewModel.Settings.Window = new WindowBounds
            {
                Left = Math.Round(bounds.Left),
                Top = Math.Round(bounds.Top),
                Width = Math.Round(bounds.Width),
                Height = Math.Round(bounds.Height),
                Maximized = WindowState == WindowState.Maximized,
            };
        }

        /// <summary>Sekme ve sütun düzenini varsayılana döndürür (Ayarlar'daki düğme).</summary>
        internal void ResetLayout()
        {
            viewModel.Settings.TabOrder.Clear();
            viewModel.Settings.Columns.Clear();
            viewModel.Settings.Window = null;
            WindowState = WindowState.Normal;
            Width = 1380;
            Height = 860;
            Left = SystemParameters.WorkArea.Left + Math.Max(0, (SystemParameters.WorkArea.Width - Width) / 2);
            Top = SystemParameters.WorkArea.Top + Math.Max(0, (SystemParameters.WorkArea.Height - Height) / 2);
            foreach (var table in tables)
            {
                table.Reset();
            }

            viewModel.Settings.Save();
        }

        // Stok sekmeleri kaldırıldı (15.11.2026, yeniden tasarlanacak); kayıtlı sekme sırası (TabOrder) olduğu gibi kalır.
        private void SaveLayout()
        {
            foreach (var table in tables)
            {
                table.Store(viewModel.Settings);
            }

            viewModel.Settings.Save();
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

                // Anahtarlı kayıt anahtarla eşlenir; eski (yalnız sıra numaralı) kayıt sütun sayısı tutuyorsa sırayla, tutmuyorsa yok sayılır.
                var legacy = saved.Any(s => string.IsNullOrEmpty(s.Key));
                if (legacy && saved.Count != original.Count)
                {
                    return;
                }

                var resolved = saved
                    .Select(s => new { s.Width, Index = string.IsNullOrEmpty(s.Key) ? s.Index : original.FindIndex(c => KeyOf(c, original) == s.Key) })
                    .Where(s => s.Index >= 0 && s.Index < original.Count)
                    .ToList();

                applying = true;
                try
                {
                    var order = resolved.Select(s => s.Index).Distinct().ToList();
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

                    foreach (var item in resolved.Where(s => s.Width > 0))
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

                settings.Columns[key] = view.Columns.Select(c => new ColumnLayout { Index = original.IndexOf(c), Key = KeyOf(c, original), Width = double.IsNaN(c.Width) ? 0 : Math.Round(c.Width) }).ToList();
            }

            // Başlığın Tag'i (yoksa yazısı, o da yoksa sırası).
            private static string KeyOf(GridViewColumn column, List<GridViewColumn> all)
            {
                var header = column.Header as GridViewColumnHeader;
                var tag = header?.Tag as string;
                if (!string.IsNullOrEmpty(tag))
                {
                    return tag!;
                }

                var text = (header?.Content ?? column.Header) as string;
                return !string.IsNullOrEmpty(text) ? text! : "#" + all.IndexOf(column).ToString(System.Globalization.CultureInfo.InvariantCulture);
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
                // 0 genişlik "gizli" demektir (ör. tür düğümünde Tür sütunu); sınıra takılmaz.
                if (applying || !(sender is GridViewColumn column) || double.IsNaN(column.Width) || column.Width <= 0)
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
