param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
. (Join-Path $PSScriptRoot '..\_Ortak.ps1')
Add-Type -AssemblyName System.Drawing
# Kapak fotoğrafı: dikey, çok geniş ve EXIF yönlü (telefon) fotoğraflar kırpılmadan, doğru yönde görünür.
function NewPhoto([string]$path, [int]$w, [int]$h, [int]$orientation = 0)
{
    $bmp = New-Object System.Drawing.Bitmap $w, $h
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.Clear([System.Drawing.Color]::SteelBlue)
    $g.FillRectangle([System.Drawing.Brushes]::Orange, 0, 0, $w, [int]($h / 6))
    $g.Dispose()
    if ($orientation -gt 0)
    {
        $item = [Runtime.Serialization.FormatterServices]::GetUninitializedObject([System.Drawing.Imaging.PropertyItem])
        $item.Id = 274; $item.Type = 3; $item.Len = 2; $item.Value = [BitConverter]::GetBytes([uint16]$orientation)
        $bmp.SetPropertyItem($item)
        $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Jpeg)
    }
    else
    {
        $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    }

    $bmp.Dispose()
}

function FindChild($d, [type]$type)
{
    if ($d -is $type) { return $d }
    for ($i = 0; $i -lt [System.Windows.Media.VisualTreeHelper]::GetChildrenCount($d); $i++)
    {
        $r = FindChild ([System.Windows.Media.VisualTreeHelper]::GetChild($d, $i)) $type
        if ($r) { return $r }
    }
    return $null
}

"--- 1. EXIF yönü"
$tmp = Join-Path $env:TEMP ('pusula-exif-' + [guid]::NewGuid().ToString('N').Substring(0, 6)); New-Item -ItemType Directory $tmp | Out-Null
NewPhoto "$tmp\yan.jpg" 400 200 6
NewPhoto "$tmp\duz.png" 400 200
Check 'yön 6 → 90°' ([Pusula.Wpf.PathToImageConverter]::RotationOf("$tmp\yan.jpg") -eq 90)
Check 'EXIF yok → 0°' ([Pusula.Wpf.PathToImageConverter]::RotationOf("$tmp\duz.png") -eq 0)
$conv = New-Object Pusula.Wpf.PathToImageConverter
$img = $conv.Convert("$tmp\yan.jpg", [object], $null, [Globalization.CultureInfo]::InvariantCulture)
Check 'döndürülmüş resim dik (en < boy)' ($img.PixelWidth -lt $img.PixelHeight) ([string]$img.PixelWidth + 'x' + [string]$img.PixelHeight)
Remove-Item $tmp -Recurse -Force

"--- 2. Kapak kırpılmaz"
$photos = Join-Path $Data '7000_MILKON\AB-7000-0002\07_Fotograflar'
Remove-Item (Join-Path $photos 'SH2000_kapak.png')
$cases = @(@{ Name = 'dikey'; W = 300; H = 600; O = 0 }, @{ Name = 'genis'; W = 1200; H = 300; O = 0 }, @{ Name = 'telefon'; W = 800; H = 600; O = 6 })
$settingsFile = Join-Path $env:LOCALAPPDATA 'Pusula\ayarlar.json'
$savedSettings = if (Test-Path $settingsFile) { [IO.File]::ReadAllText($settingsFile) } else { $null }
try {
    [System.Threading.SynchronizationContext]::SetSynchronizationContext((New-Object System.Windows.Threading.DispatcherSynchronizationContext))
    $app = New-Object Pusula.Wpf.App; $app.InitializeComponent(); $app.ShutdownMode = 'OnExplicitShutdown'
    $win = New-Object Pusula.Wpf.MainWindow
    $win.WindowStartupLocation = 'Manual'; $win.Left = -20000; $win.Top = -20000; $win.ShowActivated = $false; $win.Width = 1240; $win.Height = 760
    $win.Show(); Pump
    $vm = $win.DataContext; $vm.RootPath = $Data; Pump
    foreach ($c in $cases)
    {
        Get-ChildItem $photos -Filter '*kapak*' | Remove-Item
        $ext = if ($c.O -gt 0) { '.jpg' } else { '.png' }
        NewPhoto (Join-Path $photos ('SH2000_kapak_' + $c.Name + $ext)) $c.W $c.H $c.O
        $vm.Refresh(); Pump
        $vm.SelectNodeByPath((Join-Path $Data '7000_MILKON\AB-7000-0002')); Wait
        $button = $win.FindName('CoverButton')
        $image = FindChild $button ([System.Windows.Controls.Image])
        $src = $image.Source
        $shownRatio = $image.ActualWidth / $image.ActualHeight
        $srcRatio = $src.PixelWidth / $src.PixelHeight
        $fits = $image.ActualWidth -le $button.ActualWidth -and $image.ActualHeight -le $button.ActualHeight
        Check ($c.Name + ': oran korunur, kutuya sığar') ([Math]::Abs($shownRatio - $srcRatio) -lt 0.03 -and $fits) ('gösterilen ' + $image.ActualWidth.ToString('0') + 'x' + $image.ActualHeight.ToString('0') + ' kaynak ' + $src.PixelWidth + 'x' + $src.PixelHeight)
        if ($c.O -gt 0)
        {
            Check ($c.Name + ': dik gösterilir') ($image.ActualHeight -gt $image.ActualWidth)
        }

        Snap $win ($Out + '\kapak-' + $c.Name + '.png')
    }
} finally {
    if ($savedSettings -ne $null) { [IO.File]::WriteAllText($settingsFile, $savedSettings) }
}
"SONUÇ OK=$ok HATA=$fail"