param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
. (Join-Path $PSScriptRoot '..\_Ortak.ps1')
# Toplantıya mail (.msg): ortam Outlook iken mail seçilir; kayıtla stokun Diger klasörüne kopyalanır
# kendi adıyla (asıl dosya yerinde, üzerine yazılmaz); notta "mail: Diger/…"; pencere maille doldurulmaz; kartta ✉.
$settingsFile = Join-Path $env:LOCALAPPDATA 'Pusula\ayarlar.json'
$savedSettings = if (Test-Path $settingsFile) { [IO.File]::ReadAllText($settingsFile) } else { $null }
$src = Join-Path $env:TEMP ('pusula-mail-' + [Guid]::NewGuid().ToString('N').Substring(0, 8))
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

    # Örnek .msg (deneme aracıyla) ve .msg olmayan dosya
    Add-Type -Path (Join-Path $PSScriptRoot '..\..\tools\MailOku\MailOku.cs')
    New-Item -ItemType Directory -Force $src | Out-Null
    $m = New-Object MailOku+Mail; $m.Konu = 'RE: Ölçütler'; $m.Gonderen.Ad = 'Ayşe Kaya'; $m.Gonderen.Eposta = 'ayse@firma.com.tr'; $m.Tarih = Get-Date; $m.Govde = 'Merhaba'
    $msg = Join-Path $src 'Outlook mesajı.msg'; [MailOku+Msg]::OrnekYaz($msg, $m)
    $txt = Join-Path $src 'not.txt'; [IO.File]::WriteAllText($txt, 'x')
    $choose = { param($name) (@($vm.JournalSettingChoices) | Where-Object { $_.Key -eq $name }).IsChecked = $true }

    "--- Alan"
    [void]$vm.OpenJournalHere(); $vm.JournalKind = 'Toplantı'
    Check 'Teams''te mail alanı yok' (-not $vm.IsJournalMailVisible)
    & $choose 'Teams'; Check 'Teams seçili: yok' (-not $vm.IsJournalMailVisible)
    & $choose 'Outlook'
    Check 'Outlook: mail alanı var, seçilmedi' ($vm.IsJournalMailVisible -and $vm.JournalMailText -eq 'Seçilmedi' -and -not $vm.HasJournalMail)
    Check '.msg dışı kabul edilmez' (-not $vm.SetJournalMail($txt) -and -not $vm.HasJournalMail)
    Check '.msg seçilir' ($vm.SetJournalMail($msg) -and $vm.JournalMailText -eq 'Outlook mesajı.msg')
    Check 'pencere maille doldurulmaz' ($vm.JournalHeading -eq '' -and $vm.JournalBody -eq '')

    "--- Kayıt"
    $vm.JournalHeading = 'Ölçüt yazışması'
    Check 'eklenir' ($vm.ConfirmJournal()); Wait
    $day = (Get-Date).ToString('yyyy-MM-dd')
    $copy = Join-Path $stock 'Diger\Outlook mesajı.msg'
    Check 'Diger klasörüne kendi adıyla kopyalandı' ((Test-Path $copy) -and (Test-Path $msg)) $copy
    Check 'notta mail yolu' ([IO.File]::ReadAllText($note).Contains('mail: Diger/Outlook mesajı.msg'))
    $read = [Pusula.Core.Notes.Journal]::Read($note) | Where-Object { $_.Title -eq 'Ölçüt yazışması' } | Select-Object -First 1
    Check 'okunur: Mail, ortam Outlook' ($read.Mail -eq 'Diger/Outlook mesajı.msg' -and $read.Setting -eq 'Outlook') $read.Mail

    "--- Aynı başlık, düzenleme, kart"
    [void]$vm.OpenJournalHere(); $vm.JournalKind = 'Toplantı'; & $choose 'Outlook'; [void]$vm.SetJournalMail($msg); $vm.JournalHeading = 'Ölçüt yazışması'; $vm.JournalTimeText = '23:58'
    Check 'ikinci kayıt' ($vm.ConfirmJournal()); Wait
    Check 'aynı ad: _2, üzerine yazılmaz' (Test-Path (Join-Path $stock 'Diger\Outlook mesajı_2.msg'))
    $vm.OpenJournalEdit($read)
    Check 'düzenlemede mail görünür' ($vm.IsJournalMailVisible -and $vm.HasJournalMail -and $vm.JournalMailText -eq 'Outlook mesajı.msg')
    $vm.JournalBody = 'Ölçütler mailde kesinleşti.'
    Check 'kaydedilir, mail korunur' ($vm.ConfirmJournal() -and ([regex]::Matches([IO.File]::ReadAllText($note), 'mail: Diger/Outlook mesajı\.msg')).Count -eq 1); Wait
    $vm.RefreshCommand.Execute($null); Wait; Wait; $vm.IsRangeAll = $true; $vm.IsTimelineDone = $true; Wait
    $card = @($vm.StockDays | ForEach-Object { $_.Cards }) | Where-Object { $_.Title -eq 'Ölçüt yazışması' -and $_.Description -like 'Ölçütler*' } | Select-Object -First 1
    Check 'kartta mail düğmesi, ortam Outlook' ($card.HasMail -and $card.Setting -eq 'Outlook')

    "--- Karar"
    [void]$vm.OpenJournalHere(); $vm.JournalKind = 'Karar'
    Check 'Kararda mail alanı var' ($vm.IsJournalMailVisible -and -not $vm.IsJournalMeetingMail)
    [void]$vm.SetJournalMail($msg); $vm.JournalHeading = 'Mailde alınan karar'
    Check 'kararla mail eklenir' ($vm.ConfirmJournal() -and (Test-Path (Join-Path $stock 'Diger\Outlook mesajı_3.msg'))); Wait
    Check 'kararın notunda mail' (([Pusula.Core.Notes.Journal]::Read($note) | Where-Object { $_.Title -eq 'Mailde alınan karar' } | Select-Object -First 1).Mail -eq 'Diger/Outlook mesajı_3.msg')
    [void]$vm.OpenJournalHere(); $vm.JournalKind = 'Hazırlık'
    Check 'Hazırlıkta mail alanı yok' (-not $vm.IsJournalMailVisible)
    $vm.IsJournalOpen = $false; Pump
    # Görüntüler: pencere (mail seçili) ve kart
    function Find-Open($node) { if ($node -eq $null) { return $null }; if ($node -is [DesignKit.Controls.ContentDialog] -and $node.IsOpen) { return $node }; for ($i = 0; $i -lt [System.Windows.Media.VisualTreeHelper]::GetChildrenCount($node); $i++) { $r = Find-Open ([System.Windows.Media.VisualTreeHelper]::GetChild($node, $i)); if ($r) { return $r } }; return $null }
    function Find-Popup($node) { if ($node -is [System.Windows.Controls.Primitives.Popup]) { return $node }; for ($i = 0; $i -lt [System.Windows.Media.VisualTreeHelper]::GetChildrenCount($node); $i++) { $r = Find-Popup ([System.Windows.Media.VisualTreeHelper]::GetChild($node, $i)); if ($r) { return $r } }; return $null }
    [void]$vm.OpenJournalHere(); $vm.JournalKind = 'Toplantı'; & $choose 'Outlook'; [void]$vm.SetJournalMail($msg); $vm.JournalHeading = 'Ölçüt yazışması'
    for ($k = 0; $k -lt 20; $k++) { Pump; Start-Sleep -Milliseconds 30 }
    $dlg = Find-Open $win
    if ($dlg) {
        $pop = Find-Popup $dlg; if ($pop -and $pop.Child) { $dlg = $pop.Child; $sz = New-Object System.Windows.Size($win.ActualWidth, $win.ActualHeight); $dlg.Measure($sz); $dlg.Arrange((New-Object System.Windows.Rect($sz))); $dlg.UpdateLayout(); for ($k = 0; $k -lt 20; $k++) { Pump; Start-Sleep -Milliseconds 30 } }
        $bmp = New-Object System.Windows.Media.Imaging.RenderTargetBitmap([int]$dlg.ActualWidth, [int]$dlg.ActualHeight, 96, 96, [System.Windows.Media.PixelFormats]::Pbgra32)
        $bmp.Render($dlg); $enc = New-Object System.Windows.Media.Imaging.PngBitmapEncoder; $enc.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($bmp))
        $fs = [IO.File]::Create("$Out\mail-dialog.png"); $enc.Save($fs); $fs.Close()
    }
    $vm.IsJournalOpen = $false; Pump
    [void]$vm.OpenJournalHere(); $vm.JournalKind = 'Karar'; $vm.JournalDecidedBy = '@Ayşe_Kaya'; [void]$vm.SetJournalMail($msg); $vm.JournalHeading = 'Mailde alınan karar'
    for ($k = 0; $k -lt 20; $k++) { Pump; Start-Sleep -Milliseconds 30 }
    $dlg = Find-Open $win
    if ($dlg) {
        $pop = Find-Popup $dlg; if ($pop -and $pop.Child) { $dlg = $pop.Child; $sz = New-Object System.Windows.Size($win.ActualWidth, $win.ActualHeight); $dlg.Measure($sz); $dlg.Arrange((New-Object System.Windows.Rect($sz))); $dlg.UpdateLayout(); for ($k = 0; $k -lt 20; $k++) { Pump; Start-Sleep -Milliseconds 30 } }
        $bmp = New-Object System.Windows.Media.Imaging.RenderTargetBitmap([int]$dlg.ActualWidth, [int]$dlg.ActualHeight, 96, 96, [System.Windows.Media.PixelFormats]::Pbgra32)
        $bmp.Render($dlg); $enc = New-Object System.Windows.Media.Imaging.PngBitmapEncoder; $enc.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($bmp))
        $fs = [IO.File]::Create("$Out\decision-mail.png"); $enc.Save($fs); $fs.Close()
    }
    $vm.IsJournalOpen = $false; Pump
    Snap $win "$Out\mail.png"
} finally {
    if ($savedSettings -ne $null) { [IO.File]::WriteAllText($settingsFile, $savedSettings) }
    Remove-Item -Recurse -Force $src -ErrorAction SilentlyContinue
}
"SONUÇ OK=$ok HATA=$fail"
