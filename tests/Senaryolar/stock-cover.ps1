param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
. (Join-Path $PSScriptRoot '..\_Ortak.ps1')
"--- 1. Not çekirdeği"
$tmp = Join-Path $env:TEMP ('pusula-note-' + [guid]::NewGuid().ToString('N').Substring(0,6)); New-Item -ItemType Directory "$tmp\07_Fotograflar\Montaj" -Force | Out-Null
try {
    $info = New-Object Pusula.Core.Tracking.StockInfo; $info.StockNo = 'AB-1'; $info.Name = 'Deneme'
    $p = [Pusula.Core.Notes.StockCover]::CreateNote($tmp, $info, '7000_X')
    Check 'stok notu: AB-1.md, YAML' ((Split-Path $p -Leaf) -eq 'AB-1.md' -and ([IO.File]::ReadAllText($p) -match 'stok: AB-1') -and ([IO.File]::ReadAllText($p) -match 'proje: 7000_X'))
    [IO.File]::AppendAllText($p, 'gövde')
    [Pusula.Core.Notes.StockCover]::CreateNote($tmp, $info, 'baska') | Out-Null
    Check 'var olan nota dokunulmaz' ([IO.File]::ReadAllText($p) -match 'gövde' -and [IO.File]::ReadAllText($p) -match '7000_X')
    [IO.File]::WriteAllText("$tmp\07_Fotograflar\b.png", 'x'); [IO.File]::WriteAllText("$tmp\07_Fotograflar\Montaj\a.jpg", 'x'); [IO.File]::WriteAllText("$tmp\07_Fotograflar\liste.txt", 'x')
    $c = [Pusula.Core.Notes.StockCover]::Read($tmp, 'AB-1')
    Check 'kapak: not özeti, 2 foto (alt klasör dahil)' ($c.NoteExcerpt -eq 'gövde' -and $c.Photos.Count -eq 2 -and $c.PhotosFolder -like '*07_Fotograflar') ($c.Photos -join ',')
} finally { Remove-Item -Recurse -Force $tmp -ErrorAction SilentlyContinue }

"--- 2. Arayüz (deneme verisi)"
$settingsFile = Join-Path $env:LOCALAPPDATA 'Pusula\ayarlar.json'
$savedSettings = if (Test-Path $settingsFile) { [IO.File]::ReadAllText($settingsFile) } else { $null }
try {
    [System.Threading.SynchronizationContext]::SetSynchronizationContext((New-Object System.Windows.Threading.DispatcherSynchronizationContext))
    $app = New-Object Pusula.Wpf.App; $app.InitializeComponent(); $app.ShutdownMode = 'OnExplicitShutdown'
    $win = New-Object Pusula.Wpf.MainWindow
    $win.WindowStartupLocation = 'Manual'; $win.Left = -20000; $win.Top = -20000; $win.ShowActivated = $false; $win.Width = 1360; $win.Height = 900
    $win.Show(); Pump
    $vm = $win.DataContext; $vm.RootPath = $Data; Pump
    $proj = FindNode $vm.Nodes '7000_MILKON'; $proj.IsExpanded = $true
    foreach ($t in (($vm.Nodes | Where-Object { $_.Kind -eq 'InventoryGroup' }).Children)) { $t.IsExpanded = $true }
    $s = FindNode $vm.Nodes 'AB-7000-0002'; $vm.SelectedNode = $s; Wait
    $cv = $vm.Detail.Cover
    Check 'SH2000: not ve kapak' ($cv.HasNote -and $cv.NoteExcerpt -match '^SH2000' -and $cv.HasCover -and $cv.CoverPhoto -like '*SH2000_kapak.png') $cv.CoverPhoto
    Check 'şerit 4, Fotoğraflar · 5, Dokümanlar · 2' ($cv.Strip.Count -eq 4 -and $cv.PhotosText -eq 'Fotoğraflar · 5' -and $cv.DocumentsText -eq 'Dokümanlar · 2') ($cv.PhotosText + ' ' + $cv.DocumentsText)
    Snap $win "$Out\sh2000-general.png"
    $s3 = FindNode $vm.Nodes 'AB-7000-0003'; $vm.SelectedNode = $s3; Wait
    Check 'notu olmayan stok: Not oluştur' (-not $vm.Detail.Cover.HasNote)
    $jig = @($vm.InventoryTypes | Where-Object Key -eq 'jig')[0]
    $vm.OpenNewInventory($jig); Pump
    Check 'diyalog etiketi Numara' ([Pusula.Wpf.Properties.Resources]::Inv_Number -eq 'Numara')
    Snap $win "$Out\new-jig-dialog.png"
    $vm.IsInventoryDialogOpen = $false
    $win.Close()
} finally { if ($savedSettings -ne $null) { [IO.File]::WriteAllText($settingsFile, $savedSettings) } }
""
"SONUC: $ok basarili, $fail hata"
