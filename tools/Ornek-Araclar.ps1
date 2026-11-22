# Örnek test yardımcı araçları (22.11.2026): bir stokun altına her türden araç açar; alanları, fotoğrafları
# (kapak + numaralı şerit + görünmeyen bir numarasız), notun gövdesini ve Günlük olaylarını doldurur.
# Var olan kayda dokunmaz (aynı numara varsa atlar); hiçbir şey silmez.
# Örnek: powershell -NoProfile -ExecutionPolicy Bypass -File tools\Ornek-Araclar.ps1 -Kok C:\PusulaDeneme
param(
    [Parameter(Mandatory = $true)][string]$Kok,
    [string]$Stok = 'AB-7000-0003',
    [string]$Bin = ''
)

$ErrorActionPreference = 'Stop'
if ($Bin -eq '') { $Bin = Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) '..\src\Pusula.Wpf\bin\x64\Debug\net48' }
[Reflection.Assembly]::LoadFrom((Join-Path $Bin 'Pusula.Core.dll')) | Out-Null
Add-Type -AssemblyName System.Drawing

$stockFolder = Get-ChildItem $Kok -Recurse -Directory -Filter $Stok | Where-Object { $_.FullName -notmatch '\\_' } | Select-Object -First 1 -ExpandProperty FullName
if (-not $stockFolder) { throw "Stok bulunamadı: $Stok" }
$types = [Pusula.Core.Types.TypeSet]::CreateDefault()
$IS = [Pusula.Core.Inventory.InventoryService]
$Gunluk = [Pusula.Core.Notes.Journal]

# Basit örnek fotoğraf: koyu zemin, açık şekil, alt yazı.
function New-Photo([string]$path, [string]$label, [string]$shape)
{
    $bmp = New-Object System.Drawing.Bitmap 640, 400
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'
    $g.Clear([System.Drawing.Color]::FromArgb(46, 52, 64))
    $light = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(201, 206, 216))
    $gold = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(224, 176, 80))
    $pen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(201, 206, 216)), 14
    switch ($shape)
    {
        'cable' { $g.DrawBezier($pen, 80, 300, 220, 40, 420, 380, 560, 120); $g.FillRectangle($gold, 50, 285, 60, 34); $g.FillRectangle($light, 540, 100, 60, 40) }
        'jig'   { $g.FillRectangle($light, 120, 260, 400, 30); $g.FillRectangle($light, 270, 150, 100, 110); $g.DrawEllipse((New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(224, 176, 80)), 12), 270, 50, 100, 100) }
        'box'   { $g.FillRectangle($light, 200, 160, 240, 80); $g.FillRectangle($gold, 150, 180, 50, 40); $g.FillRectangle($gold, 440, 180, 50, 40) }
        'pole'  { $g.FillRectangle((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(176, 138, 90))), 305, 40, 30, 300); $g.FillRectangle($light, 230, 330, 180, 20) }
        default { $g.FillEllipse($light, 220, 100, 200, 200) }
    }
    $font = New-Object System.Drawing.Font 'Segoe UI', 22
    $g.DrawString($label, $font, [System.Drawing.Brushes]::White, 20, 345)
    $g.Dispose()
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
}

function Add-Tool($t)
{
    $type = $types.Find($t.Type)
    $r = $IS::Create($Kok, $type, [Pusula.Core.Inventory.InventoryLevel]::Stock, $stockFolder, $t.No, $t.Name, $t.Sub, '', 'katan', [DateTime]::Parse($t.Requested))
    if (-not $r.Succeeded) { "atlandı: $($t.No) ($($r.Error))"; return }
    $folder = $r.Folder

    # Alanlar ve durum olayları.
    $file = $IS::RecordFile($folder)
    $jf = [Pusula.Core.Tracking.JsonFile]; $rt = [Pusula.Core.Inventory.InventoryRecord]
    $record = $jf.GetMethod('TryRead').MakeGenericMethod($rt).Invoke($null, @($file))
    foreach ($k in $t.Fields.Keys) { $record.Fields[$k] = [string]$t.Fields[$k] }
    foreach ($e in $t.Events) { $record.Events.Add([Pusula.Core.Inventory.InventoryEvent]::Create($e[0], [DateTime]::Parse($e[1]), $e[2], $e[3], $e[4])) }
    $jf.GetMethod('WriteAtomic').MakeGenericMethod($rt).Invoke($null, @($file, $record)) | Out-Null

    # Fotoğraflar: kapak, numaralı şerit, bir numarasız (görünmez).
    if ($t.Photos)
    {
        New-Photo (Join-Path $folder ($t.No + '_kapak.png')) ($t.No + ' kapak') $t.Shape
        $i = 1
        foreach ($p in $t.Photos) { New-Photo (Join-Path $folder ([string]$i + '_' + $p + '.png')) ($p -replace '_', ' ') $t.Shape; $i++ }
        New-Photo (Join-Path $folder 'IMG_2041.png') 'numarasız, görünmez' $t.Shape
    }

    # Notun gövdesi (yeni açılan notun sonuna) ve Günlük olayları.
    $note = [Pusula.Core.Inventory.RecordContents]::NotePath($folder, $t.No)
    $text = [IO.File]::ReadAllText($note).TrimEnd() + "`r`n`r`n" + ($t.Body -join "`r`n") + "`r`n"
    [IO.File]::WriteAllText($note, $text, (New-Object Text.UTF8Encoding $false))
    foreach ($j in $t.Journal)
    {
        $entry = New-Object Pusula.Core.Notes.JournalEntry
        $entry.Date = [DateTime]::Parse($j.Date); $entry.Kind = $j.Kind; $entry.Title = $j.Title; $entry.Body = $j.Body; $entry.Person = $j.Person
        if ($j.Rev) { $entry.RevisionOld = $j.Rev[0]; $entry.RevisionNew = $j.Rev[1] }
        $err = $Gunluk::Add($note, $entry)
        if ($err -ne '') { throw "Günlük yazılamadı: $err" }
    }

    "açıldı: $($t.No) → $folder"
}

$tools = @(
    @{ Type = 'cable'; No = 'JT-7000-0101'; Name = 'Faz kararlı RF kablo, 1,5 m'; Sub = 'RF'; Requested = '2026-10-20'; Shape = 'cable'
       Fields = @{ boy = '1,5'; ekran = 'Ekranlı'; frekans = '2–18 GHz'; kullanim_amaci = 'SATIMO ölçümlerinde referans kablo' }
       Events = @(, @('Hazırlanıyor', '2026-10-22', 'Atölye', '', '')) + @(, @('Teslim alındı', '2026-11-01', 'katan', 'Yankısız oda, dolap 2', '')) + @(, @('Kullanımda', '2026-11-02', 'katan', '', ''))
       Photos = @('SMA_ucu', 'N_ucu', 'Etiket')
       Body = @('## Kullanım', 'Bükme yarıçapı en az 50 mm; tork 0,9 N·m.', '', '## Yapılacaklar', '- [ ] Yedek SMA adaptörü sipariş et', '- [x] Etiket yenilendi ✅ 2026-11-05')
       Journal = @(
           @{ Date = '2026-11-05'; Kind = 'Tamir / bakım'; Title = 'SMA ucunun konnektörü değişti'; Body = 'Tamirden sonra kalibrasyon gerekir.'; Person = 'Atölye' },
           @{ Date = '2026-11-12'; Kind = 'Kalibrasyon'; Title = 'Faz / kayıp doğrulaması: geçti'; Body = 'Sertifika K-2026-114, geçerlilik 12.11.2027.'; Person = 'Metroloji lab.' }) },
    @{ Type = 'cable'; No = 'JT-7000-0102'; Name = 'Pozisyoner güç kablosu'; Sub = 'Güç'; Requested = '2026-10-21'; Shape = 'cable'
       Fields = @{ boy = '3'; ekran = 'Ekransız'; gerilim = '28'; akim = '10'; kesit_awg = '16'; kullanim_amaci = 'Pozisyoner motor beslemesi' }
       Events = @(, @('Kullanımda', '2026-10-25', 'katan', '', ''))
       Photos = @('Konnektor')
       Body = @('## Kullanım', 'Besleme kapalıyken tak / çıkar.')
       Journal = @() },
    @{ Type = 'cable'; No = 'JT-7000-0103'; Name = 'RS422 kontrol kablosu'; Sub = 'Haberleşme'; Requested = '2026-10-21'; Shape = 'cable'
       Fields = @{ boy = '5'; ekran = 'Ekranlı'; pin_yapisi = 'D-Sub 9 ↔ D-Sub 9, 1:1'; kullanim_amaci = 'RS422 pozisyoner kontrolü' }
       Events = @(, @('Kullanımda', '2026-10-26', 'katan', '', ''))
       Photos = $null
       Body = @('Pin 2-3 çapraz değil; Brainbox çeviriciyle kullanılır.')
       Journal = @() },
    @{ Type = 'jig'; No = 'JT-7000-0104'; Name = 'Döner tabla montaj aparatı'; Sub = 'SATIMO'; Requested = '2026-10-28'; Shape = 'jig'
       Fields = @{ kullanim_amaci = 'DA400 antenini SATIMO döner tablasına bağlar' }
       Events = @(, @('Hazırlanıyor', '2026-10-30', 'M. Yılmaz', '', ''))
       Photos = @('Onden', 'Montajli', 'Civata_detayi')
       Body = @('## Amaç', 'Merkez kaçıklığı en fazla 0,5 mm.', '', '## Yapılacaklar', '- [ ] Cıvata deliklerini teknik resimle karşılaştır')
       Journal = @(@{ Date = '2026-11-04'; Kind = 'Revizyon'; Title = 'Cıvata delikleri 2 mm kaydırıldı'; Body = 'Teknik resim rev B.'; Person = 'M. Yılmaz'; Rev = @('A', 'B') }) },
    @{ Type = 'software'; No = 'JY-7000-0105'; Name = 'SATIMO ölçüm otomasyonu'; Sub = ''; Requested = '2026-10-01'; Shape = ''
       Fields = @{ kullanim_amaci = 'SATIMO ölçümünü otomatikleştirir' }
       Events = @(, @('Geliştiriliyor', '2026-10-02', 'A. Kaya', '', '')) + @(, @('Kullanımda', '2026-10-10', 'katan', '', ''))
       Photos = $null
       Body = @('## Kullanım', 'ayarlar.json''da VNA adresi; ölçümden önce kalibrasyon dosyası seçilir.')
       Journal = @(
           @{ Date = '2026-10-10'; Kind = 'Revizyon'; Title = 'İlk kullanım sürümü'; Body = 'Tek frekans tarama, CSV çıktısı.'; Person = 'A. Kaya'; Rev = @('', 'AA') },
           @{ Date = '2026-11-02'; Kind = 'Revizyon'; Title = '360° tarama ve Touchstone çıktısı'; Body = 'Döner tabla adım açısı ayarlanabilir.'; Person = 'A. Kaya'; Rev = @('AA', 'AB') },
           @{ Date = '2026-11-15'; Kind = 'Revizyon'; Title = 'Faz hesabında işaret düzeltildi'; Body = 'AB-7000-0003 ölçümünde faz 180° ters çıkıyordu.'; Person = 'A. Kaya'; Rev = @('AB', 'AC') }) },
    @{ Type = 'equipment'; No = 'JT-7000-0106'; Name = '20 dB zayıflatıcı, 18 GHz'; Sub = 'Zayıflatıcı'; Requested = '2025-12-01'; Shape = 'box'
       Fields = @{ kullanim_amaci = 'VNA girişini yüksek güçten korur' }
       Events = @(, @('Kullanımda', '2025-12-15', 'katan', 'VNA tezgâhı', ''))
       Photos = @('Etiket', 'Konnektor')
       Body = @('En fazla 2 W. Konnektörü tork anahtarıyla sık.')
       Journal = @(@{ Date = '2025-12-05'; Kind = 'Kalibrasyon'; Title = 'Zayıflama 20,02 dB, uyumlu'; Body = 'Sertifika K-2025-311, geçerlilik 05.12.2026.'; Person = 'Metroloji lab.' }) },
    @{ Type = 'other'; No = 'JT-7000-0107'; Name = 'Anten tutucu direk (ahşap)'; Sub = ''; Requested = '2026-11-10'; Shape = 'pole'
       Fields = @{ kullanim_amaci = 'Açık alanda anteni 2 m yükseklikte tutar' }
       Events = @(, @('Teslim alındı', '2026-11-18', 'katan', '', ''))
       Photos = @('Kelepce')
       Body = @('Metal parça yok; kelepçe naylon.')
       Journal = @() }
)

foreach ($t in $tools) { Add-Tool $t }
