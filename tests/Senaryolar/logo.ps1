param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
. (Join-Path $PSScriptRoot '..\_Ortak.ps1')
# Başlık çubuğundaki Pusula simgesine tıklayınca ana sayfaya dönülür.
[System.Threading.SynchronizationContext]::SetSynchronizationContext((New-Object System.Windows.Threading.DispatcherSynchronizationContext))
$app = New-Object Pusula.Wpf.App; $app.InitializeComponent(); $app.ShutdownMode = 'OnExplicitShutdown'
$win = New-Object Pusula.Wpf.MainWindow
$win.WindowStartupLocation = 'Manual'; $win.Left = -20000; $win.Top = -20000; $win.ShowActivated = $false; $win.Width = 1240; $win.Height = 760
$win.Show(); Pump
$vm = $win.DataContext; $vm.RootPath = $Data; Wait
$st = New-Object System.Collections.Stack; $st.Push($win); $logo = $null
while ($st.Count -gt 0) { $n = $st.Pop(); if ($n -is [System.Windows.Controls.Image] -and [object]::ReferenceEquals($n.TemplatedParent, $win)) { $logo = $n; break }; for ($i = 0; $i -lt [System.Windows.Media.VisualTreeHelper]::GetChildrenCount($n); $i++) { $st.Push([System.Windows.Media.VisualTreeHelper]::GetChild($n, $i)) } }
Check 'logo bulundu, el imleci' ($logo -ne $null -and $logo.Cursor -eq [System.Windows.Input.Cursors]::Hand)
$vm.SelectNodeByPath((Join-Path $Data '7000_MILKON\AB-7000-0002')); Wait
$e = New-Object System.Windows.Input.MouseButtonEventArgs([System.Windows.Input.Mouse]::PrimaryDevice, 0, [System.Windows.Input.MouseButton]::Left)
$e.RoutedEvent = [System.Windows.UIElement]::MouseLeftButtonUpEvent; $logo.RaiseEvent($e); Wait
Check 'logoya tÄ±klayÄ±nca ana sayfa (seÃ§im yok)' (-not $vm.HasSelection)
$win.Close()
"SONUÃ‡ OK=$ok HATA=$fail"
