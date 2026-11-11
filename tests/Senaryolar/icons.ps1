param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
. (Join-Path $PSScriptRoot '..\_Ortak.ps1')
# Kendi vektör ikonları: AppIcons ve varsayılan tür simgelerinin hepsi çizilmiş olmalı; eski yazı tipi ikonuyla yan yana pafta.
$IG = [Pusula.Wpf.IconGeometries]
$missing = @()
foreach ($f in [Pusula.Wpf.AppIcons].GetFields()) { $g = [string]$f.GetValue($null); if (-not $IG::Find($g)) { $missing += $f.Name } }
Check 'AppIcons hepsi vektör' ($missing.Count -eq 0) ($missing -join ', ')
$missing = @()
foreach ($t in [Pusula.Core.Types.TypeSet]::CreateDefault().Types) { $g = [Pusula.Wpf.ViewModels.TreeNodeViewModel]::GlyphOf($t.Icon); if (-not $IG::Find($g)) { $missing += ($t.Name + ' ' + $t.Icon) } }
Check 'varsayılan tür simgeleri vektör' ($missing.Count -eq 0) ($missing -join ', ')
Check 'çizilmemiş kod yazı tipine düşer' ($IG::Find([string][char]0xE000) -eq $null)

[System.Threading.SynchronizationContext]::SetSynchronizationContext((New-Object System.Windows.Threading.DispatcherSynchronizationContext))
$app = New-Object Pusula.Wpf.App; $app.InitializeComponent(); $app.ShutdownMode = 'OnExplicitShutdown'
foreach ($theme in 'FluentDark', 'FluentLight') {
    [Pusula.Wpf.Themes]::Apply([Pusula.Wpf.Themes]::Find($theme))
    $win = New-Object System.Windows.Window
    $win.WindowStyle = 'None'; $win.WindowStartupLocation = 'Manual'; $win.Left = -20000; $win.Top = -20000; $win.ShowActivated = $false; $win.Width = 1240; $win.SizeToContent = 'Height'
    $win.SetResourceReference([System.Windows.Controls.Control]::BackgroundProperty, 'SurfaceBaseBrush')
    $wrap = New-Object System.Windows.Controls.WrapPanel; $wrap.Margin = '12'
    foreach ($g in ($IG::All | Sort-Object)) {
        $cell = New-Object System.Windows.Controls.StackPanel; $cell.Width = 100; $cell.Margin = '0,0,0,14'
        $row = New-Object System.Windows.Controls.StackPanel; $row.Orientation = 'Horizontal'; $row.HorizontalAlignment = 'Center'
        $old = New-Object DesignKit.Controls.FontIcon; $old.Glyph = $g; $old.FontSize = 24; $old.SetResourceReference([System.Windows.Controls.Control]::ForegroundProperty, 'TextTertiaryBrush')
        $new = New-Object Pusula.Wpf.AppIcon; $new.Glyph = $g; $new.FontSize = 24; $new.Margin = '14,0,0,0'; $new.SetResourceReference([System.Windows.Controls.Control]::ForegroundProperty, 'TextPrimaryBrush')
        [void]$row.Children.Add($old); [void]$row.Children.Add($new)
        $label = New-Object System.Windows.Controls.TextBlock; $label.Text = ('{0:X4}' -f [int][char]$g); $label.HorizontalAlignment = 'Center'; $label.FontSize = 11; $label.Margin = '0,4,0,0'
        $label.SetResourceReference([System.Windows.Controls.TextBlock]::ForegroundProperty, 'TextTertiaryBrush')
        [void]$cell.Children.Add($row); [void]$cell.Children.Add($label); [void]$wrap.Children.Add($cell)
    }
    $win.Content = $wrap; $win.Show(); Pump
    Snap $win ("$Out\icons-" + $theme + '.png')
    $win.Close(); Pump
}
