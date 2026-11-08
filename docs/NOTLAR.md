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

1. ~~Sağ panel özeti~~ (yapıldı).
2. **Durum:** stoka basit bir durum (örn. Planlandı / Testte / Bitti). Ayrıntıya girmeden.
3. **Genel durum özetleri:** proje, stok ve kök düzeyinde "ÇK testleri ne durumda, diğer testler, jigi olanlar" gibi
   basit özet listeleri (sağ paneldeki tablolar gibi).
4. Eski stoklara eksik alt klasörleri ekleme ("Eksikleri tamamla").
5. Envanter (jig, kablo, cihaz) ve Kişi türleri.

## Bekleyen işler

- **Jenerik test dokümanı üretme:** yeni stok açınca (ya da istenince) stok no/ad dolu test planı vb. doküman.
  Şablon modüllerine "başlangıç dosyaları" eklenebilir. DOCX üretimi için kullanıcının
  `C:\Projeler\Aktif\Divit` projesine bakılacak (orada docx üretimi var).
- İkonlar: yazı tipi ikonları yerine kendi vektör (XAML) ikonları — kullanıcı "daha iyi olabilir" dedi.
- `00_Proje_Geneli` iç yapısı henüz kararlaştırılmadı (öneri: Sartname, Toplantilar, Yazismalar, Planlama).
- Şablonda kullanılmayan modülü kütüphaneden tamamen silme yok (şimdilik listede kalır).
- Test kütüphanesi seçilmedi (xUnit / NUnit / MSTest); `Pusula.Tests` projesi yok. Şimdilik denemeler
  geçici klasörde PowerShell betikleriyle yapılıyor (şablon düzenleyici 32, özet 13 kontrol).
- Git kullanıcı adı bu makinede tanımlı değil; commit'ler tek seferlik `-c user.name/user.email` ile atılıyor.

## Çalıştırma

```
MSBuild Pusula.sln /p:Configuration=Debug /p:Platform=x64
src\Pusula.Wpf\bin\x64\Debug\net48\Pusula.exe
```

Deneme kökü: `C:\PusulaDeneme`.
