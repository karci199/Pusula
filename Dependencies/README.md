# Dependencies

Repoya kopyalanmış derlenmiş kütüphaneler. NuGet restore gerekmez; projeler `HintPath` ile referans verir.
Güncellerken bu tabloyu da güncelle.

| Kütüphane | Sürüm | Kaynak | Lisans |
|---|---|---|---|
| `DesignKit/DesignKit.Wpf.dll` (+ `.xml`) | 0.3.4 (`0.3.4+30db891`) | `C:\Projeler\Aktif\DesignKit`, commit `30db891`, `dotnet build src\DesignKit.Wpf\DesignKit.Wpf.csproj -c Release` | İç kütüphane |

## DesignKit'i güncelleme

1. DesignKit reposunu temiz bir klasöre klonla (asıl klasörde derleme yapma).
2. `src\DesignKit.Wpf\DesignKit.Wpf.csproj` projesini Release derle.
3. `bin\Release\net48\DesignKit.Wpf.dll` ve `.xml` dosyalarını buraya kopyala.
4. Yukarıdaki sürüm ve commit bilgisini güncelle.

SHA-256 (`DesignKit.Wpf.dll`): `041277C9E117D6F3279A1ADE1CF66D4D8FEECCC5E7DCB76A3411D2C15682DEC1`
