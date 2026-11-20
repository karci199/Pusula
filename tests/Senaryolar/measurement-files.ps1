param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
. (Join-Path $PSScriptRoot '..\_Ortak.ps1')
# Ölçüm klasörü: kayıtla birlikte boş klasör açılır (tarih_madde_başlık, çakışırsa _2), Günlük'e "klasör:" yazılır;
# içi serbest (alt klasörler sayılır); sonradan kartın menüsünden açılır; düzenlemede korunur. Pusula dosya kopyalamaz.
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
    $olcum = Join-Path $stock '03_Testler\Olcumler'
    $item = New-Object Pusula.Core.Testing.VerificationItem
    $item.TgNo = 'DGÖ-1'; $item.Text = 'Yanca Hüzme Genişliği'; $item.Group = 'İşlevsel'
    [void][Pusula.Core.Testing.VerificationList]::Add($stock, $item, '')
    $vm.SelectNodeByPath($stock); Wait; Wait
    $MF = [Pusula.Core.Notes.MeasurementFiles]

    "--- Klasör adı"
    $d = [datetime]::new(2026, 11, 20)
    Check 'ad: tarih_madde_başlık, Türkçe harfsiz' ($MF::NameFor($d, 'DGÖ-1', 'Yanca Hüzme Genişliği') -eq '2026-11-20_DGO-1_Yanca_Huzme_Genisligi') ($MF::NameFor($d, 'DGÖ-1', 'Yanca Hüzme Genişliği'))
    Check 'madde yoksa yazılmaz, işaretler _' ($MF::NameFor($d, '', 'Karakterizasyon: tarama / 2') -eq '2026-11-20_Karakterizasyon_tarama_2') ($MF::NameFor($d, '', 'Karakterizasyon: tarama / 2'))
    Check 'en fazla 60 karakter' ($MF::NameFor($d, 'EK-1', ('çok uzun başlık ' * 10)).Length -le 60)

    "--- Kayıtla klasör"
    [void]$vm.OpenJournalHere(); $vm.JournalKind = 'Ölçüm'
    $vm.JournalItemOption = ($vm.JournalItemOptions | Where-Object { $_.Key -eq 'DGÖ-1' })
    (@($vm.JournalResultChoices) | Where-Object { $_.Key -eq 'Geçti' }).IsChecked = $true
    $vm.JournalHeading = 'Yanca Hüzme Genişliği'
    Check 'kutu işaretli, yol önizlemesi' ($vm.JournalCreateFolder -and -not $vm.HasJournalFolder -and $vm.JournalFolderText -like '*03_Testler\Olcumler\*_DGO-1_Yanca_Huzme_Genisligi\*') $vm.JournalFolderText
    Check 'eklenir' ($vm.ConfirmJournal()); Wait; Wait
    $folder = Get-ChildItem $olcum -Directory | Where-Object { $_.Name -like '*_DGO-1_Yanca_Huzme_Genisligi' } | Select-Object -First 1
    Check 'boş klasör açıldı' ($folder -ne $null -and @(Get-ChildItem $folder.FullName).Count -eq 0)
    $read = [Pusula.Core.Notes.Journal]::Read($note) | Where-Object { $_.Title -eq 'Yanca Hüzme Genişliği' } | Select-Object -First 1
    Check 'Günlük: klasör yolu' ($read.Folder -eq '03_Testler/Olcumler/' + $folder.Name) $read.Folder

    "--- İçi serbest"
    New-Item -ItemType Directory -Force (Join-Path $folder.FullName 'WaveStudio\Export') | Out-Null
    [IO.File]::WriteAllText((Join-Path $folder.FullName 'WaveStudio\Boynuz.wsp'), 'p')
    [IO.File]::WriteAllText((Join-Path $folder.FullName 'WaveStudio\Export\E_10GHz.csv'), 'e')
    [IO.File]::WriteAllText((Join-Path $folder.FullName 'duzenek.jpg'), 'j')
    $vm.IsRangeAll = $true; $vm.RefreshCommand.Execute($null); Wait; Wait
    $card = @($vm.StockDays | ForEach-Object { $_.Cards }) | Where-Object { $_.Title -eq 'Yanca Hüzme Genişliği' } | Select-Object -First 1
    Check 'kartta klasör, alt klasörler dahil 3 dosya' ($card.HasFolder -and $card.FileCount -eq 3 -and $card.FolderPath -eq $folder.FullName) ([string]$card.FileCount)
    $row = @($vm.VerificationGroups | ForEach-Object { $_.Rows }) | Where-Object { $_.TgNo -eq 'DGÖ-1' } | Select-Object -First 1
    Check 'denemede klasör' ($row.Attempts.Count -ge 1 -and $row.Attempts[0].HasFolder)

    "--- Kutu kapalı ve sonradan klasör"
    [void]$vm.OpenJournalHere(); $vm.JournalKind = 'Ölçüm'; $vm.JournalHeading = 'VSWR ölçümü'; $vm.JournalCreateFolder = $false
    Check 'klasörsüz eklenir' ($vm.ConfirmJournal()); Wait
    $plain = [Pusula.Core.Notes.Journal]::Read($note) | Where-Object { $_.Title -eq 'VSWR ölçümü' } | Select-Object -First 1
    Check 'klasör yok' ($plain.Folder -eq '' -and @(Get-ChildItem $olcum -Directory | Where-Object { $_.Name -like '*VSWR*' }).Count -eq 0)
    Check 'menüden klasör açılır' ($vm.CreateEntryFolder($plain)); Wait
    $plain = [Pusula.Core.Notes.Journal]::Read($note) | Where-Object { $_.Title -eq 'VSWR ölçümü' } | Select-Object -First 1
    Check 'klasör açıldı, kayda yazıldı' ($plain.Folder -like '03_Testler/Olcumler/*_VSWR_olcumu' -and (Test-Path ($MF::Resolve($stock, $plain.Folder)))) $plain.Folder
    Check 'ikinci kez: aynı klasör, yenisi yok' ($vm.CreateEntryFolder($plain) -and @(Get-ChildItem $olcum -Directory | Where-Object { $_.Name -like '*VSWR*' }).Count -eq 1)
    Check 'Ayrıntı başlığı tek' (([regex]::Matches([IO.File]::ReadAllText($note), '### [^\n]*VSWR ölçümü\r?\n[^#]*#### Ayrıntı')).Count -eq 1)

    "--- Ölçüm dışı türde klasör yok"
    [void]$vm.OpenJournalHere(); $vm.JournalKind = 'Hazırlık'; $vm.JournalHeading = 'Tezgâh kuruldu'
    Check 'eklenir' ($vm.ConfirmJournal()); Wait
    Check 'Hazırlıkta klasör açılmaz' (([Pusula.Core.Notes.Journal]::Read($note) | Where-Object { $_.Title -eq 'Tezgâh kuruldu' } | Select-Object -First 1).Folder -eq '')

    "--- Aynı adlı ikinci kayıt ve düzenleme"
    [void]$vm.OpenJournalHere(); $vm.JournalKind = 'Ölçüm'; $vm.JournalHeading = 'Yanca Hüzme Genişliği'; $vm.JournalTimeText = '23:59'
    $vm.JournalItemOption = ($vm.JournalItemOptions | Where-Object { $_.Key -eq 'DGÖ-1' })
    (@($vm.JournalResultChoices) | Where-Object { $_.Key -eq 'Kaldı' }).IsChecked = $true
    Check 'eklenir' ($vm.ConfirmJournal()); Wait
    Check 'ikinci klasör _2' (@(Get-ChildItem $olcum -Directory | Where-Object { $_.Name -like '*_DGO-1_Yanca_Huzme_Genisligi_2' }).Count -eq 1) ((Get-ChildItem $olcum -Directory | ForEach-Object Name) -join ',')
    $vm.OpenJournalEdit($read)
    Check 'düzenlemede yol gösterilir, kutu yok' ($vm.HasJournalFolder -and $vm.JournalFolderText -like ('*' + $folder.Name + '*'))
    $vm.JournalBody = 'Düzeltildi.'
    Check 'düzenleme kaydedilir' ($vm.ConfirmJournal()); Wait
    $kl = [regex]::Matches([IO.File]::ReadAllText($note), 'klasör: 03_Testler/Olcumler/' + [regex]::Escape($folder.Name) + '\r?\n').Count
    Check 'düzenlemede klasör korunur, yenisi açılmaz' ($kl -eq 1 -and @(Get-ChildItem $olcum -Directory).Count -eq 3) ([string]$kl)
    Snap $win "$Out\measurement-files.png"
} finally {
    if ($savedSettings -ne $null) { [IO.File]::WriteAllText($settingsFile, $savedSettings) }
}
"SONUÇ OK=$ok HATA=$fail"
