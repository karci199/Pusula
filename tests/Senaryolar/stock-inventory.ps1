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
    $tc0 = $win.FindName('StockTabs'); $heads = ($tc0.Items | ForEach-Object { if ($_.Header -is [string]) { $_.Header } elseif ($_.Header -is [System.Windows.Controls.Panel]) { @($_.Header.Children | Where-Object { $_ -is [System.Windows.Controls.TextBlock] })[-1].Text } else { '?' } }) -join ' | '
    Check 'sekmeler: kablolar ve jigler ayrı' ($heads -match 'Kalifikasyon · \d \| Kablolar · 2 \| Jigler · 3 \| Klasörler') $heads
    Check 'özet: Jig 3 · 1 dikkat' ($jg.Summary -eq 'Jig 3 · 1 dikkat' -and $jg.HasWarning) $jg.Summary
    Check 'dikkat kartı: onarımdaki jig' (@($vm.Detail.Alerts | Where-Object { $_.Detail -match 'JIG-1000-0009 · Onarım' }).Count -eq 1)
    Check 'aday listesinden bağlılar düşer' (@($vm.LinkCandidates($jig)).Count -eq 0)
    $inv = @($vm.Detail.Timeline | Where-Object IsInventory)
    Check 'zaman çizelgesi: jig/kablo durum olayları' (@($inv | Where-Object { $_.Title -like 'JIG-1000-0009*' -and $_.Result.Name -eq 'Talep' }).Count -eq 1 -and @($inv | Where-Object { $_.Title -like 'JIG-1000-0009*' }).Count -eq 5) (($inv | ForEach-Object { $_.Title + ' ' + $_.Result.Name }) -join ' / ')
    $dates = @($vm.Detail.Timeline | ForEach-Object SortDate)
    Check 'zaman çizelgesi yeniden eskiye' ((@($dates | Sort-Object -Descending) -join ',') -eq ($dates -join ','))
    Check 'olayda konum ayrıntısı' (@($inv | Where-Object { $_.Detail -eq 'Konum: Lab-A raf 3' }).Count -ge 1)
    function InvTitles() { @($vm.Detail.Timeline | Where-Object IsInventory | ForEach-Object { ($_.Title -split ' ')[0] } | Sort-Object -Unique) -join ',' }
    Check 'kaynaklar: stok jigi, bağlı jigler, stok kablosu, bağlı kablo' ((InvTitles) -eq 'JIG-1000-0009,JIG-1000-0014,JIG-1000-0020,JT-0001-0002,JT-0001-0102') (InvTitles)
    Check 'meta: tür · tarih · kişi' (@($inv | Where-Object { $_.Meta -match '^(Jig|Kablo) · \d\d\.\d\d\.\d{4}' }).Count -eq $inv.Count)
    Check 'not düğmesi kaydın notunu açar' (@($inv | Where-Object { $_.Title -like 'JIG-1000-0009*' -and $_.NotePath -like '*JIG-1000-0009.md' }).Count -eq 5)
    Check 'test koşuları ve olaylar da duruyor' (@($vm.Detail.Timeline | Where-Object { -not $_.IsInventory }).Count -ge 4)
    Snap $win "$Out\stock-general.png"

    "--- 3. Kombinler: bağı kaldır, yeni durum, envantersiz stok, kısa not"
    $vm.SetStockLink('JIG-1000-0020', $false); Wait
    Check 'bağı kaldırılan jigin olayları düşer' (-not ((InvTitles) -match 'JIG-1000-0020')) (InvTitles)
    $ev = New-Object Pusula.Core.Inventory.InventoryEvent; $ev.Status = 'Teslim alındı'; $ev.Date = '2026-11-09'; $ev.Person = 'katan'; $ev.Location = 'Lab-B'; $ev.Note = 'Rev AC geldi'
    $jigFolder = @($vm.Detail.InventoryItems | Where-Object { $_.Record.Number -eq 'JIG-1000-0009' })[0].Folder
    Check 'jige yeni durum yazıldı' ([Pusula.Core.Inventory.InventoryService]::AddEvent($jigFolder, $ev))
    $vm.SelectedNode = (FindNode $vm.Nodes 'AB-7000-0001'); Wait
    Check 'envantersiz stokta envanter olayı yok' (@($vm.Detail.Timeline | Where-Object IsInventory).Count -eq 0)
    $vm.RefreshCommand.Execute($null); Wait; $vm.SelectedNode = (FindNode $vm.Nodes 'AB-7000-0003'); Wait
    $top = @($vm.Detail.Timeline)[0]
    Check 'yeni durum en üstte' ($top.IsInventory -and $top.Title -like 'JIG-1000-0009*' -and $top.Result.Name -eq 'Teslim alındı' -and $top.Detail -match '^Konum: (.+ → )?Lab-B$' -and $top.IsStage) ($top.Title + ' ' + $top.Result.Name + ' ' + $top.Detail)
    $vm.SetStockLink('JT-0001-0002', $false); $vm.SetStockLink('JIG-1000-0009', $false); Wait
    Check 'yalnız stokun kendi kayıtları kalır' ((InvTitles) -eq 'JIG-1000-0014,JT-0001-0102') (InvTitles)
    $d = @($vm.Detail.Timeline | ForEach-Object SortDate)
    Check 'sıra yine yeniden eskiye' ((@($d | Sort-Object -Descending) -join ',') -eq ($d -join ','))
    $tabs = [System.Windows.Media.VisualTreeHelper]; 
    function FindTab($d) { if ($d -is [System.Windows.Controls.TabControl] -and $d.IsVisible) { return $d }; for ($i = 0; $i -lt [System.Windows.Media.VisualTreeHelper]::GetChildrenCount($d); $i++) { $r = FindTab ([System.Windows.Media.VisualTreeHelper]::GetChild($d, $i)); if ($r) { return $r } }; return $null }
    $tc = $win.FindName('StockTabs'); $tc.SelectedIndex = 4; Snap $win "$Out\stock-inventory.png"; $vm.SelectedNode = (FindNode $vm.Nodes 'AB-7000-0001'); Wait; $vm.SelectedNode = $stock; Wait; Check 'stok değişince Jigler sekmesi açık kalır' ($tc.SelectedItem.Tag -eq 'inventory:jig') $tc.SelectedItem.Tag
    # Fazladan "Jig 0" kutucuğu: Genel açıkken stoklar arasında geçince çizilen kart sayısı listeyle aynı olmalı.
    $tc.SelectedIndex = 0; Wait
    function DrawnCards() {
        $st = New-Object System.Collections.Stack; $st.Push($win); $ic = $null
        while ($st.Count -gt 0) { $x = $st.Pop(); if ($x -is [System.Windows.Controls.ItemsControl] -and [object]::ReferenceEquals($x.ItemsSource, $vm.Detail.InventoryGroups)) { $ic = $x; break }; if ($x -is [System.Windows.Media.Visual]) { for ($i = 0; $i -lt [System.Windows.Media.VisualTreeHelper]::GetChildrenCount($x); $i++) { $st.Push([System.Windows.Media.VisualTreeHelper]::GetChild($x, $i)) } } }
        if (-not $ic) { return -1 }
        $p = $null; $s2 = New-Object System.Collections.Stack; $s2.Push($ic); while ($s2.Count -gt 0) { $y = $s2.Pop(); if ($y -is [System.Windows.Controls.Primitives.UniformGrid]) { $p = $y; break }; for ($i = 0; $i -lt [System.Windows.Media.VisualTreeHelper]::GetChildrenCount($y); $i++) { $s2.Push([System.Windows.Media.VisualTreeHelper]::GetChild($y, $i)) } }
        if ($p) { $p.Children.Count } else { -2 }
    }
    $mismatch = @()
    foreach ($round in 1..3) { foreach ($n in 'AB-7000-0001', 'AB-7000-0003') { $vm.SelectedNode = (FindNode $vm.Nodes $n); Wait; $drawn = DrawnCards; if ($drawn -ne $vm.Detail.InventoryGroups.Count) { $mismatch += "$n : çizilen=$drawn liste=$($vm.Detail.InventoryGroups.Count)" } } }
    Check 'Genel: kart sayısı listeyle aynı (fazladan Jig kutucuğu yok)' ($mismatch.Count -eq 0 -and (DrawnCards) -ge 1) ($mismatch -join ' / ')
    $win.Close()
} finally { if ($savedSettings -ne $null) { [IO.File]::WriteAllText($settingsFile, $savedSettings) } }
""
"SONUC: $ok basarili, $fail hata"
