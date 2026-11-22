# Pusula veri dizini — yapay zeka rehberi

> Bu dosya **gerçek veri dizininin köküne** `CLAUDE.md` (ya da `AGENTS.md`) adıyla kopyalanır.
> O dizinde çalışan yapay zeka (Claude, Obsidian eklentisi vb.) önce bunu okur.
> Kaynağı: Pusula deposu `docs/YAPAY_ZEKA_REHBERI.md`. Uygulama değişince burası da güncellenir.

## Bu dizin nedir

Antenler/ürünler (stok), projeler, jigler ve kablolar için **dosya tabanlı** bir takip dizini.
Notlar Obsidian'da yazılır; **Pusula** adlı masaüstü uygulaması klasörleri tarar ve gösterir.
Amaç: Jira, Confluence, Excel, txt, docx, OneNote'a dağılmış bilgiyi tek yerde toplamak; sonra
"bu hafta / bu ay ne yaptım", "kaç stok hangi durumda" gibi raporları buradan çıkarmak.

**Temel ilke: klasör = yer, JSON = bilgi, md = anlatım.**

## Yapma

- **Dosya silme, taşıma, yeniden adlandırma yapma.** Gerekirse kullanıcıya öner, o yapsın. Göç gerekiyorsa **kopyala**.
- `_TAKIP\` ve `_PUSULA\` klasörlerindeki JSON dosyalarını **elle düzenleme** (uygulama yazar). Okuyabilirsin.
- Var olan bir notun **gövdesini yeniden yazma**; yalnızca kullanıcı isterse ekle. YAML'daki alan adlarını değiştirme.
- Numara uydurma (`AB-7000-0005` gibi). Numarayı kullanıcı verir.
- Dışarıya (bulut, internet) veri gönderme.

## Dizin yapısı (15.11.2026 düzeni)

```
<KÖK>\
├── _PUSULA\                 ← uygulama ayarları: sablon.json (klasör şablonları), turler.json (türler, alanlar, dosya türleri)
├── _TAKIP\                  ← kök listeleri: Altyapilar.md, Standartlar.md, Islemler.md (elle de eklenebilir)
├── _Sablonlar\              ← Obsidian şablonları
├── _SILINENLER\             ← Pusula'da "Sil" denenler (tarih\eski yol); silinenler.log. Geri almak = eski yerine taşımak
├── ENVANTER\                ← genel test yardımcı araçları (hiçbir projeye ait değil)
│   └── Kablolar\ Jigler\ Yazilimlar\ Test_Gerecleri\ Diger\  <NUMARA>\
└── 7000_MILKON\             ← proje (<projeNo>_<AD>)
    ├── _TAKIP\proje.json
    ├── 00_Proje_Geneli\Envanter\...   ← projeye ait araçlar
    └── AB-7000-0003\        ← stok (ürün / anten)
        ├── _TAKIP\stok.json
        ├── AB-7000-0003.md          ← stokun ana notu (klasörle aynı ad)
        ├── 01_Ortak\                ← spek, çizim, DM
        │   └── Fotograflar\         ← stokun fotoğrafları
        ├── 02_Test_Araclari\        ← bu stoka ait araçlar
        │   └── Kablo\ Jig\ Yazilim\ Cihaz\ Diger\  <NUMARA>\
        ├── 03_Testler\
        │   ├── AB-7000-0003_Test_Gereksinimleri.md   ← gereksinim · plan · rapor tabloları
        │   ├── 01_Islevsel\ 02_Ozellik\ 03_CK_EMC\ 04_Diger\
        │   └── Olcumler\<tarih>_<madde>_<başlık>\    ← ölçüm kaydıyla açılan boş klasör
        ├── Diger\                   ← kayıtlara eklenen mail (.msg) kopyaları
        └── _ARSIV\                  ← eski dosyaların yedekleri (.md.yedek)
```

- `_` ile başlayan klasörler taranmaz (arşiv, takip, ayar).
- Kayıt klasörünün adı **yalnız numara** (`JT-7000-0101`); tanım `kayit.json`'da ve notta durur.
- Numara biçimleri `_PUSULA\turler.json` içinde: stok `@@-####-####`, geliştirme ve yazılım `JY-####-####`,
  test yardımcı araçları `JT-####-####` (eski `JIG-`, `TG-` numaraları geçerli kalır). Yazılım revizyon eki `_AA`, `_AB` ….
- Eski stoklarda `01_Dokumanlar`, `03_Test_Sonuclari`, `05_Jigler`, `05_Kablolar`, `07_Fotograflar`, `Notlar\` okunmaya
  devam eder; yeni dosyaları yeni düzene koy, eskileri taşıma.

### Test yardımcı aracı klasörü

```
JT-7000-0101\
├── _TAKIP\kayit.json      ← tür, numara, tanım, alt tür, alanlar, durum olayları (uygulama yazar)
├── JT-7000-0101.md        ← aracın notu
├── JT-7000-0101_kapak.jpg ← kapak fotoğrafı
├── 1_SMA_ucu.jpg          ← şerit 1 (sayfada "SMA ucu"), 2_…, 3_… sayı sırasıyla
├── IMG_2041.jpg           ← numarasız: sayfada görünmez
└── 01_Belgeler\           ← türe göre (jig: 01_Cizimler, 02_Belgeler; yazılım: 01_Kaynak, 02_Belgeler)
```

Fotoğraf adları stokta da aynı (`01_Ortak\Fotograflar\`): kapak `<numara>_kapak.*`, şerit `1_ad.*`, `2_ad.*` ….
Ayrıntı: [ADLANDIRMA_VE_KULLANIM.md](ADLANDIRMA_VE_KULLANIM.md).

## md kuralları

### 1. Ana not (stok / araç)

Adı klasör numarasıyla aynı: `AB-7000-0003.md`, `JT-7000-0101.md`. YAML'da yalnızca sade alanlar:

```yaml
---
stok: AB-7000-0003        # araçta: tur: cable, numara: JT-7000-0101, tanim: …, alt_tur: RF
ad: DA400 Döner Anten
proje: 7000_MILKON
test_gereksinimleri: "[[AB-7000-0003_Test_Gereksinimleri]]"
tags: [anten, SATIMO]     # Obsidian etiketleri; # yazılmaz, boşluk yerine _
---
```

Gövde serbest (aracın notunun gövdesi Pusula'da salt okunur gösterilir). Önerilen başlıklar: Özet · Kullanım ·
Kararlar · Öğrenilenler · `## Yapılacaklar` (`- [ ] …`). Bağlantılar Obsidian biçiminde: `[[JT-7000-0101]]`.

### 2. Olaylar: notun en altındaki `## Günlük`

Olaylar ayrı dosya değil, ana notun en altındaki `## Günlük` bölümüne yazılır, **en yeni üstte**. Pusula yalnız bu
bölümün başına ekler. Biçim:

```markdown
## Günlük

### 12.11.2026 · Kalibrasyon · Faz / kayıp doğrulaması: geçti
kişi: Metroloji lab.

Sertifika K-2026-114, geçerlilik 12.11.2027.

#### Ayrıntı
Uzun açıklama (kartta görünmez, notta okunur).
```

- Başlık satırı: `### gg.aa.yyyy[ ss:dd] · Tür · Başlık`. Türler: Ölçüm, Toplantı, Karar, Talep, Teslim, Sevkiyat,
  Revizyon, Kalibrasyon, Tamir / bakım, Sorun, Çözüm, Hazırlık, Not; eklenenler `_TAKIP\Islemler.md`'de.
- Başlığın altındaki satır `·` ile ayrılmış bilgiler: `kişi:`, `önem:`, `madde: TG-1` ve `sonuç: Geçti` (ölçüm),
  `klasör:` (ölçüm klasörü), `katılımcı:` / `ortam:` (toplantı), `karar veren:`, `talep:` / `durum:` / `istenen:`,
  `rev: AB → AC` (revizyon), `mail: Diger/….msg`.
- Stoka ait aracın Günlük olayları stokun zaman çizelgesinde de görünür.
- Yazılımın revizyonları Revizyon olaylarıdır (`rev: AB → AC`, açıklama = eklenenler).
- Eski stoklardaki `Notlar\*.md` olay notları (YAML'da `olay:` ve `tarih:`) okunmaya devam eder; yenisini Günlük'e yaz.

### 3. Doğrulama dosyası

`03_Testler\<stok>_Test_Gereksinimleri.md`:

- İlk tablo **gereksinimler** (TG no, başlık, grup, yöntem, durum …).
- `## Plan` altındaki ilk tablo **test planı** maddeleri (TP no, gereksinimlere bağ), `## Rapor` altındaki ilk tablo
  **rapor** maddeleri (TR no, plana bağ, tarih, birim, sonuç).
- Plan / rapor maddesinin ayrıntısı aynı dosyada `### <madde no> · başlık` altına elle yazılır.
- Tablo satırlarını uygulama ekler / değiştirir; elle düzenlerken sütun başlıklarını değiştirme.

Ölçüm (deneme) kaydı Günlük'te Ölçüm olayıdır (`madde: TG-1 · sonuç: Kaldı`); ölçüm dosyaları kaydın açtığı
`03_Testler\Olcumler\…` klasörüne konur. Eski düzendeki `03_Test_Sonuclari\<SİSTEM>\<SN>_T<tur>\test.md` koşuları okunur.

### Bağlantı, resim, kutu (Pusula'nın md penceresi ve "Bahsedildiği yerler" bunları okur)

- Kayda bağlantı: `[[JIG-1000-0009]]`, görünen adla `[[JIG-1000-0009|SATIMO plakası]]`, stok `[[AB-7000-0003]]`, proje `[[7000_MILKON]]`.
  Düz yazılmış numara da (`JIG-1000-0009 ile ölçüldü`) bahis sayılır; kod bloğu içindeki sayılmaz.
- Kişi: `@Ad Soyad` (ayrı kişi notu açma).
- Resim: `![[foto_01.png]]`, genişlikle `![[foto_01.png|360]]`, klasörle `![[Ekler/yapistirilan_….png]]`. Ad tek ise klasör gerekmez.
  Web adresindeki resim gösterilmez (dış ağ çağrısı yok).
- Etiket: notta `#faz_kararli` ya da YAML'da `tags: [faz_kararli, RS422]`. Etiket listesi ve hangi türde geçerli oldukları Pusula'da Türler → Etiketler'de; listede olmayan etiket de yazılabilir.
- Kutu: `> [!warning] Başlık` (warning, danger, tip, success, question, note, quote). Vurgu `==metin==`, üstü çizili `~~metin~~`.
- Olmayan bir nota `[[…]]` verme: Pusula yeni not oluşturmaz, bağlantı boşa düşer.
- Yorum: `<!-- … -->` (her yerde gizlenir) ya da `%% … %%` (Obsidian / Pusula). Bütün md yazım kuralları, örnekli: [MD_KILAVUZU.md](MD_KILAVUZU.md).

### 4. Kısa not

Uygulamadan yazılan tek satırlık notlar (`stok.json`/`proje.json`/`kayit.json` → `notes`). md değildir; elle eklenmez.

## Göç (eski kaynaklardan aktarma) önerisi

1. Önce Pusula'dan proje ve stok **iskeletini** oluştur (numaralar, şablon klasörleri, `_TAKIP`).
2. Belgeleri ilgili alt klasöre **kopyala** (spek / DM → `01_Ortak`, ölçüm → `03_Testler\…`).
3. Jira/Confluence/OneNote'taki toplantı, karar, arıza kayıtlarını ana notun `## Günlük` bölümüne **tarihli olay**
   olarak ekle (en yeni üstte).
4. Gereksinim listeleri Pusula'da "Excel'den al" ile alınır; Excel'deki stok / araç listelerini kayıtlara dönüştürmeyi
   kullanıcıyla birlikte yap; numarayı kullanıcı onaylar.
5. Kaynağı belirt: olayın açıklamasına `Kaynak: Jira PRJ-123` gibi bir satır.

## Hata / eksik bildirimi (Pusula geliştirmesi için)

Uygulamada hata, eksik ya da istek görülünce `_PUSULA\GERI_BILDIRIM.md` dosyasına **ekle** (silme):

```markdown
## 2026-11-08 · hata | eksik | istek
- Nerede: Stok sayfası → Günlük
- Ne oldu: …
- Beklenen: …
- Örnek klasör: 7000_MILKON\AB-7000-0002
```

Bu liste geliştirme oturumunda Pusula deposuna taşınır.

## İleride gelecekler

- Alt / üst stok (`stok.json` → `ust: <numara>`; klasörler iç içe değil)
- Haftalık / aylık rapor (olaylar, testler, durum değişiklikleri)
