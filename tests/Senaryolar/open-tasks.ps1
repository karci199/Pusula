param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
. (Join-Path $PSScriptRoot '..\_Ortak.ps1')
"--- 1. Çekirdek: açık işler"
$T = [Pusula.Core.Notes.OpenTask]
$tmp = Join-Path $env:TEMP ('pusula-gorev-' + [guid]::NewGuid().ToString('N').Substring(0, 6))
New-Item -ItemType Directory "$tmp\Notlar", "$tmp\_ARSIV" | Out-Null
try
{
    $fence = [string]::new([char]96, 3)
    $md = "# Not`n- [ ] SN02 VNA S11 #gorev`n- [x] Bitti`n* [ ] Yıldızlı madde`n" + $fence + "`n- [ ] kod içinde`n" + $fence + "`n  - [ ] Girintili #a #b`n- [] bozuk"
    [IO.File]::WriteAllText("$tmp\Notlar\a.md", $md, (New-Object Text.UTF8Encoding $true))
    [IO.File]::WriteAllText("$tmp\_ARSIV\eski.md", "- [ ] arşivde", (New-Object Text.UTF8Encoding $true))
    $all = @($T::ReadAll($tmp))
    Check '3 açık iş (işaretli, kod içi, bozuk, arşiv hariç)' ($all.Count -eq 3) (($all | ForEach-Object Text) -join ' | ')
    Check 'etiketler atıldı' ($all[0].Text -eq 'SN02 VNA S11' -and $all[2].Text -eq 'Girintili') (($all | ForEach-Object Text) -join ' | ')
    Check 'not adı' ($all[0].NoteName -eq 'a')
}
finally
{
    Remove-Item $tmp -Recurse -Force
}
$settingsFile = Join-Path $env:LOCALAPPDATA 'Pusula\ayarlar.json'
$savedSettings = if (Test-Path $settingsFile) { [IO.File]::ReadAllText($settingsFile) } else { $null }
try {
    [System.Threading.SynchronizationContext]::SetSynchronizationContext((New-Object System.Windows.Threading.DispatcherSynchronizationContext))
    $app = New-Object Pusula.Wpf.App; $app.InitializeComponent(); $app.ShutdownMode = 'OnExplicitShutdown'
    $win = New-Object Pusula.Wpf.MainWindow
    $win.WindowStartupLocation = 'Manual'; $win.Left = -20000; $win.Top = -20000; $win.ShowActivated = $false; $win.Width = 1240; $win.Height = 760
    $win.Show(); Pump
    $vm = $win.DataContext; $vm.RootPath = $Data; Pump
    "--- 2. Stok sayfası"
    $win.Height = 1150; Pump
    $vm.SelectNodeByPath((Join-Path $Data '7000_MILKON\AB-7000-0002')); Wait
    $texts = @($vm.Detail.OpenTasks | ForEach-Object Text)
    Check 'SH2000: 6 açık iş (ana not 4 + karar notu 2)' ($texts.Count -eq 6 -and ($texts -contains 'SN02 VNA S11') -and ($texts -contains 'Antenin arkasına absorban koy')) ($texts -join ' | ')
    Check 'başlık' ($vm.Detail.OpenTasksHeader -eq 'Açık işler · 6') $vm.Detail.OpenTasksHeader
    Check 'işaretli madde yok' (-not ($texts -contains 'ATP Rev B yayımlandı (07.11.2026)'))
    Check 'test notunda kaynak klasör adı' (@($vm.Detail.OpenTasks | Where-Object { $_.NotePath -like '*\test.md' } | Where-Object { $_.Label -ne 'test' }).Count -ge 1) (($vm.Detail.OpenTasks | ForEach-Object Label) -join ',')
    Snap $win "$Out\open-tasks.png"
    "--- 3. Proje: bütün stokların açık işleri"
    $vm.SelectNodeByPath((Join-Path $Data '7000_MILKON')); Wait
    $rows = @($vm.Detail.OpenTasks)
    Check 'projede en az 6 iş (SH2000)' ($rows.Count -ge 6) ([string]$rows.Count)
    Check 'kaynakta stok numarası' (@($rows | Where-Object { $_.Label -like 'AB-7000-0002*' }).Count -ge 6) (($rows | ForEach-Object Label) -join ',')
    Snap $win "$Out\open-tasks-project.png"
    $vm.SelectNodeByPath((Join-Path $Data '7000_MILKON\AB-7000-0001')); Wait
    Check 'notsuz stokta liste boş' (-not $vm.Detail.HasOpenTasks)
} finally {
    if ($savedSettings -ne $null) { [IO.File]::WriteAllText($settingsFile, $savedSettings) }
}
"SONUÇ OK=$ok HATA=$fail"