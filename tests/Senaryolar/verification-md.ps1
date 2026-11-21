param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
. (Join-Path $PSScriptRoot '..\_Ortak.ps1')
$VL = [Pusula.Core.Testing.VerificationList]

"--- Md'deki okunur bölümler: sıra, boş satır, silme, yeniden adlandırma"
$tmp = Join-Path $env:TEMP ('pusula-vmd-' + [guid]::NewGuid().ToString('N').Substring(0,6))
$stock = Join-Path $tmp 'AB-1'
try {
    New-Item -ItemType Directory -Force $stock | Out-Null
    $template = "# AB-1 Test Gereksinimleri`r`n`r`n## Gereksinimler`r`n`r`n| # | TG no | Başlık | Durum |`r`n|---|---|---|---|`r`n`r`n## Plan`r`n`r`n## Sonuç`r`n"
    function NewItem([string]$tg, [string]$text, [string]$tp) { $i = New-Object Pusula.Core.Testing.VerificationItem; $i.TgNo = $tg; $i.Text = $text; $i.TpNo = $tp; $i.Method = 'Test'; $i }
    foreach ($x in @(@('TG-1','Bir','4.1'), @('TG-2','İki','4.2'), @('TG-3','Üç','4.3'))) { $VL::Add($stock, (NewItem $x[0] $x[1] $x[2]), $template) | Out-Null }
    $items = $VL::Read($stock)
    function Item([string]$tg) { @($VL::Read($stock) | Where-Object TgNo -eq $tg)[0] }
    $file = $VL::PathFor($stock)
    Check 'TG-1 ve TG-3 yazılır' ($VL::WriteSections($stock, (Item 'TG-1'), $items) -eq '' -and $VL::WriteSections($stock, (Item 'TG-3'), $items) -eq '')
    $VL::WriteSections($stock, (Item 'TG-2'), $items) | Out-Null
    $text = [IO.File]::ReadAllText($file)
    $plan = $text.Substring($text.IndexOf('## Plan'), $text.IndexOf('## Sonuç') - $text.IndexOf('## Plan'))
    Check 'madde sırası: TG-1, TG-2, TG-3' ($plan.IndexOf('pusula:plan TG-1') -lt $plan.IndexOf('pusula:plan TG-2') -and $plan.IndexOf('pusula:plan TG-2') -lt $plan.IndexOf('pusula:plan TG-3')) $plan
    Check 'bloklar arasında çift boş satır yok' (-not $text.Contains("`r`n`r`n`r`n")) $text
    Check 'başlık: ### TG-2 · İki' ($text.Contains("### TG-2 · İki`r`n%% pusula:plan TG-2 %%`r`n- **TP:** 4.2`r`n- **Yöntem:** Test`r`n%% pusula:son %%"))
    Check 'sonuç bölümü boşsa yazılmaz' (-not $text.Contains('pusula:sonuc'))

    # TG-1'in altına elle ayrıntı
    $text = $text.Replace("- **TP:** 4.1`r`n- **Yöntem:** Test`r`n%% pusula:son %%", "- **TP:** 4.1`r`n- **Yöntem:** Test`r`n%% pusula:son %%`r`nElle: kurulum notu.")
    [IO.File]::WriteAllText($file, $text, (New-Object System.Text.UTF8Encoding($true)))
    Check 'silme (ayrıntılı): bölüm kalır, silindi notu, ayrıntı korunur' ($VL::RemoveSections($stock, 'TG-1') -eq '' -and ([IO.File]::ReadAllText($file)).Contains("%% pusula:plan TG-1 %%`r`n- *Madde tablodan silindi.*`r`n%% pusula:son %%`r`nElle: kurulum notu."))
    Check 'silme (ayrıntısız): başlık ve blok kalkar' ($VL::RemoveSections($stock, 'TG-3') -eq '' -and -not ([IO.File]::ReadAllText($file)).Contains('pusula:plan TG-3') -and -not ([IO.File]::ReadAllText($file)).Contains('### TG-3') -and -not ([IO.File]::ReadAllText($file)).Contains("`r`n`r`n`r`n")) ([IO.File]::ReadAllText($file))
    Check 'yeniden adlandırma: işaret yeni anahtarda' ($VL::RenameSections($stock, 'TG-2', 'TG-20') -eq '' -and ([IO.File]::ReadAllText($file)).Contains('%% pusula:plan TG-20 %%') -and -not ([IO.File]::ReadAllText($file)).Contains('%% pusula:plan TG-2 %%'))
    $i2 = Item 'TG-2'; $i2.TgNo = 'TG-20'; $VL::Update($stock, $i2) | Out-Null
    $VL::WriteSections($stock, (Item 'TG-20'), $VL::Read($stock)) | Out-Null
    $text = [IO.File]::ReadAllText($file)
    Check 'başlık da yeni anahtarla, tek bölüm' ($text.Contains("### TG-20 · İki`r`n%% pusula:plan TG-20 %%") -and ([regex]::Matches($text, 'pusula:plan TG-20')).Count -eq 1 -and -not $text.Contains('### TG-2 ')) $text
    Check 'tablo sağlam' (@($VL::Read($stock)).Count -eq 3)

    "--- Plan ve Rapor tabloları (üç tablo)"
    $PR = [Pusula.Core.Testing.PlanReportList]
    $stock2 = Join-Path $tmp 'AB-2'; New-Item -ItemType Directory -Force $stock2 | Out-Null
    foreach ($x in @(@('DGÖ-1','Hüzme'), @('DGÖ-2','Yan lob'), @('DGÖ-3','VSWR'))) { $i = New-Object Pusula.Core.Testing.VerificationItem; $i.TgNo = $x[0]; $i.Text = $x[1]; $VL::Add($stock2, $i, $template) | Out-Null }
    # Eski düzen: gereksinim satırlarında TP / TR
    $rows = $VL::Read($stock2)
    $rows[0].TpNo = '4.1'; $rows[0].TpDescription = 'SATIMO'; $rows[0].Method = 'Test'; $rows[0].TrNo = '5.1'; $rows[0].TrResult = 'Geçti'
    $rows[1].TpNo = '4.1'; $rows[1].TrNo = '5.1'
    $rows[2].TpNo = '4.3'; $rows[2].Method = 'Test'; $rows[2].Facility = 'VNA'
    foreach ($r in $rows) { $VL::Update($stock2, $r) | Out-Null }
    $file2 = $VL::PathFor($stock2)
    [IO.File]::AppendAllText($file2, "`r`nElle Sonuç altında yazı.`r`n")
    Check 'taşıma: plan / rapor tablosu kurulur' ($PR::MigrateFromRequirements($stock2, $VL::Read($stock2)))
    $plans = $PR::ReadPlans($stock2); $reports = $PR::ReadReports($stock2)
    Check 'aynı TP tek plan maddesi, gereksinimleri toplanır' ($plans.Count -eq 2 -and ($plans[0].TpNo -eq '4.1') -and (($plans[0].Requirements) -join ',') -eq 'DGÖ-1,DGÖ-2' -and $plans[0].Description -eq 'SATIMO' -and $plans[1].Facility -eq 'VNA') (@($plans | ForEach-Object { $_.TpNo + ':' + ($_.Requirements -join '+') }) -join ' ')
    Check 'rapor: 5.1 Geçti, plan 4.1' ($reports.Count -eq 1 -and $reports[0].TrNo -eq '5.1' -and $reports[0].Result -eq 'Geçti' -and ($reports[0].Plans -join ',') -eq '4.1')
    $text = [IO.File]::ReadAllText($file2)
    Check 'md: ## Plan altında tablo, rapor eski ## Sonuç altında; elle yazı korunur' ($text -like '*## Plan*| TP no | Açıklama | Yöntem | Altyapı | Gereksinimler | Not |*| 4.1 | SATIMO | Test |  | DGÖ-1, DGÖ-2 |  |*## Sonuç*| TR no | Açıklama | Sonuç | Plan | Sonuç yeri | Not |*| 5.1 |  | Geçti | 4.1 |  |  |*Elle Sonuç altında yazı.*') $text
    Check 'ikinci taşıma yapılmaz' (-not $PR::MigrateFromRequirements($stock2, $VL::Read($stock2)))
    # Düzenleme + tanınmayan sütun korunur
    $text = $text.Replace('| TP no | Açıklama | Yöntem | Altyapı | Gereksinimler | Not |', '| TP no | Açıklama | Yöntem | Altyapı | Gereksinimler | Not | Sorumlu |').Replace('|---|---|---|---|---|---|', '|---|---|---|---|---|---|---|').Replace('| 4.1 | SATIMO | Test |  | DGÖ-1, DGÖ-2 |  |', '| 4.1 | SATIMO | Test |  | DGÖ-1, DGÖ-2 |  | Ali |')
    [IO.File]::WriteAllText($file2, $text, (New-Object System.Text.UTF8Encoding($true)))
    $plans = $PR::ReadPlans($stock2)
    $plans[0].Requirements.Add('DGÖ-3'); $new = New-Object Pusula.Core.Testing.PlanItem; $new.TpNo = '4.5'; $new.Description = 'Kütle'; $new.Method = 'Muayene'; $plans.Add($new)
    Check 'kaydedilir' ($PR::SavePlans($stock2, $plans) -eq '')
    $text = [IO.File]::ReadAllText($file2)
    Check 'tanınmayan sütun (Sorumlu) korunur, yeni madde eklenir' ($text -like '*| 4.1 | SATIMO | Test |  | DGÖ-1, DGÖ-2, DGÖ-3 |  | Ali |*| 4.5 | Kütle | Muayene |  |  |  |  |*') $text
    Check 'gereksinim tablosu sağlam' (@($VL::Read($stock2)).Count -eq 3)
} finally { Remove-Item -Recurse -Force $tmp -ErrorAction SilentlyContinue }
""
"SONUC: $ok basarili, $fail hata"
