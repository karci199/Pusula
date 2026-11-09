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
Check 'kablo alanları' ($cableType.Fields.Count -eq 10) ([string]$cableType.Fields.Count)
Check 'jig alanları' ($jigType.Fields.Count -eq 8) ([string]$jigType.Fields.Count)
Check 'yazılım envanter türü' ($swType -ne $null -and $swType.IsInventory)
Check 'RF kablo: frekans var, protokol yok' ((@($cableType.FieldsFor('RF') | % Key) -contains 'frekans') -and -not (@($cableType.FieldsFor('RF') | % Key) -contains 'protokol'))
Check 'Haberleşme: protokol, pin yapısı' ((@($cableType.FieldsFor('Haberleşme') | % Key) -contains 'protokol') -and (@($cableType.FieldsFor('Haberleşme') | % Key) -contains 'pin_yapisi'))
Check 'jig Montaj: tork var, yüzey yok' ((@($jigType.FieldsFor('Montaj') | % Key) -contains 'tork') -and -not (@($jigType.FieldsFor('Montaj') | % Key) -contains 'yuzey'))

"--- 2. RF kablo: alanlar alt türe göre"
$rf = Join-Path $Data 'ENVANTER\Kablolar\JT-0001-0001_SATIMO_RF_kablosu'
$note = Join-Path $rf 'JT-0001-0001.md'
$bodyBefore = Body $note
Check 'düzenle açılır, alanlar görünür' ($vm.OpenEdit($rf) -and $vm.HasEditFields)
Check 'RF alanları' ((Keys) -contains 'frekans' -and (Keys) -contains 'faz_kararli' -and (Keys) -contains 'uc_a' -and -not ((Keys) -contains 'protokol')) ((Keys) -join ',')
Check 'uç tipi seçim listesi' ((Input 'uc_a').IsChoice -and (Input 'uc_a').Choices -contains 'Banana' -and (Input 'uc_a').Choices -contains 'Krokodil')
Check 'faz kararlı evet/hayır' ((Input 'faz_kararli').IsYesNo)
Check 'boy etiketi birimli' ((Input 'boy').Label -eq 'Boy (m)') ((Input 'boy').Label)
(Input 'adet').Value = '2'; (Input 'boy').Value = '1'; (Input 'uc_a').Value = 'SMA (E)'; (Input 'uc_b').Value = 'SMA (E)'
(Input 'frekans').Value = '0.5-18'; (Input 'faz_kararli').IsYes = $true
$vm.EditSubType = 'Haberleşme'; Pump
Check 'alt tür değişince alanlar değişir' ((Keys) -contains 'protokol' -and -not ((Keys) -contains 'frekans')) ((Keys) -join ',')
Check 'ortak alan değeri korunur' ((Input 'adet').Value -eq '2')
$vm.EditSubType = 'RF'; Pump
Check 'geri dönünce değer korunmuş' ((Input 'frekans').Value -eq '0.5-18' -and (Input 'faz_kararli').IsYes)
Snap $win "$Out\fields-edit-rf.png"
Check 'kaydedildi' ($vm.ConfirmEdit())
$r = Rec $rf
Check 'kayit.json alanları' ($r.Fields['adet'] -eq '2' -and $r.Fields['boy'] -eq '1' -and $r.Fields['uc_a'] -eq 'SMA (E)' -and $r.Fields['faz_kararli'] -eq 'Evet') (($r.Fields.Keys | % { $_ + '=' + $r.Fields[$_] }) -join ', ')
Check 'boş alan yazılmaz' (-not $r.Fields.ContainsKey('protokol'))
$y = Yaml $note
Check 'YAML: boy birimli' ($y -match '(?m)^boy: "?1 m"?\r?$') $y
Check 'YAML: uç A, frekans, faz' ($y -match '(?m)^uc_a: ' -and $y -match '(?m)^frekans: ' -and $y -match '(?m)^faz_kararli: "?Evet')
Check 'not gövdesi değişmedi' ((Body $note) -eq $bodyBefore)

"--- 3. Bilgiler kartı"
$vm.SelectNodeByPath($rf); Wait
Check 'Boy satırı birimli' ((Fact 'Boy') -eq '1 m') (Fact 'Boy')
Check 'Uç A satırı' ((Fact 'Uç A') -eq 'SMA (E)')
Check 'Faz kararlı satırı' ((Fact 'Faz kararlı') -eq 'Evet')
Check 'başka alt türün alanı yok' (-not (@($vm.Inventory.Properties | % Label) -contains 'Protokol'))
Check 'özellikler ayrı bölümde, genel bilgilerde değil' ($vm.Inventory.HasProperties -and -not (@($vm.Inventory.Facts | % Label) -contains 'Boy') -and $vm.Inventory.PropertiesHeader -match '^Özellikler · RF · \d+ / \d+ dolu$') $vm.Inventory.PropertiesHeader
$vm.SelectNodeByPath((Join-Path $Data 'ENVANTER\Kablolar\JT-0001-0003_RS422_kablosu_Brainbox')); Wait
Check 'doldurulmamış kablo: alanlar boş ve soluk, 0 dolu' ($vm.Inventory.Properties.Count -gt 0 -and @($vm.Inventory.Properties | Where-Object { -not $_.IsEmpty }).Count -eq 0 -and $vm.Inventory.PropertiesHeader -match ' 0 / ') $vm.Inventory.PropertiesHeader
$vm.SelectNodeByPath($rf); Wait
Snap $win "$Out\fields-card-rf.png"

"--- 4. Değeri boşalt"
$vm.OpenEdit($rf) | Out-Null
Check 'değerler geri yüklendi' ((Input 'frekans').Value -eq '0.5-18' -and (Input 'adet').Value -eq '2')
(Input 'frekans').Value = ''
Check 'kaydedildi' ($vm.ConfirmEdit())
Check 'kayit.json: frekans kalktı' (-not (Rec $rf).Fields.ContainsKey('frekans'))
Check 'YAML: frekans kalktı' (-not ((Yaml $note) -match '(?m)^frekans:'))
Check 'not gövdesi yine aynı' ((Body $note) -eq $bodyBefore)

"--- 4b. Özel karakter ve kodlama"
$bomBefore = ([IO.File]::ReadAllBytes($note))[0] -eq 0xEF
$vm.OpenEdit($rf) | Out-Null
(Input 'uc_b').Value = 'Raf: B #3'; (Input 'frekans').Value = '#18 "max"'
Check 'kaydedildi' ($vm.ConfirmEdit())
$body = $null; $fm = [Pusula.Core.Testing.FrontMatter]::Parse([IO.File]::ReadAllText($note), [ref]$body)
Check 'YAML: ":" ve "#" içeren değer tırnaklı' ((Yaml $note) -match '(?m)^uc_b: "Raf: B #3"\r?$') (Yaml $note)
Check 'YAML geri okunur' ($fm['uc_b'] -eq 'Raf: B #3' -and $fm['numara'] -eq 'JT-0001-0001') ($fm['uc_b'])
Check 'not kodlaması korunur (BOM)' ((([IO.File]::ReadAllBytes($note))[0] -eq 0xEF) -eq $bomBefore)
Check 'gövde yine aynı' ((Body $note) -eq $bodyBefore)

"--- 5. Haberleşme kablosu: RS422"
$rs = Join-Path $Data 'ENVANTER\Kablolar\JT-0001-0002_RS422_test_kablosu'
Check 'düzenle açılır' ($vm.OpenEdit($rs))
$vm.EditSubType = 'Haberleşme'; Pump
Check 'protokol listesi RS422/RS232' ((Input 'protokol').Choices -contains 'RS422' -and (Input 'protokol').Choices -contains 'RS232')
(Input 'protokol').Value = 'RS422'; (Input 'pin_yapisi').Value = 'DB9 2-3-5'; (Input 'uc_a').Value = 'D-Sub 9'; (Input 'uc_b').Value = 'Açık uç'; (Input 'adet').Value = '3'
Check 'kaydedildi' ($vm.ConfirmEdit())
$r = Rec $rs
Check 'kayit.json protokol + pin' ($r.SubType -eq 'Haberleşme' -and $r.Fields['protokol'] -eq 'RS422' -and $r.Fields['pin_yapisi'] -eq 'DB9 2-3-5')
$vm.SelectNodeByPath($rs); Wait
Check 'kartta protokol' ((Fact 'Protokol') -eq 'RS422')

"--- 6. Mekanik jig"
$jig = Join-Path $Data 'ENVANTER\Jigler\JIG-1000-0009_SATIMO_adaptor_plakasi'
Check 'düzenle açılır' ($vm.OpenEdit($jig) -and $vm.EditSubType -eq 'Mekanik')
Check 'Mekanik alanları' ((Keys) -contains 'malzeme' -and (Keys) -contains 'yuzey' -and -not ((Keys) -contains 'tork') -and -not ((Keys) -contains 'olcum_sistemi')) ((Keys) -join ',')
(Input 'adet').Value = '1'; (Input 'malzeme').Value = 'Al 6061'; (Input 'olculer').Value = '300x200x12'; (Input 'yuzey').Value = 'Eloksal'
(Input 'baglanti').Value = 'M6 x 4, SATIMO direk flanşı'; (Input 'cizim_revizyonu').Value = 'AB'
Snap $win "$Out\fields-edit-jig.png"
Check 'kaydedildi' ($vm.ConfirmEdit())
$r = Rec $jig
Check 'kayit.json' ($r.Fields['malzeme'] -eq 'Al 6061' -and $r.Fields['yuzey'] -eq 'Eloksal' -and $r.Fields['cizim_revizyonu'] -eq 'AB')
Check 'geçmiş ve notlar korundu' ($r.Events.Count -eq 5 -and $r.Notes.Count -eq 1)
$vm.SelectNodeByPath($jig); Wait
Check 'kartta malzeme, ölçüler' ((Fact 'Malzeme') -eq 'Al 6061' -and (Fact 'Ölçüler') -eq '300x200x12')
Snap $win "$Out\fields-card-jig.png"
$vm.OpenEdit($jig) | Out-Null
$vm.EditSubType = 'Montaj'; Pump
Check 'Montaj: tork görünür, yüzey gizli' ((Keys) -contains 'tork' -and -not ((Keys) -contains 'yuzey'))
(Input 'tork').Value = '9'
Check 'kaydedildi' ($vm.ConfirmEdit())
$jn = Join-Path $jig 'JIG-1000-0009.md'
$y = Yaml $jn
Check 'YAML: tork birimli, yüzey başlıktan kalktı' ($y -match '(?m)^tork: "?9 Nm' -and -not ($y -match '(?m)^yuzey:')) $y
Check 'yüzey değeri kayıtta saklı' ((Rec $jig).Fields['yuzey'] -eq 'Eloksal')
"SONUÇ OK=$ok HATA=$fail"
