param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
. (Join-Path $PSScriptRoot '..\_Ortak.ps1')
function WaitDetail($vm) { for ($i = 0; $i -lt 100; $i++) { Pump; if (-not $vm.Detail.IsLoading) { break }; Start-Sleep -Milliseconds 30 }; Pump; Pump }
function Hashes([string]$dir) {
    Get-ChildItem $dir -Recurse -File | Where-Object { $_.FullName -notmatch '\\_TAKIP\\' } | Sort-Object FullName |
        ForEach-Object { $_.FullName.Substring($dir.Length) + '=' + (Get-FileHash $_.FullName -Algorithm SHA256).Hash }
}

$settingsFile = Join-Path $env:LOCALAPPDATA 'Pusula\ayarlar.json'
$savedSettings = if (Test-Path $settingsFile) { [IO.File]::ReadAllText($settingsFile) } else { $null }
$root = Join-Path $env:TEMP ('pusula-mark-' + [guid]::NewGuid().ToString('N').Substring(0,6))
New-Item -ItemType Directory $root | Out-Null
try {
    # Elle açılmış yapı: 7000_MILKON\AB-7000-0002 (örneğin kopyası), işaret dosyası yok
    New-Item -ItemType Directory "$root\7000_MILKON" | Out-Null
    Copy-Item $Sample "$root\7000_MILKON\AB-7000-0002" -Recurse
    Remove-Item "$root\7000_MILKON\AB-7000-0002\_uretici" -Recurse -ErrorAction SilentlyContinue
    New-Item -ItemType Directory "$root\7000_MILKON\Notlar" | Out-Null
    $before = Hashes "$root\7000_MILKON\AB-7000-0002"

    [System.Threading.SynchronizationContext]::SetSynchronizationContext((New-Object System.Windows.Threading.DispatcherSynchronizationContext))
    $app = New-Object Pusula.Wpf.App; $app.InitializeComponent(); $app.ShutdownMode = 'OnExplicitShutdown'
    $win = New-Object Pusula.Wpf.MainWindow
    $win.WindowStartupLocation = 'Manual'; $win.Left = -20000; $win.Top = -20000; $win.ShowActivated = $false
    $win.Width = 1200; $win.Height = 720
    $win.Show(); Pump
    $vm = $win.DataContext
    $vm.RootPath = $root; Pump

    "--- 1. Proje olarak isaretle"
    $milkon = $vm.Nodes | Where-Object Title -eq '7000_MILKON'
    Check 'once isaretsiz' ($milkon.Kind -eq 'Plain')
    $vm.SelectedNode = $milkon; WaitDetail $vm
    Check 'proje secenegi var' ($vm.CanMarkAsProject -and $vm.CanMarkAsStock)
    $vm.MarkAsProjectCommand.Execute($null)
    Check 'no/ad klasor adindan' ($vm.MarkNumber -eq '7000' -and $vm.MarkName -eq 'MILKON') ($vm.MarkNumber + ' / ' + $vm.MarkName)
    Check 'isaretlendi' ($vm.ConfirmMark())
    $milkon = $vm.Nodes | Where-Object Title -eq '7000_MILKON'
    Check 'artik proje' ($milkon.Kind -eq 'Project')
    Check '_TAKIP gizli' (((Get-Item -Force "$root\7000_MILKON\_TAKIP").Attributes -band [IO.FileAttributes]::Hidden) -ne 0)
    $vm.SelectedNode = $milkon; WaitDetail $vm
    $vm.MarkAsProjectCommand.Execute($null)
    Check 'ikinci kez acilmaz' (-not $vm.IsMarkDialogOpen -or -not $vm.ConfirmMark())

    "--- 2. Stok olarak isaretle"
    $node = $milkon.Children | Where-Object Title -eq 'AB-7000-0002'
    $vm.SelectedNode = $node; WaitDetail $vm
    Check 'stok secenegi var, proje yok' ($vm.CanMarkAsStock -and -not $vm.CanMarkAsProject)
    $vm.MarkAsStockCommand.Execute($null)
    Check 'numara klasor adindan' ($vm.MarkNumber -eq 'AB-7000-0002')
    $vm.MarkNumber = 'SH2000'
    Check 'bicim disi numara reddedilir' (-not $vm.ConfirmMark() -and $vm.MarkError -match 'Biçimler') $vm.MarkError
    $vm.MarkNumber = 'AB-7000-0002'; $vm.MarkName = 'SH2000 Boynuz Anten'
    Check 'stok isaretlendi' ($vm.ConfirmMark())
    $info = Get-Content "$root\7000_MILKON\AB-7000-0002\_TAKIP\stok.json" -Raw -Encoding UTF8 | ConvertFrom-Json
    Check 'stok.json: numara, ad, durum' ($info.stockNo -eq 'AB-7000-0002' -and $info.name -eq 'SH2000 Boynuz Anten' -and $info.status -eq 'Hazırlık')
    Check 'sablon bos' ($info.template -eq '')
    $after = Hashes "$root\7000_MILKON\AB-7000-0002"
    Check 'dosyalar degismedi' ((($before -join "`n") -eq ($after -join "`n")) -and $before.Count -gt 0) ("once=" + $before.Count + " sonra=" + $after.Count)

    "--- 3. Ozet"
    $milkon = $vm.Nodes | Where-Object Title -eq '7000_MILKON'
    $milkon.IsExpanded = $true
    $stock = $milkon.Children | Where-Object Title -eq 'AB-7000-0002'
    Check 'agacta stok' ($stock.Kind -eq 'Stock')
    $vm.SelectedNode = $stock; WaitDetail $vm
    Check 'dosyalar sayildi' ([int]$vm.Detail.Tiles[0].Value -eq $before.Count) ($vm.Detail.Tiles[0].Value)
    Check 'eksik satiri yok (sablonsuz)' (-not ($vm.Detail.Rows | Where-Object IsMissing))
    Snap $win (Join-Path $Out 'isaretli-stok.png')
    $vm.SelectedNode = $milkon; WaitDetail $vm
    Check 'proje tablosunda 1 stok' ($vm.Detail.Rows.Count -eq 1)
    $win.Close()
}
finally {
    if ($savedSettings -ne $null) { [IO.File]::WriteAllText($settingsFile, $savedSettings) }
    Remove-Item -Recurse -Force $root -ErrorAction SilentlyContinue
}
""
"SONUC: $ok basarili, $fail hata"
