param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
. (Join-Path $PSScriptRoot '..\_Ortak.ps1')
function W([string]$path, [string]$text) { New-Item -ItemType Directory -Force (Split-Path $path) | Out-Null; [IO.File]::WriteAllText($path, $text, (New-Object Text.UTF8Encoding $false)) }

$s = Join-Path $env:TEMP ('pusula-runs-' + [guid]::NewGuid().ToString('N').Substring(0,6))
try {
    W "$s\03_Test_Sonuclari\SATIMO\SN01_T1\test.md" "---`nsistem: SATIMO`nsn: SN01`ntur: 1`ntarih: 2026-10-05`nsonuc: Geçti`nkisi: a-demir`njig: JIG-1 Rev AB`n---`n# Baslik`n`n**Sonuç: Geçti.** Limit içinde.`n"
    W "$s\03_Test_Sonuclari\SATIMO\SN01_T2\test.md" "---`nsonuc: KALDI`ntarih: 2026-10-20`njig: JIG-1 Rev AC`n---`nKablo değişti, tekrar ölçülecek.`n"
    W "$s\03_Test_Sonuclari\SATIMO\SN02_P1\Analiz\x\_kosu.md" "# Koşu`n`n**Genel sonuç: GEÇTİ**`n`n| Başlangıç | 2026-11-08T11:53:41+03:00 |`n| Kişi | katan |`n| Plan | Hüzme · Rev 0 |`n"
    W "$s\03_Test_Sonuclari\VNA\SNP1_T1\Ham\olcum.s1p" "veri"
    W "$s\03_Test_Sonuclari\VNA\Eski_Klasor\notlar.txt" "uymayan klasör"
    W "$s\04_Kalifikasyon\03_Test_Sonuclari\CK\SN01_T1\test.md" "---`nsonuc: Devam ediyor`ntarih: 2026-11-03`n---`n**Sonuç: Devam ediyor.** RE102 geçti.`n"
    W "$s\03_Test_Sonuclari\SATIMO\SN03_T1\test.md" "notta YAML yok, sadece metin"

    $runs = [Pusula.Core.Testing.TestRunReader]::ReadStock($s)
    Check 'koşu sayısı (uymayan klasör hariç)' ($runs.Count -eq 6) ("sayi=" + $runs.Count)
    $r = $runs | Where-Object { $_.SerialNo -eq 'SN01' -and $_.Round -eq 1 -and -not $_.IsQualification }
    Check 'YAML: sonuç, kişi, tarih, jig' ($r.Result -eq 'Passed' -and $r.Person -eq 'a-demir' -and $r.Date -eq [datetime]'2026-10-05' -and $r.Jig -eq 'JIG-1 Rev AB')
    Check 'YAML: özet "Sonuç:" satırından' ($r.Summary -eq 'Limit içinde.') $r.Summary
    $r2 = $runs | Where-Object { $_.SerialNo -eq 'SN01' -and $_.Round -eq 2 }
    Check 'büyük harf KALDI' ($r2.Result -eq 'Failed')
    Check 'özet ilk satırdan' ($r2.Summary -eq 'Kablo değişti, tekrar ölçülecek.') $r2.Summary
    $k = $runs | Where-Object SerialNo -eq 'SN02'
    Check 'KIWI: _P1 klasörü, sonuç, kişi, kaynak' ($k.Round -eq 1 -and $k.Result -eq 'Passed' -and $k.Person -eq 'katan' -and $k.Source -eq 'Kiwi')
    Check 'KIWI: tarih başlangıçtan' ($k.Date.ToString('yyyy-MM-dd') -eq '2026-11-08')
    $p = $runs | Where-Object SerialNo -eq 'SNP1'
    Check 'notsuz koşu: Unknown, klasör kaynağı' ($p.Result -eq 'Unknown' -and $p.Source -eq 'Folder' -and $p.System -eq 'VNA')
    $q = $runs | Where-Object IsQualification
    Check 'kalifikasyon ayrı' ($q.Count -eq 1 -and $q.System -eq 'CK' -and $q.Result -eq 'InProgress')
    $n = $runs | Where-Object SerialNo -eq 'SN03'
    Check 'YAML yoksa Unknown, not yolu dolu' ($n.Result -eq 'Unknown' -and $n.NotePath -like '*test.md')
    Check 'sıralama: en yeni üstte' ($runs[0].SerialNo -eq 'SN02' -or $runs[0].Date -ge $runs[1].Date)

    $o = [Pusula.Core.Testing.TestOverview]::Build($runs)
    $kinds = ($o.Alerts | ForEach-Object { $_.Kind.ToString() }) -join ','
    Check 'dikkat: açık, kaldı, jig, girilmedi' ($kinds -match 'Open' -and $kinds -match 'Failed' -and $kinds -match 'JigRevision' -and $kinds -match 'Unknown') $kinds
    $jig = $o.Alerts | Where-Object Kind -eq 'JigRevision'
    Check 'jig revizyon sırası AB, AC' (($jig.Revisions -join ',') -eq 'AB,AC') ($jig.Revisions -join ',')
    Check 'son tur SN01 = T2' ($o.Latest[[Pusula.Core.Testing.TestOverview]::Key('SATIMO', $false, 'SN01')].Round -eq 2)
    Check 'sütunlar: geliştirme önce' (($o.Columns | ForEach-Object { $_.Item1 }) -join ',' -eq 'SATIMO,VNA,CK')
    Check 'SN sırası: SN önce, SNP sonra' (($o.Serials -join ',') -eq 'SN01,SN02,SN03,SNP1') ($o.Serials -join ',')

    $tree = [Pusula.Core.Scanning.FolderTreeNode]::Build($s, [Threading.CancellationToken]::None)
    Check 'ağaç: toplam dosya' ($tree.FileCount -eq 7) ("sayi=" + $tree.FileCount)
}
finally { Remove-Item -Recurse -Force $s -ErrorAction SilentlyContinue }
""
"SONUC: $ok basarili, $fail hata"
