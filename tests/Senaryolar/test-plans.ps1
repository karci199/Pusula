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
    "--- Test planları"
    $vm.SelectNodeByPath((Join-Path $Data '7000_MILKON\AB-7000-0002')); Wait
    $plans = @($vm.Detail.TestPlans)
    Check '1 plan' ($plans.Count -eq 1) ([string]$plans.Count)
    Check 'KIWI adı okundu' ($plans[0].Name -eq 'AB-7000-0002 SATIMO Hüzme ve Yan Lob') $plans[0].Name
    Check 'adım, limit, revizyon' ($plans[0].Meta -like 'KIWI · 3 adım · 3 limit · Rev 0*') $plans[0].Meta
    Check 'bu planla koşular sayıldı' ($plans[0].RunCount -ge 1) $plans[0].RunsText
    Check 'açıklama var' ($plans[0].HasDescription)
    $win.FindName('StockTabs').SelectedIndex = 1; Wait
    Snap $win "$Out\test-plans.png"
    $vm.SelectNodeByPath((Join-Path $Data '7000_MILKON\AB-7000-0003')); Wait
    Check 'boş plan klasörü: plan yok' ($vm.Detail.TestPlans.Count -eq 0 -and $vm.Detail.QualificationPlans.Count -eq 0)
} finally {
    if ($savedSettings -ne $null) { [IO.File]::WriteAllText($settingsFile, $savedSettings) }
}
"SONUÇ OK=$ok HATA=$fail"