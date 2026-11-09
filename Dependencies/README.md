# Dependencies

Repoya kopyalanmış derlenmiş kütüphaneler. NuGet restore gerekmez; projeler `HintPath` ile referans verir.
Güncellerken bu tabloyu da güncelle.

| Kütüphane | Sürüm | Kaynak | Lisans |
|---|---|---|---|
| `DesignKit/DesignKit.Wpf.dll` (+ `.xml`) | 0.3.4 (`0.3.4+30db891`) | `C:\Projeler\Aktif\DesignKit`, commit `30db891`, `dotnet build src\DesignKit.Wpf\DesignKit.Wpf.csproj -c Release` | İç kütüphane |
| `NuGet/microsoft.netframework.referenceassemblies.net48.1.0.3.nupkg` | 1.0.3 | nuget.org, Microsoft | MIT |

## DesignKit'i güncelleme

1. DesignKit reposunu temiz bir klasöre klonla (asıl klasörde derleme yapma).
2. `src\DesignKit.Wpf\DesignKit.Wpf.csproj` projesini Release derle.
3. `bin\Release\net48\DesignKit.Wpf.dll` ve `.xml` dosyalarını buraya kopyala.
4. Yukarıdaki sürüm ve commit bilgisini güncelle.

SHA-256 (`DesignKit.Wpf.dll`): `041277C9E117D6F3279A1ADE1CF66D4D8FEECCC5E7DCB76A3411D2C15682DEC1`

## .NET Framework 4.8 başvuru derlemeleri

Derleme için makinede .NET 4.8 "targeting pack" kurulu olması gerekmesin diye paket burada durur (bazı Visual Studio kurulumlarında yok; restore internet ister). `Directory.Build.props` paketi açıkça kullanır, kökteki `nuget.config` yalnız bu klasörü kaynak sayar. Yalnız derlemede kullanılır, `Pusula.exe` ile dağıtılmaz.

SHA-256: `8A7E348538E7EB91351696911689F49E3D4F63F8BAB517432BBE159B8B1104A2`
