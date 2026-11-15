using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Pusula.Wpf
{
    /// <summary>
    /// Ana sayfa · Kayıtlar tabloları: Ad / Tanım sütunu pencereyle genişler (tablo hep alanı doldurur),
    /// sütun başlığına sağ tıklayınca sütunlar açılıp kapanır (seçim ayarlarda saklanır).
    /// </summary>
    public partial class MainWindow
    {
        // Varsayılan kapalı sütunlar: araçlarda yalnız Numara, Ad, Tür, Alt tür; projelerde Kod, Ad, Stok, Boyut, Son değişiklik.
        private static readonly HashSet<string> HiddenByDefault = new HashSet<string>(StringComparer.Ordinal)
        {
            "tools:AttachedTo", "tools:Location", "tools:Status",
            "projects:Tools", "projects:OpenTasks", "projects:Files",
            "stocks:Template", "stocks:Status", "stocks:Tools", "stocks:OpenTasks",
        };

        private readonly Dictionary<ListView, RecordTable> recordTables = new Dictionary<ListView, RecordTable>();

        private sealed class RecordTable
        {
            public string Key = string.Empty;
            public GridView View = null!;
            public List<GridViewColumn> All = new List<GridViewColumn>();
            public GridViewColumn? Fill;
            public Dictionary<GridViewColumn, double> MinWidth = new Dictionary<GridViewColumn, double>();
        }

        // Pencere açılınca bir kez: üç tablo kurulur.
        private void SetUpRecordTables()
        {
            SetUpRecordTable(ProjectRecordsList, "projects", "Name");
            SetUpRecordTable(StockRecordsList, "stocks", "Name");
            SetUpRecordTable(ToolRecordsList, "tools", "Name");
        }

        private void SetUpRecordTable(ListView list, string key, string fillTag)
        {
            var table = new RecordTable { Key = key, View = (GridView)list.View };
            table.All = table.View.Columns.ToList();
            foreach (var column in table.All)
            {
                table.MinWidth[column] = column.Width;
                if ((column.Header as GridViewColumnHeader)?.Tag as string == fillTag)
                {
                    table.Fill = column;
                }

                if (column.Header is GridViewColumnHeader header)
                {
                    header.ContextMenu = BuildColumnMenu(list, table);
                    header.ToolTip = Properties.Resources.Records_ColumnsHint;
                }
            }

            recordTables[list] = table;
            ApplyColumnVisibility(list);
            list.SizeChanged += (s, e) => FitFillColumn(list);
        }

        // Başlığın sağ tık menüsü: her sütun için işaretli öğe (Numara hep görünür).
        private ContextMenu BuildColumnMenu(ListView list, RecordTable table)
        {
            var menu = new ContextMenu();
            menu.Opened += (s, e) =>
            {
                menu.Items.Clear();
                foreach (var column in table.All)
                {
                    var header = column.Header as GridViewColumnHeader;
                    var tag = header?.Tag as string;
                    if (string.IsNullOrEmpty(tag) || tag == "Number" || tag == "Code")
                    {
                        continue;
                    }

                    var item = new MenuItem { Header = header!.Content, IsCheckable = true, IsChecked = IsColumnVisible(table.Key, tag!), StaysOpenOnClick = true };
                    item.Click += (s2, e2) =>
                    {
                        SetColumnVisible(table.Key, tag!, item.IsChecked);
                        ApplyColumnVisibility(list);
                    };
                    menu.Items.Add(item);
                }
            };
            return menu;
        }

        private bool IsColumnVisible(string table, string tag)
        {
            var id = table + ":" + tag;
            bool visible;
            if (viewModel.Settings.RecordColumns != null && viewModel.Settings.RecordColumns.TryGetValue(id, out visible))
            {
                return visible;
            }

            return !HiddenByDefault.Contains(id);
        }

        private void SetColumnVisible(string table, string tag, bool visible)
        {
            var map = viewModel.Settings.RecordColumns ?? new Dictionary<string, bool>();
            map[table + ":" + tag] = visible;
            viewModel.Settings.RecordColumns = map;
            viewModel.Settings.Save();
        }

        // Görünür sütunlar sırayla yeniden dizilir; sonra Ad sütunu kalan genişliği alır.
        private void ApplyColumnVisibility(ListView list)
        {
            var table = recordTables[list];
            table.View.Columns.Clear();
            foreach (var column in table.All)
            {
                var tag = (column.Header as GridViewColumnHeader)?.Tag as string;
                if (string.IsNullOrEmpty(tag) || IsColumnVisible(table.Key, tag!))
                {
                    table.View.Columns.Add(column);
                }
            }

            FitFillColumn(list);
        }

        // Tablo hep alanı doldurur: başlıklı sütunlar tasarım genişlikleri oranında büyür (Ad / Tanım iki pay alır);
        // alan darsa tasarım genişliğinin altına inilmez, yatay kaydırma çıkar.
        private void FitFillColumn(ListView list)
        {
            RecordTable? table;
            if (!recordTables.TryGetValue(list, out table) || list.ActualWidth <= 0)
            {
                return;
            }

            var columns = table.View.Columns.ToList();
            var flexible = columns.Where(c => !string.IsNullOrEmpty((c.Header as GridViewColumnHeader)?.Tag as string)).ToList();
            var fixedWidth = columns.Except(flexible).Sum(c => table.MinWidth[c]);
            Func<GridViewColumn, double> weight = c => table.MinWidth[c] * (ReferenceEquals(c, table.Fill) ? 2 : 1);
            var design = flexible.Sum(c => table.MinWidth[c]);
            var available = list.ActualWidth - fixedWidth - SystemParameters.VerticalScrollBarWidth - 14;
            var extra = Math.Max(0, available - design);
            var weights = flexible.Sum(weight);
            foreach (var column in flexible)
            {
                column.Width = Math.Floor(table.MinWidth[column] + (weights > 0 ? extra * weight(column) / weights : 0));
            }
        }
    }
}
