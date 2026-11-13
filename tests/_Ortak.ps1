# Senaryoların ortak yardımcıları. Her senaryo bunu nokta ile yükler: . (Join-Path $PSScriptRoot '..\_Ortak.ps1')
# $Bin: Pusula.exe'nin bulunduğu klasör (Calistir.ps1 verir).
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName PresentationCore, PresentationFramework, WindowsBase
[Reflection.Assembly]::LoadFrom((Join-Path $Bin 'Pusula.Core.dll')) | Out-Null
[Reflection.Assembly]::LoadFrom((Join-Path $Bin 'DesignKit.Wpf.dll')) | Out-Null
[Reflection.Assembly]::LoadFrom((Join-Path $Bin 'Pusula.exe')) | Out-Null
$ok = 0; $fail = 0

# Bir denetim: koşul doğruysa OK, değilse HATA (+ ayrıntı) yazar ve sayar.
function Check([string]$name, [bool]$cond, [string]$detail = '')
{
    if ($cond) { $script:ok++; "  OK   $name" } else { $script:fail++; "  HATA $name $detail" }
}

# Dispatcher kuyruğunu bir kez boşaltır (bağlamalar, düzen).
function Pump
{
    $frame = New-Object System.Windows.Threading.DispatcherFrame
    [System.Windows.Threading.Dispatcher]::CurrentDispatcher.BeginInvoke([System.Windows.Threading.DispatcherPriority]::Background, [Action]{ $frame.Continue = $false }) | Out-Null
    [System.Windows.Threading.Dispatcher]::PushFrame($frame)
}

# Arka plan okumalarının (Task.Run) bitmesini bekler.
function Wait
{
    for ($k = 0; $k -lt 60; $k++) { Pump; Start-Sleep -Milliseconds 25 }
}

# Pencereyi ekran dışında PNG olarak çizer ($Out klasörüne).
function Snap($window, [string]$path)
{
    Wait
    $bmp = New-Object System.Windows.Media.Imaging.RenderTargetBitmap([int]$window.ActualWidth, [int]$window.ActualHeight, 96, 96, [System.Windows.Media.PixelFormats]::Pbgra32)
    $bmp.Render([System.Windows.Media.VisualTreeHelper]::GetChild($window, 0))
    $png = New-Object System.Windows.Media.Imaging.PngBitmapEncoder
    $png.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($bmp))
    $fs = [IO.File]::Create($path)
    try { $png.Save($fs) } finally { $fs.Close() }
}

# Ağaçta başlığı verilen ilk düğüm.
function FindNode($nodes, [string]$title)
{
    foreach ($n in $nodes) { if ($n.Title -eq $title) { return $n }; $r = FindNode $n.Children $title; if ($r) { return $r } }
    return $null
}

# Aramada görünen düğümlerin başlıkları (sırayla).
function Visible($nodes)
{
    foreach ($n in $nodes) { if ($n.IsVisible) { $n.Title; Visible $n.Children } }
}