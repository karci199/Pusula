# Çalışma notları

Son güncelleme: 22.11.2026. Dallar: **`main`** (sağlam) ve **`gelistirme`** (çalışma dalı); iş `gelistirme`'de yapılır,
sağlam olunca `main` ileri alınır (fast-forward). İkisi de pushlu. Eski dallar silindi; ilk gün dosyaları (ISIMLENDIRME.md,
yazılım şablonu, eski mocklar) `C:\Projeler\Aktif\Pusula_ARSIV\pusula-projesi-2d07af` altında.

## Şu an çalışanlar

- Kök klasör seçimi (Gözat), son seçilen kök hatırlanır (`%LOCALAPPDATA%\Pusula\ayarlar.json`).
- Solda ağaç: projeler ve altındaki stoklar; türe göre renkli ikon.
- Ağaç başlığında **+** menüsü (Yeni stok · proje, Yeni proje), ⚙ Şablonları düzenle, Yenile.
- Sağ tık: Yeni stok, Modül ekle, Klasörde aç, Yenile. Kısayollar: Ctrl+N, Ctrl+Shift+N, F5.
- Yeni proje: `<no>_<ad>` + gizli `_TAKIP\proje.json` + boş `00_Proje_Geneli`.
- Yeni stok: `<stokNo>` + gizli `_TAKIP\stok.json` + şablondaki modüllerin klasörleri.
- Şablonlar kökte gizli `_PUSULA\sablon.json` (sürüm 3) içinde; ekipte ortak. Eski sürümler okunurken çevrilir.
- Şablonlar penceresi: solda şablonlar, sağda modül listesi, "Modül ekle" menüsü, modül penceresi (iç içe alt klasörler).
- **Sağ panel özeti:** proje seçilince stok tablosu (şablon, dosya, boyut, son değişiklik) ve proje geneli dosya sayısı;
  stok seçilince klasör tablosu (dosya, boyut, son değişen dosya), boş/eksik/özel klasörler soluk. Sayım arka planda.
  `_` ile başlayan klasörler (`_ARSIV`, `_TAKIP`) toplamlara girmez. Satıra çift tıklayınca klasör açılır.
- **Tür kalıbı:** kökte gizli `_PUSULA\turler.json`. Her tür (Proje, Stok, Jig, Kablo, Yazılım): numara biçimleri
  (`#` rakam, `@` harf; örn. `@@-####-####`, `JY-####-####` geliştirme), alt türler, sıralı ve sıra dışı durumlar
  (renk anlamı: neutral/waiting/active/done/warning, not isteyen durum). Ortak etiket listesi. Yeni tür = dosyaya blok.
- **Stok durumu:** Hazırlık → Ölçüm bekliyor → Testte → Tamamlandı → Üretime teslim edildi; sıra dışı Beklemede (not ister).
  Başlıkta durum etiketi (tıkla, değiştir), aşama çubuğu, "… tarihinden beri · not". `stok.json` içine status/statusSince/statusNote.
  Yeni stok "Hazırlık" ile başlar. Biçime uymayan stok numarası kabul edilmez. Proje tablosunda Durum sütunu ve durum dağılımı.
- **Proje / stok olarak işaretle:** elle açılmış klasöre sağ tık; yalnızca gizli `_TAKIP` dosyası eklenir, dosyalara dokunulmaz.
  Proje no/ad klasör adından (`7000_MILKON`), stok no klasör adından doldurulur; stok numarası biçim kontrolünden geçer.
- **Stok sekmeleri (Win11 düzeni):** Genel (dikkat, her SN'in son turu tablosu, zaman çizelgesi), Testler (SN, Tur, Sistem,
  Sonuç, Tarih, Kişi, Not; başlığa tıkla sırala, çift tık notu aç), Kalifikasyon (aynı tablo), Klasörler (kutucuklar + ağaç).
- **Test koşusu yapısı:** `03_Test_Sonuclari\<SİSTEM>\<SN>_T<tur>\` (kalifikasyon: `04_Kalifikasyon\03_Test_Sonuclari\...`).
  SN: `SN01` seri, `SNP1` prototip; T = tur (deneme sırası; `_P1` de okunur). İçinde `test.md` (Obsidian notu, YAML: sistem, sn,
  tur, tarih, sonuc, kisi, plan, jig, etiketler), `Ham\`, `Analiz\`, `Foto\`. Sonuç sırası: test.md → KIWI `_kosu.md` → girilmedi.
  Jig alanı "JIG-1000-0009 Rev AC" biçimindeyse revizyon değişikliği Genel'de uyarı olur.
- **Envanter (kablo, jig):** ortak kayıt yapısı; tür kalıbında (`turler.json`, sürüm 2) ikon, renk, numara biçimleri
  (kablo `JT-####-####`, jig `JIG-####-####`, değiştirilebilir), alt türler, durumlar ve üç düzeyin klasörleri.
  Düzeyler: Genel `<kök>\ENVANTER\Kablolar|Jigler`, Proje `<proje>\00_Proje_Geneli\Envanter\…`, Stok `<stok>\02_Test_Araclari\Kablo|Jig|Yazilim|Diger` (eski `05_…` klasörleri de okunur).
  Kayıt: `<numara>\_TAKIP\kayit.json` — klasör adı yalnız numara (16.11.2026), tanım kayıtta (tür, numara, tanım, alt tür, not, türe özel alanlar, olaylar).
  Durum = son olay; her olayda tarih, kişi, konum, not. Talep / yapan / teslim / konum olaylardan okunur.
  Durumlar: Talep → Hazırlanıyor → Teslim alındı → Kullanımda → Üretime teslim edildi; sıra dışı: Onarım / değişim gerekiyor, Çoğaltma isteniyor.
  Ağaçta Envanter bölümü (Kablolar, Jigler, kayıtlar); "+" menüsünde Yeni kablo / Yeni jig (düzey seçimi);
  kayıt sayfası: durum etiketi (menüden değiştir → tarih, kişi, konum, not), aşama çubuğu, Bilgiler, Geçmiş.
  Kayıt uygulamadan açılır; klasör şablon alt klasörleriyle gelir (kablo `01_Belgeler`, `02_Fotograflar`;
  jig `01_Cizimler`, `02_Belgeler`, `03_Fotograflar`; `turler.json` → `recordFolders`). Numara önerilmez, elle girilir.
  Elle açılmış klasör ağaçta soluk "işaretsiz" görünür; sağ tık → "Jig/Kablo olarak işaretle" (numara ve tanım
  klasör adından önerilir, durum seçilir, klasör yeniden adlandırılmaz).
  Kayıt klasörü kendiliğinden tanınır (yalnızca okunur): `<numara>.md` Obsidian notu (yeni kayıtta YAML + başlıkla açılır;
  yoksa "Not oluştur"; var olana dokunulmaz), alt klasör kutucukları (dosya sayısı, adlardaki son `RevAC`/`Rev2`;
  tıkla aç), fotoğraf şeridi (kök + alt klasörler, ilk 4 + "+n"). Örnekler A–F `C:\PusulaDeneme` içinde (betik: make-record-files.ps1).
- **Stok kapağı (Genel sekmesinin üstü):** stok kökündeki `<stok no>.md` notunun ilk satırları (yoksa "Not oluştur": YAML stok, ad,
  proje, şablon), `07_Fotograflar` (alt klasörler dahil) fotoğrafları; adında "kapak" geçen resim kapak olur. Örnek: SH2000 (AB-7000-0002).
- **Olay notları (tek biçim, Obsidian):** stokun ya da kaydın klasöründe (`_` ile başlayanlar hariç, öneri `Notlar\`) YAML'ında
  `olay:` (toplanti, karar, numune, sevkiyat, revizyon, ariza, not …) ve `tarih:` olan her `.md` olaydır; stokta **Günlük**'e (eski adı
  Zaman çizelgesi), jig/kabloda **Geçmiş**'e düşer, tıklayınca açılır. `olay: test` notları test okuyucusundan gelir. Şablonlar:
  `<kök>\_Sablonlar\Olay.md`, `Test.md`. Sıradaki: + Olay / + Test koşusu kısayolu, durum geçmişi, bağlı jig olayları, yeni dosyalar.
- **Kısa notlar (uygulamadan):** proje / stok / envanter kaydının bilgi dosyasına (`proje.json`, `stok.json`, `kayit.json` → `notes`:
  tarih saat, kişi, metin). Stokta Genel kapağında kutu, notlar Zaman çizelgesine; kayıtta Geçmiş'e; projede "Kısa notlar" kartı.
  Tablolarda sağ tık → Not ekle… (diyalog). Obsidian notunun gövdesine yazılmaz.
- **Tablolar:** çift tık sayfaya gider (projede stoka), satır başındaki klasör düğmesi klasörü açar; sağ tık Aç · Klasörde aç · Not ekle.
- **Dosya türleri:** Ayarlar → Dosya türleri… (`turler.json` → `fileTypes`: ad, uzantılar, grup; varsayılan PDF, Word, Excel, Visio,
  STEP, DWG, SolidWorks, Touchstone, CST, HFSS, MATLAB, Fotoğraf, Not, Arşiv). Jig/kablo klasör kutucuklarında "2 STEP · 1 PDF".
- **Arama ve süzgeç:** ağacın üstünde arama (numara, ad, tanım; Türkçe i/ı farkı yok, eşleşenin üstleri açılır). Envanter listesinde
  arama (numara, tanım, konum, not) + Tür, Alt tür, Durum, Düzey (Genel / Proje / Stok) süzgeçleri; "8 kayıttan 2 tanesi".
- **Ağaç:** Envanter bölümü projelerden boşlukla ayrılır; kayıt düğümünde yalnızca numara (tanım ipucunda), işaretsiz klasör soluk italik.
- Varsayılan şablonda `03_Test_Sonuclari` altında yalnızca sistemler (VNA, SATIMO, Yankisiz, CK); `SN01\Ham`, `SN01\Analiz` ölçümle açılır.
- **Türe özel alanlar (10.11.2026):** `turler.json` → türün `fields` listesi (ad, tür: yazı / sayı / evet-hayır / seçim, birim,
  seçenekler, alt türler). Hazır alanlar: kablo (adet, boy, uç A/B, RF'de frekans + faz kararlı, Haberleşme'de protokol + pin yapısı,
  Güç / veri'de akım + kesit), jig (adet, malzeme, ölçüler, yüzey, bağlantı, Montaj'da tork, Ölçüm adaptörü'nde ölçüm sistemi,
  çizim revizyonu), Yazılım türü (sürüm, dil, konum, bağlı cihaz). Değerler Düzenle'de girilir; `kayit.json` → `fields`'a ve notun
  YAML başlığına yazılır (yalnız o alt türün alanları; özel karakterli değer tırnaklı; notun kodlaması korunur). Bilgiler kartında birimiyle.
  `turler.json` sürüm 3 göçü: alanı boş kablo/jig hazır alanları alır, Yazılım türü eklenir. Kaydı olmayan tür stokta sekme açmaz.
- **Stok günlüğünde jig/kablo olayları:** stokun kullandığı (kendi + bağlı) kayıtların durum değişiklikleri (Talep, Hazırlanıyor,
  Teslim alındı, Onarım …) Günlük'e kendiliğinden düşer: "JIG-1000-0009 · ad", durum etiketi, "Jig · tarih · kişi", konum.
  Not düğmesi kaydın notunu açar. Bağ kaldırılınca düşer.
- **Test / analiz tarihi (10.11.2026):** `test.md` → `analiz_tarihi`, `analiz_notu`; yoksa analiz = test günü. Koşu sağ tık →
  "Analiz tarihi ve notu…" (not yoksa test tarihiyle oluşturulur). Testler tablosunda Tarih'in yanında Analiz sütunu (yalnız ayrı
  günse dolu), günlük kartında "analiz 12.10.2026" ve not. Tablo sütun düzeni artık başlık anahtarıyla saklanır.
- **Bahsedildiği yerler:** kökteki bütün md'lerde `[[…]]` ve düz numaralar taranır (arka planda, Yenile'de baştan). Proje, stok ve
  kayıt sayfasında kart; kendi klasöründeki notlar, gizli / `_SILINENLER` / `_TAKIP` / arşiv klasörleri ve kod blokları sayılmaz.
- **Özellikler bölümü:** Bilgiler kartında türe özel alanlar ayrı, iki sütunlu kutucuklar; "Özellikler · Mekanik · 6 / 6 dolu".
- **md not penceresi (Markdig 0.37, BSD-2):** Ayarlar → Not programı → "Pusula (yerleşik)" (yeni kurulumda varsayılan).
  Yorumlar (`<!-- -->`, `%% %%`) görünümde gizlenir, Ctrl+Shift+C; not içi `#başlık` bağlantısı kaydırır. Yazım kılavuzu: [MD_KILAVUZU.md](MD_KILAVUZU.md).
  Bul / değiştir (Ctrl+F / Ctrl+R; harf, tam kelime, regex), başlık paneli (Ctrl+Shift+O), yan yana birlikte kaydırma (başlıklar çapa),
  satır taşı / çoğalt / sil (Alt+↑↓, Ctrl+D, Ctrl+Shift+K), otomatik çift, seçime adres yapıştırınca bağlantı, tablo hizalama
  (Ctrl+Shift+T, tabloda Tab). Bütün düzenlemeler Ctrl+Z ile tek adımda geri alınır.
  Ekranı kaplar; Düz / Yan yana / Görünüm (Ctrl+1/2/3); Görünüm Obsidian'a benzer: YAML özellik kutusu, başlık, liste, görev
  kutusu (tıklanınca metinde işaretlenir), kutular (`> [!warning]`), tablo, kod, vurgu, çizili, dipnot, `#etiket`, resim
  (`![[a.png|300]]`, `![](yol)`; adla da bulunur), `[[bağ]]` (tıklayınca ana pencerede kaydı seçer, not adıysa o notu açar,
  yoksa yeni not oluşturmaz). Kısayollar: Ctrl+B / I / E / L / K / H, Ctrl+Shift+X / H, Ctrl+S, F11, Ctrl+tekerlek yazı boyutu,
  Enter listeyi sürdürür, Tab girinti. @ ya da [[ yazınca jig / kablo / yazılım / stok / proje / kişi listesi. Resim: araç
  çubuğu, Ctrl+V (pano resmi `Ekler\yapistirilan_….png` olur), sürükle-bırak; kök dışı dosya `Ekler\`e kopyalanır, aslına
  dokunulmaz. Dışarıda değişirse: değişiklik yoksa kendiliğinden yüklenir, varsa uyarı şeridi. Kapatırken kaydedilsin mi sorusu,
  otomatik kaydet (ayar), okuma genişliği, yazı tipi/boyut, kaydırma, satır sonu, YAML ayarları `ayarlar.json` → `noteEditor`.
  Kaydetme atomik, notun BOM / satır sonu biçimi korunur; Pusula nota kendinden bir şey eklemez.
- **Diskteki değişikliği izleme:** kök izlenir; dışarıdan değişiklik olunca Yenile düğmesi sarı olur (bildirim yok), yenileyince söner.
  Sayılmaz: uygulamanın kendi yazdıkları (3 sn pencere), `.obsidian` gibi gizli klasörler, `_TAKIP`, `_PUSULA`, geçici dosyalar,
  klasörün yalnız tarih değişmesi.

## Stok klasör yapısı (15.11.2026 kararı, 16.11.2026'da koda geçti)

```
AB-7000-0002\
  AB-7000-0002.md        Birim bilgisi (her stokta; YAML'da test_gereksinimleri: "[[…_Test_Gereksinimleri]]")
  01_Ortak\              her stokta: spek, DM, çizim, ÇK/EMC gerekleri, sınıflandırılamayanlar
     Fotograflar\        (21.11.2026) bütün fotoğraflar: <stok>_kapak, _on, _arka, _yan, _ust, _montaj, _ic, _etiket (_on_2 …);
                         bu sırayla gösterilir, uymayanlar sonda. Kapak yoksa ilk fotoğraf. Şablon sürüm 5 boş açar.
  02_Test_Araclari\      şablona göre: Jig\ Kablo\ Yazilim\ Diger\  — kayıt klasörü yalnız numara (JY-7000-0005)
  03_Testler\            şablona göre: 01_Islevsel\ 02_Ozellik\ 03_CK_EMC\ 04_Diger\
     AB-7000-0002_Test_Gereksinimleri.md     doğrulama tablosu
     01_Islevsel\Anten_Oruntu\SNP1_T1\      koşum (test klasörü isteğe bağlı; koşum doğrudan grup altında da olabilir)
  _ARSIV\                gerektiğinde; uygulama okumaz
  _TAKIP\                gizli, uygulama verisi
```

- Şablonlar: Pasif anten → 01_Ortak, 03_Testler · Döner anten → 01_Ortak, 02_Test_Araclari, 03_Testler.
- `_PUSULA\sablon.json` sürüm 4: eski varsayılan modüller (00_Diger, 01_Dokumanlar, 02_Test_Planlari, 03_Test_Sonuclari,
  04_Kalifikasyon, 05_Jigler, 06_Yazilimlar, 07_Fotograflar) yeni üç modüle çevrilir; kullanıcının eklediği modüller kalır.
- `_PUSULA\turler.json` sürüm 12: stok düzeyi kayıt klasörleri `02_Test_Araclari\…`; yazılım numarası `JY-####-####`.
- **Eski stoklar taşınmaz**, okunmaya devam eder: `05_Jigler` / `05_Kablolar` / `06_Yazilimlar` / `07_Test_Gerecleri`,
  `03_Test_Sonuclari` ve `04_Kalifikasyon\03_Test_Sonuclari` koşuları, `07_Fotograflar` kapağı, `01_Dokumanlar`.

### Doğrulama tablosu (17.11.2026: test gereksinimi → test planı → test raporu)

- Sütunlar: # · TG no · Kaynak · Başlık · Kısa not · Gereksinim · Grup · Alt grup · Tür · Bağlı · Yöntem · Altyapı · TP no ·
  TP açıklama · TP not · TR no · TR açıklama · TR not · Durum (21.11.2026: 19 sütun; başlıktan tanınır, sıra serbest,
  tanınmayan sütunlar korunur; eski tablolar ilk yazımda yükselir).
- 21.11.2026 kararı: madde önce yalnız gereksinimle girilir (madde penceresinde TP / TR yok; var olanlar korunur). Plan ve
  rapor sonra, seçilen maddelere toplu "Planla…" / "Raporla…" ile (mock: `docs/mock/planla_raporla.png`; henüz kodlanmadı).
  Test sonucu türleri (rapor): Geçti · Kaldı · Hazırlanmadı. Kaldıysa bildirim no TR notuna yazılır. Hepsi test değil
  (ör. kütle tartılır = Muayene); ad "Doğrulama" kalır.
- 21.11.2026 (kodlandı): tablo 21 sütun (+ TR sonuç, Sonuç yeri). Açık satır: gereksinim üstte, altında Test planı ve
  Test raporu kartları (denemeler kaldırıldı; sonuç raporda). Plan penceresi: TP no, açıklama, yöntem, altyapı, not
  (Bekliyor → Planlandı). Rapor penceresi: TR no, açıklama, sonuç, sonucun yeri (stok içindeyse göreli), not (Geçti /
  Kaldı durumu da değiştirir). Yöntem ve altyapı madde penceresinden plana taşındı.
- **Sıradaki (kullanıcı isteği):** her şey md'de okunur olsun: "## Plan" ve "## Sonuç" altında madde başına
  "### DGÖ-1" bölümü uygulamaca yazılsın (başkaları Obsidian'da okusun); altına elle yazılan ayrıntıya dokunulmasın.
  Ardından Excel'e otomatik verme.
- **Sonra (kullanıcı isteği, 21.11.2026):** stoktaki fotoğraf kuralı (kapak `<numara>_kapak`, numaralı şerit, numarasızlar
  görünmez, kapak yoksa genel resim) test yardımcı araçlarının sayfasına da uygulanacak.
- TG no = DOORS'taki test gereksinimi dokümanının madde no'su. Tasarımcının yayınlamadığı Word dokümanındaki maddelerde no
  yoktur; Kaynak sütununa doküman no yazılır.
- Tür: Temel (şartnamede) / Türetilmiş (temelin sağlanması için tasarımcının koyduğu; Bağlı = temel TG no); boş olabilir.
- Yöntem: Test · Muayene · Belge · Gösterim · Diğer (test planında belirlenir). TP no / TR no: plan ve rapordaki madde no.
- Dokümanlar (stokta her biri tek) dosyanın YAML'ında: `test_gereksinimi`, `tasarimci_dokumani`, `test_plani`, `test_raporu`.
  Addan durum: `_AA` yayınlandı, `_XX` / `_BA1` hazırlanıyor.
- Durumlar: Bekliyor · Planlandı · Hazırlık yapılıyor · Yapılıyor · Değerlendiriliyor · Geçti · Kaldı · Şartlı kabul ·
  Tekrarlanacak · Kapsam dışı (ilerlemeye sayılmaz; şartlı kabul geçti sayılır).
- Arayüz: satır tek tıkla açılır (tam gereksinim, kaynak satırı, plan / rapor özeti), çift tıkla düzenlenir.
- Madde penceresi (17.11.2026 sadeleşti): TG no tek kutu (madde no yoksa doküman no yazılır), gereksinim ve kısa not tek
  alan (ilk satırı tabloda, tamamı açılınca), alt grup pencerede yok (md'deki eski değerler korunur), tür / bağlı temel / yöntem /
  altyapı / TP-TR madde no / durum. "Pencere açık kalsın" sıradaki maddeyi hemen girmek içindir.
- Sırada: test planı ve test raporu md'de ayrıntılandırılacak (altyapı, dikkat, sonuçlar, bildirim no, sonuç dizini, ekler).
- **Excel'den alma (21.11.2026, sonra bakılacak; kullanıcı örnek dosyalara bakacak).** Sütunlar dosyadan dosyaya biraz
  değişir, ortak olanlar genelde şunlar:
  - Test gereksinimi: doğrulama madde no · madde açıklaması · gereksinim türü · not
  - Test planı: test planı madde no · açıklama · doğrulama yöntemi · not
  - Test raporu: rapor no · açıklama · test sonucu · not
  Excel elle düzenlenebilir olduğu için sütun eşleme (başlıktan tanıma) gerekir; zor kısım bu.

## Sıradaki adımlar (kullanıcıyla konuşulan sıra)

**Örnek veri (deneme kökü, 4 stok, 12 koşu: prototip, tekrar, jig değişikliği, devam eden ÇK):** üretici betikler scratchpad'de. Asıl örnek: `C:\Projeler\Veri\Pusula-Ornek` (AB-7000-0002, SH2000; KIWI koşusu, ATD planı, jig ve düzenek görseli).
Kopyası `C:\PusulaDeneme\7000_MILKON\AB-7000-0002` altında (işaretsiz). KIWI her koşuda `_kosu.md` yazar
(Genel sonuç, adımlar, kişi, tarih); test koşuları buradan otomatik okunabilir.

1. ~~Sağ panel özeti~~ (yapıldı).
2. ~~Durum~~ (yapıldı: tür kalıbı + stok durumu).
3. ~~Test koşuları, Genel / Testler / Kalifikasyon / Klasörler sekmeleri~~ (yapıldı).
3a. **Fotoğraflar sekmesi (sıradaki):** 07_Fotograflar + koşuların Foto\ klasörleri, küçük resim ızgarası, tıkla büyüt.
3c. ~~Stokta Jigler / Kablolar sekmeleri~~ (yapıldı): stok düzeyindeki kayıtlar + `stok.json` → `inventory` bağları; her tür
    kendi sekmesinde (türlerden kurulur; ileride Yazılımlar, Diğer, Arşiv), Ekle (genel / proje kaydını bağlar), Yeni, satır menüsü (Kaydı aç, Klasörde aç, Bağı kaldır). Genel'de özet kartları,
    sıra dışı durum ve bulunamayan bağ Dikkat'e düşer. Kalan: kaydın sayfasında "kullanan stoklar".
3f. ~~Tür tanımlama ekranı~~ (yapıldı): Ayarlar → Türler…; envanter türlerinin adı, çoğulu, simgesi, rengi, numara biçimleri
    (birden çok: JIG, JT, JX …; Dene kutusu), alt türler, alanlar (metin / sayı+birim / evet-hayır / listeden seç; alt türe göre),
    durumlar (yeni eklenir, var olanın adı kilitli), şablon klasörleri, düzey klasörleri (yalnızca yeni türde). `turler.json`'a yazar.
3g. **Türe özel alanlar (sıradaki):** kayıt sayfasında doldurma; değerler `kayit.json` → `fields` ve notun YAML bloğuna da yazılır. kablo (konnektör A/B, uzunluk, faz kararlı, protokol, ekran, pin yapısı), jig; foto, blok şema.
    Özellikler ortak listeden etiket gibi seçilecek.
3h. Düzeyler arası aktarma (stok → proje → genel), adet / seri no, zimmet.
3d. **"+ Test koşusu":** sistem + SN seç, sıradaki tur, klasör + dolu test.md üret.
3e. **Otomatik günlük:** dosya ve durum değişikliklerinden zaman çizelgesi.
3b. **Test kalemleri:** ÇK, İşlevsel, Kalifikasyon; alt kalemler (RE102, CS114 …); her biri Başlamadı / Devam ediyor /
   Geçti / Kaldı / Gerek yok. Ana durum ürünü anlatır, uzun süren ÇK kendi satırında ilerler ("1 açık test: ÇK").
   İçeriği kullanıcıyla ayrıca konuşulacak.
4. **Envanter türleri:** "+" menüsünden Yeni jig / kablo / yazılım (tür kalıbından); alt tür (tek seçim) + etiketler
   (çoklu: SATIMO, VNA, ÇK …) + durum. Ağaçta Envanter bölümü.
5. **Seri numaraları ve tekrar eden testler:** bir stokun aynı ya da farklı SN'leri birkaç kez test edilir.
6. **Yazılım sürümleri:** bir yazılımın birden çok sürümü; git tam entegre değil, elle sürüm kaydı da olmalı.
7. **Tür düzenleyici ekranı** (Şablonlar penceresine benzer; önce mock).
8. **Genel durum özetleri:** proje, stok ve kök düzeyinde "ÇK testleri ne durumda, diğer testler, jigi olanlar" gibi
   basit özet listeleri (sağ paneldeki tablolar gibi).
9. Eski stoklara eksik alt klasörleri ekleme ("Eksikleri tamamla").
10. Kişi türü (yapıldı: Türler penceresinde roller, ekip, kişi listesi; ayrı kişi sayfası gerekmiyor, kullanıcı 12.11.2026).
11. Otomatik öneriler (karar kullanıcıda): "3 haftadır dosya değişmedi ama Testte", "kalibrasyon geçti", "SATIMO'ya sonuç geldi".

## Bekleyen işler

- **Kullanıcı notları (25.11.2026), önce hatalar:**
  - Yapıldı: proje sayfasında başka projenin araçları görünüyordu (tür sayıları da); uzun ince fotoğrafta kapak boşluğu
    (bulanık dolgu); doğrulama maddeleri sıkışıyordu (stok sayfası tek kaydırma, üst alan 380 px, özet görünüm düğmelerinin
    yanında; hedef ekran 1080p).
  - **ComboBox:** "chevron dışına tıklayınca tekrar" — KIWI veri analizine bakıldı (25.11.2026): orada kesit kutusu metin + ok
    düğmesi (yazılır, yalnız ok açar), ComboBox düzeltmesi yok; DesignKit Pusula'da daha yeni (0.3.8 / KIWI 0.3.5). Ekrana
    dokunmadan yeniden üretilemedi; kullanıcı hangi kutuda ne olduğunu gösterirse bakılacak.
  - **Stok dokümanları (yapıldı, 25.11.2026):** `01_Ortak\Dokumanlar\<stok>_<kod>_<rev>[_TASLAK]` (Ortak: Dokumanlar,
    Fotograflar, Girdiler). Rev P1–P9 = taslak aktarımı, AA, AB … BA = resmî; `_TASLAK` eki = hazırlanıyor. Durum yalnız
    dosya adından okunur (Burada yok · hazırlanıyor · taslak aktarıldı · aktarıldı · değişiklik bekliyor); zaman çizelgesine
    otomatik bir şey yazılmaz, revizyon geçmişi tutulmaz. Kod listesi Ayarlar → Şablonlar → Dokümanlar (kod, ad, stok
    öneki, grup): 261 (AB), TG ve TP dokümanı (kodu kullanıcı girecek), 032 / 132 / 133 (JY, "Yazılım dokümanları" grubu:
    birlikte aktarılır, durumları aynıysa tek çip). Yöntemi "261 dokümanı" olan gereksinim plan / rapor istemez; 261'in
    harfli revizyonu aktarılana kadar sarı; özet sayıları ve yüzde yalnız test planlı maddeler ("261 ile" ayrı).
    Sonra: TG / TP dokümanlarının gereksinim ve plan maddeleriyle bağı.
  - **Bağ / Durum / Sırala süzgeçleri:** çalışıyor; görsel olarak iyileştirilecek ve adları netleşecek.
  - **Sol liste madde 1 (yapıldı, 25.11.2026):** envanter türlerinin altı ağaçta açılmaz (araçlar gizli düğüm; aramada eşleşen
    geçici görünür, kaydı olmayan elle açılmış klasörler ağaçta kalır). Türe tıklayınca sağda liste: özet (kayıt · stoka bağlı ·
    genel), arama, Proje (Genel = hiçbir projede kullanılmayan), Stok (seçili projenin stokları), Alt tür, Durum, Temizle;
    "Bağlı olduğu" sütunu (stok, proje ya da Genel + kullanıldığı projeler). Satıra tek tıkla aracın sayfası; geri dönünce
    her liste kendi süzgeciyle gelir. Sıradaki: hepsini kapat, proje sıralama / süzgeç, genel araçlara kullanıldığı projeler.
  - **Sol liste madde 2 (yapıldı, 25.11.2026):** arama yanında ⇅ menüsü: sıra numara (sayı olarak) · son değişiklik · ad
    (proje.json adı), etiket süzgeci ("N proje gizli · göster"); ayarda hatırlanır. Proje olmayan kök klasörler sonda.
    "Hepsini kapat": seçili kaydın yolu açık kalır (yoksa seçim üst düğüme kayıp sayfa değişirdi).
  - **Sol liste madde 3 (yapıldı, 25.11.2026):** genel araçta "Kullanıldığı projeler" (Düzenle'de proje işaretleri; kayit.json
    fields → projeler). Kayıt sayfasında, listede (Genel + projeler, Proje süzgeci) ve proje sayfası Kayıtlar / İstatistik'te görünür.
  - **Alt / üst stok (yapıldı, 25.11.2026):** bileşenler üst stokta girilir (stok.json → components; "+ Bileşen" aramalı kutu,
    sağ tık "Bileşeni kaldır"); alt stokta "Kullanıldığı yerler" hesaplanır. Birden çok üst olabilir; kendisi, tekrar ve döngü
    (doğrudan / dolaylı) engellenir. Klasörler ve ağaç değişmez.
  - **Kişiler (yapıldı, 25.11.2026):** Ayarlar → "Kişiler…" Türler penceresini kişi listesiyle açar. @ önerisinde kişiler,
    stoklar, araçlar ve seçili stokun `Diger\*.msg` mail dosyaları (`[[Diger/….msg]]`, tıklanınca Outlook'ta açılır); bahsetme
    yalnız metin, iş atamaz.
  - **Sorumlular (yapıldı, 25.11.2026):** stok ve yardımcı araçta isteğe bağlı rol + kişi (Düzenle → Sorumlular, kişi # ile
    önerilir); stok.json / kayit.json → responsibles. Stok sayfasında yalnız stokun kendi sorumluları, araç sayfasında Bilgiler'de.
  - **Teknik borç (25.11.2026):** `JsonFile.Update<T>` (oku-değiştir-atomik yaz) eklendi; stok bağı, bileşen ve sorumlular onu
    kullanıyor. Kalan: RecordEdit'teki eski oku-yaz kalıpları, UI thread'inde eşzamanlı yazımlar (kısa not / bağ), süzgeç
    kutularının "Tümü" geçici çözümü.
  - **Hata avı (25.11.2026):** düzenle penceresinde genel araçtan kalan proje seçimi; sayı satırı 261 maddelerini sayıyordu;
    son değişiklik sırası her seferinde tarıyordu.
  - **Genel düzen (yapıldı, 25.11.2026; mock `docs/mock/genel_duzen.png`):** süzgeç çubuğu tek biçim (etiket kutunun içinde,
    seçili mavi, ✕ ile temizle; Bağ → "Plan / rapor", Sırala → "Sıra"); sol listede "Test yardımcı araçları" başlığı
    Projeler gibi; ana sayfa boşken özet (geciken, açık iş, değişiklik bekleyen doküman, son açılanlar); araç sayfasında
    Özellikler Süreç'in üstüne, çipler tek biçim. Ayrıca: araç sayfasında Sorumlular / Kullanıldığı projeler Süreç kartında
    (önceden yalnız modelde vardı, ekranda görünmüyordu); araç listesinde Konum sütunu genişledi. Tam takım 75 senaryo geçti.
  - Sonraya: Kiwi için s2p / s1p / S11 analizi (zorunlu değil, jpg de olabilir).

- **Durum (22.11.2026 akşam, 9cc1279):** aşağıdaki 22.11 maddelerinin hepsi bitti (elle durum, ## Rapor, eski TP/TR,
  plan / rapor ayrıntısı, rapor Tarih/Birim, kırpışma, geniş tablo), Excel'den gereksinim al + plan / rapor Excel'e aktar,
  aramalı bağ seçimi, "Sonuç yok" durumu, ÇK/EMC standardı (gereksinimde test tipi · standart · yöntem · prosedür;
  ortak liste `_TAKIP\Standartlar.md`, başlangıç 810F / 810G / 810G CHG1 / 810H / 461G — numaralar elle doğrulanmalı).
  **Sıradaki:** istatistikler (mock `docs/mock/istatistikler.png`; kullanıcının itirazı var, konuşulacak).
- **Test yardımcı aracı sayfası (22.11.2026 gece, kullanıcı kararları; kodlandı):**
  - Alanlar (turler.json sürüm 15): her türde **Kullanım amacı**. Kablaj: Boy, Ekran (ekranlı / ekransız), RF'te
    Frekans bandı, Haberleşme'de Pin yapısı, Güç'te Gerilim · Akım · Kesit (AWG). Jigin malzeme alanları (Malzeme,
    Ölçüler, Yüzey, Bağlantı, Tork, Çizim revizyonu), yazılımın Sürüm · Dil · Konum · Bağlı cihaz, cihazın Marka ·
    Seri no · Frekans · Kalibrasyon bitişi kalktı (kayit.json'daki değerler silinmez). Mock: `docs/mock/arac_ozellikleri.png`.
  - Fotoğraflar kayıt klasörünün kendisinde, iç klasör yok (yeni kayıtta fotoğraf alt klasörü açılmaz; eskisi okunur).
    Ad düzeni stoktakiyle aynı: kapak `<numara>_kapak.jpg`, şerit `1_SMA_ucu.jpg`, `2_N_ucu.jpg` … (numara sırası,
    sonrası şeritte ad olur); görünmesi istenmeyen fotoğraf numarasız bırakılır (`IMG_2041.jpg`), yalnız sayısı yazılır.
  - Not kartı: aracın notunun gövdesi salt okunur (YAML ve `## Günlük` hariç); MD düğmesi notu açar.
  - Olaylar: Günlük'e yazılır; yeni türler **Kalibrasyon** ve **Tamir / bakım** (jig / cihaz / kablajda revizyon
    Revizyon türüyle). Stoka ait aracın olayları stokun zaman çizelgesinde de görünür (zaten vardı).
  - Yazılım: fotoğraf kartı yerine **Revizyonlar** (Revizyon olayları: rev AA → AB, açıklama = eklenenler); en yenisi
    Özellikler'de "Revizyon". Ayrı sürüm alanı yok.
  - Kalibrasyon çipi (geçerlilik), kalibrasyon klasörü, "kalibrasyonu yaklaşan" listesi, yazılım ekran görüntüsü
    şeridi: gerek yok (kullanıcı).
  - Örnek veri: `tools\Ornek-Araclar.ps1 -Kok <kök>` (AB-7000-0003 altına 7 araç); `C:\PusulaDeneme`'ye eklendi.
    Senaryo: `tool-page`.
- **Hata avı (22.11.2026 gece):** Yöntem "—" hizası; eski düzenli dosyada gereksinimler taşımadan sonra okunur; aynı
  başlıklı iki Günlük girdisinde düzenle / sil doğru girdiyi bulur (`SourceOccurrence`); Excel okumada sayı artığı
  (3.1000000000000001 → 3.1), yazımda XML-geçersiz denetim karakterleri atılır; plan / rapor ayrıntısı `####` ya da
  `#etiket` satırında kesilmez; Excel aktarma, `_ARSIV` yedeği, mail kopyası, ölçüm / fotoğraf klasörü kendi yazımı
  sayılır. Performans: doğrulama dosyası stok açılışında 6 kez yerine 1 kez okunur (`VerificationText` önbelleği).
  Tam takım 67 senaryo geçti; eski 4 hata (mention-assist, record-files, split) artık görünmüyor. Senaryo: `bugfix-core`.
- **Doğrulama, yarın (22.11.2026 kararları):**
  1. **Elle durum ile hesaplanan durum ayrılsın:** gereksinimde iki ayrı bilgi; bağlardan hesaplanan durum elle
     verileni ezmesin (ikisi de görünür, hangisinin hesap olduğu belli). Şu an plan / rapor kaydında hesap elle
     yazılanın üstüne yazıyor.
  2. **`## Sonuç` → `## Rapor`:** eski dosyalarda başlık yeniden adlandırılır (yalnız başlık satırı).
  3. **Gereksinim tablosundan eski TP / TR sütunları kaldırılır** (TP açıklama, TP not, TR açıklama, TR not,
     TR sonuç, Sonuç yeri; TP no / TR no da): bilgi Plan / Rapor tablolarında. Önce Plan / Rapor tablolarında
     karşılığı olduğu doğrulanır, eksik varsa oraya kopyalanır; sonra sütun kaldırılır. Kod ve testler güncellenir.
  4. Sonra: test yardımcı araçlarına ayrıntı (kapak + numaralı foto şeridi, not, olaylar), ardından Excel dışa /
     içe aktarma.
  5. `mention-assist` testinde eski hata (Günlük penceresinde etiket kutusu bulunamıyor) incelenecek.
  Testler artık `tests\GizliKos.ps1` ile görünmez masaüstünde koşar.
- **Plan maddesi ayrıntılı görünümü (22.11.2026, manuel test sonunda yapılacak; mock → kod):** satıra tıklayınca
  açılır (gereksinimdeki gibi). İçinde: (1) bağlı gereksinimler madde no ile, yer varsa açıklamasıyla, ölçüt ve
  durum; (2) bağlı raporlar: sonuç + sonuç klasörü aç; (3) md'deki `### <TP no> · başlık` altındaki ayrıntı metni
  (salt okunur, "md'de aç"). Yöntem kutusundaki "—" hizası düzeltilecek.
- **Rapor tablosu düzeni (22.11.2026, onaylandı; manuel test sonunda):** sütun sırası TR no · Açıklama · Test planı ·
  Tarih · Birim (seri no) · **Sonuç** (en sağda) · [📁 sonuç klasörü simgesi, ✎]. "Sonucun yeri" sütunu kalkar,
  simgeye döner. md `## Rapor` tablosuna **Tarih** ve **Birim** sütunları eklenir (eski dosyalar ilk yazımda yükselir);
  rapor penceresine iki alan; testler güncellenir.
- **Pencere kırpışması (22.11.2026, manuel test):** plan / rapor penceresinde doğrulama hatasında (ör. "4.1 zaten
  var.") pencere kapanıp yeniden açılıyor; hata varsa hiç kapanmasın (PrimaryButtonClick'te iptal). Diğer
  pencerelerde de aynısı denetlenecek.
- **Md önizlemede tablo (22.11.2026, manuel test):** geniş tabloda sütunlar harf harf kırılıyor. Önizlemede tablo
  yatay kaydırılsın, hücreler en az bir kelime genişliğinde olsun (sözcük ortasından kırılmasın).
- **Eski madde bölümleri (22.11.2026, manuel test):** `## Plan` / `## Sonuç` altındaki eski `### DGÖ-5 · Kütle`
  (`%% pusula:plan … %%`) bölümleri artık tabloyla eşitlenmiyor; TP 4.6 → 4.7 sonrası bölümde hâlâ 4.6 yazıyor.
  Tablolara geçişten kalan bu bölümler ne olacak (tabloya taşınıp işaretler mi kalkar, yoksa TP no'ya göre
  ayrıntı bölümü mü olur — plan ayrıntılı görünümüyle birlikte karar).
- **Excel / DOORS akışı (22.11.2026, deneme; dal `deneme/excel-gereksinim`, push yok):** test mühendisinin akışı:
  (1) DOORS'tan alınan Excel ile gereksinim tablosu kurulur ("Excel'den al", çalışıyor); (2) Pusula'da plan maddeleri
  açılıp DGÖ'lerle eşlenir, bilgiler girilir, plan "Tamamlandı" denir; (3) plan DOORS biçiminde Excel'e verilir (biçime
  kullanıcı bakacak); (4) rapor maddeleri plandan kendiliğinden açılır (4.x → 5.x birebir, değiştirilebilir), yalnız
  Geçti / Kaldı ve ayrıntı girilir. İçe aktarma kuralları: başlık satırı TG no + Gereksinim / Açıklama / ID / Object Text
  sütunlarından tanınır (Tür, Not isteğe bağlı); Tür'ü boş ve metni numarayla başlayan satır ("3.1 Elektriksel …")
  bölümdür, madde olmaz, altındaki maddelerin grubu olur; yeni maddenin başlığı boş kalır (gereksinim metni üstte
  görünür); var olanın yalnız metin / tür / not / grubu güncellenir, durum ve bağ korunur, silme yok. Bağlı (üst
  gereksinim) ilk aktarımda yok. Kod: `Pusula.Core\Office\XlsxReader.cs` (kütüphanesiz, System.IO.Compression),
  `Testing\RequirementImport.cs`, `MainViewModel.Import.cs`, test `excel-import.ps1`. Gerçek DOORS çıktısının sütun
  adlarına göre eş adlar genişletilecek.
  **Karar (22.11.2026 öğleden sonra):** plan ve rapor maddeleri otomatik oluşturulmaz, hepsi elle (mock
  `docs/mock/plan_olustur.png` reddedildi). Yalnız gereksinimler Excel'den gelir. Test planı ve Test raporu sekmelerinde
  "Excel'e aktar" (`Office\XlsxWriter.cs`, kütüphanesiz): plan = TP no · Açıklama · Yöntem · Altyapı · Gereksinimler ·
  Gereksinim metni · Not · Test raporu; rapor = TR no · Açıklama · Plan · Gereksinimler · Tarih · Birim · Sonuç ·
  Sonuç yeri · Not; bağı eksik hücrede "—". DOORS'a kopyala-yapıştır yapılır (doğrudan aktarım değil). Uçtan uca test
  `excel-flow.ps1` (49 denetim). Bağ / Durum kutusu: açıkken ince mavi kenar, ikinci tık kapatır.
  **Bağ seçimi (22.11.2026, mock `bag_secimi.png`):** plan / rapor penceresinde "Kullanan stoklar" gibi: bağlı satırlar
  (başlık yoksa kısaltılmış metin, ✕) + "+ Gereksinim ekle / Plan ekle" aramalı kutu (bölümlere ayrılı, seçince açık kalır).
  **Rapor:** "sonucu girilmedi" ayrı uyarı ve "Sonuç yok" hapı; satıra tıklayınca ayrıntı (planlar, gereksinimler,
  md'deki `### TR no` ayrıntısı, sonuç klasörü).
  **Standart (22.11.2026, mock `standart_secimi.png`):** gereksinimde elle: Test tipi (İşlevsel / ÇK / EMC / Diğer),
  ÇK / EMC'de Standart + Yöntem (509.5 · Tuz sisi) + Prosedür. md sütunları Tip · Standart · Standart yöntemi · Prosedür.
  Ortak liste `_TAKIP\Standartlar.md` (`## MIL-STD-810G` / `- 509.5 · Tuz sisi`); dosya yokken başlangıç listesi
  (810G ve 461G yöntemleri, bilgiden yazıldı — kullanıcı kendi nüshasıyla doğrulamalı; 810F / H gibi sürümler
  eklenebilir); listede olmayan yöntem yazılırsa listeye eklenir. Planda ayrı seçim yok: bağlı gereksinimlerden okunur
  (turuncu çip), Excel'e aktarımda "Standart" sütunu. Aramalı seçim kutusu `SearchPickBox` (tip, standart, yöntem,
  plan altyapısı). Test `standard.ps1`.
  Raporu yok / sonucu girilmedi ayrımı yapıldı (ayrı uyarı parçaları; excel-flow testi).
- **Standart kütüphanesi (22.11.2026, sonraya; kullanıcı isteği):** ÇK / EMC test maddeleri için standartlar
  (MIL-STD-810G, MIL-STD-461G …) yüklenir; plan / gereksinim maddesinde doğrudan standart ve yöntem seçilebilir.
- **Outlook / mail (22.11.2026 kullanıcı kararı):** yalnız `.msg` dosyası tutulur (kayda eklenir, `Diger\` klasörüne
  kopyalanır, ✉ ile Outlook'ta açılır). Mailin içi okunmaz, pencere maille doldurulmaz; Outlook COM bağlantısı ve mail
  istatistikleri yapılmaz (gereksiz karmaşıklık). Aşağıdaki "Outlook'tan veri çekme" ve "Mail istatistikleri" iptal.
- **Outlook'tan veri çekme (21.11.2026, İPTAL 22.11.2026):** klasik Outlook'a COM ile bağlanılır,
  Pusula ağa çıkmaz (Exchange'e Outlook gider). (1) Kayıt penceresinde Ortam = Outlook iken "Outlook'ta seçili maili al":
  konu → başlık, tarih, gönderen + alıcılar → katılımcı, gövde başı → notlar; mail `.msg` ve seçilen ekler
  `03_Testler\Yazismalar\tarih_konu\` klasörüne; diskteki `.msg` de seçilebilir. (2) Adres defterinden kişi ekleme
  (ad, e-posta, unvan, birim, ofis, telefon, yönetici) ve dağıtım listesinden ekip (üyeleriyle); otomatik eşitleme yok.
  Yalnız klasik Outlook (yeni Outlook COM desteklemez); şirket ayarına göre Outlook güvenlik uyarısı çıkabilir
  (kullanıcı soracak). Deneme betiği: `tools\OutlookDeneme.ps1` (yalnız okur). Mock: `docs/mock/outlook.png`.
  Teams elle girilir; takvim bağlama yok (bulut gerektirir).
- **Mail dosyası okuma (21.11.2026, deneme aracı, uygulamaya bağlı değil):** `tools\MailOku\` (`MailOku.cs` +
  `MailOku.ps1`). `.msg` Windows'un ole32'siyle (OLE bileşik dosya) okunur; Outlook, lisans ya da kütüphane gerekmez.
  `.eml` (Gmail "Mesajı indir") kütüphanesiz MIME çözümüyle okunur: kodlanmış konu, quoted-printable / base64,
  multipart, ekler. Çıktı: başlık (RE:/FW:/YNT: atılır), tarih, katılımcı `@Ad_Soyad`, ekip (`.msg`'de dağıtım listesi;
  `.eml`'de tamamı büyük harfli ad), notlar (selam satırları atlanır), ekler. Örnekle: `MailOku.ps1 -Ornek`;
  gerçek dosyayla: `MailOku.ps1 -Dosya mail.msg -EkKlasoru ekler`. Gerçek bir Outlook `.msg` ile henüz denenmedi.
  Uygulamada (21.11.2026): Toplantı (ortam Outlook) ve Karar kaydına `.msg` eklenir; pencere maille doldurulmaz
  (kullanıcı kararı), dosya stokun en dışındaki `Diger\` klasörüne kendi adıyla kopyalanır (aynı ad varsa `_2`),
  notta `mail: Diger/….msg`; kartta ✉ Outlook'ta açar.
- **Mail istatistikleri (21.11.2026, İPTAL 22.11.2026):** mailler Outlook'ta okunur; Pusula `Diger\*.msg` dosyalarını arka planda
  yalnız okuyup sayar, nota yazmaz. Türetilmiş özet `_TAKIP\mail-ozet.json` (her an yeniden üretilebilir). Fikirler:
  (1) yanıt süresi (kişi / birim ortalaması, konuşma zincirinden), (2) kim bekliyor (son maili kim attı; "6 gündür
  yanıt yok"), (3) kimle ne kadar yazışılıyor, (4) talep bağı (konu / gövdede talep no geçen mail talebe bağlanır),
  (5) konu yoğunluğu (madde, revizyon başına mail), (6) dolaşan ekler. İlk adım önerisi: 2 ve 4.

- **İnceleme sonrası kalanlar (08.11.2026):** stok seçilince 5 okuma artık aynı anda yapılıyor; tek taramada birleştirmek ileride. Kısa not / bağ / not oluşturma UI thread'inde eşzamanlı dosya yazıyor. JSON oku-değiştir-yaz kalıbı 4 yerde
  tekrar (`JsonFile.Update<T>` yapılmalı). Süzgeç kutularının "Tümü" düzeltmesi geçici çözüm.

- **md penceresi (12.11.2026 kontrol):** renklendirme ve satır numarası (AvalonEdit), kısa not / Olay kutularında @ listesi,
  görev kutusu tıklamasının Ctrl+Z ile geri alınması yapılmış; yalnız görünüm açıkken de Ctrl+Z / Ctrl+Y çalışır.
- **12.11.2026 yapılanlar:** geciken işler stok sayfasının Dikkat bölümünde; stok ve proje etiketleri (Düzenle penceresi,
  json'da tags, ana notun YAML'ında tags, başlıkta çip, ağaçta #etiket araması); beklenmeyen hata uygulamayı kapatmaz
  (`%LOCALAPPDATA%\Pusula\hatalar.log`); son açılanlar gecikmeli yazılır; proje sayıları arka planda okunur.
- **Jenerik test dokümanı üretme:** yeni stok açınca (ya da istenince) stok no/ad dolu test planı vb. doküman.
  Şablon modüllerine "başlangıç dosyaları" eklenebilir. DOCX üretimi için kullanıcının
  `C:\Projeler\Aktif\Divit` projesine bakılacak (orada docx üretimi var).
- **Birim türüne göre otomatik md (11.11.2026, ileride):** stok açılınca birim türüne göre ana md hazır gelsin.
  Kullanıcı önce gerekleri belirleyecek, sonra konuşulacak. Yazılım numarası (YZ) ile geliştirme stoku (JY) arasında
  çakışma yok (kullanıcı, 11.11.2026).
- **Test süreci / kalifikasyon (11.11.2026, konuşuldu, kod yok; kullanıcı gerçek tabloları inceleyecek):**
  - Zincir: TG (test gereksinimi; temel / türetilmiş gerek; yoksa tasarımcının docx'i, ikisi birden de olabilir) →
    TP (test planı maddesi; doğrulama yöntemi: muayene, denetim, analiz, gösterim, test) → TR (sonuç ve kanıt yeri).
    Açıklar: TP'siz TG, TR'siz TP, kalan TR (bildirim).
  - Test türleri sınırsız (ÇK, EMI/EMC, İşlevsel …); her türün altında testler (CE102, titreşim …), onların altında
    doğrulama maddeleri. Gerekler tek tek girilebilmeli (örn. MIL-STD maddeleri).
  - Her tür için adım zinciri (soru → cevap → kanıt → sonraki): gerekli mi, kaynak doküman, test planı, prosedür
    (teknisyen için), yardımcı araç (jig / kablo / yazılım; talep, üretim, teslim, deneme), lab talebi (no), birim teslimi,
    testler (yapılıyor / geçti / kaldı / analizle), sonuç yeri, DOORS test raporu, müşteri raporu. İşlevselde sonunda
    üretime devir (üretim test dokümanı, araçların üretime teslimi, tasarımcı spek görüşü). Gerekmeyen adım eksik sayılmaz.
  - İlk adım mock'u: `docs/mock/kalifikasyon-gerekli.png` (Kalifikasyon sekmesinde "Test kapsamı": Gerekli / Gerekmiyor
    + gerekçe). Genel görünüm: `docs/mock/surec-ornek.png`.
- `00_Proje_Geneli` iç yapısı henüz kararlaştırılmadı (öneri: Sartname, Toplantilar, Yazismalar, Planlama).
- Şablonda kullanılmayan modülü kütüphaneden tamamen silme yok (şimdilik listede kalır).
- Test kütüphanesi seçilmedi (xUnit / NUnit / MSTest); `Pusula.Tests` projesi yok. Şimdilik denemeler
  geçici klasörde PowerShell betikleriyle yapılıyor (şablon düzenleyici 32, özet 13, durum 28, işaretleme 18, test koşuları 17, envanter 22 + 26, kayıt dosyaları 18, stok envanteri 13, türler 14, stok kapağı 7, arama 15, kısa not 13, olay notları 9 kontrol).
- Git kullanıcı adı bu makinede tanımlı değil; commit'ler tek seferlik `-c user.name/user.email` ile atılıyor.

## Çalıştırma

```
MSBuild Pusula.sln /p:Configuration=Debug /p:Platform=x64
src\Pusula.Wpf\bin\x64\Debug\net48\Pusula.exe
```

Deneme kökü: `C:\PusulaDeneme`.

- Ayarlar penceresi (⚙): kök klasör (üst çubuktan taşındı), tema (KIWI'nin 9 teması, ayarlar.json'da `theme`), Şablonlar / Türler / Dosya türleri düğmeleri. Sol ağaçta envanter ayrı bölmede, projelerin hemen altında.

- Yapay zeka rehberi: docs/YAPAY_ZEKA_REHBERI.md (gerçek veri kökünde CLAUDE.md olarak durur; hatalar _PUSULA\GERI_BILDIRIM.md'ye). İlk açılışta kök yoksa Ayarlar kendiliğinden açılır.

- Testler depoda: tests\Calistir.ps1 (-Derle), senaryolar tests\Senaryolar, deneme verisi tests\Veri (her senaryoya geçici kopya). Kök değişince eski açık düğümler sayılmıyor (Envanter kapalı açılıyordu).

- Uygulama ikonu: soyut ibre (B), tools\Ikon-Ciz.ps1 üretir (src\Pusula.Wpf\Assets\pusula.ico + pusula-logo.png); exe ve bütün pencerelerde.

- Karar (09.11): numara (klasör adı) uygulamadan değiştirilmez. Gerekirse klasör elle yeniden adlandırılır ya da kayıt takipten çıkarılıp / silinip yeniden eklenir.

- Karar (09.11): olaylar ayrı dosya değil, ana notun en altındaki `## Günlük` bölümüne yazılır (en yeni üstte). Uygulama yalnız bu bölümün başına ekler, notun geri kalanına dokunmaz (CLAUDE.md 'gövdeye dokunulmaz' kuralının kullanıcı onaylı istisnası).

- Karar (09.11): **Seri numarası kayıt olarak tutulmaz.** SN'ler test klasörü adlarından okunur (`03_Test_Sonuclari\<SİSTEM>\<SN>_T<tur>`, kalifikasyon da). `SNP…` = prototip, `SN…` = üretim birimi. Bir SN birden çok sistemde olabilir; her SN'nin her turu ayrı klasör. Genel'de **Seri numaraları** kartı (SN × sistem, son tur ve sonuç, tıklanınca not). Yetmezse ileride `stok.json`'da ayrı SN listesi (elle ekleme, yeniden adlandırma / eski adlar) eklenir.

