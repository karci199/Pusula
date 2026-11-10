# Dependencies

Repoya kopyalanmış derlenmiş kütüphaneler. NuGet restore gerekmez; projeler `HintPath` ile referans verir.
Güncellerken bu tabloyu da güncelle.

| Kütüphane | Sürüm | Kaynak | Lisans |
|---|---|---|---|
| `DesignKit/DesignKit.Wpf.dll` (+ `.xml`) | 0.3.6 | `C:\Projeler\Aktif\DesignKit`, 30db891 + ContentDialog `DialogWidth` + `TextBoxHelper.PlaceholderText` (10.11.2026), `MSBuild src\DesignKit.Wpf\DesignKit.Wpf.csproj /p:Configuration=Release` (restore yok) | İç kütüphane |
| `NuGet/microsoft.netframework.referenceassemblies.net48.1.0.3.nupkg` | 1.0.3 | nuget.org, Microsoft | MIT |
| `Markdig/Markdig.dll` (+ `.xml`) | 0.37.0 (`lib/net462`) | nuget.org `Markdig` 0.37.0, Alexandre Mutel, github.com/xoofx/markdig | BSD-2-Clause |
| `SystemMemory/System.Memory.dll` | 4.5.5 (derleme 4.0.1.2, `lib/net461`) | nuget.org, Microsoft (Markdig'in .NET 4.x bağımlılığı) | MIT |
| `SystemMemory/System.Buffers.dll` | 4.5.1 (derleme 4.0.3.0, `lib/net461`) | nuget.org, Microsoft (System.Memory bağımlılığı) | MIT |
| `SystemMemory/System.Runtime.CompilerServices.Unsafe.dll` | 4.5.3 (derleme 4.0.4.1, `lib/net461`) | nuget.org, Microsoft (System.Memory bağımlılığı) | MIT |
| `SystemMemory/System.Numerics.Vectors.dll` | 4.5.0 (derleme 4.1.4.0, `lib/net46`) | nuget.org, Microsoft (System.Memory bağımlılığı) | MIT |

## Markdig (md not penceresi)

Gerekçe: md'yi CommonMark kurallarına göre doğru ağaca çevirir (başlık, iç içe liste, tablo, görev kutusu, kod, alıntı);
görünüm Pusula'da WPF ile, DesignKit renkleriyle çizilir. Obsidian'a özgü yazımlar (`[[bağ]]`, `![[resim]]`, `> [!not]`, `==vurgu==`)
`Pusula.Core.Markdown` içinde küçük eklentilerle desteklenir. Kullanıcı onayı: 10.11.2026 ("B'den git").
System.Memory ailesi birbirine uyan sürümlerdir; bağlama yönlendirmesi (binding redirect) gerekmez.

SHA-256:
- `Markdig.dll`: `194587CDF19EC17EEE6F69461B27A7D1F542C96B78FD468A2025F3269043FB69`
- `System.Memory.dll`: `BF3FB84664F4097F1A8A9BC71A51DCF8CF1A905D4080A4D290DA1730866E856F`
- `System.Buffers.dll`: `ACCCCFBE45D9F08FFEED9916E37B33E98C65BE012CFFF6E7FA7B67210CE1FEFB`
- `System.Runtime.CompilerServices.Unsafe.dll`: `66409F670315AFE8610F17A4D3A1EE52D72B6A46C544CEC97544E8385F90AD74`
- `System.Numerics.Vectors.dll`: `1D3EF8698281E7CF7371D1554AFEF5872B39F96C26DA772210A33DA041BA1183`

## DesignKit'i güncelleme

1. DesignKit reposunu temiz bir klasöre klonla (asıl klasörde derleme yapma).
2. `src\DesignKit.Wpf\DesignKit.Wpf.csproj` projesini Release derle.
3. `bin\Release\net48\DesignKit.Wpf.dll` ve `.xml` dosyalarını buraya kopyala.
4. Yukarıdaki sürüm ve commit bilgisini güncelle.

SHA-256 (`DesignKit.Wpf.dll`): `041277C9E117D6F3279A1ADE1CF66D4D8FEECCC5E7DCB76A3411D2C15682DEC1`

## .NET Framework 4.8 başvuru derlemeleri

Derleme için makinede .NET 4.8 "targeting pack" kurulu olması gerekmesin diye paket burada durur (bazı Visual Studio kurulumlarında yok; restore internet ister). `Directory.Build.props` paketi açıkça kullanır, kökteki `nuget.config` yalnız bu klasörü kaynak sayar. Yalnız derlemede kullanılır, `Pusula.exe` ile dağıtılmaz.

SHA-256: `8A7E348538E7EB91351696911689F49E3D4F63F8BAB517432BBE159B8B1104A2`
