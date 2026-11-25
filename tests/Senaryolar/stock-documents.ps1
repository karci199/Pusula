param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
. (Join-Path $PSScriptRoot '..\_Ortak.ps1')
# Stok dokümanları (25.11.2026): 01_Ortak\Dokumanlar'da <stok>_<kod>_<rev>[_TASLAK] adından durum (P1–P9 < AA < AB < BA),
# Şablonlar → Dokümanlar listesi (sürüm 6), Doğrulamalar'da doküman şeridi, yöntemi 261 olan madde (plan / rapor istemez;
# 261'in harfli revizyonu aktarılana kadar sarı, özet sayılarına girmez). Zaman çizelgesine hiçbir şey yazılmaz.
$SD = [Pusula.Core.Testing.StockDocuments]
$def = New-Object Pusula.Core.Templates.DocumentDefinition; $def.Code = '261'; $def.Name = 'Üretim test dokümanı'; $def.StockPrefix = 'AB'
$no = 'AB-7000-0003'
$state = { param([string[]]$names) $SD::StateOf($def, $no, [string[]]($names | ForEach-Object { 'C:\x\' + $_ })) }

"--- 1. Ad çözümleme ve sıra"
$rev = ''; $draft = $false
Check 'AA taslak çözülür' ($SD::TryParse('AB-7000-0003_261_AA_TASLAK.docx', $no, '261', [ref]$rev, [ref]$draft) -and $rev -eq 'AA' -and $draft)
Check 'küçük harf ve uzantısız ad' ($SD::TryParse('ab-7000-0003_261_ba_taslak', $no, '261', [ref]$rev, [ref]$draft) -and $rev -eq 'BA' -and $draft)
Check 'aktarılmış (eksiz) ad' ($SD::TryParse('AB-7000-0003_261_P3.pdf', $no, '261', [ref]$rev, [ref]$draft) -and $rev -eq 'P3' -and -not $draft)
foreach ($bad in 'AB-7000-0004_261_AA.pdf', 'AB-7000-0003_262_AA.pdf', 'AB-7000-0003_261_A.pdf', 'AB-7000-0003_261_AAA.pdf', 'AB-7000-0003_261_P0.pdf', 'AB-7000-0003_261_AA_eski.pdf', 'AB-7000-0003_2610_AA.pdf')
{
    Check ('uymayan ad okunmaz: ' + $bad) (-not $SD::TryParse($bad, $no, '261', [ref]$rev, [ref]$draft))
}
Check 'sıra: P1 < P9 < AA < AB < AZ < BA' ($SD::RankOf('P1') -lt $SD::RankOf('P9') -and $SD::RankOf('P9') -lt $SD::RankOf('AA') -and $SD::RankOf('AA') -lt $SD::RankOf('AB') -and $SD::RankOf('AZ') -lt $SD::RankOf('BA'))

"--- 2. Durum kombinasyonları"
$cases = @(
    @(@(), 'NotHere', '', $false),
    @(@('AB-7000-0003_261_P1_TASLAK.docx'), 'PrototypeDraft', 'P1', $false),
    @(@('AB-7000-0003_261_P9.pdf', 'AB-7000-0003_261_P2.pdf'), 'PrototypeReleased', 'P9', $false),
    @(@('AB-7000-0003_261_P1.pdf'), 'PrototypeReleased', 'P1', $false),
    @(@('AB-7000-0003_261_P1.pdf', 'AB-7000-0003_261_P2_TASLAK.docx'), 'PrototypeDraft', 'P2', $false),
    @(@('AB-7000-0003_261_P2.pdf', 'AB-7000-0003_261_AA_TASLAK.docx'), 'Draft', 'AA', $false),
    @(@('AB-7000-0003_261_AA_TASLAK.docx'), 'Draft', 'AA', $false),
    @(@('AB-7000-0003_261_AA.pdf'), 'Released', 'AA', $true),
    @(@('AB-7000-0003_261_AA_TASLAK.docx', 'AB-7000-0003_261_AA.pdf'), 'Released', 'AA', $true),
    @(@('AB-7000-0003_261_AA.pdf', 'AB-7000-0003_261_BA_TASLAK.docx'), 'ChangePending', 'BA', $true),
    @(@('AB-7000-0003_261_AA.pdf', 'AB-7000-0003_261_AB.pdf', 'AB-7000-0003_261_BA_TASLAK.docx'), 'ChangePending', 'BA', $true),
    @(@('AB-7000-0003_261_AA.pdf', 'AB-7000-0003_261_BA.pdf'), 'Released', 'BA', $true),
    @(@('AB-7000-0003_261_AA.pdf', 'baska_kisinin_dokumani.pdf', 'AB-7000-0003_133_AB.pdf'), 'Released', 'AA', $true)
)
foreach ($c in $cases)
{
    $s = & $state $c[0]
    Check ('[' + ($c[0] -join ' + ') + '] → ' + $c[1] + ' ' + $c[2]) ([string]$s.State -eq $c[1] -and $s.Revision -eq $c[2] -and $s.IsOfficial -eq $c[3]) ([string]$s.State + ' ' + $s.Revision + ' resmî=' + $s.IsOfficial)
}
$s = & $state @('AB-7000-0003_261_AA.pdf', 'AB-7000-0003_261_BA_TASLAK.docx')
Check 'değişiklik bekliyor: aktarılan AA, bekleyen BA' ($s.ReleasedRevision -eq 'AA' -and $s.Revision -eq 'BA')

"--- 3. Hangi stokta aranır"
$empty = New-Object Pusula.Core.Templates.DocumentDefinition; $empty.Name = 'Test planı dokümanı'
$jy = New-Object Pusula.Core.Templates.DocumentDefinition; $jy.Code = '032'; $jy.StockPrefix = 'JY'
Check '261 AB stokta aranır, JY stokta aranmaz' ($SD::AppliesTo($def, 'AB-7000-0003') -and -not $SD::AppliesTo($def, 'JY-7000-0004'))
Check 'kodu boş satır hiçbir stokta aranmaz' (-not $SD::AppliesTo($empty, 'AB-7000-0003'))
Check '032 yalnız JY stokta' ($SD::AppliesTo($jy, 'JY-7000-0004') -and -not $SD::AppliesTo($jy, 'AB-7000-0003'))

"--- 4. Şablon dosyası sürüm 6"
$TSet = [Pusula.Core.Templates.TemplateSet]
$d = $TSet::CreateDefault()
Check 'varsayılan: 261, iki boş kod (TG, TP), 032 / 132 / 133' ((@($d.Documents | ForEach-Object Code) -join ',') -eq '261,,,032,132,133') (@($d.Documents | ForEach-Object Code) -join ',')
Check 'yazılımın üç dokümanı aynı grupta (birlikte aktarılır)' (@($d.Documents | Where-Object { $_.Group -eq 'Yazılım dokümanları' }).Count -eq 3)
Check 'Ortak: Dokumanlar, Fotograflar, Girdiler' ((@($d.Modules[0].SubFolders) -join ',') -eq 'Dokumanlar,Fotograflar,Girdiler') (@($d.Modules[0].SubFolders) -join ',')
$old = $TSet::CreateDefault(); $old.Documents.Clear(); $old.Modules[0].SubFolders.Remove('Dokumanlar') | Out-Null; $old.SchemaVersion = 5; $old.Normalize()
$old2 = $TSet::CreateDefault(); $old2.Modules[0].SubFolders.Clear(); $old2.Modules[0].SubFolders.Add('Fotograflar'); $old2.SchemaVersion = 5; $old2.Normalize()
Check 'sürüm 5 → 6: liste ve klasörler eklenir' ($old.Documents.Count -eq 6 -and (@($old2.Modules[0].SubFolders) -join ',') -eq 'Dokumanlar,Fotograflar,Girdiler' -and $old.SchemaVersion -eq 6) (@($old2.Modules[0].SubFolders) -join ',')
$mine = $TSet::CreateDefault(); $mine.Documents.Clear(); $mine.Normalize()
Check 'sürüm 6: kullanıcının boşalttığı liste yeniden dolmaz' ($mine.Documents.Count -eq 0)

"--- 5. Şablonlar penceresi"
$ed = New-Object Pusula.Wpf.ViewModels.TemplateEditor.TemplateEditorViewModel($Data, $TSet::CreateDefault())
Check 'pencerede altı doküman satırı' ($ed.Documents.Count -eq 6)
$ed.Documents[1].Code = 'TGD'
$ed.Documents.Add((New-Object Pusula.Wpf.ViewModels.TemplateEditor.DocumentEditRow))
Check 'kaydedilir (boş satır atlanır)' ($ed.Save()) $ed.Error
$saved = [Pusula.Core.Templates.TemplateStore]::LoadOrCreate($Data)
Check 'dosyada TG kodu, boş satır yok' ((@($saved.Documents | ForEach-Object Code) -join ',') -eq '261,TGD,,032,132,133') (@($saved.Documents | ForEach-Object Code) -join ',')
$ed.Documents[2].Code = '261'
Check 'aynı kod iki kez: kaydedilmez' (-not $ed.Save() -and $ed.Error -like '*261*') $ed.Error
$ed.Documents[2].Code = '26 1'
Check 'boşluklu kod: kaydedilmez' (-not $ed.Save())
$ed.Documents[2].Code = ''

"--- 6. Stok sayfası"
[System.Threading.SynchronizationContext]::SetSynchronizationContext((New-Object System.Windows.Threading.DispatcherSynchronizationContext))
$app = New-Object Pusula.Wpf.App; $app.InitializeComponent(); $app.ShutdownMode = 'OnExplicitShutdown'
$win = New-Object Pusula.Wpf.MainWindow
$win.WindowStartupLocation = 'Manual'; $win.Left = -20000; $win.Top = -20000; $win.ShowActivated = $false; $win.Width = 1920; $win.Height = 1040
$win.Show(); Pump
$vm = $win.DataContext; $vm.RootPath = $Data; Wait

$stock = Join-Path $Data '7000_MILKON\AB-7000-0003'
$docs = Join-Path $stock '01_Ortak\Dokumanlar'
$note = Join-Path $stock 'AB-7000-0003.md'
$noteBefore = if (Test-Path $note) { [IO.File]::ReadAllText($note) } else { '' }
$eventsBefore = @(Get-ChildItem $stock -Recurse -Filter '*.md' | Where-Object { $_.FullName -like '*Olaylar*' }).Count
$show = { param($path) $vm.SelectNodeByPath((Join-Path $Data '7000_MILKON\AB-7000-0002')); Wait; $vm.SelectNodeByPath($path); Wait; Wait }
& $show $stock
$rows = @($vm.StockDocumentRows)
Check 'AB stokta 261 ve pencerede kodu girilen TGD (kodu boş ve JY dokümanları yok)' ((($rows | ForEach-Object Code) -join ',') -eq '261,TGD') (($rows | ForEach-Object Code) -join ',')
Check 'dosya yokken: Burada yok, gri' ($rows[0].StateText -eq 'Burada yok' -and $rows[0].Tone -eq '' -and $rows[0].ToolTip -like '*Dokumanlar*') ($rows[0].StateText + ' | ' + $rows[0].ToolTip)

foreach ($pair in @(@('Besleme 18-32 V', 'Test'), @('Etiket üretim dokümanına uygun', '261 dokümanı'), @('Paketleme üretim dokümanına uygun', '261 dokümanı')))
{
    $vm.AddVerificationCommand.Execute($null); Pump
    $vm.NewVerText = $pair[0]; $vm.NewVerMethod = $pair[1]
    $null = $vm.ConfirmNewVerification(); $vm.IsVerificationDialogOpen = $false; Pump
}
Wait
Check 'yöntem listesinde 261 dokümanı' ([Pusula.Wpf.ViewModels.VerificationRow]::Methods -contains '261 dokümanı')
$all = { @($vm.VerificationGroups | ForEach-Object { $_.Rows }) }
$r261 = { @(& $all | Where-Object { $_.Item.Method -eq '261 dokümanı' }) }
Check 'üç madde, ikisi 261' ($vm.VerificationCount -eq 3 -and (& $r261).Count -eq 2) ([string]$vm.VerificationCount)
Check '261 maddeleri plansız sayılmaz (yalnız test maddesi)' (@($vm.RequirementsWithoutPlan).Count -eq 1) (@($vm.RequirementsWithoutPlan) -join ',')
Check '261 yokken: madde sarı, çip "261 · yok"' (@(& $r261 | Where-Object { $_.HasNoPlan -and $_.LinkedPlans[0].Kind -eq 4 }).Count -eq 2) ((& $r261)[0].LinkedPlans[0].Text)
Check '261 maddesinde rapor çipi yok' (@(& $r261 | Where-Object { @($_.LinkedReports).Count -eq 0 }).Count -eq 2)
$sum = @($vm.VerificationSummary)
Check 'özet: 1 gereksinim (test planlı), ayrıca "261 ile 2" sarı' ($vm.VerificationSummaryTotal -eq '1 gereksinim' -and $sum[-1].Label -eq '261 ile' -and $sum[-1].Count -eq 2 -and $sum[-1].Tone -eq 'waiting') (($sum | ForEach-Object { $_.Label + '=' + $_.Count }) -join ', ')
Check 'sayı satırı da 261''leri saymaz: Doğrulama 0/1 (hata avı)' ($vm.StockVerificationText -like '*0/1*') $vm.StockVerificationText

New-Item -ItemType Directory -Force $docs | Out-Null
[IO.File]::WriteAllText((Join-Path $docs 'AB-7000-0003_261_AA_TASLAK.docx'), 'x')
[IO.File]::WriteAllText((Join-Path $docs 'Tasarimci_notu.pdf'), 'x')
& $show $stock
$row = @($vm.StockDocumentRows)[0]
Check 'AA hazırlanırken: sarı, "AA · hazırlanıyor"' ($row.StateText -eq 'AA · hazırlanıyor' -and $row.Tone -eq 'waiting') $row.StateText
Check 'hazırlanırken madde hâlâ sarı, çip "261 · AA hazırlanıyor"' (@(& $r261 | Where-Object { $_.HasNoPlan }).Count -eq 2 -and (& $r261)[0].LinkedPlans[0].Text -eq '261 · AA hazırlanıyor') ((& $r261)[0].LinkedPlans[0].Text)

# Taslak aktarımı: P1 (eksiz) → "P1 · taslak aktarıldı", mavi; madde hâlâ sarı (resmî değil), çip "261 · P1 taslak".
Remove-Item (Join-Path $docs 'AB-7000-0003_261_AA_TASLAK.docx')
[IO.File]::WriteAllText((Join-Path $docs 'AB-7000-0003_261_P1.pdf'), 'x')
& $show $stock
$row = @($vm.StockDocumentRows)[0]
Check 'P1 aktarılınca: "P1 · taslak aktarıldı", mavi' ($row.StateText -eq 'P1 · taslak aktarıldı' -and $row.Tone -eq 'active') $row.StateText
Check 'taslak aktarımında madde sarı, çip "261 · P1 taslak"' (@(& $r261 | Where-Object { $_.HasNoPlan -and $_.LinkedPlans[0].Text -eq '261 · P1 taslak' }).Count -eq 2) ((& $r261)[0].LinkedPlans[0].Text)
Remove-Item (Join-Path $docs 'AB-7000-0003_261_P1.pdf')
[IO.File]::WriteAllText((Join-Path $docs 'AB-7000-0003_261_AA.docx'), 'x')
& $show $stock
$row = @($vm.StockDocumentRows)[0]
Check 'aktarılınca: yeşil, "AA · aktarıldı"' ($row.StateText -eq 'AA · aktarıldı' -and $row.Tone -eq 'done') $row.StateText
Check 'aktarılınca madde sarıdan çıkar, çip yeşil "261 · AA"' (@(& $r261 | Where-Object { -not $_.HasNoPlan -and $_.LinkedPlans[0].Kind -eq 1 -and $_.LinkedPlans[0].Text -eq '261 · AA' }).Count -eq 2)
Check 'özette 261 ile yeşil; 261 Geçti sayılmaz' (@($vm.VerificationSummary)[-1].Tone -eq 'done' -and @($vm.VerificationSummary)[0].Count -eq 0) ((@($vm.VerificationSummary) | ForEach-Object { $_.Label + '=' + $_.Count + '/' + $_.Tone }) -join ', ')

[IO.File]::WriteAllText((Join-Path $docs 'AB-7000-0003_261_BA_TASLAK.docx'), 'x')
& $show $stock
$row = @($vm.StockDocumentRows)[0]
Check 'BA taslak: "AA aktarıldı · BA değişiklik bekliyor", kırmızı' ($row.StateText -eq 'AA aktarıldı · BA değişiklik bekliyor' -and $row.Tone -eq 'failed') $row.StateText
Check 'değişiklik beklerken madde sarı değil' (@(& $r261 | Where-Object { $_.HasNoPlan }).Count -eq 0)
$vm.IsVerReqView = $true; Pump
Snap $win "$Out\stock-documents.png"
$page = $win.FindName('StockPageScroll'); $page.ScrollToVerticalOffset(330); Pump; $win.UpdateLayout(); Pump
Snap $win "$Out\stock-documents-rows.png"
$page.ScrollToTop(); Pump

Check 'zaman çizelgesine bir şey yazılmadı (not ve olay notları aynı)' (($(if (Test-Path $note) { [IO.File]::ReadAllText($note) } else { '' }) -eq $noteBefore) -and @(Get-ChildItem $stock -Recurse -Filter '*.md' | Where-Object { $_.FullName -like '*Olaylar*' }).Count -eq $eventsBefore)
Check 'uygulama dosyalara dokunmadı (üç dosya yerinde)' (@(Get-ChildItem $docs).Count -eq 3)

$soft = Join-Path $Data '7000_MILKON\JY-7000-0004'
& $show $soft
$rows = @($vm.StockDocumentRows)
Check 'JY stokta TGD ve yazılım dokümanları tek çip, Burada yok' ((($rows | ForEach-Object Code) -join '|') -eq 'TGD|032 · 132 · 133' -and $rows[1].Name -eq 'Yazılım dokümanları' -and $rows[1].StateText -eq 'Burada yok') (($rows | ForEach-Object Code) -join '|')
$sdocs = Join-Path $soft '01_Ortak\Dokumanlar'; New-Item -ItemType Directory -Force $sdocs | Out-Null
foreach ($c in '032', '132', '133') { [IO.File]::WriteAllText((Join-Path $sdocs ('JY-7000-0004_' + $c + '_P1.zip')), 'x') }
& $show $soft
$rows = @($vm.StockDocumentRows)
Check 'üçü birlikte P1 aktarılınca tek çip: P1 · taslak aktarıldı' ($rows.Count -eq 2 -and $rows[1].StateText -eq 'P1 · taslak aktarıldı') (($rows | ForEach-Object { $_.Code + '=' + $_.StateText }) -join ' | ')
New-Item -ItemType Directory -Force (Join-Path $sdocs 'JY-7000-0004_032_AA') | Out-Null
foreach ($c in '132', '133') { [IO.File]::WriteAllText((Join-Path $sdocs ('JY-7000-0004_' + $c + '_AA.zip')), 'x') }
& $show $soft
$rows = @($vm.StockDocumentRows)
Check 'AA birlikte (032 klasör olarak): tek çip AA · aktarıldı' ($rows.Count -eq 2 -and $rows[1].StateText -eq 'AA · aktarıldı') (($rows | ForEach-Object { $_.Code + '=' + $_.StateText }) -join ' | ')
[IO.File]::WriteAllText((Join-Path $sdocs 'JY-7000-0004_133_AB_TASLAK.docx'), 'x')
& $show $soft
$rows = @($vm.StockDocumentRows)
Check 'biri ayrışınca üçü ayrı çip (133 değişiklik bekliyor)' (((($rows | ForEach-Object Code) -join ',') -eq 'TGD,032,132,133') -and $rows[3].Tone -eq 'failed') (($rows | ForEach-Object { $_.Code + '=' + $_.StateText }) -join ' | ')
$edWin = New-Object Pusula.Wpf.TemplateEditorWindow((New-Object Pusula.Wpf.ViewModels.TemplateEditor.TemplateEditorViewModel($Data, [Pusula.Core.Templates.TemplateStore]::LoadOrCreate($Data))))
$edWin.WindowStartupLocation = 'Manual'; $edWin.Left = -20000; $edWin.Top = -20000; $edWin.ShowActivated = $false; $edWin.Show(); Pump; Wait
Snap $edWin "$Out\template-documents.png"
$edWin.Close()
$win.Close()
