param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
. (Join-Path $PSScriptRoot '..\_Ortak.ps1')
# Sorumlular (25.11.2026, isteğe bağlı): stok ve yardımcı araçta rol + kişi (Düzenle penceresinde); stok sayfası yalnız
# stokun kendi sorumlularını gösterir (aracınkini değil); araç sayfasında Bilgiler'de "Sorumlular". Projede yok.
[System.Threading.SynchronizationContext]::SetSynchronizationContext((New-Object System.Windows.Threading.DispatcherSynchronizationContext))
$app = New-Object Pusula.Wpf.App; $app.InitializeComponent(); $app.ShutdownMode = 'OnExplicitShutdown'
$win = New-Object Pusula.Wpf.MainWindow
$win.WindowStartupLocation = 'Manual'; $win.Left = -20000; $win.Top = -20000; $win.ShowActivated = $false; $win.Width = 1600; $win.Height = 1000
$win.Show(); Pump
$vm = $win.DataContext; $vm.RootPath = $Data; Wait
$made = & (Join-Path $PSScriptRoot '..\..\tools\Ornek-Araclar.ps1') -Kok $Data -Bin $Bin
$vm.Refresh(); Wait
$stock = Join-Path $Data '7000_MILKON\AB-7000-0003'
$tool = Join-Path $stock '02_Test_Araclari\Kablo\JT-7000-0101'

"--- 1. Stok"
Check 'stok düzenlenir, sorumlu bölümü görünür, başta boş' ($vm.OpenEdit($stock) -and $vm.IsEditResponsibles -and $vm.EditResponsibles.Count -eq 0)
$vm.AddEditResponsible(); $vm.EditResponsibles[0].Role = 'Tasarım sorumlusu'; $vm.EditResponsibles[0].Person = 'Okan Özdemir'
$vm.AddEditResponsible(); $vm.EditResponsibles[1].Role = 'Boş kişi'
$vm.AddEditResponsible(); $vm.EditResponsibles[2].Person = 'katan'
Snap $win "$Out\responsibles-edit.png"
Check 'kaydedildi' ($vm.ConfirmEdit()) $vm.EditError
$info = [IO.File]::ReadAllText([Pusula.Core.Tracking.TrackingFiles]::StockFile($stock))
Check 'stok.json: iki sorumlu (kişisi boş satır atıldı)' ($info -match 'Okan Özdemir' -and $info -match 'katan' -and -not ($info -match 'Boş kişi')) $info
$vm.SelectNodeByPath($stock); Wait
Snap $win "$Out\responsibles-stock.png"
Check 'stok sayfasında: Tasarım sorumlusu: Okan Özdemir · katan' ($vm.HasStockResponsibles -and $vm.StockResponsiblesText -eq 'Tasarım sorumlusu: Okan Özdemir · katan') $vm.StockResponsiblesText

"--- 2. Yardımcı araç"
Check 'araç düzenlenir, sorumlu bölümü görünür' ($vm.OpenEdit($tool) -and $vm.IsEditResponsibles -and $vm.EditResponsibles.Count -eq 0)
$vm.AddEditResponsible(); $vm.EditResponsibles[0].Role = 'Kablo sorumlusu'; $vm.EditResponsibles[0].Person = 'Akşimşek'
Check 'kaydedildi' ($vm.ConfirmEdit()) $vm.EditError
$vm.SelectNodeByPath($tool); Wait
$fact = $vm.Inventory.Facts | Where-Object Label -eq 'Sorumlular' | Select-Object -First 1
Snap $win "$Out\responsibles-tool.png"
Check 'araç sayfasında Sorumlular: Kablo sorumlusu: Akşimşek' ($fact -ne $null -and $fact.Value -eq 'Kablo sorumlusu: Akşimşek') ([string]$fact.Value)
$vm.SelectNodeByPath($stock); Wait
Check 'stok sayfasında aracın sorumlusu görünmez' (-not ($vm.StockResponsiblesText -match 'Akşimşek')) $vm.StockResponsiblesText

"--- 3. Proje, yeniden açma, kaldırma"
Check 'projede sorumlu bölümü yok' ($vm.OpenEdit((Join-Path $Data '7000_MILKON')) -and -not $vm.IsEditResponsibles -and $vm.EditResponsibles.Count -eq 0)
$vm.IsEditOpen = $false
Check 'stok yeniden açılınca iki satır' ($vm.OpenEdit($stock) -and $vm.EditResponsibles.Count -eq 2 -and $vm.EditResponsibles[0].Person -eq 'Okan Özdemir')
$vm.RemoveEditResponsible($vm.EditResponsibles[1]); $vm.RemoveEditResponsible($vm.EditResponsibles[0])
Check 'hepsi kaldırılıp kaydedildi' ($vm.ConfirmEdit())
$vm.SelectNodeByPath($stock); Wait
Check 'stok sayfasında sorumlu satırı gizli' (-not $vm.HasStockResponsibles)
Check 'stok.json: responsibles yok ya da boş' (-not ([IO.File]::ReadAllText([Pusula.Core.Tracking.TrackingFiles]::StockFile($stock)) -match 'Okan'))
$none = New-Object 'System.Collections.Generic.List[Pusula.Core.Tracking.Responsible]'
Check 'olmayan stokta: hata metni, klasör açılmaz (JsonFile.Update)' (([Pusula.Core.Tracking.RecordEdit]::SetResponsibles((Join-Path $Data 'yok'), [Pusula.Core.Tracking.RecordKind]::Stock, $none)).Length -gt 0 -and -not (Test-Path (Join-Path $Data 'yok')))
$win.Close()