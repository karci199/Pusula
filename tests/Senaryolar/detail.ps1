param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
. (Join-Path $PSScriptRoot '..\_Ortak.ps1')
function WaitDetail($vm) { for ($i = 0; $i -lt 100; $i++) { Pump; if (-not $vm.Detail.IsLoading) { break }; Start-Sleep -Milliseconds 30 }; Pump }
function MakeFile([string]$path, [int]$kb, [datetime]$when) {
    New-Item -ItemType Directory -Force (Split-Path $path) | Out-Null
    [IO.File]::WriteAllBytes($path, (New-Object byte[] ($kb * 1024)))
    (Get-Item $path).LastWriteTime = $when
}

$settingsFile = Join-Path $env:LOCALAPPDATA 'Pusula\ayarlar.json'
$savedSettings = if (Test-Path $settingsFile) { [IO.File]::ReadAllText($settingsFile) } else { $null }
$root = Join-Path $env:TEMP ('pusula-detail-' + [guid]::NewGuid().ToString('N').Substring(0,6))
New-Item -ItemType Directory $root | Out-Null
try {
    "--- 1. Ornek veri"
    $set = [Pusula.Core.Templates.TemplateStore]::LoadOrCreate($root)
    $p = [Pusula.Core.Tracking.ProjectService]::CreateProject($root, '7000', 'MILKON', $set.ProjectFolders)
    $doner = $set.Templates | Where-Object Name -eq 'Döner anten'
    $s1 = [Pusula.Core.Tracking.ProjectService]::CreateStock($p.Folder, 'AB-7000-0001', '', 'Döner anten', $set.StockFoldersFor([string[]]$doner.Modules))
    $pasif = $set.Templates | Where-Object Name -eq 'Pasif anten'
    $s2 = [Pusula.Core.Tracking.ProjectService]::CreateStock($p.Folder, 'AB-7000-0002', '', 'Pasif anten', $set.StockFoldersFor([string[]]$pasif.Modules))
    MakeFile "$($s1.Folder)\01_Dokumanlar\AB-7000-0001_101_AA.docx" 300 (Get-Date '2026-11-02 10:00')
    MakeFile "$($s1.Folder)\03_Test_Sonuclari\SATIMO\Ham\20261108_AB-7000-0001_SN02_P2_SATIMO_pattern_v01.csv" 2048 (Get-Date).AddMinutes(-30)
    MakeFile "$($s1.Folder)\03_Test_Sonuclari\VNA\20261101_S11.s2p" 120 (Get-Date '2026-11-01 09:00')
    MakeFile "$($s1.Folder)\_ARSIV\eski.zip" 5000 (Get-Date '2026-06-01')
    MakeFile "$($s2.Folder)\07_Fotograflar\IMG_2041.jpg" 900 (Get-Date '2026-11-05 12:00')
    MakeFile "$($p.Folder)\00_Proje_Geneli\Sartname.pdf" 400 (Get-Date '2026-10-01')
    Remove-Item "$($s1.Folder)\06_Yazilimlar"

    "--- 2. Sayim (Core)"
    $none = [Threading.CancellationToken]::None
    $ps = [Pusula.Core.Scanning.FolderSummary]::ForProject($p.Folder, $set.ProjectFolders, $none)
    Check 'proje: 2 stok' ($ps.StockCount -eq 2)
    Check 'proje: 5 dosya (arsiv haric)' ($ps.Total.FileCount -eq 5) ("sayi=" + $ps.Total.FileCount)
    Check 'proje geneli 1 dosya' ($ps.General.FileCount -eq 1)
    Check 'stok satirinda sablon' (($ps.Rows | Where-Object Name -eq 'AB-7000-0001').Template -eq 'Döner anten')
    $ss = [Pusula.Core.Scanning.FolderSummary]::ForStock($s1.Folder, $set, $none)
    Check 'stok: 3 dosya (arsiv haric)' ($ss.Total.FileCount -eq 3) ("sayi=" + $ss.Total.FileCount)
    Check 'son degisen SATIMO' ($ss.Total.LastFile -match 'SATIMO_pattern') $ss.Total.LastFile
    Check 'arsiv ayri sayilir' (($ss.Rows | Where-Object Name -eq '_ARSIV').Stats.FileCount -eq 1)
    Check 'silinen klasor eksik gorunur' (($ss.Rows | Where-Object Name -eq '06_Yazilimlar').IsMissing)
    Check '_TAKIP listede yok' (-not ($ss.Rows | Where-Object Name -eq '_TAKIP'))

    "--- 3. Ana pencere"
    $app = New-Object Pusula.Wpf.App; $app.InitializeComponent(); $app.ShutdownMode = 'OnExplicitShutdown'; $app.add_DispatcherUnhandledException({ param($s2,$e2) $global:lastErr = $e2.Exception.ToString(); $e2.Handled = $true })
    [System.Threading.SynchronizationContext]::SetSynchronizationContext((New-Object System.Windows.Threading.DispatcherSynchronizationContext))
    $win = New-Object Pusula.Wpf.MainWindow
    $win.WindowStartupLocation = 'Manual'; $win.Left = -20000; $win.Top = -20000; $win.ShowActivated = $false
    $win.Width = 1200; $win.Height = 720
    $win.Show(); Pump
    $vm = $win.DataContext
    $vm.RootPath = $root; Pump
    $project = $vm.Nodes | Where-Object Title -eq '7000_MILKON'
    $project.IsExpanded = $true
    $vm.SelectedNode = $project; WaitDetail $vm
    Check 'proje tablosu 2 satir' ($vm.Detail.IsProject -and $vm.Detail.Rows.Count -eq 2) ("isProject=" + $vm.Detail.IsProject + " rows=" + $vm.Detail.Rows.Count + " loading=" + $vm.Detail.IsLoading + " kind=" + $project.Kind)
    Check 'kutucuk: Stok 2' ($vm.Detail.Tiles[0].Value -eq '2')
    Snap $win (Join-Path $Out 'detay-proje.png')
    $vm.SelectedNode = ($project.Children | Where-Object Title -eq 'AB-7000-0001'); WaitDetail $vm
    Check 'stok tablosu' ($vm.Detail.IsStock -and $vm.Detail.Rows.Count -ge 9) ("satir=" + $vm.Detail.Rows.Count)
    Check 'alt baslik' ($vm.Detail.Subtitle -match 'Döner anten') $vm.Detail.Subtitle
    Snap $win (Join-Path $Out 'detay-stok.png')
    if ($global:lastErr) { "  YAKALANAN HATA: " + $global:lastErr.Substring(0, [Math]::Min(1500, $global:lastErr.Length)) }
    $win.Close()
}
finally {
    if ($savedSettings -ne $null) { [IO.File]::WriteAllText($settingsFile, $savedSettings) }
    Remove-Item -Recurse -Force $root -ErrorAction SilentlyContinue
}
""
"SONUC: $ok basarili, $fail hata"
