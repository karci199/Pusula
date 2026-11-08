param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
. (Join-Path $PSScriptRoot '..\_Ortak.ps1')
$settingsFile = Join-Path $env:LOCALAPPDATA 'Pusula\ayarlar.json'
$savedSettings = if (Test-Path $settingsFile) { [IO.File]::ReadAllText($settingsFile) } else { $null }
$root = Join-Path $env:TEMP ('pusula-invui-' + [guid]::NewGuid().ToString('N').Substring(0,6))
New-Item -ItemType Directory "$root\7000_MILKON\AB-7000-0003" -Force | Out-Null
[Pusula.Core.Tracking.ProjectService]::MarkAsProject("$root\7000_MILKON", '7000', 'MILKON') | Out-Null
[Pusula.Core.Tracking.ProjectService]::MarkAsStock("$root\7000_MILKON\AB-7000-0003", 'AB-7000-0003', '', 'Hazırlık') | Out-Null
try {
    [System.Threading.SynchronizationContext]::SetSynchronizationContext((New-Object System.Windows.Threading.DispatcherSynchronizationContext))
    $app = New-Object Pusula.Wpf.App; $app.InitializeComponent(); $app.ShutdownMode = 'OnExplicitShutdown'
    $app.add_DispatcherUnhandledException({ param($s, $e) "  YAKALANAN HATA: " + $e.Exception.Message; $e.Handled = $true })
    $win = New-Object Pusula.Wpf.MainWindow
    $win.WindowStartupLocation = 'Manual'; $win.Left = -20000; $win.Top = -20000; $win.ShowActivated = $false
    $win.Width = 1360; $win.Height = 860
    $win.Show(); Pump
    $vm = $win.DataContext
    $vm.RootPath = $root; Pump

    "--- 1. Ağaç"
    $group = $vm.Nodes | Where-Object { $_.Kind -eq 'InventoryGroup' }
    Check 'Envanter bölümü var' ($group -ne $null)
    Check 'türler: Kablolar, Jigler' ((($group.Children | ForEach-Object Title) -join ',') -eq 'Kablolar,Jigler')
    Check 'ENVANTER klasörü proje gibi görünmüyor' (-not ($vm.Nodes | Where-Object { $_.Title -ceq 'ENVANTER' }))

    "--- 2. Yeni kablo"
    $stock = FindNode $vm.Nodes 'AB-7000-0003'
    $vm.SelectedNode = $stock; Pump
    $cable = @($vm.InventoryTypes)[0]
    $vm.OpenNewInventory($cable)
    Check 'diyalog açıldı, başlık' ($vm.IsInventoryDialogOpen -and $vm.InventoryDialogTitle -eq 'Yeni kablo') $vm.InventoryDialogTitle
    Check 'düzeyler: Genel, Proje, Stok' ($vm.InventoryLevels.Count -eq 3 -and $vm.NewInventoryLevel.Level -eq 'Stock')
    $vm.NewInventoryNumber = 'KBL-1'; $vm.NewInventoryName = 'Deneme'
    Check 'biçim dışı reddedilir' (-not $vm.ConfirmNewInventory() -and $vm.InventoryError -match 'JT-0000-0000') $vm.InventoryError
    $vm.NewInventoryNumber = 'JT-0001-0002'; $vm.NewInventoryName = 'RS422 test kablosu'; $vm.NewInventorySubType = 'Haberleşme'
    Check 'kablo açıldı' ($vm.ConfirmNewInventory())
    Pump
    Check 'stok klasöründe' (Test-Path "$root\7000_MILKON\AB-7000-0003\05_Kablolar\JT-0001-0002_RS422_test_kablosu\_TAKIP\kayit.json")
    Check 'kayıt seçildi, sayfa açık' ($vm.Inventory.IsRecord -and $vm.SelectedNode.Title -eq 'JT-0001-0002')
    Check 'durum Talep, kişi oturum sahibi' ($vm.Inventory.Status.Name -eq 'Talep' -and $vm.Inventory.History[0].Meta -match [Environment]::UserName)

    "--- 3. Durum değiştirme"
    $vm.AskInventoryEvent('Teslim alındı')
    Check 'olay diyaloğu' ($vm.IsEventDialogOpen -and $vm.EventTitle -eq 'Durum → Teslim alındı' -and $vm.EventPerson -eq [Environment]::UserName)
    $vm.EventLocation = 'Lab-A raf 3'; $vm.EventNote = '2 adet'
    $vm.ConfirmInventoryEvent(); Pump
    Check 'durum Teslim alındı' ($vm.Inventory.Status.Name -eq 'Teslim alındı')
    Check 'geçmiş 2 olay, yeni üstte' ($vm.Inventory.History.Count -eq 2 -and $vm.Inventory.History[0].Status.Name -eq 'Teslim alındı')
    Check 'konum bilgisi' ($vm.Inventory.StatusInfo -match 'Lab-A raf 3') $vm.Inventory.StatusInfo
    Check 'bilgiler: düzey stok' (($vm.Inventory.Facts | Where-Object Label -eq 'Düzey').Value -eq 'Stok · AB-7000-0003')
    Check 'aşama çubuğu' ((($vm.Inventory.Steps | ForEach-Object State) -join ',') -eq 'past,past,current,future,future')

    "--- 4. Liste"
    $group = $vm.Nodes | Where-Object { $_.Kind -eq 'InventoryGroup' }
    $vm.SelectedNode = $group; Pump
    Check 'bölüm seçilince liste' ($vm.Inventory.IsList -and $vm.Inventory.Rows.Count -eq 1)
    $vm.SelectedNode = $stock; Pump
    Check 'stok seçilince envanter paneli kapanır' (-not $vm.Inventory.IsRecord -and -not $vm.Inventory.IsList)
    "--- 5. Şablon alt klasörleri ve elle açılmış klasör"
    $made = Get-ChildItem "$root\7000_MILKON\AB-7000-0003\05_Kablolar\JT-0001-0002_RS422_test_kablosu" -Directory | Where-Object { $_.Name -notlike '_*' } | ForEach-Object Name
    Check 'kablo şablon klasörleri' (($made -join ',') -eq '01_Belgeler,02_Fotograflar') ($made -join ',')
    New-Item -ItemType Directory "$root\ENVANTER\Jigler\JIG-1000-0030_Test_Aparati" -Force | Out-Null
    New-Item -ItemType Directory "$root\7000_MILKON\AB-7000-0003\05_Jigler\Mekanik" -Force | Out-Null
    $vm.RootPath = ''; Pump; $vm.RootPath = $root; Pump
    $un = FindNode $vm.Nodes 'JIG-1000-0030_Test_Aparati'
    Check 'işaretsiz düğüm görünür' ($un -ne $null -and $un.IsUnmarkedInventory -and $un.Subtitle -eq 'işaretsiz')
    Check 'numarasız klasör eklenmez' ((FindNode $vm.Nodes 'Mekanik') -eq $null)
    $vm.SelectedNode = $un; Pump
    Check 'menü: Jig olarak işaretle' ($vm.CanMarkAsInventory -and -not $vm.CanMarkAsStock -and $vm.MarkInventoryMenuText -eq 'Jig olarak işaretle') $vm.MarkInventoryMenuText
    Snap $win "$Out\inv-unmarked.png"
    $vm.MarkAsInventoryCommand.Execute($null); Pump
    Check 'diyalog ad ve numarayı önerir' ($vm.IsInventoryMarkMode -and $vm.NewInventoryNumber -eq 'JIG-1000-0030' -and $vm.NewInventoryName -eq 'Test Aparati' -and $vm.NewInventoryStatus -eq 'Kullanımda') ($vm.NewInventoryNumber + '|' + $vm.NewInventoryName + '|' + $vm.NewInventoryStatus)
    Snap $win "$Out\inv-mark-dialog.png"
    Check 'işaretlendi' ($vm.ConfirmNewInventory()) $vm.InventoryError
    Pump
    Check 'kayıt dosyası, klasör yeniden adlandırılmadı' (Test-Path "$root\ENVANTER\Jigler\JIG-1000-0030_Test_Aparati\_TAKIP\kayit.json")
    Check 'kayıt seçili, durum Kullanımda' ($vm.Inventory.IsRecord -and $vm.Inventory.Status.Name -eq 'Kullanımda')
    $win.Close()
}
finally {
    if ($savedSettings -ne $null) { [IO.File]::WriteAllText($settingsFile, $savedSettings) }
    Remove-Item -Recurse -Force $root -ErrorAction SilentlyContinue
}
""
"SONUC: $ok basarili, $fail hata"
