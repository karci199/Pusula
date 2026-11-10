param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
. (Join-Path $PSScriptRoot '..\_Ortak.ps1')
# md not penceresi: üç görünüm, çizim (başlık, kutu, görev, tablo, resim, bağlantı), kısayollar, @ / [[ önerileri,
# resim yapıştırma ve kopyalama, kaydetme (kodlama korunur), dış değişiklik, kapatırken soru, ayarlar, gezinme.
function SnapElement($element, [string]$path)
{
    $element.Measure([System.Windows.Size]::new([double]::PositiveInfinity, [double]::PositiveInfinity)); $element.Arrange([System.Windows.Rect]::new($element.DesiredSize)); $element.UpdateLayout()
    $bmp = New-Object System.Windows.Media.Imaging.RenderTargetBitmap([int][Math]::Max(1, $element.ActualWidth), [int][Math]::Max(1, $element.ActualHeight), 96, 96, [System.Windows.Media.PixelFormats]::Pbgra32)
    $bmp.Render($element)
    $png = New-Object System.Windows.Media.Imaging.PngBitmapEncoder; $png.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($bmp))
    $fs = [IO.File]::Create($path); try { $png.Save($fs) } finally { $fs.Close() }
}
function Blocks($doc) { $all = New-Object System.Collections.Generic.List[object]; $stack = New-Object System.Collections.Stack; foreach ($b in $doc.Blocks) { $stack.Push($b) }; while ($stack.Count -gt 0) { $b = $stack.Pop(); $all.Add($b); if ($b -is [System.Windows.Documents.Section]) { foreach ($c in $b.Blocks) { $stack.Push($c) } }; if ($b -is [System.Windows.Documents.List]) { foreach ($li in $b.ListItems) { foreach ($c in $li.Blocks) { $stack.Push($c) } } } }; $all }

$stock = Join-Path $Data '7000_MILKON\AB-7000-0003'
$note = Join-Path $stock 'AB-7000-0003.md'
$md = @"
---
tur: stok
numara: AB-7000-0003
---
# DA400 Döner Anten

Ölçüm düzeni [[JIG-1000-0009|SATIMO plakası]] ile kuruldu. #SATIMO #kalifikasyon
İkinci satır aynı paragrafta.

> [!warning] Dikkat
> Tork **4 Nm**, fazlası plakayı eğer.

## Yapılacaklar

- [ ] SN02 tekrar ölç
- [x] Kablo değişti
  - [ ] iç madde

1. Bir
2. İki

| Sistem | Sonuç | Not |
|:---|:---:|---:|
| SATIMO | Geçti | ==yan lob== |
| VNA | Kaldı | ~~eski~~ |

``````
S11 < -10 dB
``````

![[foto_01.png|320]]
![[yok_boyle.png]]

Bkz. dipnot[^1] ve [web](https://example.com).

[^1]: Ölçüm 05.10.2026.
"@
[IO.File]::WriteAllText($note, ($md -replace "`r?`n", "`r`n"), (New-Object Text.UTF8Encoding $true))

[System.Threading.SynchronizationContext]::SetSynchronizationContext((New-Object System.Windows.Threading.DispatcherSynchronizationContext))
$app = New-Object Pusula.Wpf.App; $app.InitializeComponent(); $app.ShutdownMode = 'OnExplicitShutdown'; [Pusula.Wpf.Themes]::Apply([Pusula.Wpf.Themes]::Find('FluentDark'))
$win = New-Object Pusula.Wpf.MainWindow
$win.WindowStartupLocation = 'Manual'; $win.Left = -20000; $win.Top = -20000; $win.ShowActivated = $false; $win.Width = 1360; $win.Height = 900
$win.Show(); Pump
$vm = $win.DataContext; $vm.RootPath = $Data; Wait
$s = New-Object Pusula.Wpf.NoteEditorSettings; $s.Maximized = $false; $vm.Settings.NoteEditor = $s

"--- 1. Açılış ve ayar"
Check 'varsayılan not programı Pusula' ($vm.NoteApp.Key -eq 'pusula' -and $vm.UsesNoteEditor) $vm.NoteApp.Key
$vm.OpenNoteOrFolder($note, $stock); Pump
$nw = @($win.NoteWindows)[0]
Check '"Notu aç" Pusula penceresini açar' ($nw -ne $null -and $nw.ViewModel.Path -eq $note)
$nw.WindowStartupLocation = 'Manual'; $nw.Left = -20000; $nw.Top = -20000; $nw.Width = 1500; $nw.Height = 940; Wait
$vm.OpenNoteOrFolder($note, $stock); Pump
Check 'aynı not ikinci kez açılmaz' (@($win.NoteWindows).Count -eq 1)
$e = $nw.ViewModel
Check 'metin okundu, değişiklik yok' ($e.Text.Length -gt 100 -and -not $e.IsDirty -and $e.WordsText -match '^\d+ kelime$')
Check 'pencere başlığı' ($e.WindowTitle -match '^AB-7000-0003 — Pusula$') $e.WindowTitle

"--- 2. Görünüm çizimi"
$nw.RenderPreview(); Wait
$doc = $nw.FindName('Preview').Document
$blocks = @(Blocks $doc)
Check 'tablo' (@($blocks | Where-Object { $_ -is [System.Windows.Documents.Table] }).Count -eq 1)
$table = @($blocks | Where-Object { $_ -is [System.Windows.Documents.Table] })[0]
Check 'tablo: 3 sütun, başlık + 2 satır' ($table.Columns.Count -eq 3 -and $table.RowGroups[0].Rows.Count -eq 3) ([string]$table.Columns.Count + ' / ' + [string]$table.RowGroups[0].Rows.Count)
Check 'listeler (görev, iç içe, numaralı)' (@($blocks | Where-Object { $_ -is [System.Windows.Documents.List] }).Count -ge 3)
Check 'numaralı liste' (@($blocks | Where-Object { $_ -is [System.Windows.Documents.List] -and $_.MarkerStyle -eq 'Decimal' }).Count -eq 1)
Check 'görev listesi işaretsiz' (@($blocks | Where-Object { $_ -is [System.Windows.Documents.List] -and $_.MarkerStyle -eq 'None' }).Count -ge 1)
Check 'kutu (callout) bölümü' (@($blocks | Where-Object { $_ -is [System.Windows.Documents.Section] -and $_.BorderThickness.Left -eq 3 }).Count -ge 1)
Check 'YAML özellik kutusu' (@($blocks | Where-Object { $_ -is [System.Windows.Documents.BlockUIContainer] }).Count -ge 1)
Check 'resim bulundu (adla)' ($nw.LastRender.Images.Count -eq 1 -and $nw.LastRender.Images[0] -like '*foto_01.png') ($nw.LastRender.Images -join ',')
Check 'bulunamayan resim bildirilir' ($nw.LastRender.MissingFiles -contains 'yok_boyle.png') ($nw.LastRender.MissingFiles -join ',')
Check 'durum: resim ve eksik dosya' ($nw.FindName('RenderInfo').Text -match '1 resim' -and $nw.FindName('RenderInfo').Text -match '1 dosya bulunamadı') $nw.FindName('RenderInfo').Text
$range = New-Object System.Windows.Documents.TextRange($doc.ContentStart, $doc.ContentEnd)
Check 'bağlantının görünen adı' ($range.Text -match 'SATIMO plakası' -and -not ($range.Text -match '\[\['))
Check 'satır sonu korunur (tek satır sonu yeni satır)' ($range.Text -match "kalifikasyon`r?`nİkinci")
Snap $nw "$Out\note-split.png"

"--- 3. Görünümler ve kısayollar"
$K = [System.Windows.Input.Key]
$nw.HandleKey($K::D3, $true, $false) | Out-Null; Wait
Check 'Ctrl+3: yalnız görünüm' ($e.IsPreviewMode -and $nw.FindName('Editor').Visibility -eq 'Collapsed' -and $nw.FindName('Preview').Visibility -eq 'Visible')
Snap $nw "$Out\note-preview.png"
$nw.HandleKey($K::D1, $true, $false) | Out-Null; Wait
Check 'Ctrl+1: yalnız düz' ($e.IsEditMode -and $nw.FindName('Preview').Visibility -eq 'Collapsed')
Snap $nw "$Out\note-edit.png"
$nw.HandleKey($K::D2, $true, $false) | Out-Null; Wait
Check 'Ctrl+2: yan yana, ayar kaydedildi' ($e.IsSplitMode -and $vm.Settings.NoteEditor.View -eq 'split')
$ed = $nw.FindName('Editor')
Check 'düzenleyici AvalonEdit: satır numarası, geçerli satır, md renklendirme' ($ed -is [Pusula.Wpf.MarkdownTextEditor] -and $ed.ShowLineNumbers -and $ed.Options.HighlightCurrentLine -and @($ed.TextArea.TextView.LineTransformers | ? { $_ -is [Pusula.Wpf.MarkdownColorizer] }).Count -eq 1)
Check 'metin görünüm modeliyle aynı' ($ed.Text -eq $e.Text)
$ed.TextArea.TextView.EnsureVisualLines()
$hl = $ed.Document.GetLineByNumber(1 + @($ed.Text -split "`r?`n").IndexOf(@($ed.Text -split "`r?`n" | ? { $_ -match '^# ' })[0]))
$vl = $ed.TextArea.TextView.GetOrConstructVisualLine($hl)
$accent = [System.Windows.Application]::Current.TryFindResource('AccentTextFillColorPrimaryBrush')
Check 'başlık satırı vurgu renginde' (@($vl.Elements | ? { $_.TextRunProperties.ForegroundBrush -eq $accent }).Count -ge 1)
$i = $ed.Text.IndexOf('Ölçüm düzeni'); $ed.Select($i, 5)
$nw.HandleKey($K::B, $true, $false) | Out-Null
Check 'Ctrl+B kalın' ($ed.Text.Contains('**Ölçüm** düzeni') -and $e.IsDirty)
$nw.HandleKey($K::H, $true, $true) | Out-Null
Check 'Ctrl+Shift+H vurguyu sarar' ($ed.Text.Contains('**==Ölçüm==** düzeni'))
$nw.HandleKey($K::H, $true, $true) | Out-Null
$nw.HandleKey($K::B, $true, $false) | Out-Null
Check 'tekrar basınca geri alınır' ($ed.Text.Contains('Ölçüm düzeni [[') -and -not $ed.Text.Contains('**Ölçüm**'))
$j = $ed.Text.IndexOf('- [ ] SN02 tekrar ölç') + '- [ ] SN02 tekrar ölç'.Length; $ed.Select($j, 0)
$nw.HandleKey($K::Enter, $false, $false) | Out-Null
Check 'Enter: görev listesi sürer' ($ed.Text.Contains("- [ ] SN02 tekrar ölç`r`n- [ ] `r`n- [x]"))
$ed.Text = $ed.Text.Replace("- [ ] SN02 tekrar ölç`r`n- [ ] `r`n", "- [ ] SN02 tekrar ölç`r`n")
$hpos = $ed.Text.IndexOf('# DA400'); $ed.Select($hpos + 3, 0)
$cpos = $ed.Text.IndexOf('Ölçüm düzeni'); $ed.Select($cpos, 5)
$nw.HandleKey($K::C, $true, $true) | Out-Null
Check 'Ctrl+Shift+C yorum' ($ed.Text.Contains('<!-- Ölçüm --> düzeni'))
$nw.HandleKey($K::C, $true, $true) | Out-Null
Check 'Ctrl+Shift+C tekrar: yorum kalkar' ($ed.Text.Contains('Ölçüm düzeni [[') -and -not $ed.Text.Contains('<!--'))
$ctx = New-Object Pusula.Wpf.Markdown.MarkdownRenderContext
$cdoc = [Pusula.Wpf.Markdown.MarkdownRenderer]::Render("Önce <!-- satır içi gizli --> sonra`r`n`r`n<!-- blok gizli -->`r`n`r`nOrta %%obsidian gizli%% son`r`n`r`n%%`r`nçok satır gizli`r`n%%`r`n`r`n- [ ] görev", $ctx)
$ctext = (New-Object System.Windows.Documents.TextRange($cdoc.ContentStart, $cdoc.ContentEnd)).Text
Check 'görünümde yorumlar gizli' (-not ($ctext -match 'gizli') -and $ctext -match 'Önce' -and $ctext -match 'sonra' -and $ctext -match 'Orta' -and $ctext -match 'son') $ctext
$ed.Select($hpos + 3, 0)
$nw.HandleKey($K::H, $true, $false) | Out-Null
Check 'Ctrl+H: başlık düzeyi döner (# → ##)' ($ed.Text.Contains('## DA400 Döner Anten'))
$nw.HandleKey($K::H, $true, $false) | Out-Null; $nw.HandleKey($K::H, $true, $false) | Out-Null
Check '## → ### → başlıksız' ($ed.Text -match '(?m)^DA400 Döner Anten')
$nw.HandleKey($K::H, $true, $false) | Out-Null
Check 'başlıksız → # (döngü)' ($ed.Text -match '(?m)^# DA400 Döner Anten')

"--- 4. Öneriler (@ ve [[)"
$ed.Select($ed.Text.Length, 0); $ed.SelectedText = "`r`nKullanılan @JIG-1000-00"; $ed.Select($ed.Text.Length, 0); Pump
Check '@ yazınca liste açılır' ($nw.IsSuggesting -and @($nw.SuggestionItems | Where-Object { $_.Key -eq 'JIG-1000-0009' }).Count -eq 1) ((@($nw.SuggestionItems) | % Key) -join ',')
Check 'jig adı ve türü görünür' (@($nw.SuggestionItems | Where-Object { $_.Key -eq 'JIG-1000-0009' -and $_.Kind -eq 'Jig' -and $_.Title -match 'SATIMO' }).Count -eq 1)
$nw.FindName('SuggestList').SelectedItem = @($nw.SuggestionItems | Where-Object { $_.Key -eq 'JIG-1000-0009' })[0]
SnapElement $nw.FindName('SuggestPopup').Child "$Out\note-suggest.png"
Check 'seçince [[…]] yazılır, liste kapanır' ($nw.AcceptSuggestion() -and $ed.Text.EndsWith('Kullanılan [[JIG-1000-0009]]') -and -not $nw.IsSuggesting)
$ed.Select($ed.Text.Length, 0); $ed.SelectedText = ' ve [[AB-70'; $ed.Select($ed.Text.Length, 0); Pump
Check '[[ yazınca stok önerilir' ($nw.IsSuggesting -and @($nw.SuggestionItems | Where-Object { $_.Key -eq 'AB-7000-0003' }).Count -eq 1)
$nw.FindName('SuggestList').SelectedItem = @($nw.SuggestionItems | Where-Object { $_.Key -eq 'AB-7000-0003' })[0]; $nw.AcceptSuggestion() | Out-Null
Check 'stok bağlantısı' ($ed.Text.EndsWith('ve [[AB-7000-0003]]'))
$ed.Select($ed.Text.Length, 0); $ed.SelectedText = ' sorumlu @kat'; $ed.Select($ed.Text.Length, 0); Pump
Check 'kişi önerilir' ($nw.IsSuggesting -and @($nw.SuggestionItems | Where-Object { $_.Key -eq 'katan' -and $_.Kind -eq 'Kişi' }).Count -eq 1)
$nw.FindName('SuggestList').SelectedItem = @($nw.SuggestionItems | Where-Object { $_.Key -eq 'katan' })[0]; $nw.AcceptSuggestion() | Out-Null
Check 'kişi @ ile yazılır' ($ed.Text.EndsWith('sorumlu @katan'))
$ed.Select($ed.Text.Length, 0); $ed.SelectedText = ' x@y.com'; Pump
Check 'e-postada liste açılmaz' (-not $nw.IsSuggesting)

"--- 5. Görev kutusu ve gezinme"
$line = @($ed.Text -split "`r`n").IndexOf('- [ ] SN02 tekrar ölç')
$e.ToggleTask($line)
Check 'görünümde tıklanan kutu metinde işaretlenir' ($e.Text.Contains('- [x] SN02 tekrar ölç'))
$e.ToggleTask($line)
$nw.ToggleTaskAt($line); Pump
Check 'pencerede tıklama işaretler' ($ed.Text.Contains('- [x] SN02 tekrar ölç'))
$ed.Undo() | Out-Null; Pump
Check 'Ctrl+Z tıklamayı geri alır' ($ed.Text.Contains('- [ ] SN02 tekrar ölç') -and $e.Text.Contains('- [ ] SN02 tekrar ölç'))
$e.ToggleTask($line)
Check '[[JIG-1000-0009]] ana pencerede jigi seçer' ($vm.NavigateToKey('JIG-1000-0009') -and $vm.SelectedNode.Path -like '*JIG-1000-0009*')
Check '[[AB-7000-0003]] stoğu seçer' ($vm.NavigateToKey('AB-7000-0003') -and $vm.SelectedNode.Path -eq $stock)
Check 'olmayan hedef: false, dosya oluşmaz' (-not $vm.NavigateToKey('Olmayan Not') -and -not (Test-Path (Join-Path $Data 'Olmayan Not.md')))
Check 'not adıyla: o not açılır' ($vm.NavigateToKey('2026-10-23_RevAC') -and @($win.NoteWindows).Count -eq 2)
foreach ($other in @($win.NoteWindows | Where-Object { $_ -ne $nw })) { $other.Close() }; Pump

"--- 6. Resim"
$bmp = New-Object System.Windows.Media.Imaging.WriteableBitmap(40, 30, 96, 96, [System.Windows.Media.PixelFormats]::Bgra32, $null)
$ed.Select($ed.Text.Length, 0)
Check 'yapıştırılan resim Ekler''e yazılır' ($nw.InsertImage($bmp) -and $ed.Text -match '!\[\[Ekler/yapistirilan_\d{8}_\d{6}\.png\]\]' -and @(Get-ChildItem (Join-Path $stock 'Ekler') -Filter *.png).Count -eq 1)
$outside = Join-Path $env:TEMP ('dis-resim-' + [guid]::NewGuid().ToString('N').Substring(0,6) + '.png')
$enc = New-Object System.Windows.Media.Imaging.PngBitmapEncoder; $enc.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($bmp)); $fs = [IO.File]::Create($outside); $enc.Save($fs); $fs.Close()
$nw.InsertImageFile($outside)
Check 'kök dışı resim kopyalanır, aslı yerinde' ((Test-Path $outside) -and (Test-Path (Join-Path $stock ('Ekler\' + (Split-Path $outside -Leaf)))) -and $ed.Text.Contains('![[Ekler/' + (Split-Path $outside -Leaf) + ']]'))
Remove-Item $outside -ErrorAction SilentlyContinue
$inside = Join-Path $Data 'ENVANTER\Jigler\JIG-1000-0009_SATIMO_adaptor_plakasi\03_Fotograflar\foto_02.png'
$nw.InsertImageFile($inside)
Check 'kök içi resim kopyalanmaz, köke göre yol' ($ed.Text.Contains('![[ENVANTER/Jigler/JIG-1000-0009_SATIMO_adaptor_plakasi/03_Fotograflar/foto_02.png]]') -and -not (Test-Path (Join-Path $stock 'Ekler\foto_02.png')))
$nw.RenderPreview(); Wait
Check 'görünümde 4 resim' ($nw.LastRender.Images.Count -eq 4) ([string]$nw.LastRender.Images.Count)

"--- 7. Kaydetme"
$nw.HandleKey($K::S, $true, $false) | Out-Null
$bytes = [IO.File]::ReadAllBytes($note)
Check 'Ctrl+S: kaydedildi, BOM korundu' (-not $e.IsDirty -and $bytes[0] -eq 0xEF -and $e.SaveText -match '^Kaydedildi \d\d:\d\d$') $e.SaveText
$disk = [IO.File]::ReadAllText($note)
Check 'diskteki metin aynı, CRLF korunur' ($disk -eq $ed.Text -and $disk.Contains("`r`n") -and -not ($disk -match "(?<!`r)`n"))
Check 'geçici dosya kalmadı' (@(Get-ChildItem $stock -Filter '*.tmp-*').Count -eq 0)

"--- 8. Dış değişiklik"
Start-Sleep -Milliseconds 50
[IO.File]::WriteAllText($note, $disk + "`r`nObsidian'dan eklendi`r`n", (New-Object Text.UTF8Encoding $true)); [IO.File]::SetLastWriteTimeUtc($note, [DateTime]::UtcNow.AddSeconds(5))
Check 'değişiklik yokken sessizce yüklenir' ($e.CheckExternal() -and $e.Text.Contains("Obsidian'dan eklendi") -and -not $e.IsDirty)
$ed.Select($ed.Text.Length, 0); $ed.SelectedText = 'benim'
[IO.File]::WriteAllText($note, $disk + "`r`nikinci dış`r`n", (New-Object Text.UTF8Encoding $true)); [IO.File]::SetLastWriteTimeUtc($note, [DateTime]::UtcNow.AddSeconds(10))
$e.CheckExternal() | Out-Null
Check 'değişiklik varken uyarı şeridi, metin korunur' ($e.IsExternallyChanged -and $e.Text.EndsWith('benim'))
Snap $nw "$Out\note-external.png"
$e.KeepMine()
Check 'benimkini koru' (-not $e.IsExternallyChanged -and $e.IsDirty)

"--- 9. Ayarlar"
$e.PreviewSize = 18; $e.Wrap = $false; $e.EditorFont = 'Consolas'; Pump
Check 'ayarlar kaydedildi' ($vm.Settings.NoteEditor.PreviewSize -eq 18 -and -not $vm.Settings.NoteEditor.Wrap -and $vm.Settings.NoteEditor.EditorFont -eq 'Consolas')
Check 'düz görünüme uygulandı' ($ed.TextWrapping -eq 'NoWrap' -and $ed.FontFamily.Source -eq 'Consolas')
Check 'görünüm yeni boyutla çizildi' ($nw.FindName('Preview').Document.FontSize -eq 18)
$e.ReadingWidth = 680; Pump
Check 'okuma genişliği: içerik ortada' ($nw.FindName('Preview').Document.PagePadding.Left -gt 32) ([string]$nw.FindName('Preview').Document.PagePadding.Left)
$e.PreviewSize = 15; $e.Wrap = $true; $e.EditorFont = 'Cascadia Mono'; $e.ReadingWidth = 860

"--- 9b. Bul / değiştir, geri alma"
$fb = $nw.FindName('FindBox'); $rb = $nw.FindName('ReplaceBox'); $fc = $nw.FindName('FindCount')
$orig = $ed.Text
$si = $ed.Text.IndexOf('SN02'); $ed.Select($si, 4)
$nw.HandleKey($K::F, $true, $false) | Out-Null; Pump
Check 'Ctrl+F: çubuk açılır, seçili metin aranır' ($nw.IsFindOpen -and $fb.Text -eq 'SN02' -and $nw.FindName('ReplaceRow').Visibility -ne 'Visible' -and $fb.IsKeyboardFocusWithin)
$expect = [regex]::Matches($ed.Text, 'ölç', 'IgnoreCase').Count
$fb.Text = 'ölç'; Pump
Check 'yazdıkça arar, eşleşme seçilir' ($nw.FindResult.Matches.Count -eq $expect -and $expect -ge 2 -and $ed.SelectedText -ieq 'ölç') ("$($nw.FindResult.Matches.Count) / $expect")
$first = $ed.SelectionStart
$nw.HandleGlobalKey($K::F3, $false, $false) | Out-Null
Check 'F3: sonraki' ($ed.SelectionStart -ne $first -and $ed.SelectedText -ieq 'ölç' -and $fc.Text -match "^\d+ / $expect$") $fc.Text
$nw.HandleGlobalKey($K::F3, $false, $true) | Out-Null
Check 'Shift+F3: önceki' ($ed.SelectionStart -eq $first)
for ($x = 0; $x -lt $expect; $x++) { $nw.FindNext() }
Check 'sona gelince başa döner' ($ed.SelectionStart -eq $first)
$e.FindMatchCase = $true; $nw.FindNext()
$cs = [regex]::Matches($ed.Text, 'ölç').Count
Check 'büyük / küçük harf: daha az sonuç, ayar kaydedildi' ($nw.FindResult.Matches.Count -eq $cs -and $cs -lt $expect -and $vm.Settings.NoteEditor.FindMatchCase) ("$cs < $expect")
$e.FindMatchCase = $false
$e.FindRegex = $true; $fb.Text = 'SN0('; Pump
Check 'geçersiz ifade gösterilir' ($fc.Text -eq 'Geçersiz ifade') $fc.Text
$fb.Text = 'SN0(\d)'; Pump
Check 'düzenli ifade arar' ($nw.FindResult.Matches.Count -ge 1 -and $ed.SelectedText -like 'SN0*')
$nw.HandleGlobalKey($K::R, $true, $false) | Out-Null; Pump
Check 'Ctrl+R: değiştir satırı açılır' ($nw.FindName('ReplaceRow').Visibility -eq 'Visible')
$rb.Text = 'Seri-$1'; $before = $ed.Text
$nw.ReplaceCurrent()
Check 'değiştir: $1 grubu ile' ($ed.Text -ne $before -and $ed.Text -match 'Seri-\d' -and $e.IsDirty)
$ed.Undo()
Check 'Ctrl+Z: değiştirme geri alınır' ($ed.Text -eq $before)
$e.FindRegex = $false; $fb.Text = 'ölç'; $rb.Text = 'ÖLÇ'; Pump
$n = $nw.ReplaceEverything()
Check 'tümünü değiştir' ($n -eq $expect -and -not $ed.Text.Contains('ölç') -and $fc.Text -eq "$expect değiştirildi") $fc.Text
Snap $nw "$Out\note-find.png"
$ed.Undo()
Check 'Ctrl+Z: tümü tek adımda geri alınır' ($ed.Text -eq $orig)
$nw.HandleGlobalKey($K::Escape, $false, $false) | Out-Null
Check 'Esc: çubuk kapanır' (-not $nw.IsFindOpen)
$bi = $ed.Text.IndexOf('Ölçüm düzeni'); $ed.Select($bi, 5); $nw.HandleKey($K::B, $true, $false) | Out-Null
Check 'Ctrl+B uygulandı' ($ed.Text.Contains('**Ölçüm** düzeni'))
$ed.Undo()
Check 'Ctrl+Z: biçim geri alınır (geçmiş korunur)' ($ed.Text -eq $orig)
$li = $ed.Text.IndexOf('- [x] Kablo'); $ed.Select($li + 3, 0)
$nw.MoveLine($true)
Check 'Alt+↑: satır yukarı' ($ed.Text -match "- \[x\] Kablo değişti`r`n- \[.\] SN02" -and $ed.SelectionStart -lt $li)
$ed.Undo(); Check 'geri alınır' ($ed.Text -eq $orig)
$ed.Select($li + 3, 0); $nw.HandleKey($K::D, $true, $false) | Out-Null
Check 'Ctrl+D: satır çoğalır' ($ed.Text.Contains("- [x] Kablo değişti`r`n- [x] Kablo değişti"))
$ed.Undo(); $ed.Select($li + 3, 0); $nw.HandleKey($K::K, $true, $true) | Out-Null
Check 'Ctrl+Shift+K: satır silinir' (-not $ed.Text.Contains('Kablo değişti'))
$ed.Undo(); Check 'geri alınır (2)' ($ed.Text -eq $orig)
$end = $ed.Text.Length; $ed.Select($end, 0)
Check 'parantez yazınca kapanır' ($nw.TypeChar([char]'(') -and $ed.Text.EndsWith('()') -and $ed.CaretIndex -eq $end + 1)
$nw.HandleKey($K::Back, $false, $false) | Out-Null
Check 'Backspace boş çifti siler' ($ed.Text -eq $orig)
$wi = $ed.Text.IndexOf('Ölçüm düzeni'); $ed.Select($wi, 5)
Check 'seçiliyken * sarar' ($nw.TypeChar([char]'*') -and $ed.Text.Contains('*Ölçüm* düzeni') -and $ed.SelectedText -eq 'Ölçüm')
$ed.Undo(); $ed.Select($wi, 5)
Check 'seçime adres yapıştır: bağlantı' ($nw.PasteText('https://example.com/olcum') -and $ed.Text.Contains('[Ölçüm](https://example.com/olcum) düzeni'))
$ed.Undo(); Check 'geri alınır (3)' ($ed.Text -eq $orig)
$e.AutoPair = $false; $ed.Select($end, 0)
Check 'ayar kapalı: çift eklenmez, kaydedildi' (-not $nw.TypeChar([char]'(') -and -not $vm.Settings.NoteEditor.AutoPair)
$e.AutoPair = $true
$ti = $ed.Text.IndexOf('| SATIMO'); $ed.Select($ti + 3, 0)
$nw.HandleKey($K::T, $true, $true) | Out-Null
$tlines = @($ed.Text -split "`r`n" | Where-Object { $_.StartsWith('|') })
Check 'Ctrl+Shift+T: tablo hizalanır' ($tlines.Count -eq 4 -and @($tlines | ForEach-Object { $_.Length } | Select-Object -Unique).Count -eq 1 -and $tlines[1].StartsWith('| :----- | :---: |')) ($tlines -join ' / ')
$nw.HandleKey($K::D1, $true, $false) | Out-Null; Wait; $ed.Select($ed.Text.IndexOf('| Sistem'), 0); $nw.FindName('Editor').ScrollToLine($ed.GetLineIndexFromCharacterIndex($ed.CaretIndex)); Wait
Snap $nw "$Out\note-table.png"
$nw.HandleKey($K::D2, $true, $false) | Out-Null; Wait; $ed.Select($ed.Text.IndexOf('| SATIMO') + 3, 0)
$nw.HandleKey($K::Tab, $false, $false) | Out-Null
Check 'tabloda Tab: sonraki hücre seçilir' ($ed.SelectedText -eq 'Geçti')
$nw.HandleKey($K::Tab, $false, $true) | Out-Null
Check 'tabloda Shift+Tab: önceki hücre' ($ed.SelectedText -eq 'SATIMO')
$ed.Undo(); Check 'hizalama geri alınır' ($ed.Text -eq $orig)
$ed.Select(0, 0); $nw.HandleKey($K::T, $true, $true) | Out-Null
Check 'tablo dışında: metin aynı, uyarı' ($ed.Text -eq $orig -and $e.Status -match 'tablonun içinde değil') $e.Status
$nw.HandleKey($K::D3, $true, $false) | Out-Null; Wait
$nw.HandleGlobalKey($K::F, $true, $false) | Out-Null; Pump
Check 'yalnız görünümde Ctrl+F: yan yanaya geçer' ($e.IsSplitMode -and $nw.IsFindOpen)
$nw.CloseFind()

"--- 10. Kapatırken"
$nw.Close(); Pump
Check 'kaydedilmemişse kapanmaz, soru açılır' ($nw.IsVisible -and $nw.FindName('CloseDialog').IsOpen)
Snap $nw "$Out\note-close.png"
$nw.GetType().GetMethod('CloseDialog_Discard', [Reflection.BindingFlags]'NonPublic,Instance').Invoke($nw, @($null, $null)); Wait
Check 'kaydetme seçilince kapanır, disk değişmedi' (-not $nw.IsVisible -and -not ([IO.File]::ReadAllText($note).EndsWith('benim')) -and @($win.NoteWindows).Count -eq 0)

"--- 11. md kılavuzu çizilir"
$guide = [IO.File]::ReadAllText((Join-Path $PSScriptRoot '..\..\docs\MD_KILAVUZU.md'))
$gctx = New-Object Pusula.Wpf.Markdown.MarkdownRenderContext
$gdoc = [Pusula.Wpf.Markdown.MarkdownRenderer]::Render($guide, $gctx)
$gblocks = Blocks $gdoc
$gtext = (New-Object System.Windows.Documents.TextRange($gdoc.ContentStart, $gdoc.ContentEnd)).Text
Check 'kılavuz: tablolar' (@($gblocks | Where-Object { $_ -is [System.Windows.Documents.Table] }).Count -eq 5) (@($gblocks | Where-Object { $_ -is [System.Windows.Documents.Table] }).Count)
Check 'kılavuz: uyarı kutuları' (@($gblocks | Where-Object { $_ -is [System.Windows.Documents.Section] -and $_.BorderThickness.Left -eq 3 }).Count -ge 4)
Check 'kılavuz: kod bloğundaki yorum örneği görünür' ($gtext.Contains('<!-- Bu bir yorumdur') -and $gtext.Contains('%% Obsidian yorumu'))
Check 'kılavuz: son başlık var' ($gtext.Contains('Tam örnek not'))
$R = [Pusula.Wpf.Markdown.MarkdownRenderer]
Check 'başlık bağlantısı: GitHub ve Obsidian biçimi aynı' ($R::AnchorKey('3-yazı-biçimi-kalın-italik-') -eq $R::AnchorKey('3. Yazı Biçimi (kalın, italik …)') -and $R::AnchorKey('İçindekiler') -eq $R::AnchorKey('içindekiler'))
$gparsed = [Pusula.Core.Markdown.MarkdownNotes]::Parse($guide)
$hkeys = @([Markdig.Syntax.MarkdownObjectExtensions]::Descendants($gparsed) | Where-Object { $_ -is [Markdig.Syntax.HeadingBlock] } | ForEach-Object { $R::AnchorKey((@([Markdig.Syntax.MarkdownObjectExtensions]::Descendants($_.Inline) | Where-Object { $_ -is [Markdig.Syntax.Inlines.LiteralInline] } | ForEach-Object { $_.Content.ToString() }) -join '')) })
$anchors = @([Markdig.Syntax.MarkdownObjectExtensions]::Descendants($gparsed) | Where-Object { $_ -is [Markdig.Syntax.Inlines.LinkInline] -and "$($_.Url)".StartsWith('#') } | ForEach-Object { $_.Url })
$broken = @($anchors | Where-Object { $hkeys -notcontains $R::AnchorKey([Uri]::UnescapeDataString($_.Substring(1))) })
Check 'kılavuz: içindekiler bağlantıları başlıklara denk' ($anchors.Count -ge 20 -and $broken.Count -eq 0) ("$($anchors.Count) bağlantı; kırık: " + ($broken -join ', '))
$toc = @($gblocks | Where-Object { $_ -is [System.Windows.Documents.List] -and $_.MarkerStyle -eq 'Decimal' -and $_.ListItems.Count -eq 20 })
Check 'iki basamaklı numaraya yer var (20 maddelik liste)' ($toc.Count -eq 1 -and $toc[0].Padding.Left -ge 30) $(if ($toc.Count) { $toc[0].Padding.Left })
$gview = New-Object System.Windows.Controls.FlowDocumentScrollViewer; $gview.Document = $gdoc; $gview.Width = 900; $gview.Height = 1400
$gwin = New-Object System.Windows.Window; $gwin.SetResourceReference([System.Windows.Controls.Control]::BackgroundProperty, 'SurfaceBaseBrush'); $gwin.Content = $gview; $gwin.Width = 920; $gwin.Height = 1440; $gwin.ShowInTaskbar = $false; $gwin.WindowStyle = 'None'; $gwin.Left = -3000; $gwin.Show(); Pump
Snap $gwin "$Out\md-kilavuzu.png"; $gwin.Close()

"--- 12. Başlık paneli"
$gnote = Join-Path $stock 'MD_KILAVUZU.md'; [IO.File]::WriteAllText($gnote, $guide, (New-Object Text.UTF8Encoding $true))
$vm.OpenNoteOrFolder($gnote, $stock); Pump
$gw = @($win.NoteWindows)[0]; $gw.WindowStartupLocation = 'Manual'; $gw.Left = -20000; $gw.Top = -20000; $gw.Width = 1500; $gw.Height = 940; Wait
$ge = $gw.ViewModel; $ged = $gw.FindName('Editor')
Check 'panel kapalı başlar' (-not $gw.IsOutlineOpen)
$gw.HandleGlobalKey($K::O, $true, $true) | Out-Null; Wait
$expectHeads = @([Markdig.Syntax.MarkdownObjectExtensions]::Descendants($gparsed) | Where-Object { $_ -is [Markdig.Syntax.HeadingBlock] }).Count
Check 'Ctrl+Shift+O: panel açılır, başlıklar listelenir, ayar kaydedildi' ($gw.IsOutlineOpen -and $gw.OutlineItems.Count -eq $expectHeads -and $vm.Settings.NoteEditor.ShowOutline) ("$($gw.OutlineItems.Count) / $expectHeads")
Check 'panel başlığı sayıyla' ($gw.FindName('OutlineHeader').Text -eq "Başlıklar · $expectHeads") $gw.FindName('OutlineHeader').Text
$target = -1; for ($x = 0; $x -lt $gw.OutlineItems.Count; $x++) { if ($gw.OutlineItems[$x].Title -like '12. Yorum*') { $target = $x } }
$gw.GoToHeading($target); Wait
$gline = $ged.GetLineIndexFromCharacterIndex($ged.CaretIndex)
Check 'başlığa gidildi: imleç başlıkta' ($ged.Text.Substring($ged.CaretIndex, 12) -eq '## 12. Yorum')
Check 'düz metin başlığa kaydı' ($gline -ge $ged.GetFirstVisibleLineIndex() -and $gline -le $ged.GetLastVisibleLineIndex())
$gscroll = $gw.GetType().GetMethod('FindScroll', [Reflection.BindingFlags]'NonPublic,Static').Invoke($null, @($gw.FindName('Preview')))
$hp = $gw.LastRender.Headings[$target]; $hr = $hp.ContentStart.GetCharacterRect('Forward')
Check 'görünüm başlığa kaydı (üstte)' ($gscroll.VerticalOffset -gt 500 -and $hr.Top -ge -5 -and $hr.Top -lt 200) ("offset=$($gscroll.VerticalOffset) top=$($hr.Top)")
Check 'imlecin başlığı panelde seçili' ($gw.FindName('OutlineList').SelectedIndex -eq $target)
$ged.Select($ged.Text.IndexOf('## 3. Yazı'), 0); Pump
Check 'imleç değişince seçim izler' ($gw.OutlineItems[$gw.FindName('OutlineList').SelectedIndex].Title -like '3. Yazı*')
Snap $gw "$Out\note-outline.png"
"--- 13. Yan yana birlikte kaydırma"
function HeadIdx($prefix) { for ($x = 0; $x -lt $gw.OutlineItems.Count; $x++) { if ($gw.OutlineItems[$x].Title -like "$prefix*") { return $x } }; -1 }
function PreviewTop($i) { $r = $gw.LastRender.Headings[$i].ContentStart.GetCharacterRect('Forward'); $r.Top }
function EditorTop($i) { $ged.GetRectFromCharacterIndex($gw.OutlineItems[$i].Start).Top }
Check 'varsayılan: birlikte kaydır açık' ($ge.SyncScroll)
$h17 = HeadIdx '17. Pusula'
$ged.ScrollToVerticalOffset($ged.VerticalOffset + (EditorTop $h17) - $ged.Padding.Top); Wait
Check 'düz metin kayınca görünüm aynı başlığa gelir' ([Math]::Abs((PreviewTop $h17) - (EditorTop $h17)) -lt 60 -and (PreviewTop $h17) -ge -5 -and (PreviewTop $h17) -lt 120) ("görünüm=$(PreviewTop $h17) düz=$(EditorTop $h17)")
Check 'kaydırınca panel en üstteki başlığı izler (imleç ekran dışında)' ($gw.FindName('OutlineList').SelectedIndex -eq $h17) ("seçili=$($gw.FindName('OutlineList').SelectedIndex) beklenen=$h17")
Snap $gw "$Out\note-sync.png"
$h8 = HeadIdx '8. Tablo'
$gscroll.ScrollToVerticalOffset($gscroll.VerticalOffset + (PreviewTop $h8) - 12); Wait
$gw.SyncEditorToPreview(); Wait
Check 'görünüm kayınca düz metin aynı başlığa gelir' ((EditorTop $h8) -ge 0 -and (EditorTop $h8) -lt 120) ("düz=$(EditorTop $h8)")
$h4 = HeadIdx '4. Listeler'
$ged.ScrollToVerticalOffset($ged.VerticalOffset + (EditorTop $h4) - $ged.Padding.Top + 37); Wait
Check 'başlıklar arası orantılı (aradaki satır)' ((PreviewTop $h4) -lt 30 -and (PreviewTop $h4) -gt -120) ("görünüm=$(PreviewTop $h4)")
$ge.SyncScroll = $false; Pump
$before = $gscroll.VerticalOffset
$ged.ScrollToVerticalOffset(0); Wait
Check 'kapalıyken görünüm kıpırdamaz, ayar kaydedildi' ([Math]::Abs($gscroll.VerticalOffset - $before) -lt 1 -and -not $vm.Settings.NoteEditor.SyncScroll)
$ge.SyncScroll = $true
$gw.ToggleOutline(); Pump
Check 'panel kapanır, ayar kaydedildi' (-not $gw.IsOutlineOpen -and -not $vm.Settings.NoteEditor.ShowOutline)
$gw.Close(); Pump
"SONUÇ OK=$ok HATA=$fail"
