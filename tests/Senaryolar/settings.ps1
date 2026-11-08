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
    "--- Ayarlar ve tema"
    $set = New-Object Pusula.Wpf.SettingsWindow($win, $vm)
    $set.WindowStartupLocation = 'Manual'; $set.Left = -20000; $set.Top = -20000; $set.ShowActivated = $false
    $set.Show(); Pump
    Check 'tema listesi 9' ($vm.AllThemes.Count -eq 9)
    Snap $set "$Out\settings-dark.png"
    $vm.Theme = [Pusula.Wpf.Themes]::Find('Sand'); Pump
    Check 'tema uygulandı' ([Pusula.Wpf.Themes]::Current.Key -eq 'Sand')
    $saved = [IO.File]::ReadAllText($settingsFile)
    Check 'ayarlara yazıldı' ($saved -match '"theme":\s*"Sand"') $saved
    Check 'kök klasör korundu' ($saved.Contains($Data.Replace('\', '\\'))) $saved
    Snap $set "$Out\settings-sand.png"
    $set.Close()
    ($vm.ProjectNodes[0]).IsSelected = $true; Pump; Wait
    Snap $win "$Out\main-sand.png"
    $vm.Theme = [Pusula.Wpf.Themes]::Find('Night'); Pump; Wait
    Snap $win "$Out\main-night.png"
} finally {
    if ($savedSettings -ne $null) { [IO.File]::WriteAllText($settingsFile, $savedSettings) }
}
"SONUÇ OK=$ok HATA=$fail"
