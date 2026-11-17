<#
.SYNOPSIS
    MeDitor uygulama ikonunu çizer: src\MeDitor\Assets\meditor.ico (16–256 px) ve meditor-logo.png (256 px).

.DESCRIPTION
    Pusula ikonuyla aynı düzen (yuvarlak köşeli koyu kare); üstünde köşesi kıvrık beyaz sayfa ve md işareti (M ve aşağı ok).
    Renkleri değiştirmek için aşağıdaki değişkenleri düzenleyip betiği yeniden çalıştır:
        powershell -NoProfile -ExecutionPolicy Bypass -File tools\MeDitor-Ikon.ps1
#>
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$tile = [System.Drawing.ColorTranslator]::FromHtml('#16324A')   # zemin
$page = [System.Drawing.ColorTranslator]::FromHtml('#F2F6FA')   # sayfa
$fold = [System.Drawing.ColorTranslator]::FromHtml('#B9CCDD')   # kıvrık köşe
$mark = [System.Drawing.ColorTranslator]::FromHtml('#2A9FE0')   # M↓ işareti

$assets = Join-Path (Split-Path $PSScriptRoot -Parent) 'src\MeDitor\Assets'
New-Item -ItemType Directory -Force $assets | Out-Null

function Draw([int]$size)
{
    $bmp = New-Object System.Drawing.Bitmap $size, $size, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'
    $g.PixelOffsetMode = 'HighQuality'
    $g.Clear([System.Drawing.Color]::Transparent)
    $s = $size / 256.0

    # Zemin: yuvarlak köşeli kare
    $x = 2 * $s; $w = 252 * $s; $r = 56 * $s
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $path.AddArc([float]$x, [float]$x, [float](2 * $r), [float](2 * $r), 180, 90)
    $path.AddArc([float]($x + $w - 2 * $r), [float]$x, [float](2 * $r), [float](2 * $r), 270, 90)
    $path.AddArc([float]($x + $w - 2 * $r), [float]($x + $w - 2 * $r), [float](2 * $r), [float](2 * $r), 0, 90)
    $path.AddArc([float]$x, [float]($x + $w - 2 * $r), [float](2 * $r), [float](2 * $r), 90, 90)
    $path.CloseFigure()
    $g.FillPath((New-Object System.Drawing.SolidBrush $tile), $path)

    # Sayfa: sağ üst köşesi kıvrık
    $l = 58 * $s; $t = 36 * $s; $rr = 198 * $s; $b = 220 * $s; $f = 46 * $s
    $pg = [System.Drawing.PointF[]]@((New-Object System.Drawing.PointF $l, $t), (New-Object System.Drawing.PointF ($rr - $f), $t), (New-Object System.Drawing.PointF $rr, ($t + $f)), (New-Object System.Drawing.PointF $rr, $b), (New-Object System.Drawing.PointF $l, $b))
    $g.FillPolygon((New-Object System.Drawing.SolidBrush $page), $pg)
    $fd = [System.Drawing.PointF[]]@((New-Object System.Drawing.PointF ($rr - $f), $t), (New-Object System.Drawing.PointF ($rr - $f), ($t + $f)), (New-Object System.Drawing.PointF $rr, ($t + $f)))
    $g.FillPolygon((New-Object System.Drawing.SolidBrush $fold), $fd)

    # M↓: kalın çizgilerle M ve yanında aşağı ok
    $pen = New-Object System.Drawing.Pen $mark, ([float][Math]::Max(1.6, 17 * $s))
    $pen.LineJoin = 'Round'; $pen.StartCap = 'Round'; $pen.EndCap = 'Round'
    $y1 = 112 * $s; $y2 = 176 * $s
    $m = [System.Drawing.PointF[]]@((New-Object System.Drawing.PointF (82 * $s), $y2), (New-Object System.Drawing.PointF (82 * $s), $y1), (New-Object System.Drawing.PointF (108 * $s), (146 * $s)), (New-Object System.Drawing.PointF (134 * $s), $y1), (New-Object System.Drawing.PointF (134 * $s), $y2))
    $g.DrawLines($pen, $m)
    $g.DrawLine($pen, [float](168 * $s), [float]$y1, [float](168 * $s), [float]($y2 - 4 * $s))
    $arrow = [System.Drawing.PointF[]]@((New-Object System.Drawing.PointF (150 * $s), (154 * $s)), (New-Object System.Drawing.PointF (168 * $s), $y2), (New-Object System.Drawing.PointF (186 * $s), (154 * $s)))
    $g.DrawLines($pen, $arrow)
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
$logo.Save((Join-Path $assets 'meditor-logo.png'), [System.Drawing.Imaging.ImageFormat]::Png)
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
[IO.File]::WriteAllBytes((Join-Path $assets 'meditor.ico'), $out.ToArray())
'ikon yazıldı: ' + $assets
