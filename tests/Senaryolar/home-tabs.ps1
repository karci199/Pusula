param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
. (Join-Path $PSScriptRoot '..\_Ortak.ps1')
# Ana sayfa alt kısmı: Günlük (örnek kartlar), Yapılacaklar (zaman süzgeci), Kayıtlar (tür süzgeci).
[System.Threading.SynchronizationContext]::SetSynchronizationContext((New-Object System.Windows.Threading.DispatcherSynchronizationContext))
$app = New-Object Pusula.Wpf.App; $app.InitializeComponent(); $app.ShutdownMode = 'OnExplicitShutdown'
$win = New-Object Pusula.Wpf.MainWindow
$win.WindowStartupLocation = 'Manual'; $win.Left = -20000; $win.Top = -20000; $win.ShowActivated = $false; $win.Width = 1360; $win.Height = 900
$win.Show(); Pump
$vm = $win.DataContext; $vm.RootPath = $Data; Wait; Wait

"--- Günlük"
$vm.IsRangeAll = $true; Wait
Check 'günlük: örnek günler var' ($vm.HomeTimeline.Count -ge 3) ([string]$vm.HomeTimeline.Count)
$vm.IsRangeToday = $true; Wait
Check 'bugün: yalnız bugünün günü' ($vm.HomeTimeline.Count -eq 1) ([string]$vm.HomeTimeline.Count)
$vm.IsRangeWeek = $true; Wait
Snap $win "$Out\home-gunluk.png"

"--- Yapılacaklar"
$vm.HomeTab = 1; Wait
$week = $vm.HomeTasks.Count
$vm.IsRangeAll = $true; Wait
Check 'Hepsi, Bu hafta''dan az değil' ($vm.HomeTasks.Count -ge $week -and $vm.HomeTasks.Count -ge 1) ("hafta=$week hepsi=" + $vm.HomeTasks.Count)
Snap $win "$Out\home-yapilacaklar.png"

"--- Kayıtlar"
$vm.HomeTab = 2; Wait
Check 'kayıtlar: proje + stok + envanter' ($vm.HomeRecords.Count -ge 5) ([string]$vm.HomeRecords.Count)
$stock = @($vm.HomeRecordFilters | Where-Object { $_.Key -eq 'stock' })[0]
$stock.IsSelected = $true; Wait
Check 'tür süzgeci: yalnız stoklar' (@($vm.HomeRecords | Where-Object { $_.Kind -ne 'stock' }).Count -eq 0 -and $vm.HomeRecords.Count -ge 1)
Snap $win "$Out\home-kayitlar.png"
$win.Close()
"SONUÇ OK=$ok HATA=$fail"
