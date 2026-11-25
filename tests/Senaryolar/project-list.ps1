param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
. (Join-Path $PSScriptRoot '..\_Ortak.ps1')
# Sol liste madde 2 (25.11.2026): proje sırası (numara · son değişiklik · ad; ayarda hatırlanır), etiket süzgeci
# (gizlenen proje sayısı, seçme yine bulur), hepsini kapat. Proje olmayan kök klasörler sonda, süzgeçten etkilenmez.
$settingsFile = Join-Path $env:LOCALAPPDATA 'Pusula\ayarlar.json'
$savedSettings = if (Test-Path $settingsFile) { [IO.File]::ReadAllText($settingsFile) } else { $null }
try {
    function New-Project([string]$folder, [string]$no, [string]$name, [string[]]$tags)
    {
        $dir = Join-Path $Data $folder
        New-Item -ItemType Directory -Force (Join-Path $dir '_TAKIP') | Out-Null
        $tagJson = if ($tags.Count -gt 0) { ', "tags": [' + (($tags | ForEach-Object { '"' + $_ + '"' }) -join ', ') + ']' } else { '' }
        [IO.File]::WriteAllText((Join-Path $dir '_TAKIP\proje.json'), '{ "schemaVersion": 1, "projectNo": "' + $no + '", "name": "' + $name + '"' + $tagJson + ' }')
        $dir
    }
    function Age([string]$dir, [datetime]$when)
    {
        Get-ChildItem $dir -Recurse -Force | ForEach-Object { $_.LastWriteTime = $when }
        (Get-Item $dir -Force).LastWriteTime = $when
    }
    $p8 = New-Project '8000_DENEME' '8000' 'DENEME' @('Anten')
    $p10 = New-Project '10000_ALFA' '10000' 'ALFA' @('Radar', 'Anten')
    $milkon = Join-Path $Data '7000_MILKON'
    $pj = Join-Path $milkon '_TAKIP\proje.json'
    [IO.File]::WriteAllText($pj, [IO.File]::ReadAllText($pj).Replace('"name": "MILKON"', '"name": "MILKON", "tags": ["Radar"]'))
    New-Item -ItemType Directory -Force (Join-Path $Data 'Notlar_Genel') | Out-Null
    Age $p8 (Get-Date '2024-01-01'); Age $p10 (Get-Date '2023-01-01'); Age $milkon (Get-Date '2022-01-01')
    [IO.File]::WriteAllText((Join-Path $p8 'yeni.txt'), 'x')
    if (Test-Path $settingsFile) { $s = [IO.File]::ReadAllText($settingsFile) -replace '"projectSort":"[^"]*",?', '' -replace '"projectTag":"[^"]*",?', ''; [IO.File]::WriteAllText($settingsFile, $s) }

    [System.Threading.SynchronizationContext]::SetSynchronizationContext((New-Object System.Windows.Threading.DispatcherSynchronizationContext))
    $app = New-Object Pusula.Wpf.App; $app.InitializeComponent(); $app.ShutdownMode = 'OnExplicitShutdown'
    $open = {
        $w = New-Object Pusula.Wpf.MainWindow
        $w.WindowStartupLocation = 'Manual'; $w.Left = -20000; $w.Top = -20000; $w.ShowActivated = $false; $w.Width = 1600; $w.Height = 900
        $w.Show(); Pump
        $w.DataContext.RootPath = $Data; Wait
        $w
    }
    $win = & $open
    $vm = $win.DataContext
    $order = { (@($vm.ProjectNodes | ForEach-Object Title)) -join ',' }

    "--- 1. Sıra"
    $vm.ProjectSort = 'number'; Pump
    Check 'numara: 7000, 8000, 10000 (sayı olarak), sonra proje olmayan klasör' ((& $order) -eq '7000_MILKON,8000_DENEME,10000_ALFA,Notlar_Genel') (& $order)
    Check 'varsayılan sırada menü vurgusuz' (-not $vm.HasProjectListFilter)
    $vm.ProjectSort = 'name'; Pump
    Check 'ad: ALFA, DENEME, MILKON (proje.json adı)' ((& $order) -eq '10000_ALFA,8000_DENEME,7000_MILKON,Notlar_Genel') (& $order)
    Check 'sıra değişince menü vurgulu' ($vm.HasProjectListFilter)
    $vm.ProjectSort = 'recent'; Pump
    Check 'son değişiklik: en yeni 8000, sonra 10000, 7000' ((& $order) -eq '8000_DENEME,10000_ALFA,7000_MILKON,Notlar_Genel') (& $order)
    $vm.ProjectSort = 'saçma'; Pump
    Check 'bilinmeyen değer numaraya döner' ($vm.ProjectSort -eq 'number' -and (& $order) -eq '7000_MILKON,8000_DENEME,10000_ALFA,Notlar_Genel') (& $order)
    $vm.ProjectSort = 'name'; Pump
    $win.Close(); Pump
    $win = & $open; $vm = $win.DataContext
    Check 'yeniden açınca sıra hatırlanır (ad)' ($vm.ProjectSort -eq 'name' -and (& $order) -eq '10000_ALFA,8000_DENEME,7000_MILKON,Notlar_Genel') (& $order)
    $vm.Refresh(); Wait
    Check 'yenileyince sıra korunur' ((& $order) -eq '10000_ALFA,8000_DENEME,7000_MILKON,Notlar_Genel') (& $order)

    "--- 2. Etiket süzgeci"
    Check 'etiket seçenekleri sıralı, tekrarsız: Anten, Radar' ((@($vm.ProjectTagOptions) -join ',') -eq 'Anten,Radar') (@($vm.ProjectTagOptions) -join ',')
    $vm.ProjectTag = 'Anten'; Pump
    Check 'Anten: 10000 ve 8000 (ad sırası), klasör yine görünür' ((& $order) -eq '10000_ALFA,8000_DENEME,Notlar_Genel') (& $order)
    Check '1 proje gizli yazısı' ($vm.HiddenProjectCount -eq 1 -and $vm.HiddenProjectText -like '1 proje gizli*Anten*') $vm.HiddenProjectText
    Check 'Nodes bütün projeleri tutar' (@($vm.Nodes | Where-Object { $_.Kind -eq 'Project' }).Count -eq 3)
    $vm.SelectNodeByPath((Join-Path $milkon 'AB-7000-0002')); Wait
    Check 'gizli projenin stoku yine seçilir (ana sayfadan, aramadan)' ($vm.SelectedNode.Title -eq 'AB-7000-0002')
    $vm.ProjectTag = 'radar'; Pump
    Check 'büyük / küçük harf duyarsız: Radar' ((& $order) -eq '10000_ALFA,7000_MILKON,Notlar_Genel' -or $vm.ProjectTag -eq 'radar') (& $order)
    $vm.ProjectSort = 'number'; Pump
    Check 'etiket + numara birlikte: 7000, 10000' ((& $order) -eq '7000_MILKON,10000_ALFA,Notlar_Genel') (& $order)
    $vm.SearchText = 'alfa'; Pump
    $v = @(Visible $vm.ProjectNodes)
    Check 'arama süzgeçle birlikte: yalnız ALFA' (($v -contains '10000_ALFA') -and -not ($v -contains '7000_MILKON')) ($v -join ',')
    $vm.SearchText = ''; Pump
    $vm.ProjectTag = ''; Pump
    Check 'etiket kalkınca hepsi, gizli yazısı yok' ((& $order) -eq '7000_MILKON,8000_DENEME,10000_ALFA,Notlar_Genel' -and $vm.HiddenProjectCount -eq 0 -and $vm.HiddenProjectText -eq '') (& $order)
    $vm.ProjectTag = 'Anten'; Pump; $win.Close(); Pump
    $win = & $open; $vm = $win.DataContext
    Check 'yeniden açınca etiket hatırlanır' ($vm.ProjectTag -eq 'Anten' -and (& $order) -eq '8000_DENEME,10000_ALFA,Notlar_Genel') (& $order)
    Snap $win "$Out\project-list-tag.png"
    $vm.ProjectTag = ''; Pump

    "--- 3. Hepsini kapat"
    foreach ($n in $vm.Nodes) { $n.IsExpanded = $true; foreach ($c in $n.Children) { $c.IsExpanded = $true } }
    $vm.SelectNodeByPath((Join-Path $milkon 'AB-7000-0003')); Wait
    $vm.CollapseAll(); Pump
    $open2 = @($vm.Nodes | Where-Object { $_.IsExpanded }) + @($vm.Nodes | ForEach-Object { $_.Children } | Where-Object { $_.IsExpanded })
    Check 'hepsi kapandı; yalnız seçili stokun projesi açık (envanter dahil kapalı)' ((($open2 | ForEach-Object Title) -join ',') -eq '7000_MILKON') (($open2 | ForEach-Object Title) -join ',')
    $vm.SelectedNode = $null; Pump
    foreach ($n in $vm.Nodes) { $n.IsExpanded = $true }
    $vm.CollapseAll(); Pump
    Check 'seçim yokken hepsi kapanır' (@($vm.Nodes | Where-Object { $_.IsExpanded }).Count -eq 0)
    $vm.SelectNodeByPath((Join-Path $milkon 'AB-7000-0003')); Wait
    Check 'seçim kalır' ($vm.SelectedNode.Title -eq 'AB-7000-0003')
    $tool = FindNode $vm.Nodes 'JT-0001-0001'; if ($tool) { $vm.SelectNodeByPath($tool.Path); Wait; $vm.CollapseAll(); Pump; Check 'araç seçiliyken kapat: araç sayfası kalır' ($vm.SelectedNode.Title -eq 'JT-0001-0001' -and $vm.Inventory.IsRecord) }
    Snap $win "$Out\project-list.png"
    $vm.ProjectSort = 'number'; Pump
    $win.Close()
} finally { if ($savedSettings -ne $null) { [IO.File]::WriteAllText($settingsFile, $savedSettings) } }
