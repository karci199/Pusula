param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
. (Join-Path $PSScriptRoot '..\_Ortak.ps1')
# Revizyon kaydı: stok no (varsayılan bu kayıt), yeni rev (zorunlu), değişiklik no (isteğe bağlı); önceki rev aynı stokun
# bir önceki revizyon kaydından bulunur; notta "stok: … · rev: A → B · değişiklik no: …"; kartta "rev A → B" çipi.
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
    function EntryOf([string]$title) { [Pusula.Core.Notes.Journal]::Read($note) | Where-Object { $_.Title -eq $title } | Select-Object -First 1 }

    "--- İlk revizyon"
    [void]$vm.OpenJournalHere(); $vm.JournalKind = 'Revizyon'
    Check 'Revizyon: alanlar açılır, stok bu kayıt' ($vm.IsJournalRevision -and $vm.JournalRevisionStock -eq 'AB-7000-0002' -and $vm.JournalRevisionHint -like 'İlk revizyon*') $vm.JournalRevisionStock
    $vm.JournalHeading = 'Prototip revizyonu'
    Check 'yeni rev yazılmadan Ekle yok' ($vm.JournalPrimaryText -eq '')
    $vm.JournalRevisionNew = 'A'; $vm.JournalRevisionChangeNo = 'ECO-0001'
    Check 'eklenir' ($vm.JournalPrimaryText -ne '' -and $vm.ConfirmJournal()); Wait
    $txt = [IO.File]::ReadAllText($note)
    Check 'bilgi satırı: stok, rev, değişiklik no' ($txt -match 'Prototip revizyonu\r?\n[^\n]*stok: AB-7000-0002 · rev: A · değişiklik no: ECO-0001')

    "--- İkinci revizyon"
    [void]$vm.OpenJournalHere(); $vm.JournalKind = 'Revizyon'
    Check 'önceki rev bulunur: A' ($vm.JournalRevisionHint -eq 'Önceki: A') $vm.JournalRevisionHint
    $vm.JournalRevisionNew = 'B'; $vm.JournalHeading = 'Geçiş parçası rev B'
    Check 'eklenir' ($vm.ConfirmJournal()); Wait
    $b = EntryOf 'Geçiş parçası rev B'
    Check 'okunur: A → B, değişiklik no yok' ($b.RevisionOld -eq 'A' -and $b.RevisionNew -eq 'B' -and $b.RevisionStock -eq 'AB-7000-0002' -and $b.RevisionChangeNo -eq '' -and [IO.File]::ReadAllText($note).Contains('rev: A → B'))

    "--- Başka stok"
    [void]$vm.OpenJournalHere(); $vm.JournalKind = 'Revizyon'; $vm.JournalRevisionStock = 'KN-0412-0007'
    Check 'başka stokta önceki yok' ($vm.JournalRevisionHint -like 'İlk revizyon*')
    $vm.JournalRevisionStock = 'AB-7000-0002'
    Check 'aynı stokta önceki: B' ($vm.JournalRevisionHint -eq 'Önceki: B') $vm.JournalRevisionHint
    $vm.IsJournalOpen = $false; Pump

    "--- Kart ve düzenleme"
    $vm.IsRangeAll = $true; Wait
    $card = @($vm.StockDays | ForEach-Object { $_.Cards }) | Where-Object { $_.Title -eq 'Geçiş parçası rev B' } | Select-Object -First 1
    Check 'kart: rev A → B, stok' ($card.HasRevision -and $card.RevisionText -eq 'rev A → B' -and $card.RevisionMeta -eq 'AB-7000-0002') $card.RevisionText
    $vm.OpenJournalEdit($b)
    Check 'düzenlemede alanlar ve önceki korunur' ($vm.JournalRevisionNew -eq 'B' -and $vm.JournalRevisionHint -eq 'Önceki: A')
    $vm.JournalRevisionChangeNo = 'ECO-0002'
    Check 'kaydedilir' ($vm.ConfirmJournal()); Wait
    Check 'değişiklik no eklendi, rev aynı' ([IO.File]::ReadAllText($note) -match 'Geçiş parçası rev B\r?\n[^\n]*rev: A → B · değişiklik no: ECO-0002')

    # Pencerenin görüntüsü
    function Find-Open($node) { if ($node -eq $null) { return $null }; if ($node.GetType().Name -eq 'ContentDialog' -and $node.IsOpen) { return $node }; for ($i = 0; $i -lt [System.Windows.Media.VisualTreeHelper]::GetChildrenCount($node); $i++) { $r = Find-Open ([System.Windows.Media.VisualTreeHelper]::GetChild($node, $i)); if ($r) { return $r } }; return $null }
    function Find-Popup($node) { if ($node -is [System.Windows.Controls.Primitives.Popup]) { return $node }; for ($i = 0; $i -lt [System.Windows.Media.VisualTreeHelper]::GetChildrenCount($node); $i++) { $r = Find-Popup ([System.Windows.Media.VisualTreeHelper]::GetChild($node, $i)); if ($r) { return $r } }; return $null }
    [void]$vm.OpenJournalHere(); $vm.JournalKind = 'Revizyon'; $vm.JournalRevisionNew = 'C'; $vm.JournalRevisionChangeNo = 'ECO-0003'; $vm.JournalHeading = 'Konnektör değişti'
    for ($k = 0; $k -lt 20; $k++) { Pump; Start-Sleep -Milliseconds 30 }
    $dlg = Find-Open $win
    if ($dlg) {
        $pop = Find-Popup $dlg; if ($pop -and $pop.Child) { $dlg = $pop.Child; $sz = New-Object System.Windows.Size($win.ActualWidth, $win.ActualHeight); $dlg.Measure($sz); $dlg.Arrange((New-Object System.Windows.Rect($sz))); $dlg.UpdateLayout(); for ($k = 0; $k -lt 20; $k++) { Pump; Start-Sleep -Milliseconds 30 } }
        $bmp = New-Object System.Windows.Media.Imaging.RenderTargetBitmap([int]$dlg.ActualWidth, [int]$dlg.ActualHeight, 96, 96, [System.Windows.Media.PixelFormats]::Pbgra32)
        $bmp.Render($dlg); $enc = New-Object System.Windows.Media.Imaging.PngBitmapEncoder; $enc.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($bmp))
        $fs = [IO.File]::Create("$Out\revision-dialog.png"); $enc.Save($fs); $fs.Close()
    }
    $vm.IsJournalOpen = $false; Pump
    $vm.IsTimelineDone = $true; Wait
    Snap $win "$Out\revision.png"
} finally {
    if ($savedSettings -ne $null) { [IO.File]::WriteAllText($settingsFile, $savedSettings) }
}
"SONUÇ OK=$ok HATA=$fail"
