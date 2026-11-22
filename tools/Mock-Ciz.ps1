# XAML mock'u uygulamanın kendi temasıyla (DesignKit, FluentDark) ekran dışında çizip PNG'ye döker.
# Örnek: powershell -NoProfile -ExecutionPolicy Bypass -STA -File tools\Mock-Ciz.ps1 -Xaml docs\mock\test_araci_sayfasi.xaml
# Mock dosyası tek bir kök öğedir (Border, Grid …); DynamicResource token'ları uygulamadaki gibi çözülür.
param(
    [Parameter(Mandatory = $true)][string]$Xaml,
    [string]$Png = '',
    [string]$Tema = 'FluentDark',
    [string]$Bin = ''
)

$ErrorActionPreference = 'Stop'
if ($Bin -eq '') { $Bin = Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) '..\src\Pusula.Wpf\bin\x64\Debug\net48' }
Add-Type -AssemblyName PresentationCore, PresentationFramework, WindowsBase, System.Xaml
[Reflection.Assembly]::LoadFrom((Join-Path $Bin 'Pusula.Core.dll')) | Out-Null
[Reflection.Assembly]::LoadFrom((Join-Path $Bin 'DesignKit.Wpf.dll')) | Out-Null
[Reflection.Assembly]::LoadFrom((Join-Path $Bin 'Pusula.exe')) | Out-Null

if ($Png -eq '') { $Png = [IO.Path]::ChangeExtension((Resolve-Path $Xaml).Path, '.png') }

$app = New-Object Pusula.Wpf.App
$app.InitializeComponent()
$app.ShutdownMode = 'OnExplicitShutdown'
[Pusula.Wpf.Themes]::Apply([Pusula.Wpf.Themes]::Find($Tema))

$text = [IO.File]::ReadAllText((Resolve-Path $Xaml).Path)
$root = [System.Windows.Markup.XamlReader]::Parse($text)

$win = New-Object System.Windows.Window
$win.WindowStyle = 'None'; $win.ShowInTaskbar = $false; $win.ShowActivated = $false
$win.WindowStartupLocation = 'Manual'; $win.Left = -30000; $win.Top = -30000
$win.SizeToContent = 'WidthAndHeight'
$win.Content = $root
$win.Show()

$frame = New-Object System.Windows.Threading.DispatcherFrame
[System.Windows.Threading.Dispatcher]::CurrentDispatcher.BeginInvoke([System.Windows.Threading.DispatcherPriority]::ApplicationIdle, [Action]{ $frame.Continue = $false }) | Out-Null
[System.Windows.Threading.Dispatcher]::PushFrame($frame)
$root.UpdateLayout()

$w = [int][Math]::Ceiling($root.ActualWidth); $h = [int][Math]::Ceiling($root.ActualHeight)
$bmp = New-Object System.Windows.Media.Imaging.RenderTargetBitmap($w, $h, 96, 96, [System.Windows.Media.PixelFormats]::Pbgra32)
$bmp.Render($root)
$enc = New-Object System.Windows.Media.Imaging.PngBitmapEncoder
$enc.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($bmp))
$fs = [IO.File]::Create($Png)
try { $enc.Save($fs) } finally { $fs.Close() }
$win.Close()
"PNG: $Png ($w x $h)"
