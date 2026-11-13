using System;
using System.Windows;
using System.Windows.Controls;

namespace Pusula.Wpf
{
    /// <summary>
    /// Çocukları soldan sağa iki eşit sütuna dizer; her satır yalnız kendi iki hücresinin en uzunu kadar yükselir
    /// (UniformGrid bütün satırları en uzun hücre kadar yapar). Gizli (Collapsed) çocuklar atlanır.
    /// </summary>
    public sealed class TwoColumnRowsPanel : Panel
    {
        /// <summary>Çocukları yarım genişlikle ölçer, satır yüksekliklerini toplar.</summary>
        protected override Size MeasureOverride(Size availableSize)
        {
            var columnWidth = double.IsInfinity(availableSize.Width) ? double.PositiveInfinity : availableSize.Width / 2;
            var widest = 0.0;
            var height = 0.0;
            var rowHeight = 0.0;
            var column = 0;
            foreach (UIElement child in InternalChildren)
            {
                if (child == null || child.Visibility == Visibility.Collapsed)
                {
                    continue;
                }

                child.Measure(new Size(columnWidth, double.PositiveInfinity));
                widest = Math.Max(widest, child.DesiredSize.Width);
                rowHeight = Math.Max(rowHeight, child.DesiredSize.Height);
                column++;
                if (column == 2)
                {
                    height += rowHeight;
                    rowHeight = 0;
                    column = 0;
                }
            }

            height += rowHeight;
            var width = double.IsInfinity(availableSize.Width) ? widest * 2 : availableSize.Width;
            return new Size(width, height);
        }

        /// <summary>Çocukları satır satır yerleştirir; iki hücre aynı satır yüksekliğini paylaşır.</summary>
        protected override Size ArrangeOverride(Size finalSize)
        {
            var columnWidth = finalSize.Width / 2;
            var top = 0.0;
            var pending = new UIElement[2];
            var column = 0;
            foreach (UIElement child in InternalChildren)
            {
                if (child == null || child.Visibility == Visibility.Collapsed)
                {
                    continue;
                }

                pending[column] = child;
                column++;
                if (column == 2)
                {
                    top += ArrangeRow(pending, column, columnWidth, top);
                    column = 0;
                }
            }

            if (column > 0)
            {
                ArrangeRow(pending, column, columnWidth, top);
            }

            return finalSize;
        }

        private static double ArrangeRow(UIElement[] row, int count, double columnWidth, double top)
        {
            var rowHeight = 0.0;
            for (var i = 0; i < count; i++)
            {
                rowHeight = Math.Max(rowHeight, row[i].DesiredSize.Height);
            }

            for (var i = 0; i < count; i++)
            {
                row[i].Arrange(new Rect(i * columnWidth, top, columnWidth, rowHeight));
            }

            return rowHeight;
        }
    }
}
