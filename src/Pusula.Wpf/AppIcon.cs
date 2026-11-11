using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Pusula.Wpf
{
    /// <summary>
    /// Pusula'nın kendi vektör ikonu. <see cref="Glyph"/> eski yazı tipi kod noktasıdır (türler dosyasında da böyle
    /// saklanır); karşılığı <see cref="IconGeometries"/> içinde çizilmişse vektör çizilir, yoksa yazı tipi ikonuna
    /// düşer. Renk <c>Foreground</c>, boyut <c>FontSize</c> ile verilir (DesignKit <c>FontIcon</c> gibi).
    /// </summary>
    public sealed class AppIcon : Control
    {
        /// <summary>Çizgi kalınlığı, 16 birimlik ızgarada.</summary>
        private const double StrokeWidth = 1.15;

        /// <summary>Boyut ve renk değişince yeniden çizilir.</summary>
        static AppIcon()
        {
            FontSizeProperty.OverrideMetadata(typeof(AppIcon),
                new FrameworkPropertyMetadata(14.0, FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));
            ForegroundProperty.OverrideMetadata(typeof(AppIcon),
                new FrameworkPropertyMetadata(SystemColors.ControlTextBrush, FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsRender));
            FocusableProperty.OverrideMetadata(typeof(AppIcon), new FrameworkPropertyMetadata(false));
        }

        /// <summary>Yedek yazı tipi temadan gelir.</summary>
        public AppIcon()
        {
            SetResourceReference(FontFamilyProperty, "SymbolThemeFontFamily");
        }

        /// <summary>İkon kodu (yazı tipi kod noktası, örn. <c>""</c>).</summary>
        public string Glyph
        {
            get { return (string)GetValue(GlyphProperty); }
            set { SetValue(GlyphProperty, value); }
        }

        /// <summary><see cref="Glyph"/> bağımlılık özelliği.</summary>
        public static readonly DependencyProperty GlyphProperty =
            DependencyProperty.Register(nameof(Glyph), typeof(string), typeof(AppIcon),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>İkon, kenarı <c>FontSize</c> olan bir kare kaplar.</summary>
        protected override Size MeasureOverride(Size constraint)
        {
            return new Size(FontSize, FontSize);
        }

        /// <summary>Vektörü (yoksa yazı tipi karakterini) ortalayarak çizer.</summary>
        protected override void OnRender(DrawingContext dc)
        {
            var size = FontSize;
            var left = Math.Round((ActualWidth - size) / 2);
            var top = Math.Round((ActualHeight - size) / 2);
            dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, ActualWidth, ActualHeight));
            var glyph = Glyph ?? string.Empty;
            var shape = IconGeometries.Find(glyph);
            if (shape != null)
            {
                var scale = size / 16.0;
                var pen = new Pen(Foreground, Math.Max(StrokeWidth, 1.0 / scale))
                {
                    StartLineCap = PenLineCap.Round,
                    EndLineCap = PenLineCap.Round,
                    LineJoin = PenLineJoin.Round,
                };
                dc.PushTransform(new TranslateTransform(left, top));
                dc.PushTransform(new ScaleTransform(scale, scale));
                if (shape.Stroke != null)
                {
                    dc.DrawGeometry(null, pen, shape.Stroke);
                }

                if (shape.Fill != null)
                {
                    dc.DrawGeometry(Foreground, null, shape.Fill);
                }

                dc.Pop();
                dc.Pop();
                return;
            }

            if (glyph.Length == 0)
            {
                return;
            }

            var text = new FormattedText(glyph, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                new Typeface(FontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal), size, Foreground,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);
            dc.DrawText(text, new Point((ActualWidth - text.Width) / 2, (ActualHeight - text.Height) / 2));
        }
    }
}
