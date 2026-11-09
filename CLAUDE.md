# Pusula — çalışma kuralları

Pusula, bir kök dizindeki klasörleri tarayıp ağaç olarak gösteren ve proje/stok/jig/kişi gibi
kayıtları **takip eden** yerel (çevrimdışı) bir masaüstü uygulamasıdır. Notlar Obsidian'da yazılır,
uygulama takip eder.

- Tasarım ve plan: [docs/PLAN.md](docs/PLAN.md). **§0 Kararlar** bölümü planın geri kalanından önce gelir.
- Arayüz fikirleri (birebir tasarım değil): `docs/mock/*.png`.

## Çalışma biçimi

- **Yavaş ve adım adım.** Her adım küçük olur: derlenir, çalışır, ayrı commit edilir.
- Büyük bir adıma (yeni faz, yeni proje, yeni bağımlılık) geçmeden önce kullanıcıya sor.
- Açık sorular toplu sorulur, tek mesajda.

## Teknoloji

- **.NET Framework 4.8 + WPF, x64.** Ortak ayarlar `Directory.Build.props` içinde.
- **Arayüz: DesignKit.Wpf.** DLL `Dependencies/DesignKit/` altında (bkz. oradaki README).
  - Renk, boşluk, köşe, yazı tipi **sabit yazılmaz**; DesignKit token'ları `DynamicResource` ile kullanılır
    (`SurfaceBaseBrush`, `LayerBrush`, `TextPrimaryBrush`, `PadCard`, `BodyTextBlockStyle` …).
  - Önce standart WPF kontrolü (DesignKit onu zaten temalar), yoksa DesignKit özel kontrolü.
    Katalog: `C:\Projeler\Aktif\DesignKit\USAGE.md`.
- **Bağımlılık:** NuGet restore yok. Gereken derlenmiş kütüphane `Dependencies/<Ad>/` altına konur,
  `HintPath` ile referans verilir, README'ye kaynak/sürüm/lisans yazılır. **Her yeni paket için
  lisans ve gerekçe kullanıcıya sorulur.**

## Çözüm yapısı

```
Pusula.sln
├── src/Pusula.Core   ← alan modeli, ayrıştırıcılar, tarama, kurallar (UI referansı YOK)
└── src/Pusula.Wpf    ← WPF arayüz (MVVM, elle yazılmış), çıktı: Pusula.exe
```

Testler: `tests\` (PowerShell senaryoları, kütüphanesiz; bkz. [tests/README.md](tests/README.md)).
Her adımdan sonra `powershell -NoProfile -ExecutionPolicy Bypass -File tests\Calistir.ps1 -Derle` hepsi geçmeli.

İleride: `Pusula.Ai`.

## Derleme

Visual Studio 2022 MSBuild ile:

```
MSBuild Pusula.sln /p:Configuration=Debug /p:Platform=x64
```

`dotnet build Pusula.sln -c Debug` de çalışır. Uyarılar hata sayılır; derleme 0 uyarıyla bitmeli.

## Kod kuralları

- Tanımlayıcılar **İngilizce**, XML doc yorumları **Türkçe**. Her public tür ve üyede `<summary>` olur.
- **Allman** parantez, her dalda `{}`.
- **String interpolation (`$"..."`) kullanılmaz.** `string.Format` ya da birleştirme kullanılır.
- Kullanıcıya görünen tüm metinler `Properties/Resources.resx` içinde; kodda/XAML'de sabit metin yok.
  `Properties/Resources.cs` elle yazılır (komut satırı MSBuild'de de derlensin diye); yeni anahtar
  eklenince ona da özellik eklenir.
- **Fody, CommunityToolkit.Mvvm, PolySharp kullanılmaz.** `ViewModelBase`, `RelayCommand` elle yazılır.
- `async/await`, `CancellationToken`, `IProgress<T>`; UI thread'i bloklanmaz.
- `Path.Combine`, uzun yol farkındalığı, büyük/küçük harf duyarsız yol karşılaştırması.
- Dosyalar **BOM'lu UTF-8, CRLF** (`.editorconfig`).

## Değişmez ilkeler

- **Klasör = yer, JSON = bilgi.** Klasör ağacı JSON'da tutulmaz, diskten taranır.
- Uygulama kullanıcının dosyalarını **asla silmez**. Göç **kopyalar**, taşımaz.
- Obsidian notlarının **gövdesine dokunulmaz** (yalnızca YAML bloğu yönetilir). Tek istisna: ana notun en altındaki `## Günlük` bölümünün başına olay **eklenir** (kullanıcı kararı, 09.11.2026); mevcut satırlar değiştirilmez.
- Dosya yazımı atomik: geçici dosya + yeniden adlandırma.
- Bulut yok, telemetri yok, dış ağ çağrısı yok.
- Karar kodla, öneri yapay zekayla, onay kullanıcıyla.
