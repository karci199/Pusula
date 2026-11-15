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

# 2. Kapak kırpılmaz: Genel sekmesindeki kapak çerçevesi kaldırıldı (15.11.2026, stok sayfası yeniden tasarlanıyor).
"SONUÇ OK=$ok HATA=$fail"