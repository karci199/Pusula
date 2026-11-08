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
- Varsayılan şablonda `03_Test_Sonuclari` altında yalnızca sistemler (VNA, SATIMO, Yankisiz, CK); `SN01\Ham`, `SN01\Analiz` ölçümle açılır.

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
3f. **Tür tanımlama ekranı:** ikonlu tür listesi, kilitli genel alanlar, türe özel alanlar (metin, sayı, tarih, seçim,
    evet/hayır; birim), alt türler, durumlar, numara biçimleri, düzey klasörleri.
3g. **Türe özel alanlar:** kablo (konnektör A/B, uzunluk, faz kararlı, protokol, ekran, pin yapısı), jig; foto, blok şema.
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
10. Kişi türü.
11. Otomatik öneriler (karar kullanıcıda): "3 haftadır dosya değişmedi ama Testte", "kalibrasyon geçti", "SATIMO'ya sonuç geldi".

## Bekleyen işler

- **Jenerik test dokümanı üretme:** yeni stok açınca (ya da istenince) stok no/ad dolu test planı vb. doküman.
  Şablon modüllerine "başlangıç dosyaları" eklenebilir. DOCX üretimi için kullanıcının
  `C:\Projeler\Aktif\Divit` projesine bakılacak (orada docx üretimi var).
- İkonlar: yazı tipi ikonları yerine kendi vektör (XAML) ikonları — kullanıcı "daha iyi olabilir" dedi.
- `00_Proje_Geneli` iç yapısı henüz kararlaştırılmadı (öneri: Sartname, Toplantilar, Yazismalar, Planlama).
- Şablonda kullanılmayan modülü kütüphaneden tamamen silme yok (şimdilik listede kalır).
- Test kütüphanesi seçilmedi (xUnit / NUnit / MSTest); `Pusula.Tests` projesi yok. Şimdilik denemeler
  geçici klasörde PowerShell betikleriyle yapılıyor (şablon düzenleyici 32, özet 13, durum 28, işaretleme 18, test koşuları 17, envanter 22 + 26, kayıt dosyaları 18, stok envanteri 12 kontrol).
- Git kullanıcı adı bu makinede tanımlı değil; commit'ler tek seferlik `-c user.name/user.email` ile atılıyor.

## Çalıştırma

```
MSBuild Pusula.sln /p:Configuration=Debug /p:Platform=x64
src\Pusula.Wpf\bin\x64\Debug\net48\Pusula.exe
```

Deneme kökü: `C:\PusulaDeneme`.
