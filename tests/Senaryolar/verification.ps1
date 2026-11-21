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
Check 'dosyada 19 sütunlu başlık' ($text -like '*| # | TG no | Kaynak | Başlık | Kısa not | Gereksinim | Grup | Alt grup | Tür | Bağlı | Yöntem | Altyapı | TP no | TP açıklama | TP not | TR no | TR açıklama | TR not | Durum |*') $text
Check 'dosyada satır' ($text -like '*| 1 | TG-012 |  | Kazanç ≥ 5 dBi @ 2,4 GHz |  |  | İşlevsel | Anten örüntü |  |  |  | SATIMO |  |  |  |  |  |  | Bekliyor |*') $text
Check 'dosyada Plan ve Sonuç kısımları' ($text -like '*## Plan*' -and $text -like '*## Sonuç*')
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
Check 'yalnız durum hücresi değişti' ($text -like '*| 1 | TG-012 |  | Kazanç ≥ 5 dBi @ 2,4 GHz |  |  | İşlevsel | Anten örüntü |  |  |  | SATIMO |  |  |  |  |  |  | Yapılıyor |*' -and $text -like '*| 2 |  |  | RE102 \| CE102 sınır içinde |  |  | ÇK/EMC |*') $text
$row = ($vm.VerificationGroups | Where-Object { $_.Title -eq 'İşlevsel' }).Rows[0]
Check 'yapılıyor türü' ($row.StatusKind -eq 3 -and $row.StatusText -eq 'Yapılıyor')

"--- düzenleme"
$vm.EditVerification($row); Wait
Check 'düzenleme penceresi dolu' ($vm.IsVerificationDialogOpen -and $vm.IsEditingVerification -and $vm.NewVerText -eq 'Kazanç ≥ 5 dBi @ 2,4 GHz' -and $vm.FacilityPick.Selected -eq 'SATIMO' -and $vm.NewVerSubGroup -eq 'Anten örüntü' -and $vm.NewVerStatus -eq 'Yapılıyor') $vm.VerificationDialogTitle
$vm.NewVerText = 'Kazanç ≥ 6 dBi @ 2,4 GHz'; $vm.NewVerDescription = '≥ 6 dBi ana yönde'; $vm.NewVerRequirement = 'Anten 2,4 GHz''de ana yönde en az 6 dBi kazanç sağlamalıdır.'; $vm.NewVerKind = 'Temel'; $vm.NewVerMethod = 'Test'; $vm.NewVerTgNo = 'TG-013'; $vm.NewVerTpNo = 'TP-004'; $vm.NewVerTrNo = 'TR-009'
Check 'kaydedilir' ($vm.ConfirmNewVerification())
$vm.IsVerificationDialogOpen = $false; Wait
$text = [IO.File]::ReadAllText($file)
Check 'satır yerinde değişti, numara aynı' ($text -like '*| 1 | TG-013 |  | Kazanç ≥ 6 dBi @ 2,4 GHz | ≥ 6 dBi ana yönde | Anten 2,4 GHz''de ana yönde en az 6 dBi kazanç sağlamalıdır. | İşlevsel | Anten örüntü | Temel |  | Test | SATIMO | TP-004 |  |  | TR-009 |  |  | Yapılıyor |*' -and $text -notlike '*5 dBi*' -and $vm.VerificationCount -eq 2) $text

"--- plan / rapor açıklama ve not sütunları (madde penceresinde yok, düzenlemede korunur)"
$items = [Pusula.Core.Testing.VerificationList]::Read($stock)
$it = @($items | Where-Object { $_.TgNo -eq 'TG-013' })[0]
$it.TpDescription = 'SATIMO kazanç ölçümü'; $it.TpNote = 'Jig JT-1000-0009'; $it.TrDescription = 'Kazanç sonuçları'; $it.TrNote = 'BN-2026-014'
Check 'yazılır' ([Pusula.Core.Testing.VerificationList]::Update($stock, $it) -eq '')
$text = [IO.File]::ReadAllText($file)
Check 'dosyada yeni hücreler' ($text -like '*| SATIMO | TP-004 | SATIMO kazanç ölçümü | Jig JT-1000-0009 | TR-009 | Kazanç sonuçları | BN-2026-014 | Yapılıyor |*') $text
$back = @([Pusula.Core.Testing.VerificationList]::Read($stock) | Where-Object { $_.TgNo -eq 'TG-013' })[0]
Check 'geri okunur' ($back.TpDescription -eq 'SATIMO kazanç ölçümü' -and $back.TrNote -eq 'BN-2026-014')
$row1 = ($vm.VerificationGroups | Where-Object { $_.Title -eq 'İşlevsel' }).Rows[0]
$vm.EditVerification($row1); Wait
$vm.NewVerDescription = '≥ 6 dBi ana yönde (güncel)'
Check 'pencereden kaydedilir' ($vm.ConfirmNewVerification())
$vm.IsVerificationDialogOpen = $false; Wait
$text = [IO.File]::ReadAllText($file)
Check 'düzenlemede plan / rapor açıklama ve not korunur' ($text -like '*(güncel)*| SATIMO | TP-004 | SATIMO kazanç ölçümü | Jig JT-1000-0009 | TR-009 | Kazanç sonuçları | BN-2026-014 | Yapılıyor |*') $text
$row1 = ($vm.VerificationGroups | Where-Object { $_.Title -eq 'İşlevsel' }).Rows[0]
Check 'satır: TG no, kısa not, tür, yöntem, plan / rapor' ($row1.Id -eq 'TG-013' -and $row1.Description -like '≥ 6 dBi ana yönde*' -and $row1.KindText -eq 'Temel' -and $row1.MethodTone -eq 1 -and $row1.TpText -eq 'TP-004' -and $row1.TrText -eq 'TR-009') ($row1.Id + ' ' + $row1.KindText)
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
Check 'yeni madde penceresi düzenleme kipinde değil' (-not $vm.IsEditingVerification -and $vm.NewVerText -eq '' -and $vm.NewVerTpNo -eq '')
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
function Find-Open($node) { if ($node -eq $null) { return $null }; if ($node.GetType().Name -eq 'ContentDialog' -and $node.IsOpen) { return $node }; for ($i = 0; $i -lt [System.Windows.Media.VisualTreeHelper]::GetChildrenCount($node); $i++) { $r = Find-Open ([System.Windows.Media.VisualTreeHelper]::GetChild($node, $i)); if ($r) { return $r } }; return $null }
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
"--- test planı kartı"
function RowOf([string]$sub) { @($vm.VerificationGroups | ForEach-Object { $_.Rows } | Where-Object { $_.SubGroup -eq $sub -or $_.Text -eq $sub })[0] }
$s11 = RowOf 'S11'
$vm.SetVerificationStatus($s11, 'Bekliyor'); Wait
$s11 = RowOf 'S11'
Check 'plan yok: kartta Test planı ekle' (-not $s11.HasPlan)
$vm.OpenVerPlan($s11); Wait
Check 'plan penceresi açılır, başlık' ($vm.IsVerPlanOpen -and $vm.VerPlanTitle -like 'Test planı · *') $vm.VerPlanTitle
$vm.PlanTpNo = '4.1'; $vm.PlanDescription = 'VNA S11 ölçümü'; $vm.PlanMethod = 'Test'; $vm.PlanFacilityPick.Value = 'VNA'; $vm.PlanNote = 'Kalibrasyon kiti 3,5 mm'
for ($k = 0; $k -lt 20; $k++) { Pump; Start-Sleep -Milliseconds 30 }
$dlg = Find-Open $win
if ($dlg) {
    $pop = Find-Popup $dlg; if ($pop -and $pop.Child) { $dlg = $pop.Child; $sz = New-Object System.Windows.Size($win.ActualWidth, $win.ActualHeight); $dlg.Measure($sz); $dlg.Arrange((New-Object System.Windows.Rect($sz))); $dlg.UpdateLayout(); for ($k = 0; $k -lt 20; $k++) { Pump; Start-Sleep -Milliseconds 30 } }
    $bmp = New-Object System.Windows.Media.Imaging.RenderTargetBitmap([int]$dlg.ActualWidth, [int]$dlg.ActualHeight, 96, 96, [System.Windows.Media.PixelFormats]::Pbgra32)
    $bmp.Render($dlg); $enc = New-Object System.Windows.Media.Imaging.PngBitmapEncoder; $enc.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($bmp))
    $fs = [IO.File]::Create("$Out\verification-plan.png"); $enc.Save($fs); $fs.Close()
}
Check 'kaydedilir' ($vm.ConfirmVerPlan())
$vm.IsVerPlanOpen = $false; Wait
$text = [IO.File]::ReadAllText($file)
$s11 = RowOf 'S11'
Check 'satır: plan yazıldı, Bekliyor → Planlandı, açık' ($s11.HasPlan -and $s11.TpText -eq '4.1' -and $s11.PlanDescription -eq 'VNA S11 ölçümü' -and $s11.Method -eq 'Test' -and $s11.Facility -eq 'VNA' -and $s11.StatusText -eq 'Planlandı' -and $s11.IsExpanded) ($s11.StatusText)
Check 'dosyada plan hücreleri' ($text -like '*| Test | VNA | 4.1 | VNA S11 ölçümü | Kalibrasyon kiti 3,5 mm |*| Planlandı |*') $text
$yan = RowOf 'Yan lob seviyesi'
$vm.OpenVerPlan($yan); $vm.PlanTpNo = '4.2'
Check 'ileri durum değişmez (Değerlendiriliyor)' ($vm.ConfirmVerPlan() -and (RowOf 'Yan lob seviyesi').StatusText -eq 'Değerlendiriliyor')
$vm.IsVerPlanOpen = $false; Wait
$vm.SetVerificationStatus((RowOf 'S11'), 'Yapılıyor'); Wait
Check 'durum değişince plan açıklaması silinmez' ([IO.File]::ReadAllText($file) -like '*| 4.1 | VNA S11 ölçümü | Kalibrasyon kiti 3,5 mm |*| Yapılıyor |*')
$s11 = RowOf 'S11'; $s11.IsExpanded = $true; Wait
Snap $win "$Out\verification-plan-card.png"
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
Check 'eski 7 sütunlu tablo 19 sütuna yükseldi, numara 8 sonda' ($after -like "*| # | TG no | Kaynak | Başlık | Kısa not | Gereksinim | Grup | Alt grup | Tür | Bağlı | Yöntem | Altyapı | TP no | TP açıklama | TP not | TR no | TR açıklama | TR not | Durum |`n|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|`n| 7 |  |  | Ağırlık ≤ 1,2 kg |  |  | Özellik |  |  |  |  |  |  |  |  |  |  |  | Geçti |`n| 8 |  |  | Boyut 100x50 mm |  |  | İşlevsel |  |  |  |  |  |  |  |  |  |  |  | Bekliyor |`n`nSon not.*") $after
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