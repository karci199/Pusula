param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
. (Join-Path $PSScriptRoot '..\_Ortak.ps1')
# Kişiler: Kişi türü (klasörsüz; rol = alt tür, Ekip = liste alanı), kişi listesi tür dosyasında; Ayarlar'da kullanıcı
# (ben) de bir kişidir ve talep eden / olay kişisinde varsayılan olur.
"--- 1. Kişi türü"
$set = [Pusula.Core.Types.TypeSet]::CreateDefault()
$pt = $set.Person
Check 'Kişi türü: roller alt tür, klasörsüz' ($pt.Key -eq 'person' -and ($pt.SubTypes -join ',') -eq 'Mühendis,Teknisyen' -and -not $pt.IsInventory -and $pt.GeneralFolder -eq '') ($pt.SubTypes -join ',')
Check 'Ekip liste alanı' (@($pt.Fields | ? { $_.Key -eq 'ekip' -and $_.Kind -eq 'choice' -and $_.Choices.Count -gt 0 }).Count -eq 1)
Check 'envanter türleri arasında değil' (-not (@($set.InventoryTypes | % Key) -contains 'person'))
$v9 = [Pusula.Core.Types.TypeSet]::CreateDefault(); $v9.Types.RemoveAll([Predicate[Pusula.Core.Types.TypeDefinition]]{ param($x) $x.Key -eq 'person' }) | Out-Null; $v9.SchemaVersion = 9; $v9.Normalize()
Check 'sürüm 9 → 11: Kişi ve Test gereci türleri eklendi' ($v9.SchemaVersion -eq 11 -and $v9.Find('person') -ne $null -and $v9.Find('equipment') -ne $null)
$p = New-Object Pusula.Core.Types.PersonDefinition; $p.Name = 'Ayşe Demir'; $p.Role = 'Mühendis'; $p.Fields['ekip'] = 'Anten'
$set.People.Add($p)
Check 'özet: rol · ekip' ($set.PersonSummary($p) -eq 'Mühendis · Anten' -and $p.Team -eq 'Anten') $set.PersonSummary($p)
Check 'ad ile bulunur (harf duyarsız)' ($set.FindPerson(' ayşe demir ') -eq $p)
$saveError = [Pusula.Core.Types.TypeStore]::Save($Data, $set)
$back = [Pusula.Core.Types.TypeStore]::LoadOrCreate($Data)
Check 'kaydedip okununca kişi ve ekip korunur' ($saveError.Length -eq 0 -and $back.People.Count -eq 1 -and $back.People[0].Team -eq 'Anten' -and $back.Person.SubTypes.Count -eq 2) $saveError

"--- 2. Uygulama: kullanıcı ve öneriler"
[System.Threading.SynchronizationContext]::SetSynchronizationContext((New-Object System.Windows.Threading.DispatcherSynchronizationContext))
$app = New-Object Pusula.Wpf.App; $app.InitializeComponent(); $app.ShutdownMode = 'OnExplicitShutdown'
$win = New-Object Pusula.Wpf.MainWindow
$win.WindowStartupLocation = 'Manual'; $win.Left = -20000; $win.Top = -20000; $win.ShowActivated = $false
$win.Show(); Pump
$vm = $win.DataContext; $vm.RootPath = $Data; Wait
Check 'kullanıcı boşken Windows adı' ([Pusula.Wpf.ViewModels.MainViewModel]::CurrentUser -eq $env:USERNAME)
Check 'Ayarlar seçenekleri kişi türünden' (($vm.PersonRoles -join ',') -eq 'Mühendis,Teknisyen' -and $vm.PersonTeams -contains 'Anten')
$vm.UserName = '  Ayşe Demir '
Check 'kullanıcı ayarlandı, listedeki rolü' ([Pusula.Wpf.ViewModels.MainViewModel]::CurrentUser -eq 'Ayşe Demir' -and $vm.UserRole -eq 'Mühendis' -and $vm.UserTeam -eq 'Anten') $vm.UserRole
Check 'ayarlara yazıldı' ([IO.File]::ReadAllText($env:PUSULA_AYARLAR) -match '"user":\s*"Ayşe Demir"')
$vm.UserName = 'Zeynep   Ak'
$tf = [Pusula.Core.Types.TypeStore]::LoadOrCreate($Data)
Check 'listede olmayan kullanıcı kişi olarak eklendi' ($tf.FindPerson('Zeynep Ak') -ne $null -and $tf.FindPerson('Ayşe Demir') -ne $null) (@($tf.People | % Name) -join ',')
$vm.UserRole = 'Teknisyen'; $vm.UserTeam = 'Ölçüm'
$tf = [Pusula.Core.Types.TypeStore]::LoadOrCreate($Data)
Check 'rol ve ekip tür dosyasına yazıldı' ($tf.FindPerson('Zeynep Ak').Role -eq 'Teknisyen' -and $tf.FindPerson('Zeynep Ak').Team -eq 'Ölçüm')
$vm.UserName = 'Ayşe Demir'
Check 'eski kişi silinmedi' ($tf.FindPerson('Ayşe Demir').Role -eq 'Mühendis')
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
$pi = $ed.PersonItem
Check 'Kişi türü listede, kişi sayfası' ($ed.Types -contains $pi -and $pi.IsPerson -and $ed.People.Count -eq 2 -and ($ed.Roles -join ',') -eq 'Mühendis,Teknisyen')
$st = New-Object Pusula.Wpf.ViewModels.TypeEditor.TextRow; $st.Text = 'Stajyer'; $pi.SubTypes.Add($st)
$tel = New-Object Pusula.Wpf.ViewModels.TypeEditor.FieldRow; $tel.Name = 'Dahili tel'; $tel.Kind = 'text'; $pi.Fields.Add($tel)
$cols = $ed.PersonColumns()
Check 'yeni rol ve alan sütunu hemen görünür' ($ed.Roles -contains 'Stajyer' -and (@($cols | % Name) -join ',') -eq 'Ekip,Dahili tel') (@($cols | % Name) -join ',')
$r = New-Object Pusula.Wpf.ViewModels.TypeEditor.PersonRow; $r.Name = ' Mehmet   Kaya '; $r.Role = 'Stajyer'; $r.Values['ekip'] = 'Mekanik'; $r.Values['dahili_tel'] = ' 1234 '; $ed.People.Add($r)
$dup = New-Object Pusula.Wpf.ViewModels.TypeEditor.PersonRow; $dup.Name = 'ayşe demir'; $ed.People.Add($dup)
Check 'kaydedildi' ($ed.Save()) $ed.Error
$after = [Pusula.Core.Types.TypeStore]::LoadOrCreate($Data)
Check 'ad boşlukları düzeltildi, tekrar atıldı' ((@($after.People | % Name) -join ',') -eq 'Ayşe Demir,Zeynep Ak,Mehmet Kaya') (@($after.People | % Name) -join ',')
Check 'rol, alanlar ve tür korundu' ($after.Person.SubTypes -contains 'Stajyer' -and $after.FindPerson('Mehmet Kaya').Fields['dahili_tel'] -eq '1234' -and @($after.Types | ? Key -eq 'person').Count -eq 1 -and $after.TagList.Count -eq $types.TagList.Count -and $after.Find('stock') -ne $null)
$win.Close()