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
Check 'dosyada 14 sütunlu başlık (TP / TR yok, Elle durum var)' ($text -like '*| # | TG no | Kaynak | Başlık | Kısa not | Gereksinim | Grup | Alt grup | Tür | Bağlı | Yöntem | Altyapı | Durum |*' -and $text -notlike '*TP no*') $text
Check 'dosyada satır' ($text -like '*| 1 | TG-012 |  | Kazanç ≥ 5 dBi @ 2,4 GHz |  |  | İşlevsel | Anten örüntü |  |  |  | SATIMO | Bekliyor |*') $text
Check 'dosyada Plan ve Rapor kısımları' ($text -like '*## Plan*' -and $text -like '*## Rapor*')
Check 'altyapı ortak listede' ((Test-Path $facilities) -and ([IO.File]::ReadAllText($facilities) -like '*- SATIMO*'))
Check 'listede 1 madde' ($vm.VerificationCount -eq 1 -and $vm.VerificationGroups.Count -eq 1)

"--- ikinci madde: grup ÇK/EMC, metinde | var"
$vm.AddVerificationCommand.Execute($null); Wait
Check 'altyapı önerilerinde SATIMO' ($vm.FacilitySuggestions -contains 'SATIMO')
Check 'alt grup önerisi' ($vm.SubGroupSuggestions -contains 'Anten örüntü')
$vm.NewVerFacility = 'satimo'
Check 'var olan altyapı listeden seçilir' (-not $vm.FacilityPick.IsNew -and $vm.FacilityPick.Selected -eq 'SATIMO')
($vm.VerificationGroupChoices | Where-Object { $_.Key -eq 'ÇK/EMC' }).IsChecked = $true
$vm.NewVerText = 'RE102 | CE102 sınır içinde'; $vm.NewVerSubGroup = ''; $vm.NewVerFacility = 'EMC lab'; $vm.NewVerStatus = 'Geçti'
Check 'eklenir, pencere kapanabilir (açık kalma seçeneği yok)' ($vm.ConfirmNewVerification())
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
Check 'durum seçenekleri 10 (Planlandı, Hazırlık yapılıyor, Değerlendiriliyor, Şartlı kabul dahil)' (@($vm.VerificationStatuses).Count -eq 10 -and @($vm.VerificationStatuses)[2] -eq 'Hazırlık yapılıyor' -and @($vm.VerificationStatuses)[1] -eq 'Planlandı') (@($vm.VerificationStatuses) -join ',')
$vm.SetVerificationStatus($row, 'Yapılıyor'); Wait
$text = [IO.File]::ReadAllText($file)
Check 'yalnız durum hücresi değişti' ($text -like '*| 1 | TG-012 |  | Kazanç ≥ 5 dBi @ 2,4 GHz |  |  | İşlevsel | Anten örüntü |  |  |  | SATIMO | Bekliyor | Yapılıyor |*' -and $text -like '*| 2 |  |  | RE102 \| CE102 sınır içinde |  |  | ÇK/EMC |*') $text
$row = ($vm.VerificationGroups | Where-Object { $_.Title -eq 'İşlevsel' }).Rows[0]
Check 'yapılıyor türü' ($row.StatusKind -eq 3 -and $row.StatusText -eq 'Yapılıyor')

"--- düzenleme"
$vm.EditVerification($row); Wait
Check 'düzenleme penceresi dolu' ($vm.IsVerificationDialogOpen -and $vm.IsEditingVerification -and $vm.NewVerText -eq 'Kazanç ≥ 5 dBi @ 2,4 GHz' -and $vm.FacilityPick.Selected -eq 'SATIMO' -and $vm.NewVerSubGroup -eq 'Anten örüntü' -and $vm.NewVerStatus -eq 'Yapılıyor') $vm.VerificationDialogTitle
$vm.NewVerText = 'Kazanç ≥ 6 dBi @ 2,4 GHz'; $vm.NewVerDescription = '≥ 6 dBi ana yönde'; $vm.NewVerRequirement = 'Anten 2,4 GHz''de ana yönde en az 6 dBi kazanç sağlamalıdır.'; $vm.NewVerKind = 'Temel'; $vm.NewVerMethod = 'Test'; $vm.NewVerTgNo = 'TG-013'
Check 'kaydedilir' ($vm.ConfirmNewVerification())
$vm.IsVerificationDialogOpen = $false; Wait
$text = [IO.File]::ReadAllText($file)
Check 'satır yerinde değişti, numara aynı' ($text -like '*| 1 | TG-013 |  | Kazanç ≥ 6 dBi @ 2,4 GHz | ≥ 6 dBi ana yönde | Anten 2,4 GHz''de ana yönde en az 6 dBi kazanç sağlamalıdır. | İşlevsel | Anten örüntü | Temel |  | Test | SATIMO | Bekliyor | Yapılıyor |*' -and $text -notlike '*5 dBi*' -and $vm.VerificationCount -eq 2) $text

$row1 = ($vm.VerificationGroups | Where-Object { $_.Title -eq 'İşlevsel' }).Rows[0]
Check 'satır: TG no, kısa not, tür, yöntem, plan / rapor' ($row1.Id -eq 'TG-013' -and $row1.Description -like '≥ 6 dBi ana yönde*' -and $row1.KindText -eq 'Temel' -and $row1.MethodTone -eq 1) ($row1.Id + ' ' + $row1.KindText)
$vm.ToggleVerification($row1)
Check 'satır açılır: tam gereksinim ve kaynak satırı' ($row1.IsExpanded -and $row1.Requirement -like 'Anten 2,4 GHz*' -and $row1.SourceLine -like '*TG-013*') $row1.SourceLine

"--- türetilmiş madde"
$vm.AddVerificationCommand.Execute($null); Wait
Check 'bağlı temel seçenekleri TG no''lardan' (@($vm.VerificationParents) -contains 'TG-013')
$vm.NewVerTgNo = 'AB-7000-0002_TAS_01'; $vm.NewVerText = 'Yan lob seviyesi'; $vm.NewVerDescription = 'Ana hüzmeye göre ≤ −15 dB'
$vm.NewVerKind = 'Türetilmiş'; $vm.NewVerParent = 'TG-013'; $vm.NewVerMethod = 'Test'; $vm.NewVerStatus = 'Değerlendiriliyor'
Check 'türetilmiş madde eklenir' ($vm.IsNewVerDerived -and $vm.ConfirmNewVerification())
$vm.IsVerificationDialogOpen = $false; Wait
$der = ($vm.VerificationGroups | Where-Object { $_.Title -eq 'İşlevsel' }).Rows | Where-Object { $_.Text -eq 'Yan lob seviyesi' }
Check 'türetilmiş: doküman no TG kutusunda, bağlı temel, değerlendiriliyor' ($der.Id -eq 'AB-7000-0002_TAS_01' -and $der.ParentText -eq '← TG-013' -and $der.StatusKind -eq 8) ($der.Id + ' ' + $der.ParentText)
$row1 = ($vm.VerificationGroups | Where-Object { $_.Title -eq 'İşlevsel' }).Rows[0]
Check 'temelin kaynak satırında bağlı türetilmiş' ($row1.SourceLine -like '*Yan lob seviyesi*') $row1.SourceLine
$row1 = ($vm.VerificationGroups | Where-Object { $_.Title -eq 'İşlevsel' }).Rows[0]; $row1.IsExpanded = $true; Wait
"--- sütun genişliği"
$cols = $vm.VerificationColumns; $before = $cols.Kind.Value
$cols.Resize('kind', 40); $vm.SaveVerificationColumns()
Check 'tür sütunu genişledi ve kaydedildi' ($cols.Kind.Value -eq $before + 40 -and (@($vm.Settings.Columns['verification']) | Where-Object { $_.Key -eq 'kind' }).Width -eq $before + 40) ([string]$cols.Kind.Value)
$cols.Resize('kind', -5000)
Check 'en dar sınır' ($cols.Kind.Value -eq 56)
$cols.Reset()
Snap $win "$Out\verification.png"
$vm.AddVerificationCommand.Execute($null); Wait
Check 'yeni madde penceresi düzenleme kipinde değil' (-not $vm.IsEditingVerification -and $vm.NewVerText -eq '')
Check 'metin ve alt grup boşsa eklenmez' (-not $vm.ConfirmNewVerification() -and $vm.VerificationError.Length -gt 0)
$vm.NewVerSubGroup = 'S11'; $vm.NewVerStatus = 'Planlandı'
Check 'yalnız alt grupla eklenir (madde sonra)' ($vm.ConfirmNewVerification())
$vm.IsVerificationDialogOpen = $false; Wait
$later = ($vm.VerificationGroups | Where-Object { $_.Title -eq 'İşlevsel' }).Rows | Where-Object { $_.SubGroup -eq 'S11' }
Check 'sonra yazılacak satır listede' ($later -and $later.IsTextLater -and $later.StatusKind -eq 6 -and $vm.VerificationCount -eq 4) ([string]$vm.VerificationCount)

"--- kapsam dışı ilerlemeye sayılmaz"
$row = ($vm.VerificationGroups | Where-Object { $_.Title -eq 'İşlevsel' }).Rows[0]
$vm.SetVerificationStatus($row, 'Kapsam dışı'); Wait
Check 'Doğrulama 1/3 (kapsam dışı sayılmaz)' ($vm.StockVerificationText -like '*1/3*' -and [Math]::Round($vm.VerificationProgress) -eq 33) $vm.StockVerificationText

"--- silme"
$row = ($vm.VerificationGroups | Where-Object { $_.Title -eq 'İşlevsel' }).Rows[0]
$vm.RequestDeleteVerification($row); Wait
Check 'önce sorulur' ($vm.IsVerificationDeleteOpen -and $vm.VerificationDeleteText -like '1. Kazanç*') $vm.VerificationDeleteText
Check 'sormadan silinmedi' ($vm.VerificationCount -eq 4)
$vm.ConfirmDeleteVerification(); Wait
$text = [IO.File]::ReadAllText($file)
Check 'satır kalktı, diğerinin numarası aynı' ($vm.VerificationCount -eq 3 -and $text -notlike '*| 1 | Kazanç*' -and $text -like '*| 2 |  |  | RE102*' -and $text -like '*## Plan*') $text

"--- durum süzgeci (çoklu seçim) ve sıralama"
Check 'durum süzgeci: 10 durum, seçim yok = Hepsi' (@($vm.VerificationStatusFilters).Count -eq 10 -and $vm.VerificationStatusFilterText -eq [Pusula.Wpf.Properties.Resources]::Home_RangeAll -and $vm.VerificationShownText -eq '3 / 3 madde') ($vm.VerificationStatusFilterText + ' | ' + $vm.VerificationShownText)
function ShownIds { @($vm.VerificationGroups | ForEach-Object { $_.Rows } | ForEach-Object { if ($_.Text) { $_.Text } else { $_.SubGroup } }) -join ',' }
(@($vm.VerificationStatusFilters | Where-Object Key -eq 'Planlandı')[0]).IsChecked = $true; Wait
Check 'Planlandı: yalnız S11, kutuda adı' ((ShownIds) -eq 'S11' -and $vm.VerificationStatusFilterText -eq 'Planlandı' -and $vm.VerificationShownText -eq '1 / 3 madde') ((ShownIds) + ' | ' + $vm.VerificationStatusFilterText)
(@($vm.VerificationStatusFilters | Where-Object Key -eq 'Geçti')[0]).IsChecked = $true; Wait
Check 'Planlandı + Geçti: iki madde, kutuda "2 durum"' (@($vm.VerificationGroups | ForEach-Object { $_.Rows }).Count -eq 2 -and $vm.VerificationStatusFilterText -eq '2 durum') ((ShownIds) + ' | ' + $vm.VerificationStatusFilterText)
($vm.VerificationFilters | Where-Object { $_.Key -eq 'ÇK/EMC' }).IsChecked = $true; Wait
Check 'grup + durum birlikte; işaretler korunur, sayılar gruba göre' ((ShownIds) -eq 'RE102 | CE102 sınır içinde' -and @($vm.VerificationStatusFilters | Where-Object IsChecked).Count -eq 2 -and (@($vm.VerificationStatusFilters | Where-Object Key -eq 'Planlandı')[0]).Count -eq 0) (ShownIds)
$vm.VerificationFilters[0].IsChecked = $true; Wait
$vm.ClearVerificationStatusFilter(); Wait
Check 'Temizle: hepsi geri' (@($vm.VerificationGroups | ForEach-Object { $_.Rows }).Count -eq 3 -and -not $vm.HasVerificationStatusFilter) (ShownIds)
$isl = { (@(($vm.VerificationGroups | Where-Object { $_.Title -eq 'İşlevsel' }).Rows) | ForEach-Object { if ($_.Text) { $_.Text } else { $_.SubGroup } }) -join ',' }
Check 'sıra no: Yan lob, S11' ((& $isl) -eq 'Yan lob seviyesi,S11') (& $isl)
$vm.VerificationSort = [Pusula.Wpf.Properties.Resources]::Verify_SortStatus; Wait
Check 'duruma göre: Planlandı önce, Değerlendiriliyor sonra' ((& $isl) -eq 'S11,Yan lob seviyesi') (& $isl)
$vm.VerificationSort = [Pusula.Wpf.Properties.Resources]::Verify_SortNumber; Wait
$r0 = @(($vm.VerificationGroups | Where-Object { $_.Title -eq 'İşlevsel' }).Rows)[0]
Check 'tür rengi: türetilmiş 2' ($r0.KindTone -eq 2) ([string]$r0.KindTone)
$vm.EditVerification($r0); Wait
for ($k = 0; $k -lt 20; $k++) { Pump; Start-Sleep -Milliseconds 30 }
function Find-Open($node) { if ($node -eq $null) { return $null }; if ($node -is [DesignKit.Controls.ContentDialog] -and $node.IsOpen) { return $node }; for ($i = 0; $i -lt [System.Windows.Media.VisualTreeHelper]::GetChildrenCount($node); $i++) { $r = Find-Open ([System.Windows.Media.VisualTreeHelper]::GetChild($node, $i)); if ($r) { return $r } }; return $null }
function Find-Popup($node) { if ($node -is [System.Windows.Controls.Primitives.Popup]) { return $node }; for ($i = 0; $i -lt [System.Windows.Media.VisualTreeHelper]::GetChildrenCount($node); $i++) { $r = Find-Popup ([System.Windows.Media.VisualTreeHelper]::GetChild($node, $i)); if ($r) { return $r } }; return $null }
$dlg = Find-Open $win
if ($dlg) {
    $pop = Find-Popup $dlg; if ($pop -and $pop.Child) { $dlg = $pop.Child; $sz = New-Object System.Windows.Size($win.ActualWidth, $win.ActualHeight); $dlg.Measure($sz); $dlg.Arrange((New-Object System.Windows.Rect($sz))); $dlg.UpdateLayout(); for ($k = 0; $k -lt 20; $k++) { Pump; Start-Sleep -Milliseconds 30 } }
    $bmp = New-Object System.Windows.Media.Imaging.RenderTargetBitmap([int]$dlg.ActualWidth, [int]$dlg.ActualHeight, 96, 96, [System.Windows.Media.PixelFormats]::Pbgra32)
    $bmp.Render($dlg); $enc = New-Object System.Windows.Media.Imaging.PngBitmapEncoder; $enc.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($bmp))
    $fs = [IO.File]::Create("$Out\verification-edit.png"); $enc.Save($fs); $fs.Close()
}
$vm.IsVerificationDialogOpen = $false; Wait
Snap $win "$Out\verification-filter.png"
"--- plan maddesi (çoka çok)"
function RowOf([string]$sub) { @($vm.VerificationGroups | ForEach-Object { $_.Rows } | Where-Object { $_.SubGroup -eq $sub -or $_.Text -eq $sub })[0] }
function Snap-Dialog([string]$name) {
    for ($k = 0; $k -lt 20; $k++) { Pump; Start-Sleep -Milliseconds 30 }
    $dlg = Find-Open $win
    if ($dlg) {
        $pop = Find-Popup $dlg; if ($pop -and $pop.Child) { $dlg = $pop.Child; $sz = New-Object System.Windows.Size($win.ActualWidth, $win.ActualHeight); $dlg.Measure($sz); $dlg.Arrange((New-Object System.Windows.Rect($sz))); $dlg.UpdateLayout(); for ($k = 0; $k -lt 20; $k++) { Pump; Start-Sleep -Milliseconds 30 } }
        $bmp = New-Object System.Windows.Media.Imaging.RenderTargetBitmap([int]$dlg.ActualWidth, [int]$dlg.ActualHeight, 96, 96, [System.Windows.Media.PixelFormats]::Pbgra32)
        $bmp.Render($dlg); $enc = New-Object System.Windows.Media.Imaging.PngBitmapEncoder; $enc.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($bmp))
        $fs = [IO.File]::Create("$Out\$name"); $enc.Save($fs); $fs.Close()
    }
}
$KeyOf = { param($row) [Pusula.Core.Testing.VerificationList]::KeyOf($row.Item) }
$PRL = [Pusula.Core.Testing.PlanReportList]
$s11 = RowOf 'S11'
$vm.SetVerificationStatus($s11, 'Bekliyor'); Wait
$s11 = RowOf 'S11'; $yan = RowOf 'Yan lob seviyesi'
$s11Key = & $KeyOf $s11; $yanKey = & $KeyOf $yan
Check 'plan yok: kartta Test planı ekle' (-not $s11.HasPlan)
$vm.OpenVerPlan($s11); Wait
Check 'yeni plan penceresi: başlık, Sil yok, S11 seçili, TP önerildi' ($vm.IsVerPlanOpen -and $vm.VerPlanTitle -eq 'Plan maddesi' -and $vm.VerPlanDeleteText -eq '' -and ((@($vm.PlanRequirementOptions | Where-Object IsChecked | ForEach-Object Key)) -join ',') -eq $s11Key -and $vm.PlanTpNo.Length -gt 0) ($vm.VerPlanTitle + ' / ' + $vm.PlanTpNo)
$vm.PlanTpNo = '4.1'; $vm.PlanDescription = 'VNA S11 ölçümü'; $vm.PlanMethod = 'Test'; $vm.PlanFacilityPick.Value = 'VNA'; $vm.PlanNote = 'Kalibrasyon kiti 3,5 mm'
@($vm.PlanRequirementOptions | Where-Object Key -eq $yanKey)[0].IsChecked = $true
Check 'seçim özeti iki gereksinim' ($vm.PlanSelectedText -like '2 seçili:*') $vm.PlanSelectedText
$vm.PlanSearch = 'Yan lob'
Check 'arama süzer' (@($vm.PlanRequirementOptions | Where-Object IsVisible).Count -ge 1 -and @($vm.PlanRequirementOptions | Where-Object IsVisible).Count -lt $vm.PlanRequirementOptions.Count)
$vm.PlanSearch = ''
Snap-Dialog 'verification-plan.png'
Check 'kaydedilir' ($vm.ConfirmVerPlan())
$vm.IsVerPlanOpen = $false; Wait
$p41 = @($PRL::ReadPlans($stock) | Where-Object TpNo -eq '4.1')[0]
Check 'Plan tablosunda 4.1: iki gereksinim, yöntem, altyapı, not' ($p41 -and $p41.Requirements.Count -eq 2 -and $p41.Requirements -contains $s11Key -and $p41.Requirements -contains $yanKey -and $p41.Method -eq 'Test' -and $p41.Facility -eq 'VNA' -and $p41.Note -eq 'Kalibrasyon kiti 3,5 mm')
$s11 = RowOf 'S11'
Check 'S11: kartta 4.1, Bekliyor → Planlandı' ($s11.HasPlan -and $s11.PlanItems[0].TpNo -eq '4.1' -and $s11.StatusText -eq 'Planlandı') $s11.StatusText
Check 'ileri durum değişmez (Değerlendiriliyor)' ((RowOf 'Yan lob seviyesi').StatusText -eq 'Değerlendiriliyor') (RowOf 'Yan lob seviyesi').StatusText
$vm.NewPlanItem(); $vm.PlanTpNo = '4.1'
Check 'aynı TP no reddedilir' (-not $vm.ConfirmVerPlan() -and $vm.PlanError -like '*4.1*') $vm.PlanError
$vm.PlanTpNo = ' '
Check 'boş TP no reddedilir' (-not $vm.ConfirmVerPlan() -and $vm.PlanError.Length -gt 0)
$vm.IsVerPlanOpen = $false; Wait
"--- rapor maddesi"
$s11 = RowOf 'S11'
Check 'rapor yok: kartta Rapor ekle' (-not $s11.HasReport)
$vm.OpenVerReport($s11); Wait
Check 'yeni rapor penceresi: S11 planı 4.1 seçili' ($vm.IsVerReportOpen -and $vm.VerReportTitle -eq 'Rapor maddesi' -and ((@($vm.ReportPlanOptions | Where-Object IsChecked | ForEach-Object Key)) -join ',') -eq '4.1' -and @($vm.VerificationResults).Count -eq 3) $vm.VerReportTitle
$measure = Join-Path $stock '03_Testler\01_Islevsel\S11\SNP1_T1'
New-Item -ItemType Directory -Force $measure | Out-Null
$vm.ReportTrNo = '5.3'; $vm.ReportDescription = 'S11 sonuçları'; $vm.ReportResult = 'Kaldı'; $vm.ReportPath = $measure; $vm.ReportNote = '−8 dB · BN-2026-020'
Snap-Dialog 'verification-report.png'
Check 'kaydedilir' ($vm.ConfirmVerReport())
$vm.IsVerReportOpen = $false; Wait
$r53 = @($PRL::ReadReports($stock) | Where-Object TrNo -eq '5.3')[0]
Check 'Rapor tablosunda 5.3: plan 4.1, Kaldı, yer göreli' ($r53 -and ($r53.Plans -join ',') -eq '4.1' -and $r53.Result -eq 'Kaldı' -and $r53.ResultPath -eq '03_Testler\01_Islevsel\S11\SNP1_T1' -and $r53.Note -eq '−8 dB · BN-2026-020')
$s11 = RowOf 'S11'
Check 'bir rapor Kaldı → S11 Kaldı; Yan lob elle Değerlendiriliyor kalır, yanında hesap: Kaldı' ($s11.StatusText -eq 'Kaldı' -and (RowOf 'Yan lob seviyesi').StatusText -eq 'Değerlendiriliyor' -and (RowOf 'Yan lob seviyesi').AutoStatusText -eq 'hesap: Kaldı' -and (RowOf 'Yan lob seviyesi').Item.Status -eq 'Kaldı') ($s11.StatusText + ' / ' + (RowOf 'Yan lob seviyesi').StatusText)
Check 'kart: rapor 5.3 kırmızı' ($s11.HasReport -and $s11.ReportItems[0].TrNo -eq '5.3' -and $s11.ReportItems[0].ResultKind -eq 2)
$vm.EditReportItem($r53); $vm.ReportResult = 'Geçti'
Check 'Geçti kaydedilir' ($vm.ConfirmVerReport()); $vm.IsVerReportOpen = $false; Wait
Check 'her plan raporlu ve hepsi Geçti → Geçti' ((RowOf 'S11').StatusText -eq 'Geçti') (RowOf 'S11').StatusText
$vm.EditReportItem(@($PRL::ReadReports($stock) | Where-Object TrNo -eq '5.3')[0]); $vm.ReportResult = 'Hazırlanmadı'
Check 'Hazırlanmadı kaydedilir' ($vm.ConfirmVerReport()); $vm.IsVerReportOpen = $false; Wait
Check 'sonuç yok → Planlandı' ((RowOf 'S11').StatusText -eq 'Planlandı') (RowOf 'S11').StatusText
"--- TP no değişimi bağları taşır"
$vm.EditPlanItem(@($PRL::ReadPlans($stock) | Where-Object TpNo -eq '4.1')[0])
Check 'düzenleme penceresi: başlık, Sil var' ($vm.VerPlanTitle -eq 'Plan maddesi · 4.1' -and $vm.VerPlanDeleteText -eq 'Sil') $vm.VerPlanTitle
$vm.PlanTpNo = '4.10'
Check 'yeniden adlandırılır' ($vm.ConfirmVerPlan()); $vm.IsVerPlanOpen = $false; Wait
Check 'rapor 5.3 artık 4.10 a bağlı, S11 kartında 4.10' ((@($PRL::ReadReports($stock) | Where-Object TrNo -eq '5.3')[0].Plans -join ',') -eq '4.10' -and (RowOf 'S11').PlanItems[0].TpNo -eq '4.10')
$oldH = $win.Height; $win.Height = 1800; $s11 = RowOf 'S11'; $s11.IsExpanded = $true; Wait; Wait
Snap $win "$Out\verification-plan-card.png"
$win.Height = $oldH
$vm.IsVerPlanView = $true; Wait
Snap $win "$Out\verification-plan-view.png"
$vm.IsVerReqView = $true; Wait
"--- silme: yalnız kendi satırı"
$before = [IO.File]::ReadAllText($file)
$vm.EditReportItem(@($PRL::ReadReports($stock) | Where-Object TrNo -eq '5.3')[0]); $vm.RequestDeleteReportItem()
Check 'silme onayı sorulur' ($vm.IsVerLinkDeleteOpen -and $vm.VerLinkDeleteText -like '*5.3*') $vm.VerLinkDeleteText
$vm.ConfirmDeleteVerLink(); Wait
Check 'rapor 5.3 gitti, S11 Planlandı, rapor kartı boş' (@($PRL::ReadReports($stock) | Where-Object TrNo -eq '5.3').Count -eq 0 -and (RowOf 'S11').StatusText -eq 'Planlandı' -and -not (RowOf 'S11').HasReport)
$vm.EditPlanItem(@($PRL::ReadPlans($stock) | Where-Object TpNo -eq '4.10')[0]); $vm.IsVerPlanOpen = $false; $vm.RequestDeletePlanItem(); $vm.ConfirmDeleteVerLink(); Wait
Check 'plan 4.10 gitti, S11 Bekliyor, plan kartı boş' (@($PRL::ReadPlans($stock) | Where-Object TpNo -eq '4.10').Count -eq 0 -and (RowOf 'S11').StatusText -eq 'Bekliyor' -and -not (RowOf 'S11').HasPlan) (RowOf 'S11').StatusText
$text = [IO.File]::ReadAllText($file)
Check 'ölçüm klasörü silinmedi' (Test-Path $measure)
Check 'tablo bozulmadı' ([Pusula.Core.Testing.VerificationList]::Read($stock).Count -eq $vm.VerificationCount)
Check 'sıradaki no' ($PRL::NextNo([string[]]@('4.4', '4.10', '4.2'), '1') -eq '4.11' -and $PRL::NextNo([string[]]@(), '1') -eq '1' -and $PRL::NextNo([string[]]@('TP-9'), '1') -eq 'TP-10')
Check 'yol yardımcıları' ([Pusula.Wpf.ViewModels.MainViewModel]::RelativeResultPath('C:\a\b', 'C:\a\b\c\d') -eq 'c\d' -and [Pusula.Wpf.ViewModels.MainViewModel]::RelativeResultPath('C:\a\b', 'D:\x') -eq 'D:\x' -and [Pusula.Wpf.ViewModels.MainViewModel]::FullResultPath('C:\a\b', 'c\d') -eq 'C:\a\b\c\d')
"--- elle yazılmış dosya: başka satırlara dokunulmaz"
$other = Join-Path $Data '7000_MILKON\AB-7000-0003'
$md = Join-Path $other '03_Testler\AB-7000-0003_Test_Gereksinimleri_AA.md'
New-Item -ItemType Directory -Force (Split-Path $md) | Out-Null
$hand = "# Elle`n`nGiriş metni.`n`n| # | Madde | Grup | Alt | Altyapı | TG | Durum |`n|---|---|---|---|---|---|---|`n| 7 | Ağırlık ≤ 1,2 kg | Özellik |  |  |  | Geçti |`n`nSon not.`n"
[IO.File]::WriteAllText($md, $hand, (New-Object System.Text.UTF8Encoding($false)))
$vm.SelectNodeByPath($other); Wait; Wait; Wait
Check 'revizyonlu dosya okunur' ($vm.VerificationCount -eq 1) ([string]$vm.VerificationCount)
$vm.AddVerificationCommand.Execute($null); Wait
$vm.NewVerText = 'Boyut 100x50 mm'
Check 'eklenir' ($vm.ConfirmNewVerification())
$vm.IsVerificationDialogOpen = $false; Wait
$after = [IO.File]::ReadAllText($md)
Check 'eski 7 sütunlu tablo 14 sütuna yükseldi, numara 8 sonda' ($after -like "*| # | TG no | Kaynak | Başlık | Kısa not | Gereksinim | Grup | Alt grup | Tür | Bağlı | Yöntem | Altyapı | Durum | Elle durum |`n|---|---|---|---|---|---|---|---|---|---|---|---|---|---|`n| 7 |  |  | Ağırlık ≤ 1,2 kg |  |  | Özellik |  |  |  |  |  | Geçti |  |`n| 8 |  |  | Boyut 100x50 mm |  |  | İşlevsel |  |  |  |  |  | Bekliyor |  |`n`nSon not.*") $after
Check 'giriş ve son not korunur, LF korunur' ($after.StartsWith("# Elle`n`nGiriş metni.") -and $after -notlike "*`r`n*")
Check 'yeni dosya açılmadı' (@(Get-ChildItem (Split-Path $md) -Filter *.md).Count -eq 1)

$vm.SelectNodeByPath($stock); Wait; Wait
$vm.AddVerificationCommand.Execute($null); Wait
$vm.NewVerText = 'Yan lob ≤ −15 dB (E ve H düzlemi)'; $vm.NewVerSubGroup = 'Anten örüntü'; $vm.NewVerFacility = 'Döner tabla'; Wait
Check 'yeni altyapı: yazı kutusu açık' ($vm.FacilityPick.IsNew -and $vm.FacilityPick.NewText -eq 'Döner tabla')
# Pencere ayrı bir Popup'ta: çocuğunu pencerenin üstüne çizerek görüntü alınır.
$dlg = $null; $st = New-Object System.Collections.Stack; $st.Push($win)
while ($st.Count) { $e = $st.Pop(); if ($e -is [DesignKit.Controls.ContentDialog] -and $e.IsOpen) { $dlg = $e }; for ($i = 0; $i -lt [System.Windows.Media.VisualTreeHelper]::GetChildrenCount($e); $i++) { $st.Push([System.Windows.Media.VisualTreeHelper]::GetChild($e, $i)) } }
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
