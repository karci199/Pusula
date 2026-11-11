using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Media;

namespace Pusula.Wpf
{
    /// <summary>Bir ikonun çizimi: çizgi (kontur) ve dolgu parçaları; ikisi de 16×16 birimlik ızgarada.</summary>
    public sealed class IconShape
    {
        /// <summary>Çizimi kurar; boş parça <c>null</c> kalır.</summary>
        public IconShape(string stroke, string fill)
        {
            Stroke = Parse(stroke);
            Fill = Parse(fill);
        }

        /// <summary>Kontur olarak çizilen parça.</summary>
        public Geometry? Stroke { get; }

        /// <summary>Dolgu olarak çizilen parça (noktalar).</summary>
        public Geometry? Fill { get; }

        private static Geometry? Parse(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                return null;
            }

            var geometry = Geometry.Parse(data);
            geometry.Freeze();
            return geometry;
        }
    }

    /// <summary>
    /// Pusula'nın vektör ikonları; anahtar eski yazı tipi kod noktası (türler dosyası ve <see cref="AppIcons"/> bunu
    /// kullanır). Çizgiler yuvarlak uçlu, kalınlık <see cref="AppIcon"/> içinde.
    /// </summary>
    public static class IconGeometries
    {
        private static readonly Dictionary<string, IconShape> Shapes = Build();

        /// <summary>Kod noktasının çizimi; çizilmemişse <c>null</c> (yazı tipine düşülür).</summary>
        public static IconShape? Find(string glyph)
        {
            IconShape shape;
            return glyph != null && Shapes.TryGetValue(glyph, out shape) ? shape : null;
        }

        /// <summary>Çizilmiş bütün kod noktaları (pafta ve test için).</summary>
        public static IEnumerable<string> All
        {
            get { return Shapes.Keys; }
        }

        private static string Dot(double x, double y, double r)
        {
            return string.Format(CultureInfo.InvariantCulture, "M{0},{1} a{2},{2} 0 1 0 {3},0 a{2},{2} 0 1 0 {4},0 Z ", x - r, y, r, 2 * r, -2 * r);
        }

        private static string Circle(double x, double y, double r)
        {
            return Dot(x, y, r);
        }

        private static string Gear()
        {
            var sb = new StringBuilder();
            sb.Append(Circle(8, 8, 2.2));
            sb.Append(Circle(8, 8, 4.6));
            for (var i = 0; i < 8; i++)
            {
                var a = i * Math.PI / 4;
                sb.Append(string.Format(CultureInfo.InvariantCulture, "M{0:0.##},{1:0.##} L{2:0.##},{3:0.##} ",
                    8 + 4.6 * Math.Cos(a), 8 + 4.6 * Math.Sin(a), 8 + 6.4 * Math.Cos(a), 8 + 6.4 * Math.Sin(a)));
            }

            return sb.ToString();
        }

        private static Dictionary<string, IconShape> Build()
        {
            var d = new Dictionary<string, IconShape>(StringComparer.Ordinal);
            void Add(int code, string stroke, string fill = "")
            {
                d[((char)code).ToString()] = new IconShape(stroke, fill);
            }

            var page = "M3.5,1.8 H10 L12.8,4.6 V14.2 H3.5 Z M10,1.8 V4.6 H12.8 ";
            var folder = "M1.8,3.5 H6 L7.5,5 H14.2 V12.8 H1.8 Z ";

            // Kayıt türleri
            Add(0xE821, "M2,5.5 H14 V13.5 H2 Z M5.5,5.5 V3.2 H10.5 V5.5 M2,9 H14 M7,9 V10.5 H9 V9");                       // proje: evrak çantası
            Add(0xE7B8, "M8,1.8 L14,4.8 V11.2 L8,14.2 L2,11.2 V4.8 Z M2,4.8 L8,7.8 L14,4.8 M8,7.8 V14.2 M5,3.3 L11,6.3");     // stok: paket
            Add(0xE703, "M1.6,4.8 H4.6 M4.6,3.2 H8.4 V6.4 H4.6 Z M6.5,3.2 V6.4 M8.4,4.8 C12.6,4.8 13.6,7.6 10.8,9.6 C8.2,11.4 9.2,14.2 14.4,14.2"); // kablo: konnektör ve kıvrılan kablo
            Add(0xE90F, "M13.8,4.6 A3.5,3.5 0 0 1 9.2,8.6 L4.2,13.6 A1.4,1.4 0 0 1 2.4,11.8 L7.4,6.8 A3.5,3.5 0 0 1 11.4,2.2 L9.6,4 L10.2,5.8 L12,6.4 Z"); // jig: anahtar
            Add(0xE943, "M5.5,4.5 L2,8 L5.5,11.5 M10.5,4.5 L14,8 L10.5,11.5 M9,3.2 L7,12.8");                                // yazılım / kod
            Add(0xE950, "M4.5,5.2 H11.5 V10.8 H4.5 Z M1.6,6.8 H4.5 V9.2 H1.6 Z M11.5,6.8 H14.4 V9.2 H11.5 Z M6.8,5.2 V10.8 M9.2,5.2 V10.8"); // test gereci: adaptör / zayıflatıcı
            Add(0xE77B, Circle(8, 5, 2.8) + "M2.5,14 C2.5,10.6 5,9.2 8,9.2 C11,9.2 13.5,10.6 13.5,14");                    // kişi
            Add(0xE8F1, "M1.8,13.8 H14.2 M2.8,13.8 V6.2 H5.8 V13.8 M6.6,13.8 V3 H9.6 V13.8 M10.4,13.8 L11.9,5.6 L14.1,6 L12.6,14"); // envanter: raf
            Add(0xE74C, "M4.5,4.5 H11.5 V11.5 H4.5 Z M6.5,2 V4.5 M9.5,2 V4.5 M6.5,11.5 V14 M9.5,11.5 V14 M2,6.5 H4.5 M2,9.5 H4.5 M11.5,6.5 H14 M11.5,9.5 H14"); // devre / kart
            Add(0xE7EE, "M8,7.5 V14.5 M5.5,14.5 H10.5 M5.6,4.9 A3.4,3.4 0 0 0 5.6,10.1 M10.4,4.9 A3.4,3.4 0 0 1 10.4,10.1 M3.6,2.8 A6.3,6.3 0 0 0 3.6,12.2 M12.4,2.8 A6.3,6.3 0 0 1 12.4,12.2", Dot(8, 7.5, 1.1)); // anten
            Add(0xE964, "M1.8,2.8 H14.2 V11.8 H1.8 Z M3.8,8.8 L5.6,5.2 L7.4,8.8 L9.2,5.2 L11,8.8 L12.4,6.4 M5,14.2 H11 M8,11.8 V14.2"); // ölçüm cihazı: ekran ve dalga
            Add(0xE83F, "M5.5,1.8 V5 M10.5,1.8 V5 M3.5,5 H12.5 V7.8 A4.5,4.5 0 0 1 3.5,7.8 Z M8,12.3 V14.5");              // güç / fiş
            Add(0xE7F8, "M3.2,3.2 H12.8 V10.6 H3.2 Z M1.5,13 H14.5");                                                      // dizüstü
            Add(0xE80F, "M2,7.5 L8,2.4 L14,7.5 M3.8,6 V13.6 H12.2 V6 M6.6,13.6 V9.6 H9.4 V13.6");                          // genel / ev
            Add(0xE1D3, "M8,4 C6.5,2.8 4,2.5 2,3 V13 C4,12.5 6.5,12.8 8,14 C9.5,12.8 12,12.5 14,13 V3 C12,2.5 9.5,2.8 8,4 Z M8,4 V14"); // kitap
            Add(0xE88E, "M5.2,6.2 H10.8 V14.2 H5.2 Z M6.4,6.2 V1.8 H9.6 V6.2 M7.4,3.6 V4.2 M8.6,3.6 V4.2");                  // USB
            Add(0xE945, "M9.5,1.8 L3.5,9 H7.8 L6.5,14.2 L12.5,7 H8.2 Z");                                                  // şimşek
            Add(0xE9F5, Gear());                                                                                           // çark
            Add(0xE713, Gear());                                                                                           // ayarlar
            Add(0xE8B7, folder);                                                                                           // klasör
            Add(0xF12B, folder);                                                                                           // işaretsiz klasör

            // Genel işlemler
            Add(0xED25, "M1.8,12.8 V3.5 H6 L7.5,5 H12.5 V7.2 M1.8,12.8 L4,7.5 H14.6 L12.4,12.8 Z");                       // klasörü aç
            Add(0xE712, string.Empty, Dot(3.2, 8, 1.15) + Dot(8, 8, 1.15) + Dot(12.8, 8, 1.15));                           // ⋯
            Add(0xE8C8, "M5.5,5.5 H13.5 V13.5 H5.5 Z M3.5,10.5 H2.5 V2.5 H10.5 V3.5");                                    // kopyala
            Add(0xE740, "M2,6 V2 H6 M10,2 H14 V6 M14,10 V14 H10 M6,14 H2 V10");                                           // tam boy
            Add(0xE710, "M8,2.5 V13.5 M2.5,8 H13.5");                                                                      // ekle
            Add(0xE72C, "M13,8 A5,5 0 1 1 11.6,4.5 M12.2,1.6 V4.8 H9");                                                    // yenile
            Add(0xE74D, "M2.5,4 H13.5 M6,4 V2.4 H10 V4 M3.8,4 L4.6,14 H11.4 L12.2,4 M6.6,6.6 V11.4 M9.4,6.6 V11.4");      // sil
            Add(0xE7C3, page);                                                                                             // şablon / sayfa
            Add(0xE8A5, page + "M5.8,8 H10.5 M5.8,10.8 H10.5");                                                            // not
            Add(0xE70F, "M10.5,2.5 L13.5,5.5 L5.5,13.5 L2.2,13.8 L2.5,10.5 Z M9,4 L12,7");                                // düzenle
            Add(0xE823, Circle(8, 8, 6.2) + "M8,4.4 V8 L10.6,9.6");                                                        // saat
            Add(0xE783, Circle(8, 8, 6.2) + "M8,4.6 V8.8", Dot(8, 11.3, 0.95));                                           // hata
            Add(0xE897, Circle(8, 8, 6.2) + "M6.1,6.3 A1.95,1.95 0 1 1 8.9,8 C8.2,8.3 8,8.8 8,9.5", Dot(8, 11.5, 0.9));   // soru
            Add(0xE71B, "M6.4,9.6 L9.6,6.4 M7.1,4.4 L8.3,3.2 A2.5,2.5 0 0 1 12.8,7.7 L11.6,8.9 M8.9,11.6 L7.7,12.8 A2.5,2.5 0 0 1 3.2,8.3 L4.4,7.1"); // bağlantı
            Add(0xE73E, "M2.5,8.5 L6.2,12 L13.5,4.2");                                                                     // onay
            Add(0xE711, "M3.5,3.5 L12.5,12.5 M12.5,3.5 L3.5,12.5");                                                        // ✕
            Add(0xE70D, "M3.5,6 L8,10.5 L12.5,6");                                                                         // aşağı
            Add(0xE70E, "M3.5,10 L8,5.5 L12.5,10");                                                                        // yukarı
            Add(0xE76C, "M6,3.5 L10.5,8 L6,12.5");                                                                         // sağ
            Add(0xE8A7, "M9.5,2 H14 V6.5 M14,2 L8,8 M12,9.5 V14 H2 V4 H6.5");                                             // dışarıda aç
            Add(0xE74E, "M2.5,2.5 H11.5 L13.5,4.5 V13.5 H2.5 Z M5,2.5 V6 H10.5 V2.5 M4.8,13.5 V9.5 H11.2 V13.5");         // kaydet
            Add(0xE7BA, "M8,1.8 L14.8,13.8 H1.2 Z M8,6 V9.6", Dot(8, 11.7, 0.9));                                          // uyarı
            Add(0xE946, Circle(8, 8, 6.2) + "M8,7.2 V11.6", Dot(8, 4.9, 0.9));                                             // bilgi

            // Sekmeler
            Add(0xE9D9, "M1.5,8.5 H4.5 L6.5,3.5 L9.5,12.5 L11.5,8.5 H14.5");                                               // son koşu: nabız
            Add(0xEB95, "M8,1.8 L13.5,3.8 V8 C13.5,11.3 11,13.5 8,14.5 C5,13.5 2.5,11.3 2.5,8 V3.8 Z M5.5,8 L7.3,9.8 L10.7,6.3"); // kalifikasyon: kalkan

            // Olay türleri
            Add(0xE70B, "M2.5,2.5 H13.5 V9.5 L9.5,13.5 H2.5 Z M13.5,9.5 H9.5 V13.5");                                    // kısa not
            Add(0xE716, Circle(6, 5, 2.3) + Circle(11.4, 5.8, 1.9) + "M1.5,13.5 C1.5,10.6 3.5,9.3 6,9.3 C8.5,9.3 10.5,10.6 10.5,13.5 M11.6,9.4 C13.3,9.6 14.5,10.8 14.5,13.5"); // toplantı
            Add(0xE8FB, Circle(8, 8, 6.2) + "M5,8.2 L7.2,10.4 L11.2,5.8");                                                 // karar
            Add(0xE768, "M4.5,2.5 L13,8 L4.5,13.5 Z");                                                                     // test başladı
            Add(0xE7C1, "M3.5,14.5 V2 M3.5,2.5 H12.5 L10.5,5.5 L12.5,8.5 H3.5");                                           // test bitti: bayrak
            Add(0xE896, "M8,2 V10.5 M4.5,7 L8,10.5 L11.5,7 M2.5,13.5 H13.5");                                              // teslim
            Add(0xE724, "M14.2,1.8 L1.8,7 L6.8,9.2 L9,14.2 Z M6.8,9.2 L14.2,1.8");                                         // üretime
            Add(0xE804, "M1.5,3.8 H10 V11 H1.5 Z M10,6.2 H12.8 L14.5,8.8 V11 H10" + " " + Circle(4.3, 12.3, 1.3) + Circle(11.7, 12.3, 1.3)); // sevk: kamyon

            // md penceresi
            Add(0xE8A4, "M2,3 H14 V13 H2 Z M6,3 V13");                                                                     // başlık paneli
            Add(0xE8DD, "M4.6,2.5 H9 A2.75,2.75 0 0 1 9,8 H4.6 Z M4.6,8 H9.8 A2.75,2.75 0 0 1 9.8,13.5 H4.6 Z");           // kalın
            Add(0xE8DB, "M7,2.5 H12 M4,13.5 H9 M9.5,2.5 L6.5,13.5");                                                       // italik
            Add(0xE7E6, "M9.6,2.2 L13.8,6.4 L7.8,12.4 H4.2 V8.8 Z M2,14.2 H14");                                           // vurgu
            Add(0xE8FD, "M6,4 H14 M6,8 H14 M6,12 H14", Dot(3, 4, 1) + Dot(3, 8, 1) + Dot(3, 12, 1));                      // liste
            Add(0xE9D5, "M6.5,4 H14 M6.5,8 H14 M6.5,12 H14 M2.4,2.9 L3.4,2.4 V5.6 M2.2,7 C2.6,6.3 4.3,6.4 4.1,7.6 L2.2,9.5 H4.3 M2.2,10.8 H4 L3,12 C4.7,12 4.6,14.2 2.2,13.8"); // numaralı liste
            Add(0xE73A, "M2.5,2.5 H13.5 V13.5 H2.5 Z M5,8.2 L7.2,10.4 L11.2,5.8");                                         // görev
            Add(0xE9B2, "M3,9 C3,6 4,4.5 6.5,4 M3,9 H6.5 V12.5 H3 Z M9,9 C9,6 10,4.5 12.5,4 M9,9 H12.5 V12.5 H9 Z");     // alıntı
            Add(0xEB9F, "M1.8,2.8 H14.2 V13.2 H1.8 Z M1.8,11 L5.5,7.5 L8.5,10.5 L10.5,8.5 L14.2,12" + " " + Circle(10.8, 5.6, 1.2)); // görsel
            Add(0xF0E2, "M2,2.5 H14 V13.5 H2 Z M2,6.2 H14 M2,9.8 H14 M6,2.5 V13.5 M10,2.5 V13.5");                         // tablo
            Add(0xE76F, "M1.8,8 H14.2 M4.5,4.8 H11.5 M4.5,11.2 H11.5");                                                    // ayraç
            Add(0xE90A, "M2,2.5 H14 V10.5 H7 L4,13.5 V10.5 H2 Z");                                                         // yorum
            return d;
        }
    }
}
