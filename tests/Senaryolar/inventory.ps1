param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
. (Join-Path $PSScriptRoot '..\_Ortak.ps1')
$root = Join-Path $env:TEMP ('pusula-inv-' + [guid]::NewGuid().ToString('N').Substring(0,6))
New-Item -ItemType Directory $root | Out-Null
try {
    "--- 1. Tür kalıbı"
    $types = [Pusula.Core.Types.TypeStore]::LoadOrCreate($root)
    $cable = $types.Find('cable'); $jig = $types.Find('jig')
    Check 'kablo ve jig envanter türü' ($cable.IsInventory -and $jig.IsInventory -and @($types.InventoryTypes).Count -eq 2)
    Check 'kablo biçimi JT' ($cable.AcceptsNumber('JT-0001-0002') -and -not $cable.AcceptsNumber('JIG-0001-0002'))
    Check 'durumlar' (($cable.Statuses | ForEach-Object Name) -join ',' -eq 'Talep,Hazırlanıyor,Teslim alındı,Kullanımda,Üretime teslim edildi')
    Check 'sıra dışı durumlar' (($cable.SideStatuses | ForEach-Object Name) -join ',' -eq 'Onarım / değişim gerekiyor,Çoğaltma isteniyor')
    Check 'ikon ve renk' ($cable.Icon -eq 'E703' -and $jig.Color -eq '#B4A7F5')

    "--- 2. Eski dosya çevirme"
    $oldRoot = "$root\eski"; New-Item -ItemType Directory "$oldRoot\_PUSULA" -Force | Out-Null
    [IO.File]::WriteAllText("$oldRoot\_PUSULA\turler.json", '{"schemaVersion":1,"types":[{"key":"stock","name":"Stok","numberFormats":[{"pattern":"@@-####-####","label":"Stok numarası"}],"subTypes":[],"statuses":[{"name":"Hazırlık","tone":"neutral"}],"sideStatuses":[]},{"key":"cable","name":"Kablo","folder":"02_ENVANTER\\Kablolar","numberFormats":[{"pattern":"KBL-####-####","label":"Kablo numarası"}],"subTypes":[],"statuses":[{"name":"Sipariş","tone":"neutral"}],"sideStatuses":[]}],"tags":[]}')
    $old = [Pusula.Core.Types.TypeStore]::LoadOrCreate($oldRoot)
    Check 'eski kablo tanımı yenisiyle değişti' ($old.Find('cable').AcceptsNumber('JT-0001-0001') -and $old.Find('cable').InitialStatus -eq 'Talep')
    Check 'stok tanımı korundu, ikon eklendi' ($old.Stock.Statuses.Count -eq 1 -and $old.Stock.Icon -eq 'E7B8')

    "--- 3. Kayıt açma"
    $svc = [Pusula.Core.Inventory.InventoryService]
    $r = $svc::Create($root, $cable, 'General', '', 'jt-0001-0002', 'RS422 test kablosu', 'Haberleşme', 'ekranlı · MOXA pin', 'katan')
    Check 'genel düzeyde açıldı' ($r.Succeeded -and $r.Folder -like '*\ENVANTER\Kablolar\JT-0001-0002_RS422_test_kablosu') $r.Folder
    Check '_TAKIP gizli' (((Get-Item -Force "$($r.Folder)\_TAKIP").Attributes -band [IO.FileAttributes]::Hidden) -ne 0)
    Check 'aynı numara reddedilir' ($svc::Create($root, $cable, 'General', '', 'JT-0001-0002', 'X', 'RF', '', 'katan').Error -eq 'AlreadyExists')
    Check 'biçim dışı reddedilir' ($svc::Create($root, $cable, 'General', '', 'KBL-1', 'X', 'RF', '', 'katan').Error -eq 'NumberFormat')
    Check 'tanım boş reddedilir' ($svc::Create($root, $cable, 'General', '', 'JT-0001-0003', '', 'RF', '', 'katan').Error -eq 'NameEmpty')

    New-Item -ItemType Directory "$root\7000_MILKON\AB-7000-0003" -Force | Out-Null
    [Pusula.Core.Tracking.ProjectService]::MarkAsProject("$root\7000_MILKON", '7000', 'MILKON') | Out-Null
    [Pusula.Core.Tracking.ProjectService]::MarkAsStock("$root\7000_MILKON\AB-7000-0003", 'AB-7000-0003', '', 'Hazırlık') | Out-Null
    $p = $svc::Create($root, $cable, 'Project', "$root\7000_MILKON", 'JT-0001-0010', 'MILKON ortak RF kablosu', 'RF', '', 'katan')
    $s = $svc::Create($root, $jig, 'Stock', "$root\7000_MILKON\AB-7000-0003", 'JIG-1000-0014', 'Döner tabla montaj aparatı', 'Montaj', '', 'katan')
    Check 'proje düzeyi klasörü' ($p.Folder -like '*\7000_MILKON\00_Proje_Geneli\Envanter\Kablolar\JT-0001-0010_*') $p.Folder
    Check 'stok düzeyi klasörü' ($s.Folder -like '*\AB-7000-0003\05_Jigler\JIG-1000-0014_*') $s.Folder

    "--- 4. Olaylar"
    $e1 = [Pusula.Core.Inventory.InventoryEvent]::Create('Hazırlanıyor', [datetime]'2026-09-05', 'M. Yılmaz', '', 'kablo atölyesi')
    $e2 = [Pusula.Core.Inventory.InventoryEvent]::Create('Teslim alındı', [datetime]'2026-09-18', 'katan', 'Lab-A raf 3', '')
    $e3 = [Pusula.Core.Inventory.InventoryEvent]::Create('Kullanımda', [datetime]'2026-09-20', 'katan', '', '')
    Check 'olaylar eklendi' ($svc::AddEvent($r.Folder, $e1) -and $svc::AddEvent($r.Folder, $e2) -and $svc::AddEvent($r.Folder, $e3))
    $all = $svc::ReadAll($root, $types.InventoryTypes)
    Check 'üç düzeyde 3 kayıt' ($all.Count -eq 3) ("sayi=" + $all.Count)
    $k = $all | Where-Object { $_.Record.Number -eq 'JT-0001-0002' }
    Check 'son durum Kullanımda' ($k.Record.CurrentStatus -eq 'Kullanımda')
    Check 'konum son yazılandan' ($k.Record.Location -eq 'Lab-A raf 3')
    Check 'yapan: Hazırlanıyor olayından' ($k.Record.FirstEventOf('Hazırlanıyor').Person -eq 'M. Yılmaz')
    Check 'talep olayı kendiliğinden' ($k.Record.FirstEventOf('Talep').Person -eq 'katan')
    Check 'düzey ve sahip' ((($all | Where-Object { $_.Record.Number -eq 'JIG-1000-0014' }).Owner -eq 'AB-7000-0003') -and (($all | Where-Object { $_.Record.Number -eq 'JT-0001-0010' }).Level -eq 'Project'))
    Check 'olmayan kayda olay eklenmez' (-not $svc::AddEvent("$root\yok", $e1))
}
finally { Remove-Item -Recurse -Force $root -ErrorAction SilentlyContinue }
""
"SONUC: $ok basarili, $fail hata"
