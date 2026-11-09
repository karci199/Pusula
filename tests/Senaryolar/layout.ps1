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
    $bf = [Reflection.BindingFlags]'Instance,NonPublic,Public'
    function Call($name, $arguments) { $plain = if ($arguments.Count -eq 0) { $null } else { [object[]]@($arguments | ForEach-Object { $_.PSObject.BaseObject }) }; $win.GetType().GetMethod($name, $bf).Invoke($win, $plain) | Out-Null }
    $vm.SelectNodeByPath((Join-Path $Data '7000_MILKON\AB-7000-0002')); Wait
    $tabs = $win.FindName('StockTabs')
    function Tags { @($tabs.Items | ForEach-Object { $_.Tag }) -join ',' }
    "--- Sekmeler"
    $before = Tags
    $folders = @($tabs.Items | Where-Object { $_.Tag -eq 'folders' })[0]; $tests = @($tabs.Items | Where-Object { $_.Tag -eq 'tests' })[0]
    Call 'MoveTab' @($folders, $tests)
    Check 'Klasörler, Testler''in yerine taşındı' ((Tags) -like 'general,folders,tests*') (Tags)
    Check 'sıra ayarlara yazıldı' ($vm.Settings.TabOrder[0] -eq 'folders') ($vm.Settings.TabOrder -join ',')
    $general = $tabs.Items[0]
    Call 'MoveTab' @($tests, $general)
    Check 'Genel hep ilk' ($tabs.Items[0].Tag -eq 'general') (Tags)
    $vm.SelectNodeByPath((Join-Path $Data '7000_MILKON\AB-7000-0003')); Wait
    Check 'başka stokta da aynı sıra' ((Tags) -like 'general,tests,folders*' -or (Tags) -like 'general,folders*') (Tags)

    "--- Sütunlar"
    $view = $win.FindName('ProjectStockList').View
    $view.Columns[1].Width = 900; Pump
    Check 'üst sınır 600' ($view.Columns[1].Width -eq 600) ([string]$view.Columns[1].Width)
    $view.Columns[2].Width = 5; Pump
    Check 'alt sınır 40' ($view.Columns[2].Width -eq 40) ([string]$view.Columns[2].Width)
    $view.Columns.Move(3, 1); Pump
    Call 'SaveLayout' @()
    Check 'sütun düzeni kaydedildi' ($vm.Settings.Columns.ContainsKey('stocks'))
    $saved = $vm.Settings.Columns['stocks']
    Check 'sütunlar anahtarla saklanır' (@($saved | Where-Object { -not $_.Key }).Count -eq 0 -and $saved[1].Key -eq [string]$view.Columns[1].Header) (($saved | ForEach-Object Key) -join ',')
    "--- Sıfırla"
    Call 'ResetLayout' @()
    Check 'sekmeler varsayılan' ((Tags) -eq $before -or (Tags) -like 'general,tests,qualification*') (Tags)
    Check 'sütunlar varsayılan' ($view.Columns[1].Width -eq 140 -and $vm.Settings.Columns.Count -eq 0) ([string]$view.Columns[1].Width)
} finally {
    if ($savedSettings -ne $null) { [IO.File]::WriteAllText($settingsFile, $savedSettings) }
}
"SONUÇ OK=$ok HATA=$fail"