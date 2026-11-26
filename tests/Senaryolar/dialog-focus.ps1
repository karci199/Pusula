param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
. (Join-Path $PSScriptRoot '..\_Ortak.ps1')
# 26.11.2026 (DesignKit 0.3.9): ortadaki pencereler (stok ekleme, gereksinim …) uygulama odağı kaybedince gizlenmez.
# Pencere ana pencerenin sahipliğinde, "her zaman üstte" değil; başka uygulama öne gelince ikisini birlikte örter.
# Senaryodaki ana pencere hiç etkinleşmez (ShowActivated = false): odak başka uygulamadaymış gibi.
[System.Threading.SynchronizationContext]::SetSynchronizationContext((New-Object System.Windows.Threading.DispatcherSynchronizationContext))
$app = New-Object Pusula.Wpf.App; $app.InitializeComponent(); $app.ShutdownMode = 'OnExplicitShutdown'
$win = New-Object Pusula.Wpf.MainWindow
$win.WindowStartupLocation = 'Manual'; $win.Left = -20000; $win.Top = -20000; $win.ShowActivated = $false; $win.Width = 1360; $win.Height = 900
$win.Show(); Pump
$vm = $win.DataContext; $vm.RootPath = $Data; Wait

$zorder = [DesignKit.Controls.ContentDialog].Assembly.GetType('DesignKit.Controls.PopupZOrder')
$isTopmost = $zorder.GetMethod('IsPopupTopmost', [Reflection.BindingFlags]'Static,Public,NonPublic')
Add-Type -Namespace PusulaTest -Name User32 -MemberDefinition '[DllImport("user32.dll")] public static extern System.IntPtr GetWindow(System.IntPtr h, uint cmd);'
$mainHwnd = (New-Object System.Windows.Interop.WindowInteropHelper $win).Handle

function OpenDialogs
{
    $found = @(); $st = New-Object System.Collections.Stack; $st.Push($win)
    while ($st.Count) { $e = $st.Pop(); if ($e -is [DesignKit.Controls.ContentDialog] -and $e.IsOpen) { $found += $e }; for ($i = 0; $i -lt [System.Windows.Media.VisualTreeHelper]::GetChildrenCount($e); $i++) { $st.Push([System.Windows.Media.VisualTreeHelper]::GetChild($e, $i)) } }
    $found
}

function CheckDialog([string]$name)
{
    $dlgs = @(OpenDialogs)
    Check ($name + ': açık pencere bulundu') ($dlgs.Count -eq 1) ([string]$dlgs.Count)
    if ($dlgs.Count -ne 1) { return }
    $d = $dlgs[0]
    $popup = $d.Template.FindName('PART_Popup', $d)
    Check ($name + ': ana pencere etkin değilken de görünür') (-not $win.IsActive -and $d.IsPopupOpen -and $popup.IsOpen)
    $src = [System.Windows.PresentationSource]::FromVisual($popup.Child)
    Check ($name + ': her zaman üstte değil') ($src -and -not $isTopmost.Invoke($null, @($popup)))
    Check ($name + ': ana pencerenin sahipliğinde') ($src -and [PusulaTest.User32]::GetWindow($src.Handle, 4) -eq $mainHwnd)
}

"--- Stok ekleme penceresi"
$vm.SelectNodeByPath((Join-Path $Data '7000_MILKON')); Wait
$vm.NewStockCommand.Execute($null); Wait; Wait
CheckDialog 'stok ekleme'
$vm.IsStockDialogOpen = $false; Wait
Check 'kapanınca gizlenir' (@(OpenDialogs).Count -eq 0)

"--- Gereksinim penceresi"
$vm.SelectNodeByPath((Join-Path $Data '7000_MILKON\AB-7000-0002')); Wait; Wait
$vm.AddVerificationCommand.Execute($null); Wait; Wait
CheckDialog 'gereksinim'
$vm.IsVerificationDialogOpen = $false; Wait

$win.Close()
