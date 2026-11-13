param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
. (Join-Path $PSScriptRoot '..\_Ortak.ps1')
# Bahsedildiği yerler: notlardaki [[…]] bağlantıları ve düz numaralar; kaydın kendi klasörü, gizli/silinen/arşiv klasörleri ve kod blokları sayılmaz.
function W([string]$path, [string]$text) { New-Item -ItemType Directory -Force (Split-Path $path) | Out-Null; [IO.File]::WriteAllText($path, $text, (New-Object Text.UTF8Encoding $false)) }
$MI = [Pusula.Core.Notes.MentionIndex]

"--- 1. Satır çözümleme"
$i = New-Object Pusula.Core.Notes.MentionIndex
$i.AddText('C:\k\a.md', "---`ntarih: 2026-10-01`n---`n# Başlık`nÖlçümde [[JIG-1000-0009|SATIMO plakası]] kullanıldı`nJT-0001-0002 ile bağlandı, AB-7000-0003 için`n``````n JIG-1000-0009 kod içinde`n``````n## Günlük`n`n### 05.11.2026 · Toplantı · Jig`n[[JIG-1000-0009]] Rev AC konuşuldu`n- [ ] [[JIG-1000-0014]] tork ayarı`n[[7000_MILKON]] proje notu`n", [datetime]'2026-11-09')
$j = $i.Find([string[]]@('JIG-1000-0009'), '')
Check 'bağlantı ve takma ad: 2 satır (kod bloğu sayılmaz)' ($j.Count -eq 2) ([string]$j.Count)
Check 'günlük olayı tarihli, yeni üstte' ($j[0].Date -eq [datetime]'2026-11-05' -and $j[1].Date -eq [datetime]'2026-10-01') (($j | % { $_.Date.ToString('dd.MM') }) -join ',')
Check 'takma ad gösterilir' ($j[1].Snippet -eq 'Ölçümde SATIMO plakası kullanıldı' -and $j[1].IsLink) $j[1].Snippet
$t = $i.Find([string[]]@('JT-0001-0002'), '')
Check 'düz numara bulunur, bağlantı değil' ($t.Count -eq 1 -and -not $t[0].IsLink)
Check 'görev maddesi sadeleşir' (($i.Find([string[]]@('JIG-1000-0014'), ''))[0].Snippet -eq 'JIG-1000-0014 tork ayarı') (($i.Find([string[]]@('JIG-1000-0014'), ''))[0].Snippet)
Check 'proje bağlantısı (klasör adı)' (($i.Find([string[]]@('7000_MILKON'), '')).Count -eq 1)
Check 'aynı satır iki anahtarla bir kez' (($i.Find([string[]]@('JT-0001-0002','AB-7000-0003'), '')).Count -eq 1)
Check 'kendi klasörü hariç' (($i.Find([string[]]@('JIG-1000-0009'), 'C:\k')).Count -eq 0)
Check 'benzer ama farklı numara eşleşmez' (($i.Find([string[]]@('JIG-1000-000'), '')).Count -eq 0)

"--- 2. Kök tarama"
$r = Join-Path $env:TEMP ('pusula-bahis-' + [guid]::NewGuid().ToString('N').Substring(0,6))
try {
    W "$r\7000_X\AB-7000-0001\AB-7000-0001.md" "Kullanılan: [[JIG-1000-0009]]"
    W "$r\ENVANTER\Jigler\JIG-1000-0009_Plaka\JIG-1000-0009.md" "# JIG-1000-0009 kendi notu`n[[JIG-1000-0009]]"
    W "$r\.obsidian\x.md" "[[JIG-1000-0009]]"
    W "$r\_SILINENLER\2026\eski.md" "[[JIG-1000-0009]]"
    W "$r\7000_X\AB-7000-0001\_TAKIP\n.md" "[[JIG-1000-0009]]"
    W "$r\Notlar\toplanti.md" "JIG-1000-0009 ve [[AB-7000-0001]]"
    $idx = $MI::Build($r, [Threading.CancellationToken]::None)
    $m = $idx.Find([string[]]@('JIG-1000-0009'), "$r\ENVANTER\Jigler\JIG-1000-0009_Plaka")
    Check 'gizli, silinen, takip ve kendi notu sayılmaz' ($m.Count -eq 2) (($m | % { $_.Path.Substring($r.Length) }) -join ',')
    Check 'not sayısı' ($idx.NoteCount -eq 3) ([string]$idx.NoteCount)
} finally { Remove-Item -Recurse -Force $r -ErrorAction SilentlyContinue }

"--- 3. Arayüz"
W "$Data\7000_MILKON\AB-7000-0003\03_Test_Sonuclari\SATIMO\SN01_T1\test.md" "---`ntarih: 2026-10-05`nsonuc: Geçti`n---`n[[JIG-1000-0009]] ile SATIMO'ya bağlandı; JT-0001-0001 kablosu.`n"
W "$Data\7000_MILKON\7000_MILKON.md" "# MILKON`n`n## Günlük`n`n### 06.11.2026 · Karar · Jig`n[[JIG-1000-0009]] Rev AC onaylandı, [[AB-7000-0003]] için.`n"
[System.Threading.SynchronizationContext]::SetSynchronizationContext((New-Object System.Windows.Threading.DispatcherSynchronizationContext))
$app = New-Object Pusula.Wpf.App; $app.InitializeComponent(); $app.ShutdownMode = 'OnExplicitShutdown'; [Pusula.Wpf.Themes]::Apply([Pusula.Wpf.Themes]::Find('FluentDark'))
$win = New-Object Pusula.Wpf.MainWindow
$win.WindowStartupLocation = 'Manual'; $win.Left = -20000; $win.Top = -20000; $win.ShowActivated = $false; $win.Width = 1360; $win.Height = 900
$win.Show(); Pump
$vm = $win.DataContext; $vm.RootPath = $Data; Wait
$jig = Join-Path $Data 'ENVANTER\Jigler\JIG-1000-0009_SATIMO_adaptor_plakasi'
$vm.SelectNodeByPath($jig); Wait; Wait
$sources = @($vm.Mentions | ForEach-Object Source)
Check 'jig sayfası: test notu ve proje günlüğü' ($vm.HasMentions -and ($sources -match 'SN01_T1').Count -ge 1 -and ($sources -match '7000_MILKON').Count -ge 1) ($sources -join ' | ')
Check 'başlık sayılı' ($vm.MentionsHeader -match '· \d+$') $vm.MentionsHeader
Check 'yeniden eskiye' ((@($vm.Mentions | % Date) | Sort-Object -Descending) -join ',' -eq (@($vm.Mentions | % Date) -join ','))
Snap $win "$Out\mentions-jig.png"
$stock = Join-Path $Data '7000_MILKON\AB-7000-0003'
$vm.SelectNodeByPath($stock); Wait; Wait
Check 'stok sayfası: projenin günlüğünde bahis var, kendi test notu sayılmaz' (@($vm.Mentions | Where-Object { $_.Path -like "$stock\*" }).Count -eq 0 -and @($vm.Mentions | Where-Object { $_.Source -match '7000_MILKON' }).Count -eq 1) ((@($vm.Mentions) | % Source) -join ' | ')
Snap $win "$Out\mentions-stock.png"
$vm.SelectNodeByPath((Join-Path $Data '7000_MILKON\AB-7000-0001')); Wait; Wait
Check 'bahsi olmayan stokta kart gizli' (-not $vm.HasMentions)
"--- 4. Yeni bahis yenileyince gelir"
W "$Data\Notlar\yeni.md" "AB-7000-0001 için numune geldi"
$vm.RefreshCommand.Execute($null); Wait
$vm.SelectNodeByPath((Join-Path $Data '7000_MILKON\AB-7000-0001')); Wait; Wait
Check 'yenilemeden sonra yeni bahis' ($vm.HasMentions -and @($vm.Mentions)[0].Snippet -match 'numune')
"SONUÇ OK=$ok HATA=$fail"
