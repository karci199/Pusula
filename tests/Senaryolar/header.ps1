param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
. (Join-Path $PSScriptRoot '..\_Ortak.ps1')
$settingsFile = Join-Path $env:LOCALAPPDATA 'Pusula\ayarlar.json'
$savedSettings = if (Test-Path $settingsFile) { [IO.File]::ReadAllText($settingsFile) } else { $null }
try {
    [System.Threading.SynchronizationContext]::SetSynchronizationContext((New-Object System.Windows.Threading.DispatcherSynchronizationContext))
    $app = New-Object Pusula.Wpf.App; $app.InitializeComponent(); $app.ShutdownMode = 'OnExplicitShutdown'; [Pusula.Wpf.Themes]::Apply([Pusula.Wpf.Themes]::Find('FluentDark'))
    $win = New-Object Pusula.Wpf.MainWindow
    $win.WindowStartupLocation = 'Manual'; $win.Left = -20000; $win.Top = -20000; $win.ShowActivated = $false; $win.Width = 1240; $win.Height = 760
    $win.Show(); Pump
    $vm = $win.DataContext; $vm.RootPath = $Data; Pump
    "--- Başlık"
    $stock = Join-Path $Data '7000_MILKON\AB-7000-0002'
    $win.Height = 1150; Pump
    $vm.SelectNodeByPath($stock); Wait
    Check 'stok: ad' ($vm.Detail.HeaderName -eq 'TAKIM ANTEN BOYNUZ Y') $vm.Detail.HeaderName
    Check 'stok: üst kayıt proje' ($vm.Detail.ParentName -eq '7000_MILKON' -and $vm.Detail.ParentPath -eq (Join-Path $Data '7000_MILKON')) $vm.Detail.ParentName
    Check 'stok: bağlam' ($vm.Detail.ContextText -like '*numaras*') $vm.Detail.ContextText
    Snap $win "$Out\header-stock.png"
    $win.GetType().GetMethod('HeaderParent_Click', [Reflection.BindingFlags]'Instance,NonPublic').Invoke($win, @($null, $null)) | Out-Null; Wait
    Check 'üst kayda git: proje seçildi' ($vm.SelectedNode.Path -eq (Join-Path $Data '7000_MILKON')) $vm.SelectedNode.Path
    Snap $win "$Out\header-project.png"
    $jig = Join-Path $Data '7000_MILKON\AB-7000-0003\05_Jigler\JIG-1000-0014_Doner_tabla_montaj_aparati'
    $vm.SelectNodeByPath($jig); Wait
    Check 'stoka ait jig: üst kayıt stok' ($vm.Detail.ParentName -eq 'AB-7000-0003') ($vm.Detail.ParentName + ' / ' + $vm.Detail.ContextText)
    Snap $win "$Out\header-jig.png"
} finally {
    if ($savedSettings -ne $null) { [IO.File]::WriteAllText($settingsFile, $savedSettings) }
}
"SONUÇ OK=$ok HATA=$fail"