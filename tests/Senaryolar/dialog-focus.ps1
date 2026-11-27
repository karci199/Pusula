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

function SnapDialog([string]$path)
{
    $d = @(OpenDialogs)[0]
    $child = $d.Template.FindName('PART_Popup', $d).Child
    $child.Measure((New-Object System.Windows.Size 1360, 900)); $child.Arrange((New-Object System.Windows.Rect 0, 0, 1360, 900)); $child.UpdateLayout(); Wait
    $rtb = New-Object System.Windows.Media.Imaging.RenderTargetBitmap 2040, 1350, 144, 144, ([System.Windows.Media.PixelFormats]::Pbgra32)
    $dv = New-Object System.Windows.Media.DrawingVisual; $dc = $dv.RenderOpen()
    $dc.DrawRectangle((New-Object System.Windows.Media.VisualBrush $win), $null, (New-Object System.Windows.Rect 0, 0, 1360, 900))
    $dc.DrawRectangle((New-Object System.Windows.Media.VisualBrush $child), $null, (New-Object System.Windows.Rect 0, 0, 1360, 900))
    $dc.Close(); $rtb.Render($dv)
    $pe = New-Object System.Windows.Media.Imaging.PngBitmapEncoder; $pe.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($rtb))
    $fs = [IO.File]::Create($path); $pe.Save($fs); $fs.Close()
}

"--- Stok ekleme penceresi"
$vm.SelectNodeByPath((Join-Path $Data '7000_MILKON')); Wait
$vm.NewStockCommand.Execute($null); Wait; Wait
CheckDialog 'stok ekleme'
# 27.11.2026: ekleme pencereleri genişledi; stokta modüller iki sütun, içerik kayar.
$sd = @(OpenDialogs)[0]
Check 'stok penceresi 680 geniş, şablon modülleri listede' ($sd.DialogWidth -eq 680 -and $vm.StockModules.Count -gt 0) ([string]$sd.DialogWidth)
SnapDialog "$Out\stock-dialog.png"
$vm.IsStockDialogOpen = $false; Wait
Check 'kapanınca gizlenir' (@(OpenDialogs).Count -eq 0)

"--- Gereksinim penceresi"
$vm.SelectNodeByPath((Join-Path $Data '7000_MILKON\AB-7000-0002')); Wait; Wait
$vm.AddVerificationCommand.Execute($null); Wait; Wait
CheckDialog 'gereksinim'
$vm.IsVerificationDialogOpen = $false; Wait

$win.Close()
