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
Check 'yeniden açınca kutu yine dolu (aynı kayıt)' ($vm.OpenEdit($jig) -and $combo.SelectedItem -eq 'Mekanik') "[$($combo.SelectedItem)]"
$vm.IsEditOpen = $false; Pump

"--- 2. Yeni jig: alanlar da girilir"
$types = [Pusula.Core.Types.TypeStore]::LoadOrCreate($Data)
$jigType = $types.Find('jig')
$vm.OpenNewInventory($jigType); Pump
Check 'yeni kayıt: alanlar gelir' ($vm.IsInventoryDialogOpen -and $vm.HasNewInventoryFields -and $vm.NewInventoryFieldsHeader -eq ('Özellikler · ' + $vm.NewInventorySubType)) $vm.NewInventoryFieldsHeader
$newCombo = FindLogical $win { param($x) $x -is [System.Windows.Controls.ComboBox] -and [object]::ReferenceEquals($x.ItemsSource, $vm.InventorySubTypes) }
Check 'yeni kayıt: alt tür kutusu dolu' ($newCombo.SelectedItem -eq $vm.NewInventorySubType -and $vm.NewInventorySubType.Length -gt 0)
$vm.NewInventoryNumber = 'JIG-1000-0099'; $vm.NewInventoryName = 'Deneme aparatı'
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
Check 'alan değeri kayda yazıldı' ($rec -match ('"Key":\s*"' + $fkey + '",\s*"Value":\s*"7"')) $rec.Substring(0, [Math]::Min(300, $rec.Length))
$md = @(Get-ChildItem $folder -Filter '*.md' | Where-Object { $_.Name -like 'JIG-1000-0099*' })
if ($md.Count -gt 0) { Check 'alan değeri notun YAML başlığında' ([IO.File]::ReadAllText($md[0].FullName) -match ($fkey + ':\s*"?7')) }
$win.Close()
"SONUÇ OK=$ok HATA=$fail"
