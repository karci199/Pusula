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
    Check 'kapak: not özeti; numarasız 2 foto gizli' ($c.NoteExcerpt -eq 'gövde' -and $c.Photos.Count -eq 0 -and $c.HiddenCount -eq 2 -and $c.PhotosFolder -like '*07_Fotograflar') ([string]$c.HiddenCount)
} finally { Remove-Item -Recurse -Force $tmp -ErrorAction SilentlyContinue }

"--- 1b. 01_Ortak\Fotograflar: görünüş sırası ve adları"
$tmp = Join-Path $env:TEMP ('pusula-foto-' + [guid]::NewGuid().ToString('N').Substring(0,6)); New-Item -ItemType Directory "$tmp\01_Ortak" -Force | Out-Null
try {
    $c = [Pusula.Core.Notes.StockCover]::Read($tmp, 'AB-9')
    Check 'fotoğraf yok: yer 01_Ortak\Fotograflar' ($c.Photos.Count -eq 0 -and $c.PhotosHome -eq (Join-Path $tmp '01_Ortak\Fotograflar')) $c.PhotosHome
    New-Item -ItemType Directory "$tmp\01_Ortak\Fotograflar\Diger" -Force | Out-Null
    foreach ($n in 'IMG_2041.jpg', 'AB-9_on.jpg', '2_montaj_detayi.png', '10_ic yapi.jpg', '1_on gorunus.jpg', 'AB-9_kapak.jpg', '3-konnektor.jpg', '4.etiket.jpg', '5_arka.jpg', '12345.jpg', '1x.jpg') { [IO.File]::WriteAllText("$tmp\01_Ortak\Fotograflar\$n", 'x') }
    [IO.File]::WriteAllText("$tmp\01_Ortak\Fotograflar\Diger\6_alt klasor.jpg", 'x')
    [IO.File]::WriteAllText("$tmp\01_Ortak\Cizim.png", 'x')
    $c = [Pusula.Core.Notes.StockCover]::Read($tmp, 'AB-9')
    $strip = @($c.Strip | ForEach-Object { Split-Path $_ -Leaf }) -join ','
    Check 'kapak: AB-9_kapak' ((Split-Path $c.Cover -Leaf) -eq 'AB-9_kapak.jpg') $c.Cover
    Check 'şerit: numaralılar sayı sırasıyla (alt klasör dahil)' ($strip -eq '1_on gorunus.jpg,2_montaj_detayi.png,3-konnektor.jpg,4.etiket.jpg,5_arka.jpg,6_alt klasor.jpg,10_ic yapi.jpg,12345.jpg') $strip
    Check 'numarasızlar gizli: IMG, eski _on, 1x' ($c.HiddenCount -eq 3 -and $c.Photos.Count -eq 9) ([string]$c.HiddenCount)
    Check 'Ortak''taki çizim fotoğraf değil' ($c.PhotosFolder -like '*\01_Ortak\Fotograflar' -and $strip -notmatch 'Cizim')
    $labels = @($c.Strip | Select-Object -First 3 | ForEach-Object { [Pusula.Core.Notes.StockCover]::StripLabelOf($_) }) -join ','
    Check 'şerit adları: numaradan sonrası' ($labels -eq 'on gorunus,montaj detayi,konnektor') $labels
    $cv = New-Object Pusula.Wpf.ViewModels.StockCoverViewModel; $cv.Show($c, (Join-Path $tmp 'AB-9'))
    Check 'şerit: 4 + "+4", büyükte kapak, gizli sayısı' ($cv.Thumbs.Count -eq 5 -and $cv.Thumbs[4].IsMore -and $cv.Thumbs[4].Label -eq '+4' -and -not $cv.Thumbs[0].IsSelected -and $cv.SelectedPhoto -like '*AB-9_kapak.jpg' -and $cv.SelectedLabel -eq 'Kapak' -and $cv.PhotosText -eq 'Fotoğraflar · 9' -and $cv.HiddenText -eq '3 fotoğraf daha klasörde') ($cv.PhotosText + ' | ' + $cv.HiddenText)
    $cv.Select($cv.Thumbs[1])
    Check 'şeritten seçilen büyükte' ($cv.SelectedPhoto -like '*2_montaj_detayi.png' -and $cv.SelectedLabel -eq 'montaj detayi' -and $cv.Thumbs[1].IsSelected)
    $cv.Select($cv.Thumbs[1])
    Check 'seçiliye yeniden basınca kapağa döner' ($cv.SelectedPhoto -like '*AB-9_kapak.jpg' -and -not $cv.Thumbs[1].IsSelected)
    Remove-Item "$tmp\01_Ortak\Fotograflar\AB-9_kapak.jpg"
    $c = [Pusula.Core.Notes.StockCover]::Read($tmp, 'AB-9')
    $cv.Show($c, (Join-Path $tmp 'AB-9'))
    Check 'kapak yoksa genel resim ve ipucu' ($c.Cover -eq '' -and -not $cv.HasSelectedPhoto -and $cv.NoCoverText -like '*AB-9_kapak.jpg*' -and $cv.Thumbs.Count -eq 5) $cv.NoCoverText} finally { Remove-Item -Recurse -Force $tmp -ErrorAction SilentlyContinue }

"--- 1c. Şablon: yeni stokta 01_Ortak\Fotograflar"
$ts = [Pusula.Core.Templates.TemplateSet]::CreateDefault()
$common = $ts.Modules | Where-Object Key -eq 'common'
Check 'Ortak modülünde Fotograflar' (@($common.AllFolders()) -contains '01_Ortak\Fotograflar') (@($common.AllFolders()) -join ',')
$common.SubFolders.Clear(); $ts.SchemaVersion = 4; $ts.Normalize()
Check 'sürüm 4 → 6: Fotograflar eklendi (Dokumanlar, Girdiler de)' ($ts.SchemaVersion -eq 6 -and @($common.SubFolders) -contains 'Fotograflar')

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
    $ph = Join-Path $Data '7000_MILKON\AB-7000-0002\07_Fotograflar'
    Copy-Item (Join-Path $ph 'SH2000_konnektor.png') (Join-Path $ph '1_konnektor.png'); Copy-Item (Join-Path $ph 'SH2000_etiket.png') (Join-Path $ph '2_etiket.png')
    $s = FindNode $vm.Nodes 'AB-7000-0002'; $vm.SelectedNode = $s; Wait
    $cv = $vm.Detail.Cover
    Check 'SH2000: not ve kapak' ($cv.HasNote -and $cv.NoteExcerpt -match '^SH2000' -and $cv.HasCover -and $cv.CoverPhoto -like '*SH2000_kapak.png') $cv.CoverPhoto
    Check 'şerit: 2 numaralı, 4 gizli, Fotoğraflar · 3, Dokümanlar · 2' ($cv.Thumbs.Count -eq 2 -and $cv.Thumbs[0].Label -eq 'konnektor' -and $cv.PhotosText -eq 'Fotoğraflar · 3' -and $cv.HiddenText -eq '4 fotoğraf daha klasörde' -and $cv.DocumentsText -eq 'Dokümanlar · 2') ($cv.PhotosText + ' ' + $cv.HiddenText + ' ' + $cv.DocumentsText)
    Check 'eski stok: 📁 07_Fotograflar' ($cv.PhotosHome -like '*\07_Fotograflar') $cv.PhotosHome
    Snap $win "$Out\sh2000-general.png"
    $s3 = FindNode $vm.Nodes 'AB-7000-0003'; $vm.SelectedNode = $s3; Wait
    Check 'notu olmayan stok: Not oluştur' (-not $vm.Detail.Cover.HasNote)
    $home3 = $vm.Detail.Cover.PhotosHome
    Check 'fotoğrafsız stok: genel resim, 📁 klasörü boş açılır' (-not $vm.Detail.Cover.HasCover -and -not $vm.Detail.Cover.HasSelectedPhoto -and $home3 -like '*AB-7000-0003\01_Ortak\Fotograflar' -and $vm.EnsureStockPhotosFolder() -eq $home3 -and (Test-Path $home3)) $home3
    $vm.IsTimelineDone = $true; Wait
    Snap $win "$Out\photos-empty.png"
    $jig = @($vm.InventoryTypes | Where-Object Key -eq 'jig')[0]
    $vm.OpenNewInventory($jig); Pump
    Check 'diyalog etiketi Numara' ([Pusula.Wpf.Properties.Resources]::Inv_Number -eq 'Numara')
    Snap $win "$Out\new-jig-dialog.png"
    $vm.IsInventoryDialogOpen = $false
    $win.Close()
} finally { if ($savedSettings -ne $null) { [IO.File]::WriteAllText($settingsFile, $savedSettings) } }
""
"SONUC: $ok basarili, $fail hata"
