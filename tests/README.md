# Testler

Pusula'nın senaryo testleri. Her senaryo derlenmiş `Pusula.exe` ve `Pusula.Core.dll`'i yükler, çekirdeği doğrudan
çağırır ya da ana pencereyi **ekran dışında** açıp görünüm modeli üzerinden dener. Ekran görüntüsü de alır.
Test kütüphanesi (MSTest/xUnit…) yoktur; NuGet gerekmez.

## Çalıştırma

```
powershell -NoProfile -ExecutionPolicy Bypass -File tests\Calistir.ps1 -Derle
```

- `-Derle`: önce çözümü derler (VS MSBuild, yoksa `dotnet build`).
- `-Filtre search`: yalnız adı uyan senaryolar (`inv*` gibi kalıp da olur).
- `-Yapilandirma Release`.

Bir HATA ya da çökme varsa çıkış kodu 1 olur. Ekran görüntüleri `tests\cikti\` altına düşer (depoya girmez).

## Yapı

```
tests\
├── Calistir.ps1        ← koşturucu
├── _Ortak.ps1          ← ortak yardımcılar: Check, Pump, Wait, Snap, FindNode, Visible
├── Senaryolar\*.ps1    ← her biri ayrı süreçte (STA) çalışır
└── Veri\
    ├── PusulaDeneme\   ← örnek kök (proje, stoklar, envanter, notlar, olaylar)
    └── Ornek\AB-7000-0002\ ← "stok olarak işaretle" senaryosu için işaretsiz örnek
```

- Her senaryoya `Veri\PusulaDeneme`'nin **geçici bir kopyası** verilir (`$Data`). Senaryolar birbirini ve
  kullanıcının verisini etkilemez.
- `%LOCALAPPDATA%\Pusula\ayarlar.json` koşu başında yedeklenir, sonunda geri yazılır.
- Büyük ölçüm dosyaları (`*.trx`) depoya alınmaz.

## Yeni senaryo

```powershell
param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
. (Join-Path $PSScriptRoot '..\_Ortak.ps1')

"--- 1. Başlık"
Check 'açıklama' ($deger -eq 3) ('bulunan: ' + $deger)
```

Pencere gerekiyorsa mevcut bir senaryonun (ör. `split.ps1`) başını örnek alın: `DispatcherSynchronizationContext`,
`App.InitializeComponent()`, pencere `Left = -20000`, `$vm.RootPath = $Data`. Dosyalar BOM'lu UTF-8 + CRLF.

## İleride

Gerekirse gerçek bir test projesine (`Pusula.Tests`) taşınır; kütüphane seçimi ve lisansı ayrıca kararlaştırılır.
