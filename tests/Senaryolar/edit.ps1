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

    "--- 2b. Etiketler: stok ve proje"
    $vm.OpenEdit($stock) | Out-Null
    $vm.EditTags = '#SATIMO #ÇK deneme'
    Check 'stok etiketleri kaydedildi' ($vm.ConfirmEdit())
    $info = $readStock.Invoke($null, @([Pusula.Core.Tracking.TrackingFiles]::StockFile($stock)))
    Check 'stok.json etiketler' (($info.Tags -join ',') -eq 'SATIMO,ÇK,deneme') ($info.Tags -join ',')
    Wait
    Check 'başlıkta etiket çipleri' ($vm.Detail.HasTags -and ($vm.Detail.Tags -join ' ') -eq '#SATIMO #ÇK #deneme') ($vm.Detail.Tags -join ' ')
    $mainNote = [Pusula.Core.Notes.NoteFile]::Find($stock, 'AB-7000-0002')
    if ($mainNote -and (Test-Path $mainNote)) { Check 'ana notun YAML: tags listesi' ([IO.File]::ReadAllText($mainNote) -match '(?m)^tags: \[SATIMO, ÇK, deneme\]') }
    Check 'düzenle açılınca etiketler dolu' ($vm.OpenEdit($stock) -and $vm.EditTags -like '*#SATIMO*')
    $sug = @($vm.TagSuggestions() | % Key)
    Check 'stokta # önerisi: her yerde geçen etiketler' ($sug -contains '#SATIMO' -and -not ($sug -contains '#faz_kararli')) ($sug -join ' ')
    $vm.IsEditOpen = $false
    Snap $win "$Out\edit-stock-tags.png"
    $vm.OpenEdit($project) | Out-Null; $vm.EditTags = '#Kalifikasyon'
    Check 'proje etiketi kaydedildi' ($vm.ConfirmEdit() -and ($readProject.Invoke($null, @([Pusula.Core.Tracking.TrackingFiles]::ProjectFile($project))).Tags -join ',') -eq 'Kalifikasyon')
    $vm.SelectNodeByPath($project); Wait
    Check 'proje başlığında etiket' (($vm.Detail.Tags -join ' ') -eq '#Kalifikasyon') ($vm.Detail.Tags -join ' ')
    $vm.SelectNodeByPath($stock); Wait
    $vm.SearchText = '#deneme'; Pump
    Check 'ağaç araması #etiket ile stoku bulur' (@($vm.Nodes | ? Title -eq '7000_MILKON' | % { $_.Children } | ? { $_.IsVisible } | % Title) -join ',' -eq 'AB-7000-0002') (@($vm.Nodes | ? Title -eq '7000_MILKON' | % { $_.Children } | ? { $_.IsVisible } | % Title) -join ',')
    $vm.SearchText = ''; Pump

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
    "--- 6. Sil: _SILINENLER'e taşınır"
    $victim = Join-Path $project 'AB-7000-0001'
    $count = CountAll $victim
    Check 'onay açılır, Sil düğmesi gizli' ($vm.OpenDelete($victim) -and $vm.IsDeleteOpen -and $vm.DeletePrimaryText -eq '')
    $vm.DeleteTyped = 'AB-7000-000'
    Check 'yanlış ad: düğme gizli' ($vm.DeletePrimaryText -eq '')
    Check 'yanlış adla silinmez' (-not $vm.ConfirmDelete() -and (Test-Path $victim))
    $vm.OpenDelete($victim) | Out-Null
    $vm.DeleteTyped = 'ab-7000-0001'
    Check 'doğru ad: Sil düğmesi görünür' ($vm.DeletePrimaryText.Length -gt 0)
    Check 'silindi' ($vm.ConfirmDelete())
    $moved = @(Get-ChildItem (Join-Path $Data '_SILINENLER') -Directory | ForEach-Object { Join-Path $_.FullName '7000_MILKON\AB-7000-0001' } | Where-Object { Test-Path $_ })
    Check 'eski yer boş, silinenlerde göreli yolla duruyor' (-not (Test-Path $victim) -and $moved.Count -eq 1) ($moved -join ',')
    Check 'bütün dosyalar taşındı' ($moved.Count -eq 1 -and (CountAll $moved[0]) -eq $count) ([string]$count)
    Check 'silinenler.log satırı' ((Get-Content (Join-Path $Data '_SILINENLER\silinenler.log') -Encoding UTF8) -match 'AB-7000-0001')
    Check 'ağaçta yok' ((FindNode $vm.Nodes 'AB-7000-0001') -eq $null)
    Check 'kökün dışı reddedilir' ([Pusula.Core.Tracking.RecordEdit]::MoveToTrash($project, $Data, [DateTime]::Now, 'x', [ref]'') -ne '')
} finally {
    if ($savedSettings -ne $null) { [IO.File]::WriteAllText($settingsFile, $savedSettings) }
}
"SONUÇ OK=$ok HATA=$fail"