param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
. (Join-Path $PSScriptRoot '..\_Ortak.ps1')
# Kişiler ve bahsetme (25.11.2026): Ayarlar → Kişiler… Türler penceresini kişi listesiyle açar; eklenen kişi @ önerisinde
# görünür (ayrılan görünmez); seçili stokun Diger\ klasöründeki .msg dosyaları da önerilir, [[Diger/….msg]] tıklanınca açılır.
# Bahsetme yalnız metin ekler (iş atamaz, kimseye bildirim gitmez).
[System.Threading.SynchronizationContext]::SetSynchronizationContext((New-Object System.Windows.Threading.DispatcherSynchronizationContext))
$app = New-Object Pusula.Wpf.App; $app.InitializeComponent(); $app.ShutdownMode = 'OnExplicitShutdown'
$win = New-Object Pusula.Wpf.MainWindow
$win.WindowStartupLocation = 'Manual'; $win.Left = -20000; $win.Top = -20000; $win.ShowActivated = $false; $win.Width = 1400; $win.Height = 900
$win.Show(); Pump
$vm = $win.DataContext; $vm.RootPath = $Data; Wait

"--- 1. Kişi ekleme (Türler → Kişi)"
$ed = New-Object Pusula.Wpf.ViewModels.TypeEditor.TypeEditorViewModel($Data, $vm.Types)
$ed.Selected = $ed.PersonItem
Check 'Kişiler… ile kişi türü seçili açılır' ([object]::ReferenceEquals($ed.Selected, $ed.PersonItem))
$row = New-Object Pusula.Wpf.ViewModels.TypeEditor.PersonRow; $row.Name = 'Okan Özdemir'; $ed.People.Add($row)
$gone = New-Object Pusula.Wpf.ViewModels.TypeEditor.PersonRow; $gone.Name = 'Eski Çalışan'; $gone.HasLeft = $true; $ed.People.Add($gone)
Check 'kaydedildi' ($ed.Save())
$vm.ReloadTypes(); Wait
$people = @($vm.NoteSuggestions() | Where-Object { $_.Kind -eq 'Kişi' })
Check '@ önerisinde yeni kişi: Okan Özdemir, eklenecek metin @Okan Özdemir' (@($people | Where-Object { $_.Key -eq 'Okan Özdemir' -and $_.Insert -eq '@Okan Özdemir' }).Count -eq 1) (($people | ForEach-Object Key) -join ',')
Check 'ayrılan kişi önerilmez' (@($people | Where-Object Key -eq 'Eski Çalışan').Count -eq 0)

"--- 2. Mail dosyaları"
$stock = Join-Path $Data '7000_MILKON\AB-7000-0003'
$mails = Join-Path $stock 'Diger'; New-Item -ItemType Directory -Force $mails | Out-Null
[IO.File]::WriteAllText((Join-Path $mails 'Toplanti_25-11.msg'), 'x')
[IO.File]::WriteAllText((Join-Path $mails 'not.txt'), 'x')
$vm.SelectNodeByPath($stock); Wait
$sug = @($vm.NoteSuggestions() | Where-Object { $_.Kind -eq 'Mail' })
Check 'stokta: .msg önerilir, başka dosya önerilmez' ($sug.Count -eq 1 -and $sug[0].Key -eq 'Toplanti_25-11.msg' -and $sug[0].Insert -eq '[[Diger/Toplanti_25-11.msg]]') (($sug | ForEach-Object Insert) -join ',')
Check 'stok ve kişi önerileri de var' (@($vm.NoteSuggestions() | Where-Object { $_.Key -eq 'AB-7000-0002' }).Count -eq 1 -and @($vm.NoteSuggestions() | Where-Object { $_.Key -eq 'Okan Özdemir' }).Count -eq 1)
$script:opened = ''
$vm.OpenMailFile = [Action[string]]{ param($p) $script:opened = $p }
Check '[[Diger/….msg]] tıklanınca mail açılır' ($vm.NavigateToKey('Diger/Toplanti_25-11.msg') -and $script:opened -like '*Toplanti_25-11.msg') $script:opened
$script:opened = ''
Check 'olmayan mail: açılmaz, durumda yazı' (-not $vm.NavigateToKey('Diger/yok.msg') -and $script:opened -eq '' -and $vm.Status -like '*yok.msg*') $vm.Status
$vm.SelectNodeByPath((Join-Path $Data '7000_MILKON\AB-7000-0002')); Wait
Check 'başka stokta o mail önerilmez' (@($vm.NoteSuggestions() | Where-Object { $_.Kind -eq 'Mail' }).Count -eq 0)
Check '[[AB-7000-0003]] stoka gider (bahsetme yalnız bağlantı)' ($vm.NavigateToKey('AB-7000-0003') -and $vm.SelectedNode.Title -eq 'AB-7000-0003')
$win.Close()