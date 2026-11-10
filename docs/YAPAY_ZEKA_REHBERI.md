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

## Dizin yapısı

```
<KÖK>\
├── _PUSULA\                 ← uygulama ayarları: sablon.json (klasör şablonları), turler.json (jig/kablo türleri, dosya türleri)
├── _Sablonlar\              ← Obsidian şablonları: Olay.md, Test.md, BENI_OKU.md
├── _SILINENLER\            ← Pusula'da "Sil" denenler (tarih\eski yol); silinenler.log. Geri almak = eski yerine taşımak
├── ENVANTER\                ← genel envanter (hiçbir projeye ait değil)
│   ├── Jigler\JIG-1000-0009_SATIMO_adaptor_plakasi\
│   └── Kablolar\JT-0001-0001_SATIMO_RF_kablosu\
└── 7000_MILKON\             ← proje (<projeNo>_<AD>)
    ├── _TAKIP\proje.json
    ├── 00_Proje_Geneli\Envanter\Jigler|Kablolar\...   ← projeye ait envanter
    └── AB-7000-0002\        ← stok (ürün / anten)
        ├── _TAKIP\stok.json
        ├── AB-7000-0002.md          ← stokun ana notu (klasörle aynı ad)
        ├── 01_Dokumanlar\           ← ATP, prosedür, docx/pdf
        ├── 02_Test_Planlari\
        ├── 03_Test_Sonuclari\<SİSTEM>\<SN>_T<tur>\test.md   (+ Ham\, Foto\, Analiz\)
        ├── 04_Kalifikasyon\
        ├── 05_Jigler\  05_Kablolar\  ← bu stoka özel jig/kablolar
        ├── 06_Yazilimlar\
        ├── 07_Fotograflar\          ← adında "kapak" geçen foto kapak olur
        ├── Notlar\                  ← olay notları
        └── _ARSIV\
```

- `_` ile başlayan klasörler taranmaz (arşiv, takip, ayar).
- Jig/kablo kaydı klasörü: `<NUMARA>_<kısa_ad>\` + `_TAKIP\kayit.json` + `<NUMARA>.md` + alt klasörler
  (`01_Cizimler`, `02_Belgeler`, `03_Fotograflar` …). Jiglerin geçmişindeki notlar da `Notlar\` altında durur.
- Numara biçimleri `_PUSULA\turler.json` içinde (ör. `JIG-####-####`, `JT-####-####`, `JX-…`).

## md kuralları

### 1. Ana not (stok / jig / kablo)

Adı klasör numarasıyla aynı: `AB-7000-0002.md`, `JIG-1000-0009.md`. YAML'da yalnızca sade alanlar:

```yaml
---
stok: AB-7000-0002        # jig/kabloda: tur: jig, numara: JIG-1000-0009
ad: TAKIM ANTEN BOYNUZ Y
proje: 7000_MILKON
urun: SH2000
frekans: 2–32 GHz
konnektor: N(F)
sorumlu: katan
tags: [anten, boynuz, SATIMO]   # Obsidian etiketleri; # yazılmaz, boşluk yerine _
---
```

Gövde serbest. Önerilen başlıklar: Özet · Teknik özellikler · Seri numaraları · Test geçmişi · Kararlar ·
Öğrenilenler · Açık işler (`- [ ] … #gorev`) · Dokümanlar · Fotoğraflar. Bağlantılar Obsidian biçiminde: `[[JIG-1000-0009]]`.

### 2. Olay notu (Günlük / Geçmiş'e düşer)

YAML'ında **`olay:` ve `tarih:`** olan her `.md` olaydır. Stokta "Günlük"e, jig/kabloda "Geçmiş"e kendiliğinden girer.
Dosya adı: `Notlar\YYYY-MM-DD_kisa_baslik.md`.

```yaml
---
olay: karar               # toplanti, karar, numune, sevkiyat, revizyon, ariza, not (başkası da olur)
tarih: 2026-10-22         # ya da 2026-10-22 14:30
kisi: katan               # isteğe bağlı
---
# Konnektör SMA yerine N tipi
Kısa açıklama…
```

Başlık = ilk `# ` satırı. `olay: test` yazılmaz (testler aşağıdaki biçimdedir).

### 3. Test koşusu

**Seri numarası:** ayrı kayıt yok, klasör adından okunur. `SNP1`, `SNP2` = prototip; `SN01`, `SN02` = üretim birimi. Her SN'nin her turu kendi klasöründe: `SATIMO\SNP1_T1`, `SATIMO\SN01_T2`, `Yankisiz\SN01_T1`. Aynı gün birden fazla SN ölçülse de her biri ayrı klasör. `SATIMO\SNP1\T1` gibi iç içe klasör açma.

`03_Test_Sonuclari\<SİSTEM>\<SN>_T<tur>\test.md` (ör. `SATIMO\SNP1_T1\test.md`):

```yaml
---
stok: AB-7000-0002
sistem: SATIMO            # VNA, SATIMO, CK …
sn: SNP1
tur: 1
tarih: 2026-10-20
sonuc: Kaldı              # Geçti / Kaldı / Devam (başka değer "bilinmiyor" sayılır)
kisi: a-demir
plan: AB-7000-0002 SATIMO Hüzme ve Yan Lob
jig: JIG-1000-0009
analiz_tarihi: 2026-10-23 # yalnız analiz test gününden SONRA yapıldıysa; yoksa yazma (test tarihi sayılır)
analiz_notu: Yan lob yeniden hesaplandı   # isteğe bağlı, tek satır
---
```

Ham veriler `Ham\`, fotolar `Foto\`, analiz çıktıları `Analiz\` altına.

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
2. Belgeleri ilgili alt klasöre **kopyala** (ATP → `01_Dokumanlar`, ölçüm → `03_Test_Sonuclari\…`).
3. Jira/Confluence/OneNote'taki toplantı, karar, arıza kayıtlarını **tarihli olay notlarına** çevir (her biri ayrı `.md`).
4. Excel'deki stok/jig listelerini kayıtlara dönüştürmeyi kullanıcıyla birlikte yap; numarayı kullanıcı onaylar.
5. Kaynağı belirt: notun sonuna `Kaynak: Jira PRJ-123` gibi bir satır.

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
- Haftalık / aylık rapor (olaylar, testler, durum değişiklikleri, stok sayıları)
- Kalifikasyon: TG / TR belgeleri ve test koşularına bağlantı
- Yazılım, Diğer, Arşiv sekmeleri
