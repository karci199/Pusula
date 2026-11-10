param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
. (Join-Path $PSScriptRoot '..\_Ortak.ps1')
# Tablo süzgeçleri (proje stok tablosu: metin + durum; test koşuları: metin + sonuç + sistem), başka düğümde sıfırlanma,
# yenilemede korunma; durum çubuğunda son yapılan iş (saatli, ayarlarda saklanır).
function Shown($collection) { @([System.Windows.Data.CollectionViewSource]::GetDefaultView($collection)).Count }

[System.Threading.SynchronizationContext]::SetSynchronizationContext((New-Object System.Windows.Threading.DispatcherSynchronizationContext))
$app = New-Object Pusula.Wpf.App; $app.InitializeComponent(); $app.ShutdownMode = 'OnExplicitShutdown'; [Pusula.Wpf.Themes]::Apply([Pusula.Wpf.Themes]::Find('FluentDark'))
$win = New-Object Pusula.Wpf.MainWindow
$win.WindowStartupLocation = 'Manual'; $win.Left = -20000; $win.Top = -20000; $win.ShowActivated = $false; $win.Width = 1360; $win.Height = 900
$win.Show(); Pump
$vm = $win.DataContext; $vm.RootPath = $Data; Wait
$d = $vm.Detail

"--- 1. Proje stok tablosu"
$proj = FindNode $vm.Nodes '7000_MILKON'; $vm.SelectedNode = $proj; Wait
$total = $d.Rows.Count
Check 'stoklar listelendi, özet' ($total -ge 3 -and (Shown $d.Rows) -eq $total -and $d.StockFilterSummary -eq "$total stok") $d.StockFilterSummary
Check 'durum seçenekleri: Tümü + durumlar' ($d.StockStatusOptions[0] -eq 'Tümü' -and $d.StockStatusOptions.Count -ge 2) ($d.StockStatusOptions -join ', ')
$d.StockFilterText = '0003'; Pump
Check 'metinle süzülür' ((Shown $d.Rows) -eq 1 -and $d.StockFilterSummary -eq "$total stoktan 1" -and $d.IsStockFiltered) $d.StockFilterSummary
Check 'alttaki liste değişmez (dağılım, sayılar)' ($d.Rows.Count -eq $total)
$d.StockFilterText = 'ab-7000'; Pump
Check 'büyük / küçük harf fark etmez' ((Shown $d.Rows) -eq @($d.Rows | Where-Object { $_.Name -like 'AB-7000*' }).Count)
$d.StockFilterText = ''
$st = $d.StockStatusOptions[1]; $d.StockFilterStatus = $st; Pump
$expect = @($d.Rows | Where-Object { $_.Status.Name -eq $st }).Count
Check 'durumla süzülür' ((Shown $d.Rows) -eq $expect -and $expect -ge 1) "$st → $expect"
$d.OpenTasksExpanded = $false; Wait
Snap $win "$Out\filter-stocks.png"
$d.Reload(); Wait
Check 'yenileyince süzgeç kalır' ($d.StockFilterStatus -eq $st -and (Shown $d.Rows) -eq $expect)
$d.ClearStockFilter(); Pump
Check 'temizle: hepsi görünür' ((Shown $d.Rows) -eq $total -and -not $d.IsStockFiltered)
$d.StockFilterText = 'zzz-yok'; Pump
Check 'eşleşme yoksa boş, özet 0' ((Shown $d.Rows) -eq 0 -and $d.StockFilterSummary -eq "$total stoktan 0")

"--- 2. Test koşuları"
$stockWithRuns = $null
foreach ($name in 'AB-7000-0002', 'AB-7000-0003', 'JY-7000-0004') {
    $n = FindNode $vm.Nodes $name; $vm.SelectedNode = $n; Wait
    if (($d.DevelopmentRuns.Count + $d.QualificationRuns.Count) -ge 1 -and -not $stockWithRuns) { $stockWithRuns = $name; break }
}
Check 'başka düğüme geçince stok süzgeci sıfırlandı' ($d.StockFilterText -eq '' -and -not $d.IsStockFiltered)
$runs = @($d.DevelopmentRuns) + @($d.QualificationRuns)
Check 'koşular var' ($runs.Count -ge 1) $stockWithRuns
$devTotal = $d.DevelopmentRuns.Count; $qualTotal = $d.QualificationRuns.Count
Check 'özetler' ($d.DevRunsSummary -eq "$devTotal koşu" -and $d.QualRunsSummary -eq "$qualTotal koşu") "$($d.DevRunsSummary) / $($d.QualRunsSummary)"
$serial = $runs[0].Serial
$d.RunFilterText = $serial; Pump
$devExpect = @($d.DevelopmentRuns | Where-Object { "$($_.Serial) $($_.System) $($_.Person) $($_.Title) $($_.Summary)" -match [regex]::Escape($serial) }).Count
Check 'SN ile süzülür' ((Shown $d.DevelopmentRuns) -le $devTotal -and (Shown $d.DevelopmentRuns) + (Shown $d.QualificationRuns) -ge 1 -and $d.IsRunFiltered) "$serial → $((Shown $d.DevelopmentRuns))+$((Shown $d.QualificationRuns))"
$d.RunFilterText = ''
Check 'sonuç ve sistem seçenekleri' ($d.RunResultOptions.Count -ge 2 -and $d.RunSystemOptions.Count -ge 2) (($d.RunResultOptions -join ',') + ' | ' + ($d.RunSystemOptions -join ','))
$res = $d.RunResultOptions[1]; $d.RunFilterResult = $res; Pump
$expectRes = @($runs | Where-Object { $_.ResultName -eq $res }).Count
Check 'sonuçla süzülür' (((Shown $d.DevelopmentRuns) + (Shown $d.QualificationRuns)) -eq $expectRes) "$res → $expectRes"
$d.RunFilterResult = 'Tümü'
$sys = $d.RunSystemOptions[1]; $d.RunFilterSystem = $sys; Pump
$expectSys = @($runs | Where-Object { $_.System -eq $sys }).Count
Check 'sistemle süzülür' (((Shown $d.DevelopmentRuns) + (Shown $d.QualificationRuns)) -eq $expectSys) "$sys → $expectSys"
$d.RunFilterText = 'zzz-yok'; Pump
Check 'süzülmüş özet' ($d.DevRunsSummary -eq "$devTotal koşudan 0")
$d.ClearRunFilter(); Pump
Check 'koşu süzgeci temizlenir' (-not $d.IsRunFiltered -and (Shown $d.DevelopmentRuns) -eq $devTotal -and $d.RunFilterSystem -eq 'Tümü')

"--- 3. Son yapılan iş"
Check 'başta boş (yeni ayar)' (-not $vm.HasLastAction)
$QT = [Pusula.Wpf.ViewModels.QuickNoteTarget]
Check 'kısa not eklendi' ($vm.AddQuickNoteHere($QT::Stock, 'Süzgeç denemesi')); Wait
Check 'son iş: mesaj · saat' ($vm.HasLastAction -and $vm.LastAction -match ' · \d\d:\d\d$' -and $vm.LastAction -match $stockWithRuns) $vm.LastAction
Check 'ayarlarda saklandı' ($vm.Settings.LastAction -eq $vm.LastAction)
$keep = $vm.LastAction
$vm.SelectedNode = $proj; Wait
Check 'başka sayfaya geçince de durur' ($vm.LastAction -eq $keep -and $win.FindName('LastActionText').Text -eq $keep)
$vm.NoteSaved('C:\x\AB-7000-0003.md')
Check 'not penceresinde kaydetme de yazılır' ($vm.LastAction -match '^AB-7000-0003\.md kaydedildi · ')
Snap $win "$Out\filter-statusbar.png"
$win.Close()
"SONUÇ OK=$ok HATA=$fail"
