param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
. (Join-Path $PSScriptRoot '..\_Ortak.ps1')
# Uçtan uca: Excel'den gereksinim al → planları elle kur (çoka çok, bağsız) → raporları elle kur (Geçti / Kaldı / sonuçsuz,
# birden çok plan, bağsız) → durumlar, kapsama → planı ve raporu Excel'e ver, geri okuyup denetle → Excel'i yeniden al.
Add-Type -AssemblyName System.IO.Compression; Add-Type -AssemblyName System.IO.Compression.FileSystem
$XR = [Pusula.Core.Office.XlsxReader]
$PRL = [Pusula.Core.Testing.PlanReportList]

function New-Xlsx([string]$path, [object[]]$rows)
{
    if (Test-Path $path) { Remove-Item $path }
    $zip = [System.IO.Compression.ZipFile]::Open($path, 'Create')
    function Put([string]$name, [string]$text) { $e = $zip.CreateEntry($name); $w = New-Object IO.StreamWriter($e.Open(), (New-Object Text.UTF8Encoding($false))); $w.Write($text); $w.Close() }
    Put '[Content_Types].xml' '<?xml version="1.0" encoding="UTF-8"?><Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/><Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/></Types>'
    Put '_rels/.rels' '<?xml version="1.0" encoding="UTF-8"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/></Relationships>'
    Put 'xl/workbook.xml' '<?xml version="1.0" encoding="UTF-8"?><workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets><sheet name="G" sheetId="1" r:id="rId1"/></sheets></workbook>'
    Put 'xl/_rels/workbook.xml.rels' '<?xml version="1.0" encoding="UTF-8"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/></Relationships>'
    $sb = New-Object Text.StringBuilder
    [void]$sb.Append('<?xml version="1.0" encoding="UTF-8"?><worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><sheetData>')
    for ($r = 0; $r -lt $rows.Count; $r++)
    {
        [void]$sb.Append('<row r="' + ($r + 1) + '">')
        for ($c = 0; $c -lt $rows[$r].Count; $c++)
        {
            if ([string]$rows[$r][$c] -eq '') { continue }
            [void]$sb.Append('<c r="' + [string][char](65 + $c) + ($r + 1) + '" t="inlineStr"><is><t>' + [Security.SecurityElement]::Escape([string]$rows[$r][$c]) + '</t></is></c>')
        }
        [void]$sb.Append('</row>')
    }
    [void]$sb.Append('</sheetData></worksheet>')
    Put 'xl/worksheets/sheet1.xml' $sb.ToString()
    $zip.Dispose()
}

[System.Threading.SynchronizationContext]::SetSynchronizationContext((New-Object System.Windows.Threading.DispatcherSynchronizationContext))
$app = New-Object Pusula.Wpf.App; $app.InitializeComponent(); $app.ShutdownMode = 'OnExplicitShutdown'
$win = New-Object Pusula.Wpf.MainWindow
$win.WindowStartupLocation = 'Manual'; $win.Left = -20000; $win.Top = -20000; $win.ShowActivated = $false; $win.Width = 1500; $win.Height = 1300
$win.Show(); Pump
$vm = $win.DataContext; $vm.RootPath = $Data; Wait
$stock = Join-Path $Data '7000_MILKON\AB-7000-0001'
$vm.SelectNodeByPath($stock); Wait; Wait
Check 'başta madde yok' ($vm.VerificationCount -eq 0)
function RowOf([string]$tg) { @($vm.VerificationGroups | ForEach-Object { $_.Rows } | Where-Object { $_.Item.TgNo -eq $tg })[0] }

"--- 1) Excel'den gereksinim al"
$rows = @(
    @('Test Gereksinimleri', '', '', ''), @('', '', '', ''),
    @('TG no', 'Gereksinim', 'Tür', 'Not'),
    @('DGÖ-1', '3.1 Elektriksel özellikler', '', ''),
    @('DGÖ-2', 'Anten 8–12 GHz bandında 3 dB yanca hüzme genişliği ≥ 30° olmalıdır.', 'Temel', ''),
    @('DGÖ-3', 'Yan lob seviyesi ana hüzmeye göre ≤ −15 dB olmalıdır.', 'Temel', 'E ve H düzlemi'),
    @('TSD-0042', 'Besleme konnektörü 4 Nm torkla sıkılmalıdır.', 'Türetilmiş', 'Tasarımcı dokümanı'),
    @('DGÖ-4', '3.2 Fiziksel ve çevresel', '', ''),
    @('DGÖ-5', 'Antenin kütlesi 2 kg''ı geçmemelidir.', 'Temel', ''),
    @('DGÖ-6', 'Anten −40 °C ile +71 °C arasında çalışmalıdır.', 'Temel', ''),
    @('DGÖ-7', 'Ürün üzerinde kalıcı etiket bulunmalıdır.', 'Temel', 'Belge ile')
)
$tmp = Join-Path $env:TEMP ('pusula-xf-' + [guid]::NewGuid().ToString('N').Substring(0,6)); New-Item -ItemType Directory -Force $tmp | Out-Null
$xlsx = Join-Path $tmp 'gereksinim.xlsx'
New-Xlsx $xlsx $rows
$vm.PrepareImport($xlsx); Wait
Check 'onay penceresi: 6 madde, 2 başlık, 6 yeni' ($vm.IsImportOpen -and $vm.ImportText -like '6 madde, 2 başlık*6 yeni*0 var olan*') $vm.ImportText
$vm.ConfirmImport(); Wait
Check '6 gereksinim, iki bölüm grubu' ($vm.VerificationCount -eq 6 -and (@($vm.VerificationGroups | ForEach-Object Title) -join '|') -eq '3.1 Elektriksel özellikler|3.2 Fiziksel ve çevresel') (@($vm.VerificationGroups | ForEach-Object Title) -join '|')
Check 'sabit gruplar (İşlevsel …) boşken görünmez' (@($vm.VerificationFilters | Where-Object { $_.Key -eq 'İşlevsel' }).Count -eq 0)
$r3 = RowOf 'DGÖ-3'
Check 'satır: metin üstte, not altta, başlık tekrar yok' ($r3.DisplayText -like 'Yan lob*' -and $r3.Description -eq 'E ve H düzlemi' -and -not $r3.IsTextLater) ($r3.DisplayText + ' / ' + $r3.Description)
Check 'notsuz satırda alt satır boş' ((RowOf 'DGÖ-2').Description -eq '')
Check 'hepsi Bekliyor, hepsi plansız' (@($vm.RequirementsWithoutPlan).Count -eq 6)

"--- 2) Planlar (elle)"
function NewPlan([string]$tp, [string]$desc, [string]$method, [string[]]$reqs)
{
    $vm.NewPlanItem(); Wait
    $vm.PlanTpNo = $tp; $vm.PlanDescription = $desc; $vm.PlanMethod = $method
    foreach ($o in $vm.PlanRequirementOptions) { $o.IsChecked = ($reqs -contains $o.Key) }
    $okp = $vm.ConfirmVerPlan(); $vm.IsVerPlanOpen = $false; Wait
    $okp
}
Check '4.1: bir plan iki gereksinim (DGÖ-2, DGÖ-3)' (NewPlan '4.1' 'SATIMO hüzme ve yan lob' 'Test' @('DGÖ-2', 'DGÖ-3'))
Check '4.2: DGÖ-3 ikinci plan (bir gereksinim iki plan)' (NewPlan '4.2' 'Yan lob tekrar (H düzlemi)' 'Test' @('DGÖ-3'))
Check '4.3: hiçbir gereksinime bağlı değil' (NewPlan '4.3' 'Genel kurulum kontrolü' 'Muayene' @())
Check '4.4: TSD-0042' (NewPlan '4.4' 'Montaj muayenesi' 'Muayene' @('TSD-0042'))
Check 'aynı no ikinci kez eklenmez' (-not (NewPlan '4.1' 'kopya' 'Test' @('DGÖ-5')))
$vm.IsVerPlanOpen = $false; Wait
Check 'plan sayısı 4' ($vm.PlanCount -eq 4) ([string]$vm.PlanCount)
Check 'planı olmayan: DGÖ-5, DGÖ-6, DGÖ-7' ((@($vm.RequirementsWithoutPlan) -join ',') -eq 'DGÖ-5,DGÖ-6,DGÖ-7') (@($vm.RequirementsWithoutPlan) -join ',')
Check 'gereksinimsiz plan: 4.3' ((@($vm.PlansWithoutRequirement) -join ',') -eq '4.3')
Check 'DGÖ-3 iki planda, Planlandı' (((@((RowOf 'DGÖ-3').LinkedPlans | ForEach-Object Text)) -join ',') -eq '4.1,4.2' -and (RowOf 'DGÖ-3').StatusText -eq 'Planlandı')

"--- 3) Elle durumlar"
$vm.SetVerificationStatus((RowOf 'DGÖ-6'), 'Kapsam dışı'); Wait
$vm.SetVerificationStatus((RowOf 'DGÖ-7'), 'Geçti'); Wait
Check 'DGÖ-6 Kapsam dışı → planı yok uyarısından çıkar' ((RowOf 'DGÖ-6').StatusText -eq 'Kapsam dışı' -and (@($vm.RequirementsWithoutPlan) -join ',') -eq 'DGÖ-5,DGÖ-7')
Check 'DGÖ-7 elle Geçti, yanında hesap: Bekliyor' ((RowOf 'DGÖ-7').StatusText -eq 'Geçti' -and (RowOf 'DGÖ-7').AutoStatusText -eq 'hesap: Bekliyor')

"--- 4) Raporlar (elle)"
function NewReport([string]$tr, [string]$desc, [string]$result, [string[]]$plans, [string]$unit)
{
    $vm.NewReportItem(); Wait
    $vm.ReportTrNo = $tr; $vm.ReportDescription = $desc; $vm.ReportResult = $result; $vm.ReportUnit = $unit
    foreach ($o in $vm.ReportPlanOptions) { $o.IsChecked = ($plans -contains $o.Key) }
    $okr = $vm.ConfirmVerReport(); $vm.IsVerReportOpen = $false; Wait
    $okr
}
Check '5.1: 4.1 Geçti' (NewReport '5.1' 'Hüzme sonuçları' 'Geçti' @('4.1') 'SN01')
Check '5.2: 4.2 Kaldı' (NewReport '5.2' 'Yan lob H düzlemi' 'Kaldı' @('4.2') 'SN01')
Check '5.3: 4.4 sonuçsuz' (NewReport '5.3' 'Montaj' '' @('4.4') '')
Check '5.4: plansız rapor' (NewReport '5.4' 'Fazladan ölçüm' 'Geçti' @() 'SN02')
Check '5.5: iki plan (4.1, 4.4) Geçti' (NewReport '5.5' 'Tekrar ölçüm' 'Geçti' @('4.1', '4.4') 'SN02')
Check 'rapor sayısı 5' ($vm.ReportCount -eq 5)
Check 'DGÖ-2: 4.1 → 5.1 + 5.5 Geçti → Geçti' ((RowOf 'DGÖ-2').StatusText -eq 'Geçti') (RowOf 'DGÖ-2').StatusText
Check 'DGÖ-3: 4.2 raporu Kaldı → Kaldı' ((RowOf 'DGÖ-3').StatusText -eq 'Kaldı') (RowOf 'DGÖ-3').StatusText
Check 'TSD-0042: 5.3 sonuçsuz → Planlandı' ((RowOf 'TSD-0042').StatusText -eq 'Planlandı') (RowOf 'TSD-0042').StatusText
Check 'DGÖ-5 Bekliyor, DGÖ-6 Kapsam dışı, DGÖ-7 elle Geçti' ((RowOf 'DGÖ-5').StatusText -eq 'Bekliyor' -and (RowOf 'DGÖ-6').StatusText -eq 'Kapsam dışı' -and (RowOf 'DGÖ-7').StatusText -eq 'Geçti')
Check 'kapsama: raporsuz plan 4.3, plansız rapor 5.4, sonuçsuz rapor 5.3' ((@($vm.PlansWithoutReport) -join ',') -eq '4.3' -and (@($vm.ReportsWithoutPlan) -join ',') -eq '5.4' -and (@($vm.ReportsWithoutResult) -join ',') -eq '5.3') ((@($vm.PlansWithoutReport) -join ',') + ' | ' + (@($vm.ReportsWithoutPlan) -join ',') + ' | ' + (@($vm.ReportsWithoutResult) -join ','))

"--- 4b) Sonucu girilmedi ile raporu yok ayrı"
Check 'TSD-0042: raporlu ama 5.3 sonuçsuz → "sonucu girilmedi" (raporu yok değil)' ((@($vm.RequirementsWithoutResult) -join ',') -eq 'TSD-0042' -and @($vm.RequirementsWithoutReport).Count -eq 0) ((@($vm.RequirementsWithoutResult) -join ',') + ' | ' + (@($vm.RequirementsWithoutReport) -join ','))
Check 'gereksinim süzgecinde "Sonuç yok" hapı: 1' ((@($vm.ReqReportPills | ForEach-Object { $_.Text + ':' + $_.Count }) -join ',') -eq 'Geçti:1,Kaldı:1,Sonuç yok:1,Yok:2') (@($vm.ReqReportPills | ForEach-Object { $_.Text + ':' + $_.Count }) -join ',')
Check 'uyarıda "1 gereksinimin sonucu girilmedi: TSD-0042"' ($vm.VerCoverageText -like '*1 gereksinimin sonucu girilmedi: TSD-0042*') $vm.VerCoverageText
(@($vm.ReqReportPills | Where-Object State -eq 3)[0]).IsChecked = $true; Wait
Check 'hap seçilince yalnız TSD-0042' ((@($vm.VerificationGroups | ForEach-Object { $_.Rows } | ForEach-Object { $_.Item.TgNo }) -join ',') -eq 'TSD-0042')
$vm.ClearLinkFilter(); Wait
Check 'plan: raporu yok 4.3, sonucu girilmedi 4.4' ((@($vm.PlansWithoutResult) -join ',') -eq '4.3' -and (@($vm.PlansWithoutResultValue) -join ',') -eq '4.4')
$vm.IsVerPlanView = $true; Wait
Check 'plan görünümü uyarısı: sonucu girilmedi 4.4' ($vm.VerCoverageText -like '*1 plan maddesinin sonucu girilmedi: 4.4*') $vm.VerCoverageText
Check '4.4 satırı sarı şeritli' ((@($vm.PlanRows | Where-Object TpNo -eq '4.4')[0]).Stripe -eq 4)
$vm.IsVerReqView = $true; Wait

"--- 4c) Rapor ayrıntısı"
$rr = @($vm.ReportRows | Where-Object TrNo -eq '5.5')[0]
Check '5.5: planlar 4.1 + 4.4 (açıklama, yöntem)' ((@($rr.PlanDetails | ForEach-Object { $_.Key + '/' + $_.Text }) -join ',') -eq '4.1/SATIMO hüzme ve yan lob,4.4/Montaj muayenesi' -and $rr.PlanDetails[1].Method -eq 'Muayene') (@($rr.PlanDetails | ForEach-Object { $_.Key + '/' + $_.Text }) -join ',')
Check '5.5: gereksinimler DGÖ-2, DGÖ-3, TSD-0042 (metin, durum)' ((@($rr.RequirementDetails | ForEach-Object Key) -join ',') -eq 'DGÖ-2,DGÖ-3,TSD-0042' -and $rr.RequirementDetails[1].Text -like 'Yan lob*' -and $rr.RequirementDetails[1].StatusText -eq 'Kaldı')
Check '5.4: plansız → uyarı' (-not (@($vm.ReportRows | Where-Object TrNo -eq '5.4')[0]).HasPlanDetails)
$vfile = [Pusula.Core.Testing.VerificationList]::PathFor($stock)
$vt = [IO.File]::ReadAllText($vfile); $vt = $vt.TrimEnd() + "`r`n`r`n### 5.1 · Hüzme sonuçları`r`nSATIMO, 8–12 GHz, 0,5 GHz adım.`r`nEn düşük 31,2° (11 GHz).`r`n"
[IO.File]::WriteAllText($vfile, $vt, (New-Object System.Text.UTF8Encoding($true)))
[Pusula.Wpf.ViewModels.MainViewModel].GetMethod('LoadVerifications', [Reflection.BindingFlags]'NonPublic,Instance').Invoke($vm.psobject.BaseObject, $null) | Out-Null; Wait
$r51 = @($vm.ReportRows | Where-Object TrNo -eq '5.1')[0]
Check 'md''deki ### 5.1 ayrıntısı okunur' ($r51.HasDetailText -and $r51.DetailText -like 'SATIMO, 8–12 GHz*31,2°*') $r51.DetailText
Check 'rapor kaydı ayrıntıya dokunmaz' ([Pusula.Core.Testing.PlanReportList]::SaveReports($stock, [Pusula.Core.Testing.PlanReportList]::ReadReports($stock)) -eq '' -and ([IO.File]::ReadAllText($vfile)).Contains("### 5.1 · Hüzme sonuçları`r`nSATIMO, 8–12 GHz"))
$r51.IsExpanded = $true; $vm.IsVerReportView = $true; Wait
Snap $win "$Out\report-detail.png"
$vm.IsVerReqView = $true; Wait

"--- 5) Excel'e aktar"
Check 'önerilen ad ve klasör' ($vm.ExportFileName($false) -eq 'AB-7000-0001_Test_Plani.xlsx' -and $vm.ExportFileName($true) -eq 'AB-7000-0001_Test_Raporu.xlsx' -and $vm.ExportFolder -like '*AB-7000-0001*') ($vm.ExportFolder)
$planX = Join-Path $tmp 'plan.xlsx'; $repX = Join-Path $tmp 'rapor.xlsx'
Check 'plan yazılır' ($vm.ExportPlans($planX) -and (Test-Path $planX)) $vm.Status
$p = $XR::ReadFirstSheet($planX)
Check 'plan: başlık + 4 satır, 9 sütun (Standart dahil)' ($p.Count -eq 5 -and $p[0].Count -eq 9 -and $p[0][0] -eq 'TP no' -and $p[0][5] -eq 'Gereksinim metni' -and $p[0][6] -eq 'Standart') (($p[0]) -join '|')
Check '4.1: DGÖ-2, DGÖ-3; metinleri; raporlar 5.1 Geçti, 5.5 Geçti' ($p[1][0] -eq '4.1' -and $p[1][4] -eq 'DGÖ-2, DGÖ-3' -and $p[1][5] -like 'DGÖ-2: Anten*DGÖ-3: Yan lob*' -and $p[1][8] -eq '5.1 Geçti, 5.5 Geçti') (($p[1]) -join '|')
Check '4.3: gereksinim — rapor —' ($p[3][0] -eq '4.3' -and $p[3][4] -eq '—' -and $p[3][8] -eq '—') (($p[3]) -join '|')
Check '4.4: yöntem Muayene, rapor 5.3 + 5.5 Geçti' ($p[4][2] -eq 'Muayene' -and $p[4][8] -eq '5.3, 5.5 Geçti') (($p[4]) -join '|')
Check 'rapor yazılır' ($vm.ExportReports($repX)) $vm.Status
$r = $XR::ReadFirstSheet($repX)
Check 'rapor: başlık + 5 satır, 9 sütun' ($r.Count -eq 6 -and $r[0].Count -eq 9 -and $r[0][6] -eq 'Sonuç')
Check '5.5: plan 4.1, 4.4; gereksinimler DGÖ-2, DGÖ-3, TSD-0042; birim SN02; Geçti' ($r[5][2] -eq '4.1, 4.4' -and $r[5][3] -eq 'DGÖ-2, DGÖ-3, TSD-0042' -and $r[5][5] -eq 'SN02' -and $r[5][6] -eq 'Geçti') (($r[5]) -join '|')
Check '5.3 sonuç —; 5.4 plan — gereksinim —; tarih dolu' ($r[3][6] -eq '—' -and $r[4][2] -eq '—' -and $r[4][3] -eq '—' -and $r[1][4].Length -eq 10) (($r[4]) -join '|')
Check 'tekrar aktarma üzerine yazar (geçici dosya kalmaz)' ($vm.ExportPlans($planX) -and @(Get-ChildItem $tmp -Filter '*.tmp-*').Count -eq 0)
Check 'md dosyası değişmedi (aktarma yalnız okur)' ([IO.File]::ReadAllText([Pusula.Core.Testing.VerificationList]::PathFor($stock)) -like '*| 4.1 | SATIMO hüzme ve yan lob |*')

"--- 6) Excel'i yeniden al: metin değişti, yeni madde; bağ / durum korunur"
$rows[5] = @('DGÖ-3', 'Yan lob seviyesi ana hüzmeye göre ≤ −18 dB olmalıdır.', 'Temel', 'E ve H düzlemi')
$rows += ,@('DGÖ-8', 'Anten IP65 sızdırmazlık sağlamalıdır.', 'Temel', '')
New-Xlsx $xlsx $rows
$vm.PrepareImport($xlsx); Wait
Check 'özet: 7 madde, 1 yeni, 6 var olan' ($vm.ImportText -like '7 madde*1 yeni*6 var olan*') $vm.ImportText
$vm.ConfirmImport(); Wait
Check '7 gereksinim; DGÖ-3 metni yeni, hâlâ 4.1 + 4.2, Kaldı' ($vm.VerificationCount -eq 7 -and (RowOf 'DGÖ-3').DisplayText -like '*−18 dB*' -and ((@((RowOf 'DGÖ-3').LinkedPlans | ForEach-Object Text)) -join ',') -eq '4.1,4.2' -and (RowOf 'DGÖ-3').StatusText -eq 'Kaldı')
Check 'elle durumlar korunur (DGÖ-6, DGÖ-7)' ((RowOf 'DGÖ-6').StatusText -eq 'Kapsam dışı' -and (RowOf 'DGÖ-7').StatusText -eq 'Geçti')
Check 'DGÖ-8 yeni, 3.2 grubunda, plansız' ((RowOf 'DGÖ-8').Item.Group -eq '3.2 Fiziksel ve çevresel' -and (@($vm.RequirementsWithoutPlan) -join ',') -eq 'DGÖ-5,DGÖ-7,DGÖ-8')
Check 'plan ve rapor tabloları değişmedi' (@($PRL::ReadPlans($stock)).Count -eq 4 -and @($PRL::ReadReports($stock)).Count -eq 5)

"--- 7) Bağ kutusu: açıkken düğme tıklanmaz (ikinci tık yeniden açmaz)"
$vm.IsVerReqView = $true; Wait
$st = New-Object System.Collections.Stack; $st.Push($win); $lb = $null
while ($st.Count) { $e = $st.Pop(); if ($e -is [System.Windows.Controls.Primitives.ToggleButton] -and $e.Name -eq 'LinkButton' -and $e.IsVisible) { $lb = $e; break }; for ($i = 0; $i -lt [System.Windows.Media.VisualTreeHelper]::GetChildrenCount($e); $i++) { $st.Push([System.Windows.Media.VisualTreeHelper]::GetChild($e, $i)) } }
Check 'Bağ düğmesi bulundu, kapalıyken tıklanır' ($lb -and $lb.IsHitTestVisible)
$lb.IsChecked = $true; Wait
Check 'açıkken tıklanmaz' (-not $lb.IsHitTestVisible)
$lb.IsChecked = $false; Wait
Check 'kapanınca yine tıklanır' ($lb.IsHitTestVisible)
"--- 8) Bağ seçimi kutusu (plan penceresi: gereksinimler)"
$vm.EditPlanItem(@($PRL::ReadPlans($stock) | Where-Object TpNo -eq '4.1')[0]); Wait
$opts = @($vm.PlanRequirementOptions)
function Pickable() { @($vm.PlanPickView | ForEach-Object { $_.Key }) }
Check 'bağlı 2: sayaç "2 gereksinim", uyarı yok' ($vm.PlanCountText -eq '2 gereksinim' -and $vm.HasPlanSelection) $vm.PlanCountText
Check 'ekleme listesi: bağlı olmayan 5 madde' ((Pickable).Count -eq 5 -and (Pickable) -notcontains 'DGÖ-2') ((Pickable) -join ',')
Check 'ekleme listesi bölümlere ayrılı' ((@($vm.PlanPickView.Groups | ForEach-Object Name) -join '|') -eq '3.1 Elektriksel özellikler|3.2 Fiziksel ve çevresel') (@($vm.PlanPickView.Groups | ForEach-Object Name) -join '|')
$o5 = @($opts | Where-Object Key -eq 'DGÖ-5')[0]; $o3 = @($opts | Where-Object Key -eq 'DGÖ-3')[0]
Check 'başlıksız madde: gereksinim metni görünür, tam metin ipucunda' ($o5.Text -like 'Antenin kütlesi*' -and $o5.Detail -like 'Antenin kütlesi*')
Check 'DGÖ-3 bilgi: diğer planı 4.2 (mor)' ($o3.Badge -eq '4.2' -and -not $o3.IsWarning) $o3.Badge
Check 'DGÖ-5 bilgi: plan yok (sarı)' ($o5.Badge -eq 'plan yok' -and $o5.IsWarning)
$vm.PlanSearch = 'kütle'; Wait
Check 'arama "kütle": yalnız DGÖ-5' (((Pickable) -join ',') -eq 'DGÖ-5') ((Pickable) -join ',')
$vm.PlanSearch = 'IP65'; Wait
Check 'arama metinde: "IP65" → DGÖ-8' (((Pickable) -join ',') -eq 'DGÖ-8')
$vm.PlanSearch = 'yok-böyle-bir-şey'; Wait
Check 'eşleşme yoksa liste boş' ((Pickable).Count -eq 0)
$vm.PlanSearch = ''; Wait
$o5.IsChecked = $true; Wait
Check 'DGÖ-5 eklendi: sayaç 3, listeden düştü' ($vm.PlanCountText -eq '3 gereksinim' -and (Pickable) -notcontains 'DGÖ-5' -and (Pickable).Count -eq 4)
(@($opts | Where-Object Key -eq 'DGÖ-2')[0]).IsChecked = $false; Wait
Check 'DGÖ-2 kaldırıldı: listeye döndü' ($vm.PlanCountText -eq '2 gereksinim' -and (Pickable) -contains 'DGÖ-2')
foreach ($o in $opts) { $o.IsChecked = $false }; Wait
Check 'hepsi kaldırılınca "Gereksinim yok" ve uyarı' ($vm.PlanCountText -eq 'Gereksinim yok' -and -not $vm.HasPlanSelection -and (Pickable).Count -eq 7)
$vm.IsVerPlanOpen = $false; Wait
Check 'Vazgeç: 4.1 bağları değişmedi' ((@($PRL::ReadPlans($stock) | Where-Object TpNo -eq '4.1')[0].Requirements -join ',') -eq 'DGÖ-2,DGÖ-3')
$vm.EditPlanItem(@($PRL::ReadPlans($stock) | Where-Object TpNo -eq '4.1')[0]); Wait
(@($vm.PlanRequirementOptions | Where-Object Key -eq 'DGÖ-5')[0]).IsChecked = $true
Check 'kaydedince DGÖ-5 4.1''e bağlanır' ($vm.ConfirmVerPlan() -and (@($PRL::ReadPlans($stock) | Where-Object TpNo -eq '4.1')[0].Requirements -join ',') -eq 'DGÖ-2,DGÖ-3,DGÖ-5'); $vm.IsVerPlanOpen = $false; Wait
Check 'DGÖ-5 artık planlı (uyarıdan çıktı)' ((@($vm.RequirementsWithoutPlan) -join ',') -eq 'DGÖ-7,DGÖ-8')
$i2 = [Pusula.Core.Testing.VerificationList]::Read($stock) | Where-Object TgNo -eq 'DGÖ-2'; $i2.Text = 'Yanca hüzme genişliği'; [Pusula.Core.Testing.VerificationList]::Update($stock, $i2) | Out-Null
[Pusula.Wpf.ViewModels.MainViewModel].GetMethod('LoadVerifications', [Reflection.BindingFlags]'NonPublic,Instance,Public').Invoke($vm.psobject.BaseObject, $null) | Out-Null; Wait
$vm.NewPlanItem(); Wait
Check 'başlık yazılınca listede başlık görünür (DGÖ-2 Yanca hüzme genişliği)' ((@($vm.PlanRequirementOptions | Where-Object Key -eq 'DGÖ-2')[0]).Text -eq 'Yanca hüzme genişliği')
Check 'yeni plan: Gereksinim yok, hepsi listede' ($vm.PlanCountText -eq 'Gereksinim yok' -and (Pickable).Count -eq 7)

"--- 9) Ekrandaki kutu (plan penceresi)"
$dlg = $null; $st = New-Object System.Collections.Stack; $st.Push($win)
while ($st.Count) { $e = $st.Pop(); if ($e -is [DesignKit.Controls.ContentDialog] -and $e.IsOpen) { $dlg = $e }; for ($i = 0; $i -lt [System.Windows.Media.VisualTreeHelper]::GetChildrenCount($e); $i++) { $st.Push([System.Windows.Media.VisualTreeHelper]::GetChild($e, $i)) } }
$child = $dlg.Template.FindName('PART_Popup', $dlg).Child
$pb = $win.FindName('PlanPickButton')
Check '"+ Gereksinim ekle" düğmesi var' ($pb -ne $null)
$pb.IsChecked = $true; Wait
Check 'açılınca kutu açık, düğme tıklanmaz' (-not $pb.IsHitTestVisible)
(@($vm.PlanRequirementOptions | Where-Object Key -eq 'DGÖ-6')[0]).IsChecked = $true; Wait
Check 'seçince kutu açık kalır' ($pb.IsChecked)
$child.Measure((New-Object System.Windows.Size 1500, 1300)); $child.Arrange((New-Object System.Windows.Rect 0, 0, 1500, 1300)); $child.UpdateLayout(); Wait
$rtb = New-Object System.Windows.Media.Imaging.RenderTargetBitmap 1500, 1300, 96, 96, ([System.Windows.Media.PixelFormats]::Pbgra32)
$dv = New-Object System.Windows.Media.DrawingVisual; $dc = $dv.RenderOpen()
$dc.DrawRectangle((New-Object System.Windows.Media.VisualBrush $win), $null, (New-Object System.Windows.Rect 0, 0, 1500, 1300))
$dc.DrawRectangle((New-Object System.Windows.Media.VisualBrush $child), $null, (New-Object System.Windows.Rect 0, 0, 1500, 1300))
$pp = $win.FindName('PlanPickPopup')
if ($pp -and $pp.Child) { $pt = $win.FindName('PlanPickBox').TranslatePoint((New-Object System.Windows.Point 0, ($win.FindName('PlanPickBox').ActualHeight + 4)), $child); $bx = $win.FindName('PlanPickBox'); $pp.Child.Measure((New-Object System.Windows.Size $bx.ActualWidth, 600)); $pp.Child.Arrange((New-Object System.Windows.Rect 0, 0, $bx.ActualWidth, $pp.Child.DesiredSize.Height)); $pp.Child.UpdateLayout(); $dc.DrawRectangle((New-Object System.Windows.Media.VisualBrush $pp.Child), $null, (New-Object System.Windows.Rect $pt.X, $pt.Y, $pp.Child.ActualWidth, $pp.Child.ActualHeight)) }
$dc.Close(); $rtb.Render($dv)
$enc = New-Object System.Windows.Media.Imaging.PngBitmapEncoder; $enc.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($rtb)); $fs = [IO.File]::Create("$Out\link-picker.png"); $enc.Save($fs); $fs.Close()
Check 'ekleme kutusu kutuyla aynı genişlikte' ([Math]::Abs($pp.Width - $win.FindName('PlanPickBox').ActualWidth) -lt 1) ([string]$pp.Width)
Check 'ekleme kutusunda satırlar çizildi' ($pp -and $pp.Child -and $pp.Child.DesiredSize.Height -gt 60) ([string]$pp.Child.DesiredSize.Height)
$pb.IsChecked = $false; $vm.IsVerPlanOpen = $false; Wait

"--- 10) Rapor penceresi: planlar"
$vm.EditReportItem(@($PRL::ReadReports($stock) | Where-Object TrNo -eq '5.5')[0]); Wait
Check 'rapor 5.5: "2 plan", listede kalan 2 plan, bölümsüz' ($vm.ReportCountText -eq '2 plan' -and @($vm.ReportPickView | ForEach-Object Key).Count -eq 2 -and $vm.ReportPickView.Groups -eq $null)
$r43 = @($vm.ReportPlanOptions | Where-Object Key -eq '4.3')[0]
Check 'gereksinimsiz plan 4.3 sarı uyarılı' ($r43.IsWarning)
foreach ($o in $vm.ReportPlanOptions) { $o.IsChecked = $false }; Wait
Check 'hepsi kaldırılınca "Plan yok" ve uyarı' ($vm.ReportCountText -eq 'Plan yok' -and -not $vm.HasReportSelection)
$vm.ReportSearch = '4.3'; Wait
Check 'rapor araması: 4.3' ((@($vm.ReportPickView | ForEach-Object Key) -join ',') -eq '4.3')
$vm.IsVerReportOpen = $false; Wait
Check 'Vazgeç: 5.5 planları değişmedi' ((@($PRL::ReadReports($stock) | Where-Object TrNo -eq '5.5')[0].Plans -join ',') -eq '4.1,4.4')
Snap $win "$Out\excel-flow.png"
$win.Close()
Remove-Item -Recurse -Force $tmp -ErrorAction SilentlyContinue
""
"SONUC: $ok basarili, $fail hata"
