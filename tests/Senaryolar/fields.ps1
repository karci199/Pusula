param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
. (Join-Path $PSScriptRoot '..\_Ortak.ps1')
# Türe özel alanlar: kablo ve jig için hazır alanlar, alt türe göre görünme, düzenleme, kayit.json + not YAML'ı, Bilgiler kartı.
[System.Threading.SynchronizationContext]::SetSynchronizationContext((New-Object System.Windows.Threading.DispatcherSynchronizationContext))
$app = New-Object Pusula.Wpf.App; $app.InitializeComponent(); $app.ShutdownMode = 'OnExplicitShutdown'; [Pusula.Wpf.Themes]::Apply([Pusula.Wpf.Themes]::Find('FluentDark'))
$win = New-Object Pusula.Wpf.MainWindow
$win.WindowStartupLocation = 'Manual'; $win.Left = -20000; $win.Top = -20000; $win.ShowActivated = $false; $win.Width = 1240; $win.Height = 760
$win.Show(); Pump
$vm = $win.DataContext; $vm.RootPath = $Data; Wait
$readRecord = [Pusula.Core.Tracking.JsonFile].GetMethod('TryRead').MakeGenericMethod([Pusula.Core.Inventory.InventoryRecord])
function Rec($folder) { $readRecord.Invoke($null, @([Pusula.Core.Inventory.InventoryService]::RecordFile($folder))) }
function Keys() { @($vm.EditFields | ForEach-Object { $_.Key }) }
function Input($key) { $vm.EditFields | Where-Object { $_.Key -eq $key } | Select-Object -First 1 }
function Body($path) { $t = [IO.File]::ReadAllText($path); $i = $t.IndexOf("`n---", 4); if ($i -lt 0) { $t } else { $t.Substring($i + 4) } }
function Yaml($path) { $t = [IO.File]::ReadAllText($path); $i = $t.IndexOf("`n---", 4); if ($i -lt 0) { '' } else { $t.Substring(0, $i) } }
function Fact($label) { (@($vm.Inventory.Facts) + @($vm.Inventory.Properties) | Where-Object { $_.Label -eq $label } | Select-Object -First 1).Value }

"--- 1. Türler: hazır alanlar (eski sürümlü turler.json göçü)"
$types = [Pusula.Core.Types.TypeStore]::LoadOrCreate($Data)
$cableType = $types.Find('cable'); $jigType = $types.Find('jig'); $swType = $types.Find('software')
Check 'kablo alanları (adet, uç, faz kararlı, protokol yok)' ($cableType.Fields.Count -eq 8 -and -not (@($cableType.Fields | % Key) -contains 'uc_a') -and -not (@($cableType.Fields | % Key) -contains 'faz_kararli') -and -not (@($cableType.Fields | % Key) -contains 'protokol') -and -not (@($cableType.Fields | % Key) -contains 'adet')) ([string]$cableType.Fields.Count)
Check 'jig alanları (adet yok)' ($jigType.Fields.Count -eq 1 -and -not (@($jigType.Fields | % Key) -contains 'olcum_sistemi') -and -not (@($jigType.Fields | % Key) -contains 'adet')) ([string]$jigType.Fields.Count)
Check 'yazılım envanter türü' ($swType -ne $null -and $swType.IsInventory)
Check 'RF kablo: frekans var, protokol yok' ((@($cableType.FieldsFor('RF') | % Key) -contains 'frekans') -and -not (@($cableType.FieldsFor('RF') | % Key) -contains 'protokol'))
Check 'Haberleşme: pin yapısı (protokol etiket oldu)' (-not (@($cableType.FieldsFor('Haberleşme') | % Key) -contains 'protokol') -and (@($cableType.FieldsFor('Haberleşme') | % Key) -contains 'pin_yapisi'))
Check 'kablo alt türleri: RF, Haberleşme, Güç, Diğer' (($cableType.SubTypes -join ',') -eq 'RF,Haberleşme,Güç,Diğer') ($cableType.SubTypes -join ',')
# Sürüm 7: kablo uçları (konnektör) alan değil; ileride etiket / not.
Check 'uç alanları kalktı' (-not (@($cableType.Fields | % Key) -contains 'uc_b'))
Check 'Güç: AWG kesit, akım' ((@($cableType.FieldsFor('Güç') | % Key) -contains 'kesit_awg') -and (@($cableType.FieldsFor('Güç') | % Key) -contains 'akim') -and -not (@($cableType.FieldsFor('RF') | % Key) -contains 'kesit_awg'))
Check 'jig: alt türler ölçüm altyapısı, alanlar her alt türde' (($jigType.SubTypes -join ',') -eq 'SATIMO,Yankısız oda,Kompakt range,Test sahası,Diğer' -and (@($jigType.FieldsFor('SATIMO') | % Key) -contains 'kullanim_amaci') -and (@($jigType.FieldsFor('Diğer') | % Key) -contains 'kullanim_amaci')) ($jigType.SubTypes -join ',')
Check 'yazılımda alt tür yok' ($swType.SubTypes.Count -eq 0)
Check 'eski alt tür adı yeni karşılığıyla eşleşir (RF kablo → RF)' ($cableType.CanonicalSubType('RF kablo') -eq 'RF' -and $cableType.CanonicalSubType('Güç / veri') -eq 'Güç' -and $jigType.CanonicalSubType('Mekanik') -eq 'Mekanik')

"--- 1b. Sürüm 3 → 4 dönüşümü (Güç / veri, Kablaj, Adet, mm² kesit)"
$old = [Pusula.Core.Types.TypeSet]::CreateDefault(); $oc = $old.Find('cable')
$oc.SubTypes.Clear(); foreach ($s in 'RF', 'Haberleşme', 'Güç / veri', 'Kablaj') { $oc.SubTypes.Add($s) }
$oc.Fields.RemoveAll([Predicate[Pusula.Core.Types.FieldDefinition]]{ param($x) $x.Key -eq 'kesit_awg' }) | Out-Null
foreach ($x in $oc.Fields) { for ($i = 0; $i -lt $x.SubTypes.Count; $i++) { if ($x.SubTypes[$i] -eq 'Güç') { $x.SubTypes[$i] = 'Güç / veri' } } }
$ua = New-Object Pusula.Core.Types.FieldDefinition; $ua.Key = 'uc_a'; $ua.Name = 'Uç A'; $ua.Kind = 'choice'; $ua.Choices.Add('SMA (E)'); $ua.Choices.Add('Özel uç'); $oc.Fields.Add($ua)
$adet = New-Object Pusula.Core.Types.FieldDefinition; $adet.Key = 'adet'; $adet.Name = 'Adet'; $adet.Kind = 'number'; $oc.Fields.Insert(0, $adet)
$mm = New-Object Pusula.Core.Types.FieldDefinition; $mm.Key = 'kesit'; $mm.Name = 'Kesit'; $mm.Kind = 'number'; $mm.Unit = 'mm²'; $mm.SubTypes.Add('Güç / veri'); $oc.Fields.Add($mm)
$jadet = New-Object Pusula.Core.Types.FieldDefinition; $jadet.Key = 'adet'; $jadet.Name = 'Adet'; $jadet.Kind = 'number'; $old.Find('jig').Fields.Insert(0, $jadet)
$old.SchemaVersion = 3; $old.Normalize()
Check 'dönüşüm: alt türler RF, Haberleşme, Güç, Diğer (sıra korunur)' (($oc.SubTypes -join ',') -eq 'RF,Haberleşme,Güç,Diğer') ($oc.SubTypes -join ',')
Check 'dönüşüm: alanların alt türü de Güç oldu' ((@($oc.FieldsFor('Güç') | % Key) -contains 'akim') -and -not (@($oc.Fields | ForEach-Object { $_.SubTypes }) -contains 'Güç / veri'))
Check 'dönüşüm: adet kablo ve jigden kalktı' (-not (@($oc.Fields | % Key) -contains 'adet') -and -not (@($old.Find('jig').Fields | % Key) -contains 'adet'))
Check 'dönüşüm: mm² kesit yerine AWG' (-not (@($oc.Fields | % Key) -contains 'kesit') -and (@($oc.FieldsFor('Güç') | % Key) -contains 'kesit_awg'))
Check 'dönüşüm: uç alanları sonunda kalktı (sürüm 7)' (-not (@($oc.Fields | % Key) -contains 'uc_a') -and $old.SchemaVersion -ge 7)

"--- 2. RF kablo: alanlar alt türe göre"
$rf = Join-Path $Data 'ENVANTER\Kablolar\JT-0001-0001_SATIMO_RF_kablosu'
$note = Join-Path $rf 'JT-0001-0001.md'
$bodyBefore = Body $note
Check 'düzenle açılır, alanlar görünür' ($vm.OpenEdit($rf) -and $vm.HasEditFields)
Check 'RF alanları (faz kararlı etiket oldu)' ((Keys) -contains 'frekans' -and -not ((Keys) -contains 'faz_kararli') -and -not ((Keys) -contains 'uc_a') -and -not ((Keys) -contains 'protokol')) ((Keys) -join ',')
Check 'alan ipuçları: birimli alanda birim (m), birimsiz alanda yalın ad' ((Input 'boy').Placeholder -eq 'm' -and (Input 'frekans').Label -eq 'Frekans bandı') ((Input 'boy').Placeholder + ' / ' + (Input 'frekans').Label)
Check 'RF etiket önerisinde faz_kararli' (@($vm.TagSuggestions() | % Key) -contains '#faz_kararli')
Check 'boy etiketi birimli' ((Input 'boy').Label -eq 'Boy (m)') ((Input 'boy').Label)
(Input 'boy').Value = '1'
(Input 'frekans').Value = '0.5-18'; $vm.EditTags = '#faz_kararli'
$vm.EditSubType = 'Haberleşme'; Pump
Check 'alt tür değişince alanlar değişir' ((Keys) -contains 'pin_yapisi' -and -not ((Keys) -contains 'frekans')) ((Keys) -join ',')
Check 'ortak alan değeri korunur' ((Input 'boy').Value -eq '1')
$vm.EditSubType = 'RF'; Pump
Check 'geri dönünce değer korunmuş' ((Input 'frekans').Value -eq '0.5-18' -and $vm.EditTags -eq '#faz_kararli')
Snap $win "$Out\fields-edit-rf.png"
Check 'kaydedildi' ($vm.ConfirmEdit())
$r = Rec $rf
Check 'kayit.json alanları' ($r.Fields['boy'] -eq '1' -and ($r.Tags -contains 'faz_kararli')) (($r.Fields.Keys | % { $_ + '=' + $r.Fields[$_] }) -join ', ')
Check 'boş alan yazılmaz' (-not $r.Fields.ContainsKey('protokol'))
$y = Yaml $note
Check 'YAML: boy birimli' ($y -match '(?m)^boy: "?1 m"?\r?$') $y
Check 'YAML: frekans bandı, etiket' ($y -match '(?m)^frekans: ' -and $y -match '(?m)^tags: \[faz_kararli\]')
Check 'not gövdesi değişmedi' ((Body $note) -eq $bodyBefore)

"--- 3. Bilgiler kartı"
$vm.SelectNodeByPath($rf); Wait
Check 'Boy satırı birimli' ((Fact 'Boy') -eq '1 m') (Fact 'Boy')
Check 'kartta uç satırı yok' (-not (@($vm.Inventory.Properties | % Label) -contains '1. uç (konnektör)'))
Check 'faz kararlı çip olarak' (($vm.Inventory.Tags -join ' ') -eq '#faz_kararli')
Check 'başka alt türün alanı yok' (-not (@($vm.Inventory.Properties | % Label) -contains 'Pin yapısı'))
Check 'özellikler ayrı bölümde, genel bilgilerde değil' ($vm.Inventory.HasProperties -and -not (@($vm.Inventory.Facts | % Label) -contains 'Boy') -and $vm.Inventory.PropertiesHeader -match '^Özellikler · RF · \d+ / \d+ dolu$') $vm.Inventory.PropertiesHeader
$vm.SelectNodeByPath((Join-Path $Data 'ENVANTER\Kablolar\JT-0001-0003_RS422_kablosu_Brainbox')); Wait
Check 'doldurulmamış kablo: alanlar boş ve soluk, 0 dolu' ($vm.Inventory.Properties.Count -gt 0 -and @($vm.Inventory.Properties | Where-Object { -not $_.IsEmpty }).Count -eq 0 -and $vm.Inventory.PropertiesHeader -match ' 0 / ') $vm.Inventory.PropertiesHeader
$vm.SelectNodeByPath($rf); Wait
Snap $win "$Out\fields-card-rf.png"

"--- 4. Değeri boşalt"
$vm.OpenEdit($rf) | Out-Null
Check 'değerler geri yüklendi' ((Input 'frekans').Value -eq '0.5-18' -and (Input 'boy').Value -eq '1')
(Input 'frekans').Value = ''
Check 'kaydedildi' ($vm.ConfirmEdit())
Check 'kayit.json: frekans kalktı' (-not (Rec $rf).Fields.ContainsKey('frekans'))
Check 'YAML: frekans kalktı' (-not ((Yaml $note) -match '(?m)^frekans:'))
Check 'not gövdesi yine aynı' ((Body $note) -eq $bodyBefore)

"--- 4b. Özel karakter ve kodlama"
$bomBefore = ([IO.File]::ReadAllBytes($note))[0] -eq 0xEF
$vm.OpenEdit($rf) | Out-Null
(Input 'frekans').Value = 'Raf: B #3'
Check 'kaydedildi' ($vm.ConfirmEdit())
$body = $null; $fm = [Pusula.Core.Testing.FrontMatter]::Parse([IO.File]::ReadAllText($note), [ref]$body)
Check 'YAML: ":" ve "#" içeren değer tırnaklı' ((Yaml $note) -match '(?m)^frekans: "Raf: B #3"\r?$') (Yaml $note)
Check 'YAML geri okunur' ($fm['frekans'] -eq 'Raf: B #3' -and $fm['numara'] -eq 'JT-0001-0001') ($fm['frekans'])
Check 'not kodlaması korunur (BOM)' ((([IO.File]::ReadAllBytes($note))[0] -eq 0xEF) -eq $bomBefore)
Check 'gövde yine aynı' ((Body $note) -eq $bodyBefore)

"--- 5. Haberleşme kablosu: RS422"
$rs = Join-Path $Data 'ENVANTER\Kablolar\JT-0001-0002_RS422_test_kablosu'
Check 'düzenle açılır' ($vm.OpenEdit($rs))
$vm.EditSubType = 'Haberleşme'; Pump
Check 'haberleşme etiketleri RS422 / RS232' (@($vm.TagSuggestions() | % Key) -contains '#RS422' -and @($vm.TagSuggestions() | % Key) -contains '#RS232')
$vm.EditTags = '#RS422'; (Input 'pin_yapisi').Value = 'DB9 2-3-5'
Check 'kaydedildi' ($vm.ConfirmEdit())
$r = Rec $rs
Check 'kayit.json RS422 etiketi + pin' ($r.SubType -eq 'Haberleşme' -and ($r.Tags -contains 'RS422') -and $r.Fields['pin_yapisi'] -eq 'DB9 2-3-5')
$vm.SelectNodeByPath($rs); Wait
Check 'kartta #RS422' (($vm.Inventory.Tags -join ' ') -eq '#RS422')

"--- 6. Mekanik jig (22.11.2026: yalnız Kullanım amacı; eski malzeme alanları kalktı)"
$jig = Join-Path $Data 'ENVANTER\Jigler\JIG-1000-0009_SATIMO_adaptor_plakasi'
Check 'düzenle açılır' ($vm.OpenEdit($jig) -and $vm.EditSubType -eq 'Mekanik')
Check 'jig alanları: yalnız kullanım amacı' (((Keys) -join ',') -eq 'kullanim_amaci') ((Keys) -join ',')
(Input 'kullanim_amaci').Value = 'SATIMO direk flanşına anten bağlar'
Snap $win "$Out\fields-edit-jig.png"
Check 'kaydedildi' ($vm.ConfirmEdit())
$r = Rec $jig
Check 'kayit.json' ($r.Fields['kullanim_amaci'] -eq 'SATIMO direk flanşına anten bağlar')
Check 'geçmiş ve notlar korundu' ($r.Events.Count -eq 5 -and $r.Notes.Count -eq 1)
$vm.SelectNodeByPath($jig); Wait
Check 'kartta kullanım amacı' ((Fact 'Kullanım amacı') -eq 'SATIMO direk flanşına anten bağlar')
Snap $win "$Out\fields-card-jig.png"
$vm.OpenEdit($jig) | Out-Null
$vm.EditSubType = 'Montaj'; Pump
Check 'alt tür değişse de jig alanı aynı' ((Keys) -contains 'kullanim_amaci')
Check 'kaydedildi' ($vm.ConfirmEdit())
$y = Yaml (Join-Path $jig 'JIG-1000-0009.md')
Check 'YAML: kullanım amacı' ($y -match '(?m)^kullanim_amaci: ') $y
"SONUÇ OK=$ok HATA=$fail"
