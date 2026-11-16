param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
. (Join-Path $PSScriptRoot '..\_Ortak.ps1')
# Doğrulamalar: 03_Testler\<stok>_Test_Gereksinimleri.md tablosu; "+ Madde" satır ekler, yeni altyapı ortak listeye (_TAKIP\Altyapilar.md).
[System.Threading.SynchronizationContext]::SetSynchronizationContext((New-Object System.Windows.Threading.DispatcherSynchronizationContext))
$app = New-Object Pusula.Wpf.App; $app.InitializeComponent(); $app.ShutdownMode = 'OnExplicitShutdown'
$win = New-Object Pusula.Wpf.MainWindow
$win.WindowStartupLocation = 'Manual'; $win.Left = -20000; $win.Top = -20000; $win.ShowActivated = $false; $win.Width = 1360; $win.Height = 900
$win.Show(); Pump
$vm = $win.DataContext; $vm.RootPath = $Data; Wait

$stock = Join-Path $Data '7000_MILKON\AB-7000-0002'
$file = Join-Path $stock '03_Testler\AB-7000-0002_Test_Gereksinimleri.md'
$facilities = Join-Path $Data '_TAKIP\Altyapilar.md'
$vm.SelectNodeByPath($stock); Wait; Wait; Wait
Check 'başta madde yok' ($vm.VerificationCount -eq 0 -and $vm.IsVerificationEmpty)
Check 'süzgeç: Hepsi + 4 grup' ($vm.VerificationFilters.Count -eq 5) ([string]$vm.VerificationFilters.Count)

$vm.AddVerificationCommand.Execute($null); Wait
Check 'pencere açılır' ($vm.IsVerificationDialogOpen)
Check 'pencere dosyayı söyler' ($vm.VerificationFileText -like '*03_Testler*_Test_Gereksinimleri.md') $vm.VerificationFileText
Check 'boş metinle eklenmez' (-not $vm.ConfirmNewVerification() -and $vm.VerificationError.Length -gt 0)
Check 'dosya oluşmadı' (-not (Test-Path $file))

$vm.NewVerText = 'Kazanç ≥ 5 dBi @ 2,4 GHz'; $vm.NewVerSubGroup = 'Anten örüntü'; $vm.NewVerFacility = 'SATIMO'; $vm.NewVerTgNo = 'TG-012'
Check 'eklenir, pencere kapanabilir' ($vm.ConfirmNewVerification())
$vm.IsVerificationDialogOpen = $false; Wait
Check 'dosya oluştu' (Test-Path $file)
$text = [IO.File]::ReadAllText($file)
Check 'dosyada 9 sütunlu başlık' ($text -like '*| # | Doğrulama maddesi | Grup | Alt grup | Altyapı | TG no | TP no | TR no | Durum |*') $text
Check 'dosyada satır' ($text -like '*| 1 | Kazanç ≥ 5 dBi @ 2,4 GHz | İşlevsel | Anten örüntü | SATIMO | TG-012 |  |  | Bekliyor |*') $text
Check 'dosyada Plan ve Sonuç kısımları' ($text -like '*## Plan*' -and $text -like '*## Sonuç*')
Check 'altyapı ortak listede' ((Test-Path $facilities) -and ([IO.File]::ReadAllText($facilities) -like '*- SATIMO*'))
Check 'listede 1 madde' ($vm.VerificationCount -eq 1 -and $vm.VerificationGroups.Count -eq 1)

"--- yenisini aç: grup ÇK/EMC, metinde | var"
$vm.AddVerificationCommand.Execute($null); Wait
Check 'altyapı önerilerinde SATIMO' ($vm.FacilitySuggestions -contains 'SATIMO')
Check 'alt grup önerisi' ($vm.SubGroupSuggestions -contains 'Anten örüntü')
$vm.NewVerFacility = 'satimo'
Check 'var olan altyapı listeden seçilir' (-not $vm.FacilityPick.IsNew -and $vm.FacilityPick.Selected -eq 'SATIMO')
$vm.KeepVerificationOpen = $true
($vm.VerificationGroupChoices | Where-Object { $_.Key -eq 'ÇK/EMC' }).IsChecked = $true
$vm.NewVerText = 'RE102 | CE102 sınır içinde'; $vm.NewVerSubGroup = ''; $vm.NewVerFacility = 'EMC lab'; $vm.NewVerStatus = 'Geçti'
Check 'yenisini aç: pencere açık kalır' (-not $vm.ConfirmNewVerification())
Check 'metin temizlendi, grup kaldı' ($vm.NewVerText -eq '' -and ($vm.VerificationGroupChoices | Where-Object { $_.IsChecked }).Key -eq 'ÇK/EMC')
$vm.IsVerificationDialogOpen = $false; Wait
Check '2 madde, 2 grup' ($vm.VerificationCount -eq 2 -and $vm.VerificationGroups.Count -eq 2) ([string]$vm.VerificationCount)
$emc = $vm.VerificationGroups | Where-Object { $_.Title -eq 'ÇK/EMC' }
Check '| kaçışla geri okunur' ($emc.Rows[0].Text -eq 'RE102 | CE102 sınır içinde') $emc.Rows[0].Text
Check 'geçti sayılır' ($emc.Rows[0].StatusKind -eq 1 -and $vm.StockVerificationText -like '*1/2*') $vm.StockVerificationText
Check 'yeni altyapı da listede' ([IO.File]::ReadAllText($facilities) -like '*- EMC lab*')

"--- süzgeç"
($vm.VerificationFilters | Where-Object { $_.Key -eq 'ÇK/EMC' }).IsChecked = $true; Wait
Check 'süzgeç: yalnız ÇK/EMC' ($vm.VerificationGroups.Count -eq 1 -and $vm.VerificationGroups[0].Title -eq 'ÇK/EMC')
$vm.VerificationFilters[0].IsChecked = $true; Wait
Check 'Hepsi: iki grup' ($vm.VerificationGroups.Count -eq 2)

"--- birim notunda bağlantı, Dosyayı aç"
$unit = Join-Path $stock 'AB-7000-0002.md'
$unitText = [IO.File]::ReadAllText($unit)
Check 'birim notunun YAML''ında bağlantı' ($unitText -like '*test_gereksinimleri: "`[`[AB-7000-0002_Test_Gereksinimleri`]`]"*') ($unitText.Substring(0, [Math]::Min(300, $unitText.Length)))
Check 'Dosyayı aç görünür' ($vm.HasVerificationFile)

"--- durum değiştirme"
$row = ($vm.VerificationGroups | Where-Object { $_.Title -eq 'İşlevsel' }).Rows[0]
Check 'durum seçenekleri 7 (Planlandı dahil)' (@($vm.VerificationStatuses).Count -eq 7 -and @($vm.VerificationStatuses)[1] -eq 'Planlandı') (@($vm.VerificationStatuses) -join ',')
$vm.SetVerificationStatus($row, 'Yapılıyor'); Wait
$text = [IO.File]::ReadAllText($file)
Check 'yalnız durum hücresi değişti' ($text -like '*| 1 | Kazanç ≥ 5 dBi @ 2,4 GHz | İşlevsel | Anten örüntü | SATIMO | TG-012 |  |  | Yapılıyor |*' -and $text -like '*| 2 | RE102 \| CE102 sınır içinde | ÇK/EMC |*') $text
$row = ($vm.VerificationGroups | Where-Object { $_.Title -eq 'İşlevsel' }).Rows[0]
Check 'yapılıyor türü' ($row.StatusKind -eq 3 -and $row.StatusText -eq 'Yapılıyor')

"--- düzenleme"
$vm.EditVerification($row); Wait
Check 'düzenleme penceresi dolu' ($vm.IsVerificationDialogOpen -and $vm.IsEditingVerification -and $vm.NewVerText -eq 'Kazanç ≥ 5 dBi @ 2,4 GHz' -and $vm.FacilityPick.Selected -eq 'SATIMO' -and $vm.NewVerSubGroup -eq 'Anten örüntü' -and $vm.NewVerStatus -eq 'Yapılıyor') $vm.VerificationDialogTitle
$vm.NewVerText = 'Kazanç ≥ 6 dBi @ 2,4 GHz'; $vm.NewVerTgNo = 'TG-013'; $vm.NewVerTpNo = 'TP-004'; $vm.NewVerTrNo = 'TR-009'
Check 'kaydedilir' ($vm.ConfirmNewVerification())
$vm.IsVerificationDialogOpen = $false; Wait
$text = [IO.File]::ReadAllText($file)
Check 'satır yerinde değişti, numara aynı' ($text -like '*| 1 | Kazanç ≥ 6 dBi @ 2,4 GHz | İşlevsel | Anten örüntü | SATIMO | TG-013 | TP-004 | TR-009 | Yapılıyor |*' -and $text -notlike '*5 dBi*' -and $vm.VerificationCount -eq 2) $text
$vm.AddVerificationCommand.Execute($null); Wait
Check 'yeni madde penceresi düzenleme kipinde değil' (-not $vm.IsEditingVerification -and $vm.NewVerText -eq '' -and $vm.NewVerTpNo -eq '')
Check 'metin ve alt grup boşsa eklenmez' (-not $vm.ConfirmNewVerification() -and $vm.VerificationError.Length -gt 0)
$vm.KeepVerificationOpen = $false; $vm.NewVerSubGroup = 'S11'; $vm.NewVerStatus = 'Planlandı'
Check 'yalnız alt grupla eklenir (madde sonra)' ($vm.ConfirmNewVerification())
$vm.IsVerificationDialogOpen = $false; Wait
$later = ($vm.VerificationGroups | Where-Object { $_.Title -eq 'İşlevsel' }).Rows | Where-Object { $_.SubGroup -eq 'S11' }
Check 'sonra yazılacak satır listede' ($later -and $later.IsTextLater -and $later.StatusKind -eq 6 -and $vm.VerificationCount -eq 3) ([string]$vm.VerificationCount)
Snap $win "$Out\verification.png"

"--- kapsam dışı ilerlemeye sayılmaz"
$row = ($vm.VerificationGroups | Where-Object { $_.Title -eq 'İşlevsel' }).Rows[0]
$vm.SetVerificationStatus($row, 'Kapsam dışı'); Wait
Check 'Doğrulama 1/2 (kapsam dışı sayılmaz)' ($vm.StockVerificationText -like '*1/2*' -and $vm.VerificationProgress -eq 50) $vm.StockVerificationText

"--- silme"
$row = ($vm.VerificationGroups | Where-Object { $_.Title -eq 'İşlevsel' }).Rows[0]
$vm.RequestDeleteVerification($row); Wait
Check 'önce sorulur' ($vm.IsVerificationDeleteOpen -and $vm.VerificationDeleteText -like '1. Kazanç*') $vm.VerificationDeleteText
Check 'sormadan silinmedi' ($vm.VerificationCount -eq 3)
$vm.ConfirmDeleteVerification(); Wait
$text = [IO.File]::ReadAllText($file)
Check 'satır kalktı, diğerinin numarası aynı' ($vm.VerificationCount -eq 2 -and $text -notlike '*| 1 | Kazanç*' -and $text -like '*| 2 | RE102*' -and $text -like '*## Plan*') $text

"--- elle yazılmış dosya: başka satırlara dokunulmaz"
$other = Join-Path $Data '7000_MILKON\AB-7000-0003'
$md = Join-Path $other '03_Testler\AB-7000-0003_Test_Gereksinimleri_AA.md'
New-Item -ItemType Directory -Force (Split-Path $md) | Out-Null
$hand = "# Elle`n`nGiriş metni.`n`n| # | Madde | Grup | Alt | Altyapı | TG | Durum |`n|---|---|---|---|---|---|---|`n| 7 | Ağırlık ≤ 1,2 kg | Özellik |  |  |  | Geçti |`n`nSon not.`n"
[IO.File]::WriteAllText($md, $hand, (New-Object System.Text.UTF8Encoding($false)))
$vm.SelectNodeByPath($other); Wait; Wait; Wait
Check 'revizyonlu dosya okunur' ($vm.VerificationCount -eq 1) ([string]$vm.VerificationCount)
$vm.AddVerificationCommand.Execute($null); Wait
$vm.KeepVerificationOpen = $false
$vm.NewVerText = 'Boyut 100x50 mm'
Check 'eklenir' ($vm.ConfirmNewVerification())
$vm.IsVerificationDialogOpen = $false; Wait
$after = [IO.File]::ReadAllText($md)
Check 'eski 7 sütunlu tablo 9 sütuna yükseldi, numara 8 sonda' ($after -like "*| # | Doğrulama maddesi | Grup | Alt grup | Altyapı | TG no | TP no | TR no | Durum |`n|---|---|---|---|---|---|---|---|---|`n| 7 | Ağırlık ≤ 1,2 kg | Özellik |  |  |  |  |  | Geçti |`n| 8 | Boyut 100x50 mm | İşlevsel |  |  |  |  |  | Bekliyor |`n`nSon not.*") $after
Check 'giriş ve son not korunur, LF korunur' ($after.StartsWith("# Elle`n`nGiriş metni.") -and $after -notlike "*`r`n*")
Check 'yeni dosya açılmadı' (@(Get-ChildItem (Split-Path $md) -Filter *.md).Count -eq 1)

$vm.SelectNodeByPath($stock); Wait; Wait
$vm.AddVerificationCommand.Execute($null); Wait
$vm.NewVerText = 'Yan lob ≤ −15 dB (E ve H düzlemi)'; $vm.NewVerSubGroup = 'Anten örüntü'; $vm.NewVerFacility = 'Döner tabla'; Wait
Check 'yeni altyapı: yazı kutusu açık' ($vm.FacilityPick.IsNew -and $vm.FacilityPick.NewText -eq 'Döner tabla')
# Pencere ayrı bir Popup'ta: çocuğunu pencerenin üstüne çizerek görüntü alınır.
$dlg = $null; $st = New-Object System.Collections.Stack; $st.Push($win)
while ($st.Count) { $e = $st.Pop(); if ($e.GetType().Name -eq 'ContentDialog' -and $e.IsOpen) { $dlg = $e }; for ($i = 0; $i -lt [System.Windows.Media.VisualTreeHelper]::GetChildrenCount($e); $i++) { $st.Push([System.Windows.Media.VisualTreeHelper]::GetChild($e, $i)) } }
if ($dlg)
{
    $child = $dlg.Template.FindName('PART_Popup', $dlg).Child
    $child.Measure((New-Object System.Windows.Size 1360, 900)); $child.Arrange((New-Object System.Windows.Rect 0, 0, 1360, 900)); $child.UpdateLayout(); Wait
    $rtb = New-Object System.Windows.Media.Imaging.RenderTargetBitmap 2040, 1350, 144, 144, ([System.Windows.Media.PixelFormats]::Pbgra32)
    $dv = New-Object System.Windows.Media.DrawingVisual; $dc = $dv.RenderOpen()
    $dc.DrawRectangle((New-Object System.Windows.Media.VisualBrush $win), $null, (New-Object System.Windows.Rect 0, 0, 1360, 900))
    $dc.DrawRectangle((New-Object System.Windows.Media.VisualBrush $child), $null, (New-Object System.Windows.Rect 0, 0, 1360, 900))
    $dc.Close(); $rtb.Render($dv)
    $enc = New-Object System.Windows.Media.Imaging.PngBitmapEncoder; $enc.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($rtb))
    $fs = [IO.File]::Create("$Out\verification-dialog.png"); $enc.Save($fs); $fs.Close()
}
$vm.IsVerificationDialogOpen = $false; Wait
$win.Close()
"SONUÇ OK=$ok HATA=$fail"