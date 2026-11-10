using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
using Pusula.Core.Types;
using Pusula.Wpf.ViewModels;

namespace Pusula.Wpf
{
    /// <summary>
    /// Zaman çizgisi: olaylar (<see cref="ITimelinePoint"/>) yatay bir tarih ekseninde tarihlerine göre dizilir; aşama aşama
    /// ilerleyen bir çubuk değildir. Eksenin başı ilk olay, sonu bugündür; ay başları işaretlenir. Ad ve tarih etiketleri
    /// sırayla eksenin üstüne ve altına yazılır; çok yakın olaylarda etiket atlanır, noktanın ipucunda görünür.
    /// Bağlanan listenin varsayılan görünümü okunur (süzgeç uygulanmışsa yalnızca süzülen olaylar çizilir).
    /// </summary>
    public sealed class TimeAxis : Canvas
    {
        /// <summary>Olaylar (bir liste; varsayılan görünümü kullanılır).</summary>
        public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register(
            nameof(Items), typeof(IEnumerable), typeof(TimeAxis), new PropertyMetadata(null, (d, e) => ((TimeAxis)d).Attach(e.NewValue as IEnumerable)));

        private const double AxisY = 64;
        private const double Side = 12;
        private const double LabelGap = 96;
        private INotifyCollectionChanged? watched;

        /// <summary>Kontrolü kurar.</summary>
        public TimeAxis()
        {
            Height = 128;
            ClipToBounds = true;
            SizeChanged += (s, e) => Rebuild();
        }

        /// <summary>Olaylar.</summary>
        public IEnumerable? Items
        {
            get { return (IEnumerable?)GetValue(ItemsProperty); }
            set { SetValue(ItemsProperty, value); }
        }

        /// <summary>Çizilen olay sayısı (testler için).</summary>
        public int PointCount { get; private set; }

        /// <summary>Yazılan etiket sayısı (testler için; yakın olaylarda atlananlar sayılmaz).</summary>
        public int LabelCount { get; private set; }

        private void Attach(IEnumerable? items)
        {
            if (watched != null)
            {
                watched.CollectionChanged -= Items_CollectionChanged;
            }

            watched = items == null ? null : CollectionViewSource.GetDefaultView(items) as INotifyCollectionChanged ?? items as INotifyCollectionChanged;
            if (watched != null)
            {
                watched.CollectionChanged += Items_CollectionChanged;
            }

            Rebuild();
        }

        private void Items_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // Liste bir seferde temizlenip yeniden doldurulur; çizimi bir kez, iş bitince yap.
            Dispatcher.BeginInvoke(new Action(Rebuild), System.Windows.Threading.DispatcherPriority.Background);
        }

        private IEnumerable<ITimelinePoint> Points()
        {
            var items = Items;
            if (items == null)
            {
                return new ITimelinePoint[0];
            }

            IEnumerable source = CollectionViewSource.GetDefaultView(items) ?? items;
            return source.OfType<ITimelinePoint>().Where(p => p.SortDate > DateTime.MinValue).OrderBy(p => p.SortDate).ToList();
        }

        /// <summary>Ekseni yeniden çizer.</summary>
        public void Rebuild()
        {
            Children.Clear();
            PointCount = 0;
            LabelCount = 0;
            var width = ActualWidth;
            var points = Points().ToList();
            if (width < 80)
            {
                return;
            }

            if (points.Count == 0)
            {
                var empty = Caption(Properties.Resources.TimeAxis_Empty, "TextTertiaryBrush", 12);
                SetLeft(empty, 0);
                SetTop(empty, AxisY - 10);
                Children.Add(empty);
                return;
            }

            var start = points[0].SortDate.Date;
            var end = DateTime.Today > points[points.Count - 1].SortDate.Date ? DateTime.Today : points[points.Count - 1].SortDate.Date;
            if ((end - start).TotalDays < 7)
            {
                start = end.AddDays(-7);
            }

            var left = Side;
            var right = width - Side - 50;
            Func<DateTime, double> x = d => left + (right - left) * ((d - start).TotalDays / Math.Max(1, (end - start).TotalDays));

            var axis = new Line { X1 = left, X2 = right, Y1 = AxisY, Y2 = AxisY, StrokeThickness = 2 };
            axis.SetResourceReference(Shape.StrokeProperty, "ControlStrokeColorDefaultBrush");
            Children.Add(axis);

            // Ay başları (aralık uzunsa yalnızca bazıları).
            var culture = new CultureInfo("tr-TR");
            var months = new List<DateTime>();
            for (var m = new DateTime(start.Year, start.Month, 1).AddMonths(1); m <= end; m = m.AddMonths(1))
            {
                months.Add(m);
            }

            var step = Math.Max(1, (int)Math.Ceiling(months.Count * 70 / Math.Max(1, right - left)));
            for (var i = 0; i < months.Count; i += step)
            {
                var mx = x(months[i]);
                var tick = new Line { X1 = mx, X2 = mx, Y1 = AxisY - 5, Y2 = AxisY + 5, StrokeThickness = 1 };
                tick.SetResourceReference(Shape.StrokeProperty, "TextTertiaryBrush");
                Children.Add(tick);
                var name = Caption(months[i].ToString(months[i].Month == 1 ? "MMM yyyy" : "MMM", culture), "TextTertiaryBrush", 11);
                SetLeft(name, mx + 3);
                SetTop(name, AxisY + 4);
                Children.Add(name);
            }

            // Bugün.
            var tx = x(DateTime.Today);
            var today = new Line { X1 = tx, X2 = tx, Y1 = AxisY - 14, Y2 = AxisY + 14, StrokeThickness = 2, StrokeDashArray = new DoubleCollection { 2, 2 } };
            today.SetResourceReference(Shape.StrokeProperty, "AccentBrush");
            Children.Add(today);
            var todayText = Caption(Properties.Resources.TimeAxis_Today, "AccentTextFillColorPrimaryBrush", 11);
            SetLeft(todayText, tx + 4);
            SetTop(todayText, AxisY - 30);
            Children.Add(todayText);

            // Olaylar: etiket sırayla üstte / altta; aynı yanda bir öncekine çok yakınsa yazılmaz.
            var lastLabel = new[] { double.MinValue, double.MinValue };
            var side = 0;
            foreach (var point in points)
            {
                var px = x(point.SortDate.Date);
                var brushKey = BrushKey(point.PointTone);
                var tip = point.SortDate.ToString(Properties.Resources.Format_Date) + " · " + (point.PointSource.Length > 0 ? point.PointSource + " · " : string.Empty) + point.PointLabel;
                var placed = false;
                for (var attempt = 0; attempt < 2 && !placed; attempt++)
                {
                    var s = (side + attempt) % 2;
                    if (px - lastLabel[s] < LabelGap)
                    {
                        continue;
                    }

                    var up = s == 0;
                    var stem = new Line { X1 = px, X2 = px, Y1 = up ? AxisY - 7 : AxisY + 7, Y2 = up ? AxisY - 26 : AxisY + 26, StrokeThickness = 1 };
                    stem.SetResourceReference(Shape.StrokeProperty, brushKey);
                    Children.Add(stem);
                    var label = new StackPanel { ToolTip = tip, MaxWidth = LabelGap - 6 };
                    var head = Caption(point.PointLabel, brushKey, 12);
                    head.FontWeight = FontWeights.SemiBold;
                    head.TextTrimming = TextTrimming.CharacterEllipsis;
                    label.Children.Add(head);
                    var sub = Caption(point.SortDate.ToString("dd.MM", culture) + (point.PointSource.Length > 0 ? " · " + point.PointSource : string.Empty), "TextSecondaryBrush", 11);
                    sub.TextTrimming = TextTrimming.CharacterEllipsis;
                    label.Children.Add(sub);
                    SetLeft(label, px - 3);
                    SetTop(label, up ? AxisY - 60 : AxisY + 26);
                    Children.Add(label);
                    lastLabel[s] = px;
                    side = 1 - s;
                    placed = true;
                    LabelCount++;
                }

                var dot = new Ellipse { Width = 12, Height = 12, StrokeThickness = 2, ToolTip = tip };
                dot.SetResourceReference(Shape.FillProperty, brushKey);
                dot.SetResourceReference(Shape.StrokeProperty, "LayerBrush");
                SetLeft(dot, px - 6);
                SetTop(dot, AxisY - 6);
                Children.Add(dot);
                PointCount++;
            }
        }

        private static TextBlock Caption(string text, string brushKey, double size)
        {
            var block = new TextBlock { Text = text, FontSize = size };
            block.SetResourceReference(TextBlock.ForegroundProperty, brushKey);
            return block;
        }

        /// <summary>Renk anlamının fırçası (Geçmiş listesindeki noktalarla aynı).</summary>
        public static string BrushKey(string tone)
        {
            switch (tone)
            {
                case StatusTone.Done:
                    return "SystemFillColorSuccessBrush";
                case StatusTone.Active:
                    return "AccentBrush";
                case StatusTone.Waiting:
                case StatusTone.Warning:
                    return "SystemFillColorCautionBrush";
                case StatusTone.Failed:
                    return "SystemFillColorCriticalBrush";
                default:
                    return "TextSecondaryBrush";
            }
        }
    }
}