param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
. (Join-Path $PSScriptRoot '..\_Ortak.ps1')
$readStock = [Pusula.Core.Tracking.JsonFile].GetMethod('TryRead').MakeGenericMethod([Pusula.Core.Tracking.StockInfo])
$settingsFile = Join-Path $env:LOCALAPPDATA 'Pusula\ayarlar.json'
$savedSettings = if (Test-Path $settingsFile) { [IO.File]::ReadAllText($settingsFile) } else { $null }
$root = Join-Path $env:TEMP ('pusula-qn-' + [guid]::NewGuid().ToString('N').Substring(0,6))
New-Item -ItemType Directory "$root\7000_X\AB-7000-0001" -Force | Out-Null
[Pusula.Core.Tracking.ProjectService]::MarkAsProject("$root\7000_X", '7000', 'X') | Out-Null
[Pusula.Core.Tracking.ProjectService]::MarkAsStock("$root\7000_X\AB-7000-0001", 'AB-7000-0001', '', 'Hazırlık') | Out-Null
$types = [Pusula.Core.Types.TypeStore]::LoadOrCreate($root)
$jr = [Pusula.Core.Inventory.InventoryService]::Create($root, $types.Find('jig'), 'General', '', 'JIG-1000-0001', 'Deneme', '', '', 'k')
$QT = [Pusula.Wpf.ViewModels.QuickNoteTarget]
try {
    [System.Threading.SynchronizationContext]::SetSynchronizationContext((New-Object System.Windows.Threading.DispatcherSynchronizationContext))
    $app = New-Object Pusula.Wpf.App; $app.InitializeComponent(); $app.ShutdownMode = 'OnExplicitShutdown'
    $win = New-Object Pusula.Wpf.MainWindow
    $win.WindowStartupLocation = 'Manual'; $win.Left = -20000; $win.Top = -20000; $win.ShowActivated = $false; $win.Width = 1360; $win.Height = 900
    $win.Show(); Pump
    $vm = $win.DataContext; $vm.RootPath = $root; Pump
    "--- 1. Stok"
    $stock = FindNode $vm.Nodes 'AB-7000-0001'; $vm.SelectedNode = $stock; Wait
    Check 'boş not yazılmaz' (-not $vm.AddQuickNoteHere($QT::Stock, '   '))
    Check 'stoka not' ($vm.AddQuickNoteHere($QT::Stock, 'Numune geldi, yarın ölçülecek')); Wait
    $info = $readStock.Invoke($null, @("$root\7000_X\AB-7000-0001\_TAKIP\stok.json"))
    Check 'stok.json: not, kişi, durum korunur' ($info.Notes.Count -eq 1 -and $info.Notes[0].Person -eq [Environment]::UserName -and $info.Status -eq 'Hazırlık')
    Check 'zaman çizelgesinde' ($vm.Detail.Timeline.Count -eq 1 -and $vm.Detail.Timeline[0].IsQuickNote -and $vm.Detail.Timeline[0].Summary -match 'Numune') 
    Check 'koşu yok sayılır (KPI gizli)' (-not $vm.Detail.HasRuns)
    "--- 2. Proje"
    $proj = FindNode $vm.Nodes '7000_X'; $vm.SelectedNode = $proj; Wait
    Check 'projeye not' ($vm.AddQuickNoteHere($QT::Project, 'Müşteri toplantısı 12.11')); Wait
    Check 'proje notları listede' ($vm.Detail.ProjectNotes.Count -eq 1 -and $vm.Detail.ProjectNotes[0].Detail -eq 'Müşteri toplantısı 12.11')
    $vm.OpenQuickNote($QT::Stock, "$root\7000_X\AB-7000-0001", 'AB-7000-0001')
    Check 'tablodan diyalog' ($vm.IsQuickNoteOpen -and $vm.QuickNoteTitle -eq 'Not ekle · AB-7000-0001')
    $vm.QuickNoteText = 'Tablodan not'; $vm.ConfirmQuickNote(); $vm.IsQuickNoteOpen = $false; Wait
    $info = $readStock.Invoke($null, @("$root\7000_X\AB-7000-0001\_TAKIP\stok.json"))
    Check 'tablodan yazılan stoka gitti' ($info.Notes.Count -eq 2)
    $vm.SelectNodeByPath("$root\7000_X\AB-7000-0001"); Wait
    Check 'tablodan stoka git' ($vm.SelectedNode.Title -eq 'AB-7000-0001')
    "--- 3. Jig"
    $vm.ShowInventory($jr.Folder); Wait
    Check 'kayda not' ($vm.AddQuickNoteHere($QT::Record, 'Rev AC çizimi atölyeye verildi')); Wait
    Check 'geçmişte en üstte Not' ($vm.Inventory.IsRecord -and $vm.Inventory.History[0].Status.Name -eq 'Not' -and $vm.Inventory.History[0].Detail -match 'Rev AC' -and $vm.Inventory.History.Count -eq 2)
    Check 'durum değişmedi' ($vm.Inventory.Status.Name -eq 'Talep')
    $win.Close()
} finally {
    if ($savedSettings -ne $null) { [IO.File]::WriteAllText($settingsFile, $savedSettings) }
    Remove-Item -Recurse -Force $root -ErrorAction SilentlyContinue
}
""
"SONUC: $ok basarili, $fail hata"
