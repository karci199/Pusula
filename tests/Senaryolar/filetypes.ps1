param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
. (Join-Path $PSScriptRoot '..\_Ortak.ps1')
$TS = [Pusula.Core.Types.TypeStore]
"--- 1. Çekirdek"
$set = [Pusula.Core.Types.TypeSet]::CreateDefault()
Check 'varsayılan türler: Visio, STEP, Touchstone' ($set.FindFileType('.vsdx').Name -eq 'Visio' -and $set.FindFileType('STP').Name -eq 'STEP' -and $set.FindFileType('s2p').Name -eq 'Touchstone')
$d = $TS::DescribeFiles($set, [string[]]@('a_RevAB.step','a_RevAC.step','a.pdf','b.xyz','c.vsdx'), 'diğer')
Check 'özet: çoktan aza, diğer sonda' ($d -eq '2 STEP · 1 PDF · 1 Visio · 1 diğer') $d
$root = Join-Path $env:TEMP ('pusula-ft-' + [guid]::NewGuid().ToString('N').Substring(0,6)); New-Item -ItemType Directory $root | Out-Null
try {
    $types = $TS::LoadOrCreate($root)
    $vm = New-Object Pusula.Wpf.ViewModels.TypeEditor.FileTypesViewModel($root, $types)
    Check 'pencere listesi dolu' ($vm.Rows.Count -ge 10)
    $r = New-Object Pusula.Wpf.ViewModels.TypeEditor.FileTypeRow; $r.Name = 'WaveStudio'; $r.Extensions = '.wsx, *.wsp'; $r.Group = 'Simülasyon'; $vm.Rows.Add($r)
    $dup = New-Object Pusula.Wpf.ViewModels.TypeEditor.FileTypeRow; $dup.Name = 'Başka'; $dup.Extensions = 'vsdx'; $vm.Rows.Add($dup)
    Check 'aynı uzantı iki türde → hata' (-not $vm.Save() -and $vm.Error -match 'vsdx') $vm.Error
    $vm.Rows.Remove($dup) | Out-Null
    Check 'kaydedildi' ($vm.Save()) $vm.Error
    $re = $TS::LoadOrCreate($root)
    Check 'WaveStudio: wsx, wsp; jig türü korunur' ((($re.FindFileType('wsp').Extensions) -join ',') -eq 'wsx,wsp' -and $re.Find('jig') -ne $null)
} finally { Remove-Item -Recurse -Force $root -ErrorAction SilentlyContinue }
"--- 2. Arayüz"
$settingsFile = Join-Path $env:LOCALAPPDATA 'Pusula\ayarlar.json'
$savedSettings = if (Test-Path $settingsFile) { [IO.File]::ReadAllText($settingsFile) } else { $null }
try {
    [System.Threading.SynchronizationContext]::SetSynchronizationContext((New-Object System.Windows.Threading.DispatcherSynchronizationContext))
    $app = New-Object Pusula.Wpf.App; $app.InitializeComponent(); $app.ShutdownMode = 'OnExplicitShutdown'
    $win = New-Object Pusula.Wpf.MainWindow
    $win.WindowStartupLocation = 'Manual'; $win.Left = -20000; $win.Top = -20000; $win.ShowActivated = $false; $win.Width = 1360; $win.Height = 900
    $win.Show(); Pump
    $vmm = $win.DataContext; $vmm.RootPath = $Data; Pump
    $vmm.SelectedNode = (FindNode $vmm.Nodes 'JIG-1000-0009'); Wait
    $tiles = ($vmm.Inventory.FolderTiles | ForEach-Object { $_.Label + '=' + $_.Kinds }) -join ' | '
    Check 'kutucuk türleri' ($tiles -match 'Cizimler=2 STEP · 1 PDF' -and $tiles -match 'Fotograflar=5 Fotoğraf') $tiles
    Snap $win "$Out\filetypes-jig.png"
    $win.Close()
} finally { if ($savedSettings -ne $null) { [IO.File]::WriteAllText($settingsFile, $savedSettings) } }
""
"SONUC: $ok basarili, $fail hata"
