param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
. (Join-Path $PSScriptRoot '..\_Ortak.ps1')
$settingsFile = Join-Path $env:LOCALAPPDATA 'Pusula\ayarlar.json'
$savedSettings = if (Test-Path $settingsFile) { [IO.File]::ReadAllText($settingsFile) } else { $null }
try {
    [System.Threading.SynchronizationContext]::SetSynchronizationContext((New-Object System.Windows.Threading.DispatcherSynchronizationContext))
    $app = New-Object Pusula.Wpf.App; $app.InitializeComponent(); $app.ShutdownMode = 'OnExplicitShutdown'
    $win = New-Object Pusula.Wpf.MainWindow
    $win.WindowStartupLocation = 'Manual'; $win.Left = -20000; $win.Top = -20000; $win.ShowActivated = $false; $win.Width = 1240; $win.Height = 760
    $win.Show(); Pump
    $vm = $win.DataContext; $vm.RootPath = $Data; Pump
    "--- 1. Ağaç araması"
    "--- Ayarlar ve tema"
    $set = New-Object Pusula.Wpf.SettingsWindow($win, $vm)
    $set.WindowStartupLocation = 'Manual'; $set.Left = -20000; $set.Top = -20000; $set.ShowActivated = $false
    $set.Show(); Pump
    Check 'tema listesi 9' ($vm.AllThemes.Count -eq 9)
    Snap $set "$Out\settings-dark.png"
    $vm.Theme = [Pusula.Wpf.Themes]::Find('Sand'); Pump
    Check 'tema uygulandı' ([Pusula.Wpf.Themes]::Current.Key -eq 'Sand')
    $saved = [IO.File]::ReadAllText($env:PUSULA_AYARLAR)
    Check 'ayarlara yazıldı' ($saved -match '"theme":\s*"Sand"') $saved
    Check 'kök klasör korundu' ($saved.Contains($Data.Replace('\', '\\'))) $saved
    Snap $set "$Out\settings-sand.png"
    "--- Notları şununla aç"
    $N = [Pusula.Wpf.NoteOpener]
    Check 'varsayılan Obsidian' ($vm.NoteApp.Key -eq 'obsidian')
    Check 'seçenekler: Obsidian, VS Code, Windows' ((($vm.NoteApps | ForEach-Object Key) -join ',') -eq 'obsidian,vscode,default')
    $vm.NoteApp = $N::Find('vscode')
    Check 'seçim ayarlara yazıldı' ([IO.File]::ReadAllText($env:PUSULA_AYARLAR) -match '"noteApp":\s*"vscode"')
    Check 'obsidian bağlantısı' ($N::UriFor('C:\Kök\a b.md', 'obsidian') -eq 'obsidian://open?path=C%3A%5CK%C3%B6k%5Ca%20b.md') ($N::UriFor('C:\Kök\a b.md', 'obsidian'))
    Check 'vscode bağlantısı (sürücü)' ($N::UriFor('C:\Kök\a b.md', 'vscode') -eq 'vscode://file/C:/K%C3%B6k/a%20b.md') ($N::UriFor('C:\Kök\a b.md', 'vscode'))
    Check 'VS Code yolu bulunur (Code.exe)' ([string]::IsNullOrEmpty($N::VsCodeExe()) -or $N::VsCodeExe().EndsWith('Code.exe')) ($N::VsCodeExe())
    Check 'vscode bağlantısı (ağ yolu)' ($N::UriFor('\\sunucu\pay\x.md', 'vscode') -eq 'vscode://file//sunucu/pay/x.md') ($N::UriFor('\\sunucu\pay\x.md', 'vscode'))
    Pump; Check 'seçim kutusu seçili öğeyi gösterir' ($set.FindName('NoteAppBox').SelectedItem -ne $null -and $set.FindName('NoteAppBox').SelectedItem.Key -eq 'vscode')
    Snap $set "$Out\settings-noteapp.png"    $set.Close()
    ($vm.ProjectNodes[0]).IsSelected = $true; Pump; Wait
    Snap $win "$Out\main-sand.png"
    $vm.Theme = [Pusula.Wpf.Themes]::Find('Night'); Pump; Wait
    Snap $win "$Out\main-night.png"
} finally {
    if ($savedSettings -ne $null) { [IO.File]::WriteAllText($settingsFile, $savedSettings) }
}
"SONUÇ OK=$ok HATA=$fail"
