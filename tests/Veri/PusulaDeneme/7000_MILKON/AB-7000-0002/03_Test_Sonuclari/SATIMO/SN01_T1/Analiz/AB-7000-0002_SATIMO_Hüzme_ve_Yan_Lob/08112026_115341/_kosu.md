# Koşu 08112026_115341 · AB-7000-0002 SATIMO Hüzme ve Yan Lob

**Genel sonuç: GEÇTİ**

## Adımlar

| Adım | Sonuç | Eğri | Geçen | Kalan | Dosya | Hata | Excel |
|---|---|--:|--:|--:|--:|--:|---|
| H Duzlem Huzme | GEÇTİ | 1 | 1 | 0 | 1 | 0 | [H_Duzlem_Huzme.xlsx](H_Duzlem_Huzme/H_Duzlem_Huzme.xlsx) |
| E Duzlem Huzme | GEÇTİ | 1 | 1 | 0 | 1 | 0 | [E_Duzlem_Huzme.xlsx](E_Duzlem_Huzme/E_Duzlem_Huzme.xlsx) |
| Yan Lob | GEÇTİ | 2 | 2 | 0 | 1 | 0 | [Yan_Lob.xlsx](Yan_Lob/Yan_Lob.xlsx) |

## Koşu

| | |
|---|---|
| Kimlik | 08112026_115341 |
| Başlangıç | 2026-11-08T11:53:41+03:00 |
| Bitiş | 2026-11-08T11:53:42+03:00 |
| Süre | 0,3 sn |
| Kişi | katan |
| Bilgisayar | KARCI-PC · KARCI-PC\katan |
| İşletim sistemi | Windows 10 Pro 19045 · .NET 4.8.1 |
| Yazılım | KIWI 1.0.0 · 0000-0000 |
| Plan | AB-7000-0002 SATIMO Hüzme ve Yan Lob · Rev 0 · 7b0c2e51-3d4a-4f6e-9a2b-5c8d1e0f7002 |
| Ham dizin | C:\Projeler\Veri\Pusula-Ornek\AB-7000-0002\03_Test_Sonuclari\SATIMO\SN01\Ham |
| Çıktı klasörü | C:\Projeler\Veri\Pusula-Ornek\AB-7000-0002\03_Test_Sonuclari\SATIMO\SN01\Analiz\AB-7000-0002_SATIMO_Hüzme_ve_Yan_Lob\08112026_115341 |
| APM biçimi | Metin (ASCII) |
| Aynı anda adım | 3 |
| Tepe bellek | 106 MB |

## Ürün ve iş

| | |
|---|---|
| Ürün tanımı | — |
| Proje numarası | — |
| Stok numarası | — |
| Seri numarası | — |
| Revizyon | — |
| Testi yapan | — |
| İş emri | — |
| Firma | — |
| İş planı adımı | — |
| Test kayıt dizini | — |
| Ek bilgiler | — |

## Adım ayarları ve dosyalar

### H Duzlem Huzme — GEÇTİ

- **Kaynak:** 20261107_AB-7000-0002_SN01_P1_SATIMO_pattern.trx
- **Biçim:** trx · spherical
- **Eksen:** φ 0 · θ tümü · f tümü
- **Sütun:** Ludwig-III ECo|DB
- **Analiz:** Hüzme Genişliği · "H Duzlem Huzme"
- **Limit:** 
  1. Değer · φ 0 · 2000:6000: %100 Alt 50 Üst 70
- **Excel:** [H_Duzlem_Huzme.xlsx](H_Duzlem_Huzme/H_Duzlem_Huzme.xlsx) (155 ms)

| Kaynak | Boyut | Nokta × sütun | Eğri | Okuma ms | APM ms | Analiz ms | Limit ms | Sonuç |
|---|--:|--:|--:|--:|--:|--:|--:|---|
| 20261107_AB-7000-0002_SN01_P1_SATIMO_pattern.trx | 2,7 MB | 1.241 × 1 | 1 | 27 | 7 | 13 | 21 | tamam |

### E Duzlem Huzme — GEÇTİ

- **Kaynak:** 20261107_AB-7000-0002_SN01_P1_SATIMO_pattern.trx
- **Biçim:** trx · spherical
- **Eksen:** φ 90 · θ tümü · f tümü
- **Sütun:** Ludwig-III ECo|DB
- **Analiz:** Hüzme Genişliği · "E Duzlem Huzme"
- **Limit:** 
  1. Değer · φ 90 · 2000:6000: %100 Alt 50 Üst 70
- **Excel:** [E_Duzlem_Huzme.xlsx](E_Duzlem_Huzme/E_Duzlem_Huzme.xlsx) (156 ms)

| Kaynak | Boyut | Nokta × sütun | Eğri | Okuma ms | APM ms | Analiz ms | Limit ms | Sonuç |
|---|--:|--:|--:|--:|--:|--:|--:|---|
| 20261107_AB-7000-0002_SN01_P1_SATIMO_pattern.trx | 2,7 MB | 1.241 × 1 | 1 | 27 | 7 | 13 | 22 | tamam |

### Yan Lob — GEÇTİ

- **Kaynak:** 20261107_AB-7000-0002_SN01_P1_SATIMO_pattern.trx
- **Biçim:** trx · spherical
- **Eksen:** φ 0 ; 90 · θ tümü · f tümü
- **Sütun:** Ludwig-III ECo|DB
- **Analiz:** Yan Lob Seviyesi · "Yan Lob"
- **Limit:** 
  1. Değer · tümü · 2000:6000: %100 Üst -10
- **Excel:** [Yan_Lob.xlsx](Yan_Lob/Yan_Lob.xlsx) (166 ms)

| Kaynak | Boyut | Nokta × sütun | Eğri | Okuma ms | APM ms | Analiz ms | Limit ms | Sonuç |
|---|--:|--:|--:|--:|--:|--:|--:|---|
| 20261107_AB-7000-0002_SN01_P1_SATIMO_pattern.trx | 2,7 MB | 2.482 × 1 | 2 | 28 | 6 | 12 | 22 | tamam |

## Süreler

| Aşama | Toplam ms |
|---|--:|
| Okuma (başlık + veri + yorumlama) | 82 |
| APM yazma | 20 |
| Analiz | 38 |
| Limit | 65 |
| Excel | 477 |
| Koşu (duvar saati) | 283 |

Aşama süreleri adımlar ve dosyalar boyunca toplamdır; adımlar aynı anda koştuğu için toplamları duvar saatini aşabilir.

---
_KIWI 1.0.0 · koşu 08112026_115341 · bu dosya koşu bittiğinde yazıldı_
