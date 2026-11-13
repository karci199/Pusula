param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
. (Join-Path $PSScriptRoot '..\_Ortak.ps1')
# Test tarihi / analiz tarihi: varsayılan aynı; test.md'de analiz_tarihi ve analiz_notu; uygulamadan "Analiz…" ile yazılır.
function W([string]$path, [string]$text) { New-Item -ItemType Directory -Force (Split-Path $path) | Out-Null; [IO.File]::WriteAllText($path, $text, (New-Object Text.UTF8Encoding $false)) }
$R = [Pusula.Core.Testing.TestRunReader]
function Run($runs, $serial, $system) { $runs | Where-Object { $_.SerialNo -eq $serial -and $_.System -eq $system } | Select-Object -First 1 }
function Body($path) { $t = [IO.File]::ReadAllText($path); $i = $t.IndexOf("`n---", 4); $t.Substring($i + 4) }

"--- 1. Okuma"
$s = Join-Path $env:TEMP ('pusula-analiz-' + [guid]::NewGuid().ToString('N').Substring(0,6))
try {
    W "$s\03_Test_Sonuclari\SATIMO\SN01_T1\test.md" "---`ntarih: 2026-10-05`nsonuc: Geçti`n---`nGövde 1`n"
    W "$s\03_Test_Sonuclari\SATIMO\SN02_T1\test.md" "---`ntarih: 2026-10-05`nanaliz_tarihi: 2026-10-09`nanaliz_notu: Yan lob düzeltildi`nsonuc: Kaldı`n---`nGövde 2`n"
    W "$s\03_Test_Sonuclari\SATIMO\SN03_T1\test.md" "---`ntarih: 2026-10-05`nanaliz_tarihi: 2026-10-05`n---`n"
    W "$s\03_Test_Sonuclari\VNA\SN01_T1\Analiz\x\_kosu.md" "**Genel sonuç: GEÇTİ**`n`n| Başlangıç | 2026-11-08T11:53:41+03:00 |`n"
    W "$s\03_Test_Sonuclari\VNA\SNP1_T1\Ham\olcum.s1p" "veri"
    $runs = $R::ReadStock($s)
    $a = Run $runs 'SN01' 'SATIMO'; $b = Run $runs 'SN02' 'SATIMO'; $c = Run $runs 'SN03' 'SATIMO'
    Check 'yalnız test tarihi: analiz aynı gün' ($a.AnalysisDate -eq $a.Date -and -not $a.HasSeparateAnalysis)
    Check 'ayrı analiz tarihi ve notu' ($b.HasSeparateAnalysis -and $b.AnalysisDate -eq [datetime]'2026-10-09' -and $b.AnalysisNote -eq 'Yan lob düzeltildi' -and $b.Date -eq [datetime]'2026-10-05')
    Check 'analiz tarihi test günüyle aynı: ayrı sayılmaz' (-not $c.HasSeparateAnalysis)
    $k = Run $runs 'SN01' 'VNA'
    Check 'KIWI koşusu: analiz = test' ($k.AnalysisDate -eq $k.Date -and -not $k.HasSeparateAnalysis)
    $p = Run $runs 'SNP1' 'VNA'
    Check 'notsuz koşu: analiz = klasör tarihi' ($p.AnalysisDate -eq $p.Date)

    "--- 2. Yazma"
    $err = $R::SetAnalysis($a.Folder, $a.Date, [datetime]'2026-10-12', 'Ölçüm tekrarlandı: #2')
    $a2 = Run ($R::ReadStock($s)) 'SN01' 'SATIMO'
    Check 'ayrı analiz yazıldı' ($err -eq '' -and $a2.HasSeparateAnalysis -and $a2.AnalysisDate -eq [datetime]'2026-10-12' -and $a2.AnalysisNote -eq 'Ölçüm tekrarlandı: #2') ($err + ' ' + $a2.AnalysisNote)
    Check 'test tarihi ve sonuç değişmedi' ($a2.Date -eq [datetime]'2026-10-05' -and $a2.Result -eq 'Passed')
    Check 'gövdeye dokunulmadı' ((Body "$($a.Folder)\test.md") -match '^\r?\nGövde 1')
    $R::SetAnalysis($a.Folder, $a.Date, [datetime]'2026-10-05', 'aynı gün') | Out-Null
    $a3 = Run ($R::ReadStock($s)) 'SN01' 'SATIMO'
    Check 'test gününe çekilince analiz_tarihi kalkar, not kalır' (-not $a3.HasSeparateAnalysis -and $a3.AnalysisNote -eq 'aynı gün' -and -not ([IO.File]::ReadAllText("$($a.Folder)\test.md") -match 'analiz_tarihi'))
    $R::SetAnalysis($b.Folder, $b.Date, $null, '') | Out-Null
    $b2 = Run ($R::ReadStock($s)) 'SN02' 'SATIMO'
    Check 'boş bırakılınca ikisi de kalkar' (-not $b2.HasSeparateAnalysis -and $b2.AnalysisNote -eq '' -and $b2.Result -eq 'Failed')
    $R::SetAnalysis($k.Folder, $k.Date, [datetime]'2026-11-10', 'KIWI sonrası') | Out-Null
    $k2 = Run ($R::ReadStock($s)) 'SN01' 'VNA'
    Check 'KIWI koşusu: test.md oluştu, KIWI sonucu ve tarihi korundu' ((Test-Path "$($k.Folder)\test.md") -and $k2.Result -eq 'Passed' -and $k2.Date.Date -eq [datetime]'2026-11-08' -and $k2.HasSeparateAnalysis) ([string]$k2.Date + ' ' + $k2.Result)
    $R::SetAnalysis($p.Folder, $p.Date, $p.Date.AddDays(3), '') | Out-Null
    $p2 = Run ($R::ReadStock($s)) 'SNP1' 'VNA'
    Check 'notsuz koşu: test.md test tarihiyle oluştu, analiz 3 gün sonra' ($p2.Date.Date -eq $p.Date.Date -and ($p2.AnalysisDate - $p2.Date).Days -eq 3) ([string]$p2.Date + ' / ' + [string]$p2.AnalysisDate)
} finally { Remove-Item -Recurse -Force $s -ErrorAction SilentlyContinue }

"--- 3. Arayüz"
[System.Threading.SynchronizationContext]::SetSynchronizationContext((New-Object System.Windows.Threading.DispatcherSynchronizationContext))
$app = New-Object Pusula.Wpf.App; $app.InitializeComponent(); $app.ShutdownMode = 'OnExplicitShutdown'; [Pusula.Wpf.Themes]::Apply([Pusula.Wpf.Themes]::Find('FluentDark'))
$win = New-Object Pusula.Wpf.MainWindow
$win.WindowStartupLocation = 'Manual'; $win.Left = -20000; $win.Top = -20000; $win.ShowActivated = $false; $win.Width = 1360; $win.Height = 860
$win.Show(); Pump
$vm = $win.DataContext; $vm.RootPath = $Data; Wait
$stock = Join-Path $Data '7000_MILKON\AB-7000-0003'
$vm.SelectNodeByPath($stock); Wait
$row = @($vm.Detail.DevelopmentRuns)[0]
Check 'pencere açılır, tarih test tarihiyle dolu' ($vm.OpenAnalysis($row) -and $vm.IsAnalysisOpen -and $vm.AnalysisDateText -eq $row.Date.ToString('dd.MM.yyyy')) $vm.AnalysisDateText
$vm.AnalysisDateText = '32.13.2026'
Check 'hatalı tarih: kaydedilmez, hata yazar' (-not $vm.ConfirmAnalysis() -and $vm.AnalysisError.Length -gt 0)
$later = $row.Date.AddDays(2)
$vm.AnalysisDateText = $later.ToString('d.M.yyyy'); $vm.AnalysisNote = 'Sonradan analiz edildi'
Check 'kaydedildi' ($vm.ConfirmAnalysis() -and -not $vm.IsAnalysisOpen); Wait
$row2 = @($vm.Detail.DevelopmentRuns | Where-Object { $_.Folder -eq $row.Folder })[0]
Check 'tabloda Analiz sütunu dolu' ($row2.AnalysisText -eq $later.ToString('dd.MM.yyyy')) $row2.AnalysisText
$card = @($vm.Detail.Timeline | Where-Object { $_.Folder -eq $row.Folder })[0]
Check 'günlük kartında analiz tarihi ve notu' ($card.Meta -match 'analiz ' -and $card.Detail -match 'Sonradan analiz edildi') ($card.Meta + ' | ' + $card.Detail)
Check 'seçim korundu' ($vm.SelectedNode.Path -eq $stock)
$tc = $win.FindName('StockTabs'); $tc.SelectedIndex = 1; Snap $win "$Out\analysis-runs.png"
$vm.OpenAnalysis($row2) | Out-Null
Check 'yeniden açınca kayıtlı analiz tarihi gelir' ($vm.AnalysisDateText -eq $later.ToString('dd.MM.yyyy') -and $vm.AnalysisNote -eq 'Sonradan analiz edildi')
$vm.AnalysisDateText = ''; $vm.AnalysisNote = ''
Check 'boşaltınca kalkar' ($vm.ConfirmAnalysis() -and (Wait) -eq $null -and (@($vm.Detail.DevelopmentRuns | Where-Object { $_.Folder -eq $row.Folder })[0].AnalysisText -eq ''))
"SONUÇ OK=$ok HATA=$fail"
