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
    Check 'pencere açılır, Ekle gizli' ($vm.OpenJournalHere() -and $vm.IsJournalOpen -and $vm.JournalPrimaryText -eq '')
    Check 'ilgili: seri numaraları' (@($vm.JournalRelated | ForEach-Object Number) -contains 'SN01') (($vm.JournalRelated | ForEach-Object Number) -join ',')
    $vm.JournalKind = 'Toplantı'; $vm.JournalDate = [datetime]'2026-11-05'; $vm.JournalHeading = 'MILKON haftalık'; $vm.JournalBody = 'SN01 paylaşıldı.'
    ($vm.JournalRelated | Where-Object Number -eq 'SN01').IsChecked = $true; $vm.JournalTags = 'MILKON'
    Check 'eklendi' ($vm.ConfirmJournal()); Wait
    $vm.OpenJournalHere() | Out-Null
    $vm.JournalKind = 'Karar'; $vm.JournalDate = [datetime]'2026-11-12'; $vm.JournalHeading = 'Konnektör N tipine'; $vm.JournalImportance = 'Yüksek'
    Check 'ikinci eklendi' ($vm.ConfirmJournal()); Wait
    $after = [IO.File]::ReadAllText($note)
    Check 'notun önceki kısmı aynen duruyor' ($after.StartsWith($before.TrimEnd()))
    $iKarar = $after.IndexOf('### 12.11.2026 · Karar · Konnektör N tipine'); $iToplanti = $after.IndexOf('### 05.11.2026 · Toplantı · MILKON haftalık')
    Check 'en yeni üstte' ($iKarar -gt 0 -and $iToplanti -gt $iKarar) ("$iKarar / $iToplanti")
    Check 'bilgi satırı' ($after.Contains('kişi: ') -and $after.Contains('ilgili: [[SN01]]') -and $after.Contains('#MILKON') -and $after.Contains('önem: Yüksek'))
    $titles = @($vm.Detail.Timeline | ForEach-Object Title)
    Check 'Günlük sekmesinde görünür' (($titles -contains 'Konnektör N tipine') -and ($titles -contains 'MILKON haftalık')) ($titles -join ' | ')

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