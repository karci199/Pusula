# Pusula — Stok / Proje Takip Uygulaması: Tasarım ve Uygulama Planı (v3, birleştirilmiş)

> Claude Code için başlangıç dokümanı. Ad: **Pusula** (repo, klasör ve namespace).
> v3, önceki planı ve sonraki tüm konuşmayı tek dosyada toplar: şablon üretimi, özel/arşiv klasörü, göç aracı, kayıt türleri, kişiler, günlük/sorun/ders/görev, Obsidian ile iş bölümü, doğrulama ve izlenebilirlik (TG/TP/TR), rapor ve yazdırma, blok şemalar.
> Yanındaki PNG'ler **düzen fikri** içindir, birebir tasarım değildir. İçlerindeki tüm veriler örnektir.
> Kodlamaya başlamadan önce "Açık Sorular" bölümünü kullanıcıyla tek mesajda netleştir.

## 0. Kararlar (07.11.2026)

Bu bölüm planın geri kalanından önce gelir; çelişki olursa burası geçerlidir.

- **Ad:** Pusula (repo, klasör, namespace). Sıfırdan yazılır; başka bir ürünle bağlantısı yoktur.
- **Framework:** .NET Framework 4.8 + WPF, x64. Arayüz bileşenleri ve tema **DesignKit.Wpf** kütüphanesinden gelir (derlenmiş DLL `Dependencies/DesignKit/` altında).
- **Konum:** veri genelde yerel diskte; kök klasör ağ paylaşımında da olabilir.
- **Klasör = yer, JSON = bilgi.** Gerçek klasör ağacı JSON'da tutulmaz, her seferinde diskten taranır. Şablon yalnızca "yeni stok açınca hangi klasörler oluşsun" tarifidir. Klasörden çıkarılamayan tanımlar (kişi, jig, durum, not, bağ) JSON'da durur.
- **Ana alanlar:** kökün altındaki her ana klasör bir alandır (Projeler, Envanter, Rehberler, Fotoğraflar, Ortak, Arşiv). Alan türü ayar dosyasından okunur; tanımsız klasör "Genel" sayılır ve yine listelenir. Projeler ana eksendir ama zorunlu değildir.
- **Adlandırma örneği:** proje klasörü `<projeNo>_<AD>` (`7000_MILKON`), altında stok klasörü `AB-7000-0000`. Stok numarasının ortası proje numarasıyla uyuşmazsa uyarı verilmez, stok **"genel stok"** olarak etiketlenir.
- **Klasör açma:** klasörler elle de açılabilir, uygulama onları da tanır; tercih edilen yol "Şablondan oluştur"dur.
- **Arayüz:** sol tarafta ikonlu ağaç; sağ panel seçilen düğümün türüne göre değişir (proje → stok listesi, stok → stok bilgisi). Her kayıt türünün kendi "Yeni ..." komutu olur (proje, stok, jig, kişi). Türler bir temel türü genişleten alt türler gibi tasarlanır.
- **Sıra:** arayüz erken ve ince dilimlerle gelir: (1) pencere + ağaç, (2) proje/stok tanıma, (3) seçilen düğümün özeti, (4) şablondan oluştur. Kişi, jig, test ve izlenebilirlik sonra, birer birer. Aşağıdaki faz sırası buna göre uyarlanır.

---

**Mock dosyaları (planla birlikte oku):**

| Dosya | Gösterdiği |
|---|---|
| `mock/Genel_Sayfa.png` | Açılış sayfası: sol ağaç, KPI'lar, proje tablosu, görev panosu, uyarılar |
| `mock/Ornek_Arayuz.png` | İlk stok detay örneği (test, jig, yazılım, kalifikasyon, notlar) |
| `mock/SH2000_Stok_Karti.png` | Stok kartı: tanım, kişiler, özellikler, jig, zaman çizelgesi, Obsidian notu, test analizi |
| `mock/Izlenebilirlik_3Sutun.png` | TG → doğrulama kayıtları → sonuç ve kanıt, turlar, limit önizlemesi |
| `mock/Dogrulama_Matrisi.png` | Gereksinim × TG/TP/TR × test sonucu matrisi, maske ve karar |
| `mock/Kisi_Gunluk_Sorun.png` | Kişiler, günlük, sorunlar; ortak bilgi bankası (dersler) |
| `mock/Obsidian_Bolusumu.png` | Obsidian (yazma) ve uygulama (takip) iş bölümü |
| `mock/Kayit_Turleri.png` | Kayıt türleri ve alt türleri |

---

## 1. Amaç

Küçük, yerel (offline) çalışan bir masaüstü uygulaması. Kullanıcı bir anten/RF test mühendisi (Aselsan); stok numaralı ürünlerle çalışıyor. İlham kaynakları: **Obsidian** (dosya-öncelikli, not yazma), **SAP** (stok, ürün yapısı, kalite, değişiklik), **DOORS** (gereksinim izlenebilirliği). Ambar miktarı/stok adedi takibi **kapsam dışı**: "stok" = stok numaralı ürün/öğe.

**Temel iş bölümü:** notlar **Obsidian'da yazılır**, bu uygulama **takip içindir**. Uygulama notları gösterir (salt okunur render), sayıları, durumu, ilişkileri ve geçmişi tutar.

Uygulama tek ekrandan şunları cevaplamalı:

- Kaç proje, kaç stok var? Her stokun durumu, bekleneni, sahibi ne?
- Hangi stok hangisinin alt/üst/aile üyesi?
- Testler yapıldı mı, kaçıncı turda, hangi altyapıda, hangi seri numarasıyla, kim yaptı, geçti mi? Test geçme oranı ve tur ilerlemesi (P1/P2/P3) stok → proje → tüm kök seviyesinde.
- Gereksinimler nasıl doğrulandı (test, analiz, inceleme, bildirimle geçiş, iptal)? (opsiyonel)
- Kalifikasyon sonuçları nerede?
- Jig, kablo, üretim/test yardımcı araçları ve cihazlar: kim yaptı, ne zaman yapıldı/geldi, durumu, kalibrasyonu.
- Yazılım sürümleri (git).
- Kişiler (tasarımcı, jig yapımcısı, test sorumlusu) ve hangi işlerle bağlantılı olduğu.
- Günlük, yaşanan sorunlar, çıkarılan dersler (ortak bilgi bankası), yapılacak işler.
- Fotoğraflar ve dosyalar: tıkla, önizle, aç, klasörde göster.
- Her klasörde en son hangi dosya var, kaç MB, ne zaman düzenlendi?
- Yerel yapay zeka ile doğal dil sorgulama ve analiz yorumu.
- Yazdırılabilir özet rapor.

Ek olarak: **stok şablonu oluştur** boş klasörleri ve gerekli bağları üretir; kullanıcı sonra doldurur. Her şey her zaman doldurulmaz, ama **her şey belirli bir düzende** durur.

Bugünkü hedef kullanıcı tek kişi (kendi çalışma düzeni). Sonra ekibe, sonra şirkete yayılacak: şablonlar, türler, durum listeleri, isimlendirme **ayarlanabilir** olsun, kodda sabit yazma.

**Tetikleyici ihtiyaç:** ekip değişti, veriler hâlâ **eski ekip dizininde**. Yeni düzene aktarım için bir **göç aracı** ilk sürümün asıl değeri olabilir (bkz. 4.10).

## 2. Bağlam (kısıtlar)

- Savunma sanayi ortamı: **bulut yok, telemetri yok, dış ağ çağrısı yok.** Yapay zeka yalnızca yerel (loopback).
- Windows, **.NET Framework 4.8 + WPF, x64**. Arayüz bileşenleri ve tema: **DesignKit.Wpf**.
- Asıl ürün yapısı (BOM, revizyon, onay) SAP ve Teamcenter'de, gereksinimler DOORS'ta durur. Bu uygulama bunların **yerine geçmez**. Şimdilik **yalnızca ilham alınır**; tam entegrasyon hedef değil. Gereksinim tarafında DOORS Excel dışa aktarımından salt okunur içe aktarma (Faz 8).
- Şirkete özgü ihtiyaçlar olabilir (örn. DOORS'ta TG/TP/TR yapısı); ayarlanabilir eşlemelerle karşılanmalı.
- Jira ve Confluence şirkette kullanılıyor; çok ilerisi için düşünülür (Faz 11), şimdi kapsam dışı.

## 3. Araştırma Özeti

- Birebir eşleşen hazır araç yok. Referanslar: **eLabFTW** (kayıt + ek + bağlantı, kavramsal en yakın), **Obsidian + Dataview** (dosya-öncelikli kanıtı), **OpenTMI**, **NI SystemLink**, **Kiwi TCMS**, CDOT/WSDOT dizin standartları ve otomatik proje klasörü üretici fikri, Skyworks TDM (parça numarası hiyerarşisi).
- Yerel veri deposu: **SQLite** (`Microsoft.Data.Sqlite`) + **FTS5** arama.
- Yerel yapay zeka (.NET): `Microsoft.Extensions.AI` `IChatClient` + **OllamaSharp**. `Microsoft.Extensions.AI.Ollama` **kullanımdan kaldırıldı**, kullanma. Küçük 7B modeller çoğu zaman araç çağırmaz; araç kullanımı için `llama3.2` veya `qwen3:4b` dene.
- Mimari ilke: **karar kodla, öneri yapay zekayla, onay kullanıcıyla.** Sayı, tarih, liste, geçti/kaldı deterministik koddan gelir.

### SAP işlem kodlarından alınan fikirler

| SAP | Bizdeki karşılığı |
|---|---|
| CC04 / CS03 / CS12 | Ürün ağacı (üst/alt/aile) görünümü |
| CS15 / CS15M (kullanım yeri) | "Bu jig/alt stok nerede kullanılıyor?" ters sorgusu |
| CS14 (karşılaştırma) | İki stok/varyant arası fark ekranı |
| MMBE / MB52 / MB51 | Stok kartı, stok listesi, olay günlüğü (miktar değil durum) |
| QM01 / QM02 / IQS12 / IQS9 | Sorun ve aksiyon kaydı, görev listesi |
| CC01–CC05 (değişiklik) | Değişiklik kaydı (ne, neden, hangi stoklar etkilenir) |
| QP01 / QA01 / QE51N / QA11 | Test planı şablonu → test koşusu → sonuç → **kullanım kararı** |

## 4. Temel Tasarım Kararları

### 4.1 Gerçeğin kaynağı: dosyalar. SQLite yalnızca türetilmiş indeks.

- Elle girilen her şey **ilgili klasördeki dosyalarda** durur. Klasörü kopyalayan veriyi de kopyalar.
- SQLite **silinip yeniden taranarak üretilebilir** (`rebuild index`).
- Veritabanı **yerel diskte** (`%LOCALAPPDATA%\Pusula\index.db`), ağ paylaşımında değil.
- Tek yazıcı varsayımı; atomik yazım (geçici dosya + yeniden adlandırma), çakışma algılama (mtime/versiyon).
- Dosyadan çıkarılabilen her şey (stok sayısı, yüzdeler, son değişiklik) **taramadan** gelir; elle girilen yalnızca durum, karar, görev, not ve bağlardır.

### 4.2 Artımlı tarama

- Meta veri: yol, boyut, mtime, uzantı, ayrıştırma sonucu. Değişiklik algılama boyut + mtime; hash isteğe bağlı.
- `FileSystemWatcher` ağ paylaşımında güvenilmez: **periyodik tarama + elle "Yenile"** ana yöntem.
- Arka planda tarama (`IProgress<T>`, `CancellationToken`); arayüz donmaz.

### 4.3 İsimlendirme sözleşmeleri (ayrıştırıcılar toleranslı)

- **Stok no:** `XX-XXXX-XXXX` veya `XXXX-XXXX-XXXX`. Örnek: `AB-7000-0001`.
- **Doküman:** `<stok>_<3 hane kod>_<2 harf rev>.<uzantı>`. Örnek: `AB-7000-0001_101_AA.docx`.
- **Ölçüm dosyası:** `YYYYMMDD_<stok>_SN<nn>_P<n>_<SISTEM>_<aciklama>_v<NN>.<uzantı>`
  Örnek: `20261107_AB-7000-0001_SN02_P2_SATIMO_pattern_v01.csv`
  - `P1/P2/P3` ölçüm/test aşaması **dosya adında ve günlükte**, klasörde değil.
  - `SISTEM`: `VNA`, `SATIMO`, `YANKISIZ`, `CK` vb. (ayarlanabilir).
- Ayrıştırılamayan dosya hata değil, **"uyumsuz dosya"** listesine düşer.
- Yol uzunluğu: toplam **>200 uyarı, 260'a yaklaşırsa hata**.
- Test kayıtları için ileride bir standart getirilebilir; **şimdilik esnek**, elle de olabilir.

### 4.4 Klasör iskeleti, şablon komutu ve özel klasörler

```
01_PROJELER\
├── 7107_SATCOM\
│   ├── 00_Proje_Geneli\              ← projeye ortak (şartname, toplantı, ortak raporlar)
│   ├── _ARSIV\                       ← proje düzeyi özel/arşiv
│   └── AB-7000-0001\                 ← stok (aile/üst/alt stoklar yan yana)
│       ├── _TAKIP\                   ← uygulama dosyaları
│       │   ├── stok.json
│       │   ├── gunluk.jsonl
│       │   ├── sorunlar.jsonl
│       │   ├── gorevler.jsonl
│       │   ├── dogrulama.json        ← opsiyonel (4.7)
│       │   └── AB-7000-0001.md       ← Obsidian stok kartı (4.8)
│       ├── 01_Dokumanlar\
│       ├── 02_Test_Planlari\
│       ├── 03_Test_Sonuclari\
│       │   ├── VNA_S11\
│       │   ├── SATIMO\SN01\{Ham, Analiz}\
│       │   ├── YANKISIZ\
│       │   └── CK\
│       ├── 04_Fotograflar\SATIMO\
│       ├── 05_Jig\                   ← yalnızca bu stoğa özel
│       ├── 06_Yazilimlar\
│       ├── 07_Kalifikasyon\
│       └── _ARSIV\                   ← stok düzeyi özel/arşiv
├── XX-4410-0007\                     ← tek stoklu: proje klasörü açılmadı (4.6)
00_ENVANTER\
├── Jigler\JIG-XXXX-XXXX_Ad\          ← ortak jig (jig.json)
├── Kablolar\
└── Yardimci_Araclar\
00_ORTAK\
├── kisiler.json                      ← kişi sözlüğü (4.5)
└── bilgi_bankasi\                    ← dersler (D-001.md ...)
```

**"Stok şablonu oluştur":**
- `template.json` ile tanımlı (birden fazla şablon: `Boynuz-Anten-v1`, `Tek-Stok-Minimal` vb.): klasör listesi, başlangıç dosyaları, isteğe bağlı bağlar, **zorunlu işaretli öğeler**.
- Mevcut klasörü **ezmez**, eksikleri tamamlar. **Kuru çalıştırma** önizlemesi gösterir, sonra onaylatır.
- Boş klasör "eksik" değil "boş" sayılır; "eksik" yalnızca şablonda **zorunlu** işaretlenenler için.
- Klasör adları Türkçe karaktersiz, numara önekli; şablondan okunur.

**Özel klasörler (her düzeyde):**
- `_` önekli klasörler (`_ARSIV`, `_SERBEST`, `_TAKIP`) **düzen kurallarının dışındadır**: isimlendirme/tamamlanma kontrolüne girmez, uyumsuz dosya uyarısı üretmez.
- Kullanıcı herhangi bir düzeyde `_ARSIV` veya `_SERBEST` açabilir. İçeriği taranır ve aranabilir, ama panolarda varsayılan olarak gizli ve "güncel" sayılmaz; sayılara girmez.
- Önek listesi ve davranış ayarlanabilir.

### 4.5 Kayıt türleri ve alt türleri (`mock/Kayit_Turleri.png`)

Her tür bir **tür tanımı** (`type.json`) ile tanımlanır; ekip/şirket kendi tür ve alt türünü ekler.

| Tür | Alt türler (başlangıç önerisi) | Türe özel alanlar |
|---|---|---|
| Proje | Çok stoklu, Tek stoklu (klasörsüz), Ortak/Genel | proje no, müşteri, dönem |
| Stok | Sistem, Anten, Besleme ağı, Radom, Alt montaj; rol: aile ana / varyant / alt-üst | SN listesi, aile, varyant farkı, özellikler (specs) |
| Jig | Mekanik, Elektriksel, Montaj, Ölçüm adaptörü | kapsam (özel/ortak), kalibrasyon, jig rev, yapan, üretim tarihi, geliş tarihi |
| Kablo ve yardımcı araç | RF kablo, Güç/veri kablosu, Üretim aracı, Test aracı, Ölçüm cihazı | uzunluk/bant, kalibrasyon bitişi, kullanım sayısı |
| Yazılım | Analiz aracı, Test betiği, Firmware, Kütüphane | git yolu, sürüm, commit, sabitlenen sürüm |
| Doküman | Test planı, Test raporu, Kalifikasyon, Şartname, Çizim, Prosedür | doküman kodu (3 hane), rev (2 harf) |
| Test | Tur: P1/P2/P3; altyapı: VNA S11, SATIMO, Yankısız oda, ÇK/EMC | SN, tur, altyapı, sonuç, kullanım kararı, düzenek (jig/kablo/cihaz) |
| Medya / Not / Gereksinim | Fotoğraf, Obsidian notu, TG, TP, TR | çekim SN/tur, etiket, PUID + sürüm |
| **Kişi** | rol bağlantısıyla: anten tasarımcısı, jig yapımcısı, test sorumlusu | ad, birim, roller (4.5.1) |
| Sorun | kapsam: stok / proje / genel | şiddet, sahip, kök neden, çözüm, bağlı stok/jig/test, durum |
| Ders | süreç, jig, kablo, yazılım, SATIMO, iyileştirme önerisi | kaynak sorunlar, durum (öneri/uygulandı/açık) |
| Görev | — | sahip, termin, durum (yapılacak/devam/bekliyor/bitti), bağlı kayıt |

Ortak alanlar (her türde): numara, ad, revizyon, durum, sahip, notlar, bağlantılar, geçmiş.

**Bağlantı türleri:** `üst–alt` stok · `aile–varyant` · `kullanır` (stok → jig/kablo/yazılım) · `doğrular` (doğrulama kaydı → gereksinim) · `belgeler` (doküman → stok) · `etkiler` (jig/rev değişimi → sonuçlar) · `rol` (kişi → stok/jig/test).

#### 4.5.1 Kişiler

- Kişi **gömülü alan değil**, ayrı kayıttır; tüm kök için tek ortak sözlükte (`00_ORTAK\kisiler.json`) bir kez tanımlanır. Roller **bağlantıyla** verilir (stoka, jige, teste).
- Örnek: "Seyit Karcı, ELINT anten tasarımcısı" bir kişi kaydı; SH2000'e "anten tasarımcısı" rolüyle bağlıdır.
- Kişi sayfası ters sorgu gösterir: tasarladığı stoklar, yaptığı jigler, açık sorunları, görevleri.
- **Minimum veri:** ad, rol, birim. İletişim ve özlük bilgisi **tutulmaz**.

### 4.6 Proje yapısı: çok stoklu, ortak klasör, tek stoklu

- **Çok stoklu proje:** `7107_SATCOM\` altında stoklar yan yana, `00_Proje_Geneli\` ortak klasör.
- **Tek stoklu proje:** proje klasörü açmaya değmeyen stok doğrudan `01_PROJELER\` altında (ya da ayrı bir "tek stoklu" kökünde). `projectNo` boş olabilir. Ağaçta ayrı "Tek stoklu" grubu.
- "Projeye taşı" komutu: klasörü taşır, `projectNo` yazar, dizini günceller.
- Proje geneli klasördeki belgeler projenin altında görünür, tüm stoklardan erişilebilir.

### 4.7 Dosya biçimleri

JSON anahtarları İngilizce camelCase, arayüz metinleri Türkçe.

`_TAKIP/stok.json`:
```json
{
  "schemaVersion": 1,
  "stockNo": "AB-7000-0001",
  "name": "SH2000 Boynuz Anten",
  "type": "Stock",
  "subType": "Antenna.Horn",
  "description": "2-18 GHz geniş bant boynuz anten, 4'lü dizinin eleman anteni",
  "projectNo": "7107",
  "parentStocks": ["AB-7000-0002"],
  "family": "AB-7000-0001",
  "variantDifference": null,
  "people": [
    { "person": "seyit-karci", "role": "AntennaDesigner" },
    { "person": "m-yilmaz", "role": "JigMaker" },
    { "person": "a-demir", "role": "TestOwner" }
  ],
  "specs": [
    { "name": "Frequency", "value": "2-18 GHz", "requirement": "TG-11" },
    { "name": "Polarization", "value": "Linear", "requirement": null }
  ],
  "jigs": ["JIG-1000-0009"],
  "cables": ["KBL-2000-0031"],
  "aidTools": [],
  "software": [{ "name": "AnalizAraci", "repoPath": "06_Yazilimlar\\analiz-araci", "pinnedVersion": "v2.4.1" }],
  "revision": "AB",
  "status": "TestWaiting",
  "waitingFor": "SATIMO slot",
  "serialNumbers": [{ "sn": "SN01", "location": "Lab-A raf 3", "lifecycle": "InTest" }],
  "template": "Boynuz-Anten-v1",
  "resultsValidityNote": null
}
```

- `family` kendine işaret ediyorsa ailenin ana üyesidir; varyantta `variantDifference` dolu. `resultsValidityNote` mühendis notudur, uygulama otomatik varsaymaz.
- Durum listesi **ayarlanabilir** (öneri: Planned, InPreparation, TestWaiting, InTest, ResultWaiting, Completed, OnHold), geçiş kuralları tanımlanabilir.
- **Yazılım sürümleri:** yerel `git` salt okunur çağrılır (`describe`, `rev-parse`, `status`); ağ işlemi yok. git yoksa elle girilir.

`_TAKIP/gunluk.jsonl` (satır başına kayıt, yalnızca ekleme). Giriş **türleri:** ölçüm, sorun, karar, değişiklik:
```json
{"date":"2026-11-04","kind":"Measurement","system":"SATIMO","sn":"SN01","phase":"P2","result":"Pending","by":"a-demir","jigRev":"AB","note":"Ölçüm alındı, analiz bekleniyor","files":["03_Test_Sonuclari\\SATIMO\\SN01\\Ham\\..."]}
```

`_TAKIP/sorunlar.jsonl`: `{ id:"S-014", scope:"Stock", severity, owner, rootCause, resolution, links:[stok/jig/test], status, lessonId }`.
`_TAKIP/gorevler.jsonl`: `{ id, title, owner, due, status, links }`. Obsidian notlarındaki `- [ ] ... #gorev` satırları listede **gösterilir** (başta salt okunur).

`00_ENVANTER/Jigler/<JIG>/jig.json` (kablo ve yardımcı araç aynı yapıda):
```json
{ "schemaVersion": 1, "jigNo": "JIG-1000-0009", "name": "SATIMO adaptör plakası", "type": "Jig", "subType": "Mechanical",
  "scope": "Shared", "revision": "AB", "status": "Ready", "maker": "m-yilmaz",
  "producedOn": "2026-09-18", "receivedOn": "2026-09-25", "calibrationDue": null }
```
- Stok–jig ilişkisi **tek yönlü**: `stok.json → jigs`. "Kullanan stoklar" taramayla türetilir.
- Stoğa özel jig `05_Jig\`'de; ikinci stok kullanmaya başlayınca `00_ENVANTER`'e taşınır, eski yerde taşındı notu kalır.
- Durumlar: Ready / RevChanged / InRepair / CalibrationDue vb. Kalibrasyon bitişine X gün kala uyarı.

`00_ORTAK/bilgi_bankasi/D-007.md` (ders): YAML (id, kategori, durum, kaynak sorunlar, etiket) + serbest metin.

### 4.8 Obsidian ile iş bölümü (`mock/Obsidian_Bolusumu.png`)

- **Yazma Obsidian'da, takip uygulamada.** Uygulama içinde not düzenleyici yazılmaz (ileride gerekirse eklenir).
- Her stokta düz markdown **stok kartı** `_TAKIP\<stok>.md`: YAML ön bilgi (stok, üst, jig, durum, bekleniyor, etiket) + serbest gövde. Uygulama **yalnızca YAML bloğunu** yönetir; gövdeye dokunmaz.
- `[[AB-7000-0002]]` viki bağları kullanılır; Obsidian grafiği ve geri bağlantılar çalışır. Uygulamada bağa tıklayınca ilgili **stoğa** gider.
- **Uygulamada render (salt okunur):** başlıklar, listeler, kalın, tablo, resim. Notlar markdown olduğu için doğrudan gösterilir.
  - `![[foto.png]]` gömmeleri çözülür ve önizlenir (tıkla: büyüt veya dosyayı aç).
  - Dataview sorguları, eklenti blokları gibi **Obsidian'a özgü biçimler düz metin** görünür; birebir Obsidian görünümü hedef değil.
  - Render için hafif bir markdown bileşeni seç (WPF; ya da WebView2 içinde HTML). **Lisansı ve bağımlılık sayısını Code kontrol etsin.**
- "Obsidian'da aç": `obsidian://open?vault=...&file=...`. Vault kökü ayarlanabilir.
- "Hızlı not ekle": not dosyasının **sonuna tek satır** ekler (atomik); başka değişiklik yapmaz.
- Notlar FTS5 ile aranır, yapay zeka da okuyabilir.

### 4.9 Doğrulama ve izlenebilirlik (DOORS esinli, opsiyonel)

DOORS yerine geçmez; Excel dışa aktarımından **salt okunur içe aktarma** ve kullanıcının bağlaması. (`mock/Izlenebilirlik_3Sutun.png`, `mock/Dogrulama_Matrisi.png`)

- **İçe aktarma:** DOORS Excel (formal modül). `Heading` ve `Requirement` satırlarını ayır; anahtar **PUID**; hiyerarşi bölüm numarasından; çok satırlı metin; sürüm sütunu varsa **değişiklik algılama**. **Sütun eşleme sihirbazı** (her modül farklı olabilir; eşleme kaydedilir).
- **Türler (ayarlanabilir):** TG (Test Gereği), TP (Test Planı), TR (Test Raporu) ve şirkete özel türler.
- **Gereksinim ağacı:** bir gereksinimden **türetilmiş** gereksinimler üretilebilir (TG-12 → TG-12.1, TG-12.2). Türetilmişin durumu ana gereksinime toplanır.
- **Doğrulama kaydı:** her gereksinim **bir veya birden çok doğrulama kaydıyla** bağlanır; bir doğrulama kaydı **birden çok gereksinimi** doğrulayabilir (çoka çok). Yöntem türleri:
  - **Test** (turlu), **Analiz** (belge/benzetim), **İnceleme**, **Bildirim/sapma** (Jira no, belge no, onay), **Uygulanamaz**, **İptal** (gerekçe, kim, ne zaman).
  - Test dışı doğrulamalar da geçerli birer karardır ("analizle geçti", "bildirimle geçti").
- **Turlar:** bir test birden çok turdan oluşabilir; **tur 1 kaldı → tur 2 geçti** akışı tarih sırasıyla saklanır, hiçbiri silinmez. Her turda: tarih, altyapı, SN, **yapan kişi**, sonuç, jig/kablo revizyonu, not, bağlı sorun, dosyalar.
- **Karar:** **otomatik** (onaylı maske + ölçüm, deterministik) ya da **elle** (Geçti, Kaldı, Analizle geçti, Bildirimle geçti, Uygulanamaz, İptal). Elle onay ayrıca "kim, ne zaman" ile yazılır. Çoğu zaman karışık olacaktır; ikisi de desteklenir.
- **Gereksinim durumu** bağlı doğrulama kayıtlarının **son kararlarından** hesaplanır. Kural **ayarlanabilir** ("test geçmeli, analiz yeterli değil" ya da "biri yeterli"), gereksinim türüne göre.
- **Eksik bilgi hata değil:** teknisyen/tarih boşsa alan boş görünür.
- **Gereksinim maskesi (limit çizgisi):** "2–6 GHz ≥6 dB, 6–18 GHz ≥9 dB" gibi metinden yapısal limit (`bands: [{from,to,op,value,unit}]`). **Yapay zeka yalnızca önerir, kullanıcı onaylar.** Onaylı maske ile geçti/kaldı **kodla** hesaplanır; ölçüm eğrisi üstüne limit çizilir, aşan bölge işaretlenir. **Onaysız maske ile karar verilmez.**
- **Şüpheli bağ:** DOORS'ta gereksinim sürümü değişirse bağlı sonuçlar "yeniden doğrula" işaretlenir.
- **Kapsama boşluğu** listesi, **doğrulama matrisi** (Excel'e aktarılabilir).
- Tüm panel **opsiyonel**: bağlanmamış gereksinim "karşılanmadı" değil "bağlanmadı".

### 4.10 Göç aracı (eski ekip dizini → yeni düzen)

```
Eski dizin → tara (salt okunur) → öneri tablosu (dosya → stok/klasör, KURU ÇALIŞTIRMA)
          → kullanıcı düzeltir/onaylar → yeni dizine KOPYALA (taşıma değil)
          → hash/boyut doğrulama → rapor (kopyalanan, eşleşmeyen, uyumsuz ad, yol uzunluğu uyarısı)
```
- **Kopyala, taşıma:** eski dizin dokunulmadan kalır.
- Eşleşmeyen dosyalar atılmaz: `_ARSIV` veya `_SERBEST` altına konur.
- Eşleştirme dosya ve klasör adından çıkarılır (stok no, doküman kodu, SN, P, sistem); uyuşmayanlar "elle seç".
- Bittikten sonra şablonlar eksik klasörleri tamamlar, tamamlanma kontrolü boşlukları gösterir.

### 4.11 Etki analizi, tamamlanma, geçerlilik (ilk öncelikli değer)

- **Etki analizi:** "bu değişirse ne etkilenir?" — eski revizyonla alınmış sonuçlar, jig/kablo revizyonu değişince o jigle (`jigRev` günlükte) alınan sonuçlar, aile stoğu değişince varyantlar. Yalnızca **uyarı ve liste**; karar kullanıcıya ait.
- **Tamamlanma kontrolü:** şablondaki **zorunlu** öğelere göre eksik listesi (kalifikasyon raporu yok, SN03 için SATIMO sonucu yok…).
- **Geçerlilik etiketi:** sonuç satırında geçerli / eski rev / jig değişti / süresi doldu (kalibrasyon, revizyon, jig revizyonu karşılaştırması).
- **Kullanım kararı** (SAP QA11 esinli): kabul / şartlı kabul / ret; kim, ne zaman, neden.
- **Değişiklik kaydı** (CC01 esinli): neden, etkilenen stoklar, onay notu. Basit kayıt; iş akışı motoru değil.
- **Baseline mühürü:** seçilen dosya kümesinin hash'i (örn. kalifikasyon teslimi); sonradan değişen dosya tespit edilir.
- **Ölçüm sistemi takvimi:** SATIMO/yankısız oda bekleme/rezervasyon notları (`waitingFor` ile bağlı).

### 4.12 Günlük, sorun, ders, görev (`mock/Kisi_Gunluk_Sorun.png`)

- **Günlük** stok/proje düzeyinde; giriş türleri ölçüm, sorun, karar, değişiklik; tek zaman çizelgesinde akar.
- **Sorun** bir olaydır (kapsam: stok / proje / genel). Çözülünce **"Ders'e dönüştür"** seçilebilir.
- **Ders** genel kural veya iyileştirme önerisidir; **ortak bilgi bankasında** durur, etiketle filtrelenir, birden çok sorundan beslenir. Durum: öneri / uygulandı / açık.
- **Görev:** liste ve pano (yapılacak / devam eden / bekliyor / bitti), sahip + termin + bağlı kayıt.
- Yeni stok açarken benzer geçmiş derslerin hatırlatılması opsiyonel özellik.
- **Bilgi bankası tek kaynak olmalı:** ekip Confluence kullanıyorsa dersler orada, uygulama bağlantıyı gösterir; yoksa bizde kalır. İki yerde tutma.

### 4.13 Özet seviyeleri ve görünümler

**Özet seviyeleri:** stok → proje → tüm kök. Göstergeler: test geçme %, tur ilerlemesi (P1/P2/P3 ayrı), eksik, açık sorun, kalibrasyon yaklaşan, açık görev.

**Görünümler:**
- **Genel sayfa** (`mock/Genel_Sayfa.png`): sol ağaç + KPI + proje tablosu + uyarılar + görev panosu + son hareketler. Aynı sayfa her düğümde yeniden kullanılır (kök/proje/stok).
- **Stok kartı** (`mock/SH2000_Stok_Karti.png`): tanım, kişiler, özellikler, jigler (kim yaptı/ne zaman geldi), zaman çizelgesi, Obsidian notu, test sonuçları ve analiz grafiği.
- **İzlenebilirlik görünümleri:** 3 sütun (TG → doğrulama kayıtları → sonuç ve kanıt), matris, ağaç, zaman çizelgesi, izlenebilirlik grafiği.
- **Fotoğraf galerisi:** küçük resim ızgarası, büyüt, varsayılan programla aç, klasörde göster.
- **Blok şemalar** (veriden **otomatik** çizilir, elle çizim editörü yok): ürün/aile şeması (`stok.json` ilişkilerinden), doğrulama akışı (gereksinim → TP → test → TR), **ölçüm düzeneği** (testin jig/kablo/cihaz bağlarından: VNA/SATIMO → kablo → jig → anten).
- Sonuç satırı ve dosya listelerinde **"Aç" (varsayılan program) ve "Klasörde göster"** düğmeleri.
- Durum renkleri yalnızca renge bağlı olmasın (metin/ikon da).

### 4.14 Dosya okuma, önizleme, rapor ve yazdırma

**Okuma (içerik indekse girer, "doküman içinde ara" çalışır):**

| Dosya | Önerilen yol | Not |
|---|---|---|
| Excel (DOORS vb.) | ExcelDataReader veya ClosedXML | .xlsx/.xls; hızlı |
| DOCX | Microsoft Open XML SDK | metin, başlık, tablo |
| PDF metni | PdfPig | metin çıkarır; taranmış PDF için OCR ilk sürümde yok |
| PDF önizleme | WebView2 | çoğu Windows'ta hazır, internet gerekmez |

- DOCX önizlemesi zordur: metin özeti + "Word'de aç" yeterli.
- **Kütüphane lisansları zamanla değişir; seçmeden önce güncel lisansı kontrol et.** Kural: şirkette ticari kullanım serbest olmalı.

**Rapor ve yazdırma:**
- Rapor **HTML şablonuyla** üretilir, **WebView2 ile PDF'e çevrilir** (`PrintToPdfAsync`) ya da doğrudan yazdırılır (Türkçe karakter, tablo, SVG grafik sorunsuz, ek kütüphane yok).
- Düzenlenebilir rapor gerekirse aynı veriden Open XML SDK ile **DOCX**.
- Rapor türleri: stok özeti, proje özeti, doğrulama matrisi, açık sorunlar ve görevler, kalifikasyon teslim listesi. Mühür varsa "bu dosyalar şu tarihte mühürlendi" notu.

## 5. Mimari

```
Pusula.sln
├── Pusula.Core   ← alan modeli, ayrıştırıcılar, tarayıcı, SQLite, ağaç, kurallar (UI bağımsız)
├── Pusula.Wpf    ← WPF arayüz (MVVM, elle yazılmış)
├── Pusula.Ai     ← yerel LLM katmanı (ayrı proje)
└── Pusula.Tests  ← birim testleri
```

Katmanlar: `Domain` → `Application` → `Infrastructure`. Çekirdekte UI referansı yok.

Önemli bileşenler:
- Ayrıştırıcılar: `StockNumberParser`, `DocumentNameParser`, `MeasurementFileNameParser` (hata fırlatmaz, sonuç nesnesi döner).
- `TypeRegistry` (tür/alt tür tanımları, ayarlanabilir), `FolderTemplateService` (kuru çalıştırma, ezmeme, zorunlu öğeler), `SpecialFolderPolicy`.
- `ScanService`, `StockTreeBuilder` (**döngü algılama zorunlu**), `EquipmentService` (jig, kablo, yardımcı araç).
- `StockFileStore`, `JigFileStore`, `PersonStore`, `JournalStore`, `IssueStore`, `LessonStore`, `TaskStore` (hepsi atomik yazım).
- `MarkdownNoteReader` (render, `[[bağ]]` çözümü, gömme çözümü), `ObsidianCardWriter` (yalnızca YAML bloğu), `QuickNoteAppender` (tek satır ekleme).
- `GitInfoReader` (salt okunur, yerel).
- `RequirementImporter` (Excel + sütun eşleme), `VerificationStore`, `LimitMask` + `MaskEvaluator` (deterministik), `StatusRollup` (ayarlanabilir kural).
- `ImpactAnalyzer`, `CompletenessChecker`, `ValidityEvaluator`, `VerificationMatrixBuilder`, `BaselineSealService`, `ChangeRecordStore`.
- `MigrationService` (tarama, eşleştirme önerisi, kopyalama, hash doğrulama, rapor).
- `SummaryAggregator` (stok → proje → kök yüzdeler).
- `DiagramBuilder` (ürün/aile, doğrulama akışı, ölçüm düzeneği; SVG/vektör çıktı).
- `ReportBuilder` (HTML şablonu → PDF/yazdır; opsiyonel DOCX), `DocumentTextExtractor` (xlsx/docx/pdf).
- `StockRepository` / `IndexRepository` (SQLite).

## 6. Kod Standartları

- **İngilizce** tanımlayıcılar, **Türkçe** XML doc yorumları.
- Allman parantez. **String interpolation kullanma.**
- Kullanıcıya görünen tüm metinler `Resources.resx` (sabit string yok).
- **Fody, CommunityToolkit.Mvvm, PolySharp kullanma.** `ViewModelBase`, `RelayCommand` elle yazılır.
- Bağımlılığı minimumda tut (`Microsoft.Data.Sqlite` yeterli; EF Core şart değil). Her yeni paket için **lisans ve gerekçe** sor. Pragmatik, araç odaklı; gereksiz soyutlama ekleme.
- `async/await`, `CancellationToken`, `IProgress<T>`; UI thread'i bloklama.
- `Path.Combine`, uzun yol farkındalığı, büyük/küçük harf duyarsız karşılaştırma.

## 7. Fazlar

**Faz 0 — İskelet:** çözüm yapısı, ayarlar (kök dizin, yerel indeks yolu), günlükleme, `Resources.resx`.

**Faz 1 — Çekirdek:** ayrıştırıcılar + testler, tür tanımları (`TypeRegistry`), `FolderTemplateService` (**stok şablonu oluştur**, kuru çalıştırma, zorunlu öğeler, özel klasör politikası), `stok.json` okuma/yazma, tek stoklu/ortak klasör, yol uzunluğu kontrolü.

**Faz 2 — Tarama ve indeks:** artımlı tarama → SQLite; "yeniden oluştur"; uyumsuz dosya listesi; özel klasörlerin hariç tutulması.

**Faz 3 — Göç aracı:** eski dizin tarama, eşleştirme önerisi tablosu, kuru çalıştırma, kopyalama, hash doğrulama, rapor. (Kullanıcının hazır verisi olduğu için ilk günden değer üretir.)

**Faz 4 — Panolar:** genel sayfa (KPI, proje tablosu, uyarılar), sol ağaç, stok kartı, test sonuçları tablosu (altyapı/SN/tur/kim), jig-kablo-yardımcı durumu, zaman çizelgesi, fotoğraf galerisi, "Aç" ve "Klasörde göster", `SummaryAggregator`.

**Faz 5 — Kişi, günlük, sorun, ders, görev:** `kisiler.json`, kişi bağlantıları ve kişi sayfası, günlük türleri, sorun kaydı ve "Ders'e dönüştür", ortak bilgi bankası, görev listesi/panosu, durum geçişleri, SN konum/yaşam döngüsü.

**Faz 6 — Notlar ve arama:** Obsidian stok kartı (YAML yönetimi), notların uygulamada render'ı (`[[bağ]]`, gömme), `obsidian://` bağlantısı, hızlı not ekle, FTS5 arama, xlsx/docx/pdf metin çıkarma ve indeksleme.

**Faz 7 — Etki analizi ve tamamlanma kontrolü (öncelikli değer):** `ImpactAnalyzer`, `CompletenessChecker`, geçerlilik etiketleri, kalibrasyon uyarıları, yazılım sürümü (git) okuma, kullanım kararı, değişiklik kaydı, baseline mühürü.

**Faz 8 — Doğrulama ve izlenebilirlik (opsiyonel):** DOORS Excel içe aktarma + sütun eşleme, türetilmiş gereksinimler, doğrulama kayıtları (test/analiz/inceleme/bildirim/iptal), turlar, otomatik/elle karar, `StatusRollup`, şüpheli bağ, kapsama boşluğu, doğrulama matrisi, 3 sütun görünümü, **gereksinim maskesi + limit çizgisi** (önce elle maske; yapay zeka önerisi Faz 10'dan sonra).

**Faz 9 — Rapor ve blok şemalar:** özet raporlar (HTML → PDF/yazdır), opsiyonel DOCX, ürün/aile, doğrulama akışı ve ölçüm düzeneği şemaları.

**Faz 10 — Yerel yapay zeka (yalnızca okuma):**
- `IChatClient` + OllamaSharp; sağlayıcı değiştirilebilir. Uç nokta **yalnızca loopback**; başka adres reddedilir.
- **Yalnızca okuma araçları:** `list_stocks`, `get_stock`, `get_tree`, `get_test_log`, `search_notes`, `get_jig_usage`, `list_files_summary`, `get_impact`, `get_completeness`, `get_requirement_coverage`, `list_issues`, `list_lessons`, `get_summary`. Yazma/silme/dosya açma yok.
- Sayı, tarih, liste, geçti/kaldı araçlardan gelir; model üretmez; cevapta kaynak araç görünür.
- Analiz yorumu: ölçüm sonucu + notlar + sorunlar birleştirilir (örn. "limit aşan ölçüm için ilgili notu öner"); sayılar hesaptan gelir.
- Limit maskesi önerisi: model `bands` önerir, **kullanıcı onaylamadan etkin olmaz**.
- Örnek sorular: "Son 30 günde testi yapılan stoklar?", "SATIMO ham verisi olup analizi olmayan stoklar?", "JIG-1000-0009 kaç stokta?", "Kablo kaynaklı tekrar eden sorunlar?", "AB-7000-0001 için son notları özetle."
- Modeli araç çağırma başarısına göre seç (llama3.2, qwen3:4b). İstemleri ve çağrıları yerel günlüğe yaz.

**Faz 11 — Entegrasyonlar (çok ileride, şimdilik kapsam dışı):**
- Adaptör katmanı (`IImportSource`): her sistem kendi işinin sahibi kalır, uygulama **salt okunur içe aktarma** yapar: SAP/Teamcenter (üst-alt stok), Jira (sorun/görev eşleştirme `S-014 ↔ PRJ-1234`), Confluence (bilgi bankası bağlantısı).
- Önce dışa aktarım dosyalarıyla (Excel/CSV/XML) başla. Kurum içi sunucuya bağlanmak "dış ağ yok" kuralını "yalnızca izinli kurum içi adres" olarak değiştirir; bunu açıkça onaylat.
- Çok kullanıcılı/şirket ölçeği: paylaşılan şablonlar, roller.

## 8. Test Stratejisi

- Ayrıştırıcılar: geçerli/geçersiz/sınır (iki stok no biçimi, eksik SN/P, tuhaf revizyon).
- Tarayıcı: geçici dizinlerle artımlı tarama; `_` klasörlerinin hariç tutulması.
- Ağaç: döngü (A→B→A), kendi kendine referans, eksik stok.
- Şablon: var olan klasörü ezmeme, eksik tamamlama, kuru çalıştırma çıktısı.
- Göç: kopyalama sonrası hash/boyut eşitliği, eşleşmeyen dosyaların `_ARSIV`'e gitmesi, kaynak dizinin değişmemesi.
- Dosya yazımı: yarıda kesilen yazmanın veriyi bozmaması; Obsidian kartında kullanıcı gövdesinin korunması; hızlı not eklemenin yalnızca satır eklemesi.
- Markdown render: `[[bağ]]`, `![[gömme]]`, bilinmeyen Obsidian bloğunun düz metin kalması.
- Gereksinim içe aktarma: Heading/Requirement ayrımı, çok satırlı metin, sürüm değişimi, bozuk Excel.
- Doğrulama: çoka çok bağ, türetilmiş gereksinim toplama, tur geçmişi (tur 1 kaldı → tur 2 geçti), test dışı kararlar, iptal, ayarlanabilir durum kuralı.
- Maske: bant sınırları (6 GHz'de hangi limit?), birim dönüşümü, **onaysız maske ile karar verilmemesi**.
- Etki/tamamlanma/geçerlilik: bilinen senaryolarla beklenen bayraklar.
- Özet yüzdeleri: `_ARSIV` hariç tutma, gereksinimi bağlanmamış stokun "karşılanmadı" değil "bağlanmadı" sayılması.
- Rapor: HTML'den PDF üretimi, Türkçe karakter, SVG grafik.

## 9. Dağıtım

Teknisyen sürümü kaynaksız: `PUSULA_SETUP`, `PUSULA_DATA`, `PUSULA_RELEASE`, `PUSULA_LOG`, `Readme.txt`. Self-contained yayın değerlendir (bağımlılık sorunu olmasın). Yazma gereken klasörler (günlük, indeks) yazma izni olan yerlerde. WebView2 çalışma zamanının hedef makinelerde bulunduğunu doğrula.

## 10. Açık Sorular (kodlamadan önce kullanıcıya **tek mesajda** sor)

1. Aynı veriyi birden fazla kişi kullanacak mı? (ağ paylaşımı, çakışma)
2. Durum listesi ve geçiş kuralları kullanıcının kendi terimleriyle ne olmalı?
3. Ölçüm dosya adı sözleşmesi (4.3) onaylı mı? Mevcut dosyalar uyuyor mu?
4. **Eski ekip dizini:** ağ paylaşımında mı, kaç GB/dosya, eski ekipte isimlendirme tutarlı mıydı? Göç sırasında hangi dosyalar `_ARSIV`'e gitsin?
5. Şablonda hangi öğeler **zorunlu** sayılsın (tamamlanma kontrolü için)?
6. Tek stoklu projeler `01_PROJELER` altında mı yoksa ayrı bir kökte mi dursun?
7. Obsidian vault kökü nerede (proje kökü mü, ayrı vault mu)? Obsidian kullanmayan teknisyenler için uygulama içi hızlı not yeterli mi?
8. DOORS Excel örnek dosyası paylaşılabilir mi (başlıklar/anonim satırlar yeter)? TG/TP/TR yapısı ve sütun adları?
9. Gereksinim durumu kuralı: hangi gereksinim türü için hangi doğrulama yöntemi yeterli? (test zorunlu mu, analiz yeterli mi)
10. SAP "MMBE0X" ifadesi standart MMBE mi, şirkete özel işlem mi? (Stok kartı ekranını etkiler.)
11. Yapay zeka: kurumda onaylı yerel model yolu (Ollama) var mı?
12. *(Yanıtlandı: .NET Framework 4.8 + WPF, x64, DesignKit.Wpf.)*
13. Bilgi bankası (dersler) tek kaynak olarak nerede dursun: bu uygulamada mı, Confluence'ta mı?
14. Excel, docx, pdf okuma ve markdown render kütüphaneleri için şirket lisans politikası?
15. Kişi sözlüğünde hangi bilgiler tutulabilir (öneri: ad, rol, birim; iletişim/özlük yok)?

## 11. Claude Code için Başlangıç Talimatı

1. Bu dokümanı ve `docs/mock/` altındaki PNG mock'ları oku.
2. Açık soruları kullanıcıya **tek mesajda** sor (kullanıcı bu konuşmada çoğunu "Code'da konuşuruz" diye bıraktı).
3. Cevaplara göre Faz 0–3 için ayrıntılı plan çıkar, onay al. **Göç aracı (Faz 3) ilk değer** olabilir; sırayı kullanıcıyla netleştir.
4. Faz 1'e ayrıştırıcı ve şablon üreticiyle başla, **testleriyle birlikte**.
5. Her fazdan sonra çalışan sürüm bırak; arayüzden önce çekirdek test edilmiş olsun.
6. Kural: **karar kodla, öneri yapay zekayla, onay kullanıcıyla.** Uygulama kullanıcının dosyalarını **asla silmez**, göç **kopyalar**, notların gövdesine **dokunmaz**.
7. Mock'lardaki sayılar örnektir; gerçek veri modelinden hesaplanmalıdır.

## Kaynaklar

- SQLite FTS5 giriş: https://dbpro.app/blog/sqlite-full-text-search-fts5
- Ollama ile .NET: https://dev.to/aiengfoundry/ollama-with-c-and-net-run-a-local-llm-in-aspnet-core-3l9c
- Microsoft Agent Framework, Ollama sağlayıcısı: https://learn.microsoft.com/en-us/agent-framework/integrations/by-component/model-providers/ollama
- OpenTMI (veri modeli referansı): https://github.com/opentmi/opentmi
- NI SystemLink: https://www.ni.com/docs/en-US/bundle/systemlink/page/systemlink-early-access.html
