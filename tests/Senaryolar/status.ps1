param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
. (Join-Path $PSScriptRoot '..\_Ortak.ps1')
function WaitDetail($vm) { for ($i = 0; $i -lt 100; $i++) { Pump; if (-not $vm.Detail.IsLoading) { break }; Start-Sleep -Milliseconds 30 }; Pump; Pump }

"--- 1. Numara bicimleri"
$np = [Pusula.Core.Types.NumberPattern]
Check 'AB-7000-0001 uyar' ($np::IsMatch('@@-####-####', 'AB-7000-0001'))
Check 'ab-7000-0001 (kucuk harf) uyar' ($np::IsMatch('@@-####-####', 'ab-7000-0001'))
Check 'AB-700-0001 uymaz' (-not $np::IsMatch('@@-####-####', 'AB-700-0001'))
Check 'A1-7000-0001 uymaz' (-not $np::IsMatch('@@-####-####', 'A1-7000-0001'))
Check 'JY bicimi' ($np::IsMatch('JY-####-####', 'JY-7000-0001') -and -not $np::IsMatch('JY-####-####', 'AB-7000-0001'))
Check 'ornek metin' ($np::Example('@@-####-####') -eq 'AB-0000-0000') ($np::Example('@@-####-####'))
$types = [Pusula.Core.Types.TypeSet]::CreateDefault()
Check 'JY etiketi Gelistirme' ($types.Stock.MatchNumber('JY-7000-0001').Label -eq 'Geliştirme numarası')
Check 'AB etiketi Stok' ($types.Stock.MatchNumber('AB-7000-0001').Label -eq 'Stok numarası')
Check '1234-5678-9012 kabul' ($types.Stock.AcceptsNumber('1234-5678-9012'))
Check 'STOK1 reddedilir' (-not $types.Stock.AcceptsNumber('STOK1'))
Check 'baslangic durumu Hazirlik' ($types.Stock.InitialStatus -eq 'Hazırlık')

$settingsFile = Join-Path $env:LOCALAPPDATA 'Pusula\ayarlar.json'
$savedSettings = if (Test-Path $settingsFile) { [IO.File]::ReadAllText($settingsFile) } else { $null }
$root = Join-Path $env:TEMP ('pusula-status-' + [guid]::NewGuid().ToString('N').Substring(0,6))
New-Item -ItemType Directory $root | Out-Null
try {
    $set = [Pusula.Core.Templates.TemplateStore]::LoadOrCreate($root)
    $p = [Pusula.Core.Tracking.ProjectService]::CreateProject($root, '7000', 'MILKON', $set.ProjectFolders)
    $doner = $set.Templates | Where-Object Name -eq 'Döner anten'
    $pasif = $set.Templates | Where-Object Name -eq 'Pasif anten'
    $s1 = [Pusula.Core.Tracking.ProjectService]::CreateStock($p.Folder, 'AB-7000-0001', '', 'Döner anten', $set.StockFoldersFor([string[]]$doner.Modules), 'Hazırlık')
    $s2 = [Pusula.Core.Tracking.ProjectService]::CreateStock($p.Folder, 'AB-7000-0002', '', 'Pasif anten', $set.StockFoldersFor([string[]]$pasif.Modules), 'Hazırlık')
    $s3 = [Pusula.Core.Tracking.ProjectService]::CreateStock($p.Folder, 'AB-7000-0003', '', 'Pasif anten', $set.StockFoldersFor([string[]]$pasif.Modules), 'Hazırlık')
    New-Item -ItemType Directory -Force "$($s1.Folder)\03_Test_Sonuclari\SATIMO\SN01\Ham" | Out-Null
    [IO.File]::WriteAllBytes("$($s1.Folder)\03_Test_Sonuclari\SATIMO\SN01\Ham\20261108_SATIMO.csv", (New-Object byte[] 200000))
    [Pusula.Core.Tracking.ProjectService]::SetStockStatus($s2.Folder, 'Tamamlandı', '') | Out-Null
    [Pusula.Core.Tracking.ProjectService]::SetStockStatus($s3.Folder, 'Beklemede', 'numune gelmedi') | Out-Null

    "--- 2. Durum dosyasi"
    $info = (Get-Content "$($s3.Folder)\_TAKIP\stok.json" -Raw -Encoding UTF8 | ConvertFrom-Json)
    Check 'durum yazildi' ($info.status -eq 'Beklemede' -and $info.statusNote -eq 'numune gelmedi')
    Check 'tarih yazildi' ($info.statusSince -eq (Get-Date -Format 'yyyy-MM-dd'))
    Check 'sablon korundu' ($info.template -eq 'Pasif anten')

    "--- 3. Arayuz"
    [System.Threading.SynchronizationContext]::SetSynchronizationContext((New-Object System.Windows.Threading.DispatcherSynchronizationContext))
    $app = New-Object Pusula.Wpf.App; $app.InitializeComponent(); $app.ShutdownMode = 'OnExplicitShutdown'
    $win = New-Object Pusula.Wpf.MainWindow
    $win.WindowStartupLocation = 'Manual'; $win.Left = -20000; $win.Top = -20000; $win.ShowActivated = $false
    $win.Width = 1200; $win.Height = 720
    $win.Show(); Pump
    $vm = $win.DataContext
    $vm.RootPath = $root; Pump
    Check 'turler.json olustu' (Test-Path ([Pusula.Core.Types.TypeStore]::FilePath($root)))
    $project = $vm.Nodes | Where-Object Title -eq '7000_MILKON'
    $project.IsExpanded = $true

    $vm.SelectedNode = $project; WaitDetail $vm
    $vm.NewStockCommand.Execute($null)
    $vm.NewStockNo = 'STOK1'
    Check 'gecersiz numara reddedilir' (-not $vm.ConfirmNewStock() -and $vm.StockError -match 'JY-0000-0000') $vm.StockError
    $vm.NewStockNo = 'JY-7000-0001'
    Check 'JY numarasiyla stok acilir' ($vm.ConfirmNewStock())
    $jy = (Get-Content "$($p.Folder)\JY-7000-0001\_TAKIP\stok.json" -Raw -Encoding UTF8 | ConvertFrom-Json)
    Check 'yeni stok Hazirlik ile baslar' ($jy.status -eq 'Hazırlık')

    $project = $vm.Nodes | Where-Object Title -eq '7000_MILKON'
    $project.IsExpanded = $true
    $stock = $project.Children | Where-Object Title -eq 'AB-7000-0001'
    $vm.SelectedNode = $stock; WaitDetail $vm
    Check 'stok durumu Hazirlik' ($vm.Detail.Status.Name -eq 'Hazırlık')
    $vm.SetStockStatus('Testte', ''); WaitDetail $vm
    Check 'durum degisti: Testte' ($vm.Detail.Status.Name -eq 'Testte' -and $vm.Detail.Status.Tone -eq 'active')
    Check 'asama cubugu' (($vm.Detail.Steps | ForEach-Object State) -join ',' -eq 'past,past,current,future,future') (($vm.Detail.Steps | ForEach-Object State) -join ',')
    Check 'alt baslik numara turu' ($vm.Detail.Subtitle -match 'Stok numarası' -and $vm.Detail.Subtitle -match 'Döner anten')
    Snap $win (Join-Path $Out 'durum-stok.png')

    $vm.SelectedNode = ($project.Children | Where-Object Title -eq 'AB-7000-0003'); WaitDetail $vm
    Check 'Beklemede uyari rengi' ($vm.Detail.Status.Tone -eq 'warning')
    Check 'not gorunur' ($vm.Detail.StatusInfo -match 'numune gelmedi') $vm.Detail.StatusInfo
    $vm.AskStatusNote('Beklemede'); $vm.StatusNoteText = 'jig bekleniyor'; $vm.ConfirmStatusNote(); WaitDetail $vm
    Check 'not diyalogu ile yeni not' ($vm.Detail.StatusInfo -match 'jig bekleniyor') $vm.Detail.StatusInfo

    $vm.SelectedNode = $project; WaitDetail $vm
    $dist = ($vm.Detail.Distribution | ForEach-Object Name) -join ' | '
    Check 'dagilim' ($dist -eq '1 Hazırlık | 1 Testte | 1 Tamamlandı | 1 Beklemede') $dist
    Check 'tabloda durum' (($vm.Detail.Rows | Where-Object Name -eq 'AB-7000-0002').Status.Name -eq 'Tamamlandı')
    Snap $win (Join-Path $Out 'durum-proje.png')

    $jyNode = $project.Children | Where-Object Title -eq 'JY-7000-0001'
    $vm.SelectedNode = $jyNode; WaitDetail $vm
    Check 'JY alt baslik Gelistirme' ($vm.Detail.Subtitle -match 'Geliştirme numarası') $vm.Detail.Subtitle
    $win.Close()
}
finally {
    if ($savedSettings -ne $null) { [IO.File]::WriteAllText($settingsFile, $savedSettings) }
    Remove-Item -Recurse -Force $root -ErrorAction SilentlyContinue
}
""
"SONUC: $ok basarili, $fail hata"
