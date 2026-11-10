param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
. (Join-Path $PSScriptRoot '..\_Ortak.ps1')
# Kayıt pencereleri (yeni kayıt, düzenle): açılır kutu kayıtlı değeri gösterir, listeden seçim hemen işler; türe özel
# alanlar iki sütun, yeni kayıtta da girilir ve kayda + notun YAML başlığına yazılır.
function FindLogical($root, [scriptblock]$match)
{
    $stack = New-Object System.Collections.Stack; $stack.Push($root)
    while ($stack.Count -gt 0)
    {
        $node = $stack.Pop()
        if (& $match $node) { return $node }
        if ($node -is [System.Windows.DependencyObject]) { foreach ($c in [System.Windows.LogicalTreeHelper]::GetChildren($node)) { if ($c -is [System.Windows.DependencyObject]) { $stack.Push($c) } } }
    }
    $null
}
function FindVisual($root, [scriptblock]$match)
{
    $stack = New-Object System.Collections.Stack; $stack.Push($root)
    while ($stack.Count -gt 0)
    {
        $node = $stack.Pop()
        if (& $match $node) { return $node }
        if ($node -is [System.Windows.Media.Visual]) { $n = [System.Windows.Media.VisualTreeHelper]::GetChildrenCount($node); for ($i = 0; $i -lt $n; $i++) { $stack.Push([System.Windows.Media.VisualTreeHelper]::GetChild($node, $i)) } }
    }
    $null
}
# Alan listesindeki n. alanın açılır kutusu (şablondan üretilen).
function FieldCombo($itemsSource, [int]$index)
{
    $list = FindLogical $win { param($x) $x -is [System.Windows.Controls.ItemsControl] -and [object]::ReferenceEquals($x.ItemsSource, $itemsSource) }
    if (-not $list) { return $null }
    $container = $list.ItemContainerGenerator.ContainerFromIndex($index)
    if (-not $container) { return $null }
    FindVisual $container { param($x) $x -is [System.Windows.Controls.ComboBox] }
}

[System.Threading.SynchronizationContext]::SetSynchronizationContext((New-Object System.Windows.Threading.DispatcherSynchronizationContext))
$app = New-Object Pusula.Wpf.App; $app.InitializeComponent(); $app.ShutdownMode = 'OnExplicitShutdown'; [Pusula.Wpf.Themes]::Apply([Pusula.Wpf.Themes]::Find('FluentDark'))
$win = New-Object Pusula.Wpf.MainWindow
$win.WindowStartupLocation = 'Manual'; $win.Left = -20000; $win.Top = -20000; $win.ShowActivated = $false; $win.Width = 1360; $win.Height = 900
$win.Show(); Pump
$vm = $win.DataContext; $vm.RootPath = $Data; Wait

"--- 1. Düzenle: alt tür kutusu"
$jig = Join-Path $Data 'ENVANTER\Jigler\JIG-1000-0009_SATIMO_adaptor_plakasi'
Check 'düzenle açılır' ($vm.OpenEdit($jig)); Wait
$combo = FindLogical $win { param($x) $x -is [System.Windows.Controls.ComboBox] -and [object]::ReferenceEquals($x.ItemsSource, $vm.EditSubTypes) }
Check 'kutu kayıtlı alt türü gösterir' ($combo -ne $null -and $vm.EditSubType -eq 'Mekanik' -and $combo.SelectedItem -eq 'Mekanik' -and -not $combo.IsEditable) "seçili=[$($combo.SelectedItem)] vm=[$($vm.EditSubType)]"
Check 'başlık: Özellikler · Mekanik' ($vm.EditFieldsHeader -eq 'Özellikler · Mekanik') $vm.EditFieldsHeader
$choice = @($vm.EditFields | Where-Object { $_.IsChoice })[0]
Check 'seçim alanı var, listede boş seçenek başta' ($choice -ne $null -and $choice.Choices[0] -eq '' -and $choice.Choices.Count -ge 2) ($choice.Choices -join '|')
Snap $win "$Out\dialog-edit-jig.png"
# Not: test penceresi ekran dışında olduğundan diyalog içi şablon kutuları üretilmez; seçim alanı değer düzeyinde sınanır,
# görüntü uygulamada elle / ekran görüntüsüyle kontrol edildi.
$pick = @($choice.Choices | Where-Object { $_ -ne '' -and $_ -ne $choice.Value })[0]
$choice.Value = $pick; Pump
Check 'seçim alanı: değer listeden' ($choice.Value -eq $pick -and $choice.Choices -contains $pick)
$saved = New-Object Pusula.Core.Types.FieldDefinition; $saved.Key = 'x'; $saved.Name = 'X'; $saved.Kind = [Pusula.Core.Types.FieldKind]::Choice; $saved.Choices.Add('A')
$fi = New-Object Pusula.Wpf.ViewModels.FieldInput($saved, 'Eski değer')
Check 'listede olmayan kayıtlı değer seçeneklere eklenir (kutuda görünsün)' ($fi.Choices[0] -eq '' -and $fi.Choices -contains 'A' -and $fi.Choices -contains 'Eski değer') ($fi.Choices -join '|')
$other = @($vm.EditSubTypes | Where-Object { $_ -ne 'Mekanik' })[0]
$combo.SelectedItem = $other; Pump
Check 'alt türü listeden seçince hemen işler' ($vm.EditSubType -eq $other -and $vm.EditFieldsHeader -eq "Özellikler · $other") "vm=$($vm.EditSubType)"
$combo.SelectedItem = 'Mekanik'; Pump
Check 'geri dönünce girilen değer kaybolmaz' (@($vm.EditFields | Where-Object { $_.Key -eq $choice.Key })[0].Value -eq $pick)
$vm.IsEditOpen = $false; Pump
"--- 1b. Düzenle: Süreç (isteğe bağlı)"
$vm.OpenEdit($jig) | Out-Null; Pump
Check 'süreç: talep tarihi ve kişisi ilk olaydan' ($vm.EditRequestDateText -eq '02.09.2026' -and $vm.EditRequester -eq 'katan') "$($vm.EditRequestDateText) $($vm.EditRequester)"
$vm.EditRequestDateText = '31.13.2026'
Check 'geçersiz tarih: kaydedilmez' (-not $vm.ConfirmEdit() -and $vm.EditError -match 'okunamadı')
$vm.EditRequestDateText = '01.09.2026'; $vm.EditRequester = 'M. Yılmaz'; $vm.EditMaker = 'Atölye A'; $vm.EditDocsReady = $true
Check 'süreç kaydedildi' ($vm.ConfirmEdit()) $vm.EditError
$jr = [IO.File]::ReadAllText((Join-Path $jig '_TAKIP\kayit.json'))
Check 'ilk olay: yeni tarih ve kişi; yapan, doküman kayıtta' ($jr -match '"date":\s*"2026-09-01"' -and $jr -match '"person":\s*"M. Yılmaz"' -and $jr -match '"Key":\s*"yapan",\s*"Value":\s*"Atölye A"' -and $jr -match '"Key":\s*"dokuman_hazir",\s*"Value":\s*"Evet"')
$vm.ShowInventory($jig); Wait
$pf = @{}; foreach ($x in $vm.Inventory.ProcessFacts) { $pf[$x.Label] = $x.Value }
Check 'kayıt sayfası: Yapan, Doküman Hazır, Talep' ($pf['Yapan'] -eq 'Atölye A' -and $pf['Doküman'] -eq 'Hazır' -and $pf['Talep'] -match '^01\.09\.2026') (($pf.Keys | % { $_ + '=' + $pf[$_] }) -join ', ')
Check 'yeniden açınca kutu yine dolu (aynı kayıt)' ($vm.OpenEdit($jig) -and $combo.SelectedItem -eq 'Mekanik') "[$($combo.SelectedItem)]"
$vm.IsEditOpen = $false; Pump

"--- 2. Yeni jig: alanlar da girilir"
$types = [Pusula.Core.Types.TypeStore]::LoadOrCreate($Data)
$jigType = $types.Find('jig')
$vm.OpenNewInventory($jigType); Pump
Check 'yeni kayıt: alanlar gelir' ($vm.IsInventoryDialogOpen -and $vm.HasNewInventoryFields -and $vm.NewInventoryFieldsHeader -eq ('Özellikler · ' + $vm.NewInventorySubType)) $vm.NewInventoryFieldsHeader
$newCombo = FindLogical $win { param($x) $x -is [System.Windows.Controls.ComboBox] -and [object]::ReferenceEquals($x.ItemsSource, $vm.InventorySubTypes) }
Check 'yeni kayıt: alt tür kutusu dolu' ($newCombo.SelectedItem -eq $vm.NewInventorySubType -and $vm.NewInventorySubType.Length -gt 0)
Check 'talep tarihi bugün, talep eden bu kullanıcı (varsayılan)' ($vm.NewInventoryRequestDate -eq [DateTime]::Today -and $vm.NewInventoryRequester -eq [Environment]::UserName)
$vm.NewInventoryNumber = 'JIG-1000-0099'; $vm.NewInventoryName = 'Deneme aparatı'
$vm.NewInventoryRequestDateText = '31.02.2026'
Check 'geçersiz tarih: kayıt açılmaz, uyarı' (-not $vm.ConfirmNewInventory() -and $vm.InventoryError -match 'okunamadı') $vm.InventoryError
$vm.NewInventoryRequestDateText = [DateTime]::Today.AddDays(3).ToString('dd.MM.yyyy')
Check 'ileri tarih: kayıt açılmaz' (-not $vm.ConfirmNewInventory() -and $vm.InventoryError -match 'ileri') $vm.InventoryError
$vm.NewInventoryRequestDateText = '3.11.26'
Check 'kısa yazım okunur (3.11.26)' ($vm.NewInventoryRequestDate -eq [DateTime]'2026-11-03')
$vm.NewInventoryRequestDateText = '03.11.2026'; $vm.NewInventoryRequester = 'A. Test'
$first = $vm.NewInventoryFields[0]; $first.Value = '7'
$sub2 = @($vm.InventorySubTypes | Where-Object { $_ -ne $vm.NewInventorySubType })[0]
$keep = $vm.NewInventorySubType
$newCombo.SelectedItem = $sub2; Pump
Check 'alt tür değişince alanlar değişir' ($vm.NewInventorySubType -eq $sub2 -and $vm.NewInventoryFieldsHeader -eq "Özellikler · $sub2")
$newCombo.SelectedItem = $keep; Pump
Check 'geri dönünce değer korunur' ($vm.NewInventoryFields[0].Value -eq '7')
Snap $win "$Out\dialog-new-jig.png"
$fkey = $vm.NewInventoryFields[0].Key
Check 'oluşturuldu' ($vm.ConfirmNewInventory()); Wait
$folder = @(Get-ChildItem (Join-Path $Data 'ENVANTER\Jigler') -Directory | Where-Object { $_.Name -like 'JIG-1000-0099*' })[0].FullName
$rec = [IO.File]::ReadAllText((Join-Path $folder '_TAKIP\kayit.json'))
Check 'talep olayı girilen tarih ve kişiyle' ($rec -match '"date":\s*"2026-11-03"' -and $rec -match '"person":\s*"A. Test"')
Check 'alan değeri kayda yazıldı' ($rec -match ('"Key":\s*"' + $fkey + '",\s*"Value":\s*"7"')) $rec.Substring(0, [Math]::Min(300, $rec.Length))
$md = @(Get-ChildItem $folder -Filter '*.md' | Where-Object { $_.Name -like 'JIG-1000-0099*' })
if ($md.Count -gt 0) { Check 'alan değeri notun YAML başlığında' ([IO.File]::ReadAllText($md[0].FullName) -match ($fkey + ':\s*"?7')) }

"--- 3. Kayıt sayfası: Süreç kartı başlıkta yazanları tekrarlamaz"
$vm.ShowInventory($jig); Wait
$labels = @($vm.Inventory.ProcessFacts | ForEach-Object { $_.Label }) -join ','
Check 'süreç: düzey, talep, yapan, teslim, not, doküman' ($labels -eq 'Düzey,Talep,Yapan,Teslim,Not,Doküman') $labels
Check 'bilgiler listesi (eski) yerinde: numara dahil' (@($vm.Inventory.Facts | ForEach-Object { $_.Label }) -contains 'Numara')

"--- 3b. Etiket kutusu: rozetler"
$hostW = New-Object System.Windows.Window; $hostW.Left = -20000; $hostW.Top = -20000; $hostW.ShowActivated = $false; $hostW.Width = 500; $hostW.Height = 120
$tb = New-Object Pusula.Wpf.TagBox; $hostW.Content = $tb; $hostW.Show(); Pump
$tb.Tags = '#SATIMO'; Pump
Check 'var olan etiket rozet' ($tb.ChipCount -eq 1)
$tb.Input.Text = '#faz_kararli '; Pump
Check 'yazılan etiket boşlukla rozete döner' ($tb.ChipCount -eq 2 -and $tb.Tags -eq '#SATIMO #faz_kararli' -and $tb.Input.Text -eq '') $tb.Tags
$tb.Input.Text = '#SATIMO '; Pump
Check 'aynı etiket iki kez eklenmez' ($tb.ChipCount -eq 2)
$tb.Input.Text = 'RS422 RS232 '; Pump
Check '# yazmadan da, birden çok' ($tb.Tags -eq '#SATIMO #faz_kararli #RS422 #RS232') $tb.Tags
$hostW.Close()

"--- 4. Etiketler"
$vm.OpenEdit($jig) | Out-Null; Pump
$jt = @($vm.TagSuggestions() | ForEach-Object { $_.Key })
Check 'jigde: her yerde geçenler var, kablo etiketi yok' ($jt -contains '#SATIMO' -and -not ($jt -contains '#faz_kararli')) ($jt -join ',')
$vm.IsEditOpen = $false; Pump
$cableType = $types.Find('cable')
$vm.OpenNewInventory($cableType); Pump; $vm.NewInventorySubType = 'RF'; Pump
Check 'tanım ipucu türe göre (kablo: TST KBL XXXX)' ($vm.NewInventoryNamePlaceholder -eq 'TST KBL XXXX')
$people = @($vm.PersonSuggestions() | ForEach-Object { $_.Key })
Check 'talep eden: kişi önerisi, yalnız ad yazılır' ($people -contains [Environment]::UserName -and @($vm.PersonSuggestions() | Where-Object { $_.Insert -ne $_.Key }).Count -eq 0) ($people -join ',')
$ct = @($vm.TagSuggestions() | ForEach-Object { $_.Key })
Check 'yeni RF kablo: #faz_kararli önerilir, RS422 önerilmez' ($ct -contains '#faz_kararli' -and -not ($ct -contains '#RS422')) ($ct -join ',')
$vm.NewInventorySubType = 'Güç'; Pump
Check 'güç kablosu: #faz_kararli önerilmez' (-not (@($vm.TagSuggestions() | ForEach-Object { $_.Key }) -contains '#faz_kararli'))
$vm.NewInventorySubType = 'RF'; Pump
$vm.NewInventoryNumber = 'JT-0001-0099'; $vm.NewInventoryName = 'Etiketli kablo'; $vm.NewInventoryTags = '#faz_kararli #SATIMO'
Check 'oluşturuldu (etiketli)' ($vm.ConfirmNewInventory()); Wait
$cf = @(Get-ChildItem (Join-Path $Data 'ENVANTER') -Recurse -Directory | Where-Object { $_.Name -like 'JT-0001-0099*' })[0].FullName
$ck = [IO.File]::ReadAllText((Join-Path $cf '_TAKIP\kayit.json'))
Check 'etiketler kayıtta' ($ck -match '"tags"\s*:\s*\[\s*"faz_kararli",\s*"SATIMO"') $ck.Substring(0, [Math]::Min(400, $ck.Length))
$cmd = @(Get-ChildItem $cf -Filter 'JT-0001-0099*.md')
if ($cmd.Count -gt 0) { Check 'notun YAML''ında Obsidian tags' ([IO.File]::ReadAllText($cmd[0].FullName) -match '(?m)^tags: \[faz_kararli, SATIMO\]') }
$win.Width = 1500; Wait; Snap $win "$Out\tags-record.png"
Check 'kayıt sayfasında çipler' ($vm.Inventory.HasTags -and ($vm.Inventory.Tags -join ' ') -eq '#faz_kararli #SATIMO') ($vm.Inventory.Tags -join ' ')
$vm.OpenEdit($cf) | Out-Null; Pump
Check 'düzenle: etiketler kutuda' ($vm.EditTags -eq '#faz_kararli #SATIMO') $vm.EditTags
$vm.EditTags = '#SATIMO'; Check 'kaydedildi' ($vm.ConfirmEdit()); Wait
Check 'etiket kaldırılınca kayıttan da kalkar' (-not ([IO.File]::ReadAllText((Join-Path $cf '_TAKIP\kayit.json')) -match 'faz_kararli'))
$win.Close()
"SONUÇ OK=$ok HATA=$fail"
