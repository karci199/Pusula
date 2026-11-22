param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
. (Join-Path $PSScriptRoot '..\_Ortak.ps1')
# 22.11.2026 hata avı: aynı başlıklı iki Günlük girdisinde doğru olanın değişmesi; Excel'e yazımda geçersiz denetim
# karakterleri; Excel'den okumada sayı hücrelerinin kayan nokta artığı; doğrulama dosyası önbelleğinin yazımdan sonra tazelenmesi.
Add-Type -AssemblyName System.IO.Compression; Add-Type -AssemblyName System.IO.Compression.FileSystem
$J = [Pusula.Core.Notes.Journal]

"--- 1. Aynı başlıklı iki Günlük girdisi"
$note = Join-Path $Data 'ikiz.md'
$nl = "`r`n"
$txt = '# Not' + $nl + $nl + '## Günlük' + $nl + $nl + '### 22.11.2026 · Ölçüm · VSWR' + $nl + 'birinci' + $nl + $nl + '### 22.11.2026 · Ölçüm · VSWR' + $nl + 'ikinci' + $nl
[IO.File]::WriteAllText($note, $txt, (New-Object Text.UTF8Encoding $true))
$read = @($J::Read($note))
Check 'iki girdi okunur, sıra numaraları 0 ve 1' ($read.Count -eq 2 -and $read[0].SourceOccurrence -eq 0 -and $read[1].SourceOccurrence -eq 1) ($read.Count)
$second = $read | Where-Object { $_.Body -like '*ikinci*' } | Select-Object -First 1
$entry = $J::EntryOf($second); $entry.Body = 'ikinci düzeltildi'
$err = $J::Replace($note, $second.SourceLine, $second.SourceOccurrence, $entry)
$now = [IO.File]::ReadAllText($note)
Check 'ikinci girdi değişir, birinci yerinde kalır' ($err -eq '' -and $now.Contains('birinci') -and $now.Contains('ikinci düzeltildi') -and $now.IndexOf('birinci') -lt $now.IndexOf('ikinci düzeltildi')) $now
$read = @($J::Read($note))
$err = $J::Replace($note, $read[1].SourceLine, $read[1].SourceOccurrence, $null)
$now = [IO.File]::ReadAllText($note)
Check 'ikinci silinir, birinci kalır' ($err -eq '' -and $now.Contains('birinci') -and -not $now.Contains('ikinci')) $now
$err = $J::Replace($note, $read[1].SourceLine, 5, $null)
Check 'olmayan sıra bulunamaz, dosya değişmez' ($err -ne '' -and [IO.File]::ReadAllText($note) -eq $now) $err

"--- 2. Excel'e yazım: denetim karakteri, okuma: sayı"
$x = Join-Path $Data 'deneme.xlsx'
$rows = New-Object 'System.Collections.Generic.List[string[]]'
$rows.Add([string[]]@('TG no', 'Gereksinim'))
$rows.Add([string[]]@('DGÖ-1', ('Satır' + [char]11 + 'iki' + [char]1 + ' son')))
$err = [Pusula.Core.Office.XlsxWriter]::Write($x, 'Gereksinimler', $rows)
$ok2 = $false; $cellText = ''
try
{
    $read2 = [Pusula.Core.Office.XlsxReader]::ReadFirstSheet($x)
    $cellText = $read2[1][1]; $ok2 = $true
}
catch { $cellText = $_.Exception.Message }
Check 'geçersiz karakterli metin yazılır ve geri okunur' ($err -eq '' -and $ok2 -and $cellText -eq 'Satıriki son') $cellText

$y = Join-Path $Data 'sayi.xlsx'
if (Test-Path $y) { Remove-Item $y }
$zip = [System.IO.Compression.ZipFile]::Open($y, 'Create')
function Put([string]$name, [string]$text) { $e = $zip.CreateEntry($name); $w = New-Object IO.StreamWriter($e.Open(), (New-Object Text.UTF8Encoding($false))); $w.Write($text); $w.Close() }
Put 'xl/workbook.xml' '<?xml version="1.0" encoding="UTF-8"?><workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets><sheet name="S" sheetId="1" r:id="rId1"/></sheets></workbook>'
Put 'xl/_rels/workbook.xml.rels' '<?xml version="1.0" encoding="UTF-8"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/></Relationships>'
Put 'xl/worksheets/sheet1.xml' '<?xml version="1.0" encoding="UTF-8"?><worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><sheetData><row r="1"><c r="A1"><v>3.1000000000000001</v></c><c r="B1"><v>12</v></c><c r="C1" t="n"><v>0.30000000000000004</v></c><c r="D1"><v>1.5E-3</v></c></row></sheetData></worksheet>'
$zip.Dispose()
$r3 = [Pusula.Core.Office.XlsxReader]::ReadFirstSheet($y)
Check 'sayı hücreleri Excel''de göründüğü gibi' ($r3[0][0] -eq '3.1' -and $r3[0][1] -eq '12' -and $r3[0][2] -eq '0.3' -and $r3[0][3] -eq '1.5E-3') ($r3[0] -join ' | ')

"--- 3. Doğrulama dosyası önbelleği"
$stock = (Get-ChildItem $Data -Recurse -Directory | Where-Object { $_.Name -eq 'AB-7000-0003' } | Select-Object -First 1).FullName
$VL = [Pusula.Core.Testing.VerificationList]
$before = @($VL::Read($stock)).Count
$item = New-Object Pusula.Core.Testing.VerificationItem; $item.Text = 'Önbellek denemesi'; $item.Group = 'Diğer'
$tpl = "| # | TG no | Gereksinim | Grup | Durum |`r`n|---|---|---|---|---|`r`n"
$err = $VL::Add($stock, $item, $tpl)
$after = @($VL::Read($stock)).Count
Check 'uygulamanın yazımından sonra yeni satır okunur' ($err -eq '' -and $after -eq $before + 1) ("$before → $after $err")
$path = $VL::PathFor($stock)
$t2 = [IO.File]::ReadAllText($path).Replace('Önbellek denemesi', 'Dışarıdan değişti')
Start-Sleep -Milliseconds 30
[IO.File]::WriteAllText($path, $t2, (New-Object Text.UTF8Encoding $true))
$again = @($VL::Read($stock)) | Where-Object { $_.Text -eq 'Dışarıdan değişti' }
Check 'dışarıdan (Obsidian) değişiklik okunur' ($again -ne $null)

"--- 4. Plan ayrıntısı: #### alt başlık ve #etiket satırı kesmez"
$t3 = [IO.File]::ReadAllText($path).TrimEnd() + "`r`n`r`n## Plan`r`n`r`n### 4.1 · Kütle`r`nbir`r`n#### Adımlar`r`niki`r`n#etiket`r`nüç`r`n`r`n### 4.2 · Boy`r`ndört`r`n"
[IO.File]::WriteAllText($path, $t3, (New-Object Text.UTF8Encoding $true))
$det = [Pusula.Core.Testing.PlanReportList]::ReadDetails($stock, 'Plan')
Check '4.1 ayrıntısı tam' ($det['4.1'] -match 'bir' -and $det['4.1'] -match 'iki' -and $det['4.1'] -match 'üç' -and -not ($det['4.1'] -match 'dört')) $det['4.1']
Check '4.2 ayrıntısı ayrı' ($det['4.2'] -eq 'dört') $det['4.2']

"SONUÇ OK=$ok HATA=$fail"
