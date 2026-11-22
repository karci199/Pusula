# Pusula — adlandırma ve kullanım

Kısa başvuru. Ayrıntı ve kararların tarihçesi: [NOTLAR.md](NOTLAR.md).

## Temel kurallar

- **Klasör = yer, JSON = bilgi, md = anlatım.** Klasör ağacı diskten taranır; `_TAKIP\` içindeki JSON'ları uygulama yazar.
- Pusula dosya **silmez, taşımaz, yeniden adlandırmaz**. Klasör ya da dosya adını sen değiştirirsin.
- Notlar Obsidian'da yazılır. Pusula notun yalnız YAML'ına, `## Günlük` bölümünün başına, `## Yapılacaklar`
  satırlarına ve doğrulama dosyasının tablolarına yazar.

## Numaralar

| Ne | Biçim | Örnek |
|---|---|---|
| Proje | `####` (klasör `####_AD`) | `7000_MILKON` |
| Stok (ürün) | `@@-####-####` ya da `####-####-####` | `AB-7000-0003` |
| Geliştirme stoku | `JY-####-####` | `JY-7000-0004` |
| Test yardımcı aracı (kablaj, jig, cihaz, diğer) | `JT-####-####` | `JT-7000-0101` |
| Yazılım | `JY-####-####`, revizyon eki `_AA`, `_AB` … | `JY-7000-0105` |

Kayıt klasörünün adı yalnız numaradır (`JT-7000-0101`); tanım kayıtta ve notta durur.
Numara uygulamadan değiştirilmez; gerekirse klasör elle yeniden adlandırılır.

## Stok klasörü

```
AB-7000-0003\
├── AB-7000-0003.md              ← ana not (YAML, serbest metin, ## Yapılacaklar, en altta ## Günlük)
├── 01_Ortak\                    ← spek, çizim, DM
│   └── Fotograflar\             ← stokun fotoğrafları
├── 02_Test_Araclari\            ← bu stoka ait araçlar
│   ├── Kablo\JT-7000-0101\
│   ├── Jig\JT-7000-0104\
│   ├── Yazilim\JY-7000-0105\
│   ├── Cihaz\JT-7000-0106\
│   └── Diger\JT-7000-0107\
├── 03_Testler\
│   ├── AB-7000-0003_Test_Gereksinimleri.md   ← gereksinim · plan · rapor tabloları
│   ├── 01_Islevsel\ 02_Ozellik\ 03_CK_EMC\ 04_Diger\
│   └── Olcumler\<tarih>_<madde>_<başlık>\    ← ölçüm kaydı açınca boş açılır, dosyaları sen koyarsın
└── _ARSIV\                      ← eski dosyaların yedekleri (.md.yedek)
```

Genel araçlar kökte `ENVANTER\`, projeye ait olanlar `<proje>\00_Proje_Geneli\Envanter\` altında durur.

## Test yardımcı aracı klasörü

```
JT-7000-0101\
├── JT-7000-0101.md        ← aracın notu (gövde sayfada salt okunur görünür)
├── JT-7000-0101_kapak.jpg ← kapak
├── 1_SMA_ucu.jpg          ← şerit 1 (sayfada "SMA ucu")
├── 2_N_ucu.jpg            ← şerit 2
├── IMG_2041.jpg           ← numarasız: sayfada görünmez, yalnız sayısı yazılır
└── 01_Belgeler\           ← türe göre (jig: 01_Cizimler, 02_Belgeler; yazılım: 01_Kaynak, 02_Belgeler)
```

Fotoğraflar için ayrı klasör gerekmez. Eski kayıtlardaki `02_Fotograflar` / `03_Fotograflar` de okunur.

## Fotoğraf adları (stok ve araç için aynı)

| Ad | Ne olur |
|---|---|
| `<numara>_kapak.jpg` | Büyük resim (kapak) |
| `1_ad.jpg`, `2_ad.jpg` … `10_ad.jpg` | Şerit, sayı sırasıyla; sayıdan sonrası şeritte ad olur (`_` boşluk olur) |
| Başka her ad (`IMG_2041.jpg`, `taslak.png`) | Görünmez; yalnız "n fotoğraf daha klasörde" |

Ayraç olarak `_`, `-` ya da boşluk kullanılabilir (`1-on.jpg`, `2 yan.png`). Uzantı: jpg, jpeg, png, bmp, gif, tif.
Kapak yoksa büyük alanda türün simgesi ve bu kuralın ipucu görünür.

## Notlar ve olaylar

- Ana not stokla / araçla aynı adı taşır: `<numara>.md`.
- Olaylar notun en altındaki `## Günlük` bölümüne yazılır, en yeni üstte. Başlık: `### gg.aa.yyyy · Tür · Başlık`.
- Olay türleri: Ölçüm, Toplantı, Karar, Talep, Teslim, Sevkiyat, Revizyon, **Kalibrasyon**, **Tamir / bakım**, Sorun,
  Çözüm, Hazırlık, Not. Kendi türün için: `_TAKIP\Islemler.md` (`- Ad · ikon · renk`).
- Stoka ait aracın olayları stokun zaman çizelgesinde de görünür.
- Yazılım revizyonu: Revizyon olayı (`rev: AB → AC`), açıklamaya eklenenler yazılır; en yenisi Özellikler'de görünür.

## Araç alanları

Her türde **Kullanım amacı**. Kablajda ayrıca Boy ve Ekran (ekranlı / ekransız); RF'te Frekans bandı, Haberleşme'de
Pin yapısı, Güç'te Gerilim · Akım · Kesit (AWG). Alan eklemek / çıkarmak: Ayarlar → Türler.

## Doğrulama dosyası

`03_Testler\<stok>_Test_Gereksinimleri.md`: ilk tablo gereksinimler; `## Plan` ve `## Rapor` altındaki ilk tablolar
plan ve rapor maddeleri. Satırlar uygulamadan eklenir; Excel'den alınabilir, Excel'e verilebilir. Plan / rapor
ayrıntısı aynı dosyada `### <madde no> · başlık` altına elle yazılır.

## Örnek veri

`tools\Ornek-Araclar.ps1 -Kok <kök>`: bir stokun altına her türden örnek araç açar (fotoğraf, not, olaylarla).
