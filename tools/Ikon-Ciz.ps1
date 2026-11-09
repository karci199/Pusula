<#
.SYNOPSIS
    Pusula uygulama ikonunu çizer: src\Pusula.Wpf\Assets\pusula.ico (16–256 px) ve pusula-logo.png (256 px).

.DESCRIPTION
    KIWI'deki kivi ikonuyla aynı mantık: adı anlatan tek, düz renkli bir çizim. Seçilen biçim (B): yuvarlak köşeli koyu
    kare üzerinde soyut pusula ibresi (kuzey ucu turuncu-kırmızı, güney ucu açık). Renkleri değiştirmek için aşağıdaki
    değişkenleri düzenleyip betiği yeniden çalıştır:
        powershell -NoProfile -ExecutionPolicy Bypass -File tools\Ikon-Ciz.ps1
#>
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$tile = [System.Drawing.ColorTranslator]::FromHtml('#24364A')   # zemin (yuvarlak köşeli kare)
$north = [System.Drawing.ColorTranslator]::FromHtml('#FF6B57')  # kuzey ucu
$south = [System.Drawing.ColorTranslator]::FromHtml('#E9EEF4')  # güney ucu
$tilt = 40                                                       # ibrenin eğimi (derece)

$assets = Join-Path (Split-Path $PSScriptRoot -Parent) 'src\Pusula.Wpf\Assets'
New-Item -ItemType Directory -Force $assets | Out-Null

function Draw([int]$size)
{
    $bmp = New-Object System.Drawing.Bitmap $size, $size, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'
    $g.PixelOffsetMode = 'HighQuality'
    $g.Clear([System.Drawing.Color]::Transparent)
    $s = $size / 256.0
    $c = $size / 2.0

    # Zemin: yuvarlak köşeli kare
    $x = 2 * $s; $w = 252 * $s; $r = 56 * $s
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $path.AddArc([float]$x, [float]$x, [float](2 * $r), [float](2 * $r), 180, 90)
    $path.AddArc([float]($x + $w - 2 * $r), [float]$x, [float](2 * $r), [float](2 * $r), 270, 90)
    $path.AddArc([float]($x + $w - 2 * $r), [float]($x + $w - 2 * $r), [float](2 * $r), [float](2 * $r), 0, 90)
    $path.AddArc([float]$x, [float]($x + $w - 2 * $r), [float](2 * $r), [float](2 * $r), 90, 90)
    $path.CloseFigure()
    $g.FillPath((New-Object System.Drawing.SolidBrush $tile), $path)

    # İbre: iki üçgen (kuzey renkli, güney açık), ortada zemin renginde nokta
    $state = $g.Save()
    $g.TranslateTransform([float]$c, [float]$c)
    $g.RotateTransform([float]$tilt)
    $len = 112 * $s; $wid = 40 * $s
    $n = [System.Drawing.PointF[]]@((New-Object System.Drawing.PointF 0, (-$len)), (New-Object System.Drawing.PointF $wid, 0), (New-Object System.Drawing.PointF (-$wid), 0))
    $so = [System.Drawing.PointF[]]@((New-Object System.Drawing.PointF 0, $len), (New-Object System.Drawing.PointF $wid, 0), (New-Object System.Drawing.PointF (-$wid), 0))
    $g.FillPolygon((New-Object System.Drawing.SolidBrush $north), $n)
    $g.FillPolygon((New-Object System.Drawing.SolidBrush $south), $so)
    $g.Restore($state)

    $h = 14 * $s
    $g.FillEllipse((New-Object System.Drawing.SolidBrush $tile), [float]($c - $h), [float]($c - $h), [float](2 * $h), [float](2 * $h))
    $g.Dispose()
    return $bmp
}
function PngBytes($bmp)
{
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    return ,$ms.ToArray()
}

# Logo (pencere başlığı, Hakkında): 256 px PNG
$logo = Draw 256
$logo.Save((Join-Path $assets 'pusula-logo.png'), [System.Drawing.Imaging.ImageFormat]::Png)
$logo.Dispose()

# ICO: her boyut ayrı çizilir (küçültme değil), PNG olarak gömülür (Windows Vista+).
$sizes = 16, 24, 32, 48, 64, 128, 256
$images = New-Object 'System.Collections.Generic.List[byte[]]'
# 256 px PNG, küçükler klasik 32 bit bitmap (bazı Windows/.NET bileşenleri küçük PNG girdisini okuyamıyor).
function DibBytes($bmp)
{
    $n = $bmp.Width
    $ms = New-Object System.IO.MemoryStream
    $bw = New-Object System.IO.BinaryWriter $ms
    $bw.Write([uint32]40); $bw.Write([int32]$n); $bw.Write([int32]($n * 2)); $bw.Write([uint16]1); $bw.Write([uint16]32)
    $bw.Write([uint32]0); $bw.Write([uint32]0); $bw.Write([int32]0); $bw.Write([int32]0); $bw.Write([uint32]0); $bw.Write([uint32]0)
    for ($y = $n - 1; $y -ge 0; $y--)
    {
        for ($x = 0; $x -lt $n; $x++)
        {
            $p = $bmp.GetPixel($x, $y)
            $bw.Write([byte]$p.B); $bw.Write([byte]$p.G); $bw.Write([byte]$p.R); $bw.Write([byte]$p.A)
        }
    }
    $maskRow = [int]([Math]::Ceiling($n / 32.0) * 4)
    $bw.Write((New-Object byte[] ($maskRow * $n)))
    $bw.Flush()
    return ,$ms.ToArray()
}

foreach ($size in $sizes) { $b = Draw $size; if ($size -ge 256) { $images.Add((PngBytes $b)) } else { $images.Add((DibBytes $b)) }; $b.Dispose() }
$out = New-Object System.IO.MemoryStream
$w = New-Object System.IO.BinaryWriter $out
$w.Write([uint16]0); $w.Write([uint16]1); $w.Write([uint16]$sizes.Count)
$offset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++)
{
    $dim = if ($sizes[$i] -ge 256) { 0 } else { $sizes[$i] }
    $w.Write([byte]$dim); $w.Write([byte]$dim); $w.Write([byte]0); $w.Write([byte]0)
    $w.Write([uint16]1); $w.Write([uint16]32)
    $w.Write([uint32]$images[$i].Length); $w.Write([uint32]$offset)
    $offset += $images[$i].Length
}
foreach ($img in $images) { $w.Write($img) }
$w.Flush()
[IO.File]::WriteAllBytes((Join-Path $assets 'pusula.ico'), $out.ToArray())
'ikon yazıldı: ' + $assets
