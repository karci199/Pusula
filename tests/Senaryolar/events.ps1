param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
. (Join-Path $PSScriptRoot '..\_Ortak.ps1')
$E = [Pusula.Core.Notes.EventNote]
"--- 1. Okuyucu"
$tmp = Join-Path $env:TEMP ('pusula-ev-' + [guid]::NewGuid().ToString('N').Substring(0,6)); New-Item -ItemType Directory "$tmp\Notlar","$tmp\_Arsiv","$tmp\03\SN01_T1" -Force | Out-Null
try {
    [IO.File]::WriteAllText("$tmp\Notlar\a.md", "---`nolay: toplanti`ntarih: 2026-11-05`nkisi: katan`n---`n# Haftalık`n`nKonuşuldu.")
    [IO.File]::WriteAllText("$tmp\Notlar\b.md", "---`nolay: karar`ntarih: 2026-10-22`n---`nGövde")
    [IO.File]::WriteAllText("$tmp\Notlar\c.md", "---`ntarih: 2026-10-01`n---`n# olay alanı yok")
    [IO.File]::WriteAllText("$tmp\Notlar\d.md", "---`nolay: not`ntarih: yarın`n---`n# tarih bozuk")
    [IO.File]::WriteAllText("$tmp\_Arsiv\e.md", "---`nolay: not`ntarih: 2026-01-01`n---`n# arşiv")
    [IO.File]::WriteAllText("$tmp\03\SN01_T1\test.md", "---`nolay: test`ntarih: 2026-11-01`n---`n# test")
    $all = $E::ReadAll($tmp)
    Check 'yalnız olay + tarih olanlar; _ ve test atlanır' ($all.Count -eq 2) (($all | ForEach-Object Title) -join ',')
    Check 'yeniden eskiye, başlık, kişi, özet' ($all[0].Title -eq 'Haftalık' -and $all[0].Person -eq 'katan' -and $all[0].Excerpt -eq 'Konuşuldu.' -and $all[1].Kind -eq 'karar')
    Check 'başlık yoksa dosya adı' ($all[1].Title -eq 'b')
} finally { Remove-Item -Recurse -Force $tmp -ErrorAction SilentlyContinue }
"--- 2. Arayüz (deneme verisi)"
$settingsFile = Join-Path $env:LOCALAPPDATA 'Pusula\ayarlar.json'
$savedSettings = if (Test-Path $settingsFile) { [IO.File]::ReadAllText($settingsFile) } else { $null }
try {
    [System.Threading.SynchronizationContext]::SetSynchronizationContext((New-Object System.Windows.Threading.DispatcherSynchronizationContext))
    $app = New-Object Pusula.Wpf.App; $app.InitializeComponent(); $app.ShutdownMode = 'OnExplicitShutdown'
    $win = New-Object Pusula.Wpf.MainWindow
    $win.WindowStartupLocation = 'Manual'; $win.Left = -20000; $win.Top = -20000; $win.ShowActivated = $false; $win.Width = 1360; $win.Height = 1000
    $win.Show(); Pump
    $vm = $win.DataContext; $vm.RootPath = $Data; Pump
    $vm.SelectedNode = (FindNode $vm.Nodes 'AB-7000-0002'); Wait
    $tl = @($vm.Detail.Timeline)
    $titles = ($tl | ForEach-Object Title)
    Check 'Günlük: toplantı, karar, numune olayları' (($titles -contains 'MILKON haftalık toplantı') -and ($titles -contains 'Konnektör SMA yerine N tipi') -and ($titles -contains 'SNP1 prototip numunesi geldi')) ($titles -join ' | ')
    Check 'tarih sırası' (($tl | ForEach-Object SortDate) -join ',' -eq (($tl | Sort-Object SortDate -Descending | ForEach-Object SortDate) -join ','))
    $m = $tl | Where-Object Title -eq 'MILKON haftalık toplantı'
    Check 'toplantı: etiket, not yolu' ($m.Result.Name -eq 'Toplantı' -and $m.NotePath -like '*2026-11-05_MILKON_haftalik.md')
    Check 'koşular hâlâ 4' ($vm.Detail.DevelopmentRuns.Count -eq 4)
    Check 'başlık Günlük' ([Pusula.Wpf.Properties.Resources]::General_Timeline -eq 'Günlük')
    $sv = $win.FindName('StockTabs')
    Snap $win "$Out\gunluk-stock.png"
    $vm.SelectedNode = (FindNode $vm.Nodes 'JIG-1000-0009'); Wait
    $rev = $vm.Inventory.History | Where-Object { $_.Status.Name -eq 'Revizyon' }
    Check 'jig Geçmiş: Revizyon notu, açılabilir' ($rev -ne $null -and $rev.HasNote -and $rev.Detail -match 'Rev AC çizildi')
    $vm.Inventory.ReloadContentsAsync() | Out-Null; Wait
    Check 'yeniden yüklemede çiftlenmez' (@($vm.Inventory.History | Where-Object { $_.Status.Name -eq 'Revizyon' }).Count -eq 1)
    Snap $win "$Out\gunluk-jig.png"
    $win.Close()
} finally { if ($savedSettings -ne $null) { [IO.File]::WriteAllText($settingsFile, $savedSettings) } }
""
"SONUC: $ok basarili, $fail hata"
