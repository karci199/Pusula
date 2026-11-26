param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
. (Join-Path $PSScriptRoot '..\_Ortak.ps1')
# 26.11.2026: Test tipi gruba katıldı (İşlevsel, Özellik, ÇK, EMC, Diğer), Alt grup kalktı. Eski dosyada bir kez:
# Tip gruba geçer, tanınmayan grup (Excel başlığı) Diğer olur, Tip ve Alt grup sütunları kalkar; önce _ARSIV yedeği.
$VL = [Pusula.Core.Testing.VerificationList]
$PR = [Pusula.Core.Testing.PlanReportList]
$utf8 = New-Object System.Text.UTF8Encoding($true)

"--- GroupFor kuralı"
Check 'gruplar: İşlevsel, Özellik, ÇK, EMC, Diğer' (($VL::Groups -join ',') -eq 'İşlevsel,Özellik,ÇK,EMC,Diğer')
Check 'Excel başlığı + Tip ÇK → ÇK' ($VL::GroupFor('3.1 Elektriksel özellikler', 'ÇK') -eq 'ÇK')
Check 'Excel başlığı, Tip yok → Diğer' ($VL::GroupFor('3.2 Fiziksel özellikler', '') -eq 'Diğer')
Check 'ÇK/EMC + Tip EMC → EMC' ($VL::GroupFor('ÇK/EMC', 'EMC') -eq 'EMC')
Check 'ÇK/EMC, Tip yok → olduğu gibi (kullanıcı seçer)' ($VL::GroupFor('ÇK/EMC', '') -eq 'ÇK/EMC')
Check 'Özellik + Tip İşlevsel → Özellik kalır (tanınan grup ezilmez)' ($VL::GroupFor('Özellik', 'İşlevsel') -eq 'Özellik')
Check 'boş grup + Tip EMC → EMC; ikisi boş → boş' ($VL::GroupFor('', 'EMC') -eq 'EMC' -and $VL::GroupFor('', '') -eq '')
Check 'ÇK / EMC standart grubu' ($VL::IsStandardGroup('ÇK') -and $VL::IsStandardGroup('emc') -and -not $VL::IsStandardGroup('İşlevsel'))

$tmp = Join-Path $env:TEMP ('pusula-grp-' + [guid]::NewGuid().ToString('N').Substring(0,6))
try {
    "--- Eski dosya: Alt grup ve Tip sütunları"
    $stock = Join-Path $tmp 'AB-7000-0003'
    New-Item -ItemType Directory -Force (Join-Path $stock '03_Testler') | Out-Null
    $h = '| # | TG no | Kaynak | Başlık | Kısa not | Gereksinim | Grup | Alt grup | Tür | Bağlı | Yöntem | Altyapı | Tip | Standart | Standart yöntemi | Prosedür | Durum | Elle durum |'
    $s = '|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|'
    $lines = @('# Test gereksinimleri', '', $h, $s,
        '| 1 | TG-1 |  | Gerilim |  |  | 3.1 Elektriksel özellikler |  | Temel |  |  |  |  |  |  |  | Bekliyor |  |',
        '| 2 | TG-2 |  | Sıcaklık |  |  | 3.1 Elektriksel özellikler | Anten örüntü | Temel |  |  |  | ÇK | MIL-STD-810G | 501.5 · Yüksek sıcaklık |  | Bekliyor |  |',
        '| 3 | TG-3 |  | Işıma |  |  | ÇK/EMC |  |  |  |  |  | EMC | MIL-STD-461G |  |  | Bekliyor |  |',
        '| 4 | TG-4 |  | Etiket |  |  | Özellik |  |  |  | 261 dokümanı |  |  |  |  |  | Bekliyor |  |',
        '', 'Elle yazılmış son satır.', '')
    $file = Join-Path $stock '03_Testler\AB-7000-0003_Test_Gereksinimleri.md'
    $original = $lines -join "`r`n"
    [IO.File]::WriteAllText($file, $original, $utf8)
    $err = ''
    Check 'geçiş yapılır' ($PR::RetireLegacy($stock, [ref]$err)) $err
    $text = [IO.File]::ReadAllText($file)
    $backups = @(Get-ChildItem (Join-Path $stock '_ARSIV') -Filter '*.md.yedek' -ErrorAction SilentlyContinue)
    Check 'yedek alınır, içerik aynı' ($backups.Count -eq 1 -and [IO.File]::ReadAllText($backups[0].FullName) -eq $original) ([string]$backups.Count)
    Check 'Alt grup ve Tip sütunları kalktı' ($text -notlike '*Alt grup*' -and $text -notlike '*| Tip |*') $text
    Check 'başlık yeni sıra' ($text.Contains('| # | TG no | Kaynak | Başlık | Kısa not | Gereksinim | Grup | Tür | Bağlı | Yöntem | Altyapı | Standart | Standart yöntemi | Prosedür | Durum | Elle durum |')) $text
    $items = $VL::Read($stock)
    Check 'gruplar: Diğer, ÇK, EMC, Özellik' ((($items | ForEach-Object Group) -join ',') -eq 'Diğer,ÇK,EMC,Özellik') (($items | ForEach-Object Group) -join ',')
    Check 'standart, yöntem ve 261 korunur' ($items[1].Standard -eq 'MIL-STD-810G' -and $items[1].StandardMethod -eq '501.5 · Yüksek sıcaklık' -and $items[3].Method -eq '261 dokümanı')
    Check 'dosyanın geri kalanı aynı' ($text.StartsWith("# Test gereksinimleri`r`n`r`n") -and $text.Contains("`r`n`r`nElle yazılmış son satır."))
    Check 'ikinci kez bir şey yapılmaz' (-not $PR::RetireLegacy($stock, [ref]$err) -and @(Get-ChildItem (Join-Path $stock '_ARSIV')).Count -eq 1)

    "--- Yeni madde eklenince Alt grup / Tip yazılmaz"
    $it = New-Object Pusula.Core.Testing.VerificationItem; $it.TgNo = 'TG-5'; $it.Text = 'Yeni'; $it.Group = 'EMC'
    Check 'eklenir' ($VL::Add($stock, $it, '') -eq '')
    $text = [IO.File]::ReadAllText($file)
    Check 'satır 16 hücre' ($text.Contains('| 5 | TG-5 |  | Yeni |  |  | EMC |  |  |  |  |  |  |  |  |  |')) $text
}
finally {
    Remove-Item -Recurse -Force $tmp -ErrorAction SilentlyContinue
}
