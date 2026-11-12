param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
. (Join-Path $PSScriptRoot '..\_Ortak.ps1')
# Başlangıç sayfası (hiçbir şey seçili değilken): son açılanlar, bütün projelerdeki geciken işler ve bana ait işler.
$TL = [Pusula.Core.Notes.TaskList]
[System.Threading.SynchronizationContext]::SetSynchronizationContext((New-Object System.Windows.Threading.DispatcherSynchronizationContext))
$app = New-Object Pusula.Wpf.App; $app.InitializeComponent(); $app.ShutdownMode = 'OnExplicitShutdown'; [Pusula.Wpf.Themes]::Apply([Pusula.Wpf.Themes]::Find('FluentDark'))
$win = New-Object Pusula.Wpf.MainWindow
$win.WindowStartupLocation = 'Manual'; $win.Left = -20000; $win.Top = -20000; $win.ShowActivated = $false; $win.Width = 1240; $win.Height = 820
$win.Show(); Pump
$vm = $win.DataContext; $vm.RootPath = $Data; Wait

$user = [Pusula.Wpf.ViewModels.MainViewModel]::CurrentUser
$stock = Join-Path $Data '7000_MILKON\AB-7000-0002'
$note = Get-ChildItem $stock -Filter '*.md' | Select -First 1
if (-not $note) { $notePath = Join-Path $stock 'AB-7000-0002.md' } else { $notePath = $note.FullName }
$TL::Add($notePath, 'Geciken deneme işi', 'Başka Biri', [datetime]::Today.AddDays(-2)) | Out-Null
$TL::Add($notePath, 'Benim deneme işim', $user, [datetime]::Today.AddDays(3)) | Out-Null
$TL::Add($notePath, 'Başkasının işi', 'Başka Biri', [datetime]::Today.AddDays(3)) | Out-Null

$vm.Refresh(); Wait
Check 'seçim yok: başlangıç sayfası' (-not $vm.HasSelection -and $win.FindName('HomePage').Visibility -eq 'Visible')
Check 'geciken iş listede, tarih kırmızı' (@($vm.HomeLateTasks | ? { $_.Text -eq 'Geciken deneme işi' -and $_.IsLate }).Count -eq 1) (@($vm.HomeLateTasks | % Text) -join ' | ')
Check 'bana ait: yalnız kullanıcının işi' (@($vm.HomeMyTasks | ? { $_.Text -eq 'Benim deneme işim' }).Count -eq 1 -and @($vm.HomeMyTasks | ? { $_.PersonName -ne $user }).Count -eq 0) (@($vm.HomeMyTasks | % Text) -join ' | ')
Check 'iş kaynağı: proje · stok' (@($vm.HomeMyTasks | ? { $_.Meta -like '*7000_MILKON*' }).Count -ge 1) (@($vm.HomeMyTasks | % Meta) -join ' | ')

$vm.SelectNodeByPath($stock); Wait
Check 'stok Dikkat: geciken iş' (@($vm.Detail.Alerts | ? { $_.Detail -like 'Geciken deneme işi*' }).Count -eq 1 -and @($vm.Detail.Alerts | ? { $_.Detail -like 'Benim deneme işim*' }).Count -eq 0) (@($vm.Detail.Alerts | % { $_.Title + ': ' + $_.Detail }) -join ' | ')
$vm.SelectNodeByPath((Join-Path $Data '7000_MILKON')); Wait
$vm.Refresh(); Wait
$vm.SelectedNode = $null; $vm.LoadHome(); Wait
Check 'son açılanlar: en yeni başta' ($vm.RecentNodes.Count -ge 2 -and $vm.RecentNodes[0].Title -eq '7000_MILKON' -and $vm.RecentNodes[1].Title -eq 'AB-7000-0002') (@($vm.RecentNodes | % Title) -join ' | ')
function CountOf($list, $label) { (@($list | ? { $_.Label -eq $label }) | Select -First 1).Count }
Check 'genel sayılar: proje, stok, kablo, jig' ((CountOf $vm.HomeCounts 'Projeler') -ge 1 -and (CountOf $vm.HomeCounts 'Stoklar') -ge 4 -and (CountOf $vm.HomeCounts 'Kablolar') -ge 1 -and (CountOf $vm.HomeCounts 'Jigler') -ge 1) (@($vm.HomeCounts | % { $_.Label + ' ' + $_.Count }) -join ' | ')
Check 'benim sayılarım: açık işim en az 1' ($vm.HasMyCounts -and (CountOf $vm.MyCounts 'Açık işim') -ge 1) (@($vm.MyCounts | % { $_.Label + ' ' + $_.Count }) -join ' | ')
Snap $win "$Out\home.png"
$vm.SelectNodeByPath((Join-Path $Data '7000_MILKON')); Wait
Check 'proje sayıları: stoklarda kullanılan kablo ve jig' ($vm.Detail.HasProjectCounts -and (CountOf $vm.Detail.ProjectCounts 'Kablolar') -ge 1 -and (CountOf $vm.Detail.ProjectCounts 'Jigler') -ge 1) (@($vm.Detail.ProjectCounts | % { $_.Label + ' ' + $_.Count }) -join ' | ')
Snap $win "$Out\home-project.png"
$vm.SelectedNode = $null; $vm.LoadHome(); Wait

$row = @($vm.HomeMyTasks | ? { $_.Text -eq 'Benim deneme işim' })[0]
$vm.OpenHomeTask($row); Wait
Check 'işe tıklayınca stok açılır' ($vm.HasSelection -and $vm.SelectedNode.Path -eq $stock) $vm.SelectedNode.Path
$win.Close(); Pump
