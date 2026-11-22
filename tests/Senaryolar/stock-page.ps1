param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
. (Join-Path $PSScriptRoot '..\_Ortak.ps1')
# Stok sayfası: üstte TEÜ ve zaman çizelgesi; sekmeler Doğrulamalar, Yapılacaklar (gruplu), Test yardımcı araçları.
[System.Threading.SynchronizationContext]::SetSynchronizationContext((New-Object System.Windows.Threading.DispatcherSynchronizationContext))
$app = New-Object Pusula.Wpf.App; $app.InitializeComponent(); $app.ShutdownMode = 'OnExplicitShutdown'
$win = New-Object Pusula.Wpf.MainWindow
$win.WindowStartupLocation = 'Manual'; $win.Left = -20000; $win.Top = -20000; $win.ShowActivated = $false; $win.Width = 1360; $win.Height = 900
$win.Show(); Pump
$vm = $win.DataContext; $vm.RootPath = $Data; Wait

function StockTabs()
{
    $st = New-Object System.Collections.Stack; $st.Push($win)
    while ($st.Count -gt 0) { $x = $st.Pop(); if ($x -is [System.Windows.Controls.TabControl] -and $x.IsVisible -and $x.Items.Count -eq 4) { return $x }; for ($i = 0; $i -lt [System.Windows.Media.VisualTreeHelper]::GetChildrenCount($x); $i++) { $st.Push([System.Windows.Media.VisualTreeHelper]::GetChild($x, $i)) } }
    $null
}

$vm.IsRangeAll = $true; Wait
$vm.SelectNodeByPath((Join-Path $Data '7000_MILKON\AB-7000-0003')); Wait; Wait; Wait
Check 'stok sayfası görünür' ($vm.IsDetailVisible -and $vm.Detail.IsStock)
Check 'zaman çizelgesi gün gün' ($vm.StockDays.Count -ge 1) ([string]$vm.StockDays.Count)
Check 'test yardımcı araçları: bu stoka bağlı' ($vm.StockToolRows.Count -ge 1) ([string]$vm.StockToolRows.Count)
Check 'sayı satırı: açık iş yazısı' ($vm.StockTasksText -like '*iş*') $vm.StockTasksText
Snap $win "$Out\stock-page.png"

$tabs = StockTabs
Check 'dört sekme (İstatistik dahil)' ($tabs -ne $null)
if ($tabs) { $tabs.SelectedIndex = 1; Wait; Snap $win "$Out\stock-page-tasks.png"; $tabs.SelectedIndex = 2; Wait; Snap $win "$Out\stock-page-tools.png" }

$vm.SelectNodeByPath((Join-Path $Data '7000_MILKON\AB-7000-0002')); Wait; Wait
Check 'başka stoka geçince yenilenir' ($vm.StockOpenCount -ge 1) ([string]$vm.StockOpenCount)
$win.Close()
"SONUÇ OK=$ok HATA=$fail"
