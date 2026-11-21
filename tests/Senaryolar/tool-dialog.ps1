param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
. (Join-Path $PSScriptRoot '..\_Ortak.ps1')

"--- Ortak ekleme penceresi: tür, kullanım yeri"
[System.Threading.SynchronizationContext]::SetSynchronizationContext((New-Object System.Windows.Threading.DispatcherSynchronizationContext))
$app = New-Object Pusula.Wpf.App; $app.InitializeComponent(); $app.ShutdownMode = 'OnExplicitShutdown'
$win = New-Object Pusula.Wpf.MainWindow
$win.WindowStartupLocation = 'Manual'; $win.Left = -20000; $win.Top = -20000; $win.ShowActivated = $false; $win.Width = 1360; $win.Height = 900
$win.Show(); Pump
$vm = $win.DataContext; $vm.RootPath = $Data; Pump
$res = [Pusula.Wpf.Properties.Resources]

$stock = FindNode $vm.Nodes 'AB-7000-0002'
$vm.SelectedNode = $stock; Wait
$vm.OpenAddTool()
Check 'pencere açılır, başlık ortak' ($vm.IsInventoryDialogOpen -and $vm.InventoryDialogTitle -eq $res::Inv_AddToolTitle) $vm.InventoryDialogTitle
Check 'tür listesi: bütün envanter türleri' ($vm.InventoryTypeOptions.Count -eq @($vm.InventoryTypes).Count -and $vm.InventoryTypeOptions.Count -ge 4) ($vm.InventoryTypeOptions.Count)
$levels = @($vm.InventoryLevels)
Check 'kullanım yeri: Genel + projeler + stoklar' ($levels[0].Level -eq 'General' -and @($levels | Where-Object { $_.Level -eq 'Project' }).Count -ge 1 -and @($levels | Where-Object { $_.Level -eq 'Stock' }).Count -ge 3) (($levels | ForEach-Object { $_.Badge + ':' + $_.Label }) -join ', ')
Check 'varsayılan kullanım yeri: seçili stok' ($vm.NewInventoryLevel.Level -eq 'Stock' -and $vm.NewInventoryLevel.Label -eq 'AB-7000-0002') $vm.NewInventoryLevel.Label
Check 'stok satırında ad' ($vm.NewInventoryLevel.Detail.Length -gt 0) $vm.NewInventoryLevel.Detail

$jig = @($vm.InventoryTypeOptions | Where-Object Key -eq 'jig')[0]
$names = @($vm.InventoryTypeOptions | ForEach-Object { $_.Name })
Check 'tür sırası ve adları' (($names[0..4] -join ',') -eq 'Mekanik jig,Kablaj,Yazılım,Cihaz,Diğer') ($names -join ',')
$equipment = @($vm.InventoryTypeOptions | Where-Object Key -eq 'equipment')[0]
Check 'cihaz stok klasörü 02_Test_Araclari\Cihaz' ($equipment.StockFolder -eq '02_Test_Araclari\Cihaz') $equipment.StockFolder
Check 'jig: eski JIG numarası da geçerli' ($jig.AcceptsNumber('JIG-1000-0009') -and $jig.AcceptsNumber('JT-1000-0009'))

$cable = @($vm.InventoryTypeOptions | Where-Object Key -eq 'cable')[0]
$vm.InventoryTypeChoice = $cable
$vm.NewInventoryName = 'Yazılan tanım'
$vm.InventoryTypeChoice = $jig
Check 'tür değişince alt türler o türün' ((@($vm.InventorySubTypes) -join ',') -eq ($jig.SubTypes -join ',') -and $vm.NewInventorySubType -eq $jig.SubTypes[0]) (@($vm.InventorySubTypes) -join ',')
Check 'tür değişince tanım korunur' ($vm.NewInventoryName -eq 'Yazılan tanım')
Check 'numara ipucu türden (JT)' ($vm.InventoryNumberHint -like 'JT-*') $vm.InventoryNumberHint
$vm.InventoryTypeChoice = $null
Check 'boş değer seçimi silmez' ($vm.InventoryTypeChoice.Key -eq 'jig')

"--- Yeni alt tür"
$before = $jig.SubTypes.Count
$vm.IsAddingSubType = $true; $vm.NewSubTypeText = '  Anten   tutucu '
Check 'yeni alt tür eklenir ve seçilir' ($vm.AddSubType() -and $vm.NewInventorySubType -eq 'Anten tutucu' -and $jig.SubTypes.Count -eq $before + 1 -and -not $vm.IsAddingSubType) $vm.NewInventorySubType
$typesFile = @(Get-ChildItem $Data -Recurse -Filter 'turler.json')[0].FullName
Check 'tür dosyasına yazıldı' ([IO.File]::ReadAllText($typesFile).Contains('Anten tutucu')) $typesFile
$vm.IsAddingSubType = $true; $vm.NewSubTypeText = 'satimo'
Check 'var olan alt tür tekrar eklenmez, seçilir' ($vm.AddSubType() -and $vm.NewInventorySubType -eq 'SATIMO' -and $jig.SubTypes.Count -eq $before + 1) $vm.NewInventorySubType
$vm.IsAddingSubType = $true; $vm.NewSubTypeText = '   '
Check 'boş ad eklenmez' (-not $vm.AddSubType() -and $jig.SubTypes.Count -eq $before + 1)
$vm.IsAddingSubType = $false
$vm.NewInventorySubType = 'Anten tutucu'

$vm.NewInventoryName = 'TEST MEKANIK SATIMO AB-7000-0002'; $vm.NewInventoryNumber = 'JT-7000-0011'
$vm.NewInventoryNote = "Satır 1`r`nSatır 2"; $vm.NewInventoryTags = '#satimo'
# Pencere görüntüsü: açık ContentDialog'un açılır katmanı ayrıca çizilir (Snap onu yakalamaz).
function Find-Open($node) { if ($node -eq $null) { return $null }; if ($node.GetType().Name -eq 'ContentDialog' -and $node.IsOpen) { return $node }; for ($i = 0; $i -lt [System.Windows.Media.VisualTreeHelper]::GetChildrenCount($node); $i++) { $r = Find-Open ([System.Windows.Media.VisualTreeHelper]::GetChild($node, $i)); if ($r) { return $r } }; return $null }
function Find-Popup($node) { if ($node -is [System.Windows.Controls.Primitives.Popup]) { return $node }; for ($i = 0; $i -lt [System.Windows.Media.VisualTreeHelper]::GetChildrenCount($node); $i++) { $r = Find-Popup ([System.Windows.Media.VisualTreeHelper]::GetChild($node, $i)); if ($r) { return $r } }; return $null }
for ($k = 0; $k -lt 20; $k++) { Pump; Start-Sleep -Milliseconds 30 }
$dlg = Find-Open $win
if ($dlg) {
    $pop = Find-Popup $dlg; if ($pop -and $pop.Child) { $dlg = $pop.Child; $sz = New-Object System.Windows.Size($win.ActualWidth, $win.ActualHeight); $dlg.Measure($sz); $dlg.Arrange((New-Object System.Windows.Rect($sz))); $dlg.UpdateLayout(); for ($k = 0; $k -lt 20; $k++) { Pump; Start-Sleep -Milliseconds 30 } }
    $bmp = New-Object System.Windows.Media.Imaging.RenderTargetBitmap([int]$dlg.ActualWidth, [int]$dlg.ActualHeight, 96, 96, [System.Windows.Media.PixelFormats]::Pbgra32)
    $bmp.Render($dlg); $enc = New-Object System.Windows.Media.Imaging.PngBitmapEncoder; $enc.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($bmp))
    $fs = [IO.File]::Create("$Out\tool-dialog.png"); $enc.Save($fs); $fs.Close()
}
Check 'stoka açılır: 02_Test_Araclari\Jig' ($vm.ConfirmNewInventory() -and (Test-Path "$Data\7000_MILKON\AB-7000-0002\02_Test_Araclari\Jig\JT-7000-0011\_TAKIP\kayit.json"))
$rec = [IO.File]::ReadAllText("$Data\7000_MILKON\AB-7000-0002\02_Test_Araclari\Jig\JT-7000-0011\_TAKIP\kayit.json")
Check 'kayıtta tanım ve çok satırlı not' ($rec.Contains('TEST MEKANIK SATIMO AB-7000-0002') -and $rec.Contains('Satır 2'))

"--- Proje seçiliyken; Genel"
$project = FindNode $vm.Nodes '7000_MILKON'
$vm.SelectedNode = $project; Wait
$vm.OpenAddTool()
Check 'son seçilen tür hatırlanır' ($vm.InventoryTypeChoice.Key -eq 'jig') $vm.InventoryTypeChoice.Key
Check 'varsayılan kullanım yeri: proje' ($vm.NewInventoryLevel.Level -eq 'Project' -and $vm.NewInventoryLevel.Label -eq '7000_MILKON') $vm.NewInventoryLevel.Label
$vm.InventoryTypeChoice = $cable
$vm.NewInventoryLevel = @($vm.InventoryLevels)[0]
$vm.NewInventoryName = 'RF TEST KABLOSU'; $vm.NewInventoryNumber = 'JT-1000-0031'
Check 'Genel: ENVANTER\Kablolar' ($vm.ConfirmNewInventory() -and (@(Get-ChildItem "$Data\ENVANTER\Kablolar" -Directory | Where-Object { $_.Name -like 'JT-1000-0031*' }).Count -eq 1))

"--- Stok sayfası: Ekle düğmesi"
$vm.SelectedNode = (FindNode $vm.Nodes 'AB-7000-0003'); Wait
$m = $win.GetType().GetMethod('StockToolAdd_Click', [Reflection.BindingFlags]'NonPublic,Instance')
$m.Invoke($win, [object[]]@($null, [System.Windows.RoutedEventArgs]::new())) | Out-Null
Check 'stok sekmesinden açılır, yer bu stok' ($vm.IsInventoryDialogOpen -and $vm.NewInventoryLevel.Label -eq 'AB-7000-0003') $vm.NewInventoryLevel.Label
$vm.IsInventoryDialogOpen = $false
$win.Close()
""
"SONUC: $ok basarili, $fail hata"
