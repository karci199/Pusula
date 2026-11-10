param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
. (Join-Path $PSScriptRoot '..\_Ortak.ps1')
# Kişiler: tür dosyasında kişi listesi (ad soyad, rol, ekip), roller; Ayarlar'da kullanıcı (ben) varsayılan kişi olur.
"--- 1. Tür dosyası"
$set = [Pusula.Core.Types.TypeSet]::CreateDefault()
Check 'varsayılan roller Mühendis, Teknisyen' (($set.PersonRoles -join ',') -eq 'Mühendis,Teknisyen') ($set.PersonRoles -join ',')
$v9 = [Pusula.Core.Types.TypeSet]::CreateDefault(); $v9.PersonRoles.Clear(); $v9.SchemaVersion = 9; $v9.Normalize()
Check 'sürüm 9 → 10: roller eklendi' ($v9.SchemaVersion -eq 10 -and $v9.PersonRoles.Count -eq 2)
$p = New-Object Pusula.Core.Types.PersonDefinition; $p.Name = 'Ayşe Demir'; $p.Role = 'Mühendis'; $p.Team = 'Anten'
$set.People.Add($p)
Check 'özet: rol · ekip' ($p.Summary -eq 'Mühendis · Anten') $p.Summary
Check 'ad ile bulunur (harf duyarsız)' ($set.FindPerson(' ayşe demir ') -eq $p)
$saveError = [Pusula.Core.Types.TypeStore]::Save($Data, $set)
$back = [Pusula.Core.Types.TypeStore]::LoadOrCreate($Data)
Check 'kaydedip okununca kişi ve rol korunur' ($saveError.Length -eq 0 -and $back.People.Count -eq 1 -and $back.People[0].Team -eq 'Anten' -and $back.PersonRoles.Count -eq 2) $saveError

"--- 2. Uygulama: kullanıcı ve öneriler"
[System.Threading.SynchronizationContext]::SetSynchronizationContext((New-Object System.Windows.Threading.DispatcherSynchronizationContext))
$app = New-Object Pusula.Wpf.App; $app.InitializeComponent(); $app.ShutdownMode = 'OnExplicitShutdown'
$win = New-Object Pusula.Wpf.MainWindow
$win.WindowStartupLocation = 'Manual'; $win.Left = -20000; $win.Top = -20000; $win.ShowActivated = $false
$win.Show(); Pump
$vm = $win.DataContext; $vm.RootPath = $Data; Wait
Check 'kullanıcı boşken Windows adı' ([Pusula.Wpf.ViewModels.MainViewModel]::CurrentUser -eq $env:USERNAME)
$vm.UserName = '  Ayşe Demir '
Check 'kullanıcı ayarlandı' ([Pusula.Wpf.ViewModels.MainViewModel]::CurrentUser -eq 'Ayşe Demir' -and $vm.UserSummary -eq 'Mühendis · Anten') $vm.UserSummary
Check 'ayarlara yazıldı' ([IO.File]::ReadAllText($env:PUSULA_AYARLAR) -match '"user":\s*"Ayşe Demir"')
$sug = @($vm.PersonSuggestions())
Check 'öneride kişi ve rolü' (@($sug | ? { $_.Key -eq 'Ayşe Demir' -and $_.Title -eq 'Mühendis · Anten' }).Count -eq 1) (($sug | % { $_.Key + '=' + $_.Title }) -join ' | ')
$types = [Pusula.Core.Types.TypeStore]::LoadOrCreate($Data)
$vm.OpenNewInventory($types.Find('jig')); Wait
Check 'yeni kayıtta talep eden kullanıcı' ($vm.NewInventoryRequester -eq 'Ayşe Demir') $vm.NewInventoryRequester
$vm.IsInventoryDialogOpen = $false; Wait
$vm.UserName = ''
Check 'temizlenince yine Windows adı' ([Pusula.Wpf.ViewModels.MainViewModel]::CurrentUser -eq $env:USERNAME)

"--- 3. Türler penceresi"
$ed = New-Object Pusula.Wpf.ViewModels.TypeEditor.TypeEditorViewModel($Data, $types)
Check 'kişiler yüklendi' ($ed.People.Count -eq 1 -and $ed.RolesText -eq 'Mühendis, Teknisyen')
$r = New-Object Pusula.Wpf.ViewModels.TypeEditor.PersonRow; $r.Name = ' Mehmet   Kaya '; $r.Role = 'Teknisyen'; $r.Team = 'Mekanik'; $ed.People.Add($r)
$dup = New-Object Pusula.Wpf.ViewModels.TypeEditor.PersonRow; $dup.Name = 'ayşe demir'; $ed.People.Add($dup)
$ed.RolesText = 'Mühendis, Teknisyen, Stajyer'
Check 'rol listesi' ($ed.Roles.Count -eq 3)
Check 'kaydedildi' ($ed.Save()) $ed.Error
$after = [Pusula.Core.Types.TypeStore]::LoadOrCreate($Data)
Check 'ad boşlukları düzeltildi, tekrar atıldı' ((@($after.People | % Name) -join ',') -eq 'Ayşe Demir,Mehmet Kaya') (@($after.People | % Name) -join ',')
Check 'roller ve etiketler korundu' ($after.PersonRoles.Count -eq 3 -and $after.TagList.Count -eq $types.TagList.Count)
$win.Close()