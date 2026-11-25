param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
. (Join-Path $PSScriptRoot '..\_Ortak.ps1')
# Alt / üst stok (25.11.2026): bileşenler üst stokta (stok.json → components), kullanıldığı yerler hesaplanır;
# bir bileşen birden çok üst stokta olabilir; kendisi, tekrar ve döngü (doğrudan / dolaylı) engellenir; klasörler değişmez.
[System.Threading.SynchronizationContext]::SetSynchronizationContext((New-Object System.Windows.Threading.DispatcherSynchronizationContext))
$app = New-Object Pusula.Wpf.App; $app.InitializeComponent(); $app.ShutdownMode = 'OnExplicitShutdown'
$win = New-Object Pusula.Wpf.MainWindow
$win.WindowStartupLocation = 'Manual'; $win.Left = -20000; $win.Top = -20000; $win.ShowActivated = $false; $win.Width = 1600; $win.Height = 1000
$win.Show(); Pump
$vm = $win.DataContext; $vm.RootPath = $Data; Wait

$m = Join-Path $Data '7000_MILKON'
$s1 = Join-Path $m 'AB-7000-0001'; $s2 = Join-Path $m 'AB-7000-0002'; $s3 = Join-Path $m 'AB-7000-0003'; $s4 = Join-Path $m 'JY-7000-0004'
$foldersBefore = @(Get-ChildItem $m -Recurse -Directory | ForEach-Object FullName) -join '|'
$go = { param($p) $vm.SelectNodeByPath($p); Wait }
$chips = { (@($vm.StockComponentChips | ForEach-Object Number)) -join ',' }
$used = { (@($vm.StockUsedInChips | ForEach-Object Number)) -join ',' }
$opts = { @($vm.ComponentOptions | ForEach-Object Label) }

"--- 1. Bileşen ekleme"
& $go $s1
Check 'başta bileşen ve kullanıldığı yer yok' ((& $chips) -eq '' -and -not $vm.HasStockUsedIn)
Check 'seçenekler: kendisi hariç bütün stoklar' (-not ((& $opts) -contains 'AB-7000-0001') -and (& $opts) -contains 'AB-7000-0003') ((& $opts) -join ',')
Check '0003 eklendi' ($vm.AddComponent($s3)) $vm.Status
Check 'JY-7000-0004 numarayla eklendi' ($vm.AddComponent('JY-7000-0004'))
Check 'bileşenler sıralı: 0003, JY-0004' ((& $chips) -eq 'AB-7000-0003,JY-7000-0004') (& $chips)
Check 'çipte ad var (numara · ad)' ($vm.StockComponentChips[0].Label -like 'AB-7000-0003 · *') $vm.StockComponentChips[0].Label
Check 'eklenenler seçeneklerden çıktı' (-not ((& $opts) -contains 'AB-7000-0003'))
Check 'aynı bileşen ikinci kez eklenmez' (-not $vm.AddComponent($s3))
Check 'kendisi eklenmez' (-not $vm.AddComponent($s1))
$json = [IO.File]::ReadAllText([Pusula.Core.Tracking.TrackingFiles]::StockFile($s1))
Check 'stok.json: components' ($json -match '"components"' -and $json -match 'AB-7000-0003' -and $json -match 'JY-7000-0004') $json

"--- 2. Kullanıldığı yerler, birden çok üst"
& $go $s2
Check '0002''ye de 0003 eklenir (iki üst)' ($vm.AddComponent($s3))
& $go $s3
Check '0003 kullanıldığı yerler: 0001, 0002' ((& $used) -eq 'AB-7000-0001,AB-7000-0002' -and $vm.HasStockUsedIn) (& $used)
Check '0003''ün kendi bileşeni yok' ((& $chips) -eq '')

"--- 3. Döngü engeli"
Check '0003 seçeneklerinde üstleri (0001, 0002) ve kendisi yok' (-not ((& $opts) -contains 'AB-7000-0001') -and -not ((& $opts) -contains 'AB-7000-0002') -and -not ((& $opts) -contains 'AB-7000-0003')) ((& $opts) -join ',')
Check '0003''e üstü 0001 eklenemez (doğrudan döngü)' (-not $vm.AddComponent($s1)) $vm.Status
Check 'JY-0004 0003''e eklenebilir (elmas, döngü değil)' ($vm.AddComponent($s4))
& $go $s4
Check 'JY-0004 kullanıldığı yerler: 0001, 0003' ((& $used) -eq 'AB-7000-0001,AB-7000-0003') (& $used)
Check 'JY-0004''e 0002 eklenemez (dolaylı üst: 0002 > 0003 > JY-0004)' (-not $vm.AddComponent($s2) -and -not ((& $opts) -contains 'AB-7000-0002')) ((& $opts) -join ',')
$map = New-Object 'System.Collections.Generic.Dictionary[string,System.Collections.Generic.List[string]]'
foreach ($k in 'A', 'B', 'C') { $map[$k] = New-Object 'System.Collections.Generic.List[string]' }
$map['A'].Add('B'); $map['B'].Add('C')
$SC = [Pusula.Core.Tracking.StockComponents]
Check 'çekirdek: C''ye A eklenemez (A > B > C)' (-not $SC::CanAdd('C', 'A', $map) -and $SC::CanAdd('A', 'C', $map) -eq $true)
Check 'çekirdek: üstler A, B' ((@($SC::AncestorsOf('C', $map)) | Sort-Object) -join ',' -eq 'A,B')

"--- 4. Kaldırma ve bulunamayan numara"
& $go $s1
$vm.RemoveComponent('AB-7000-0003') | Out-Null
Check '0001''den 0003 kaldırıldı' ((& $chips) -eq 'JY-7000-0004') (& $chips)
& $go $s3
Check '0003 kullanıldığı yerler: yalnız 0002' ((& $used) -eq 'AB-7000-0002') (& $used)
$f2 = [Pusula.Core.Tracking.TrackingFiles]::StockFile($s2)
[IO.File]::WriteAllText($f2, [IO.File]::ReadAllText($f2).Replace('"AB-7000-0003"', '"AB-7000-0003","AB-9999-0001"'))
& $go $s2
$miss = $vm.StockComponentChips | Where-Object Number -eq 'AB-9999-0001'
Check 'ağaçta olmayan numara soluk çip (IsMissing)' ($miss -ne $null -and $miss.IsMissing) ((& $chips))
Snap $win "$Out\stock-links.png"
& $go $s3
Snap $win "$Out\stock-links-used.png"

"--- 5. Dokunulmayanlar"
Check 'klasörler aynı (taşınmadı, açılmadı)' ((@(Get-ChildItem $m -Recurse -Directory | ForEach-Object FullName) -join '|') -eq $foldersBefore)
$vm.SelectNodeByPath((Join-Path $Data '7000_MILKON')); Wait
Check 'proje seçilince stok bağ kutusu kapalı' (-not $vm.IsAddingComponent)
$win.Close()
