param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
. (Join-Path $PSScriptRoot '..\_Ortak.ps1')
# Excel'den gereksinim içe aktarma (deneme): kütüphanesiz xlsx okuma, başlık satırları, ekleme / güncelleme.
Add-Type -AssemblyName System.IO.Compression; Add-Type -AssemblyName System.IO.Compression.FileSystem
$RI = [Pusula.Core.Testing.RequirementImport]
$VL = [Pusula.Core.Testing.VerificationList]

function New-Xlsx([string]$path, [object[]]$rows)
{
    if (Test-Path $path) { Remove-Item $path }
    $zip = [System.IO.Compression.ZipFile]::Open($path, 'Create')
    function Put([string]$name, [string]$text) { $e = $zip.CreateEntry($name); $w = New-Object IO.StreamWriter($e.Open(), (New-Object Text.UTF8Encoding($false))); $w.Write($text); $w.Close() }
    Put '[Content_Types].xml' '<?xml version="1.0" encoding="UTF-8"?><Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/><Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/></Types>'
    Put '_rels/.rels' '<?xml version="1.0" encoding="UTF-8"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/></Relationships>'
    Put 'xl/workbook.xml' '<?xml version="1.0" encoding="UTF-8"?><workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets><sheet name="Gereksinimler" sheetId="1" r:id="rId1"/></sheets></workbook>'
    Put 'xl/_rels/workbook.xml.rels' '<?xml version="1.0" encoding="UTF-8"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/></Relationships>'
    $sb = New-Object Text.StringBuilder
    [void]$sb.Append('<?xml version="1.0" encoding="UTF-8"?><worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><sheetData>')
    for ($r = 0; $r -lt $rows.Count; $r++)
    {
        [void]$sb.Append('<row r="' + ($r + 1) + '">')
        $cells = $rows[$r]
        for ($c = 0; $c -lt $cells.Count; $c++)
        {
            if ([string]$cells[$c] -eq '') { continue }
            $ref = [string][char](65 + $c) + ($r + 1)
            [void]$sb.Append('<c r="' + $ref + '" t="inlineStr"><is><t>' + [Security.SecurityElement]::Escape([string]$cells[$c]) + '</t></is></c>')
        }
        [void]$sb.Append('</row>')
    }
    [void]$sb.Append('</sheetData></worksheet>')
    Put 'xl/worksheets/sheet1.xml' $sb.ToString()
    $zip.Dispose()
}

$rows = @(
    @('Test Gereksinimleri — AB-7000-0002', '', '', ''),
    @('', '', '', ''),
    @('TG no', 'Gereksinim', 'Tür', 'Not'),
    @('DGÖ-1', '3.1 Elektriksel özellikler', '', ''),
    @('DGÖ-2', 'Anten 8–12 GHz bandında 3 dB yanca hüzme genişliği ≥ 30° olmalıdır.', 'Temel', ''),
    @('DGÖ-3', 'Yan lob seviyesi ana hüzmeye göre ≤ −15 dB olmalıdır.', 'Temel', 'E ve H düzlemi'),
    @('TSD-0042', 'Besleme konnektörü 4 Nm torkla sıkılmalıdır.', 'Türetilmiş', 'Tasarımcı dokümanı'),
    @('DGÖ-4', '3.2 Fiziksel özellikler', '', ''),
    @('DGÖ-5', 'Antenin kütlesi 2 kg''ı geçmemelidir.', 'Temel', '')
)
$tmp = Join-Path $env:TEMP ('pusula-xl-' + [guid]::NewGuid().ToString('N').Substring(0,6))
try {
    New-Item -ItemType Directory -Force $tmp | Out-Null
    $xlsx = Join-Path $tmp 'gereksinim.xlsx'
    New-Xlsx $xlsx $rows
    Copy-Item $xlsx (Join-Path $Out 'ornek_gereksinim.xlsx') -Force

    "--- okuma"
    $sheet = [Pusula.Core.Office.XlsxReader]::ReadFirstSheet($xlsx)
    Check 'xlsx okunur: 9 satır, boş satır korunur' ($sheet.Count -eq 9 -and $sheet[1].Count -eq 0 -and $sheet[2][1] -eq 'Gereksinim') ([string]$sheet.Count)
    $res = $RI::ReadExcel($xlsx)
    Check '4 madde, 2 başlık' ($res.Error -eq '' -and $res.Items.Count -eq 4 -and $res.Headings.Count -eq 2) ($res.Error + ' ' + $res.Items.Count + '/' + $res.Headings.Count)
    Check 'bölüm: DGÖ-2..TSD-0042 3.1, DGÖ-5 3.2' ($res.Items[0].Section -eq '3.1 Elektriksel özellikler' -and $res.Items[2].Section -eq '3.1 Elektriksel özellikler' -and $res.Items[3].Section -eq '3.2 Fiziksel özellikler')
    Check 'tür ve not' ($res.Items[2].Kind -eq 'Türetilmiş' -and $res.Items[1].Note -eq 'E ve H düzlemi')

    "--- yazma"
    $stock = Join-Path $tmp 'AB-1'; New-Item -ItemType Directory -Force (Join-Path $stock '03_Testler') | Out-Null
    $template = [string]::Format([Pusula.Wpf.Properties.Resources]::Verify_FileTemplate, 'AB-1')
    $err = ''
    $c = $RI::Apply($stock, $res.Items, 'Diğer', $template, [ref]$err)
    Check 'ilk aktarım: 4 yeni' ($err -eq '' -and $c.Item1 -eq 4 -and $c.Item2 -eq 0) ($err + ' ' + $c)
    $items = $VL::Read($stock)
    Check 'tabloda 4 madde: TG no, gereksinim, tür, not, bölüm' ($items.Count -eq 4 -and $items[1].TgNo -eq 'DGÖ-3' -and $items[1].Requirement -like 'Yan lob*' -and $items[1].Description -eq 'E ve H düzlemi' -and $items[2].Kind -eq 'Türetilmiş' -and $items[3].Group -eq '3.2 Fiziksel özellikler' -and $items[0].Group -eq '3.1 Elektriksel özellikler' -and $items[0].Text -eq '')
    $items[0].Status = 'Geçti'; $items[0].Text = 'Hüzme genişliği'; $VL::Update($stock, $items[0]) | Out-Null

    $rows[5] = @('DGÖ-3', 'Yan lob seviyesi ana hüzmeye göre ≤ −18 dB olmalıdır.', 'Temel', 'E ve H düzlemi')
    $rows += ,@('DGÖ-6', 'Anten −40 °C ile +71 °C arasında çalışmalıdır.', 'Temel', '')
    New-Xlsx $xlsx $rows
    $c = $RI::Apply($stock, $RI::ReadExcel($xlsx).Items, 'Diğer', $template, [ref]$err)
    Check 'ikinci aktarım: 1 yeni, 1 güncellendi, 3 aynı' ($c.Item1 -eq 1 -and $c.Item2 -eq 1 -and $c.Item3 -eq 3) ([string]$c)
    $items = $VL::Read($stock)
    Check 'durum ve elle başlık korunur, metin güncellenir, silme yok' ($items.Count -eq 5 -and $items[0].Status -eq 'Geçti' -and $items[0].Text -eq 'Hüzme genişliği' -and $items[1].Requirement -like '*−18 dB*')

    "--- başlık satırı yoksa"
    New-Xlsx $xlsx @(@('a', 'b'), @('1', '2'))
    Check 'başlık bulunamazsa hata' ($RI::ReadExcel($xlsx).Error -eq 'header')
} finally { Remove-Item -Recurse -Force $tmp -ErrorAction SilentlyContinue }
""
"SONUC: $ok basarili, $fail hata"
