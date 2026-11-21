param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
. (Join-Path $PSScriptRoot '..\_Ortak.ps1')
# 15.11.2026 stok klasör düzeni: 01_Ortak · 02_Test_Araclari (Jig, Kablo, Yazilim, Diger) · 03_Testler (gruplar) · _ARSIV.
# Eski stoklar (05_Jigler, 03_Test_Sonuclari, 07_Fotograflar …) okunmaya devam eder; diskte hiçbir şey taşınmaz.
function W([string]$path, [string]$text) { New-Item -ItemType Directory -Force (Split-Path $path) | Out-Null; [IO.File]::WriteAllText($path, $text, (New-Object Text.UTF8Encoding $false)) }

"--- şablon: yeni varsayılan"
$def = [Pusula.Core.Templates.TemplateSet]::CreateDefault()
$folders = @($def.Modules | ForEach-Object Folder) -join ','
Check 'modüller: 01_Ortak, 02_Test_Araclari, 03_Testler, _ARSIV' ($folders -eq '01_Ortak,02_Test_Araclari,03_Testler,_ARSIV') $folders
$doner = $def.Templates | Where-Object Name -eq 'Döner anten'
$paths = @($def.StockFoldersFor($doner.Modules))
Check 'Döner anten: test araçları ve test grupları (Diğer dahil)' ($paths -contains '02_Test_Araclari\Yazilim' -and $paths -contains '02_Test_Araclari\Diger' -and $paths -contains '03_Testler\01_Islevsel' -and $paths -contains '03_Testler\04_Diger') ($paths -join ', ')

"--- şablon: eski dosya yeni düzene çevrilir"
$old = [Pusula.Core.Templates.TemplateStore]::LoadOrCreate($Data)
$oldFolders = @($old.Modules | ForEach-Object Folder)
Check 'eski modüller kalktı, yeniler geldi' (-not ($oldFolders -contains '05_Jigler') -and -not ($oldFolders -contains '07_Fotograflar') -and $oldFolders -contains '01_Ortak' -and $oldFolders -contains '02_Test_Araclari' -and $oldFolders -contains '03_Testler') ($oldFolders -join ',')
$oldDoner = $old.Templates | Where-Object Name -eq 'Döner anten'
Check 'şablon anahtarları çevrildi (tekrarsız)' ((@($oldDoner.Modules) -join ',') -eq 'common,tests,testTools,_ARSIV') (@($oldDoner.Modules) -join ',')

"--- yeni stok"
$project = Join-Path $Data '7000_MILKON'
$res = [Pusula.Core.Tracking.ProjectService]::CreateStock($project, 'AB-7000-0099', 'Deneme anteni', 'Döner anten', [string[]]$paths, '')
$stock = $res.Folder
Check 'stok açıldı' ($res.Succeeded) ([string]$res.Error)
Check 'birim bilgisi notu ve 01_Ortak her stokta' ((Test-Path (Join-Path $stock 'AB-7000-0099.md')) -and (Test-Path (Join-Path $stock '01_Ortak')))
Check 'şablon klasörleri' ((Test-Path (Join-Path $stock '02_Test_Araclari\Jig')) -and (Test-Path (Join-Path $stock '03_Testler\04_Diger')) -and -not (Test-Path (Join-Path $stock '05_Jigler')))
$res2 = [Pusula.Core.Tracking.ProjectService]::CreateStock($project, 'AB-7000-0098', '', 'Pasif anten', [string[]]@(), '')
Check 'şablonsuz da 01_Ortak açılır' ($res2.Succeeded -and (Test-Path (Join-Path $res2.Folder '01_Ortak')))

"--- koşular 03_Testler'den"
W "$stock\03_Testler\01_Islevsel\Anten_Oruntu\SNP1_T1\test.md" "---`nsonuc: Geçti`ntarih: 2026-11-10`n---`nKazanç limit içinde.`n"
W "$stock\03_Testler\03_CK_EMC\SN01_T2\notlar.txt" "doğrudan grup altında koşu"
W "$stock\03_Testler\01_Islevsel\_ARSIV\SNP1_T9\x.txt" "arşiv okunmaz"
$runs = @([Pusula.Core.Testing.TestRunReader]::ReadStock($stock))
Check 'iki koşu (arşiv hariç)' ($runs.Count -eq 2) ([string]$runs.Count)
$a = $runs | Where-Object SerialNo -eq 'SNP1'
Check 'test klasöründeki koşu: sistem = test adı' ($a -and $a.System -eq 'Anten Oruntu' -and $a.Result -eq 'Passed') ([string]$a.System)
$b = $runs | Where-Object SerialNo -eq 'SN01'
Check 'grup altındaki koşu: sistem = grup adı' ($b -and $b.System -eq 'CK EMC' -and $b.Round -eq 2) ([string]$b.System)
$legacyRuns = @([Pusula.Core.Testing.TestRunReader]::ReadStock((Join-Path $project 'AB-7000-0002')))
Check 'eski stokta 03_Test_Sonuclari okunmaya devam' ($legacyRuns.Count -gt 0) ([string]$legacyRuns.Count)

"--- kapak 01_Ortak'tan"
W "$stock\01_Ortak\AB-7000-0099_kapak.png" "x"
W "$stock\01_Ortak\cizim.png" "x"
$cover = [Pusula.Core.Notes.StockCover]::Read($stock, 'AB-7000-0099')
Check 'kapak: yalnız adında kapak geçen' ($cover.Photos.Count -eq 1 -and $cover.Photos[0] -like '*AB-7000-0099_kapak.png') (@($cover.Photos) -join ',')
Check 'dokümanlar 01_Ortak' ($cover.DocumentsFolder -like '*\01_Ortak')

"--- test araçları: 02_Test_Araclari, klasör adı yalnız numara"
$types = [Pusula.Core.Types.TypeSet]::CreateDefault()
$jig = $types.Find('jig'); $sw = $types.Find('software'); $eq = $types.Find('equipment')
Check 'stok klasörleri 02_Test_Araclari altında' ($jig.StockFolder -eq '02_Test_Araclari\Jig' -and $sw.StockFolder -eq '02_Test_Araclari\Yazilim' -and $eq.StockFolder -eq '02_Test_Araclari\Cihaz' -and $types.Find('other').StockFolder -eq '02_Test_Araclari\Diger' -and $types.Find('cable').StockFolder -eq '02_Test_Araclari\Kablo')
Check 'yazılım numarası JY-####-####' ($sw.AcceptsNumber('JY-7000-0005') -and -not $sw.AcceptsNumber('YZ-7000-0005'))
$made = [Pusula.Core.Inventory.InventoryService]::Create($Data, $jig, [Pusula.Core.Inventory.InventoryLevel]::Stock, $stock, 'JIG-7000-0001', 'Adaptör plakası', '', '', 'deneme', $null)
Check 'jig açıldı: klasör adı yalnız numara' ($made.Succeeded -and $made.Folder -eq (Join-Path $stock '02_Test_Araclari\Jig\JIG-7000-0001')) ([string]$made.Folder)
$madeSw = [Pusula.Core.Inventory.InventoryService]::Create($Data, $sw, [Pusula.Core.Inventory.InventoryLevel]::Stock, $stock, 'JY-7000-0005', 'Döner tabla yazılımı', '', '', 'deneme', $null)
Check 'yazılım: 02_Test_Araclari\Yazilim\JY-7000-0005' ($madeSw.Succeeded -and $madeSw.Folder -eq (Join-Path $stock '02_Test_Araclari\Yazilim\JY-7000-0005')) ([string]$madeSw.Folder)
$all = @([Pusula.Core.Inventory.InventoryService]::ReadAll($Data, [Pusula.Core.Types.TypeDefinition[]]@($jig)))
Check 'yeni ve eski (05_Jigler) jigler birlikte okunur' (($all | Where-Object { $_.Record.Number -eq 'JIG-7000-0001' }) -and ($all | Where-Object { $_.Folder -like '*\05_Jigler\*' })) (@($all | ForEach-Object Folder) -join ' | ')

"SONUÇ OK=$ok HATA=$fail"
