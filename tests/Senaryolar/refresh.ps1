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
    "--- Yenile: seçim ve sekme korunur"
    $stock = Join-Path $Data '7000_MILKON\AB-7000-0002'
    $vm.SelectNodeByPath($stock); Wait
    $tabs = $win.FindName('StockTabs')
    $tabs.SelectedIndex = 1; Pump
    $vm.Refresh(); Wait
    Check 'seçili stok aynı' ($vm.SelectedNode -ne $null -and $vm.SelectedNode.Path -eq $stock) ([string]$vm.SelectedNode.Path)
    Check 'proje açık kaldı' ((FindNode $vm.Nodes '7000_MILKON').IsExpanded)
    Check 'sekme aynı (Testler)' ($tabs.SelectedIndex -eq 1) ([string]$tabs.SelectedIndex)
    Check 'stok içeriği yeniden yüklendi' ($vm.Detail.IsStock)

    $jig = Join-Path $Data 'ENVANTER\Jigler\JIG-1000-0009_SATIMO_adaptor_plakasi'
    $vm.SelectNodeByPath($jig); Wait
    $vm.Refresh(); Wait
    Check 'envanter kaydı seçili kalır' ($vm.SelectedNode -ne $null -and $vm.SelectedNode.Path -eq $jig) ([string]$vm.SelectedNode.Path)

    $empty = Join-Path $env:TEMP ('pusula-bos-' + [guid]::NewGuid().ToString('N').Substring(0, 6)); New-Item -ItemType Directory $empty | Out-Null
    $vm.RootPath = $empty; Pump; $vm.RootPath = $Data; Pump; Remove-Item $empty -Recurse -Force -ErrorAction SilentlyContinue
    Check 'kök değişince seçim sıfırlanır' ($vm.SelectedNode -eq $null)
} finally {
    if ($savedSettings -ne $null) { [IO.File]::WriteAllText($settingsFile, $savedSettings) }
}
"SONUÇ OK=$ok HATA=$fail"