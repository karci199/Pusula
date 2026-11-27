param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
. (Join-Path $PSScriptRoot '..\_Ortak.ps1')
# 27.11.2026: iş raporu. Ana sayfa Günlük'te dönem (Bugün / Bu hafta / Bu ay / Bu yıl / Hepsi) ve özet satırı;
# "İş raporu" Rapor sekmesini açar (dönem, proje / stok süzgeci, tablo); seçilen dönemi (gün / hafta / ay / yıl / tüm zaman) Excel'e yazar: İşler (süzgeçli) + Özet sayfası.
# Durum isteğe bağlı. Stok sayfasının ⋯ menüsünde "Modül ekle".
Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem
$WR = [Pusula.Core.Office.WorkReport]
$P = [Pusula.Core.Office.ReportPeriod]

"--- Dönem hesabı"
$from = $null; $to = $null
$WR::RangeOf($P::Week, [datetime]'2026-11-27', [ref]$from, [ref]$to)
Check 'hafta pazartesi–pazar' ($from -eq [datetime]'2026-11-23' -and $to -eq [datetime]'2026-11-30') ([string]$from + ' ' + [string]$to)
$WR::RangeOf($P::Month, [datetime]'2026-11-27', [ref]$from, [ref]$to)
Check 'ay' ($from -eq [datetime]'2026-11-01' -and $to -eq [datetime]'2026-12-01')
$WR::RangeOf($P::Year, [datetime]'2026-11-27', [ref]$from, [ref]$to)
Check 'yıl' ($from -eq [datetime]'2026-01-01' -and $to -eq [datetime]'2027-01-01')
$WR::RangeOf($P::All, [datetime]'2026-11-27', [ref]$from, [ref]$to)
Check 'tüm zaman sınırsız' ($from -eq $null -and $to -eq $null)
Check 'ISO hafta 48 / 1' ($WR::IsoWeek([datetime]'2026-11-27') -eq 48 -and $WR::IsoWeek([datetime]'2026-01-01') -eq 1 -and $WR::IsoWeek([datetime]'2027-01-01') -eq 53)
Check 'dosya adları' ($WR::FileName('Is_raporu', $P::Week, [datetime]'2026-11-27', 'tum') -eq 'Is_raporu_2026-H48.xlsx' -and $WR::FileName('Is_raporu', $P::All, [datetime]'2026-11-27', 'tum') -eq 'Is_raporu_tum.xlsx')

[System.Threading.SynchronizationContext]::SetSynchronizationContext((New-Object System.Windows.Threading.DispatcherSynchronizationContext))
$app = New-Object Pusula.Wpf.App; $app.InitializeComponent(); $app.ShutdownMode = 'OnExplicitShutdown'
$win = New-Object Pusula.Wpf.MainWindow
$win.WindowStartupLocation = 'Manual'; $win.Left = -20000; $win.Top = -20000; $win.ShowActivated = $false; $win.Width = 1360; $win.Height = 900
$win.Show(); Pump
$vm = $win.DataContext; $vm.RootPath = $Data; Wait; Wait

"--- Ana sayfa Günlük: Bu yıl ve özet"
$vm.HomeTab = 0; $vm.IsRangeAll = $true; Wait
$all = @($vm.HomeTimeline | ForEach-Object { $_.Cards }).Count
Check 'Hepsi: kayıt var' ($all -gt 0) ([string]$all)
Check 'özet satırı: kayıt · proje · stok' ($vm.HomeJournalSummary -like ([string]$all + ' kayıt · * proje · * stok*')) $vm.HomeJournalSummary
$vm.IsRangeYear = $true; Wait
Check 'Bu yıl seçilir' ($vm.IsRangeYear -and -not $vm.IsRangeAll)
Check 'İş raporu düğmesi Günlük sekmesinde' ($vm.IsHomeJournalTab)

"--- Rapor sekmesi"
$vm.IsRangeAll = $true; Wait
$vm.OpenWorkReportCommand.Execute($null); Wait
Check 'İş raporu → Rapor sekmesi, dönem ana sayfadan (Hepsi → tüm zaman)' ($vm.HomeTab -eq 4 -and $vm.IsHomeReportTab -and $vm.IsReportAll -and -not $vm.IsReportDateVisible)
Check 'tabloda bütün kayıtlar' ($vm.WorkReportLines.Count -eq $all) ([string]$vm.WorkReportLines.Count)
Check 'ilk kutu Kayıt' ($vm.WorkReportTiles[0].Count -eq $all -and $vm.WorkReportTiles.Count -ge 2)
Check 'önizleme kayıt sayısı' ($vm.ReportPreview -like ('* · ' + $all + ' kayıt')) $vm.ReportPreview
Check 'proje / stok seçenekleri' ($vm.ReportProjects.Count -ge 2 -and $vm.ReportProjects[0] -eq 'Tüm projeler' -and $vm.ReportStocks[0] -eq 'Tüm stoklar')
$vm.ReportProject = $vm.ReportProjects[1]; Wait
Check 'proje süzgeci' ($vm.WorkReportLines.Count -gt 0 -and @($vm.WorkReportLines | Where-Object { $_.Project -ne $vm.ReportProjects[1] }).Count -eq 0)
$stock = $vm.ReportStocks[1]
$vm.ReportStock = $stock; Wait
Check 'stok süzgeci' ($vm.WorkReportLines.Count -gt 0 -and @($vm.WorkReportLines | Where-Object { $_.Stock -ne $stock }).Count -eq 0) $stock
$vm.ReportProject = 'Tüm projeler'; Wait
Check 'Tüm projeler: stok da sıfırlanır' ($vm.ReportStock -eq 'Tüm stoklar' -and $vm.WorkReportLines.Count -eq $all)
$tmp = Join-Path $env:TEMP ('pusula-rapor-' + [guid]::NewGuid().ToString('N').Substring(0,6))
New-Item -ItemType Directory $tmp | Out-Null
try {
    $file = Join-Path $tmp $vm.WorkReportFileName
    Check 'tüm zaman yazılır' ($vm.ExportWorkReport($file) -and (Test-Path $file)) $vm.Status
    $zip = [IO.Compression.ZipFile]::OpenRead($file)
    $read = { param($n) $s = New-Object IO.StreamReader($zip.GetEntry($n).Open()); $x = $s.ReadToEnd(); $s.Close(); $x }
    $wb = & $read 'xl/workbook.xml'; $s1 = & $read 'xl/worksheets/sheet1.xml'; $s2 = & $read 'xl/worksheets/sheet2.xml'
    $zip.Dispose()
    Check 'iki sayfa: İşler, Özet' ($wb.Contains('name="İşler"') -and $wb.Contains('name="Özet"'))
    Check 'başlık satırında süzgeç (proje / stok)' ($s1.Contains('<autoFilter ref="B10:I') -and $s1.Contains('>Proje<') -and $s1.Contains('>Stok<'))
    Check 'satır sayısı = kayıt' (([regex]::Matches($s1, '<row r="(\d+)"') | Where-Object { [int]$_.Groups[1].Value -ge 11 -and [int]$_.Groups[1].Value -lt 11 + $all }).Count -eq $all)
    Check 'özet kutuları süzgece göre (SUBTOTAL)' ($s1.Contains('<f>SUBTOTAL(103,'))
    Check 'özet sayfası formülle (COUNTIFS)' ($s2.Contains('COUNTIFS(&apos;İşler&apos;!'))
    Check 'proje adı yazılır' ($s1.Contains('>7000_MILKON<'))
    Check 'Açıklama sütunu var, Ayrıntı bölümü yok' ($s1.Contains('>Açıklama<') -and -not $s1.Contains('Ayrıntı'))
    Check 'arayüzde kısa açıklama' (@($vm.WorkReportLines | Where-Object { $_.HasNote }).Count -gt 0)

    $vm.IsReportYear = $true; $vm.WorkReportDate = [datetime]'2020-06-01'; Wait
    Check 'kaydı olmayan yıl: 0 kayıt' ($vm.ReportPreview -like '* · 0 kayıt') $vm.ReportPreview
    $empty = Join-Path $tmp 'bos.xlsx'
    Check 'boş rapor da yazılır' ($vm.ExportWorkReport($empty) -and (Test-Path $empty))
    $vm.IsReportWeek = $true; $vm.WorkReportDate = [datetime]'2026-11-25'; Wait
    Check 'haftalık dosya adı' ($vm.WorkReportFileName -eq 'Is_raporu_2026-H48.xlsx') $vm.WorkReportFileName
}
finally {
    Remove-Item -Recurse -Force $tmp -ErrorAction SilentlyContinue
}

# Rapor sekmesinin resmi (tüm zaman)
$vm.IsReportAll = $true; Wait
Snap $win "$Out\work-report-tab.png"
$win.Close()
