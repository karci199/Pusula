---
baslik: md yazım kılavuzu
tarih: 2026-11-10
etiketler: [kılavuz, md]
---

# md yazım kılavuzu

Pusula'daki notlar **md (Markdown)** dosyasıdır: düz yazıdır, birkaç işaretle başlık, liste, tablo, bağlantı olur.
Aynı dosya Pusula'nın not penceresinde, Obsidian'da ve VS Code'da aynı görünür.

> [!tip] Bu kılavuzu denemenin en kolay yolu
> Pusula'nın not penceresinde **Ctrl+2** (yan yana) aç: solda yazdığın, sağda nasıl göründüğü.
> Her örneği sola yaz, sağa bak.

İçindekiler:

1. [Paragraf ve satır](#1-paragraf-ve-satır)
2. [Başlıklar](#2-başlıklar)
3. [Yazı biçimi](#3-yazı-biçimi-kalın-italik-)
4. [Listeler](#4-listeler)
5. [Görev kutuları (checklist)](#5-görev-kutuları-checklist)
6. [Bağlantılar: kayıt, not, web](#6-bağlantılar)
7. [Resim](#7-resim)
8. [Tablo](#8-tablo)
9. [Uyarı kutuları](#9-uyarı-kutuları-callout)
10. [Alıntı](#10-alıntı)
11. [Kod ve ölçüm çıktısı](#11-kod-ve-ölçüm-çıktısı)
12. [Yorum (gizli not)](#12-yorum-gizli-not)
13. [Etiket](#13-etiket)
14. [Dipnot](#14-dipnot)
15. [Ayraç çizgisi](#15-ayraç-çizgisi)
16. [YAML başlığı (özellikler)](#16-yaml-başlığı-özellikler)
17. [Pusula'ya özel kurallar](#17-pusulaya-özel-kurallar)
18. [Kısayollar](#18-pusula-not-penceresi-kısayolları)
19. [Sık yapılan hatalar](#19-sık-yapılan-hatalar)
20. [Tam örnek not](#20-tam-örnek-not)

---

## 1. Paragraf ve satır

Düz yazı yazılır. **Boş satır yeni paragraf** açar.

```md
Bu birinci paragraf.
Bu satır da birinci paragrafın içinde.

Bu ikinci paragraf (arada boş satır var).
```

> [!note] Tek satır sonu
> Standart md'de tek Enter satırı **birleştirir**. Pusula ve Obsidian varsayılan olarak tek Enter'ı da yeni satır
> gösterir (Pusula'da: Ayarlar → **Satır sonu**). Her yerde aynı görünsün istiyorsan paragrafları boş satırla ayır.

---

## 2. Başlıklar

Satır başında `#` ve **bir boşluk**. `#` sayısı düzeyi verir (1–6).

```md
# Ana başlık (notta bir tane)
## Bölüm
### Alt bölüm
#### Daha küçük
```

- `#Başlık` (boşluksuz) başlık **olmaz**, etiket olur → bkz. [Etiket](#13-etiket).
- Pusula'da **Ctrl+H** satırı `#` → `##` → `###` → düz arasında döndürür.

---

## 3. Yazı biçimi (kalın, italik …)

| Yazılan              | Görünen             | Kısayol      |
| -------------------- | ------------------- | ------------ |
| `**kalın**`          | **kalın**           | Ctrl+B       |
| `*italik*`           | *italik*            | Ctrl+I       |
| `***ikisi***`        | ***ikisi***         |              |
| `~~üstü çizili~~`    | ~~üstü çizili~~     | Ctrl+Shift+X |
| `==vurgu==`          | ==vurgu==           | Ctrl+Shift+H |
| `` `kod` ``          | `kod`               | Ctrl+E       |

Örnek:

```md
Konnektör **SMA** yerine *N tipi* seçildi; ~~eski çizim~~ geçersiz. ==Teslimden önce tork kontrolü.==
```

- Kısayol seçili metni sarar; aynı kısayola **tekrar** basınca işaret kalkar.
- İşaretle metin arasında boşluk olmaz: `** kalın**` çalışmaz, `**kalın**` çalışır.

---

## 4. Listeler

**Madde işaretli:** satır başında `- ` (ya da `* `, `+ `).

```md
- SATIMO ölçümü
- Yankısız oda
  - Ön hazırlık (iki boşlukla iç içe)
  - Kalibrasyon
- Rapor
```

**Numaralı:** `1. `, `2. ` … (numaralar sırayla olmasa da md kendisi sıralar).

```md
1. Jig'i tak
2. Kabloyu tork anahtarıyla sık (0,9 Nm)
3. Ölçümü başlat
```

Pusula'da:

- Liste satırında **Enter** → yeni madde kendiliğinden gelir; boş maddede Enter listeden çıkar.
- **Tab** girintiyi artırır (iç içe), **Shift+Tab** azaltır.

---

## 5. Görev kutuları (checklist)

Satır başında `- [ ] ` (boş) ya da `- [x] ` (bitti).

```md
- [ ] SN02 tekrar ölç
- [x] Kablo kayıpları girildi
- [ ] Rapor taslağı #gorev
```

- Pusula'nın **Görünüm**ünde kutuya tıklayınca metindeki `[ ]` ↔ `[x]` değişir ve not kaydedilir.
- **Ctrl+L** seçili satırları görev yapar / görevi kaldırır.
- `#gorev` etiketi ana notun **Açık işler** bölümünde işi işaretlemek için kullanılır (Pusula kuralı).

---

## 6. Bağlantılar

### Kayda / nota bağlantı (Obsidian biçimi)

```md
[[JIG-1000-0009]]                       jig
[[JT-0001-0003]]                        kablo
[[AB-7000-0003]]                        stok
[[7000_MILKON]]                         proje
[[JIG-1000-0009|SATIMO plakası]]        görünen ad farklı
[[AB-7000-0003#Açık işler]]             notun bir başlığına
```

- Yazarken **@** ya da **[[** yaz → jig, kablo, stok, proje, kişi listesi açılır. Ok tuşlarıyla seç, **Enter**.
- Görünüm'de bağlantıya tıklayınca Pusula ana pencerede o kaydı seçer.
- **Olmayan** bir şeye `[[…]]` verme: Pusula yeni not oluşturmaz, bağlantı boşa düşer (Obsidian ise boş not açar).
- Kaydın numarasını bağlantısız düz yazmak da yeter (`JIG-1000-0009 ile ölçüldü`): **Bahsedildiği yerler** kartı
  onu da bulur. Bağlantı ise tıklanabilir.

### Kişi

```md
@Ahmet ile konuşuldu.
```

### Web bağlantısı

```md
[Markdig belgesi](https://github.com/xoofx/markdig)
<https://example.com>
```

Pusula web bağlantısını tarayıcıda açar; sayfayı kendisi indirmez.

---

## 7. Resim

```md
![[foto_01.png]]                    adla (kök altında tek ise klasör gerekmez)
![[foto_01.png|360]]                360 piksel genişlik
![[Ekler/yapistirilan_20261110_0915.png]]
![Açıklama](Foto/foto_02.jpg)       standart md biçimi
```

Pusula'da resim eklemenin üç yolu:

1. Araç çubuğundaki **resim** butonu → dosya seç.
2. Ekran görüntüsünü kopyala, notta **Ctrl+V** → `Ekler\yapistirilan_….png` olarak kaydedilir.
3. Dosyayı pencereye **sürükle-bırak**.

> [!info] Dosya nereye gider
> Kök klasörün **içindeki** bir resim yerinde kalır, bağlantısı yazılır. **Dışındaki** bir resim notun yanındaki
> `Ekler` klasörüne **kopyalanır**; aslına dokunulmaz. Web adresindeki resim gösterilmez (dış ağ yok), bağlantı olarak çıkar.

---

## 8. Tablo

Sütunlar `|` ile ayrılır, başlığın altında `---` satırı olur.

```md
| Seri no | Frekans (GHz) | Kazanç (dBi) | Sonuç |
| ------- | ------------: | -----------: | :---: |
| SN01    |          8,50 |         12,4 |  ✔    |
| SN02    |          8,50 |         11,9 |  ✘    |
```

Görünüşü:

| Seri no | Frekans (GHz) | Kazanç (dBi) | Sonuç |
| ------- | ------------: | -----------: | :---: |
| SN01    |          8,50 |         12,4 |  ✔    |
| SN02    |          8,50 |         11,9 |  ✘    |

- Hizalama ikinci satırda: `---` sola, `---:` sağa, `:---:` ortaya.
- Sütunların düz yazıda hizalı olması **gerekmez**; `|a|b|` da çalışır. Hizalamak yalnızca okumayı kolaylaştırır.
- Hücre içinde `|` yazmak için `\|`.
- Araç çubuğundaki **tablo** butonu boş bir tablo ekler.

---

## 9. Uyarı kutuları (callout)

Alıntı satırının ilkine `[!tür] Başlık` yazılır.

```md
> [!warning] Tork sınırı
> SMA konnektörde 0,9 Nm'yi geçme.

> [!danger] Yüksek güç
> Yükselteç açıkken antenin önünde durma.

> [!tip] Hızlı yol
> Kalibrasyon dosyası `_Sablonlar` altında.

> [!success] Kalifikasyon geçti
> 3/3 numune sınır içinde.

> [!question] Açık soru
> Yankısız oda mı SATIMO mu?

> [!note]
> Başlıksız kutu da olur.
```

> [!warning] Tork sınırı
> SMA konnektörde 0,9 Nm'yi geçme.

> [!success] Kalifikasyon geçti
> 3/3 numune sınır içinde.

Türler ve renkleri:

| Tür (eş anlamlıları)                         | Renk          |
| -------------------------------------------- | ------------- |
| `warning` (`caution`, `attention`)           | sarı / uyarı  |
| `danger` (`error`, `bug`, `failure`)         | kırmızı       |
| `success` (`check`, `done`)                  | yeşil         |
| `question` (`help`, `faq`)                   | soru          |
| `tip` (`hint`, `important`)                  | ipucu         |
| `quote` (`cite`)                             | alıntı        |
| `note`, `info` ve diğerleri                  | bilgi (mavi)  |

- `> [!warning]- Başlık` (tireli) Obsidian'da **kapalı** başlar.
- Araç çubuğundaki **kutu** butonu `> [!warning]` ekler.

---

## 10. Alıntı

```md
> Müşteri: "Teslim haftaya kalabilir."
> — Toplantı notu, 10.11.2026
```

Araç çubuğundaki **alıntı** butonu seçili satırların başına `> ` koyar (tekrar basınca kaldırır).

---

## 11. Kod ve ölçüm çıktısı

Satır içinde: `` `C:\PusulaDeneme\_Sablonlar` ``

Blok olarak üç ters tırnak (```` ``` ````) arasına; içi olduğu gibi gösterilir, md işaretleri çalışmaz:

~~~md
```
VNA: S11 = -18,2 dB @ 8,5 GHz
VNA: S11 = -16,9 dB @ 9,0 GHz
```
~~~

Dil adı yazılabilir (` ```powershell `); Pusula renklendirmez, Obsidian renklendirir.

---

## 12. Yorum (gizli not)

Düz yazıda görünür, **Görünüm'de gizlenir**. Kendine not, yarım fikir, "sonra bak" için.

```md
<!-- Bu bir yorumdur: Pusula, Obsidian ve VS Code görünümünde gizlenir -->

%% Obsidian yorumu: Pusula ve Obsidian'da gizlenir, VS Code'da görünür %%

%%
Birden çok satıra
yayılan yorum
%%
```

- Pusula'da **Ctrl+Shift+C** (ya da araç çubuğundaki yorum butonu) seçili metni `<!-- … -->` içine alır; tekrar basınca kaldırır.
- Doğru yazım **`<!--`** (önce `<`, sonra `!`, sonra iki tire). `<--!` yorum **değildir**, düz metin olarak görünür.
- Kod bloğundaki `%%` yorum sayılmaz.

---

## 13. Etiket

`#` ve **boşluksuz** kelime. Obsidian'da etikete göre arama yapılır; Pusula vurgu renginde gösterir.

```md
Kablo değişti #kablo #revizyon
- [ ] Tork anahtarını kalibrasyona gönder #gorev
```

Etiket içinde boşluk olmaz: `#açık iş` değil `#açık_iş` ya da `#acik-is`.

---

## 14. Dipnot

```md
Kazanç beklenenden düşük[^1].

[^1]: Kablo kaybı (0,4 dB) düşülmeden.
```

Dipnotlar notun sonunda numaralı listelenir.

---

## 15. Ayraç çizgisi

Kendi satırında üç tire:

```md
---
```

> [!warning] Notun en başında `---` YAML başlığıdır, ayraç değil (bkz. 16). Ayracı başlığın altına koy.

---

## 16. YAML başlığı (özellikler)

Notun **en başında**, iki `---` arasında `ad: değer` satırları. Pusula bu bloğu okur ve **kendisi yönetir**
(durum, tarih, alanlar). Görünüm'de üstte özellik kutusu olarak çıkar (Ayarlar'dan gizlenebilir).

```md
---
numara: JIG-1000-0009
ad: SATIMO plakası
durum: Kullanımda
tarih: 2026-11-10
etiketler: [jig, satimo]
---
```

Kurallar:

- İki nokta üst üsteden sonra **bir boşluk**: `durum: Kullanımda`.
- Değer `:` ya da `#` içeriyorsa tırnak: `not: "Saat 10:30'da"`.
- Tarih `yyyy-aa-gg` biçiminde.
- Liste: `[a, b]` ya da alt alta `- a`.
- Pusula'nın yazdığı alanları elle bozma; değiştireceksen Pusula'nın **Düzenle** penceresinden değiştir.

---

## 17. Pusula'ya özel kurallar

Bunlar md'nin değil Pusula'nın kuralları; ayrıntısı [YAPAY_ZEKA_REHBERI.md](YAPAY_ZEKA_REHBERI.md) içinde.

**Ana not** (`<Numara>/<Numara>.md`): YAML + bölümler.

```md
## Öğrenilenler
## Açık işler
- [ ] SN02 tekrar ölç #gorev
## Dokümanlar
## Fotoğraflar
## Günlük
```

**Günlük:** Pusula olayları `## Günlük` bölümünün **başına** ekler (en yeni üstte). Eski satırlara dokunmaz.
Elle olay yazacaksan aynı biçimde yaz:

```md
## Günlük

### 2026-11-10 · Konnektör SMA yerine N tipi
Tork sorunu yüzünden değişti. [[JT-0001-0003]] kablosu da yenilendi.
```

**Test koşusu** (`03_Test_Sonuclari/<Altyapı>/<SN>_T<n>/test.md`):

```md
---
tarih: 2026-11-08
analiz_tarihi: 2026-11-10
analiz_notu: Kablo kaybı düşüldü
sonuc: geçti
---
```

`analiz_tarihi` testle aynı günse yazılmaz (Pusula kendisi siler).

**Dosya ve klasör:** Pusula hiçbir dosyayı silmez. Notun **gövdesine** Pusula dokunmaz; yalnız YAML ve Günlük'ün başı.

---

## 18. Pusula not penceresi kısayolları

| Kısayol             | İş                                             |
| ------------------- | ---------------------------------------------- |
| Ctrl+1 / 2 / 3      | Düz / Yan yana / Görünüm                       |
| F11, Esc            | Tam ekran / çık                                |
| Ctrl+S              | Kaydet                                         |
| Ctrl+B / I / E      | Kalın / italik / kod                           |
| Ctrl+Shift+X        | Üstü çizili                                    |
| Ctrl+Shift+H        | Vurgu (`==…==`)                                |
| Ctrl+Shift+C        | Yorum (`<!-- … -->`)                           |
| Ctrl+H              | Başlık düzeyi döngüsü                          |
| Ctrl+L              | Görev kutusu                                   |
| Ctrl+K ya da @      | Bağlantı önerisi                               |
| Enter (listede)     | Yeni madde                                     |
| Tab / Shift+Tab     | Girinti artır / azalt                          |
| Ctrl+V (resim)      | Resmi `Ekler`e kaydet ve ekle                  |
| Ctrl+V (adres, seçim varken) | `[seçim](adres)` bağlantısı           |
| Ctrl + / −, Ctrl+tekerlek | Yazı boyutu                              |
| Ctrl+F / Ctrl+R     | Bul / Bul ve değiştir                          |
| F3, Shift+F3        | Sonraki / önceki sonuç                         |
| Ctrl+Shift+O        | Başlık paneli (içindekiler)                    |
| Alt+↑ / Alt+↓       | Satırı yukarı / aşağı taşı                     |
| Ctrl+D              | Satırı çoğalt                                  |
| Ctrl+Shift+K        | Satırı sil                                     |
| Ctrl+Shift+T        | Tabloyu hizala                                 |
| Tab (tabloda)       | Sonraki hücre (son hücrede yeni satır)         |
| Ctrl+Z / Ctrl+Y     | Geri al / yinele (her işlem tek adım)          |

Ayrıca: `(`, `[`, `{`, `"` ve `` ` `` yazınca kapanışı kendiliğinden gelir; seçili metinde `*`, `_`, `~`, `=` seçimi sarar
(Ayarlar → "Parantez ve işaretleri kendiliğinden kapat"). Yan yana görünümde iki taraf birlikte kayar (Ayarlar'dan kapatılabilir).

AltGr ile yazılan karakterler (`#`, `@`, `{`, `[`, `|`, `~` …) kısayol sayılmaz, normal yazılır.

---

## 19. Sık yapılan hatalar

| Yanlış                    | Doğru                     | Neden                                   |
| ------------------------- | ------------------------- | --------------------------------------- |
| `#Başlık`                 | `# Başlık`                | `#`'ten sonra boşluk gerekir            |
| `-Madde`                  | `- Madde`                 | `-`'den sonra boşluk gerekir            |
| `- []  iş`                | `- [ ] iş`                | Köşeli parantez içinde **bir boşluk**   |
| `<--! yorum -->`          | `<!-- yorum -->`          | Önce `<`, sonra `!`                     |
| `** kalın **`             | `**kalın**`               | İşaretle metin bitişik                  |
| Tablo başlığı altında satır yok | `\| --- \| --- \|` ekle | Ayraç satırı olmadan tablo olmaz   |
| Listeden hemen sonra paragraf | Araya boş satır       | Yoksa paragraf son maddeye yapışır      |
| `[[JIG 1000 0009]]`       | `[[JIG-1000-0009]]`       | Numara tam ve tireli yazılır            |
| Notun ortasında `---` ile YAML | YAML yalnız en başta | Ortadaki `---` ayraç çizgisidir         |

Bir işareti **işaret olarak değil düz karakter** olarak yazmak için önüne `\` koy: `\*yıldız\*`, `\#1`, `\[[`.

---

## 20. Tam örnek not

~~~md
---
numara: AB-7000-0003
ad: DA400 Döner Anten
durum: Kalifikasyonda
tarih: 2026-11-10
---

# DA400 Döner Anten

> [!warning] Teslim öncesi
> Tüm konnektörlerde tork kontrolü yapılacak (0,9 Nm).

## Öğrenilenler

- SATIMO'da **SN02** kazancı ~0,5 dB düşük çıktı; kablo kaybı düşülünce sınır içinde.
- [[JIG-1000-0009|SATIMO plakası]] ile ölçüm düzeni tekrarlanabilir.

## Açık işler

- [ ] SN02 tekrar ölç #gorev
- [x] Kablo kayıpları girildi
- [ ] @Ahmet ile rapor taslağını gözden geçir #gorev

## Ölçüm özeti

| Seri no | Frekans (GHz) | Kazanç (dBi) | Sonuç |
| ------- | ------------: | -----------: | :---: |
| SN01    |          8,50 |         12,4 |  ✔    |
| SN02    |          8,50 |         11,9 |  ✘    |

![[foto_01.png|360]]

<!-- SN03 gelince tabloya eklenecek -->

## Günlük

### 2026-11-10 · Analiz tamamlandı
Kablo kaybı düşüldü, sonuç [[JT-0001-0003]] ölçümüyle doğrulandı[^1].

[^1]: VNA ölçümü, 03_Test_Sonuclari/VNA/SNP1_T1.
~~~
