param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
. (Join-Path $PSScriptRoot '..\_Ortak.ps1')
# Test tipi ve standart (ÇK / EMC): ortak liste (_TAKIP\Standartlar.md), gereksinim penceresi, aramalı seçim kutusu,
# satır çipi, plan çipi ve Excel'deki Standart sütunu; Excel'den gelen maddenin grubu düzenlemede korunur.
$SL = [Pusula.Core.Testing.StandardList]
$VL = [Pusula.Core.Testing.VerificationList]
$PRL = [Pusula.Core.Testing.PlanReportList]

"--- 1) Ortak liste (çekirdek)"
$tmp = Join-Path $env:TEMP ('pusula-std-' + [guid]::NewGuid().ToString('N').Substring(0,6)); New-Item -ItemType Directory -Force $tmp | Out-Null
try {
    $d = $SL::Read($tmp)
    Check 'dosya yoksa başlangıç listesi: 810F, 810G, 810G CHG1, 810H, 461G' ($d.Count -eq 5 -and (($d | ForEach-Object { $_.Name }) -join ',') -eq 'MIL-STD-810F,MIL-STD-810G,MIL-STD-810G CHG1,MIL-STD-810H,MIL-STD-461G' -and $d[1].Methods -contains '509.5 · Tuz sisi' -and $d[0].Methods -contains '509.4 · Tuz sisi' -and $d[3].Methods -contains '514.8 · Titreşim' -and $d[2].Methods.Count -eq 29 -and $d[4].Methods -contains 'RE102 · Işıma emisyonu, elektrik alan')
    Check 'okuma dosya oluşturmaz' (-not (Test-Path $SL::PathFor($tmp)))
    Check 'numara ve kısa yazım' ($SL::NumberOf('509.5 · Tuz sisi') -eq '509.5' -and $SL::NumberOf('RE102') -eq 'RE102' -and $SL::ShortText('MIL-STD-810G', '509.5 · Tuz sisi') -eq '810G 509.5' -and $SL::ShortText('RTCA DO-160G', 'Bölüm 4') -eq 'RTCA DO-160G Bölüm')
    $hdr = "# Test standartları`r`n`r`nOrtak liste."
    Check 'yeni yöntem: dosya başlangıç listesiyle oluşur, 810G altına eklenir' ($SL::Add($tmp, 'MIL-STD-810G', '509.4 · Tuz sisi (810F)', $hdr) -eq '' -and (Test-Path $SL::PathFor($tmp)))
    $txt = [IO.File]::ReadAllText($SL::PathFor($tmp))
    Check 'dosya: başlık, 810G''nin son maddesinden sonra 509.4, 461G sonra' ($txt.StartsWith('# Test standartları') -and $txt.IndexOf('- 528 ·') -lt $txt.IndexOf('- 509.4 · Tuz sisi (810F)') -and $txt.IndexOf('- 509.4') -lt $txt.IndexOf('## MIL-STD-810G CHG1')) $txt.Substring(0, [Math]::Min(400, $txt.Length))
    Check 'yeni standart sona eklenir' ($SL::Add($tmp, 'RTCA DO-160G', 'Bölüm 4 · Sıcaklık ve irtifa', $hdr) -eq '' -and ([IO.File]::ReadAllText($SL::PathFor($tmp))).TrimEnd().EndsWith("## RTCA DO-160G`r`n- Bölüm 4 · Sıcaklık ve irtifa"))
    $before = [IO.File]::ReadAllText($SL::PathFor($tmp))
    Check 'var olan eklenmez (dosya aynı kalır)' ($SL::Add($tmp, 'mil-std-810g', '509.5 · TUZ SISI', $hdr) -eq '' -and [IO.File]::ReadAllText($SL::PathFor($tmp)) -eq $before)
    $r = $SL::Read($tmp)
    Check 'geri okunur: 6 standart, 810G''de 509.4 var' ($r.Count -eq 6 -and $r[1].Methods -contains '509.4 · Tuz sisi (810F)' -and $r[5].Name -eq 'RTCA DO-160G')
    $p = $SL::Parse([string[]]@('## A', '* x', '- y', '## B', '- z', '## A', '- w', 'serbest yazı'))
    Check 'ayrıştırma: * ve - maddeler, aynı başlık birleşir, serbest yazı atlanır' ($p.Count -eq 2 -and ($p[0].Methods -join ',') -eq 'x,y,w' -and ($p[1].Methods -join ',') -eq 'z')
} finally { Remove-Item -Recurse -Force $tmp -ErrorAction SilentlyContinue }

[System.Threading.SynchronizationContext]::SetSynchronizationContext((New-Object System.Windows.Threading.DispatcherSynchronizationContext))
$app = New-Object Pusula.Wpf.App; $app.InitializeComponent(); $app.ShutdownMode = 'OnExplicitShutdown'
$win = New-Object Pusula.Wpf.MainWindow
$win.WindowStartupLocation = 'Manual'; $win.Left = -20000; $win.Top = -20000; $win.ShowActivated = $false; $win.Width = 1500; $win.Height = 1200
$win.Show(); Pump
$vm = $win.DataContext; $vm.RootPath = $Data; Wait
$stock = Join-Path $Data '7000_MILKON\AB-7000-0001'
$vm.SelectNodeByPath($stock); Wait; Wait
$stdFile = $SL::PathFor($Data)
function RowOf([string]$tg) { @($vm.VerificationGroups | ForEach-Object { $_.Rows } | Where-Object { $_.Item.TgNo -eq $tg })[0] }

"--- 2) Gereksinim penceresi: ÇK + 810G 509.5, Prosedür I"
$vm.AddVerificationCommand.Execute($null); Wait
Check 'yeni pencerede tip boş, standart kısmı gizli' ($vm.NewVerTestType -eq '' -and -not $vm.IsNewVerStandard)
Check 'tip seçenekleri: İşlevsel, ÇK, EMC, Diğer' ((@($vm.VerificationTestTypes) -join ',') -eq 'İşlevsel,ÇK,EMC,Diğer')
$vm.NewVerTgNo = 'DGÖ-6'; $vm.NewVerText = 'Tuz sisi dayanımı'; $vm.NewVerRequirement = 'Anten MIL-STD-810G Yöntem 509.5''e göre tuz sisine dayanmalıdır.'
$vm.NewVerTestType = 'İşlevsel'
Check 'İşlevsel: standart kısmı gizli' (-not $vm.IsNewVerStandard)
$vm.NewVerTestType = 'ÇK'
Check 'ÇK: standart kısmı açık, standartlar listede' ($vm.IsNewVerStandard -and @($vm.StandardNames) -contains 'MIL-STD-810G' -and @($vm.StandardNames) -contains 'MIL-STD-461G')
Check 'standart seçilmeden yöntem listesi boş' ($vm.StandardMethodOptions.Count -eq 0)
$vm.NewVerStandard = 'MIL-STD-810G'
Check '810G yöntemleri (29) listede' ($vm.StandardMethodOptions.Count -eq 29 -and @($vm.StandardMethodOptions) -contains '514.6 · Titreşim') ([string]$vm.StandardMethodOptions.Count)
$vm.NewVerStandardMethod = '509.5 · Tuz sisi'; $vm.NewVerProcedure = 'I'
Check 'kaydedilir' ($vm.ConfirmNewVerification()); $vm.IsVerificationDialogOpen = $false; Wait
$it = @($VL::Read($stock) | Where-Object TgNo -eq 'DGÖ-6')[0]
Check 'md: Tip ÇK, Standart, yöntem, prosedür' ($it.TestType -eq 'ÇK' -and $it.Standard -eq 'MIL-STD-810G' -and $it.StandardMethod -eq '509.5 · Tuz sisi' -and $it.Procedure -eq 'I')
$mdText = [IO.File]::ReadAllText($VL::PathFor($stock))
Check 'md başlığında yeni sütunlar' ($mdText -like '*| Altyapı | Tip | Standart | Standart yöntemi | Prosedür | Durum | Elle durum |*' -and $mdText -like '*| ÇK | MIL-STD-810G | 509.5 · Tuz sisi | I | Bekliyor |*') $mdText
$r6 = RowOf 'DGÖ-6'
Check 'satır çipi: ÇK · 810G 509.5 · I; ipucu uzun' ($r6.TypeChipText -eq 'ÇK · 810G 509.5 · I' -and $r6.StandardLongText -eq 'MIL-STD-810G 509.5 · Tuz sisi, Prosedür I') ($r6.TypeChipText + ' | ' + $r6.StandardLongText)
Check 'listedeki yöntem seçildi: ortak dosya oluşmadı' (-not (Test-Path $stdFile))

"--- 3) EMC + listede olmayan yöntem → ortak listeye eklenir"
$vm.AddVerificationCommand.Execute($null); Wait
$vm.NewVerTgNo = 'DGÖ-7'; $vm.NewVerText = 'Işıma emisyonu'; $vm.NewVerTestType = 'EMC'; $vm.NewVerStandard = 'MIL-STD-461G'
Check '461G yöntemleri (20)' ($vm.StandardMethodOptions.Count -eq 20) ([string]$vm.StandardMethodOptions.Count)
$vm.NewVerStandardMethod = 'RE102 · Işıma emisyonu, elektrik alan'
Check 'kaydedilir' ($vm.ConfirmNewVerification()); $vm.IsVerificationDialogOpen = $false; Wait
$vm.AddVerificationCommand.Execute($null); Wait
$vm.NewVerTgNo = 'DGÖ-9'; $vm.NewVerText = 'Özel EMC'; $vm.NewVerTestType = 'EMC'; $vm.NewVerStandard = 'MIL-STD-461G'; $vm.NewVerStandardMethod = 'CS999 · Kurum içi deneme'
Check 'kaydedilir' ($vm.ConfirmNewVerification()); $vm.IsVerificationDialogOpen = $false; Wait
Check 'ortak liste oluştu, CS999 461G altında' ((Test-Path $stdFile) -and ([IO.File]::ReadAllText($stdFile)) -like '*## MIL-STD-461G*- CS999 · Kurum içi deneme*')
$vm.AddVerificationCommand.Execute($null); Wait
$vm.NewVerStandard = 'MIL-STD-461G'
Check 'sonraki pencerede CS999 listede' (@($vm.StandardMethodOptions) -contains 'CS999 · Kurum içi deneme')
$vm.NewVerStandard = 'RTCA DO-160G'
Check 'listede olmayan standart: yöntem listesi boş' ($vm.StandardMethodOptions.Count -eq 0)
$vm.IsVerificationDialogOpen = $false; Wait

"--- 4) Düzenleme: alanlar dolar; İşlevsel'e geçince standart korunur mu"
$vm.EditVerification((RowOf 'DGÖ-6')); Wait
Check 'düzenleme penceresi dolu' ($vm.NewVerTestType -eq 'ÇK' -and $vm.NewVerStandard -eq 'MIL-STD-810G' -and $vm.NewVerStandardMethod -eq '509.5 · Tuz sisi' -and $vm.NewVerProcedure -eq 'I' -and $vm.IsNewVerStandard)
$vm.NewVerStandard = ''; $vm.NewVerTestType = 'İşlevsel'
Check 'standart silinip İşlevsel seçilince kısım gizlenir' (-not $vm.IsNewVerStandard)
Check 'kaydedilir' ($vm.ConfirmNewVerification()); $vm.IsVerificationDialogOpen = $false; Wait
$it = @($VL::Read($stock) | Where-Object TgNo -eq 'DGÖ-6')[0]
Check 'md: İşlevsel, standart / yöntem / prosedür boş' ($it.TestType -eq 'İşlevsel' -and $it.Standard -eq '' -and $it.StandardMethod -eq '' -and $it.Procedure -eq '')
Check 'çip yalnız tip: İşlevsel' ((RowOf 'DGÖ-6').TypeChipText -eq 'İşlevsel')
$vm.EditVerification((RowOf 'DGÖ-6')); Wait
$vm.NewVerTestType = 'ÇK'; $vm.NewVerStandard = 'MIL-STD-810G'; $vm.NewVerStandardMethod = '509.5 · Tuz sisi'; $vm.NewVerProcedure = 'I'
Check 'geri alınır' ($vm.ConfirmNewVerification()); $vm.IsVerificationDialogOpen = $false; Wait

"--- 5) Excel'den gelen maddenin grubu düzenlemede korunur"
$i8 = New-Object Pusula.Core.Testing.VerificationItem; $i8.TgNo = 'DGÖ-8'; $i8.Requirement = 'Anten IP65 sızdırmazlık sağlamalıdır.'; $i8.Group = '3.2 Fiziksel ve çevresel'
$VL::Add($stock, $i8, '') | Out-Null
[Pusula.Wpf.ViewModels.MainViewModel].GetMethod('LoadVerifications', [Reflection.BindingFlags]'NonPublic,Instance').Invoke($vm.psobject.BaseObject, $null) | Out-Null; Wait
$vm.EditVerification((RowOf 'DGÖ-8')); Wait
Check 'grup seçeneklerinde bölüm var ve seçili' ((@($vm.VerificationGroupChoices | Where-Object { $_.IsChecked } | ForEach-Object Key) -join ',') -eq '3.2 Fiziksel ve çevresel')
$vm.NewVerText = 'Sızdırmazlık'; $vm.NewVerTestType = 'ÇK'; $vm.NewVerStandard = 'MIL-STD-810G'; $vm.NewVerStandardMethod = '512.5 · Suya daldırma'
Check 'kaydedilir' ($vm.ConfirmNewVerification()); $vm.IsVerificationDialogOpen = $false; Wait
$it8 = @($VL::Read($stock) | Where-Object TgNo -eq 'DGÖ-8')[0]
Check 'grup hâlâ bölüm, başlık ve standart yazıldı' ($it8.Group -eq '3.2 Fiziksel ve çevresel' -and $it8.Text -eq 'Sızdırmazlık' -and $it8.StandardMethod -eq '512.5 · Suya daldırma') $it8.Group

"--- 6) Plan: standart gereksinimden gelir; Excel'de Standart sütunu"
$vm.NewPlanItem(); Wait
$vm.PlanTpNo = '4.6'; $vm.PlanDescription = 'Tuz sisi ve daldırma'; $vm.PlanMethod = 'Test'
foreach ($o in $vm.PlanRequirementOptions) { $o.IsChecked = (@('DGÖ-6', 'DGÖ-8') -contains $o.Key) }
Check 'plan kaydedilir' ($vm.ConfirmVerPlan()); $vm.IsVerPlanOpen = $false; Wait
$p46 = @($vm.PlanRows | Where-Object TpNo -eq '4.6')[0]
Check 'plan çipi: 810G 509.5 · I, 810G 512.5' ($p46.HasStandard -and $p46.StandardText -eq '810G 509.5 · I, 810G 512.5') $p46.StandardText
$xl = Join-Path $env:TEMP ('pusula-std-' + [guid]::NewGuid().ToString('N').Substring(0,6) + '.xlsx')
Check 'Excel''e aktarılır' ($vm.ExportPlans($xl))
$rows = [Pusula.Core.Office.XlsxReader]::ReadFirstSheet($xl)
$row46 = @($rows | Where-Object { $_[0] -eq '4.6' })[0]
Check 'Excel Standart sütunu: iki satır uzun yazım' ($rows[0][6] -eq 'Standart' -and $row46[6] -eq ("MIL-STD-810G 509.5 · Tuz sisi, Prosedür I`nMIL-STD-810G 512.5 · Suya daldırma")) ($row46[6])
Remove-Item $xl -ErrorAction SilentlyContinue

"--- 7) Aramalı seçim kutusu (ekranda)"
$vm.EditVerification((RowOf 'DGÖ-7')); Wait
$mp = $win.FindName('StandardMethodPick'); $tp = $win.FindName('TestTypePick'); $sp = $win.FindName('StandardPick')
Check 'kutular bulundu' ($mp -and $tp -and $sp)
Check 'düğmede seçili değer' ($mp.SelectedValue -eq 'RE102 · Işıma emisyonu, elektrik alan')
$mp.IsDropOpen = $true; Wait
Check 'açılınca tüm yöntemler (21)' ($mp.ShownItems.Count -eq 21) ([string]$mp.ShownItems.Count)
$mp.SearchText = 'rs10'; Wait
Check 'arama "rs10": RS101, RS103, RS105' (((@($mp.ShownItems | ForEach-Object { $_.Substring(0, 5) })) -join ',') -eq 'RS101,RS103,RS105') ($mp.ShownItems -join ' | ')
$mp.SearchText = 'elektrik alan'; Wait
Check 'arama ad içinde: RE102, RS103' ($mp.ShownItems.Count -eq 2)
$mp.PickFirst(); Wait
Check 'Enter ilkini seçer, kutu kapanır' ($vm.NewVerStandardMethod -eq 'RE102 · Işıma emisyonu, elektrik alan' -and -not $mp.IsDropOpen)
$mp.IsDropOpen = $true; $mp.SearchText = 'CS200 · Yeni deneme'; Wait
Check 'eşleşme yok: liste boş' ($mp.ShownItems.Count -eq 0)
$mp.PickFirst(); Wait
Check 'listede olmayan yazı seçilir' ($vm.NewVerStandardMethod -eq 'CS200 · Yeni deneme')
$tp.IsDropOpen = $true; $tp.SearchText = 'olmayan tip'; Wait
$tp.PickFirst(); Wait
Check 'tip kutusu yeni değer kabul etmez' ($vm.NewVerTestType -eq 'EMC')
$tp.SearchText = 'ç'; Wait
Check 'tip araması "ç": ÇK' (($tp.ShownItems -join ',') -eq 'ÇK')
$tp.IsDropOpen = $false; Wait
$vm.IsVerificationDialogOpen = $false; Wait
Check 'Vazgeç: md değişmedi' ((@($VL::Read($stock) | Where-Object TgNo -eq 'DGÖ-7')[0]).StandardMethod -eq 'RE102 · Işıma emisyonu, elektrik alan')

"--- 8) Plan penceresinde altyapı aramalı kutusu"
$vm.NewPlanItem(); Wait
$fb = $win.FindName('PlanFacilityBox')
Check 'altyapı kutusu var, boş' ($fb -and $fb.SelectedValue -eq '' -and $vm.PlanFacilityPick.Value -eq '')
$fb.SelectedValue = 'Yankısız oda'; Wait
Check 'yazılan yeni altyapı değere geçer' ($vm.PlanFacilityPick.Value -eq 'Yankısız oda')
$vm.PlanTpNo = '4.7'; $vm.PlanDescription = 'Örüntü'
Check 'kaydedilir, ortak altyapı listesine eklenir' ($vm.ConfirmVerPlan() -and ([IO.File]::ReadAllText([Pusula.Core.Testing.FacilityList]::PathFor($Data))) -like '*- Yankısız oda*'); $vm.IsVerPlanOpen = $false; Wait
$vm.EditPlanItem(@($PRL::ReadPlans($stock) | Where-Object TpNo -eq '4.7')[0]); Wait
Check 'düzenlemede kutu dolu, ad listede' ($fb.SelectedValue -eq 'Yankısız oda' -and @($vm.PlanFacilityPick.Names) -contains 'Yankısız oda')
$vm.IsVerPlanOpen = $false; Wait
$vm.IsVerReqView = $true; Wait
Snap $win "$Out\standard.png"
$win.Close()
""
"SONUC: $ok basarili, $fail hata"
