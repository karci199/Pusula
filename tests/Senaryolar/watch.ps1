param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
. (Join-Path $PSScriptRoot '..\_Ortak.ps1')
# Diskteki değişikliği izleme: dışarıdan değişiklikte Yenile düğmesi renklenir, uygulamanın kendi yazdığı ve gizli/takip dosyaları sayılmaz.
[System.Threading.SynchronizationContext]::SetSynchronizationContext((New-Object System.Windows.Threading.DispatcherSynchronizationContext))
$app = New-Object Pusula.Wpf.App; $app.InitializeComponent(); $app.ShutdownMode = 'OnExplicitShutdown'; [Pusula.Wpf.Themes]::Apply([Pusula.Wpf.Themes]::Find('FluentDark'))
$win = New-Object Pusula.Wpf.MainWindow
$win.WindowStartupLocation = 'Manual'; $win.Left = -20000; $win.Top = -20000; $win.ShowActivated = $false; $win.Width = 1240; $win.Height = 760
$win.Show(); Pump
$vm = $win.DataContext; $vm.RootPath = $Data; Wait
$W = [Pusula.Core.Scanning.DiskWatcher]
function Settle() { Start-Sleep -Milliseconds ([int]$W::OwnWriteWindow.TotalMilliseconds + 300) }
function Changed() { for ($k = 0; $k -lt 40; $k++) { Pump; Start-Sleep -Milliseconds 50; if ($vm.IsDiskChanged) { return $true } }; return $vm.IsDiskChanged }
$stock = Join-Path $Data '7000_MILKON\AB-7000-0003'

"--- 1. Hangi yollar sayılır"
Check 'not dosyası sayılır' ($W::Counts($Data, "$stock\AB-7000-0003.md"))
Check '.obsidian sayılmaz' (-not $W::Counts($Data, "$Data\.obsidian\workspace.json"))
Check '_TAKIP sayılmaz' (-not $W::Counts($Data, "$stock\_TAKIP\stok.json"))
Check 'geçici dosya sayılmaz' (-not $W::Counts($Data, "$stock\AB.md.tmp") -and -not $W::Counts($Data, "$stock\AB.md.tmp-1a2b3c4d"))

"--- 2. Dışarıdan değişiklik"
Settle
Check 'başta renk yok' (-not $vm.IsDiskChanged)
[IO.File]::WriteAllText("$stock\disaridan.md", 'deneme')
Check 'dışarıdan dosya: düğme renklenir' (Changed)
Snap $win "$Out\watch-changed.png"
$vm.RefreshCommand.Execute($null); Wait
Check 'yenileyince söner' (-not $vm.IsDiskChanged)

"--- 3. Sayılmayanlar"
Settle
New-Item -ItemType Directory "$Data\.obsidian" -Force | Out-Null
[IO.File]::WriteAllText("$Data\.obsidian\workspace.json", '{}')
[IO.File]::WriteAllText("$stock\_TAKIP\deneme.json", '{}')
Check 'gizli ve takip klasörü: renk yok' (-not (Changed))

"--- 4. Uygulamanın kendi yazdığı"
$vm.SelectNodeByPath($stock); Wait
Check 'kısa not yazıldı' ($vm.AddQuickNoteHere([Pusula.Wpf.ViewModels.QuickNoteTarget]::Stock, 'izleme denemesi')); Wait
Check 'kısa not (uygulama yazdı): renk yok' (-not (Changed)) $vm.DiskChangedPath
Settle
Remove-Item "$stock\disaridan.md"
Check 'dışarıdan silme: renklenir' (Changed)
$vm.RefreshCommand.Execute($null); Wait; Settle
New-Item -ItemType Directory "$stock\09_Yeni_klasor" | Out-Null
Check 'dışarıdan yeni klasör: renklenir' (Changed) $vm.DiskChangedPath
$vm.RefreshCommand.Execute($null); Wait; Settle
[IO.File]::AppendAllText("$stock\AB-7000-0003.md", 'x')
Check 'not dışarıda düzenlendi: renklenir' (Changed)

"--- 5. Kök değişince"
$vm.RootPath = ''; Wait
Check 'kök yokken renk yok' (-not $vm.IsDiskChanged)
"SONUÇ OK=$ok HATA=$fail"
