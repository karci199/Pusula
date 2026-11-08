param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
. (Join-Path $PSScriptRoot '..\_Ortak.ps1')
$svc = [Pusula.Core.Inventory.InventoryService]
$readInfo = [Pusula.Core.Tracking.JsonFile].GetMethod('TryRead').MakeGenericMethod([Pusula.Core.Tracking.StockInfo])

"--- 1. Çekirdek (geçici kök)"
$tmp = Join-Path $env:TEMP ('pusula-link-' + [guid]::NewGuid().ToString('N').Substring(0,6))
try {
    New-Item -ItemType Directory "$tmp\7000_X\AB-7000-0001" -Force | Out-Null
    [Pusula.Core.Tracking.ProjectService]::MarkAsProject("$tmp\7000_X", '7000', 'X') | Out-Null
    [Pusula.Core.Tracking.ProjectService]::MarkAsStock("$tmp\7000_X\AB-7000-0001", 'AB-7000-0001', '', 'Hazırlık') | Out-Null
    $types = [Pusula.Core.Types.TypeStore]::LoadOrCreate($tmp); $jig = $types.Find('jig')
    $svc::Create($tmp, $jig, 'General', '', 'JIG-1000-0001', 'Genel jig', '', '', 'k') | Out-Null
    $svc::Create($tmp, $jig, 'Stock', "$tmp\7000_X\AB-7000-0001", 'JIG-1000-0002', 'Stok jigi', '', '', 'k') | Out-Null
    $stock = "$tmp\7000_X\AB-7000-0001"
    Check 'bağ yazıldı' ($svc::SetStockLink($stock, 'JIG-1000-0001', $true))
    $svc::SetStockLink($stock, 'JIG-1000-0099', $true) | Out-Null
    $info = $readInfo.Invoke($null, @("$stock\_TAKIP\stok.json"))
    Check 'stok.json: durum korunur, 2 bağ' ($info.Status -eq 'Hazırlık' -and ($info.Inventory -join ',') -eq 'JIG-1000-0001,JIG-1000-0099') ($info.Inventory -join ',')
    $all = $svc::ReadAll($tmp, $types.InventoryTypes); $missing = $null
    $used = $svc::UsedByStock($stock, $info.Inventory, $all, [ref]$missing)
    Check 'kullanılan: stok kaydı + bağlı genel' ((($used | ForEach-Object { $_.Record.Number }) -join ',') -eq 'JIG-1000-0001,JIG-1000-0002')
    Check 'bulunamayan bağ' (($missing -join ',') -eq 'JIG-1000-0099')
    $svc::SetStockLink($stock, 'JIG-1000-0099', $false) | Out-Null
    $info = $readInfo.Invoke($null, @("$stock\_TAKIP\stok.json"))
    Check 'bağ kaldırıldı, kayıt yerinde' (($info.Inventory -join ',') -eq 'JIG-1000-0001' -and (Test-Path "$tmp\ENVANTER\Jigler\JIG-1000-0001_Genel_jig"))
} finally { Remove-Item -Recurse -Force $tmp -ErrorAction SilentlyContinue }

"--- 2. Arayüz (deneme verisi, AB-7000-0003)"
$root = $Data
$settingsFile = Join-Path $env:LOCALAPPDATA 'Pusula\ayarlar.json'
$savedSettings = if (Test-Path $settingsFile) { [IO.File]::ReadAllText($settingsFile) } else { $null }
try {
    [System.Threading.SynchronizationContext]::SetSynchronizationContext((New-Object System.Windows.Threading.DispatcherSynchronizationContext))
    $app = New-Object Pusula.Wpf.App; $app.InitializeComponent(); $app.ShutdownMode = 'OnExplicitShutdown'
    $win = New-Object Pusula.Wpf.MainWindow
    $win.WindowStartupLocation = 'Manual'; $win.Left = -20000; $win.Top = -20000; $win.ShowActivated = $false; $win.Width = 1360; $win.Height = 900
    $win.Show(); Pump
    $vm = $win.DataContext; $vm.RootPath = $root; Pump
    $stock = FindNode $vm.Nodes 'AB-7000-0003'
    $vm.SelectedNode = $stock; Wait
    $jig = @($vm.InventoryTypes | Where-Object Key -eq 'jig')[0]; $cable = @($vm.InventoryTypes | Where-Object Key -eq 'cable')[0]
    $cands = $vm.LinkCandidates($jig) | ForEach-Object { $_.Record.Number }
    Check 'ekle adaylarında stok jigi yok' (-not ($cands -contains 'JIG-1000-0014')) ($cands -join ',')
    foreach ($n in 'JIG-1000-0009','JIG-1000-0020') { if ($cands -contains $n) { $vm.SetStockLink($n, $true); Wait } }
    if (($vm.LinkCandidates($cable) | ForEach-Object { $_.Record.Number }) -contains 'JT-0001-0002') { $vm.SetStockLink('JT-0001-0002', $true); Wait }
    $groups = @($vm.Detail.InventoryGroups)
    $jg = $groups | Where-Object { $_.Type.Key -eq 'jig' }; $cg = $groups | Where-Object { $_.Type.Key -eq 'cable' }
    Check 'jigler: 3 (1 stok + 2 bağ)' ($jg.Rows.Count -eq 3 -and @($jg.Rows | Where-Object IsLinked).Count -eq 2) (($jg.Rows | ForEach-Object Number) -join ',')
    Check 'kablolar: 2' ($cg.Rows.Count -eq 2) (($cg.Rows | ForEach-Object Number) -join ',')
    $tc0 = $win.FindName('StockTabs'); $heads = ($tc0.Items | ForEach-Object { if ($_.Header -is [string]) { $_.Header } else { '?' } }) -join ' | '
    Check 'sekmeler: kablolar ve jigler ayrı' ($heads -match 'Kalifikasyon · \d \| Kablolar · 2 \| Jigler · 3 \| Klasörler') $heads
    Check 'özet: Jig 3 · 1 dikkat' ($jg.Summary -eq 'Jig 3 · 1 dikkat' -and $jg.HasWarning) $jg.Summary
    Check 'dikkat kartı: onarımdaki jig' (@($vm.Detail.Alerts | Where-Object { $_.Detail -match 'JIG-1000-0009 · Onarım' }).Count -eq 1)
    Check 'aday listesinden bağlılar düşer' (@($vm.LinkCandidates($jig)).Count -eq 0)
    Snap $win "$Out\stock-general.png"
    $tabs = [System.Windows.Media.VisualTreeHelper]; 
    function FindTab($d) { if ($d -is [System.Windows.Controls.TabControl] -and $d.IsVisible) { return $d }; for ($i = 0; $i -lt [System.Windows.Media.VisualTreeHelper]::GetChildrenCount($d); $i++) { $r = FindTab ([System.Windows.Media.VisualTreeHelper]::GetChild($d, $i)); if ($r) { return $r } }; return $null }
    $tc = $win.FindName('StockTabs'); $tc.SelectedIndex = 4; Snap $win "$Out\stock-inventory.png"; $vm.SelectedNode = (FindNode $vm.Nodes 'AB-7000-0001'); Wait; $vm.SelectedNode = $stock; Wait; Check 'stok değişince Jigler sekmesi açık kalır' ($tc.SelectedItem.Tag -eq 'inventory:jig') $tc.SelectedItem.Tag
    $win.Close()
} finally { if ($savedSettings -ne $null) { [IO.File]::WriteAllText($settingsFile, $savedSettings) } }
""
"SONUC: $ok basarili, $fail hata"
