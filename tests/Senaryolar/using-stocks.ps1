param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
. (Join-Path $PSScriptRoot '..\_Ortak.ps1')

"--- Kayıt sayfası: Kullanan stoklar"
[System.Threading.SynchronizationContext]::SetSynchronizationContext((New-Object System.Windows.Threading.DispatcherSynchronizationContext))
$app = New-Object Pusula.Wpf.App; $app.InitializeComponent(); $app.ShutdownMode = 'OnExplicitShutdown'
$win = New-Object Pusula.Wpf.MainWindow
$win.WindowStartupLocation = 'Manual'; $win.Left = -20000; $win.Top = -20000; $win.ShowActivated = $false; $win.Width = 1360; $win.Height = 900
$win.Show(); Pump
$vm = $win.DataContext; $vm.RootPath = $Data; Pump
$folders = @{}; foreach ($i in [Pusula.Core.Inventory.InventoryService]::ReadAll($Data, $vm.InventoryTypes)) { $folders[$i.Record.Number] = $i.Folder }
function StockFile([string]$n) { [IO.File]::ReadAllText((Join-Path $Data "7000_MILKON\$n\_TAKIP\stok.json")) }
function Rows { ($vm.UsingStocks | ForEach-Object { $_.Number + ':' + $_.Badge }) -join ', ' }

$vm.ShowInventory($folders['JIG-1000-0009']); Wait
$linkedBefore = @($vm.UsingStocks).Count
Check 'genel jig: kullanan stoklar bağdan okunur' ($linkedBefore -ge 1 -and @($vm.UsingStocks | Where-Object IsOwner).Count -eq 0 -and $vm.UsingStockCountText -eq ('· ' + $linkedBefore)) (Rows)
Check 'bağlanabilecekler: kullanmayan stoklar' (@($vm.StockLinkOptions | Where-Object { $_.Label -eq 'AB-7000-0002' }).Count -eq 1 -and @($vm.StockLinkOptions | Where-Object { $vm.UsingStocks.Number -contains $_.Label }).Count -eq 0)

$opt = @($vm.StockLinkOptions | Where-Object { $_.Label -eq 'AB-7000-0002' })[0]
$vm.IsLinkingStock = $true
$vm.StockLinkChoice = $opt
Check 'stoka bağla: stok.json yazıldı, liste yenilendi, kutu kapandı' ((StockFile 'AB-7000-0002').Contains('JIG-1000-0009') -and @($vm.UsingStocks).Count -eq $linkedBefore + 1 -and -not $vm.IsLinkingStock -and @($vm.StockLinkOptions | Where-Object { $_.Label -eq 'AB-7000-0002' }).Count -eq 0) (Rows)
Snap $win "$Out\using-stocks.png"

$row = @($vm.UsingStocks | Where-Object Number -eq 'AB-7000-0002')[0]
Check 'bağı kaldır: ilk tık onay ister' (-not $vm.UnlinkUsingStock($row) -and $row.IsConfirming -and (StockFile 'AB-7000-0002').Contains('JIG-1000-0009'))
Check 'ikinci tık kaldırır; aracın klasörü yerinde' ($vm.UnlinkUsingStock($row) -and -not (StockFile 'AB-7000-0002').Contains('JIG-1000-0009') -and @($vm.UsingStocks).Count -eq $linkedBefore -and (Test-Path $folders['JIG-1000-0009']))

"--- Stok düzeyindeki araç"
$vm.ShowInventory($folders['JIG-1000-0014']); Wait
$first = @($vm.UsingStocks)[0]
Check 'klasörü içinde olan stok en üstte, rozet' ($first.Number -eq 'AB-7000-0003' -and $first.IsOwner -and -not $first.IsLinked) (Rows)
Check 'klasörü burada olan kaldırılamaz' (-not $vm.UnlinkUsingStock($first) -and -not $vm.UnlinkUsingStock($first))

"--- Satır stoku açar"
$m = $win.GetType().GetMethod('UsingStockOpen_Click', [Reflection.BindingFlags]'NonPublic,Instance')
$b = New-Object System.Windows.Controls.Button; $b.SetValue([System.Windows.FrameworkElement]::TagProperty, [string]$first.Path.psobject.BaseObject)
$m.Invoke($win, [object[]]@($b.psobject.BaseObject, [System.Windows.RoutedEventArgs]::new())) | Out-Null; Wait
Check 'stok sayfası açıldı' ($vm.SelectedNode.Title -eq 'AB-7000-0003') ($vm.SelectedNode.Title + ' | ' + $first.Path + ' | ' + [string]$vm.SelectedNode.Path)
$win.Close()
""
"SONUC: $ok basarili, $fail hata"
