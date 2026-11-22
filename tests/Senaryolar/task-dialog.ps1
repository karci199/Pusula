param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
. (Join-Path $PSScriptRoot '..\_Ortak.ps1')
# Yapılacak iş penceresi: iş, işlem, açıklama, kişiler, son tarih kısayolu, ilgili madde, etiket; nota satır + girintili açıklama; kartta görünür.
$settingsFile = Join-Path $env:LOCALAPPDATA 'Pusula\ayarlar.json'
$savedSettings = if (Test-Path $settingsFile) { [IO.File]::ReadAllText($settingsFile) } else { $null }
try {
    [System.Threading.SynchronizationContext]::SetSynchronizationContext((New-Object System.Windows.Threading.DispatcherSynchronizationContext))
    $app = New-Object Pusula.Wpf.App; $app.InitializeComponent(); $app.ShutdownMode = 'OnExplicitShutdown'
    $win = New-Object Pusula.Wpf.MainWindow
    $win.WindowStartupLocation = 'Manual'; $win.Left = -20000; $win.Top = -20000; $win.ShowActivated = $false; $win.Width = 1240; $win.Height = 860
    $win.Show(); Pump
    $vm = $win.DataContext; $vm.RootPath = $Data; Pump
    $stock = Join-Path $Data '7000_MILKON\AB-7000-0002'
    $note = Join-Path $stock 'AB-7000-0002.md'
    # Bağlanacak bir doğrulama maddesi
    $item = New-Object Pusula.Core.Testing.VerificationItem
    $item.TgNo = 'DGÖ-1'; $item.Text = 'Yanca hüzme genişliği'; $item.Group = 'İşlevsel'
    [void][Pusula.Core.Testing.VerificationList]::Add($stock, $item, '')
    $vm.SelectNodeByPath($stock); Wait; Wait

    "--- Pencere"
    Check 'pencere açılır' ($vm.OpenTaskDialogHere() -and $vm.IsTaskDialogOpen)
    $me = $vm.TaskPeople.TrimStart('@')
    Check 'kişi: kullanıcı ekli gelir' ($me.Length -gt 0 -and -not $me.Contains(' ')) $vm.TaskPeople
    Check 'ilgili madde: Yok ve DGÖ-1' ($vm.TaskItemOption.Text -eq '— Yok' -and @($vm.TaskItemOptions | Where-Object { $_.Key -eq 'DGÖ-1' }).Count -eq 1) (@($vm.TaskItemOptions | ForEach-Object Text) -join ',')
    Check 'iş boşken Ekle yok' ($vm.TaskPrimaryText -eq '')
    $vm.TaskTitle = 'Raporu tasarımcıya gönder'; $vm.TaskKind = 'Hazırlık'; $vm.TaskBody = "SATIMO sonuçlarıyla birlikte.`r`nGeçti-kaldı tablosu ekli olsun."
    $vm.TaskPeople = $vm.TaskPeople + ' @Ali_Demirci'; $vm.TaskTags = '#SATIMO'
    $next = @($vm.TaskDueChoices)[3]; $next.IsChecked = $true
    Check 'Gelecek hafta: tarih yazılır' ($vm.TaskDueText -eq (Get-Date).Date.AddDays(7).ToString('dd.MM.yyyy')) $vm.TaskDueText
    $vm.TaskItemOption = ($vm.TaskItemOptions | Where-Object { $_.Key -eq 'DGÖ-1' })
    Snap $win "$Out\task-dialog.png"
    Check 'iş eklenir' ($vm.ConfirmTaskDialog()); Wait; Wait
    $txt = [IO.File]::ReadAllText($note)
    $due = (Get-Date).Date.AddDays(7).ToString('yyyy-MM-dd')
    Check 'nota satır' ($txt.Contains('- [ ] Hazırlık: Raporu tasarımcıya gönder @' + $me + ' @Ali_Demirci #SATIMO [[DGÖ-1]]') -and $txt.Contains($due))
    Check 'altında girintili açıklama' ($txt.Contains("`n    SATIMO sonuçlarıyla birlikte.") -and $txt.Contains("`n    Geçti-kaldı tablosu ekli olsun."))

    "--- Kart"
    $vm.IsRangeAll = $true; Wait
    $card = @($vm.StockDays | ForEach-Object { $_.Cards }) | Where-Object { $_.IsPlanned -and $_.Title -eq 'Raporu tasarımcıya gönder' } | Select-Object -First 1
    $tags = @($card.Tags | ForEach-Object Text)
    Check 'kart: Hazırlık, açıklama, iki kişi, etiket, madde bağı' ($card -and $card.KindLabel -eq 'Hazırlık' -and $card.FullDescription.Contains('Geçti-kaldı') -and ($tags -contains '@Ali_Demirci') -and ($tags -contains '#SATIMO') -and (@($tags | Where-Object { $_ -like '*DGÖ-1' }).Count -eq 1)) ($tags -join ',')
    $task = [Pusula.Core.Notes.OpenTask]::Read($note) | Where-Object { $_.Text -like '*tasarımcıya*' } | Select-Object -First 1
    Check 'okunur: sorumlu ilk kişi, bağ DGÖ-1' ($task.PersonName -eq $me.Replace('_', ' ') -and $task.LinkTarget -eq 'DGÖ-1' -and $task.People.Count -eq 2) ($task.PersonName + ' / ' + $task.LinkTarget)
    Snap $win "$Out\task-card.png"

    "--- Ana sayfadan + İş"
    $vm.SelectedNode = $null; $vm.LoadHome(); Wait; Wait; $vm.HomeTab = 1; Wait
    Check 'pencere stok seçimiyle açılır' ($vm.OpenTaskDialogHome() -and $vm.IsTaskDialogOpen -and $vm.IsTaskStockPick -and $vm.TaskStockOptions.Count -ge 2) ($vm.TaskStockOptions.Count)
    $vm.TaskTitle = 'Ana sayfadan iş'
    Check 'stok seçilmeden Ekle yok' ($vm.TaskPrimaryText -eq '' -and -not $vm.ConfirmTaskDialog())
    $opt = $vm.TaskStockOptions | Where-Object { $_.Key -eq $stock } | Select-Object -First 1
    Check 'seçenekte stok no ve ad' ($opt -ne $null -and $opt.Text -like 'AB-7000-0002*') $opt.Text
    $vm.TaskStockOption = $opt
    Check 'stok seçilince madde listesi o stokun (DGÖ-1)' ($vm.TaskPrimaryText -ne '' -and @($vm.TaskItemOptions | Where-Object { $_.Key -eq 'DGÖ-1' }).Count -eq 1 -and $vm.TaskTarget -like 'AB-7000-0002*')
    $vm.TaskStockOption = $null
    Check 'boş değer seçimi silmez' ($vm.TaskStockOption.Key -eq $stock)
    $vm.TaskDueText = (Get-Date).ToString('dd.MM.yyyy')
    # Pencere görüntüsü: açık ContentDialog'un açılır katmanı ayrıca çizilir (Snap onu yakalamaz).
    function Find-Open($node) { if ($node -eq $null) { return $null }; if ($node -is [DesignKit.Controls.ContentDialog] -and $node.IsOpen) { return $node }; for ($i = 0; $i -lt [System.Windows.Media.VisualTreeHelper]::GetChildrenCount($node); $i++) { $r = Find-Open ([System.Windows.Media.VisualTreeHelper]::GetChild($node, $i)); if ($r) { return $r } }; return $null }
    function Find-Popup($node) { if ($node -is [System.Windows.Controls.Primitives.Popup]) { return $node }; for ($i = 0; $i -lt [System.Windows.Media.VisualTreeHelper]::GetChildrenCount($node); $i++) { $r = Find-Popup ([System.Windows.Media.VisualTreeHelper]::GetChild($node, $i)); if ($r) { return $r } }; return $null }
    for ($k = 0; $k -lt 20; $k++) { Pump; Start-Sleep -Milliseconds 30 }
    $dlg = Find-Open $win
    if ($dlg) {
        $pop = Find-Popup $dlg; if ($pop -and $pop.Child) { $dlg = $pop.Child; $sz = New-Object System.Windows.Size($win.ActualWidth, $win.ActualHeight); $dlg.Measure($sz); $dlg.Arrange((New-Object System.Windows.Rect($sz))); $dlg.UpdateLayout(); for ($k = 0; $k -lt 20; $k++) { Pump; Start-Sleep -Milliseconds 30 } }
        $bmp = New-Object System.Windows.Media.Imaging.RenderTargetBitmap([int]$dlg.ActualWidth, [int]$dlg.ActualHeight, 96, 96, [System.Windows.Media.PixelFormats]::Pbgra32)
        $bmp.Render($dlg); $enc = New-Object System.Windows.Media.Imaging.PngBitmapEncoder; $enc.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($bmp))
        $fs = [IO.File]::Create("$Out\task-home-dialog.png"); $enc.Save($fs); $fs.Close()
    }
    Check 'iş seçilen stokun notuna' ($vm.ConfirmTaskDialog() -and [IO.File]::ReadAllText($note).Contains('- [ ] Ana sayfadan iş')); Wait; Wait
    $vm.LoadHome(); Wait; Wait
    Check 'ana sayfa listesinde görünür' (@($vm.HomeTasks | Where-Object { $_.Task.Text -like '*Ana sayfadan iş*' }).Count -eq 1) (@($vm.HomeTasks | ForEach-Object { $_.Task.Text }) -join ' | ')
    Snap $win "$Out\task-home.png"
    $vm.SelectNodeByPath($stock) | Out-Null; Wait; Wait
    Check 'stok sayfası: seçim gizli' ($vm.OpenTaskDialogHere() -and -not $vm.IsTaskStockPick); $vm.IsTaskDialogOpen = $false
} finally {
    if ($savedSettings -ne $null) { [IO.File]::WriteAllText($settingsFile, $savedSettings) }
}
"SONUÇ OK=$ok HATA=$fail"