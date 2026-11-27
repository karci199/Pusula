param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
. (Join-Path $PSScriptRoot '..\_Ortak.ps1')
# 27.11.2026: iş raporu. Ana sayfa Günlük'te dönem (Bugün / Bu hafta / Bu ay / Bu yıl / Hepsi) ve özet satırı;
# "İş raporu" seçilen dönemi (gün / hafta / ay / yıl / tüm zaman) Excel'e yazar: İşler (süzgeçli) + Özet sayfası.
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

"--- İş raporu penceresi"
$vm.IsRangeAll = $true; Wait
$vm.OpenWorkReportCommand.Execute($null); Wait
Check 'pencere açık, dönem ana sayfadan (Hepsi → tüm zaman)' ($vm.IsWorkReportOpen -and $vm.IsReportAll -and -not $vm.IsReportDateVisible)
Check 'önizleme kayıt sayısı' ($vm.ReportPreview -like ('* · ' + $all + ' kayıt')) $vm.ReportPreview
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
    Check 'başlık satırında süzgeç (proje / stok)' ($s1.Contains('<autoFilter ref="B10:H') -and $s1.Contains('>Proje<') -and $s1.Contains('>Stok<'))
    Check 'satır sayısı = kayıt' (([regex]::Matches($s1, '<row r="(\d+)"') | Where-Object { [int]$_.Groups[1].Value -ge 11 -and [int]$_.Groups[1].Value -lt 11 + $all }).Count -eq $all)
    Check 'özet kutuları süzgece göre (SUBTOTAL)' ($s1.Contains('<f>SUBTOTAL(103,'))
    Check 'özet sayfası formülle (COUNTIFS)' ($s2.Contains('COUNTIFS(&apos;İşler&apos;!'))
    Check 'proje adı yazılır' ($s1.Contains('>7000_MILKON<'))

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

# Pencerenin resmi
$vm.IsReportWeek = $true; $vm.WorkReportDate = [datetime]::Today; Wait
$d = $null; $st = New-Object System.Collections.Stack; $st.Push($win)
while ($st.Count) { $e = $st.Pop(); if ($e -is [DesignKit.Controls.ContentDialog] -and $e.IsOpen) { $d = $e }; for ($i = 0; $i -lt [System.Windows.Media.VisualTreeHelper]::GetChildrenCount($e); $i++) { $st.Push([System.Windows.Media.VisualTreeHelper]::GetChild($e, $i)) } }
if ($d) {
    $child = $d.Template.FindName('PART_Popup', $d).Child
    $child.Measure((New-Object System.Windows.Size 1360, 900)); $child.Arrange((New-Object System.Windows.Rect 0, 0, 1360, 900)); $child.UpdateLayout(); Wait
    $rtb = New-Object System.Windows.Media.Imaging.RenderTargetBitmap 2040, 1350, 144, 144, ([System.Windows.Media.PixelFormats]::Pbgra32)
    $dv = New-Object System.Windows.Media.DrawingVisual; $dc = $dv.RenderOpen()
    $dc.DrawRectangle((New-Object System.Windows.Media.VisualBrush $win), $null, (New-Object System.Windows.Rect 0, 0, 1360, 900))
    $dc.DrawRectangle((New-Object System.Windows.Media.VisualBrush $child), $null, (New-Object System.Windows.Rect 0, 0, 1360, 900))
    $dc.Close(); $rtb.Render($dv)
    $pe = New-Object System.Windows.Media.Imaging.PngBitmapEncoder; $pe.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($rtb))
    $fs = [IO.File]::Create("$Out\work-report-dialog.png"); $pe.Save($fs); $fs.Close()
}
$vm.IsWorkReportOpen = $false; Wait

$win.Close()
