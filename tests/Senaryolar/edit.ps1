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
    $readStock = [Pusula.Core.Tracking.JsonFile].GetMethod('TryRead').MakeGenericMethod([Pusula.Core.Tracking.StockInfo])
    $readProject = [Pusula.Core.Tracking.JsonFile].GetMethod('TryRead').MakeGenericMethod([Pusula.Core.Tracking.ProjectInfo])
    $readRecord = [Pusula.Core.Tracking.JsonFile].GetMethod('TryRead').MakeGenericMethod([Pusula.Core.Inventory.InventoryRecord])
    function CountAll($folder) { @(Get-ChildItem $folder -Recurse -Force -File).Count }

    "--- 1. Düzenle: proje"
    $project = Join-Path $Data '7000_MILKON'
    Check 'proje düzenlenebilir' ($vm.OpenEdit($project) -and $vm.IsEditOpen -and -not $vm.IsEditStock -and -not $vm.IsEditInventory)
    $vm.EditName = 'MILKON Deneme'
    Check 'kaydedildi' ($vm.ConfirmEdit())
    Check 'proje.json adı değişti' ($readProject.Invoke($null, @([Pusula.Core.Tracking.TrackingFiles]::ProjectFile($project))).Name -eq 'MILKON Deneme')

    "--- 2. Düzenle: stok"
    $stock = Join-Path $project 'AB-7000-0002'
    $vm.SelectNodeByPath($stock); Wait
    Check 'stok diyaloğu: şablon görünür' ($vm.OpenEdit($stock) -and $vm.IsEditStock -and $vm.EditNumber -eq 'AB-7000-0002' -and $vm.EditTemplates.Count -ge 2)
    $vm.EditName = 'TAKIM ANTEN BOYNUZ Z'; $vm.EditTemplate = $vm.EditTemplates[1]
    Snap $win "$Out\edit-stock.png"
    Check 'kaydedildi' ($vm.ConfirmEdit())
    $info = $readStock.Invoke($null, @([Pusula.Core.Tracking.TrackingFiles]::StockFile($stock)))
    Check 'stok.json ad + şablon' ($info.Name -eq 'TAKIM ANTEN BOYNUZ Z' -and $info.Template -eq $vm.EditTemplates[1]) ($info.Name + ' / ' + $info.Template)
    Check 'notlar ve durum korundu' ($info.Notes.Count -eq 3 -and $info.Status -eq 'Ölçüm bekliyor')
    Check 'seçim korundu' ($vm.SelectedNode.Path -eq $stock)

    "--- 3. Düzenle: jig"
    $jig = Join-Path $Data 'ENVANTER\Jigler\JIG-1000-0009_SATIMO_adaptor_plakasi'
    Check 'jig diyaloğu: alt tür görünür' ($vm.OpenEdit($jig) -and $vm.IsEditInventory -and $vm.EditSubTypes.Count -gt 0)
    $vm.EditName = 'SATIMO adaptör plakası v2'; $vm.EditSubType = 'Montaj'; $vm.EditNote = 'Rev AC'
    Check 'kaydedildi' ($vm.ConfirmEdit())
    $rec = $readRecord.Invoke($null, @([Pusula.Core.Inventory.InventoryService]::RecordFile($jig)))
    Check 'kayit.json ad, alt tür, açıklama' ($rec.Name -eq 'SATIMO adaptör plakası v2' -and $rec.SubType -eq 'Montaj' -and $rec.Note -eq 'Rev AC')
    Check 'geçmiş korundu' ($rec.Events.Count -ge 3)

    "--- 4. Takipten çıkar: stok"
    $other = Join-Path $project 'JY-7000-0004'
    $before = CountAll $other
    Check 'onay açılır' ($vm.OpenUntrack($other) -and $vm.IsUntrackOpen)
    Snap $win "$Out\untrack-stock.png"
    Check 'çıkarıldı' ($vm.ConfirmUntrack())
    Check 'klasör ve dosyalar yerinde' ((Test-Path $other) -and (CountAll $other) -eq $before) ([string](CountAll $other) + ' / ' + $before)
    Check 'stok.json yeniden adlandırıldı' (-not (Test-Path ([Pusula.Core.Tracking.TrackingFiles]::StockFile($other))) -and @(Get-ChildItem (Join-Path $other '_TAKIP') -Force -Filter 'stok.json.kaldirildi-*').Count -eq 1)
    $node = FindNode $vm.Nodes 'JY-7000-0004'
    Check 'ağaçta işaretsiz' ($node -ne $null -and [string]$node.Kind -eq 'Plain') ([string]$node.Kind)

    "--- 5. Takipten çıkar: kablo"
    $cable = Join-Path $Data 'ENVANTER\Kablolar\JT-0001-0003_RS422_kablosu_Brainbox'
    Check 'çıkarıldı' ($vm.OpenUntrack($cable) -and $vm.ConfirmUntrack())
    $c = FindNode $vm.Nodes 'JT-0001-0003_RS422_kablosu_Brainbox'
    Check 'ağaçta işaretsiz kablo' ($c -ne $null -and $c.IsUnmarkedInventory)
    Check 'takip edilmeyen klasör düzenlenemez' (-not $vm.OpenEdit($other))
} finally {
    if ($savedSettings -ne $null) { [IO.File]::WriteAllText($settingsFile, $savedSettings) }
}
"SONUÇ OK=$ok HATA=$fail"