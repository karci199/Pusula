# Çalışma notları

Son güncelleme: 08.11.2026. Dal: `claude/pusula-project-setup-c6ce02` (push edilmedi).

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
  Düzeyler: Genel `<kök>\ENVANTER\Kablolar|Jigler`, Proje `<proje>\00_Proje_Geneli\Envanter\…`, Stok `<stok>\05_Kablolar|05_Jigler`.
  Kayıt: `<numara>_<tanım>\_TAKIP\kayit.json` (tür, numara, tanım, alt tür, not, türe özel alanlar, olaylar).
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

## Stok klasör yapısı (kararlaştırılan)

```
00_Diger             temel
01_Dokumanlar        temel
02_Test_Planlari     modül  (ATD/KIWI, test prosedürleri)
03_Test_Sonuclari    temel  (VNA, SATIMO\Ham, SATIMO\Analiz, Yankisiz, CK)
04_Kalifikasyon      modül  (01_Isterler, 02_Test_Plani, 03_Test_Sonuclari, 04_Rapor)
05_Jigler            modül  (Mekanik, Kablaj)
06_Yazilimlar        modül
07_Fotograflar       temel
08                   boş (ileride, örn. Analiz)
_ARSIV               temel
```

"Temel" = yeni şablonda hazır gelen modül; "her stokta" kavramı kaldırıldı, her şablon kendi listesini tutar.
Kural: kalifikasyona ait her şey `04_Kalifikasyon` altında; geri kalanı türüne göre. Numaralar sabit.

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

