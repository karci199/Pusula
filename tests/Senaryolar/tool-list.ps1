param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
. (Join-Path $PSScriptRoot '..\_Ortak.ps1')
# Sol liste (25.11.2026): envanter türlerinin altı ağaçta açılmaz (araçlar gizli düğüm); türe tıklayınca sağda liste:
# özet, Proje / Stok / Alt tür / Durum / arama süzgeçleri, "Bağlı olduğu" (stok, proje, Genel + kullanıldığı projeler),
# satıra tıklayınca aracın sayfası, geri dönünce aynı süzgeç (her liste kendi süzgecini hatırlar).
[System.Threading.SynchronizationContext]::SetSynchronizationContext((New-Object System.Windows.Threading.DispatcherSynchronizationContext))
$JF = [Pusula.Core.Tracking.JsonFile]
$read = $JF.GetMethod('TryRead').MakeGenericMethod([Pusula.Core.Tracking.StockInfo])
$write = $JF.GetMethod('WriteAtomic').MakeGenericMethod([Pusula.Core.Tracking.StockInfo])
function Link-Tools([string]$stockPath, [string[]]$numbers)
{
    $file = [Pusula.Core.Tracking.TrackingFiles]::StockFile($stockPath)
    $info = $read.Invoke($null, @($file))
    foreach ($n in $numbers) { $info.Inventory.Add($n) }
    $write.Invoke($null, @($file, $info)) | Out-Null
}
function New-ToolCopy([string]$from, [string]$to, [string]$oldNo, [string]$newNo)
{
    New-Item -ItemType Directory -Force (Split-Path $to) | Out-Null
    Copy-Item $from $to -Recurse
    $kj = Get-ChildItem $to -Recurse -Filter 'kayit.json' | Select-Object -First 1
    [IO.File]::WriteAllText($kj.FullName, [IO.File]::ReadAllText($kj.FullName).Replace($oldNo, $newNo))
}

"--- Hazırlık: iki proje, stok düzeyi, proje düzeyi ve genel araçlar"
$made = & (Join-Path $PSScriptRoot '..\..\tools\Ornek-Araclar.ps1') -Kok $Data -Bin $Bin
$stock3 = Join-Path $Data '7000_MILKON\AB-7000-0003'
$cableDir = Join-Path $stock3 '02_Test_Araclari\Kablo\JT-7000-0101'
$other = Join-Path $Data '8000_DENEME'
New-Item -ItemType Directory -Force (Join-Path $other '_TAKIP') | Out-Null
[IO.File]::WriteAllText((Join-Path $other '_TAKIP\proje.json'), '{ "schemaVersion": 1, "projectNo": "8000", "name": "DENEME" }')
$stock8 = Join-Path $other 'AB-8000-0001'
Copy-Item (Join-Path $Data '7000_MILKON\AB-7000-0001') $stock8 -Recurse
$sj = [Pusula.Core.Tracking.TrackingFiles]::StockFile($stock8); [IO.File]::WriteAllText($sj, [IO.File]::ReadAllText($sj).Replace('AB-7000-0001', 'AB-8000-0001'))
New-ToolCopy $cableDir (Join-Path $stock8 '02_Test_Araclari\Kablo\JT-8000-0011') 'JT-7000-0101' 'JT-8000-0011'

[System.Threading.SynchronizationContext]::SetSynchronizationContext((New-Object System.Windows.Threading.DispatcherSynchronizationContext))
$app = New-Object Pusula.Wpf.App; $app.InitializeComponent(); $app.ShutdownMode = 'OnExplicitShutdown'
$win = New-Object Pusula.Wpf.MainWindow
$win.WindowStartupLocation = 'Manual'; $win.Left = -20000; $win.Top = -20000; $win.ShowActivated = $false; $win.Width = 1920; $win.Height = 1040
$win.Show(); Pump
$vm = $win.DataContext; $vm.RootPath = $Data; Wait

$cableType = @($vm.InventoryTypes | Where-Object Key -eq 'cable')[0]
$general = [Pusula.Core.Inventory.InventoryService]::LevelFolder($cableType, [Pusula.Core.Inventory.InventoryLevel]::General, $Data, '')
$projectLevel = [Pusula.Core.Inventory.InventoryService]::LevelFolder($cableType, [Pusula.Core.Inventory.InventoryLevel]::Project, $Data, '8000_DENEME')
$projectLevel = if ($projectLevel -and -not [IO.Path]::IsPathRooted($projectLevel)) { Join-Path $Data $projectLevel } else { $projectLevel }
New-ToolCopy $cableDir (Join-Path $general 'JT-0000-0004') 'JT-7000-0101' 'JT-0000-0004'
New-ToolCopy $cableDir (Join-Path $general 'JT-0000-0005') 'JT-7000-0101' 'JT-0000-0005'
New-ToolCopy $cableDir (Join-Path $projectLevel 'JT-8000-0020') 'JT-7000-0101' 'JT-8000-0020'
Link-Tools $stock3 @('JT-0000-0004')
Link-Tools $stock8 @('JT-0000-0004')
$vm.Refresh(); Wait; for ($k = 0; $k -lt 40; $k++) { Pump; Start-Sleep -Milliseconds 30 }

"--- 1. Ağaç: türlerin altı açılmaz"
$group = $vm.InventoryNodes[0]
$cables = $group.Children | Where-Object { $_.Type -and $_.Type.Key -eq 'cable' } | Select-Object -First 1
$jigs = $group.Children | Where-Object { $_.Type -and $_.Type.Key -eq 'jig' } | Select-Object -First 1
$hidden = @($cables.HiddenItems | ForEach-Object Title)
Check 'kablaj düğümünde ağaç çocuğu yok (kayıtlı araçlar gizli)' (@($cables.Children | Where-Object { $_.Item -ne $null }).Count -eq 0) ([string]$cables.Children.Count)
Check 'yeni dört kablo gizli düğümlerde' (@('JT-8000-0011', 'JT-8000-0020', 'JT-0000-0004', 'JT-0000-0005' | Where-Object { $hidden -contains $_ }).Count -eq 4) ($hidden -join ',')
Check 'ağaçtaki sayı gizli araç sayısı' ($cables.Subtitle -eq [string]$hidden.Count) ($cables.Subtitle + ' / ' + $hidden.Count)
Check 'hiçbir türün ağaç çocuğu kayıtlı araç değil' (@($group.Children | ForEach-Object { $_.Children } | Where-Object { $_.Item -ne $null }).Count -eq 0)

"--- 2. Liste: özet, Bağlı olduğu"
$vm.SelectedNode = $cables; Wait
$inv = $vm.Inventory
$total = $hidden.Count
Check 'liste açıldı, bütün kablolar' ($inv.IsList -and $inv.Rows.Count -eq $total) ([string]$inv.Rows.Count)
$summary = '{0} kayıt · {1} stoka bağlı · {2} genel' -f $total, @($inv.Rows | Where-Object { $_.Stocks.Count -gt 0 }).Count, @($inv.Rows | Where-Object { $_.IsGeneral }).Count
Check 'özet: kayıt · stoka bağlı · genel' ($inv.ListSummary -eq $summary) ($inv.ListSummary + ' / ' + $summary)
$row = { param($no) $inv.Rows | Where-Object Number -eq $no | Select-Object -First 1 }
$stockCables = @($inv.Rows | Where-Object { $_.Attached -eq 'AB-7000-0003' } | ForEach-Object Number)
Check 'örnek kablolar: Bağlı olduğu = AB-7000-0003' ($stockCables.Count -ge 3 -and $stockCables -contains 'JT-7000-0101') ($stockCables -join ',')
Check 'proje düzeyi: Bağlı olduğu = proje klasörü' ((& $row 'JT-8000-0020').Attached -eq '8000_DENEME') ((& $row 'JT-8000-0020').Attached)
Check 'genel, iki projede kullanılıyor: Genel + 7000, 8000' ((& $row 'JT-0000-0004').IsGeneral -and (& $row 'JT-0000-0004').AttachedNote -eq '7000, 8000') ((& $row 'JT-0000-0004').AttachedNote)
Check 'genel, kullanılmıyor: Genel, not boş' ((& $row 'JT-0000-0005').IsGeneral -and (& $row 'JT-0000-0005').AttachedNote -eq '')

"--- 3. Proje / Stok süzgeci kombinasyonları"
$nums = { @($inv.Rows | ForEach-Object Number) }
$has = { param([string[]]$yes, [string[]]$no) $n = & $nums; (@($yes | Where-Object { $n -notcontains $_ }).Count -eq 0) -and (@($no | Where-Object { $n -contains $_ }).Count -eq 0) }
Check 'proje seçenekleri: Tümü, Genel, 7000_MILKON, 8000_DENEME' ((@($inv.ProjectOptions) -join ',') -eq 'Tümü,Genel,7000_MILKON,8000_DENEME') (@($inv.ProjectOptions) -join ',')
$inv.FilterProject = '7000_MILKON'; Pump
Check '7000: stok kabloları ve 7000''de kullanılan genel var; 8000 ve kullanılmayan genel yok' ((& $has (@('JT-0000-0004') + $stockCables) @('JT-8000-0011', 'JT-8000-0020', 'JT-0000-0005')) -and @($inv.Rows | Where-Object { $_.Projects -notcontains '7000_MILKON' }).Count -eq 0) ((& $nums) -join ',')
Check '7000 seçiliyken stok seçeneklerinde 8000 stoku yok' (-not (@($inv.StockOptions) -contains 'AB-8000-0001') -and @($inv.StockOptions) -contains 'AB-7000-0003') (@($inv.StockOptions) -join ',')
$inv.FilterProject = '8000_DENEME'; Pump
Check '8000: stok kablosu, proje kablosu, genel; 7000 kablosu yok' (& $has @('JT-8000-0011', 'JT-8000-0020', 'JT-0000-0004') @('JT-7000-0101', 'JT-0000-0005')) ((& $nums) -join ',')
Check '8000 seçiliyken stok seçenekleri: Tümü, AB-8000-0001' ((@($inv.StockOptions) -join ',') -eq 'Tümü,AB-8000-0001') (@($inv.StockOptions) -join ',')
$inv.FilterStock = 'AB-8000-0001'; Pump
Check '8000 + AB-8000-0001: kendi kablosu ve bağladığı genel; proje kablosu yok' ((& $has @('JT-8000-0011', 'JT-0000-0004') @('JT-8000-0020')) -and @($inv.Rows | Where-Object { $_.Stocks -notcontains 'AB-8000-0001' }).Count -eq 0) ((& $nums) -join ',')
$inv.FilterProject = 'Genel'; Pump
Check 'proje değişince stok süzgeci Tümü''ye döner' ($inv.FilterStock -eq 'Tümü') $inv.FilterStock
Check 'Genel: yalnız hiçbir projede kullanılmayan genel kablolar' ((& $has @('JT-0000-0005') @('JT-0000-0004', 'JT-7000-0101')) -and @($inv.Rows | Where-Object { -not $_.IsGeneral -or $_.Projects.Count -gt 0 }).Count -eq 0) ((& $nums) -join ',')
$inv.FilterProject = 'Tümü'; $inv.FilterStock = 'AB-7000-0003'; Pump
Check 'yalnız stok: AB-7000-0003 kabloları + bağlı genel' ((& $has (@('JT-0000-0004') + $stockCables) @('JT-8000-0011', 'JT-0000-0005')) -and @($inv.Rows | Where-Object { $_.Stocks -notcontains 'AB-7000-0003' }).Count -eq 0) ((& $nums) -join ',')
$st = @($inv.StatusOptions)[1]; $inv.FilterStatus = $st; Pump
Check 'stok + durum birlikte (durumu farklı satır yok)' (@($inv.Rows | Where-Object { $_.Status.Name -ne $st -or $_.Stocks -notcontains 'AB-7000-0003' }).Count -eq 0) $st
$inv.FilterStatus = 'Tümü'; $inv.FilterStock = 'Tümü'; $inv.FilterText = 'ab-8000'; Pump
Check 'arama Bağlı olduğu''nda da arar (ab-8000)' (& $has @('JT-8000-0011') @('JT-7000-0101', 'JT-0000-0005')) ((& $nums) -join ',')
$inv.FilterText = 'zzz-yok'; Pump
Check 'eşleşme yok: boş, süzgeç var' ($inv.Rows.Count -eq 0 -and $inv.HasFilter) $inv.FilterSummary
$inv.ClearFilters(); Pump
Check 'Temizle: hepsi geri, süzgeç yok' ($inv.Rows.Count -eq $total -and -not $inv.HasFilter -and $inv.FilterProject -eq 'Tümü' -and $inv.FilterText -eq '') ([string]$inv.Rows.Count)
"--- 4. Satırdan araca, geri dönünce aynı süzgeç"
$inv.FilterProject = '7000_MILKON'; $inv.FilterSubType = @($inv.SubTypeOptions | Where-Object { $_ -ne 'Tümü' })[0]; Pump
$before = (& $nums) -join ','; $sub = $inv.FilterSubType
$target = (& $row 'JT-7000-0101')
if (-not $target) { $target = @($inv.Rows)[0] }
$vm.OpenRecordPath($target.Folder); Wait
Check 'satırdan aracın sayfası açıldı' ($vm.Inventory.IsRecord -and $vm.SelectedNode.Path -eq $target.Folder) ([string]$vm.SelectedNode.Title)
Check 'kablaj düğümü açılmadı (çocuk göstermez)' (-not $cables.IsExpanded -or @($cables.Children).Count -eq 0)
Check 'ağaçta eski seçim kalktı (aynı türe yeniden tıklanabilir)' (-not $cables.IsSelected)
Snap $win "$Out\tool-list-record.png"
$cables.IsSelected = $true; Pump; Wait
Check 'türe dönünce liste' ($vm.Inventory.IsList -and [object]::ReferenceEquals($vm.SelectedNode, $cables))
Check 'süzgeç aynı (proje + alt tür) ve aynı satırlar' ($vm.Inventory.FilterProject -eq '7000_MILKON' -and $vm.Inventory.FilterSubType -eq $sub -and ((& $nums) -join ',') -eq $before) ($vm.Inventory.FilterProject + ' / ' + $vm.Inventory.FilterSubType)
$vm.SelectedNode = $jigs; Wait
Check 'başka türün listesi kendi süzgeciyle (Tümü)' ($vm.Inventory.IsList -and $vm.Inventory.FilterProject -eq 'Tümü' -and -not $vm.Inventory.HasFilter)
$vm.Inventory.FilterText = 'jig'; Pump
$vm.SelectedNode = $cables; Wait
Check 'kablajlara dönünce kablaj süzgeci; jig araması taşınmadı' ($vm.Inventory.FilterProject -eq '7000_MILKON' -and $vm.Inventory.FilterText -eq '')
$vm.SelectedNode = $jigs; Wait
Check 'jiglere dönünce jig araması geri' ($vm.Inventory.FilterText -eq 'jig')
$vm.Inventory.ClearFilters(); $vm.SelectedNode = $cables; Wait; $vm.Inventory.ClearFilters(); Pump
Snap $win "$Out\tool-list.png"

"--- 5. Envanter bölümü ve başka yollar"
$vm.SelectedNode = $group; Wait
Check 'Envanter bölümü: bütün türler, tür süzgeci görünür' ($vm.Inventory.IsList -and $vm.Inventory.ShowTypeFilter -and $vm.Inventory.Rows.Count -ge $total)
$vm.SelectNodeByPath((Join-Path $general 'JT-0000-0005')); Wait
Check 'yol ile seçince genel araç açılır' ($vm.Inventory.IsRecord -and $vm.SelectedNode.Title -eq 'JT-0000-0005')
$vm.SelectNodeByPath($stock3); Wait
$vm.ShowInventory((Join-Path $general 'JT-0000-0004')); Wait
Check 'ShowInventory (yeni kayıt sonrası) aracı açar' ($vm.Inventory.IsRecord -and $vm.SelectedNode.Title -eq 'JT-0000-0004')
$found = FindNode $vm.Nodes 'JT-8000-0011'
Check 'FindNode gizli aracı bulur' ($found -ne $null -and $found.Item -ne $null)
$vm.SelectNodeByPath($stock3); Wait
Check 'stok seçilince stok sayfası (araç değil)' ($vm.SelectedNode.Kind -eq 'Stock' -and -not $vm.Inventory.IsRecord)
"--- 6. Genel araç: kullanıldığı projeler (madde 3)"
$g5 = Join-Path $general 'JT-0000-0005'
Check 'genel araç düzenlenir, proje seçimi görünür (iki proje)' ($vm.OpenEdit($g5) -and $vm.IsEditGeneral -and (@($vm.EditProjects | ForEach-Object Key) -join ',') -eq '7000_MILKON,8000_DENEME') (@($vm.EditProjects | ForEach-Object Key) -join ',')
Check 'başta işaretli proje yok' (@($vm.EditProjects | Where-Object IsChecked).Count -eq 0)
($vm.EditProjects | Where-Object Key -eq '8000_DENEME').IsChecked = $true
Check 'kaydedildi' ($vm.ConfirmEdit()) $vm.EditError
$kayit = [IO.File]::ReadAllText((Join-Path $g5 '_TAKIP\kayit.json'))
Check 'kayit.json: projeler = 8000_DENEME' ($kayit -match 'projeler"[^}]*"8000_DENEME"') $kayit
Check 'stok aracında proje seçimi yok' ($vm.OpenEdit($cableDir) -and -not $vm.IsEditGeneral -and $vm.EditProjects.Count -eq 0)
$vm.IsEditOpen = $false
$vm.SelectNodeByPath($g5); Wait
$fact = $vm.Inventory.Facts | Where-Object Label -eq 'Kullanıldığı projeler' | Select-Object -First 1
Check 'kayıt sayfasında Kullanıldığı projeler: 8000_DENEME' ($fact -ne $null -and $fact.Value -eq '8000_DENEME') ([string]$fact.Value)
$vm.SelectedNode = $cables; Wait; $vm.Inventory.ClearFilters(); Pump
$r5 = $vm.Inventory.Rows | Where-Object Number -eq 'JT-0000-0005' | Select-Object -First 1
Check 'listede Genel + 8000' ($r5.IsGeneral -and $r5.AttachedNote -eq '8000') $r5.AttachedNote
$vm.Inventory.FilterProject = '8000_DENEME'; Pump
Check '8000 süzgecinde görünür' (@($vm.Inventory.Rows | ForEach-Object Number) -contains 'JT-0000-0005')
$vm.Inventory.FilterProject = 'Genel'; Pump
Check 'artık "Genel" (projesiz) süzgecinde değil' (-not (@($vm.Inventory.Rows | ForEach-Object Number) -contains 'JT-0000-0005'))
$vm.Inventory.ClearFilters()
$vm.SelectNodeByPath($other); Wait
$vm.HomeTab = 2; $vm.IsViewTools = $true; $vm.ToolKindFilters[0].IsSelected = $true; Pump
$nums8 = @($vm.ToolRows | ForEach-Object Number)
Check '8000 proje sayfası Kayıtlar: genel araç görünür' ($nums8 -contains 'JT-0000-0005') ($nums8 -join ',')
$vm.SelectNodeByPath((Join-Path $Data '7000_MILKON')); Wait
$vm.HomeTab = 2; $vm.IsViewTools = $true; $vm.ToolKindFilters[0].IsSelected = $true; Pump
Check '7000 proje sayfasında görünmez' (-not (@($vm.ToolRows | ForEach-Object Number) -contains 'JT-0000-0005'))
$vm.OpenEdit($g5) | Out-Null
foreach ($o in $vm.EditProjects) { $o.IsChecked = $false }
$null = $vm.ConfirmEdit()
Check 'işaret kalkınca projeler boş (alan düşer ya da boş)' (-not (([IO.File]::ReadAllText((Join-Path $g5 '_TAKIP\kayit.json'))) -match 'projeler"[^}]*8000_DENEME'))$win.Close()
