param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
. (Join-Path $PSScriptRoot '..\_Ortak.ps1')
# Sorun ve çözüm: sorun isteğe bağlı madde, birim ve durumla (Açık / Çözüldü) yazılır. Çözüm bir açık soruna bağlanabilir
# (sorun "Çözüldü <tarih>" olur) ya da tek başına (doğrudan çözüm) yazılır. Açık sorunlar Yapılacaklar'da listelenir.
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
    function EntryOf([string]$title, [string]$kind) { [Pusula.Core.Notes.Journal]::Read($note) | Where-Object { $_.Title -eq $title -and $_.KindLabel -eq $kind } | Select-Object -First 1 }
    function At([datetime]$when) { $vm.JournalDateText = $when.ToString('dd.MM.yyyy HH:mm'); $vm.JournalDate = $when }
    $d1 = (Get-Date).Date.AddDays(-7).AddHours(14).AddMinutes(30)
    $d2 = (Get-Date).Date.AddDays(-3).AddHours(9)

    "--- Sorun"
    [void]$vm.OpenJournalHere(); $vm.JournalKind = 'Sorun'
    Check 'Sorun: sorun alanları açılır' ($vm.IsJournalIssue -and -not $vm.IsJournalSolution -and -not $vm.IsJournalMeasurement)
    Check 'durum Açık başlar' ($vm.JournalIssueStatus -eq 'Açık' -and (@($vm.JournalIssueStatusChoices) | ForEach-Object Key) -join ',' -eq 'Açık,Çözüldü')
    $item = @($vm.JournalItemOptions) | Where-Object { $_.Key.Length -gt 0 -and $_.Key[0] -ne [char]1 } | Select-Object -First 1
    if ($item) { $vm.JournalItem = $item.Key }
    $vm.JournalIssueUnit = 'SN-002'; $vm.JournalHeading = 'Titreşimde jig gevşedi'; $vm.JournalBody = 'VSWR değişimi 0,16 (sınır 0,1).'; At $d1
    Check 'eklenir' ($vm.ConfirmJournal()); Wait; Wait
    $txt = [IO.File]::ReadAllText($note)
    Check 'bilgi satırı: birim, durum' ($txt -match 'Sorun · Titreşimde jig gevşedi\r?\n[^\n]*birim: SN-002 · durum: Açık')
    $i1 = EntryOf 'Titreşimde jig gevşedi' 'Sorun'
    Check 'okunur: birim, durum, madde, özet temiz' ($i1.IssueUnit -eq 'SN-002' -and $i1.RequestStatus -eq 'Açık' -and $i1.RequestType -eq '' -and (-not $item -or $i1.Item -eq $item.Key) -and $i1.Excerpt -eq 'VSWR değişimi 0,16 (sınır 0,1).') $i1.Excerpt
    [void]$vm.OpenJournalHere(); $vm.JournalKind = 'Sorun'; $vm.JournalHeading = '+70 °C''de kazanç düşüşü'; $vm.JournalBody = '1,3 dB düşüş.'; At $d2
    Check 'ikinci sorun (alanlar boş)' ($vm.ConfirmJournal()); Wait

    "--- Çözüm penceresi"
    [void]$vm.OpenJournalHere(); $vm.JournalKind = 'Çözüm'
    $opts = @($vm.JournalSolutionOptions)
    Check 'Çözüm: çözdüğü sorun alanı, — Yok seçili' ($vm.IsJournalSolution -and -not $vm.IsJournalIssue -and $vm.JournalSolutionOption.Key -eq '' -and $opts[0].Text -eq '— Yok (doğrudan çözüm)')
    Check 'açık sorunlar listede, eskiden yeniye, rozet Açık' ($opts.Count -ge 3 -and $opts[1].Text -like 'Titreşimde jig gevşedi · *SN-002' -and $opts[1].Badge -eq 'Açık' -and $opts[2].Text -like '+70*') ($opts | ForEach-Object Text) -join ' | '

    "--- Doğrudan çözüm"
    $vm.JournalHeading = 'Kablo yolu kısaltıldı'
    Check 'eklenir' ($vm.ConfirmJournal()); Wait
    $s0 = EntryOf 'Kablo yolu kısaltıldı' 'Çözüm'
    Check 'tek başına: çözdüğü yok, sorunlar açık' ($s0.Resolves -eq '' -and -not ([IO.File]::ReadAllText($note) -match 'Kablo yolu kısaltıldı\r?\n[^\n]*çözdüğü') -and (EntryOf 'Titreşimde jig gevşedi' 'Sorun').RequestStatus -eq 'Açık')

    "--- Kart ve açık sorunlar"
    $vm.RefreshCommand.Execute($null); Wait; Wait; $vm.IsRangeAll = $true; Wait
    $cards = @($vm.StockDays | ForEach-Object { $_.Cards })
    $c1 = $cards | Where-Object { $_.Title -eq 'Titreşimde jig gevşedi' } | Select-Object -First 1
    Check 'kart: açık · 7 gün, birim, Çözüm ekle' ($c1.IsIssue -and $c1.IsIssueOpen -and $c1.IssueStatusText -eq 'Açık · 7 gün' -and $c1.IssueUnit -eq 'SN-002') $c1.IssueStatusText
    Check 'açık sorunlar: 2, eskiden yeniye' ($vm.HasOpenIssues -and $vm.OpenIssues.Count -eq 2 -and $vm.OpenIssues[0].Title -eq 'Titreşimde jig gevşedi') ([string]$vm.OpenIssues.Count)

    "--- Bağlı çözüm (kartın düğmesinden)"
    Check 'Çözüm ekle: pencere Çözüm ve sorun seçili' ($vm.OpenSolutionFor($c1.Item.Journal) -and $vm.IsJournalSolution -and $vm.JournalSolutionOption.Text -like 'Titreşimde jig gevşedi*') $vm.JournalSolutionOption.Text
    $vm.JournalHeading = 'Sabitleme vidaları değişti'; $vm.JournalBody = 'Titreşim sonrası gevşeme giderildi.'
    Check 'eklenir' ($vm.ConfirmJournal()); Wait; Wait
    $txt = [IO.File]::ReadAllText($note)
    $key = $d1.ToString('dd.MM.yyyy HH:mm') + ' — Titreşimde jig gevşedi'
    Check 'bilgi satırı: çözdüğü' ($txt -match ('Sabitleme vidaları değişti\r?\n[^\n]*çözdüğü: ' + [regex]::Escape($key)))
    $i1 = EntryOf 'Titreşimde jig gevşedi' 'Sorun'
    $today = (Get-Date).ToString('dd.MM.yyyy')
    Check 'sorun Çözüldü <tarih>, gövde aynı' ($i1.RequestStatus -eq ('Çözüldü ' + $today) -and $i1.Excerpt -eq 'VSWR değişimi 0,16 (sınır 0,1).' -and $i1.IssueUnit -eq 'SN-002') $i1.RequestStatus
    $cards = @($vm.StockDays | ForEach-Object { $_.Cards })
    $c1 = $cards | Where-Object { $_.Title -eq 'Titreşimde jig gevşedi' } | Select-Object -First 1
    $cs = $cards | Where-Object { $_.Title -eq 'Sabitleme vidaları değişti' } | Select-Object -First 1
    Check 'kart: Çözüldü · gün, düğme yok' ($c1.IssueStatusKind -eq 2 -and -not $c1.IsIssueOpen -and $c1.IssueStatusText -eq ('Çözüldü · ' + (Get-Date).ToString('dd.MM'))) $c1.IssueStatusText
    Check 'çözüm kartı: ↳ sorun' ($cs.HasSolution -and $cs.SolutionText -eq '↳ Titreşimde jig gevşedi') $cs.SolutionText
    Check 'açık sorunlar: 1' ($vm.OpenIssues.Count -eq 1) ([string]$vm.OpenIssues.Count)
    [void]$vm.OpenJournalHere(); $vm.JournalKind = 'Çözüm'
    Check 'kapanan sorun listeden çıkar' (-not (@($vm.JournalSolutionOptions) | Where-Object { $_.Text -like 'Titreşimde*' }))
    $vm.IsJournalOpen = $false

    "--- Düzenleme"
    $vm.OpenJournalEdit((EntryOf 'Sabitleme vidaları değişti' 'Çözüm'))
    Check 'çözüm düzenlemede bağ korunur' ($vm.JournalResolves -eq $key -and $vm.JournalSolutionOption.Key -eq $key)
    Check 'kaydedilir, bağ kalır' ($vm.ConfirmJournal() -and (EntryOf 'Sabitleme vidaları değişti' 'Çözüm').Resolves -eq $key); Wait
    $vm.OpenJournalEdit((EntryOf 'Titreşimde jig gevşedi' 'Sorun'))
    Check 'sorun düzenlemede: birim, Çözüldü' ($vm.JournalIssueUnit -eq 'SN-002' -and $vm.JournalIssueStatus -eq 'Çözüldü')
    Check 'kaydedilir, tarih korunur' ($vm.ConfirmJournal() -and (EntryOf 'Titreşimde jig gevşedi' 'Sorun').RequestStatus -eq ('Çözüldü ' + $today)); Wait
    $vm.OpenJournalEdit((EntryOf '+70 °C''de kazanç düşüşü' 'Sorun')); $vm.JournalIssueStatus = 'Çözüldü'
    Check 'elle Çözüldü' ($vm.ConfirmJournal() -and (EntryOf '+70 °C''de kazanç düşüşü' 'Sorun').RequestStatus -eq ('Çözüldü ' + $today)); Wait
    Check 'açık sorun kalmadı' (-not $vm.HasOpenIssues)

    # Görüntüler: sorun penceresi, çözüm penceresi, çizelge
    function Find-Open($node) { if ($node -eq $null) { return $null }; if ($node.GetType().Name -eq 'ContentDialog' -and $node.IsOpen) { return $node }; for ($i = 0; $i -lt [System.Windows.Media.VisualTreeHelper]::GetChildrenCount($node); $i++) { $r = Find-Open ([System.Windows.Media.VisualTreeHelper]::GetChild($node, $i)); if ($r) { return $r } }; return $null }
    function Find-Popup($node) { if ($node -is [System.Windows.Controls.Primitives.Popup]) { return $node }; for ($i = 0; $i -lt [System.Windows.Media.VisualTreeHelper]::GetChildrenCount($node); $i++) { $r = Find-Popup ([System.Windows.Media.VisualTreeHelper]::GetChild($node, $i)); if ($r) { return $r } }; return $null }
    function Shot([string]$file) {
        for ($k = 0; $k -lt 20; $k++) { Pump; Start-Sleep -Milliseconds 30 }
        $dlg = Find-Open $win
        if ($dlg) {
            $pop = Find-Popup $dlg; if ($pop -and $pop.Child) { $dlg = $pop.Child; $sz = New-Object System.Windows.Size($win.ActualWidth, $win.ActualHeight); $dlg.Measure($sz); $dlg.Arrange((New-Object System.Windows.Rect($sz))); $dlg.UpdateLayout(); for ($k = 0; $k -lt 20; $k++) { Pump; Start-Sleep -Milliseconds 30 } }
            $bmp = New-Object System.Windows.Media.Imaging.RenderTargetBitmap([int]$dlg.ActualWidth, [int]$dlg.ActualHeight, 96, 96, [System.Windows.Media.PixelFormats]::Pbgra32)
            $bmp.Render($dlg); $enc = New-Object System.Windows.Media.Imaging.PngBitmapEncoder; $enc.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($bmp))
            $fs = [IO.File]::Create($file); $enc.Save($fs); $fs.Close()
        }
        $vm.IsJournalOpen = $false; Pump
    }
    [void]$vm.OpenJournalHere(); $vm.JournalKind = 'Sorun'; if ($item) { $vm.JournalItem = $item.Key }
    $vm.JournalIssueUnit = 'SN-003'; $vm.JournalHeading = 'Konnektör çatlağı'; $vm.JournalBody = 'Görsel kontrolde görüldü.'
    [void]$vm.ConfirmJournal(); Wait
    [void]$vm.OpenJournalHere(); $vm.JournalKind = 'Sorun'; if ($item) { $vm.JournalItem = $item.Key }
    $vm.JournalIssueUnit = 'SN-004'; $vm.JournalHeading = 'Kapak vidası eksik'
    Shot "$Out\issue-dialog.png"
    [void]$vm.OpenJournalHere(); $vm.JournalKind = 'Çözüm'; $vm.JournalSolutionOption = @($vm.JournalSolutionOptions)[1]; $vm.JournalHeading = 'Konnektör değişti'
    Shot "$Out\solution-dialog.png"
    $vm.IsTimelineDone = $true; Wait
    Snap $win "$Out\issue.png"
} finally {
    if ($savedSettings -ne $null) { [IO.File]::WriteAllText($settingsFile, $savedSettings) }
}
"SONUÇ OK=$ok HATA=$fail"
