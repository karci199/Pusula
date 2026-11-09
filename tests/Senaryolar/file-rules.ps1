param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
. (Join-Path $PSScriptRoot '..\_Ortak.ps1')
# Sayımlara girmeyen dosyalar (Thumbs.db, Office kilitleri …) ve klasörler (_ARSIV, Arsiv, .obsidian …).
$R = [Pusula.Core.Scanning.FileRules]
"--- 1. Kurallar"
foreach ($n in 'Thumbs.db', 'desktop.ini', '~$Rapor.docx', '.~lock.olcum.xlsx#', 'kopya.tmp', 'indir.crdownload') { Check ('önemsiz: ' + $n) ($R::IsJunkFile($n)) }
foreach ($n in 'Rapor.docx', 'olcum.s2p', 'SH2000_kapak.png', 'notlar.md') { Check ('sayılır: ' + $n) (-not $R::IsJunkFile($n)) }
foreach ($n in '_ARSIV', '_TAKIP', 'Arsiv', 'ARŞİV', '99_Arşiv', 'Archive', 'Eski', '.obsidian') { Check ('atlanan klasör: ' + $n) ($R::IsSkippedFolder($n)) }
foreach ($n in '01_Dokumanlar', 'Eskiz', 'Arsivleme_Plani', '07_Fotograflar') { Check ('sayılan klasör: ' + $n) (-not $R::IsSkippedFolder($n)) }

"--- 2. Sayım"
$tmp = Join-Path $env:TEMP ('pusula-rules-' + [guid]::NewGuid().ToString('N').Substring(0, 6))
New-Item -ItemType Directory "$tmp\01_Dokumanlar", "$tmp\01_Dokumanlar\Arsiv", "$tmp\_ARSIV", "$tmp\07_Fotograflar" | Out-Null
try
{
    'a' | Set-Content "$tmp\01_Dokumanlar\ATP.docx"
    'a' | Set-Content "$tmp\01_Dokumanlar\~`$ATP.docx"
    'a' | Set-Content "$tmp\01_Dokumanlar\Arsiv\ATP_eski.docx"
    'a' | Set-Content "$tmp\_ARSIV\eski.pdf"
    'a' | Set-Content "$tmp\07_Fotograflar\on.png"
    'a' | Set-Content "$tmp\07_Fotograflar\Thumbs.db"
    $stats = [Pusula.Core.Scanning.FolderStats]::Compute($tmp, [Threading.CancellationToken]::None)
    Check 'klasör istatistiği: 2 dosya (ATP.docx, on.png)' ($stats.FileCount -eq 2) ([string]$stats.FileCount)
    $tree = [Pusula.Core.Scanning.FolderTreeNode]::Build($tmp, [Threading.CancellationToken]::None)
    Check 'klasör ağacı: toplam 2' ($tree.FileCount -eq 2) ([string]$tree.FileCount)
    $docs = $tree.Children | Where-Object Name -eq '01_Dokumanlar'
    Check 'arşiv ağaçta görünür ama sayılmaz' ($docs.FileCount -eq 1 -and @($docs.Children | Where-Object Name -eq 'Arsiv').Count -eq 1) ([string]$docs.FileCount)
    Check 'kayıt sayımı: 2' ([Pusula.Core.Inventory.RecordContents]::CountFiles($tmp) -eq 2) ([string][Pusula.Core.Inventory.RecordContents]::CountFiles($tmp))
}
finally
{
    Remove-Item $tmp -Recurse -Force
}
"SONUÇ OK=$ok HATA=$fail"