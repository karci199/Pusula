param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
. (Join-Path $PSScriptRoot '..\_Ortak.ps1')
# Toplantı kaydı: katılımcılar (@), ortam (ikonlu), kararlar (serbest metin, "#### Kararlar" altında), açıklama "Notlar";
# kartta ortam çipi, katılımcı hapları, kararlar kutusu; düzenlemede kararlar korunur.
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
    $vm.SelectNodeByPath($stock); Wait; Wait

    "--- Alanlar"
    [void]$vm.OpenJournalHere(); $vm.JournalKind = 'Hazırlık'
    Check 'Hazırlık: toplantı alanı yok, Açıklama' (-not $vm.IsJournalMeeting -and $vm.JournalBodyLabel -eq 'Açıklama')
    $vm.JournalKind = 'Toplantı'
    Check 'yeni kayıtta MD düğmesi yok' (-not $vm.IsJournalEditing)
    Check 'Toplantı: alanlar açılır, Notlar' ($vm.IsJournalMeeting -and $vm.JournalBodyLabel -eq 'Notlar')
    $me = $vm.JournalParticipants
    Check 'katılımcı: kullanıcı ekli' ($me -like '@*' -and -not $me.Trim().Contains(' ')) $me
    $choices = @($vm.JournalSettingChoices)
    Check 'ortamlar ikonlu: Yüz yüze, Teams, Skype, Zoom, Telefon' (($choices | ForEach-Object Key) -join ',' -eq 'Yüz yüze,Teams,Skype,Zoom,Telefon,Outlook' -and ($choices | ForEach-Object Badge) -join ',' -eq 'face,teams,skype,zoom,phone,mail')

    "--- Kayıt"
    $vm.JournalParticipants = $me + ' @Ali_Demirci'
    ($choices | Where-Object { $_.Key -eq 'Teams' }).IsChecked = $true
    Check 'Teams seçili' ($vm.JournalSetting -eq 'Teams' -and @($vm.JournalSettingChoices | Where-Object { $_.IsChecked }).Count -eq 1)
    $vm.JournalHeading = 'Ölçüt toplantısı'; $vm.JournalBody = 'Tasarımcıyla ölçütler konuşuldu.'
    $vm.JournalDecisions = "Geri yansıma en fazla -7 dB.`r`n3 dB yanca hüzme en az 20 derece."
    Check 'eklenir' ($vm.ConfirmJournal()); Wait; Wait
    $txt = [IO.File]::ReadAllText($note)
    Check 'bilgi satırı: katılımcı ve ortam' ($txt.Contains('katılımcı: ' + $me + ' @Ali_Demirci · ortam: Teams'))
    Check 'Kararlar başlığı, Ayrıntı''dan önce' ($txt -match 'Tasarımcıyla ölçütler konuşuldu\.\r?\n\r?\n#### Kararlar\r?\nGeri yansıma en fazla -7 dB\.\r?\n3 dB yanca hüzme en az 20 derece\.\r?\n\r?\n#### Ayrıntı')
    $read = [Pusula.Core.Notes.Journal]::Read($note) | Where-Object { $_.Title -eq 'Ölçüt toplantısı' } | Select-Object -First 1
    Check 'okunur: katılımcı, ortam, kararlar' ($read.Participants.Count -eq 2 -and $read.Setting -eq 'Teams' -and $read.Decisions -like 'Geri yansıma*20 derece.' -and $read.Excerpt -eq 'Tasarımcıyla ölçütler konuşuldu.') $read.Excerpt

    "--- Kart"
    $vm.IsRangeAll = $true; Wait
    $card = @($vm.StockDays | ForEach-Object { $_.Cards }) | Where-Object { $_.Title -eq 'Ölçüt toplantısı' } | Select-Object -First 1
    $tags = @($card.Tags | ForEach-Object Text)
    $ali = @($card.Tags | Where-Object { $_.Text -eq '@Ali_Demirci' })[0]
    Check 'kişi: ad soyad ve baş harfler' ($ali.Name -eq 'Ali Demirci' -and $ali.Initials -eq 'AD' -and $ali.AvatarColor.StartsWith('#')) ($ali.Name + ' / ' + $ali.Initials)
    Check 'kart: ortam çipi, kararlar, katılımcılar, notlar özette' ($card.HasSetting -and $card.SettingIcon -eq 'teams' -and $card.HasDecisions -and ($tags -contains '@Ali_Demirci') -and $card.Description -eq 'Tasarımcıyla ölçütler konuşuldu.') ($tags -join ',')

    "--- Karar"
    [void]$vm.OpenJournalHere(); $vm.JournalKind = 'Karar'
    Check 'Karar: karar alanları, toplantı alanı yok' ($vm.IsJournalDecision -and -not $vm.IsJournalMeeting -and -not $vm.IsJournalMeasurement -and $vm.JournalDecidedBy -eq '')
    $vm.JournalDecidedBy = '@Ayşe_Kaya'; $vm.JournalHeading = 'Rapor biçimi değişti'
    Check 'maddesiz karar eklenir' ($vm.JournalPrimaryText -ne '' -and $vm.ConfirmJournal()); Wait
    $txt = [IO.File]::ReadAllText($note)
    Check 'karar veren yazılır, madde yok' ($txt -match 'Rapor biçimi değişti\r?\n[^\n]*karar veren: @Ayşe_Kaya' -and -not ($txt -match 'Rapor biçimi değişti\r?\n[^\n]*madde:'))
    $it = New-Object Pusula.Core.Testing.VerificationItem; $it.TgNo = 'DGÖ-1'; $it.Text = 'VSWR'; $it.Group = 'İşlevsel'; $it.Status = 'Geçti'
    [void][Pusula.Core.Testing.VerificationList]::Add($stock, $it, ''); $vm.RefreshCommand.Execute($null); Wait; Wait
    [void]$vm.OpenJournalHere(); $vm.JournalKind = 'Karar'; $vm.JournalDecidedBy = '@Ayşe_Kaya'; $vm.JournalHeading = 'VSWR sınırı gevşetildi'
    $vm.JournalItemOption = ($vm.JournalItemOptions | Where-Object { $_.Key -eq 'DGÖ-1' })
    Check 'maddeli karar eklenir' ($vm.JournalPrimaryText -ne '' -and $vm.ConfirmJournal()); Wait; Wait
    $txt = [IO.File]::ReadAllText($note)
    $kl = [regex]::Match($txt, 'VSWR sınırı gevşetildi\r?\n([^\r\n]*)').Groups[1].Value
    Check 'madde ve karar veren yazılır, sonuç yok' ($kl.Contains('karar veren: @Ayşe_Kaya') -and $kl.Contains('madde: DGÖ-1') -and -not $kl.Contains('sonuç:')) $kl
    Check 'madde durumu değişmez' ((([Pusula.Core.Testing.VerificationList]::Read($stock)) | Where-Object { $_.TgNo -eq 'DGÖ-1' }).Status -eq 'Geçti')
    $vm.IsRangeAll = $true; Wait
    $kc = @($vm.StockDays | ForEach-Object { $_.Cards }) | Where-Object { $_.Title -eq 'VSWR sınırı gevşetildi' } | Select-Object -First 1
    Check 'kart: karar veren ve madde bağı' (($kc.Tags | ForEach-Object Text) -contains '@Ayşe_Kaya' -and $kc.ResultText -eq '↳ DGÖ-1') $kc.ResultText
    $vr = @($vm.VerificationGroups | ForEach-Object { $_.Rows }) | Where-Object { $_.TgNo -eq 'DGÖ-1' } | Select-Object -First 1
    Check 'doğrulama satırında Karar olarak' ($vr.Attempts.Count -eq 1 -and $vr.Attempts[0].Result -eq 'Karar')
    "--- Düzenleme"
    # MD'de ayrıntı yazılmış gibi: kartta ve düzenleme penceresinde görünmemeli
    $txt = [IO.File]::ReadAllText($note)
    $txt = [regex]::Replace($txt, '(Ölçüt toplantısı[\s\S]*?#### Ayrıntı)(\r?\n)', '$1$2Uzun tutanak: ölçüm takvimi, tezgâh, sorumlular …$2', 1)
    [IO.File]::WriteAllText($note, $txt, (New-Object Text.UTF8Encoding $true))
    $read = [Pusula.Core.Notes.Journal]::Read($note) | Where-Object { $_.Title -eq 'Ölçüt toplantısı' } | Select-Object -First 1
    $vm.OpenJournalEdit($read)
    Check 'ayrıntı pencerede görünmez, MD düğmesi var' ($vm.IsJournalEditing -and -not $vm.JournalBody.Contains('Uzun tutanak') -and -not $vm.JournalDecisions.Contains('Uzun tutanak'))
    $win.Height = 1100; for ($k = 0; $k -lt 40; $k++) { Pump; Start-Sleep -Milliseconds 50 }
    # Açık diyalog ayrı katmanda çizilir: görsel ağaçta bulunup kendisi PNG'ye çizilir.
    function Find-Open($node) {
        if ($node -eq $null) { return $null }
        if ($node.GetType().Name -eq 'ContentDialog' -and $node.IsOpen) { return $node }
        for ($i = 0; $i -lt [System.Windows.Media.VisualTreeHelper]::GetChildrenCount($node); $i++) { $r = Find-Open ([System.Windows.Media.VisualTreeHelper]::GetChild($node, $i)); if ($r) { return $r } }
        return $null
    }
    $dlg = Find-Open $win
    if ($dlg) {
        function Find-Popup($node) { if ($node -is [System.Windows.Controls.Primitives.Popup]) { return $node }; for ($i = 0; $i -lt [System.Windows.Media.VisualTreeHelper]::GetChildrenCount($node); $i++) { $r = Find-Popup ([System.Windows.Media.VisualTreeHelper]::GetChild($node, $i)); if ($r) { return $r } }; return $null }
        $pop = Find-Popup $dlg; if ($pop -and $pop.Child) { $dlg = $pop.Child; $sz = New-Object System.Windows.Size($win.ActualWidth, $win.ActualHeight); $dlg.Measure($sz); $dlg.Arrange((New-Object System.Windows.Rect($sz))); $dlg.UpdateLayout(); for ($k = 0; $k -lt 20; $k++) { Pump; Start-Sleep -Milliseconds 30 } }
        "diyalog öğesi: " + $dlg.GetType().Name + ' ' + [int]$dlg.ActualWidth + 'x' + [int]$dlg.ActualHeight + ' popup=' + ($pop -ne $null)
        $bmp = New-Object System.Windows.Media.Imaging.RenderTargetBitmap([int]$dlg.ActualWidth, [int]$dlg.ActualHeight, 96, 96, [System.Windows.Media.PixelFormats]::Pbgra32)
        $bmp.Render($dlg); $enc = New-Object System.Windows.Media.Imaging.PngBitmapEncoder; $enc.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($bmp))
        $fs = [IO.File]::Create("$Out\meeting-edit.png"); $enc.Save($fs); $fs.Close()
    }
    Check 'düzenlemede alanlar dolu' ($vm.IsJournalMeeting -and $vm.JournalSetting -eq 'Teams' -and $vm.JournalDecisions -like 'Geri yansıma*' -and $vm.JournalBody -eq 'Tasarımcıyla ölçütler konuşuldu.' -and $vm.JournalParticipants -like '*@Ali_Demirci*')
    $vm.JournalDecisions = 'Ölçütler kabul edildi.'
    Check 'kaydedilir' ($vm.ConfirmJournal()); Wait
    $txt = [IO.File]::ReadAllText($note)
    Check 'kararlar güncellendi, başlık tek, ayrıntı korunur' ($txt.Contains('Uzun tutanak') -and $txt.Contains("#### Kararlar") -and ([regex]::Matches($txt, '#### Kararlar')).Count -eq 1 -and $txt -match '#### Kararlar\r?\nÖlçütler kabul edildi\.\r?\n' -and -not $txt.Contains('Geri yansıma en fazla -7 dB.'))
    $vm.IsTimelineDone = $true; $win.Width = 1500; Wait
    $c2 = @($vm.StockDays | ForEach-Object { $_.Cards }) | Where-Object { $_.Title -eq 'Ölçüt toplantısı' } | Select-Object -First 1; if ($c2) { $c2.IsExpanded = $true }
    $kn = [Pusula.Core.Notes.Journal]::Read($note) | Where-Object { $_.Title -eq 'VSWR sınırı gevşetildi' } | Select-Object -First 1
    $vm.OpenJournalEdit($kn); for ($k = 0; $k -lt 20; $k++) { Pump; Start-Sleep -Milliseconds 30 }
    $dlg = Find-Open $win
    if ($dlg) {
        $pop = Find-Popup $dlg; if ($pop -and $pop.Child) { $dlg = $pop.Child; $sz = New-Object System.Windows.Size($win.ActualWidth, $win.ActualHeight); $dlg.Measure($sz); $dlg.Arrange((New-Object System.Windows.Rect($sz))); $dlg.UpdateLayout(); for ($k = 0; $k -lt 20; $k++) { Pump; Start-Sleep -Milliseconds 30 } }
        $bmp = New-Object System.Windows.Media.Imaging.RenderTargetBitmap([int]$dlg.ActualWidth, [int]$dlg.ActualHeight, 96, 96, [System.Windows.Media.PixelFormats]::Pbgra32)
        $bmp.Render($dlg); $enc = New-Object System.Windows.Media.Imaging.PngBitmapEncoder; $enc.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($bmp))
        $fs = [IO.File]::Create("$Out\decision-edit.png"); $enc.Save($fs); $fs.Close()
    }
    $vm.IsJournalOpen = $false; Pump
    Snap $win "$Out\meeting.png"
} finally {
    if ($savedSettings -ne $null) { [IO.File]::WriteAllText($settingsFile, $savedSettings) }
}
"SONUÇ OK=$ok HATA=$fail"
