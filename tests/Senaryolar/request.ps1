param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
. (Join-Path $PSScriptRoot '..\_Ortak.ps1')
# Talep kaydı: tür (Mekanik, Kablaj, Cihaz, Malzeme, Test altyapı, Test destek, Diğer), talep no, kimden, istenen tarih;
# türe göre ilgili kayıt / kalemler / istenen. Açık başlar, kartta "Teslim alındı" ile kapanır ve teslim kaydı düşer.
$settingsFile = Join-Path $env:LOCALAPPDATA 'Pusula\ayarlar.json'
$savedSettings = if (Test-Path $settingsFile) { [IO.File]::ReadAllText($settingsFile) } else { $null }
try {
    [System.Threading.SynchronizationContext]::SetSynchronizationContext((New-Object System.Windows.Threading.DispatcherSynchronizationContext))
    $app = New-Object Pusula.Wpf.App; $app.InitializeComponent(); $app.ShutdownMode = 'OnExplicitShutdown'
    $win = New-Object Pusula.Wpf.MainWindow
    $win.WindowStartupLocation = 'Manual'; $win.Left = -20000; $win.Top = -20000; $win.ShowActivated = $false; $win.Width = 1500; $win.Height = 1100
    $win.Show(); Pump
    $vm = $win.DataContext; $vm.RootPath = $Data; $vm.OpenCreatedFolders = $false; Pump
    $stock = Join-Path $Data '7000_MILKON\AB-7000-0002'
    $note = Join-Path $stock 'AB-7000-0002.md'
    $vm.SelectNodeByPath($stock); Wait; Wait
    $due = (Get-Date).Date.AddDays(4).ToString('dd.MM.yyyy')
    $past = (Get-Date).Date.AddDays(-2).ToString('dd.MM.yyyy')
    function EntryOf([string]$title) { [Pusula.Core.Notes.Journal]::Read($note) | Where-Object { $_.Title -eq $title } | Select-Object -First 1 }
    function Pick([string]$type) { (@($vm.JournalRequestTypeChoices) | Where-Object { $_.Key -eq $type }).IsChecked = $true }

    "--- Alanlar"
    [void]$vm.OpenJournalHere(); $vm.JournalKind = 'Talep'
    Check 'Talep: talep alanları açılır' ($vm.IsJournalRequest -and -not $vm.IsJournalMeeting -and -not $vm.IsJournalMeasurement)
    $types = @($vm.JournalRequestTypeChoices)
    Check 'yedi tür ikonlu' (($types | ForEach-Object Key) -join ',' -eq 'Mekanik,Kablaj,Cihaz,Malzeme,Test altyapı,Test destek,Diğer' -and ($types | ForEach-Object Badge) -join ',' -eq 'mechanic,cable,device,material,infra,support,other')
    Pick 'Malzeme'
    Check 'Malzeme: kalemler görünür, ilgili kayıt yok' ($vm.ShowsRequestItems -and -not $vm.ShowsRequestRecord -and -not $vm.ShowsRequestDetail -and $vm.JournalRequestItems.Count -eq 1)

    "--- Malzeme talebi"
    $vm.JournalRequestNo = 'AMB-2026-1182'; $vm.JournalRequestFrom = '@Ambar'; $vm.JournalRequestDueText = $due
    $r = $vm.JournalRequestItems[0]; $r.StockNo = 'KN-0412-0007'; $r.Name = 'SMA erkek konnektör'; $r.Quantity = '10'
    $vm.AddRequestItemRow(); $r2 = $vm.JournalRequestItems[1]; $r2.Name = '20 dB zayıflatıcı'; $r2.Quantity = '2'
    $vm.AddRequestItemRow()
    $vm.JournalHeading = 'Konnektör ve zayıflatıcı'
    Check 'eklenir' ($vm.ConfirmJournal()); Wait; Wait
    $txt = [IO.File]::ReadAllText($note)
    Check 'bilgi satırı: tür, no, kimden, istenen, durum' ($txt -match ('Konnektör ve zayıflatıcı\r?\n[^\n]*talep: Malzeme · talep no: AMB-2026-1182 · kimden: @Ambar · istenen: ' + [regex]::Escape($due) + ' · durum: Açık'))
    Check 'kalemler başlığın altında' ($txt -match '#### Kalemler\r?\n- KN-0412-0007 · SMA erkek konnektör · 10\r?\n- — · 20 dB zayıflatıcı · 2')
    $m = EntryOf 'Konnektör ve zayıflatıcı'
    Check 'okunur: tür, kalemler, durum, özet temiz' ($m.RequestType -eq 'Malzeme' -and $m.RequestItems.Count -eq 2 -and $m.RequestItems[1].StockNo -eq '' -and $m.RequestStatus -eq 'Açık' -and $m.RequestFrom[0] -eq 'Ambar' -and $m.Excerpt -eq '') $m.Excerpt

    "--- Mekanik (gecikmiş) ve Test altyapı"
    [void]$vm.OpenJournalHere(); $vm.JournalKind = 'Talep'; Pick 'Mekanik'
    Check 'Mekanik: ilgili kayıt görünür' ($vm.ShowsRequestRecord -and -not $vm.ShowsRequestItems)
    $vm.JournalRequestRecord = 'JIG-0012'; $vm.JournalRequestFrom = '@Mekanik_Atölye'; $vm.JournalRequestDueText = $past; $vm.JournalHeading = 'Boynuz test jigi rev B'
    Check 'eklenir' ($vm.ConfirmJournal()); Wait
    $j = EntryOf 'Boynuz test jigi rev B'
    Check 'ilgili kayıt bağı' (($j.Related -contains 'JIG-0012') -and [IO.File]::ReadAllText($note).Contains('ilgili: [[JIG-0012]]'))
    [void]$vm.OpenJournalHere(); $vm.JournalKind = 'Talep'; Pick 'Test altyapı'
    Check 'Test altyapı: istenen görünür' ($vm.ShowsRequestDetail -and $vm.JournalRequestDetailHint -like '*CE102*')
    $vm.JournalRequestDetail = 'CE102, RE102'; $vm.JournalHeading = 'EMC ölçümü'
    Check 'eklenir' ($vm.ConfirmJournal()); Wait
    Check 'istenen başlığın altında' ([IO.File]::ReadAllText($note) -match '#### İstenen\r?\nCE102, RE102')
    [void]$vm.OpenJournalHere(); $vm.JournalKind = 'Talep'; $vm.JournalHeading = 'Türsüz talep'
    Check 'tür seçilmezse Diğer' ($vm.ConfirmJournal() -and (EntryOf 'Türsüz talep').RequestType -eq 'Diğer'); Wait
    [void]$vm.OpenJournalHere(); $vm.JournalKind = 'Talep'; $vm.JournalHeading = 'Yanlış tarih'; $vm.JournalRequestDueText = '45.13.2026'
    Check 'yanlış tarihte yazılmaz' (-not $vm.ConfirmJournal() -and -not (EntryOf 'Yanlış tarih'))

    "--- Kart ve bekleyen talepler"
    $vm.RefreshCommand.Execute($null); Wait; Wait; $vm.IsRangeAll = $true; Wait
    $cards = @($vm.StockDays | ForEach-Object { $_.Cards })
    $mc = $cards | Where-Object { $_.Title -eq 'Konnektör ve zayıflatıcı' } | Select-Object -First 1
    Check 'kart: Malzeme, açık, kalemler, no · kimden' ($mc.IsRequest -and $mc.RequestIcon -eq 'material' -and $mc.RequestStatusKind -eq 1 -and $mc.IsRequestOpen -and $mc.RequestLines.Contains('SMA erkek konnektör') -and $mc.RequestMeta -eq 'AMB-2026-1182 · Ambar') $mc.RequestMeta
    $jc = $cards | Where-Object { $_.Title -eq 'Boynuz test jigi rev B' } | Select-Object -First 1
    Check 'gecikmiş talep' ($jc.RequestStatusKind -eq 2 -and $jc.RequestStatusText -like '*gecikti') $jc.RequestStatusText
    Check 'bekleyen talepler: 4, gecikmiş önce' ($vm.HasOpenRequests -and $vm.OpenRequests.Count -eq 4 -and $vm.OpenRequests[0].Title -eq 'Boynuz test jigi rev B') ([string]$vm.OpenRequests.Count)

    "--- Düzenleme"
    $vm.OpenJournalEdit($m)
    Check 'düzenlemede alanlar dolu' ($vm.JournalRequestType -eq 'Malzeme' -and $vm.JournalRequestNo -eq 'AMB-2026-1182' -and @($vm.JournalRequestItems | Where-Object { -not $_.IsEmpty }).Count -eq 2 -and $vm.JournalBody -eq '')
    $vm.JournalBody = 'Ölçüm düzeneği için.'
    Check 'kaydedilir' ($vm.ConfirmJournal()); Wait
    $txt = [IO.File]::ReadAllText($note)
    Check 'kalemler korunur, başlık tek' (([regex]::Matches($txt, '#### Kalemler')).Count -eq 1 -and $txt.Contains('- KN-0412-0007 · SMA erkek konnektör · 10'))

    "--- Teslim"
    $m = EntryOf 'Konnektör ve zayıflatıcı'
    Check 'teslim alınır' ($vm.MarkRequestDelivered($m)); Wait; Wait
    $m = [Pusula.Core.Notes.Journal]::Read($note) | Where-Object { $_.Title -eq 'Konnektör ve zayıflatıcı' -and $_.RequestType -eq 'Malzeme' } | Select-Object -First 1
    $d = [Pusula.Core.Notes.Journal]::Read($note) | Where-Object { $_.Title -eq 'Konnektör ve zayıflatıcı' -and $_.KindLabel -eq 'Teslim' } | Select-Object -First 1
    Check 'talep kapandı, teslim kaydı düştü' ($m.RequestStatus -eq 'Teslim alındı' -and $d -ne $null -and $d.Excerpt -like 'Malzeme talebi teslim alındı*AMB-2026-1182') ($m.RequestStatus + ' | ' + $(if ($d) { $d.Excerpt } else { 'kayıt yok: ' + ((([Pusula.Core.Notes.Journal]::Read($note)) | Select-Object -First 3 | ForEach-Object { $_.KindLabel + '/' + $_.Title }) -join ', ') }))
    Check 'ikinci kez kapatılmaz' (-not $vm.MarkRequestDelivered($m))
    Check 'bekleyen talepler: 3' ($vm.OpenRequests.Count -eq 3) ([string]$vm.OpenRequests.Count)
    $vm.IsTimelineDone = $true; Wait
    # Pencerenin görüntüsü (ayrı katmanda çizilir): Malzeme talebi dolu
    function Find-Open($node) { if ($node -eq $null) { return $null }; if ($node.GetType().Name -eq 'ContentDialog' -and $node.IsOpen) { return $node }; for ($i = 0; $i -lt [System.Windows.Media.VisualTreeHelper]::GetChildrenCount($node); $i++) { $r = Find-Open ([System.Windows.Media.VisualTreeHelper]::GetChild($node, $i)); if ($r) { return $r } }; return $null }
    function Find-Popup($node) { if ($node -is [System.Windows.Controls.Primitives.Popup]) { return $node }; for ($i = 0; $i -lt [System.Windows.Media.VisualTreeHelper]::GetChildrenCount($node); $i++) { $r = Find-Popup ([System.Windows.Media.VisualTreeHelper]::GetChild($node, $i)); if ($r) { return $r } }; return $null }
    [void]$vm.OpenJournalHere(); $vm.JournalKind = 'Talep'; Pick 'Malzeme'
    $vm.JournalRequestNo = 'AMB-2026-1190'; $vm.JournalRequestFrom = '@Ambar'; $vm.JournalRequestDueText = $due; $vm.JournalHeading = 'Ölçüm düzeneği için konnektör'
    $r = $vm.JournalRequestItems[0]; $r.StockNo = 'KN-0412-0007'; $r.Name = 'SMA erkek konnektör'; $r.Quantity = '10'; $vm.AddRequestItemRow()
    for ($k = 0; $k -lt 20; $k++) { Pump; Start-Sleep -Milliseconds 30 }
    $dlg = Find-Open $win
    if ($dlg) {
        $pop = Find-Popup $dlg; if ($pop -and $pop.Child) { $dlg = $pop.Child; $sz = New-Object System.Windows.Size($win.ActualWidth, $win.ActualHeight); $dlg.Measure($sz); $dlg.Arrange((New-Object System.Windows.Rect($sz))); $dlg.UpdateLayout(); for ($k = 0; $k -lt 20; $k++) { Pump; Start-Sleep -Milliseconds 30 } }
        $bmp = New-Object System.Windows.Media.Imaging.RenderTargetBitmap([int]$dlg.ActualWidth, [int]$dlg.ActualHeight, 96, 96, [System.Windows.Media.PixelFormats]::Pbgra32)
        $bmp.Render($dlg); $enc = New-Object System.Windows.Media.Imaging.PngBitmapEncoder; $enc.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($bmp))
        $fs = [IO.File]::Create("$Out\request-dialog.png"); $enc.Save($fs); $fs.Close()
    }
    $vm.IsJournalOpen = $false; Pump
    Snap $win "$Out\request.png"
} finally {
    if ($savedSettings -ne $null) { [IO.File]::WriteAllText($settingsFile, $savedSettings) }
}
"SONUÇ OK=$ok HATA=$fail"
