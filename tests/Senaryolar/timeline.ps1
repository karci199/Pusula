param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
. (Join-Path $PSScriptRoot '..\_Ortak.ps1')
# Zaman çizgisi: kayıt sayfasında olaylar tarih ekseninde; Geçmiş süzgeci (Aşama / Revizyon / Konum / Not); konum
# değişikliği "eski → yeni"; stok çizelgesinde bağlı jigin durum ve Günlük olayları, "Yalnız stok" süzgeci.
$root = Join-Path $env:TEMP ('pusula-tl-' + [guid]::NewGuid().ToString('N').Substring(0,6))
New-Item -ItemType Directory "$root\7000_MILKON\AB-7000-0003" -Force | Out-Null
[Pusula.Core.Tracking.ProjectService]::MarkAsProject("$root\7000_MILKON", '7000', 'MILKON') | Out-Null
[Pusula.Core.Tracking.ProjectService]::MarkAsStock("$root\7000_MILKON\AB-7000-0003", 'AB-7000-0003', '', 'Hazırlık') | Out-Null
try {
    [System.Threading.SynchronizationContext]::SetSynchronizationContext((New-Object System.Windows.Threading.DispatcherSynchronizationContext))
    $app = New-Object Pusula.Wpf.App; $app.InitializeComponent(); $app.ShutdownMode = 'OnExplicitShutdown'
    $app.add_DispatcherUnhandledException({ param($s, $e) "  YAKALANAN HATA: " + $e.Exception.Message; $e.Handled = $true })
    $win = New-Object Pusula.Wpf.MainWindow
    $win.WindowStartupLocation = 'Manual'; $win.Left = -20000; $win.Top = -20000; $win.ShowActivated = $false
    $win.Width = 1360; $win.Height = 900
    $win.Show(); Pump
    $vm = $win.DataContext
    $vm.RootPath = $root; Pump

    "--- 1. Jig ve olaylar"
    $stock = FindNode $vm.Nodes 'AB-7000-0003'
    $vm.SelectedNode = $stock; Pump
    $types = [Pusula.Core.Types.TypeStore]::LoadOrCreate($root)
    $vm.OpenNewInventory($types.Find('jig'))
    $vm.NewInventoryNumber = 'JIG-1000-0009'; $vm.NewInventoryName = 'SATIMO adaptör plakası'
    $vm.NewInventoryRequestDateText = '02.09.2026'
    Check 'jig açıldı' ($vm.ConfirmNewInventory()); Wait
    foreach ($e in @(@('Hazırlanıyor','2026-09-05','Mekanik atölye'), @('Teslim alındı','2026-09-18',''), @('Kullanımda','2026-09-20','SATIMO odası'), @('Onarım / değişim gerekiyor','2026-10-22','Mekanik atölye'), @('Kullanımda','2026-10-28','SATIMO odası'))) {
        $vm.AskInventoryEvent($e[0]); $vm.EventDate = [datetime]$e[1]; $vm.EventLocation = $e[2]; $vm.EventNote = 'deneme'; $vm.ConfirmInventoryEvent(); Wait
    }
    $vm.OpenJournalHere() | Out-Null
    $vm.JournalKind = 'Revizyon'; $vm.JournalDate = [datetime]'2026-10-23'; $vm.JournalHeading = 'Rev AB → AC'; $vm.JournalBody = 'delikler kaydırıldı'
    Check 'revizyon eklendi' ($vm.ConfirmJournal()); Wait; Start-Sleep -Milliseconds 400; Wait
    $hist = @($vm.Inventory.History)
    Check 'geçmiş: 6 durum + revizyon' ($hist.Count -eq 7) ([string]$hist.Count)
    $loc = @($hist | ? { $_.IsLocationChange })
    Check 'konum değişiklikleri (4) ve eski → yeni' ($loc.Count -eq 4 -and @($loc | ? { $_.Detail -match 'SATIMO odası → Mekanik atölye' }).Count -eq 1) (($loc | % Detail) -join ' | ')
    Check 'revizyon satırı' (@($hist | ? { $_.IsRevision -and $_.Status.Name -eq 'Revizyon' }).Count -eq 1)

    "--- 2. Süzgeç ve zaman çizgisi"
    $axis = $win.FindName('RecordTimeAxis'); $axis.UpdateLayout(); Wait
    Check 'eksende hepsi çizildi' ($axis.PointCount -eq $hist.Count -and $axis.LabelCount -gt 0) ("$($axis.PointCount)/$($hist.Count) etiket $($axis.LabelCount)")
    $f = $vm.Inventory.HistoryFilter
    Check 'süzgeç seçenekleri' ((($f.Options | % Label) -join ',') -eq 'Hepsi,Aşama,Revizyon,Konum,Not')
    $f.Options[2].IsSelected = $true; Wait
    $view = [System.Windows.Data.CollectionViewSource]::GetDefaultView($vm.Inventory.History)
    Check 'Revizyon: yalnız revizyon, eksen de süzülür' (@($view).Count -eq 1 -and $axis.PointCount -eq 1 -and $f.Selected -eq 'revision' -and -not $f.Options[0].IsSelected) ("$(@($view).Count) / $($axis.PointCount)")
    $f.Select('location'); Wait
    Check 'Konum: 4' (@($view).Count -eq 4)
    $f.Select('stage'); Wait
    Check 'Aşama: 6' (@($view).Count -eq 6)
    $f.Options[1].IsSelected = $false
    Check 'seçili seçenek bırakılamaz' ($f.Selected -eq 'stage' -and $f.Options[1].IsSelected)
    $f.Select('all'); Wait
    Snap $win "$Out\timeline-record.png"

    "--- 3. Stok çizelgesi"
    $vm.SelectedNode = $stock; Wait; Start-Sleep -Milliseconds 400; Wait
    $tl = @($vm.Detail.Timeline)
    Check 'bağlı jigin durumları ve revizyonu stokta' (@($tl | ? { $_.IsLinked -and $_.IsStage }).Count -eq 6 -and @($tl | ? { $_.IsLinked -and $_.IsRevision }).Count -eq 1) ([string]$tl.Count)
    Check 'stokta konum eski → yeni' (@($tl | ? { $_.Detail -match 'SATIMO odası → Mekanik atölye' }).Count -eq 1)
    $sf = $vm.Detail.TimelineFilter
    Check 'stok süzgeci Yalnız stok içerir' (($sf.Options | % Key) -contains 'own')
    $sv = [System.Windows.Data.CollectionViewSource]::GetDefaultView($vm.Detail.Timeline)
    $sf.Select('own'); Wait
    Check 'Yalnız stok: bağlı kayıt yok' (@($sv | ? { $_.IsLinked }).Count -eq 0)
    $sf.Select('revision'); Wait
    Check 'stokta Revizyon süzgeci' (@($sv).Count -eq 1)
    $sf.Select('all'); Wait
    $sa = $win.FindName('StockTimeAxis')
    Check 'stok ekseni çizildi' ($sa.PointCount -gt 0) ([string]$sa.PointCount)
    Snap $win "$Out\timeline-stock.png"
    $win.Close()
} finally { Remove-Item -Recurse -Force $root -ErrorAction SilentlyContinue }