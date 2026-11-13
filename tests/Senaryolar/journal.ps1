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
    "--- Stok: + Olay"
    $stock = Join-Path $Data '7000_MILKON\AB-7000-0002'
    $note = Join-Path $stock 'AB-7000-0002.md'
    $before = [IO.File]::ReadAllText($note)
    $vm.SelectNodeByPath($stock); Wait
    Check 'pencere açılır, Ekle gizli, tarih bugün' ($vm.OpenJournalHere() -and $vm.IsJournalOpen -and $vm.JournalPrimaryText -eq '' -and $vm.JournalDateText -eq [datetime]::Today.ToString('dd.MM.yyyy'))
    Check 'ilgili: seri numaraları' (@($vm.JournalRelated | ForEach-Object Number) -contains 'SN01') (($vm.JournalRelated | ForEach-Object Number) -join ',')
    $vm.JournalKind = 'Toplantı'; $vm.JournalDate = [datetime]'2026-11-05'; $vm.JournalHeading = 'MILKON haftalık'; $vm.JournalBody = 'SN01 paylaşıldı.'
    ($vm.JournalRelated | Where-Object Number -eq 'SN01').IsChecked = $true; $vm.JournalTags = 'MILKON'
    Check 'eklendi' ($vm.ConfirmJournal()); Wait
    $vm.OpenJournalHere() | Out-Null
    $vm.JournalKind = 'Karar'; $vm.JournalDateText = '12.11.2026'; $vm.JournalHeading = 'Konnektör N tipine'; $vm.JournalImportance = 'Yüksek'
    Check 'ikinci eklendi' ($vm.ConfirmJournal()); Wait
    $after = [IO.File]::ReadAllText($note)
    Check 'notun önceki kısmı aynen duruyor' ($after.StartsWith($before.TrimEnd()))
    $iKarar = $after.IndexOf('### 12.11.2026 · Karar · Konnektör N tipine'); $iToplanti = $after.IndexOf('### 05.11.2026 · Toplantı · MILKON haftalık')
    Check 'en yeni üstte' ($iKarar -gt 0 -and $iToplanti -gt $iKarar) ("$iKarar / $iToplanti")
    Check 'bilgi satırı' ($after.Contains('kişi: ') -and $after.Contains('ilgili: [[SN01]]') -and $after.Contains('#MILKON') -and $after.Contains('önem: Yüksek'))
    $titles = @($vm.Detail.Timeline | ForEach-Object Title)
    Check 'Günlük sekmesinde görünür' (($titles -contains 'Konnektör N tipine') -and ($titles -contains 'MILKON haftalık')) ($titles -join ' | ')

    "--- Düzenle / sil"
    $item = @($vm.Detail.Timeline | Where-Object { $_.Title -eq 'Konnektör N tipine' })[0]
    Check 'günlük olayı düzenlenebilir' ($item -ne $null -and $item.IsJournal)
    $vm.OpenJournalEdit($item.Journal)
    Check 'pencere dolu açıldı' ($vm.JournalHeading -eq 'Konnektör N tipine' -and $vm.JournalImportance -eq 'Yüksek' -and $vm.JournalPrimaryText -eq 'Kaydet')
    $vm.JournalHeading = 'Konnektör N tipine çevrildi'; $vm.JournalImportance = 'Normal'
    $vm.ConfirmJournal() | Out-Null; Wait
    $txt = [IO.File]::ReadAllText($note)
    Check 'düzenlendi, çiftlenmedi' ($txt.Contains('### 12.11.2026 · Karar · Konnektör N tipine çevrildi') -and -not $txt.Contains('Konnektör N tipine' + "`r`n") -and -not $txt.Contains('önem: Yüksek'))
    $toDelete = @($vm.Detail.Timeline | Where-Object { $_.Title -eq 'MILKON haftalık' })[0].Journal
    $vm.OpenJournalDelete($toDelete)
    $vm.ConfirmJournalDelete() | Out-Null; Wait
    $txt = [IO.File]::ReadAllText($note)
    Check 'silindi, diğeri duruyor, not başı aynı' (-not $txt.Contains('### 05.11.2026 · Toplantı · MILKON haftalık') -and $txt.Contains('Konnektör N tipine çevrildi') -and $txt.StartsWith($before.TrimEnd()))
    "--- Proje günlüğü"
    $project = Join-Path $Data '7000_MILKON'
    $vm.SelectNodeByPath($project); Wait
    Check 'projede pencere açılır' ($vm.OpenJournalHere())
    $vm.JournalKind = 'Toplantı'; $vm.JournalHeading = 'Proje haftalık'
    $vm.ConfirmJournal() | Out-Null; Wait
    Check 'proje notuna yazıldı' ((Test-Path (Join-Path $project '7000_MILKON.md')) -and ([IO.File]::ReadAllText((Join-Path $project '7000_MILKON.md'))).Contains('· Toplantı · Proje haftalık'))
    Check 'proje sayfasında görünür' (@($vm.Detail.ProjectNotes | Where-Object { $_.Detail -like 'Proje haftalık*' }).Count -eq 1) (($vm.Detail.ProjectNotes | ForEach-Object Detail) -join ' | ')
    "--- Stoka ait jig: stoktaki ilgili olay Geçmiş'te"
    $stock3 = Join-Path $Data '7000_MILKON\AB-7000-0003'
    $vm.SelectNodeByPath($stock3); Wait
    $vm.OpenJournalHere() | Out-Null
    $opt = $vm.JournalRelated | Where-Object Number -eq 'JIG-1000-0014'
    Check 'ilgili: stoktaki jig' ($opt -ne $null) (($vm.JournalRelated | ForEach-Object Number) -join ',')
    if ($opt) { $opt.IsChecked = $true }
    $vm.JournalKind = 'Teslim'; $vm.JournalHeading = 'JIG-1000-0014 üretime teslim edildi'
    $vm.ConfirmJournal() | Out-Null; Wait
    $vm.SelectNodeByPath((Join-Path $stock3 '05_Jigler\JIG-1000-0014_Doner_tabla_montaj_aparati')); Wait; Wait
    $hist = @($vm.Inventory.History | ForEach-Object Detail)
    Check 'jigin Geçmiş''inde' (@($hist | Where-Object { $_ -like '*üretime teslim edildi*' }).Count -eq 1) ($hist -join ' | ')
    Snap $win "$Out\journal.png"
} finally {
    if ($savedSettings -ne $null) { [IO.File]::WriteAllText($settingsFile, $savedSettings) }
}
"SONUÇ OK=$ok HATA=$fail"