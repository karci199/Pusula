param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
. (Join-Path $PSScriptRoot '..\_Ortak.ps1')
# Proje sayfası = ana sayfanın o projeye daraltılmış hali: aynı sekmeler, proje başlığı, Projeler görünümü yok.
[System.Threading.SynchronizationContext]::SetSynchronizationContext((New-Object System.Windows.Threading.DispatcherSynchronizationContext))
$app = New-Object Pusula.Wpf.App; $app.InitializeComponent(); $app.ShutdownMode = 'OnExplicitShutdown'
$win = New-Object Pusula.Wpf.MainWindow
$win.WindowStartupLocation = 'Manual'; $win.Left = -20000; $win.Top = -20000; $win.ShowActivated = $false; $win.Width = 1360; $win.Height = 900
$win.Show(); Pump
$vm = $win.DataContext; $vm.RootPath = $Data; Wait; Wait
$allTasks = $vm.HomeOpenCount

$project = Join-Path $Data '7000_MILKON'
$vm.SelectNodeByPath($project); Wait; Wait; Wait
Check 'proje seçilince sekmeli sayfa görünür, kayıt sayfası gizli' ($vm.IsHomePageVisible -and -not $vm.IsDetailVisible -and $vm.IsProjectScope)
Check 'başlık: kod ve ad' ($vm.ScopeProjectCode -eq '7000' -and $vm.ScopeProjectName -eq 'MILKON') ($vm.ScopeProjectCode + ' ' + $vm.ScopeProjectName)
Check 'başlık: sayılar' ($vm.ScopeProjectStats -like '*stok*') $vm.ScopeProjectStats
Check 'işler yalnız bu projenin' ($vm.HomeOpenCount -le $allTasks) ("$allTasks -> " + $vm.HomeOpenCount)
Check 'kapsamda tek proje' ($vm.ProjectOptions.Count -eq 1)
Check 'Projeler görünümü yerine Stoklar' (-not $vm.IsViewProjects)
Check 'stoklar yalnız bu projenin' (@($vm.StockRows | Where-Object { $_.Path -notlike "$project\*" }).Count -eq 0 -and $vm.StockRows.Count -ge 1) ([string]$vm.StockRows.Count)
$vm.HomeTab = 0; Wait
Snap $win "$Out\project-page.png"

$vm.SelectNodeByPath((Join-Path $project 'AB-7000-0002')); Wait
Check 'stok seçilince kayıt sayfası' ($vm.IsDetailVisible -and -not $vm.IsHomePageVisible)
$vm.GoHome(); Wait; Wait
Check 'ana sayfaya dönünce kapsam kalkar' (-not $vm.IsProjectScope -and $vm.HomeOpenCount -eq $allTasks)
$win.Close()
"SONUÇ OK=$ok HATA=$fail"
