param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
. (Join-Path $PSScriptRoot '..\_Ortak.ps1')
$settingsFile = Join-Path $env:LOCALAPPDATA 'Pusula\ayarlar.json'
$savedSettings = if (Test-Path $settingsFile) { [IO.File]::ReadAllText($settingsFile) } else { $null }
try {
    [System.Threading.SynchronizationContext]::SetSynchronizationContext((New-Object System.Windows.Threading.DispatcherSynchronizationContext))
    $app = New-Object Pusula.Wpf.App; $app.InitializeComponent(); $app.ShutdownMode = 'OnExplicitShutdown'
    $win = New-Object Pusula.Wpf.MainWindow
    $win.WindowStartupLocation = 'Manual'; $win.Left = -20000; $win.Top = -20000; $win.ShowActivated = $false; $win.Width = 1240; $win.Height = 760
    $win.Show(); Pump
    $vm = $win.DataContext; $vm.RootPath = $Data; Pump
    "--- + Yeni menüsü"
    $vm.SelectNodeByPath((Join-Path $Data '7000_MILKON')); Wait
    $menu = $win.FindResource('AddMenu'); $menu.PlacementTarget = $win.FindName('AddButton'); $menu.IsOpen = $true; Wait
    $heads = @($menu.Items | ForEach-Object { if ($_ -is [System.Windows.Controls.Separator]) { '---' } else { [string]$_.Header } })
    Check 'sıra: proje, stok, envanter' ($heads[0] -eq 'Yeni proje' -and $heads[1] -like 'Yeni stok*' -and $heads[2] -eq '---' -and $heads[3] -eq 'Envanter') ($heads -join ' | ')
    Check 'şablon / tür maddeleri yok' (-not ($heads -match 'Şablon|Türler'))
    Check 'envanter türleri Türler''den' (($heads -contains 'Yeni kablo') -and ($heads -contains 'Yeni jig')) ($heads -join ' | ')
    Check 'alt çizgi korunur (7000_MILKON)' ($vm.NewStockMenuText -eq 'Yeni stok · 7000__MILKON') $vm.NewStockMenuText
    $menu.IsOpen = $false
    $vm.SelectNodeByPath((Join-Path $Data 'ENVANTER\Kablolar\JT-0001-0001_SATIMO_RF_kablosu')); Wait
    Check 'proje yokken ipucu' ($vm.NewStockMenuText -eq 'Yeni stok (önce proje seç)') $vm.NewStockMenuText
    Snap $win "$Out\add-button.png"
} finally {
    if ($savedSettings -ne $null) { [IO.File]::WriteAllText($settingsFile, $savedSettings) }
}
"SONUÇ OK=$ok HATA=$fail"