param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
. (Join-Path $PSScriptRoot '..\_Ortak.ps1')
# Stok sayfası tek kaydırma (25.11.2026): 1080p ekranda (1920 x 1040 pencere) doğrulama maddeleri sıkışmaz;
# tablo bütün maddeleriyle açılır, sayfa kayar. İçteki zaman çizelgesi kendi kaydırmasını korur.
[System.Threading.SynchronizationContext]::SetSynchronizationContext((New-Object System.Windows.Threading.DispatcherSynchronizationContext))
$app = New-Object Pusula.Wpf.App; $app.InitializeComponent(); $app.ShutdownMode = 'OnExplicitShutdown'
$win = New-Object Pusula.Wpf.MainWindow
$win.WindowStartupLocation = 'Manual'; $win.Left = -20000; $win.Top = -20000; $win.ShowActivated = $false; $win.Width = 1920; $win.Height = 1040
$win.Show(); Pump
$vm = $win.DataContext; $vm.RootPath = $Data; Wait

$stock = Join-Path $Data '7000_MILKON\AB-7000-0002'
$vm.SelectNodeByPath($stock); Wait; Wait
for ($n = 1; $n -le 30; $n++)
{
    $vm.AddVerificationCommand.Execute($null); Pump
    $vm.NewVerText = 'Gereksinim ' + $n + ': çıkış gücü sınır içinde'
    $vm.NewVerTgNo = 'TG-' + $n.ToString('000')
    $null = $vm.ConfirmNewVerification()
    $vm.IsVerificationDialogOpen = $false; Pump
}
Wait
Check '30 madde eklendi' ($vm.VerificationCount -eq 30) ([string]$vm.VerificationCount)

$page = $win.FindName('StockPageScroll')
$win.UpdateLayout(); Pump
Check 'sayfa kayar (içerik pencereden uzun)' ($page.ScrollableHeight -gt 0) ([string]$page.ScrollableHeight)

# Tablonun kendi kaydırması kalmadı: bütün satırlar açık, içteki kaydırma alanının kaydıracak yeri yok.
$inner = $null
$st = New-Object System.Collections.Stack; $st.Push($page)
while ($st.Count -gt 0)
{
    $x = $st.Pop()
    if ($x -is [System.Windows.Controls.ScrollViewer] -and $x -ne $page -and $x.IsVisible -and $x.ExtentHeight -gt 1500) { $inner = $x }
    for ($i = 0; $i -lt [System.Windows.Media.VisualTreeHelper]::GetChildrenCount($x); $i++) { $st.Push([System.Windows.Media.VisualTreeHelper]::GetChild($x, $i)) }
}
Check 'tablo bütün maddeleriyle açık (içte kaydırma yok)' ($inner -ne $null -and $inner.ScrollableHeight -lt 1) ($(if ($inner) { [string]$inner.ScrollableHeight } else { 'bulunamadı' }))
Snap $win "$Out\stock-scroll-top.png"

$page.ScrollToVerticalOffset(700); Pump; $win.UpdateLayout(); Pump
Check 'sayfa aşağı kaydı' ($page.VerticalOffset -gt 600) ([string]$page.VerticalOffset)
Snap $win "$Out\stock-scroll-down.png"

$vm.SelectNodeByPath((Join-Path $Data '7000_MILKON\AB-7000-0003')); Wait; Pump
Check 'başka stok seçilince sayfa başa döner' ($page.VerticalOffset -eq 0) ([string]$page.VerticalOffset)
$win.Close()
