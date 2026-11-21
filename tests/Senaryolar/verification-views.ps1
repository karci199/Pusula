param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
. (Join-Path $PSScriptRoot '..\_Ortak.ps1')
$VL = [Pusula.Core.Testing.VerificationList]
$PR = [Pusula.Core.Testing.PlanReportList]

"--- Örnek: eski düzende (satırda TP / TR) 6 gereksinim"
$stock = Join-Path $Data '7000_MILKON\AB-7000-0002'
$template = [string]::Format([Pusula.Wpf.Properties.Resources]::Verify_FileTemplate, 'AB-7000-0002')
function Add-Req($tg, $text, $desc, $group, $kind, $method, $facility, $tp, $tpd, $tr, $result, $status) {
    $i = New-Object Pusula.Core.Testing.VerificationItem
    $i.TgNo = $tg; $i.Text = $text; $i.Description = $desc; $i.Group = $group; $i.Kind = $kind; $i.Method = $method; $i.Facility = $facility
    $i.TpNo = $tp; $i.TpDescription = $tpd; $i.TrNo = $tr; $i.TrResult = $result; $i.Status = $status
    $VL::Add($stock, $i, $template) | Out-Null
}
Add-Req 'DGÖ-1' 'Yanca hüzme genişliği' '8–12 GHz, 3 dB ≥ 30°' 'İşlevsel' 'Temel' 'Test' 'SATIMO' '4.1' 'SATIMO hüzme ve yan lob' '5.1' 'Geçti' 'Geçti'
Add-Req 'DGÖ-2' 'Yan lob seviyesi' '≤ −15 dB' 'İşlevsel' 'Temel' 'Test' 'SATIMO' '4.1' '' '5.2' 'Kaldı' 'Kaldı'
Add-Req 'TSD-0042' 'Besleme konnektörü torku' '4 Nm' 'İşlevsel' 'Türetilmiş' 'Muayene' '' '4.4' 'Montaj muayenesi' '5.4' 'Geçti' 'Geçti'
Add-Req 'DGÖ-3' 'VSWR' '≤ 1,5:1' 'İşlevsel' 'Temel' 'Test' 'VNA' '4.3' 'VNA S11 ölçümü' '' '' 'Planlandı'
Add-Req 'DGÖ-4' 'Kazanç' '≥ 15 dBi' 'Özellik' 'Temel' '' '' '' '' '' '' 'Bekliyor'
Add-Req 'DGÖ-5' 'Kütle' '≤ 2 kg' 'Özellik' 'Temel' 'Muayene' '' '4.4' '' '5.4' 'Geçti' 'Geçti'

[System.Threading.SynchronizationContext]::SetSynchronizationContext((New-Object System.Windows.Threading.DispatcherSynchronizationContext))
$app = New-Object Pusula.Wpf.App; $app.InitializeComponent(); $app.ShutdownMode = 'OnExplicitShutdown'
$win = New-Object Pusula.Wpf.MainWindow
$win.WindowStartupLocation = 'Manual'; $win.Left = -20000; $win.Top = -20000; $win.ShowActivated = $false; $win.Width = 1500; $win.Height = 1400
$win.Show(); Pump
$vm = $win.DataContext; $vm.RootPath = $Data; Pump
$vm.SelectNodeByPath($stock); Wait; Wait

"--- Taşıma ve bağlar"
$plans = $PR::ReadPlans($stock); $reports = $PR::ReadReports($stock)
Check 'taşıma: 3 plan (4.1, 4.3, 4.4), 3 rapor' ($vm.PlanCount -eq 3 -and $vm.ReportCount -eq 3 -and (($plans | ForEach-Object TpNo) -join ',') -eq '4.1,4.3,4.4') (($plans | ForEach-Object { $_.TpNo + ':' + ($_.Requirements -join '+') }) -join ' ')
Check '4.1 iki gereksinimi kapsar' ((($plans | Where-Object TpNo -eq '4.1').Requirements -join ',') -eq 'DGÖ-1,DGÖ-2')
Check '4.4: TSD-0042 ve DGÖ-5' ((($plans | Where-Object TpNo -eq '4.4').Requirements -join ',') -eq 'TSD-0042,DGÖ-5')
Check 'kapsama: DGÖ-4 planda yok, 4.3 raporsuz' ((@($vm.RequirementsWithoutPlan) -join ',') -eq 'DGÖ-4' -and (@($vm.PlansWithoutReport) -join ',') -eq '4.3') ($vm.VerCoverageText)
$rows = @($vm.VerificationGroups | ForEach-Object { $_.Rows })
$r1 = $rows | Where-Object { $_.Item.TgNo -eq 'DGÖ-1' }
Check 'gereksinim satırı: DGÖ-1 planı 4.1, raporu 5.1 Geçti + 5.2 Kaldı' ((($r1.LinkedPlans | ForEach-Object Text) -join ',') -eq '4.1' -and (($r1.LinkedReports | ForEach-Object { $_.Text + '/' + $_.Kind }) -join ',') -eq '5.1/1,5.2/2') ((($r1.LinkedReports | ForEach-Object { $_.Text + '/' + $_.Kind }) -join ','))
$r4 = $rows | Where-Object { $_.Item.TgNo -eq 'DGÖ-4' }
Check 'DGÖ-4: plan yok (sarı)' ($r4.LinkedPlans.Count -eq 1 -and $r4.LinkedPlans[0].Kind -eq 4)
Check 'raporlar varsayılan kapalı' (-not $vm.ShowRequirementReports -and -not $vm.ShowReportRequirements)
$pr = @($vm.PlanRows | Where-Object TpNo -eq '4.3')[0]
Check 'plan satırı: 4.3 rapor yok' ($pr.Reports.Count -eq 1 -and $pr.Reports[0].Kind -eq 4)
$rr = @($vm.ReportRows | Where-Object TrNo -eq '5.2')[0]
Check 'rapor satırı: 5.2 Kaldı, plan 4.1, gereksinimler DGÖ-1, DGÖ-2' ($rr.ResultKind -eq 2 -and (($rr.Plans | ForEach-Object Text) -join ',') -eq '4.1' -and (($rr.Requirements | ForEach-Object Text) -join ',') -eq 'DGÖ-1,DGÖ-2')

"--- Bağ süzgeci (hap düğmeler)"
function Pill($pills, [int]$state) { @($pills | Where-Object State -eq $state)[0] }
function Counts($pills) { (@($pills | ForEach-Object { $_.Text + $_.Count }) -join ',') }
function ReqKeys { (@($vm.VerificationGroups | ForEach-Object { $_.Rows } | ForEach-Object { $_.Item.TgNo }) | Sort-Object) -join ',' }
Check 'gereksinim hap sayıları' ((Counts $vm.ReqPlanPills) -eq 'Var5,Yok1' -and (Counts $vm.ReqReportPills) -eq 'Geçti2,Kaldı2,Yok2') ((Counts $vm.ReqPlanPills) + ' | ' + (Counts $vm.ReqReportPills))
Check 'uyarı iki parça: planı yok DGÖ-4, raporu yok DGÖ-3' ($vm.CoverageParts.Count -eq 2 -and $vm.CoverageParts[0].Text -like '*DGÖ-4*' -and $vm.CoverageParts[1].Text -like '*DGÖ-3*') $vm.VerCoverageText
$vm.ApplyCoveragePart($vm.CoverageParts[0]); Wait
Check 'parça 1: yalnız DGÖ-4, parça koyu, Temizle görünür' ((ReqKeys) -eq 'DGÖ-4' -and $vm.CoverageParts[0].IsActive -and $vm.HasReqLinkFilter -and (Pill $vm.ReqPlanPills 0).IsChecked) (ReqKeys)
$vm.ApplyCoveragePart($vm.CoverageParts[1]); Wait
Check 'parça 2: planı var + raporu yok → DGÖ-3' ((ReqKeys) -eq 'DGÖ-3' -and $vm.CoverageParts[1].IsActive -and -not $vm.CoverageParts[0].IsActive) (ReqKeys)
$vm.ClearLinkFilter(); Wait
Check 'temizle: 6 madde' (@($vm.VerificationGroups | ForEach-Object { $_.Rows }).Count -eq 6 -and -not $vm.HasReqLinkFilter)
(Pill $vm.ReqPlanPills 1).IsChecked = $true; (Pill $vm.ReqReportPills 1).IsChecked = $true; Wait
Check 'Var + Geçti = tamam olanlar: TSD-0042, DGÖ-5' ((ReqKeys) -eq 'DGÖ-5,TSD-0042') (ReqKeys)
(Pill $vm.ReqReportPills 2).IsChecked = $true; Wait
Check 'aynı grupta iki seçim = ya o ya bu (Geçti ya da Kaldı)' ((ReqKeys) -eq 'DGÖ-1,DGÖ-2,DGÖ-5,TSD-0042') (ReqKeys)
$noPlan = @($vm.VerificationGroups | ForEach-Object { $_.Rows })
$vm.ClearLinkFilter(); Wait
Check 'planı yok satırı işaretli' ((@($vm.VerificationGroups | ForEach-Object { $_.Rows } | Where-Object HasNoPlan | ForEach-Object { $_.Item.TgNo }) -join ',') -eq 'DGÖ-4')
Check 'raporlar kapalıyken Test raporu sütunu sıfır genişlik' ($vm.VerificationColumns.Report.Value -eq 0)
$vm.ShowRequirementReports = $true; Wait
Check 'açınca genişlik geri gelir' ($vm.VerificationColumns.Report.Value -gt 0)
$vm.ShowRequirementReports = $false; Wait
$vm.IsVerPlanView = $true; Wait
Check 'plan hap sayıları' ((Counts $vm.PlanReqPills) -eq 'Var3,Yok0' -and (Counts $vm.PlanReportPills) -eq 'Geçti1,Kaldı1,Yok1') ((Counts $vm.PlanReqPills) + ' | ' + (Counts $vm.PlanReportPills))
(Pill $vm.PlanReportPills 0).IsChecked = $true; Wait
Check 'plan: raporu yok → 4.3, 1 / 3' ((@($vm.PlanRows | ForEach-Object TpNo) -join ',') -eq '4.3' -and $vm.PlanShownText -like '1*3*') $vm.PlanShownText
Check 'şerit: 4.3 sarı, 4.1 kırmızı' (@($vm.PlanRows)[0].Stripe -eq 4 -and (@([Pusula.Wpf.ViewModels.MainViewModel].GetField('allPlanRows', [Reflection.BindingFlags]'NonPublic,Instance').GetValue($vm.psobject.BaseObject)) | Where-Object TpNo -eq '4.1').Stripe -eq 2)
$vm.ClearLinkFilter(); $vm.PlanListSearch = 'montaj'; Wait
Check 'plan araması: montaj → 4.4' ((@($vm.PlanRows | ForEach-Object TpNo) -join ',') -eq '4.4')
$vm.PlanListSearch = ''; Wait
$vm.IsVerReportView = $true; Wait
Check 'rapor hap sayıları' ((Counts $vm.ReportPlanPills) -eq 'Var3,Yok0' -and (Counts $vm.ReportResultPills) -eq 'Geçti2,Kaldı1,Yok0') ((Counts $vm.ReportPlanPills) + ' | ' + (Counts $vm.ReportResultPills))
(Pill $vm.ReportResultPills 2).IsChecked = $true; Wait
Check 'rapor: Kaldı → 5.2, şerit kırmızı' ((@($vm.ReportRows | ForEach-Object TrNo) -join ',') -eq '5.2' -and @($vm.ReportRows)[0].Stripe -eq 2)
$vm.ClearLinkFilter(); Wait
Check 'sekme değişince seçim sekmeye özel kalır' (@($vm.ReportRows).Count -eq 3)
$cols = $vm.VerificationColumns; $w0 = $cols.PlanTp.Value; $cols.Resize('ptp', 30)
Check 'plan sütunu genişler' ($cols.PlanTp.Value -eq $w0 + 30 -and [Pusula.Wpf.ViewModels.VerificationColumns]::IsLeading('ptp') -and -not [Pusula.Wpf.ViewModels.VerificationColumns]::IsLeading('pmethod'))
$cols.Reset()
$vm.IsVerReqView = $true; Wait
"--- Görünümler (PNG)"
function Find-TabItem($node, $label) { if ($node -eq $null) { return $null }; if ($node -is [System.Windows.Controls.TabItem] -and $node.Header -is [System.Windows.Controls.StackPanel] -and $node.Header.Children[0].Text -like "$label*") { return $node }; for ($i = 0; $i -lt [System.Windows.Media.VisualTreeHelper]::GetChildrenCount($node); $i++) { $r = Find-TabItem ([System.Windows.Media.VisualTreeHelper]::GetChild($node, $i)) $label; if ($r) { return $r } }; return $null }
$vm.ApplyCoveragePart($vm.CoverageParts[0]); Wait
Snap $win "$Out\ver-view-requirements.png"
$vm.ClearLinkFilter(); Wait
$vm.IsVerPlanView = $true; Wait
Check 'plan görünümü seçili' ($vm.IsVerPlanView -and -not $vm.IsVerReqView -and $vm.VerCoverageText -like '*4.3*')
(Pill $vm.PlanReportPills 0).IsChecked = $true; Wait
Snap $win "$Out\ver-view-plan.png"
$vm.ClearLinkFilter(); Wait
$vm.IsVerReportView = $true; $vm.ShowReportRequirements = $true; Wait
Snap $win "$Out\ver-view-report.png"
$vm.IsVerReqView = $true; Wait
$win.Close()
""
"SONUC: $ok basarili, $fail hata"
