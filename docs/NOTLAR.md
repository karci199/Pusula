# Çalışma notları

Son güncelleme: 08.11.2026. Dal: `claude/pusula-project-setup-c6ce02` (push edilmedi).

## Şu an çalışanlar

- Kök klasör seçimi (Gözat), son seçilen kök hatırlanır (`%LOCALAPPDATA%\Pusula\ayarlar.json`).
- Solda ağaç: projeler ve altındaki stoklar; türe göre renkli ikon.
- Ağaç başlığında **+** menüsü (Yeni stok · proje, Yeni proje), ⚙ Şablonları düzenle, Yenile.
- Sağ tık: Yeni stok, Modül ekle, Klasörde aç, Yenile. Kısayollar: Ctrl+N, Ctrl+Shift+N, F5.
- Yeni proje: `<no>_<ad>` + gizli `_TAKIP\proje.json` + boş `00_Proje_Geneli`.
- Yeni stok: `<stokNo>` + gizli `_TAKIP\stok.json` + şablondan klasörler.
- Şablonlar kökte gizli `_PUSULA\sablon.json` (sürüm 2) içinde; ekipte ortak.
- Şablon düzenleyici penceresi (ilk sürüm).

## Stok klasör yapısı (kararlaştırılan)

```
00_Diger             her stokta
01_Dokumanlar        her stokta
02_Test_Planlari     modül  (ATD/KIWI, test prosedürleri)
03_Test_Sonuclari    her stokta  (VNA, SATIMO\Ham, SATIMO\Analiz, Yankisiz, CK)
04_Kalifikasyon      modül  (01_Isterler, 02_Test_Plani, 03_Test_Sonuclari, 04_Rapor)
05_Jigler            modül  (Mekanik, Kablaj)
06_Yazilimlar        modül
07_Fotograflar       her stokta
08                   boş (ileride, örn. Analiz)
_ARSIV               her stokta
```

Kural: kalifikasyona ait her şey `04_Kalifikasyon` altında; geri kalanı türüne göre. Numaralar sabit.

## Yarın: şablon düzenleyiciyi yeniden kurgula

Kullanıcı geri bildirimi:

- **Sol liste şablonlar olsun**, modüller değil. Önce "Şablon ekle" gelir.
- Seçili şablonun sağında modülleri listelenir; **"Modül ekle"** ile modül seçilip şablona eklenir (çıkarılabilir).
- Modül sayfasındaki **"Şablonlarda" onay kutuları kalkar** (yukarıdaki düzenle gereksizleşir).
- **"Şablon ekle" düğmesinin yeri yanlış**; sol listenin başlığına taşınmalı.
- **Görsellik iyileştirilmeli.**
- Açık soru: modül tanımı (ad, numara, alt klasörler) nerede düzenlenecek? Şablon içinden mi, ayrı bir "Modüller" bölümünden mi? Önce mock çiz, onay al, sonra yaz.

## Bekleyen işler

- İkonlar: yazı tipi ikonları yerine kendi vektör (XAML) ikonları — kullanıcı "daha iyi olabilir" dedi.
- Var olan bir modüle sonradan eklenen alt klasörler eski stoklara gelmiyor ("Modül ekle" yalnızca eksik modülü ekliyor). Gerekirse "Eksikleri tamamla" komutu.
- `00_Proje_Geneli` iç yapısı henüz kararlaştırılmadı (öneri: Sartname, Toplantilar, Yazismalar, Planlama).
- Test kütüphanesi seçilmedi (xUnit / NUnit / MSTest); `Pusula.Tests` projesi yok. Şimdilik denemeler geçici klasörde betikle yapılıyor.
- Tür listesi: ilk sürümde Proje, Stok, Envanter öğesi (jig/kablo/cihaz), Kişi önerildi; Envanter ve Kişi henüz yok.
- Git kullanıcı adı bu makinede tanımlı değil; commit'ler tek seferlik `-c user.name/user.email` ile atılıyor.

## Çalıştırma

```
MSBuild Pusula.sln /p:Configuration=Debug /p:Platform=x64
src\Pusula.Wpf\bin\x64\Debug\net48\Pusula.exe
```

Deneme kökü: `C:\PusulaDeneme`.
