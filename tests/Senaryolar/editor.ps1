param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
. (Join-Path $PSScriptRoot '..\_Ortak.ps1')
"--- 1. Eski dosya cevirme (deneme verisi)"
$old = [Pusula.Core.Templates.TemplateStore]::LoadOrCreate($Data)
Check 'surum 3' ($old.SchemaVersion -eq 3)
$pasif = $old.Templates | Where-Object Name -eq 'Pasif anten'
Check 'Pasif anten temel modulleri iceriyor' ($pasif.Modules.Count -ge 5) ("sayi=" + $pasif.Modules.Count)
$names = ($old.Modules | ForEach-Object Name) -join ', '
Check 'Turkce adlar duzeldi' ($names -match 'Diğer' -and $names -match 'Arşiv' -and $names -notmatch 'ARSIV') $names
Check 'core kalmadi' (-not ($old.Modules | Where-Object Core))

"--- 2. Duzenleyici mantigi"
$t = Join-Path $env:TEMP ('pusula-test-' + [guid]::NewGuid().ToString('N').Substring(0,6))
New-Item -ItemType Directory $t | Out-Null
$set = [Pusula.Core.Templates.TemplateStore]::LoadOrCreate($t)
$vm = New-Object Pusula.Wpf.ViewModels.TemplateEditor.TemplateEditorViewModel($t, $set)
Check 'iki sablon' ($vm.Templates.Count -eq 2)
Check 'ilk sablon secili' ($vm.SelectedTemplate.Name -eq 'Pasif anten')
Check 'Pasif anten 5 modul' ($vm.SelectedTemplate.ModuleCount -eq 5)
$avail = $vm.AvailableModules()
Check 'eklenebilir 4 modul' ($avail.Count -eq 4) ("sayi=" + $avail.Count)
$vm.AddToSelected($avail[0])
Check 'modul eklendi, sirali' ($vm.SelectedTemplate.Modules[2].Folder -eq '02_Test_Planlari') ($vm.SelectedTemplate.Modules | ForEach-Object Folder)
Check 'kullanim metni' ($vm.UsageText -match '2 şablonda') $vm.UsageText
$vm.RemoveModuleCommand.Execute($avail[0])
Check 'modul cikarildi' ($vm.SelectedTemplate.ModuleCount -eq 5)

$vm.AddTemplateCommand.Execute($null)
Check 'yeni sablon temel modullerle' ($vm.SelectedTemplate.ModuleCount -eq 5 -and $vm.SelectedTemplate.Name -eq 'Yeni şablon')
$vm.SelectedTemplate.Name = 'Tasarımcı'

$dlg = New-Object Pusula.Wpf.ViewModels.TemplateEditor.ModuleDialogViewModel($vm, $null)
Check 'yeni modul numarasi 08' ($dlg.Working.Number -eq '08') $dlg.Working.Number
$dlg.Working.Name = 'Ölçüm düzeneği'
Check 'klasor adi addan' ($dlg.Working.FolderName -eq 'Olcum_duzenegi') $dlg.Working.FolderName
Check 'onizleme' ($dlg.FolderPreview -match '08_Olcum_duzenegi') $dlg.FolderPreview
$dlg.Working.FolderName = 'Analiz'
$dlg.Working.Name = 'Analiz'
Check 'elle yazilan klasor adi korunur' ($dlg.Working.FolderName -eq 'Analiz')
$dlg.Working.AddSubFolderCommand.Execute($null); $n = $dlg.Working.SubFolders[0]; $n.Name = 'Modeller'; $n.AddChildCommand.Execute($null); $n.Children[0].Name = 'CST'
Check 'yeni modul olustu' ($dlg.Confirm())
Check 'sablona eklendi' (($vm.SelectedTemplate.Modules | ForEach-Object Folder) -contains '08_Analiz')
Check 'alt klasor metni' (($vm.SelectedTemplate.Modules | Where-Object Folder -eq '08_Analiz').SubFolderText -eq '2 alt klasör')

$bad = New-Object Pusula.Wpf.ViewModels.TemplateEditor.ModuleDialogViewModel($vm, $null)
$bad.Working.Name = 'Kopya'; $bad.Working.Number = '01'; $bad.Working.FolderName = 'Dokumanlar'
Check 'ayni klasor reddedilir' (-not $bad.Confirm() -and $bad.Error -match '01_Dokumanlar') $bad.Error
$bad.Working.Number = '09'; $bad.Working.FolderName = 'a:b'
Check 'gecersiz ad reddedilir' (-not $bad.Confirm()) $bad.Error

$test = $vm.Library | Where-Object Folder -eq '03_Test_Sonuclari'
$edit = New-Object Pusula.Wpf.ViewModels.TemplateEditor.ModuleDialogViewModel($vm, $test)
$edit.Working.SubFolders.Add((New-Object Pusula.Wpf.ViewModels.TemplateEditor.FolderNodeViewModel('NearField', $edit.Working.SubFolders)))
Check 'duzenleme uygulandi' ($edit.Confirm() -and ($test.ToDefinition().SubFolders -contains 'NearField'))

Check 'kaydet' ($vm.Save()) $vm.Error
$back = [Pusula.Core.Templates.TemplateStore]::LoadOrCreate($t)
$tas = $back.Templates | Where-Object Name -eq 'Tasarımcı'
Check 'Tasarimci kaydedildi' ($tas.Modules.Count -eq 6) ("sayi=" + $tas.Modules.Count)
Check 'dosyada core yok' (-not ((Get-Content ([Pusula.Core.Templates.TemplateStore]::FilePath($t)) -Raw) -match '"core"'))

$vm.SelectedTemplate.Name = 'Pasif anten'
Check 'tekrarli ad reddedilir' (-not $vm.Save()) $vm.Error

"--- 3. Stok olusturma"
$p = [Pusula.Core.Tracking.ProjectService]::CreateProject($t, '7000', 'MILKON', $back.ProjectFolders)
$s = [Pusula.Core.Tracking.ProjectService]::CreateStock($p.Folder, 'AB-7000-0000', '', 'Tasarımcı', $back.StockFoldersFor([string[]]$tas.Modules))
$dirs = Get-ChildItem -Recurse -Directory $s.Folder | ForEach-Object { $_.FullName.Substring($s.Folder.Length + 1) }
Check 'Analiz\Modeller\CST' ($dirs -contains '08_Analiz\Modeller\CST')
Check 'Test sonuclari\NearField' ($dirs -contains '03_Test_Sonuclari\NearField')
Check 'Kalifikasyon yok' (-not ($dirs -contains '04_Kalifikasyon'))

"--- 4. Pencereler yukleniyor mu (XAML)"
$app = New-Object Pusula.Wpf.App
$app.InitializeComponent()
$w = New-Object Pusula.Wpf.TemplateEditorWindow($vm)
Check 'Sablonlar penceresi' ($w -ne $null)
$m = New-Object Pusula.Wpf.ModuleWindow((New-Object Pusula.Wpf.ViewModels.TemplateEditor.ModuleDialogViewModel($vm, $test)))
Check 'Modul penceresi' ($m -ne $null)
$main = New-Object Pusula.Wpf.MainWindow
Check 'Ana pencere' ($main -ne $null)

Remove-Item -Recurse -Force $t
""
"SONUC: $ok basarili, $fail hata"
