param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
. (Join-Path $PSScriptRoot '..\_Ortak.ps1')
# Ölçüm kaydı: işlem Ölçüm seçilince Madde (Yok / doğrulama maddeleri / + Yeni ek madde) ve elle Sonuç; Günlük'e madde ve
# sonuç yazılır, maddenin durumu güncellenir, yeni ek madde EK-n olarak tabloya eklenir; kartta rozet, doğrulama satırında deneme.
$settingsFile = Join-Path $env:LOCALAPPDATA 'Pusula\ayarlar.json'
$savedSettings = if (Test-Path $settingsFile) { [IO.File]::ReadAllText($settingsFile) } else { $null }
try {
    [System.Threading.SynchronizationContext]::SetSynchronizationContext((New-Object System.Windows.Threading.DispatcherSynchronizationContext))
    $app = New-Object Pusula.Wpf.App; $app.InitializeComponent(); $app.ShutdownMode = 'OnExplicitShutdown'
    $win = New-Object Pusula.Wpf.MainWindow
    $win.WindowStartupLocation = 'Manual'; $win.Left = -20000; $win.Top = -20000; $win.ShowActivated = $false; $win.Width = 1240; $win.Height = 860
    $win.Show(); Pump
    $vm = $win.DataContext; $vm.RootPath = $Data; $vm.OpenCreatedFolders = $false; Pump
    $stock = Join-Path $Data '7000_MILKON\AB-7000-0002'
    $note = Join-Path $stock 'AB-7000-0002.md'
    $item = New-Object Pusula.Core.Testing.VerificationItem
    $item.TgNo = 'DGÖ-1'; $item.Text = 'Çalışma bandında VSWR'; $item.Group = 'İşlevsel'; $item.Status = 'Geçti'
    [void][Pusula.Core.Testing.VerificationList]::Add($stock, $item, '')
    $vm.SelectNodeByPath($stock); Wait; Wait
    function ItemOf([string]$key) { [Pusula.Core.Testing.VerificationList]::Read($stock) | Where-Object { $_.TgNo -eq $key } | Select-Object -First 1 }

    "--- Alanlar"
    Check 'pencere açılır' ($vm.OpenJournalHere() -and $vm.IsJournalOpen)
    $vm.JournalKind = 'Hazırlık'
    Check 'Hazırlık: ölçüm alanları yok' (-not $vm.IsJournalMeasurement)
    $vm.JournalKind = 'Ölçüm'
    Check 'Ölçüm: ölçüm alanları açılır' ($vm.IsJournalMeasurement)
    $keys = @($vm.JournalItemOptions | ForEach-Object Key)
    Check 'madde listesi: Yok, DGÖ-1, + Yeni ek madde' ($keys[0] -eq '' -and ($keys -contains 'DGÖ-1') -and @($vm.JournalItemOptions)[-1].Text -like '+ Yeni ek madde*') (@($vm.JournalItemOptions | ForEach-Object Text) -join ' | ')
    Check 'DGÖ-1 rozeti durumuyla' ((@($vm.JournalItemOptions | Where-Object { $_.Key -eq 'DGÖ-1' })[0]).Badge -eq 'Geçti')
    Check 'madde yok: sonuç yalnız Bilgi, seçili' (@($vm.JournalResultChoices).Count -eq 1 -and $vm.JournalResult -eq 'Bilgi')

    "--- Maddeye bağlı ölçüm"
    $vm.JournalHeading = 'İlk VSWR ölçümü'; $vm.JournalBody = '8,2 GHz''de 1,72.'; $vm.JournalTags = '#SN-003'
    $vm.JournalItemOption = ($vm.JournalItemOptions | Where-Object { $_.Key -eq 'DGÖ-1' })
    Check 'madde seçilince Geçti / Kaldı, sonuç boş' (@($vm.JournalResultChoices | ForEach-Object Key) -join ',' -eq 'Geçti,Kaldı' -and $vm.JournalResult -eq '')
    Check 'sonuç seçilmeden Ekle yok' ($vm.JournalPrimaryText -eq '')
    (@($vm.JournalResultChoices) | Where-Object { $_.Key -eq 'Kaldı' }).IsChecked = $true
    Check 'Kaldı seçilince Ekle var, durum değişimi yazılı' ($vm.JournalPrimaryText -ne '' -and $vm.JournalResultHint -like '*Geçti*Kaldı*') $vm.JournalResultHint
    Snap $win "$Out\measurement-dialog.png"
    Check 'eklenir' ($vm.ConfirmJournal()); Wait; Wait
    $txt = [IO.File]::ReadAllText($note)
    Check 'Günlük: madde ve sonuç' ($txt.Contains('madde: DGÖ-1 · sonuç: Kaldı'))
    Check 'madde durumu Kaldı' ((ItemOf 'DGÖ-1').EffectiveStatus -eq 'Kaldı')
    $read = [Pusula.Core.Notes.Journal]::Read($note) | Where-Object { $_.Title -eq 'İlk VSWR ölçümü' } | Select-Object -First 1
    Check 'okunur: Item, Result' ($read.Item -eq 'DGÖ-1' -and $read.Result -eq 'Kaldı' -and ($read.Tags -contains 'SN-003'))

    "--- Kart ve doğrulama satırı"
    $vm.IsRangeAll = $true; Wait
    $card = @($vm.StockDays | ForEach-Object { $_.Cards }) | Where-Object { $_.Title -eq 'İlk VSWR ölçümü' } | Select-Object -First 1
    Check 'kart rozeti: ↳ DGÖ-1 · Kaldı' ($card -and $card.ResultText -eq '↳ DGÖ-1 · Kaldı' -and $card.ResultKind -eq 2) $card.ResultText
    $row = @($vm.VerificationGroups | ForEach-Object { $_.Rows }) | Where-Object { $_.TgNo -eq 'DGÖ-1' } | Select-Object -First 1
    Check 'satırda 1 deneme, Kaldı' ($row -and $row.Attempts.Count -eq 1 -and $row.Attempts[0].Result -eq 'Kaldı' -and $row.AttemptCountText -eq '1 deneme') $row.AttemptCountText
    Check '+ Deneme: Ölçüm ve madde seçili açılır' ($vm.OpenMeasurementFor($row) -and $vm.IsJournalMeasurement -and $vm.JournalItem -eq 'DGÖ-1')
    $vm.IsJournalOpen = $false; Pump

    "--- Yeni ek madde"
    [void]$vm.OpenJournalHere(); $vm.JournalKind = 'Ölçüm'
    $vm.JournalItemOption = @($vm.JournalItemOptions)[-1]
    Check 'yeni ek madde kutusu: EK-1' ($vm.IsJournalNewItem -and $vm.JournalNewItemCaption -like '*EK-1') $vm.JournalNewItemCaption
    $vm.JournalHeading = 'Ön-arka oranı'
    (@($vm.JournalResultChoices) | Where-Object { $_.Key -eq 'Geçti' }).IsChecked = $true
    Check 'metin yazılmadan Ekle yok' ($vm.JournalPrimaryText -eq '')
    $vm.JournalNewItemText = 'Ön-arka oranı ≥ 20 dB'; $vm.JournalNewItemSource = 'ANT-TSR-012 §4.2'
    Check 'metinle Ekle var' ($vm.JournalPrimaryText -ne '')
    Check 'eklenir' ($vm.ConfirmJournal()); Wait; Wait
    $ek = ItemOf 'EK-1'
    Check 'tabloya EK-1: metin, kaynak, durum Geçti' ($ek -and $ek.Text -eq 'Ön-arka oranı ≥ 20 dB' -and $ek.Source -eq 'ANT-TSR-012 §4.2' -and $ek.EffectiveStatus -eq 'Geçti')
    Check 'Günlük: madde EK-1' ([IO.File]::ReadAllText($note).Contains('madde: EK-1 · sonuç: Geçti'))
    Check 'sıradaki ek no EK-2' ([Pusula.Core.Testing.VerificationList]::NextExtraNo([Pusula.Core.Testing.VerificationList]::Read($stock)) -eq 'EK-2')

    "--- Bilgi amaçlı"
    [void]$vm.OpenJournalHere(); $vm.JournalKind = 'Ölçüm'; $vm.JournalHeading = 'Karakterizasyon taraması'
    Check 'Bilgi ile eklenir' ($vm.JournalPrimaryText -ne '' -and $vm.ConfirmJournal()); Wait
    $txt = [IO.File]::ReadAllText($note)
    Check 'Günlük: sonuç Bilgi, madde yok' ($txt -match '### [^\n]*Karakterizasyon taraması\r?\n[^\n]*sonuç: Bilgi' -and -not ($txt -match 'Karakterizasyon taraması\r?\n[^\n]*madde:'))
    Check 'DGÖ-1 durumu değişmedi' ((ItemOf 'DGÖ-1').EffectiveStatus -eq 'Kaldı')
    "--- MD'de eklenen ayrıntı"
    $txt = [IO.File]::ReadAllText($note)
    Check 'kayda boş Ayrıntı başlığı yazılır' ($txt -match "8,2 GHz'de 1,72\.\r?\n\r?\n#### Ayrıntı\r?\n")
    $txt = [regex]::Replace($txt, "(8,2 GHz'de 1,72\.\r?\n\r?\n#### Ayrıntı)(\r?\n)", '$1$2MD''de yazılan uzun ayrıntı.$2', 1)
    [IO.File]::WriteAllText($note, $txt, (New-Object Text.UTF8Encoding $true))
    $vm.RefreshCommand.Execute($null); Wait; Wait; $vm.IsRangeAll = $true; Wait
    $card = @($vm.StockDays | ForEach-Object { $_.Cards }) | Where-Object { $_.Title -eq 'İlk VSWR ölçümü' } | Select-Object -First 1
    Check 'kartta yalnız Ayrıntı başlığından öncesi' ($card -and $card.Description -eq "8,2 GHz'de 1,72." -and $card.FullDescription -eq "8,2 GHz'de 1,72.") $card.FullDescription
    $edit = [Pusula.Core.Notes.Journal]::Read($note) | Where-Object { $_.Title -eq 'İlk VSWR ölçümü' } | Select-Object -First 1
    $vm.OpenJournalEdit($edit)
    Check 'düzenlemede yalnız açıklama' ($vm.JournalBody -eq "8,2 GHz'de 1,72.") $vm.JournalBody
    $vm.JournalBody = "8,2 GHz'de 1,72 (düzeltildi)."
    Check 'düzenleme kaydedilir' ($vm.ConfirmJournal()); Wait
    $txt = [IO.File]::ReadAllText($note)
    Check 'ayrıntı korunur, başlık tek' ($txt.Contains("MD'de yazılan uzun ayrıntı.") -and ([regex]::Matches($txt, 'düzeltildi\)\.\r?\n\r?\n#### Ayrıntı\r?\nMD''de yazılan')).Count -eq 1)

    $vm.IsTimelineDone = $true; $win.Height = 1500
    $open = @($vm.VerificationGroups | ForEach-Object { $_.Rows }) | Where-Object { $_.TgNo -eq 'DGÖ-1' } | Select-Object -First 1
    if ($open) { $open.IsExpanded = $true }
    Snap $win "$Out\measurement-card.png"
} finally {
    if ($savedSettings -ne $null) { [IO.File]::WriteAllText($settingsFile, $savedSettings) }
}
"SONUÇ OK=$ok HATA=$fail"
