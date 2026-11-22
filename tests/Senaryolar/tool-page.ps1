param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
. (Join-Path $PSScriptRoot '..\_Ortak.ps1')
# Test yardımcı aracı sayfası (22.11.2026): türlerin yeni alanları (sürüm 15), fotoğraf kuralı (kapak + numaralı şerit,
# numarasızlar görünmez), not kartı (gövde, Günlük hariç), yazılımın revizyonları, olayların stok çizelgesine düşmesi.
$TS = [Pusula.Core.Types.TypeSet]

"--- 1. Türler (sürüm 15)"
$d = $TS::CreateDefault()
$keys = { param($k) @($d.Find($k).Fields | ForEach-Object Key) -join ',' }
Check 'kablaj: boy, ekran, frekans bandı, pin, gerilim, akım, kesit, amaç' ((& $keys 'cable') -eq 'boy,ekran,frekans,pin_yapisi,gerilim,akim,kesit_awg,kullanim_amaci') (& $keys 'cable')
Check 'jig: yalnız kullanım amacı' ((& $keys 'jig') -eq 'kullanim_amaci') (& $keys 'jig')
Check 'yazılım: yalnız kullanım amacı' ((& $keys 'software') -eq 'kullanim_amaci') (& $keys 'software')
Check 'cihaz ve diğer: yalnız kullanım amacı' ((& $keys 'equipment') -eq 'kullanim_amaci' -and (& $keys 'other') -eq 'kullanim_amaci')
Check 'fotoğraf alt klasörü açılmaz' (-not (@($d.Find('cable').RecordFolders + $d.Find('jig').RecordFolders) -match 'Fotograflar')) (@($d.Find('jig').RecordFolders) -join ',')
Check 'Güç alanları yalnız Güç''te' (@($d.Find('cable').FieldsFor('Güç') | ForEach-Object Key) -contains 'gerilim' -and -not (@($d.Find('cable').FieldsFor('RF') | ForEach-Object Key) -contains 'gerilim'))

# Sürüm 14 dosyası: eski alanlar (kullanıcının eklediği bir alan da var) yükseltilir.
$old = $TS::CreateDefault()
$jig = $old.Find('jig'); $jig.Fields.Clear()
foreach ($n in 'Malzeme', 'Ölçüler', 'Tork', 'Benim alanım') { $f = New-Object Pusula.Core.Types.FieldDefinition; $f.Name = $n; $f.Key = [Pusula.Core.Types.FieldDefinition]::KeyFrom($n); $f.Kind = 'text'; $jig.Fields.Add($f) }
$cable = $old.Find('cable'); $cable.Fields.RemoveAll([Predicate[Pusula.Core.Types.FieldDefinition]]{ param($x) $x.Key -in 'ekran', 'frekans', 'gerilim', 'kullanim_amaci' }) | Out-Null
$fr = New-Object Pusula.Core.Types.FieldDefinition; $fr.Name = 'Frekans'; $fr.Key = 'frekans'; $fr.Kind = 'text'; $fr.Unit = 'MHz'; $fr.SubTypes.Add('RF'); $cable.Fields.Insert(1, $fr)
$old.Find('jig').RecordFolders.Add('03_Fotograflar')
$old.SchemaVersion = 14; $old.Normalize()
Check 'göç: jig malzeme alanları kalktı, kullanıcının alanı kaldı' ((@($old.Find('jig').Fields | ForEach-Object Key) -join ',') -eq 'benim_alanim,kullanim_amaci') (@($old.Find('jig').Fields | ForEach-Object Key) -join ',')
Check 'göç: Frekans → Frekans bandı, Ekran ve Gerilim eklendi' ((@($old.Find('cable').Fields | ForEach-Object Key)) -contains 'frekans' -and (@($old.Find('cable').Fields | ForEach-Object Key)) -contains 'ekran' -and (@($old.Find('cable').Fields | ForEach-Object Key)) -contains 'gerilim' -and $old.SchemaVersion -eq 15)
Check 'göç: fotoğraf alt klasörü şablondan çıktı' (-not (@($old.Find('jig').RecordFolders) -contains '03_Fotograflar'))
$again = (@($old.Find('cable').Fields | ForEach-Object Key) -join ','); $old.SchemaVersion = 14; $old.Normalize()
Check 'göç iki kez çalışsa da aynı' ((@($old.Find('cable').Fields | ForEach-Object Key) -join ',') -eq $again)

"--- 2. Örnek araçlar ve fotoğraf kuralı"
$made = & (Join-Path $PSScriptRoot '..\..\tools\Ornek-Araclar.ps1') -Kok $Data -Bin $Bin
Check 'yedi örnek araç açıldı' (@($made | Where-Object { $_ -like 'açıldı*' }).Count -eq 7) ($made -join ' | ')
$stock = (Get-ChildItem $Data -Recurse -Directory -Filter 'AB-7000-0003' | Select-Object -First 1).FullName
$cableDir = Join-Path $stock '02_Test_Araclari\Kablo\JT-7000-0101'
$g = [Pusula.Core.Notes.StockCover]::ReadRecord($cableDir, 'JT-7000-0101')
Check 'kapak: <numara>_kapak' ([IO.Path]::GetFileName($g.Cover) -eq 'JT-7000-0101_kapak.png') $g.Cover
Check 'şerit: 1_, 2_, 3_ sırasıyla' ((@($g.Strip | ForEach-Object { [IO.Path]::GetFileName($_) }) -join ',') -eq '1_SMA_ucu.png,2_N_ucu.png,3_Etiket.png') (@($g.Strip) -join ',')
Check 'numarasız görünmez, sayılır' ($g.HiddenCount -eq 1)
Check 'şerit adı: 1_SMA_ucu → SMA ucu' ([Pusula.Core.Notes.StockCover]::StripLabelOf($g.Strip[0]) -eq 'SMA ucu')
$legacy = Join-Path $stock '02_Test_Araclari\Kablo\JT-7000-0102\02_Fotograflar'
New-Item -ItemType Directory -Force $legacy | Out-Null; Copy-Item (Join-Path $cableDir '2_N_ucu.png') (Join-Path $legacy '5_eski.png')
$g2 = [Pusula.Core.Notes.StockCover]::ReadRecord((Split-Path $legacy), 'JT-7000-0102')
Check 'eski fotoğraf klasörü de okunur' (@($g2.Strip | ForEach-Object { [IO.Path]::GetFileName($_) }) -contains '5_eski.png')

"--- 2b. Fotoğraf adı kombinasyonları"
function Combo([string[]]$names, [string]$no = 'JT-9000-0001')
{
    $dir = Join-Path $env:TEMP ('pusula-foto-' + [guid]::NewGuid().ToString('N').Substring(0, 6))
    New-Item -ItemType Directory $dir | Out-Null
    foreach ($n in $names) { $p = Join-Path $dir $n; New-Item -ItemType Directory -Force (Split-Path $p) | Out-Null; Copy-Item (Join-Path $cableDir '1_SMA_ucu.png') $p }
    $r = [Pusula.Core.Notes.StockCover]::ReadRecord($dir, $no)
    $res = [pscustomobject]@{ Cover = [IO.Path]::GetFileName($r.Cover); Strip = (@($r.Strip | ForEach-Object { [IO.Path]::GetFileName($_) }) -join ','); Hidden = $r.HiddenCount }
    Remove-Item -Recurse -Force $dir
    $res
}
$c = Combo @('2_b.png', '10_c.png', '1_a.png')
Check 'sıra sayıya göre: 1, 2, 10' ($c.Strip -eq '1_a.png,2_b.png,10_c.png') $c.Strip
$c = Combo @('kapak.jpg', 'JT-9000-0001_kapak.png')
Check 'iki kapak: numaralı olan seçilir, diğeri gizli sayılır' ($c.Cover -eq 'JT-9000-0001_kapak.png' -and $c.Hidden -eq 1) ($c.Cover + ' ' + $c.Hidden)
$c = Combo @('JT-9000-0001_KAPAK.JPG', '1-on.JPEG', '2 yan.Png')
Check 'büyük harf uzantı, tire ve boşluk ayraç' ($c.Cover -eq 'JT-9000-0001_KAPAK.JPG' -and $c.Strip -eq '1-on.JPEG,2 yan.Png') ($c.Cover + ' | ' + $c.Strip)
$c = Combo @('1_a.png', '3_c.png')
Check 'kapak yok: kapak boş, şerit numaralılar' ($c.Cover -eq '' -and $c.Strip -eq '1_a.png,3_c.png') ($c.Cover + ' | ' + $c.Strip)
$c = Combo @('IMG_1.png', 'foto.png', '12345.png')
Check 'numarasızlar gizli; yalnız sayıdan oluşan ad şeritte' ($c.Strip -eq '12345.png' -and $c.Hidden -eq 2) ($c.Strip + ' ' + $c.Hidden)
$c = Combo @('01_Belgeler\1_cizim.png', '_gizli\1_x.png', '2_b.png')
Check 'belge alt klasörü ve _ klasörü okunmaz' ($c.Strip -eq '2_b.png' -and $c.Hidden -eq 0) ($c.Strip + ' ' + $c.Hidden)
$c = Combo @('03_Fotograflar\JT-9000-0001_kapak.png', '03_Fotograflar\1_a.png', '2_b.png')
Check 'eski fotoğraf klasörü ile kök birlikte' ($c.Cover -eq 'JT-9000-0001_kapak.png' -and $c.Strip -eq '1_a.png,2_b.png') ($c.Cover + ' | ' + $c.Strip)
$c = Combo @('1_a.txt', 'not.md', '1_b.pdf')
Check 'resim olmayanlar sayılmaz' ($c.Strip -eq '' -and $c.Hidden -eq 0 -and $c.Cover -eq '')
$empty = [Pusula.Core.Notes.StockCover]::ReadRecord((Join-Path $env:TEMP 'yok-boyle-klasor'), 'X')
Check 'klasör yoksa boş, hata yok' ($empty.Photos.Count -eq 0)

"--- 3. Not gövdesi"
function Body([string]$text)
{
    $f = Join-Path $env:TEMP ('pusula-not-' + [guid]::NewGuid().ToString('N').Substring(0, 6) + '.md')
    [IO.File]::WriteAllText($f, $text, (New-Object Text.UTF8Encoding $true))
    $r = @([Pusula.Core.Notes.NoteFile]::BodyLines($f, 40)); Remove-Item $f; ,$r
}
Check 'yalnız YAML ve başlık: gövde boş' ((Body "---`r`nnumara: X`r`n---`r`n`r`n# X Ad`r`n").Count -eq 0)
Check 'yalnız Günlük: gövde boş' ((Body "# X`r`n`r`n---`r`n`r`n## Günlük`r`n`r`n### 01.01.2026 · Not · a`r`n").Count -eq 0)
Check 'YAML yok, başlık yok: ilk satırdan' ((Body "düz metin`r`nikinci").Count -eq 2)
Check 'eski "## Gunluk" başlığı da kesilir' (-not ((Body "a`r`n## Gunluk`r`n### x") -match 'x'))
$long = (1..60 | ForEach-Object { "satır $_" }) -join "`r`n"
Check 'uzun not 40 satırla sınırlı' ((Body $long).Count -eq 40)
Check 'ikinci # başlık gövdede kalır' ((Body "# Ad`r`nmetin`r`n# İkinci`r`n") -contains '# İkinci')
$lines = [Pusula.Core.Notes.NoteFile]::BodyLines((Join-Path $cableDir 'JT-7000-0101.md'), 40)
Check 'YAML, başlık ve Günlük yok' ($lines[0] -eq '## Kullanım' -and -not ($lines -match 'Günlük') -and -not ($lines -match '^numara:') -and $lines[-1] -match 'Etiket yenilendi') ($lines -join ' | ')

"--- 4. Sayfa"
$settingsFile = Join-Path $env:LOCALAPPDATA 'Pusula\ayarlar.json'
$savedSettings = if (Test-Path $settingsFile) { [IO.File]::ReadAllText($settingsFile) } else { $null }
try {
    [System.Threading.SynchronizationContext]::SetSynchronizationContext((New-Object System.Windows.Threading.DispatcherSynchronizationContext))
    $app = New-Object Pusula.Wpf.App; $app.InitializeComponent(); $app.ShutdownMode = 'OnExplicitShutdown'; [Pusula.Wpf.Themes]::Apply([Pusula.Wpf.Themes]::Find('FluentDark'))
    $win = New-Object Pusula.Wpf.MainWindow
    $win.WindowStartupLocation = 'Manual'; $win.Left = -20000; $win.Top = -20000; $win.ShowActivated = $false; $win.Width = 1400; $win.Height = 1250
    $win.Show(); Pump
    $vm = $win.DataContext; $vm.RootPath = $Data; Wait
    $folders = @{}; foreach ($i in [Pusula.Core.Inventory.InventoryService]::ReadAll($Data, $vm.InventoryTypes)) { $folders[$i.Record.Number] = $i.Folder }
    function Show([string]$n) { $vm.ShowInventory($folders[$n]); Wait; for ($k = 0; $k -lt 20; $k++) { Pump; Start-Sleep -Milliseconds 30 } }
    function Prop([string]$name) { ($vm.Inventory.Properties | Where-Object Label -eq $name | Select-Object -First 1).Value }

    Show 'JT-7000-0101'
    $inv = $vm.Inventory
    Check 'kablaj: fotoğraf kartı, kapak büyükte' ($inv.ShowGallery -and [IO.Path]::GetFileName($inv.Gallery.SelectedPhoto) -eq 'JT-7000-0101_kapak.png') $inv.Gallery.SelectedPhoto
    Check 'kablaj: şeritte 3, numarasız bilgisi' ($inv.Gallery.Thumbs.Count -eq 3 -and $inv.Gallery.HasHidden -and $inv.Gallery.Thumbs[0].Label -eq 'SMA ucu')
    $inv.Gallery.Select($inv.Gallery.Thumbs[1]); Pump
    Check 'şeritten seçilen büyükte' ([IO.Path]::GetFileName($inv.Gallery.SelectedPhoto) -eq '2_N_ucu.png')
    Check 'kablaj: alanlar (ekran, frekans bandı, amaç)' ((Prop 'Ekran') -eq 'Ekranlı' -and (Prop 'Frekans bandı') -eq '2–18 GHz' -and (Prop 'Kullanım amacı') -like 'SATIMO*') ((Prop 'Ekran') + ' / ' + (Prop 'Frekans bandı'))
    Check 'not kartı: gövde var, Günlük yok' ($inv.HasNoteLines -and ($inv.NoteLines | Where-Object IsHeading | Select-Object -First 1).Text -eq 'Kullanım' -and -not ($inv.NoteLines | Where-Object { $_.Text -match 'Günlük|Kalibrasyon' })) (($inv.NoteLines | ForEach-Object Text) -join ' | ')
    Check 'not kartı: kapanmış görev soluk' (@($inv.NoteLines | Where-Object IsDone).Count -eq 1)
    Check 'geçmiş: kalibrasyon ve tamir olayları' (@($inv.History | Where-Object { $_.Detail -like 'Faz / kayıp*' }).Count -eq 1 -and @($inv.History | Where-Object { $_.Detail -like 'SMA ucunun*' }).Count -eq 1)
    Snap $win "$Out\tool-kablaj.png"

    Show 'JT-7000-0102'
    Check 'güç kablosu: gerilim, akım, kesit' ((Prop 'Gerilim') -eq '28 V' -and (Prop 'Akım') -eq '10 A' -and (Prop 'Kesit (AWG)') -eq '16') ((Prop 'Gerilim') + ' / ' + (Prop 'Akım'))

    Show 'JT-7000-0104'
    Check 'jig: yalnız kullanım amacı, revizyon olayı geçmişte' ($vm.Inventory.Properties.Count -eq 1 -and @($vm.Inventory.History | Where-Object { $_.Detail -like 'Cıvata delikleri*' }).Count -eq 1)
    Snap $win "$Out\tool-jig.png"

    Show 'JY-7000-0105'
    $inv = $vm.Inventory
    Check 'yazılım: fotoğraf kartı yok, revizyonlar var' (-not $inv.ShowGallery -and $inv.IsSoftware -and $inv.Revisions.Count -eq 3) ($inv.Revisions.Count)
    Check 'yazılım: en yeni AC, eklenenler yazar' ($inv.Revisions[0].Name -eq 'AC' -and $inv.Revisions[0].Text -match 'Faz hesabında' -and $inv.Revisions[2].Name -eq 'AA') (($inv.Revisions | ForEach-Object { $_.Name + ':' + $_.Text }) -join ' | ')
    Check 'yazılım: Özellikler''de Revizyon AC' ((Prop 'Revizyon') -eq 'AC') (Prop 'Revizyon')
    Snap $win "$Out\tool-yazilim.png"

    Show 'JT-7000-0106'
    Check 'cihaz: kullanım amacı, kalibrasyon olayı' ((Prop 'Kullanım amacı') -like 'VNA*' -and @($vm.Inventory.History | Where-Object { $_.Detail -like 'Zayıflama*' }).Count -eq 1)
    Snap $win "$Out\tool-cihaz.png"

    Show 'JT-7000-0107'
    Check 'diğer: kapak ve bir şerit fotoğrafı' ($vm.Inventory.Gallery.HasSelectedPhoto -and $vm.Inventory.Gallery.Thumbs.Count -eq 1)
    Snap $win "$Out\tool-diger.png"

    Show 'JT-7000-0103'
    Check 'fotoğrafsız: genel resim ve adlandırma ipucu' (-not $vm.Inventory.Gallery.HasSelectedPhoto -and $vm.Inventory.PhotoRule -match 'JT-7000-0103_kapak') $vm.Inventory.PhotoRule

    "--- 5. Olay türleri ve stok çizelgesi"
    $kinds = @($vm.JournalKinds | ForEach-Object Label)
    Check 'olay türlerinde Kalibrasyon ve Tamir / bakım' ($kinds -contains 'Kalibrasyon' -and $kinds -contains 'Tamir / bakım') ($kinds -join ', ')
    $vm.SelectedNode = FindNode $vm.Nodes 'AB-7000-0003'; Wait; for ($k = 0; $k -lt 30; $k++) { Pump; Start-Sleep -Milliseconds 30 }
    Check 'stokun çizelgesinde aracın kalibrasyonu' (@($vm.Detail.Timeline | Where-Object { $_.Title -like '*Faz / kayıp*' }).Count -ge 1) (@($vm.Detail.Timeline | ForEach-Object Title) -join ' | ')

    "--- 6. İstatistik"
    $cards = @($vm.StockStatCards)
    $line = { param($card, $label) ($card.Lines | Where-Object { $_.Label -like "$label*" } | Select-Object -First 1).Count }
    Check 'stok: üç kart (doğrulama, araçlar, açık iş)' ($cards.Count -eq 3) (($cards | ForEach-Object Title) -join ', ')
    Check 'stok: araç kartında kablaj 3+, yazılım 1' ($cards[1].Total -ge 7 -and (& $line $cards[1] 'Kablaj') -ge 3 -and (& $line $cards[1] 'Yazılım') -eq 1) (($cards[1].Lines | ForEach-Object { $_.Label + '=' + $_.Count }) -join ', ')
    Check 'stok: doğrulama satırları (geçti, kaldı, sürüyor, bekliyor, plansız, raporsuz)' (@($cards[0].Lines).Count -ge 6)
    $vm.SelectedNode = $null; $vm.LoadHome(); Wait; for ($k = 0; $k -lt 40; $k++) { Pump; Start-Sleep -Milliseconds 30 }
    $anaKart = @($vm.HomeStatCards)
    $stockCount = @(Get-ChildItem $Data -Recurse -Directory | Where-Object { Test-Path (Join-Path $_.FullName '_TAKIP\stok.json') }).Count
    Check 'ana sayfa: üç kart; stok sayısı diskteki stok.json sayısı' ($anaKart.Count -eq 3 -and $anaKart[0].Total -eq $stockCount) ("kart=$($anaKart.Count) stok=$($anaKart[0].Total)/$stockCount")
    Check 'ana sayfa: araçlar toplamı = türlerin toplamı' ($anaKart[1].Total -eq (($anaKart[1].Lines | Measure-Object Count -Sum).Sum)) ($anaKart[1].Total)
    $vm.HomeTab = 3; Pump
    Check 'istatistik sekmesinde zaman süzgeci yok' (-not $vm.IsHomeRangeTab)
    Snap $win "$Out\stats-home.png"
    $kab = $anaKart[1].Lines | Where-Object { $_.Label -eq 'Kablaj' } | Select-Object -First 1
    $kab.Open.Execute($null); Pump
    Check 'kablaj sayısına tıklayınca Kayıtlar · araçlar · Kablaj' ($vm.HomeTab -eq 2 -and $vm.IsViewTools -and $vm.ToolRowCount -eq $kab.Count) ("sekme=$($vm.HomeTab) satır=$($vm.ToolRowCount) sayı=$($kab.Count)")
    $win.Close()
} finally {
    if ($savedSettings -ne $null) { [IO.File]::WriteAllText($settingsFile, $savedSettings) }
}

"SONUÇ OK=$ok HATA=$fail"
