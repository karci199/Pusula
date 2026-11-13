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
    $vm.SearchText = '0009'
    $v = @(Visible $vm.Nodes)
    Check '0009: Envanter > Jigler > JIG-1000-0009' (($v -join ',') -eq 'Envanter,Jigler,JIG-1000-0009') ($v -join ',')
    $jt = ($vm.Nodes | Where-Object { $_.Kind -eq 'InventoryGroup' }).Children | Where-Object Title -eq 'Jigler'
    Check 'arama sayısı: Jigler 1' ($jt.DisplaySubtitle -eq '1') $jt.DisplaySubtitle
    $vm.SearchText = 'satimo'
    $v = @(Visible $vm.Nodes)
    Check 'satimo: tanımdan bulur (JIG-1000-0009, JT-0001-0001)' (($v -contains 'JIG-1000-0009') -and ($v -contains 'JT-0001-0001') -and -not ($v -contains '7000_MILKON')) ($v -join ',')
    $vm.SearchText = 'milkon'
    $v = @(Visible $vm.Nodes)
    Check 'milkon: proje ve stokları görünür' (($v -contains '7000_MILKON') -and ($v -contains 'AB-7000-0002')) ($v -join ',')
    $vm.SearchText = 'boynuz'
    Check 'stok adından bulur (TAKIM ANTEN BOYNUZ Y)' ((@(Visible $vm.Nodes)) -contains 'AB-7000-0002') ((@(Visible $vm.Nodes)) -join ',')
    $vm.SearchText = ''
    Check 'boş: hepsi görünür, sayı 3' ((@(Visible $vm.Nodes).Count -gt 15) -and $jt.DisplaySubtitle -eq '3')
    "--- 2. Envanter süzgeçleri"
    $group = $vm.Nodes | Where-Object { $_.Kind -eq 'InventoryGroup' }
    $vm.SelectedNode = $group; Wait
    $inv = $vm.Inventory
    Check 'seçenekler: Tümü, Jig, Kablo' ((($inv.TypeOptions) -join ',') -eq 'Tümü,Jig,Kablo') (($inv.TypeOptions) -join ',')
    Check 'hepsi: 8 kayıt' ($inv.Rows.Count -eq 8 -and $inv.FilterSummary -eq '8 kayıt') $inv.FilterSummary
    $inv.FilterType = 'Jig'
    Check 'Jig: 3, alt türler jiginkiler' ($inv.Rows.Count -eq 3 -and (($inv.SubTypeOptions) -join ',') -eq 'Tümü,Mekanik,Montaj,Ölçüm adaptörü') (($inv.SubTypeOptions) -join ',')
    $inv.FilterSubType = 'Mekanik'
    Check 'Jig + Mekanik: 1' ($inv.Rows.Count -eq 1 -and $inv.FilterSummary -eq '8 kayıttan 1 tanesi') $inv.FilterSummary
    $inv.FilterType = 'Tümü'; $inv.FilterText = 'satimo'
    Check 'metin SATIMO: 3 (tanım / konum)' ($inv.Rows.Count -eq 3) (($inv.Rows | ForEach-Object Number) -join ',')
    $inv.FilterText = ''; $inv.FilterStatus = 'Talep'
    Check 'durum Talep: 2' ($inv.Rows.Count -eq 2)
    $inv.FilterStatus = 'Tümü'; $inv.FilterLevel = 'Stok'
    Check 'düzey Stok: 2 (stoka özgü)' ($inv.Rows.Count -eq 2) (($inv.Rows | ForEach-Object Number) -join ',')
    $inv.FilterLevel = 'Tümü'; $inv.FilterType = 'Kablo'; $inv.FilterSubType = 'RF'
    Snap $win "$Out\inventory-filter.png"
    $cables = $group.Children | Where-Object { $_.Title -eq 'Kablolar' }
    $vm.SelectedNode = $cables; Wait
    Check 'tür düğümünde tür süzgeci ve sütunu gizli' (-not $inv.ShowTypeFilter -and $inv.Rows.Count -eq 5 -and $inv.TypeColumnWidth -eq 0)
    $lv = $win.FindName('InventoryList'); $col = $lv.View.Columns | Where-Object { $_.Header -eq 'Tür' }
    Check 'Tür sütunu ekranda 0 geniş' ($col.ActualWidth -eq 0) $col.ActualWidth
    function Combos($d) { if ($d -is [System.Windows.Controls.ComboBox] -and $d.IsVisible) { $d }; for ($i = 0; $i -lt [System.Windows.Media.VisualTreeHelper]::GetChildrenCount($d); $i++) { Combos ([System.Windows.Media.VisualTreeHelper]::GetChild($d, $i)) } }
    $shown = @(Combos $win | ForEach-Object { [string]$_.SelectionBoxItem })
    Check 'açılır kutular Tümü gösteriyor' ($shown.Count -eq 3 -and ($shown | Where-Object { $_ -ne 'Tümü' }).Count -eq 0) ($shown -join '|')
    Snap $win "$Out\inventory-cables.png"
    $win.Close()
} finally { if ($savedSettings -ne $null) { [IO.File]::WriteAllText($settingsFile, $savedSettings) } }
""
"SONUC: $ok basarili, $fail hata"
