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
"--- 6. Düzenleme işlemleri"
$E = [Pusula.Core.Markdown.MarkdownEditing]
$r = $E::Wrap('a kalın b', 2, 5, '**'); Check 'kalın sar' ($r.Text -eq 'a **kalın** b' -and $r.SelectionStart -eq 4 -and $r.SelectionLength -eq 5)
$r2 = $E::Wrap($r.Text, $r.SelectionStart, $r.SelectionLength, '**'); Check 'tekrar basınca kaldır' ($r2.Text -eq 'a kalın b')
$r = $E::Wrap('ab', 1, 0, '=='); Check 'boş seçim: imleç arada' ($r.Text -eq 'a====b' -and $r.SelectionStart -eq 3)
$r = $E::TogglePrefix("bir`r`niki", 0, 8, '- [ ] '); Check 'iki satıra görev kutusu (CRLF)' ($r.Text -eq "- [ ] bir`r`n- [ ] iki")
$r = $E::TogglePrefix($r.Text, 0, $r.Text.Length, '- [ ] '); Check 'görev kutusu kaldır' ($r.Text -eq "bir`r`niki")
$r = $E::TogglePrefix("# Eski", 3, 0, '## '); Check 'başlık düzeyi değişir' ($r.Text -eq '## Eski')
$r = $E::ContinueList("- bir", 5); Check 'liste sürer' ($r.Text -eq "- bir`r`n- " -and $r.SelectionStart -eq 9)
$r = $E::ContinueList("a`n  3. üç", 9); Check 'numara artar, girinti korunur, LF' ($r.Text -eq "a`n  3. üç`n  4. ")
$r = $E::ContinueList("- [x] bitti", 11); Check 'görev boş kutuyla sürer' ($r.Text -eq "- [x] bitti`r`n- [ ] ")
$r = $E::ContinueList("x`r`n- ", 5); Check 'boş madde: listeden çık' ($r.Text -eq "x`r`n" -and $r.SelectionStart -eq 3)
Check 'liste değil: null' ($E::ContinueList('düz satır', 4) -eq $null)
$r = $E::Indent("- a`n- b", 4, 3, $false); Check 'girinti' ($r.Text -eq "- a`n  - b")
$r = $E::Indent($r.Text, 6, 0, $true); Check 'girinti geri' ($r.Text -eq "- a`n- b")
$q = $E::FindQuery('Ölçüm @JIG-10', 13); Check '@ sorgusu' ($q -ne $null -and $q.Text -eq 'JIG-10' -and -not $q.IsLink -and $q.Start -eq 6)
$q2 = $E::FindQuery('bak [[AB-70', 11); Check '[[ sorgusu' ($q2 -ne $null -and $q2.IsLink -and $q2.Text -eq 'AB-70' -and $q2.Start -eq 4)
Check 'e-posta @ değil' ($E::FindQuery('a@b', 3) -eq $null)
Check 'kapanmış bağlantı sorgu değil' ($E::FindQuery('[[X]] y', 7) -eq $null)
$r = $E::ApplySuggestion('Ölçüm @JIG-10 tamam', 13, $q, '[[JIG-1000-0009]]'); Check 'öneri yerleşir' ($r.Text -eq 'Ölçüm [[JIG-1000-0009]] tamam' -and $r.SelectionStart -eq 23)
$r = $E::ApplySuggestion('bak [[AB-70]]', 11, $q2, '[[AB-7000-0003]]'); Check 'otomatik kapanış iki kez yazılmaz' ($r.Text -eq 'bak [[AB-7000-0003]]')
$lc = $E::LineColumn("ab`r`ncd", 5); Check 'satır / sütun' ($lc.Item1 -eq 2 -and $lc.Item2 -eq 2)
"--- 7. Yorumlar"
$r = $E::Wrap('a not b', 2, 3, '<!-- ', ' -->'); Check 'yorum sar' ($r.Text -eq 'a <!-- not --> b' -and $r.SelectionStart -eq 7 -and $r.SelectionLength -eq 3)
$r2 = $E::Wrap($r.Text, $r.SelectionStart, $r.SelectionLength, '<!-- ', ' -->'); Check 'yorum tekrar basınca kalkar' ($r2.Text -eq 'a not b')
$src = "a %%gizli%% b`r`n%%`r`nçok`r`nsatır`r`n%%`r`nson ``%%kod%%```r`n" + '```' + "`r`n%%blok%%`r`n" + '```'
$m = $MN::MaskComments($src)
Check 'örtme uzunluğu korur' ($m.Length -eq $src.Length)
Check 'satır içi yorum örtülür' ($m.StartsWith('a ') -and -not $m.Contains('gizli') -and $m.Contains(' b'))
Check 'çok satırlı yorum örtülür, satırlar korunur' (-not $m.Contains('çok') -and -not $m.Contains('satır') -and ($m.Split("`n").Count -eq $src.Split("`n").Count))
Check 'satır içi koddaki %% korunur' ($m.Contains('`%%kod%%`'))
Check 'kod bloğundaki %% korunur' ($m.Contains('%%blok%%'))
Check 'kapanmamış %% sona kadar' (($MN::MaskComments("a`n%% b`nc")).Trim() -eq 'a')
Check '%% yoksa aynı metin' ([object]::ReferenceEquals($MN::MaskComments('düz'), 'düz') -or $MN::MaskComments('düz') -eq 'düz')
"--- 8. Bul / değiştir"
$TS = [Pusula.Core.Markdown.TextSearch]
function Opt($c, $w, $r) { $o = New-Object Pusula.Core.Markdown.SearchOptions; $o.MatchCase = $c; $o.WholeWord = $w; $o.UseRegex = $r; $o }
$txt = "SN01 ölçüldü. sn02 ölçülmedi.`r`nİzmir ve izmir, IŞIK ışık. snx"
$res = $TS::FindAll($txt, 'sn', (Opt $false $false $false)); Check 'düz arama, harf ayrımı yok' ($res.Matches.Count -eq 3)
$res = $TS::FindAll($txt, 'sn', (Opt $true $false $false)); Check 'büyük / küçük harf ayrımı' ($res.Matches.Count -eq 2)
$res = $TS::FindAll($txt, 'izmir', (Opt $false $false $false)); Check 'Türkçe: İzmir = izmir' ($res.Matches.Count -eq 2)
$res = $TS::FindAll($txt, 'ışık', (Opt $false $false $false)); Check 'Türkçe: IŞIK = ışık' ($res.Matches.Count -eq 2)
$res = $TS::FindAll($txt, 'ölçüldü', (Opt $false $true $false)); Check 'tam kelime (ölçülmedi sayılmaz)' ($res.Matches.Count -eq 1)
$res = $TS::FindAll($txt, 'SN\d+', (Opt $false $false $true)); Check 'düzenli ifade' ($res.Matches.Count -eq 2 -and $res.Matches[1].Start -eq $txt.IndexOf('sn02'))
$res = $TS::FindAll($txt, 'SN(', (Opt $false $false $true)); Check 'geçersiz ifade hata verir' ($res.Matches.Count -eq 0 -and $res.Error.Length -gt 0)
$res = $TS::FindAll($txt, 'a.b', (Opt $false $false $false)); Check 'düz aramada nokta harfiyen' ($res.Matches.Count -eq 0)
$res = $TS::FindAll($txt, '', (Opt $false $false $false)); Check 'boş arama: sonuç yok, hata yok' ($res.Matches.Count -eq 0 -and $res.Error -eq '')
$res = $TS::FindAll($txt, 'sn', (Opt $false $false $false))
Check 'imleçten sonraki ilk' ($res.IndexFrom(5) -eq 1 -and $res.IndexFrom(10000) -eq 0)
Check 'imleçten önceki' ($res.IndexBefore(5) -eq 0 -and $res.IndexBefore(0) -eq 2)
$res = $TS::FindAll($txt, 'SN(\d+)', (Opt $false $false $true))
$e1 = $TS::Replace($txt, $res.Matches[0], 'SN(\d+)', 'Seri-$1', (Opt $false $false $true)); Check 'tek değiştir ($1 grubu)' ($e1.Text.StartsWith('Seri-01 ölçüldü') -and $e1.SelectionLength -eq 7)
$cnt = 0; $all = $TS::ReplaceAll($txt, 'ölçül', 'test edil', (Opt $false $false $false), [ref]$cnt); Check 'tümünü değiştir' ($cnt -eq 2 -and $all.Contains('test edildü') -and $all.Contains('test edilmedi'))
$cnt = 0; $same = $TS::ReplaceAll($txt, 'yok', 'x', (Opt $false $false $false), [ref]$cnt); Check 'eşleşme yoksa metin aynı' ($cnt -eq 0 -and $same -eq $txt)
$stale = New-Object Pusula.Core.Markdown.TextMatch(0, 3); Check 'eskimiş eşleşme değiştirilmez' ($TS::Replace('abc', $stale, 'xyz', 'q', (Opt $false $false $false)) -eq $null)
"--- 8b. Satır işlemleri, otomatik çift, bağlantı yapıştırma"
$L = "bir`r`niki`r`nüç"
$r = $E::MoveLines($L, 6, 0, $true); Check 'satır yukarı (CRLF)' ($r.Text -eq "iki`r`nbir`r`nüç" -and $r.SelectionStart -eq 1)
$r = $E::MoveLines($L, 1, 0, $false); Check 'satır aşağı' ($r.Text -eq "iki`r`nbir`r`nüç" -and $r.SelectionStart -eq 6)
Check 'ilk satır yukarı: yok' ($E::MoveLines($L, 0, 0, $true) -eq $null)
Check 'son satır aşağı: yok' ($E::MoveLines($L, 11, 0, $false) -eq $null)
$r = $E::MoveLines($L, 0, 7, $false); Check 'iki satır birden aşağı' ($r.Text -eq "üç`r`nbir`r`niki" -and $r.SelectionStart -eq 4 -and $r.SelectionLength -eq 7)
$r = $E::MoveLines($L, 0, 5, $false); Check 'satır başında biten seçim o satırı almaz' ($r.Text -eq "iki`r`nbir`r`nüç")
$r = $E::MoveLines("a`nb", 2, 0, $true); Check 'LF korunur' ($r.Text -eq "b`na")
$r = $E::DuplicateLines($L, 6, 0); Check 'satır çoğalt' ($r.Text -eq "bir`r`niki`r`niki`r`nüç" -and $r.SelectionStart -eq 11)
$r = $E::DeleteLines($L, 6, 0); Check 'satır sil' ($r.Text -eq "bir`r`nüç" -and $r.SelectionStart -eq 5)
$r = $E::DeleteLines($L, 11, 0); Check 'son satırı sil' ($r.Text -eq "bir`r`niki")
$r = $E::DeleteLines('tek', 1, 0); Check 'tek satırı sil' ($r.Text -eq '')
$r = $E::AutoPair('ab ', 3, 0, [char]'('); Check 'parantez kapanır' ($r.Text -eq 'ab ()' -and $r.SelectionStart -eq 4)
$r = $E::AutoPair('ab ()', 4, 0, [char]')'); Check 'kapanışın üzerinden geçer' ($r.Text -eq 'ab ()' -and $r.SelectionStart -eq 5)
$r = $E::AutoPair('', 0, 0, [char]'['); $r = $E::AutoPair($r.Text, $r.SelectionStart, 0, [char]'['); Check '[[ → [[]] (bağlantı)' ($r.Text -eq '[[]]' -and $r.SelectionStart -eq 2)
Check 'kelimenin önünde açılmaz' ($E::AutoPair('ab', 0, 0, [char]'(') -eq $null)
Check 'harften sonra tırnak açılmaz' ($E::AutoPair('Pusula', 6, 0, [char]'"') -eq $null)
Check 'tek tırnak çiftlenmez (kesme işareti)' ($E::AutoPair('Pusula ', 7, 0, [char]"'") -eq $null)
$r = $E::AutoPair('x kalın y', 2, 5, [char]'*'); Check 'yıldız seçimi sarar' ($r.Text -eq 'x *kalın* y' -and $r.SelectionStart -eq 3 -and $r.SelectionLength -eq 5)
Check 'yıldız seçimsiz: normal' ($E::AutoPair('x ', 2, 0, [char]'*') -eq $null)
$r = $E::AutoPair('a ', 2, 0, [char]'`'); $r = $E::AutoPair($r.Text, $r.SelectionStart, 0, [char]'`'); $r2 = $E::AutoPair($r.Text, $r.SelectionStart, 0, [char]'`')
Check '``` üç ters tırnak düzgün çıkar' ($r.Text -eq 'a ``' -and $r2 -eq $null)
$r = $E::DeletePair('a ()', 3); Check 'Backspace boş çifti siler' ($r.Text -eq 'a ' -and $r.SelectionStart -eq 2)
Check 'dolu çift silinmez' ($E::DeletePair('(x)', 1) -eq $null)
$r = $E::PasteLink('bak Markdig belgesi', 4, 7, ' https://github.com/xoofx/markdig '); Check 'seçime adres yapıştır: bağlantı' ($r.Text -eq 'bak [Markdig](https://github.com/xoofx/markdig) belgesi')
Check 'seçim yokken normal yapıştırma' ($E::PasteLink('bak', 3, 0, 'https://x.y') -eq $null)
Check 'adres değilse normal' ($E::PasteLink('bak a', 4, 1, 'düz yazı') -eq $null)
Check 'seçim zaten adresse normal' ($E::PasteLink('https://a.b', 0, 11, 'https://x.y') -eq $null)

"--- 9. Başlıklar (içindekiler)"
$osrc = "---`r`nbaslik: x`r`n---`r`n# Ana **kalın**`r`n`r`nmetin`r`n`r`n## Alt ##`r`n`r`n" + '```' + "`r`n# kod değil`r`n" + '```' + "`r`n%%`r`n# yorum değil`r`n%%`r`nSetext`r`n------`r`n`r`n> ## Alıntıda`r`n"
$ol = @($MN::Outline($osrc))
Check 'başlık sayısı (kod, yorum, YAML hariç)' ($ol.Count -eq 4) (($ol | ForEach-Object { "$($_.Level):$($_.Title)" }) -join ' | ')
Check 'düzey ve metin' ($ol[0].Level -eq 1 -and $ol[0].Title -eq 'Ana **kalın**' -and $ol[1].Level -eq 2 -and $ol[1].Title -eq 'Alt')
Check 'setext başlık' ($ol[2].Level -eq 2 -and $ol[2].Title -eq 'Setext')
Check 'satır ve konum' ($ol[0].Line -eq 3 -and $osrc.Substring($ol[0].Start, 5) -eq '# Ana' -and $ol[1].Indent -eq 14)
Check 'başlıksız not: boş' (@($MN::Outline('düz metin')).Count -eq 0)
"SONUÇ OK=$ok HATA=$fail"
