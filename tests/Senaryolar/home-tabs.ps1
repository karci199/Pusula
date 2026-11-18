param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
. (Join-Path $PSScriptRoot '..\_Ortak.ps1')
# Ana sayfa alt kısmı: Günlük (stokların gerçek kayıtları), Yapılacaklar (zaman süzgeci), Kayıtlar (tür süzgeci).
[System.Threading.SynchronizationContext]::SetSynchronizationContext((New-Object System.Windows.Threading.DispatcherSynchronizationContext))
$app = New-Object Pusula.Wpf.App; $app.InitializeComponent(); $app.ShutdownMode = 'OnExplicitShutdown'
$win = New-Object Pusula.Wpf.MainWindow
$win.WindowStartupLocation = 'Manual'; $win.Left = -20000; $win.Top = -20000; $win.ShowActivated = $false; $win.Width = 1360; $win.Height = 900
$win.Show(); Pump
$vm = $win.DataContext; $vm.RootPath = $Data; Wait; Wait

"--- Günlük"
# Gerçek kayıt: ilk stokun ana notuna bugün bir "Sorun" yazılır, ana sayfa yeniden yüklenir.
$stock = @{ Path = (Join-Path $Data '7000_MILKON\AB-7000-0002'); Title = 'AB-7000-0002' }
$entry = New-Object Pusula.Core.Notes.JournalEntry
$entry.Date = (Get-Date).Date.AddHours(9).AddMinutes(30); $entry.Kind = 'Sorun'; $entry.Title = 'Ana sayfa denemesi'; $entry.Importance = 'Kritik'
[void]$entry.Tags.Add('VNA')
$err = [Pusula.Core.Notes.Journal]::Add((Join-Path $stock.Path 'AB-7000-0002.md'), $entry)
Check 'günlük kaydı yazıldı' ($err -eq '') $err
$vm.IsRangeAll = $true; $vm.LoadHome(); Wait; Wait
$cards = @($vm.HomeTimeline | ForEach-Object { $_.Cards })
Check 'günlük: gerçek kartlar var' ($cards.Count -ge 1) ([string]$cards.Count)
Check 'günlük: her kartta stok numarası' (@($cards | Where-Object { -not $_.HasSource }).Count -eq 0) ''
$mine = $cards | Where-Object { $_.Title -eq 'Ana sayfa denemesi' } | Select-Object -First 1
Check 'günlük: yeni kayıt kartta (stok, proje rengi, Kritik)' ($mine -ne $null -and $mine.Source -eq $stock.Title -and $mine.ProjectColor -ne '' -and $mine.ImportanceRank -eq 2) ([string]$mine.Source)
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
$vm.HomeTab = 2; Wait; Wait
Check 'projeler görünümü: satır var' ($vm.ProjectRows.Count -ge 1) ([string]$vm.ProjectRows.Count)
Check 'proje istatistiği: stok sayısı' ($vm.ProjectRows[0].Stocks -ge 1) ([string]$vm.ProjectRows[0].Stocks)
$vm.IsViewStocks = $true; Wait
Check 'stoklar: hepsi' ($vm.StockRows.Count -ge 3) ([string]$vm.StockRows.Count)
$vm.IsViewTools = $true; Wait
$all = $vm.ToolRows.Count
Check 'başta bütün stoklar işaretli' (@($vm.StockOptions | Where-Object { -not $_.IsChecked }).Count -eq 0)
$vm.SelectAllStocks($false); Wait
Check 'hiçbiri işaretli değilse araç yok' ($vm.ToolRows.Count -eq 0) ([string]$vm.ToolRows.Count)
$vm.StockOptions[0].IsChecked = $true; Wait
Check 'bir stok seçince araçlar daralır' ($vm.ToolRows.Count -le $all) ("$all -> " + $vm.ToolRows.Count)
$vm.SelectAllStocks($true); Wait
Check 'tümünü seçince hepsi' ($vm.ToolRows.Count -eq $all) ([string]$vm.ToolRows.Count)
Snap $win "$Out\home-kayitlar.png"
$win.Close()
"SONUÇ OK=$ok HATA=$fail"
