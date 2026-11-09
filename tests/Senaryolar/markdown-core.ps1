param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
. (Join-Path $PSScriptRoot '..\_Ortak.ps1')
# md çekirdeği: Obsidian bağlantıları, gömme, kutular, görev kutusu, tablo, vurgu, dosya bulma, kaydetme.
$MN = [Pusula.Core.Markdown.MarkdownNotes]
function Descend($doc) { [Markdig.Syntax.MarkdownObjectExtensions]::Descendants($doc) }
function AllInlines($doc) { Descend $doc | Where-Object { $_ -is [Markdig.Syntax.Inlines.Inline] } }

"--- 1. Wiki bağlantıları"
$doc = $MN::Parse("JIG [[JIG-1000-0009]] ve [[JIG-1000-0009|plaka]] ile [[Not#Bölüm]]; ![[foto 1.jpg|300]] ![[cizim.pdf]] [normal](https://x) [[kapanmamış")
$w = @(AllInlines $doc | Where-Object { $_ -is [Pusula.Core.Markdown.WikiLinkInline] })
Check '5 wiki bağlantısı' ($w.Count -eq 5) ([string]$w.Count)
Check 'hedef ve görünen ad' ($w[0].Target -eq 'JIG-1000-0009' -and $w[1].Alias -eq 'plaka' -and $w[1].DisplayText -eq 'plaka')
Check 'başlık' ($w[2].Target -eq 'Not' -and $w[2].Heading -eq 'Bölüm' -and $w[2].DisplayText -eq 'Not › Bölüm')
Check 'gömme ve genişlik' ($w[3].IsEmbed -and $w[3].Target -eq 'foto 1.jpg' -and $w[3].EmbedWidth -eq 300 -and $w[4].IsEmbed -and $w[4].EmbedWidth -eq 0)
Check 'normal bağlantı bozulmaz' (@(AllInlines $doc | Where-Object { $_ -is [Markdig.Syntax.Inlines.LinkInline] -and $_.Url -eq 'https://x' }).Count -eq 1)
Check 'kapanmamış [[ düz yazı kalır' (@(AllInlines $doc | Where-Object { $_ -is [Markdig.Syntax.Inlines.LiteralInline] -and $_.Content.ToString() -match 'kapanmamış' }).Count -eq 1)
Check 'kod içinde bağlantı sayılmaz' (@(AllInlines ($MN::Parse('`[[JIG-1]]` metin')) | Where-Object { $_ -is [Pusula.Core.Markdown.WikiLinkInline] }).Count -eq 0)
Check 'satır bilgisi' ((@(AllInlines ($MN::Parse("a`n`nb [[X]]")) | Where-Object { $_ -is [Pusula.Core.Markdown.WikiLinkInline] })[0]).Line -eq 2)

"--- 2. Bloklar"
$md = "---`ntur: stok`n---`n# Başlık`n`n> [!warning]- Dikkat edilecek`n> Tork 4 Nm`n`n> düz alıntı`n`n- [ ] yapılacak`n- [x] bitti`n`n| A | B |`n|---|---|`n| 1 | 2 |`n`n==önemli== ve ~~eski~~`n"
$d = $MN::Parse($md)
$blocks = @(Descend $d)
Check 'YAML başlığı ayrı blok' (@($blocks | Where-Object { $_ -is [Markdig.Extensions.Yaml.YamlFrontMatterBlock] }).Count -eq 1)
$quotes = @($blocks | Where-Object { $_ -is [Markdig.Syntax.QuoteBlock] })
$c = $MN::CalloutOf($quotes[0], $md)
Check 'kutu: tür, başlık, kapalı' ($c -ne $null -and $c.Kind -eq 'warning' -and $c.Title -eq 'Dikkat edilecek' -and $c.IsFolded)
Check 'düz alıntı kutu değil' ($MN::CalloutOf($quotes[1], $md) -eq $null)
$tasks = @(AllInlines $d | Where-Object { $_ -is [Markdig.Extensions.TaskLists.TaskList] })
Check 'görev kutuları' ($tasks.Count -eq 2 -and -not $tasks[0].Checked -and $tasks[1].Checked)
Check 'tablo' (@($blocks | Where-Object { $_ -is [Markdig.Extensions.Tables.Table] }).Count -eq 1)
$em = @(AllInlines $d | Where-Object { $_ -is [Markdig.Syntax.Inlines.EmphasisInline] })
Check 'vurgu (==) ve çizili (~~)' (@($em | Where-Object { $_.DelimiterChar -eq '=' }).Count -eq 1 -and @($em | Where-Object { $_.DelimiterChar -eq '~' }).Count -eq 1)

"--- 3. Görev kutusunu çevir"
$t = "a`r`n- [ ] bir`r`n  - [x] iki`r`n1. [ ] üç`r`nmetin [ ] değil"
$t1 = $MN::ToggleTask($t, 1)
Check 'işaretlenir, CRLF korunur' ($t1 -eq "a`r`n- [x] bir`r`n  - [x] iki`r`n1. [ ] üç`r`nmetin [ ] değil")
Check 'iç içe kaldırılır' (($MN::ToggleTask($t, 2)) -match '  - \[ \] iki')
Check 'numaralı liste' (($MN::ToggleTask($t, 3)) -match '1\. \[x\] üç')
Check 'kutu olmayan satır değişmez' (($MN::ToggleTask($t, 4)) -eq $t -and ($MN::ToggleTask($t, 99)) -eq $t)

"--- 4. Dosya bulma"
$r = Join-Path $env:TEMP ('pusula-md-' + [guid]::NewGuid().ToString('N').Substring(0,6))
try {
    New-Item -ItemType Directory "$r\Stok\Foto", "$r\Ekler", "$r\.obsidian", "$r\_SILINENLER" -Force | Out-Null
    'x' | Set-Content "$r\Stok\Foto\a b.png"; 'x' | Set-Content "$r\Ekler\plan.jpg"; 'x' | Set-Content "$r\.obsidian\gizli.png"; 'x' | Set-Content "$r\_SILINENLER\sil.png"
    Check 'nota göre, %20 çözülür' (($MN::ResolveFile("$r\Stok", $r, 'Foto/a%20b.png')) -eq "$r\Stok\Foto\a b.png")
    Check 'köke göre' (($MN::ResolveFile("$r\Stok", $r, 'Ekler/plan.jpg')) -eq "$r\Ekler\plan.jpg")
    Check 'yalnız adla (Obsidian gibi)' (($MN::ResolveFile("$r\Stok", $r, 'plan.jpg')) -eq "$r\Ekler\plan.jpg")
    Check 'gizli ve silinen klasör aranmaz' (($MN::ResolveFile("$r\Stok", $r, 'gizli.png')) -eq '' -and ($MN::ResolveFile("$r\Stok", $r, 'sil.png')) -eq '')
    Check 'web adresi ve boş' (($MN::ResolveFile("$r\Stok", $r, 'https://a/b.png')) -eq '' -and ($MN::ResolveFile("$r\Stok", $r, '')) -eq '')
    Check 'resim uzantısı' ($MN::IsImage('a.JPG') -and -not $MN::IsImage('a.pdf'))

    "--- 5. Kaydetme"
    [IO.File]::WriteAllText("$r\bom.md", "eski", (New-Object Text.UTF8Encoding $true))
    [IO.File]::WriteAllText("$r\nobom.md", "eski", (New-Object Text.UTF8Encoding $false))
    Check 'kaydedildi' (($MN::Save("$r\bom.md", 'yeni ğ')) -eq '' -and ($MN::Save("$r\nobom.md", 'yeni ğ')) -eq '' -and ($MN::Save("$r\Yeni\yeni.md", 'ilk')) -eq '')
    $b1 = [IO.File]::ReadAllBytes("$r\bom.md"); $b2 = [IO.File]::ReadAllBytes("$r\nobom.md"); $b3 = [IO.File]::ReadAllBytes("$r\Yeni\yeni.md")
    Check 'BOM korunur / eklenmez' ($b1[0] -eq 0xEF -and $b2[0] -ne 0xEF -and $b3[0] -ne 0xEF)
    Check 'içerik' ([IO.File]::ReadAllText("$r\nobom.md") -eq 'yeni ğ')
    Check 'geçici dosya kalmaz' (@(Get-ChildItem $r -Recurse -Filter '*.tmp-*').Count -eq 0)
} finally { Remove-Item -Recurse -Force $r -ErrorAction SilentlyContinue }
Check 'kelime sayısı YAML hariç' (($MN::WordCount("---`na: b c`n---`nİki kelime, üç.")) -eq 3)
"SONUÇ OK=$ok HATA=$fail"
