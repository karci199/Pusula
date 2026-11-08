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
    "--- 1. Ağaç araması"
    "--- Bölmeler"
    Check 'üst ağaçta envanter yok' (-not ($vm.ProjectNodes | Where-Object { $_.Kind -eq 'InventoryGroup' }))
    Check 'alt bölmede Envanter' ($vm.InventoryNodes.Count -eq 1 -and $vm.HasInventory)
    $jig = $vm.InventoryNodes[0].Children | Where-Object Title -eq 'Jigler'
    $jig.IsSelected = $true; Pump
    Check 'envanterden seçim' ($vm.SelectedNode -eq $jig)
    $p = $vm.ProjectNodes[0]; $p.IsSelected = $true; Pump
    Check 'projeye geçince eski seçim kalkar' ($vm.SelectedNode -eq $p -and -not $jig.IsSelected)
    $vm.SelectedNode = $null; Pump
    Snap $win "$Out\tree-split.png"
    $win.Height = 420; Pump; Snap $win "$Out\tree-split-small.png"
} finally {
    if ($savedSettings -ne $null) { [IO.File]::WriteAllText($settingsFile, $savedSettings) }
}
"SONUÇ OK=$ok HATA=$fail"
