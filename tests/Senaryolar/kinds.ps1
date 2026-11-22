param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
. (Join-Path $PSScriptRoot '..\_Ortak.ps1')
# İşlem türleri (21.11.2026 sadeleştirmesi): listede 11 tür; eski türler (Arıza, Onarım, Test başladı/bitti, Üretime teslim,
# Numune, Teslim alındı) adıyla okunur, yeni türün ikon ve rengiyle görünür. Her tür için örnek kart PNG'ye çizilir.
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
    $EK = [Pusula.Wpf.ViewModels.EventKinds]

    "--- Liste"
    $labels = @($vm.JournalKinds | ForEach-Object Label)
    $expected = 'Ölçüm,Toplantı,Karar,Talep,Teslim,Sevkiyat,Revizyon,Kalibrasyon,Tamir / bakım,Sorun,Çözüm,Hazırlık,Not'
    Check 'listede 13 tür (22.11: Kalibrasyon, Tamir / bakım), sırasıyla' (($labels[0..12] -join ',') -eq $expected) ($labels -join ',')
    foreach ($old in 'Test başladı', 'Test bitti', 'Arıza', 'Onarım', 'Üretime teslim', 'Numune', 'Teslim alındı') { if ($labels -contains $old) { Check ('eski tür listede yok: ' + $old) $false } }
    Check 'eski türler listede yok' (-not ($labels -contains 'Arıza') -and -not ($labels -contains 'Test başladı'))

    "--- Eşleme"
    Check 'Arıza → Sorun (ikon, renk); adı Arıza' ($EK::Find('Arıza').Label -eq 'Sorun' -and $EK::Label('Arıza') -eq 'Arıza' -and $EK::ToneFor('Arıza', '') -eq $EK::ToneFor('Sorun', ''))
    Check 'Onarım → Çözüm' ($EK::Find('Onarım').Label -eq 'Çözüm')
    Check 'Test başladı / bitti → Ölçüm' ($EK::Find('Test başladı').Label -eq 'Ölçüm' -and $EK::Find('Test bitti').Label -eq 'Ölçüm')
    Check 'Üretime teslim, Numune → Sevkiyat' ($EK::Find('Üretime teslim').Label -eq 'Sevkiyat' -and $EK::Find('Numune').Label -eq 'Sevkiyat')
    Check 'Teslim alındı → Teslim' ($EK::Find('Teslim alındı').Label -eq 'Teslim')

    "--- Her tür için örnek kayıt"
    function Add-Entry([string]$kind, [string]$title, [string]$body, [int]$minute, [scriptblock]$fill) {
        $e = New-Object Pusula.Core.Notes.JournalEntry
        $e.Date = (Get-Date).Date.AddHours(9).AddMinutes($minute); $e.Kind = $kind; $e.Title = $title; $e.Body = $body; $e.Person = 'katan'
        if ($fill) { & $fill $e }
        [void][Pusula.Core.Notes.Journal]::Add($note, $e)
    }
    Add-Entry 'Not' 'Tezgâh alanı düzenlendi' 'Kablolar etiketlendi.' 1 $null
    Add-Entry 'Hazırlık' 'SN-003 tezgâha kuruldu' 'K-04 düzeneği.' 2 $null
    Add-Entry 'Çözüm' 'Sabitleme vidaları değişti' 'Titreşim sonrası gevşeme giderildi.' 3 $null
    Add-Entry 'Sorun' 'Titreşimde jig gevşedi' 'VSWR değişimi 0,16.' 4 { param($x) $x.Importance = 'Kritik' }
    Add-Entry 'Revizyon' 'Geçiş parçası rev B' 'Tasarımcı önerisi.' 5 { param($x) $x.RevisionStock = 'AB-7000-0002'; $x.RevisionOld = 'A'; $x.RevisionNew = 'B'; $x.RevisionChangeNo = 'ECO-0012' }
    Add-Entry 'Sevkiyat' 'SN-001 müşteriye gönderildi' 'Kabul testleri için.' 6 $null
    Add-Entry 'Teslim' 'JIG-0012 teslim alındı' 'Mekanik atölyeden.' 7 $null
    Add-Entry 'Talep' 'Konnektör ve zayıflatıcı' '' 8 { param($x) $x.RequestType = 'Malzeme'; $x.RequestNo = 'AMB-2026-1182'; $x.RequestFrom.Add('Ambar'); $x.RequestDue = (Get-Date).Date.AddDays(4); $x.RequestStatus = 'Açık'
        $i = New-Object Pusula.Core.Notes.RequestItem; $i.StockNo = 'KN-0412-0007'; $i.Name = 'SMA erkek konnektör'; $i.Quantity = '10'; $x.RequestItems.Add($i) }
    Add-Entry 'Karar' 'VSWR sınırı 1,6' 'Bant kenarında kabul.' 9 { param($x) $x.DecidedBy.Add('Ayşe_Kaya'); $x.Item = 'DGÖ-1' }
    Add-Entry 'Toplantı' 'Ölçüt toplantısı' 'Tasarımcıyla ölçütler konuşuldu.' 10 { param($x) $x.Participants.Add('katan'); $x.Participants.Add('Ali_Demirci'); $x.Setting = 'Teams'; $x.Decisions = 'Geri yansıma en fazla -7 dB.' }
    Add-Entry 'Ölçüm' 'İlk VSWR ölçümü' '8,2 GHz''de 1,72.' 11 { param($x) $x.Item = 'DGÖ-1'; $x.Result = 'Kaldı'; $x.Tags.Add('SN-003') }
    Add-Entry 'Arıza' 'Eski kayıt: konnektör kırıldı' 'Sadeleştirmeden önce yazılmış.' 0 $null

    $vm.RefreshCommand.Execute($null); Wait; Wait; $vm.IsRangeAll = $true; $vm.IsTimelineDone = $true; Wait
    $cards = @($vm.StockDays | ForEach-Object { $_.Cards }) | Where-Object { $_.Item -ne $null -and $_.Item.Journal -ne $null -and $_.When.Date -eq (Get-Date).Date }
    Check 'on iki örnek kart' ($cards.Count -eq 12) ([string]$cards.Count)
    $old = $cards | Where-Object { $_.Title -like 'Eski kayıt*' } | Select-Object -First 1
    $issue = $cards | Where-Object { $_.Title -eq 'Titreşimde jig gevşedi' } | Select-Object -First 1
    Check 'eski Arıza kartı: adı Arıza, Sorun rengi ve ikonu' ($old.KindLabel -eq 'Arıza' -and $old.Tone -eq $issue.Tone -and $old.Glyph -eq $issue.Glyph) $old.KindLabel

    # Her tür için kart: kartlar çizelge şablonuyla ayrı bir panele çizilir
    $panel = New-Object System.Windows.Controls.ItemsControl
    $panel.ItemTemplate = $win.FindResource('TimelineCardTemplate'); $panel.ItemsSource = $cards
    $host1 = New-Object System.Windows.Controls.Border; $host1.Padding = '16'; $host1.Width = 980; $host1.Child = $panel
    $host1.Background = $win.FindResource('LayerBrush')
    $win.Height = 1800; Wait
    $win.Content.Children.Add($host1) | Out-Null; [System.Windows.Controls.Grid]::SetRowSpan($host1, 3); $host1.HorizontalAlignment = 'Left'; $host1.VerticalAlignment = 'Top'
    Wait
    $host1.Measure((New-Object System.Windows.Size(980, [double]::PositiveInfinity))); $host1.Arrange((New-Object System.Windows.Rect(0, 0, 980, $host1.DesiredSize.Height))); $host1.UpdateLayout(); Wait
    $bmp = New-Object System.Windows.Media.Imaging.RenderTargetBitmap([int]$host1.ActualWidth, [int]$host1.ActualHeight, 96, 96, [System.Windows.Media.PixelFormats]::Pbgra32)
    $bmp.Render($host1); $enc = New-Object System.Windows.Media.Imaging.PngBitmapEncoder; $enc.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($bmp))
    $fs = [IO.File]::Create("$Out\kinds-cards.png"); $enc.Save($fs); $fs.Close()
    Check 'kart görüntüsü çizildi' ((Get-Item "$Out\kinds-cards.png").Length -gt 10000)
} finally {
    if ($savedSettings -ne $null) { [IO.File]::WriteAllText($settingsFile, $savedSettings) }
}
"SONUÇ OK=$ok HATA=$fail"
