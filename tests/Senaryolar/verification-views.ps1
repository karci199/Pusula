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

"--- Görünümler (PNG)"
function Find-TabItem($node, $label) { if ($node -eq $null) { return $null }; if ($node -is [System.Windows.Controls.TabItem] -and $node.Header -is [System.Windows.Controls.StackPanel] -and $node.Header.Children[0].Text -like "$label*") { return $node }; for ($i = 0; $i -lt [System.Windows.Media.VisualTreeHelper]::GetChildrenCount($node); $i++) { $r = Find-TabItem ([System.Windows.Media.VisualTreeHelper]::GetChild($node, $i)) $label; if ($r) { return $r } }; return $null }
$vm.ShowRequirementReports = $true; Wait
Snap $win "$Out\ver-view-requirements.png"
$vm.IsVerPlanView = $true; Wait
Check 'plan görünümü seçili' ($vm.IsVerPlanView -and -not $vm.IsVerReqView -and $vm.VerCoverageText -like '*4.3*')
Snap $win "$Out\ver-view-plan.png"
$vm.IsVerReportView = $true; $vm.ShowReportRequirements = $true; Wait
Snap $win "$Out\ver-view-report.png"
$vm.IsVerReqView = $true; Wait
$win.Close()
""
"SONUC: $ok basarili, $fail hata"
