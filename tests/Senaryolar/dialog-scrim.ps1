param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
. (Join-Path $PSScriptRoot '..\_Ortak.ps1')
# Diyalog kapanınca pencerenin üstündeki karartma (scrim) kalmamalı: yeni proje, yeni stok, hatalı onay sonrası yeniden açılış.
[System.Threading.SynchronizationContext]::SetSynchronizationContext((New-Object System.Windows.Threading.DispatcherSynchronizationContext))
$app = New-Object Pusula.Wpf.App; $app.InitializeComponent(); $app.ShutdownMode = 'OnExplicitShutdown'
$win = New-Object Pusula.Wpf.MainWindow
$win.WindowStartupLocation = 'Manual'; $win.Left = -20000; $win.Top = -20000; $win.ShowActivated = $true; $win.Width = 1240; $win.Height = 760
$win.Show(); $win.Activate() | Out-Null; Pump
$vm = $win.DataContext; $vm.RootPath = $Data; Wait

function Scrims()
{
    $n = 0
    $st = New-Object System.Collections.Stack; $st.Push($win)
    while ($st.Count -gt 0)
    {
        $x = $st.Pop()
        if ($x -is [System.Windows.UIElement])
        {
            $layer = [System.Windows.Documents.AdornerLayer]::GetAdornerLayer($x)
            if ($layer) { $ads = $layer.GetAdorners($x); if ($ads) { $n += @($ads | Where-Object { $_.GetType().Name -like '*Scrim*' }).Count } }
        }
        for ($i = 0; $i -lt [System.Windows.Media.VisualTreeHelper]::GetChildrenCount($x); $i++) { $st.Push([System.Windows.Media.VisualTreeHelper]::GetChild($x, $i)) }
    }
    $n
}

Check 'başta karartma yok' ((Scrims) -eq 0) ([string](Scrims))
$vm.NewProjectCommand.Execute($null); Wait
Check 'yeni proje açıkken karartma var' ((Scrims) -ge 1) ([string](Scrims))
$vm.IsProjectDialogOpen = $false; Wait; Wait
Check 'kapanınca karartma kalkar' ((Scrims) -eq 0) ([string](Scrims))

"--- hatalı onay: kapanıp hemen yeniden açılır, sonra Vazgeç"
$vm.NewProjectCommand.Execute($null); Wait
$vm.NewProjectNo = ''; $vm.NewProjectName = ''
$vm.IsProjectDialogOpen = $false; Pump
$ok2 = $vm.ConfirmNewProject()
$vm.IsProjectDialogOpen = $true; Wait
Check 'yeniden açılınca tek karartma' ((Scrims) -eq 1) ([string](Scrims))
$vm.IsProjectDialogOpen = $false; Wait; Wait
Check 'Vazgeç sonrası karartma kalmaz' ((Scrims) -eq 0) ([string](Scrims))

"--- hatalı onay düğmesi: pencere hiç kapanmaz (kırpışma yok)"
function OpenFormDialog()
{
    $st = New-Object System.Collections.Stack; $st.Push($win)
    while ($st.Count -gt 0)
    {
        $x = $st.Pop()
        if ($x -is [Pusula.Wpf.FormDialog] -and $x.IsOpen) { return $x }
        for ($i = 0; $i -lt [System.Windows.Media.VisualTreeHelper]::GetChildrenCount($x); $i++) { $st.Push([System.Windows.Media.VisualTreeHelper]::GetChild($x, $i)) }
    }
    $null
}
$templateChild = [System.Windows.FrameworkElement].GetMethod('GetTemplateChild', [System.Reflection.BindingFlags]'NonPublic,Instance')
$vm.NewProjectCommand.Execute($null); Wait
$vm.NewProjectNo = ''; $vm.NewProjectName = ''
$dlg = OpenFormDialog
Check 'yeni proje penceresi FormDialog' ($dlg -ne $null)
$btn = $templateChild.Invoke($dlg, @('PART_PrimaryButton'))
$btn.RaiseEvent((New-Object System.Windows.RoutedEventArgs ([System.Windows.Controls.Primitives.ButtonBase]::ClickEvent)))
Check 'hata: IsOpen hemen ardından da açık' ($dlg.IsOpen -and $vm.IsProjectDialogOpen)
Wait
Check 'hata: hâlâ açık, tek karartma' ($dlg.IsOpen -and $vm.IsProjectDialogOpen -and (Scrims) -eq 1) ([string](Scrims))
$vm.IsProjectDialogOpen = $false; Wait; Wait
Check 'Vazgeç ile kapanır, karartma kalmaz' (-not $dlg.IsOpen -and (Scrims) -eq 0) ([string](Scrims))
$vm.NewProjectCommand.Execute($null); Wait
Check 'yeniden açılır' ($vm.IsProjectDialogOpen -and $dlg.IsOpen)
$vm.IsProjectDialogOpen = $false; Wait; Wait

"--- pencere etkin değilken kapanış"
$vm.NewProjectCommand.Execute($null); Wait
$other = New-Object System.Windows.Window; $other.Left = -20000; $other.Top = -20000; $other.Width = 200; $other.Height = 100; $other.Show(); $other.Activate() | Out-Null; Wait
$vm.IsProjectDialogOpen = $false; Wait
$other.Close(); $win.Activate() | Out-Null; Wait; Wait
Check 'etkin değilken kapanınca da karartma kalmaz' ((Scrims) -eq 0) ([string](Scrims))
Snap $win "$Out\dialog-scrim.png"
$win.Close()
"SONUÇ OK=$ok HATA=$fail"
