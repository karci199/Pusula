param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
. (Join-Path $PSScriptRoot '..\_Ortak.ps1')
$RC = [Pusula.Core.Inventory.RecordContents]

"--- 1. Çekirdek"
Check 'etiket: 01_Cizimler → Cizimler' ($RC::LabelOf('01_Cizimler') -eq 'Cizimler')
Check 'etiket: Teknik_Resim' ($RC::LabelOf('Teknik_Resim') -eq 'Teknik Resim')
Check 'rev: AB < AC' ($RC::LatestRevision([string[]]@('p_RevAB.step','p_RevAC.pdf','x.txt')) -eq 'AC')
Check 'rev: Z < AA' ($RC::LatestRevision([string[]]@('p_RevZ.step','p_RevAA.step')) -eq 'AA')
Check 'rev: sayı 2 < 10' ($RC::LatestRevision([string[]]@('a_Rev2.dwg','a_Rev10.dwg')) -eq '10')
Check 'rev: "Review" sayılmaz' ($RC::LatestRevision([string[]]@('Review_notlari.pdf')) -eq '')
$tmp = Join-Path $env:TEMP ('pusula-rec-' + [guid]::NewGuid().ToString('N').Substring(0,6))
try {
    $types = [Pusula.Core.Types.TypeStore]::LoadOrCreate($tmp)
    $r = [Pusula.Core.Inventory.InventoryService]::Create($tmp, $types.Find('jig'), 'General', '', 'JIG-1000-0001', 'Deneme aparatı', 'Mekanik', '', 'katan')
    Check 'yeni kayıtta not var' (Test-Path "$($r.Folder)\JIG-1000-0001.md")
    $text = [IO.File]::ReadAllText("$($r.Folder)\JIG-1000-0001.md")
    Check 'not YAML: numara, tanım' ($text -match 'numara: JIG-1000-0001' -and $text -match 'tanim: Deneme aparatı')
    [IO.File]::AppendAllText("$($r.Folder)\JIG-1000-0001.md", 'elle yazılan satır')
    $rec = New-Object Pusula.Core.Inventory.InventoryRecord; $rec.Number = 'JIG-1000-0001'; $rec.Name = 'baska'
    [Pusula.Core.Inventory.InventoryService]::CreateNote($r.Folder, $rec) | Out-Null
    Check 'var olan nota dokunulmaz' ([IO.File]::ReadAllText("$($r.Folder)\JIG-1000-0001.md") -match 'elle yazılan satır')
    $c = $RC::Read($r.Folder, 'JIG-1000-0001')
    Check 'boş kayıt: 3 klasör, hepsi 0' ($c.Folders.Count -eq 3 -and ($c.Folders | Where-Object FileCount -gt 0).Count -eq 0)
    Check 'özet: başlık atlanır' ($c.NoteExcerpt -eq 'elle yazılan satır') $c.NoteExcerpt
} finally { Remove-Item -Recurse -Force $tmp -ErrorAction SilentlyContinue }

"--- 2. Örnekler (deneme verisi)"
$root = $Data
$settingsFile = Join-Path $env:LOCALAPPDATA 'Pusula\ayarlar.json'
$savedSettings = if (Test-Path $settingsFile) { [IO.File]::ReadAllText($settingsFile) } else { $null }
try {
    [System.Threading.SynchronizationContext]::SetSynchronizationContext((New-Object System.Windows.Threading.DispatcherSynchronizationContext))
    $app = New-Object Pusula.Wpf.App; $app.InitializeComponent(); $app.ShutdownMode = 'OnExplicitShutdown'
    $win = New-Object Pusula.Wpf.MainWindow
    $win.WindowStartupLocation = 'Manual'; $win.Left = -20000; $win.Top = -20000; $win.ShowActivated = $false; $win.Width = 1360; $win.Height = 900
    $win.Show(); Pump
    $vm = $win.DataContext; $vm.RootPath = $root; Pump
    $folders = @{}; foreach ($i in [Pusula.Core.Inventory.InventoryService]::ReadAll($root, $vm.InventoryTypes)) { $folders[$i.Record.Number] = $i.Folder }
    function Show([string]$n) { $vm.ShowInventory($folders[$n]); Wait }
    function Tiles { ($vm.Inventory.FolderTiles | ForEach-Object { $_.Label + '=' + $_.Value }) -join ', ' }

    Show 'JIG-1000-0009'
    Check 'A: kutucuklar' ((Tiles) -eq 'Cizimler=3 · Rev AC, Belgeler=1, Fotograflar=5') (Tiles)
    Check 'A: 4 foto + "+1"' ($vm.Inventory.Photos.Count -eq 4 -and $vm.Inventory.MorePhotos -eq '+1')
    Check 'A: not özeti' ($vm.Inventory.HasNote -and $vm.Inventory.NoteExcerpt -match '^Montaj delikleri') $vm.Inventory.NoteExcerpt
    Snap $win "$Out\rec-A.png"
    Show 'JT-0001-0001'
    Check 'B: belgeler boş, 2 foto, not yok' ((Tiles) -eq 'Belgeler=boş, Fotograflar=2') (Tiles)
    Snap $win "$Out\rec-B.png"
    Show 'JIG-1000-0020'
    Check 'C: hepsi boş, not boş' ((Tiles) -eq 'Cizimler=boş, Belgeler=boş, Fotograflar=boş' -and $vm.Inventory.NoteExcerpt -eq 'Not henüz boş.') ((Tiles) + ' | ' + $vm.Inventory.NoteExcerpt)
    Snap $win "$Out\rec-C.png"
    Show 'JT-0001-0002'
    Check 'D: belgeler Rev B, not yok' ((Tiles) -eq 'Belgeler=3 · Rev B, Fotograflar=1') (Tiles)
    Snap $win "$Out\rec-D.png"
    Show 'JIG-1000-0014'
    Check 'E: kendi klasörü, kökteki foto' ((Tiles) -eq 'Teknik Resim=2 · Rev 2' -and $vm.Inventory.Photos.Count -eq 1 -and $vm.Inventory.HasNote) (Tiles)
    Snap $win "$Out\rec-E.png"
    $win.Close()
} finally { if ($savedSettings -ne $null) { [IO.File]::WriteAllText($settingsFile, $savedSettings) } }
""
"SONUC: $ok basarili, $fail hata"
