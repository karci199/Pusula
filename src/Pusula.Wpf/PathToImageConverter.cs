using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Pusula.Wpf
{
    /// <summary>
    /// Dosya yolunu küçültülmüş resme çevirir. Dosya kilitlenmez (OnLoad), bozuk ya da yoksa <c>null</c> döner.
    /// Telefon fotoğraflarındaki EXIF yönü uygulanır (dik çekilen fotoğraf yan yatık görünmez).
    /// </summary>
    public sealed class PathToImageConverter : IValueConverter
    {
        /// <summary>Resmin çözüleceği genişlik (piksel).</summary>
        public int DecodeWidth { get; set; } = 480;

        /// <inheritdoc />
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var path = value as string;
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return null;
            }

            try
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.DecodePixelWidth = DecodeWidth;
                image.UriSource = new Uri(path, UriKind.Absolute);
                image.EndInit();
                image.Freeze();

                var angle = RotationOf(path!);
                if (angle == 0)
                {
                    return image;
                }

                var rotated = new TransformedBitmap(image, new RotateTransform(angle));
                rotated.Freeze();
                return rotated;
            }
            catch (IOException)
            {
                return null;
            }
            catch (NotSupportedException)
            {
                return null;
            }
            catch (UnauthorizedAccessException)
            {
                return null;
            }
        }

        /// <summary>EXIF yön etiketinden (274) döndürme açısı: 3 → 180, 6 → 90, 8 → 270; yoksa 0.</summary>
        public static int RotationOf(string path)
        {
            try
            {
                using (var stream = File.OpenRead(path))
                {
                    var frame = BitmapFrame.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
                    var metadata = frame.Metadata as BitmapMetadata;
                    if (metadata == null || !metadata.ContainsQuery("/app1/ifd/{ushort=274}"))
                    {
                        return 0;
                    }

                    var value = metadata.GetQuery("/app1/ifd/{ushort=274}");
                    var orientation = value is ushort number ? number : 1;
                    switch (orientation)
                    {
                        case 3:
                            return 180;
                        case 6:
                            return 90;
                        case 8:
                            return 270;
                        default:
                            return 0;
                    }
                }
            }
            catch (IOException)
            {
                return 0;
            }
            catch (NotSupportedException)
            {
                return 0;
            }
            catch (UnauthorizedAccessException)
            {
                return 0;
            }
            catch (InvalidOperationException)
            {
                return 0;
            }
            catch (ArgumentException)
            {
                return 0;
            }
        }

        /// <inheritdoc />
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
