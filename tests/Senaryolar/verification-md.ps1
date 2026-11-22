param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
. (Join-Path $PSScriptRoot '..\_Ortak.ps1')
$VL = [Pusula.Core.Testing.VerificationList]
$PR = [Pusula.Core.Testing.PlanReportList]
$utf8 = New-Object System.Text.UTF8Encoding($true)

# Eski düzen (22.11.2026 öncesi): gereksinim tablosunda TP / TR sütunları, ## Sonuç başlığı, işaretli madde bölümleri.
$H21 = '| # | TG no | Kaynak | Başlık | Kısa not | Gereksinim | Grup | Alt grup | Tür | Bağlı | Yöntem | Altyapı | TP no | TP açıklama | TP not | TR no | TR açıklama | TR not | TR sonuç | Sonuç yeri | Durum | Sorumlu |'
$S21 = '|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|'
$H13 = '| # | TG no | Kaynak | Başlık | Kısa not | Gereksinim | Grup | Alt grup | Tür | Bağlı | Yöntem | Altyapı | Durum | Sorumlu |'
function Row([string]$no, [string]$tg, [string]$text, [string]$method, [string]$facility, [string]$tp, [string]$tpd, [string]$tpn, [string]$tr, [string]$trd, [string]$trn, [string]$res, [string]$path, [string]$status, [string]$owner)
{
    '| ' + (@($no, $tg, '', $text, '', '', 'İşlevsel', '', 'Temel', '', $method, $facility, $tp, $tpd, $tpn, $tr, $trd, $trn, $res, $path, $status, $owner) -join ' | ') + ' |'
}

$tmp = Join-Path $env:TEMP ('pusula-vmd-' + [guid]::NewGuid().ToString('N').Substring(0,6))
try {
    "--- Eski düzenden geçiş: Plan / Rapor tablosu yok"
    $stock = Join-Path $tmp 'AB-1'
    New-Item -ItemType Directory -Force (Join-Path $stock '03_Testler') | Out-Null
    $lines = @(
        '---', 'tur: test-gereksinimleri', '---', '', '# AB-1 Test Gereksinimleri', '', 'Giriş metni.', '', '## Gereksinimler', '',
        $H21, $S21,
        (Row '1' 'DGÖ-1' 'Hüzme' 'Test' 'SATIMO' '4.1' 'SATIMO hüzme' 'Jig JT-9' '5.1' 'Hüzme sonuçları' '31,2°' 'Geçti' '03_Testler\S1' 'Geçti' 'Ali'),
        (Row '2' 'DGÖ-2' 'Yan lob' 'Test' 'SATIMO' '4.1' '' '' '5.2' 'Yan lob sonuçları' '−13,8 dB' 'Kaldı' '' 'Kaldı' ''),
        (Row '3' 'DGÖ-3' 'VSWR' 'Test' 'VNA' '4.3' 'VNA S11' '' '' '' '' '' '' 'Planlandı' ''),
        (Row '4' 'DGÖ-4' 'Kazanç' '' '' '' '' '' '' '' '' '' '' 'Bekliyor' 'Ayşe'),
        '', '## Plan', '',
        '### DGÖ-1 · Hüzme', '%% pusula:plan DGÖ-1 %%', '- **TP:** 4.1 · SATIMO hüzme', '%% pusula:son %%', 'Elle: kurulum notu.', '',
        '### DGÖ-3 · VSWR', '%% pusula:plan DGÖ-3 %%', '- **TP:** 4.3 · VNA S11', '%% pusula:son %%', '',
        '## Sonuç', '',
        '### DGÖ-1 · Hüzme', '%% pusula:sonuc DGÖ-1 %%', '- **TR:** 5.1', '%% pusula:son %%', '',
        'Son not.', '')
    $file = Join-Path $stock '03_Testler\AB-1_Test_Gereksinimleri.md'
    $original = $lines -join "`r`n"
    [IO.File]::WriteAllText($file, $original, $utf8)
    $err = ''
    Check 'geçiş yapılır' ($PR::RetireLegacy($stock, [ref]$err)) $err
    $text = [IO.File]::ReadAllText($file)
    $backups = @(Get-ChildItem (Join-Path $stock '_ARSIV') -Filter '*.md.yedek' -ErrorAction SilentlyContinue)
    Check 'yedek alınır (_ARSIV, .md.yedek, içerik aynı)' ($backups.Count -eq 1 -and [IO.File]::ReadAllText($backups[0].FullName) -eq $original) ([string]$backups.Count)
    Check 'gereksinim tablosu 13 sütun (+ tanınmayan Sorumlu)' ($text.Contains("$H13`r`n|---|---|---|---|---|---|---|---|---|---|---|---|---|---|`r`n| 1 | DGÖ-1 |  | Hüzme |  |  | İşlevsel |  | Temel |  | Test | SATIMO | Geçti | Ali |")) $text
    Check 'TP / TR sütunları yok' ($text -notlike '*TP açıklama*' -and $text -notlike '*TR sonuç*' -and $text -notlike '*| TP no | TP*')
    $items = $VL::Read($stock)
    Check '4 gereksinim, durum ve tanınmayan sütun korunur' ($items.Count -eq 4 -and $items[1].Status -eq 'Kaldı' -and $items[3].Extra['Sorumlu'] -eq 'Ayşe')
    $plans = $PR::ReadPlans($stock); $reports = $PR::ReadReports($stock)
    Check 'plan: 4.1 (DGÖ-1, DGÖ-2) ve 4.3' ($plans.Count -eq 2 -and ($plans[0].Requirements -join ',') -eq 'DGÖ-1,DGÖ-2' -and $plans[0].Description -eq 'SATIMO hüzme' -and $plans[0].Note -eq 'Jig JT-9' -and $plans[1].TpNo -eq '4.3' -and $plans[1].Facility -eq 'VNA') (@($plans | ForEach-Object { $_.TpNo + ':' + ($_.Requirements -join '+') }) -join ' ')
    Check 'rapor: 5.1 Geçti (yer, not), 5.2 Kaldı; ikisi de 4.1' ($reports.Count -eq 2 -and $reports[0].Result -eq 'Geçti' -and $reports[0].ResultPath -eq '03_Testler\S1' -and $reports[0].Note -eq '31,2°' -and $reports[1].Result -eq 'Kaldı' -and ($reports[1].Plans -join ',') -eq '4.1')
    Check '## Sonuç → ## Rapor (yalnız başlık)' ($text -like "*`r`n## Rapor`r`n*" -and $text -notlike "*## Sonuç*")
    Check 'işaretli eski bölümler kalktı' ($text -notlike '*pusula:plan*' -and $text -notlike '*pusula:sonuc*' -and $text -notlike '*pusula:son*' -and $text -notlike '*### DGÖ-3*') $text
    Check 'elle yazılan başlığıyla kalır' ($text.Contains("### DGÖ-1 · Hüzme`r`nElle: kurulum notu.")) $text
    Check 'giriş, son not, YAML korunur' ($text.StartsWith("---`r`ntur: test-gereksinimleri`r`n---") -and $text.Contains('Giriş metni.') -and $text.Contains('Son not.'))
    Check 'üç boş satır yok' (-not $text.Contains("`r`n`r`n`r`n")) $text
    Check 'Plan tablosu ## Plan altında, Rapor tablosu ## Rapor altında (Tarih, Birim)' ($text -like '*## Plan*| TP no | Açıklama | Yöntem | Altyapı | Gereksinimler | Not |*## Rapor*| TR no | Açıklama | Plan | Tarih | Birim | Sonuç | Sonuç yeri | Not |*| 5.1 | Hüzme sonuçları | 4.1 |  |  | Geçti | 03_Testler\S1 | 31,2° |*') $text
    Check 'ikinci geçiş yapılmaz, yeni yedek yok' (-not $PR::RetireLegacy($stock, [ref]$err) -and @(Get-ChildItem (Join-Path $stock '_ARSIV') -Filter '*.md.yedek').Count -eq 1)
    Check 'madde eklenince eski sütun geri gelmez' ($VL::Add($stock, (New-Object Pusula.Core.Testing.VerificationItem -Property @{ Text = 'Yeni'; TpNo = '9.9' }), '') -eq '' -and ([IO.File]::ReadAllText($file)) -notlike '*TP no |*9.9*')

    "--- Plan / Rapor tablosu varken: eksikler tamamlanır, dolu alanlar korunur"
    $stock2 = Join-Path $tmp 'AB-2'
    New-Item -ItemType Directory -Force (Join-Path $stock2 '03_Testler') | Out-Null
    $lines2 = @('# AB-2', '', $H21, $S21,
        (Row '1' 'DGÖ-1' 'Hüzme' 'Test' 'SATIMO' '4.1' 'eski açıklama' 'eski not' '5.1' '' '' 'Geçti' '' 'Geçti' ''),
        (Row '2' 'DGÖ-2' 'Yan lob' 'Test' '' '4.2' 'Yan lob planı' '' '' '' '' '' '' 'Planlandı' ''),
        '', '## Plan', '', '| TP no | Açıklama | Yöntem | Altyapı | Gereksinimler | Not |', '|---|---|---|---|---|---|', '| 4.1 | Yeni açıklama | Test | SATIMO |  |  |', '',
        '## Rapor', '', '| TR no | Açıklama | Sonuç | Plan | Sonuç yeri | Not |', '|---|---|---|---|---|---|', '| 5.1 | Hüzme | Geçti | 4.1 |  |  |', '')
    $file2 = Join-Path $stock2 '03_Testler\AB-2_Test_Gereksinimleri.md'
    [IO.File]::WriteAllText($file2, ($lines2 -join "`n"), (New-Object System.Text.UTF8Encoding($false)))
    Check 'geçiş yapılır' ($PR::RetireLegacy($stock2, [ref]$err)) $err
    $plans = $PR::ReadPlans($stock2)
    Check '4.1: açıklama korunur, boş not dolar, DGÖ-1 bağlanır' ($plans[0].Description -eq 'Yeni açıklama' -and $plans[0].Note -eq 'eski not' -and ($plans[0].Requirements -join ',') -eq 'DGÖ-1') ($plans[0].Description + ' / ' + $plans[0].Note)
    Check '4.2 eklenir (DGÖ-2)' ($plans.Count -eq 2 -and $plans[1].TpNo -eq '4.2' -and $plans[1].Description -eq 'Yan lob planı' -and ($plans[1].Requirements -join ',') -eq 'DGÖ-2')
    Check 'rapor değişmez' (@($PR::ReadReports($stock2)).Count -eq 1)
    $reps = $PR::ReadReports($stock2); $reps[0].Date = '22.11.2026'; $reps[0].Unit = 'SN01'
    Check 'tarih ve birim yazılır, eski sıradaki tablo yeni sıraya geçer' ($PR::SaveReports($stock2, $reps) -eq '' -and ([IO.File]::ReadAllText($file2)) -like '*| TR no | Açıklama | Plan | Tarih | Birim | Sonuç | Sonuç yeri | Not |*| 5.1 | Hüzme | 4.1 | 22.11.2026 | SN01 | Geçti |  |  |*' -and @($PR::ReadReports($stock2))[0].Unit -eq 'SN01') ([IO.File]::ReadAllText($file2))
    $text2 = [IO.File]::ReadAllText($file2)
    Check 'LF korunur, BOM eklenmez, TP sütunu yok' ($text2 -notlike "*`r`n*" -and [IO.File]::ReadAllBytes($file2)[0] -ne 0xEF -and $text2 -notlike '*TP açıklama*') $text2

    "--- Plan tablosu: tanınmayan sütun korunur"
    $plans = $PR::ReadPlans($stock2)
    $text2 = $text2.Replace('| TP no | Açıklama | Yöntem | Altyapı | Gereksinimler | Not |', '| TP no | Açıklama | Yöntem | Altyapı | Gereksinimler | Not | Sorumlu |').Replace("|---|---|---|---|---|---|`n| 4.1", "|---|---|---|---|---|---|---|`n| 4.1")
    $text2 = $text2.Replace('| 4.1 | Yeni açıklama | Test | SATIMO | DGÖ-1 | eski not |', '| 4.1 | Yeni açıklama | Test | SATIMO | DGÖ-1 | eski not | Ali |')
    [IO.File]::WriteAllText($file2, $text2, (New-Object System.Text.UTF8Encoding($false)))
    $plans = $PR::ReadPlans($stock2)
    $plans[0].Requirements.Add('DGÖ-3'); $new = New-Object Pusula.Core.Testing.PlanItem; $new.TpNo = '4.5'; $new.Description = 'Kütle'; $new.Method = 'Muayene'; $plans.Add($new)
    Check 'kaydedilir' ($PR::SavePlans($stock2, $plans) -eq '')
    $text2 = [IO.File]::ReadAllText($file2)
    Check 'tanınmayan sütun (Sorumlu) korunur, yeni madde eklenir' ($text2 -like '*| 4.1 | Yeni açıklama | Test | SATIMO | DGÖ-1, DGÖ-3 | eski not | Ali |*| 4.5 | Kütle | Muayene |  |  |  |  |*') $text2
    Check 'gereksinim tablosu sağlam' (@($VL::Read($stock2)).Count -eq 2)

    "--- Plan ayrıntısı (### TP no altında elle yazılan)"
    $text2 = [IO.File]::ReadAllText($file2).Replace("`n## Rapor", "`n### 4.1 · Kurulum`nAdım 1: kalibrasyon.`nAdım 2: ölçüm.`n`n### 4.5`n`n## Rapor")
    [IO.File]::WriteAllText($file2, $text2, (New-Object System.Text.UTF8Encoding($false)))
    $det = $PR::ReadDetails($stock2, 'Plan')
    Check 'ayrıntı okunur: 4.1 iki satır, boş 4.5 yok' ($det.Count -eq 1 -and $det['4.1'] -eq ("Adım 1: kalibrasyon." + [Environment]::NewLine + "Adım 2: ölçüm.")) ([string]$det.Count)
    Check 'plan kaydı ayrıntıya dokunmaz' ($PR::SavePlans($stock2, $PR::ReadPlans($stock2)) -eq '' -and ([IO.File]::ReadAllText($file2)).Contains("### 4.1 · Kurulum`nAdım 1: kalibrasyon.`nAdım 2: ölçüm."))
    Check 'rapor bölümünde ayrıntı yok' ($PR::ReadDetails($stock2, 'Rapor').Count -eq 0)
} finally { Remove-Item -Recurse -Force $tmp -ErrorAction SilentlyContinue }
""
"SONUC: $ok basarili, $fail hata"
