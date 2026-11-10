param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
. (Join-Path $PSScriptRoot '..\_Ortak.ps1')
# Kısa not, + Olay ve not ekle kutularında @ / [[ önerisi: liste açılır, süzülür, ↑ ↓ Enter / Tab / Esc, kısa notta düz numara,
# md'ye giden kutuda [[bağlantı]]; Enter öneriyi ekler, notu göndermez; Ctrl+Z geri alır.
function FindBox($root, [string]$tag)
{
    $stack = New-Object System.Collections.Stack; $stack.Push($root)
    while ($stack.Count -gt 0)
    {
        $node = $stack.Pop()
        if ($node -is [System.Windows.Controls.TextBox] -and "$($node.Tag)" -eq $tag -and $node.IsVisible) { return $node }
        $n = [System.Windows.Media.VisualTreeHelper]::GetChildrenCount($node)
        for ($i = 0; $i -lt $n; $i++) { $stack.Push([System.Windows.Media.VisualTreeHelper]::GetChild($node, $i)) }
    }
    $null
}
function Key($box, $key)
{
    $source = [System.Windows.PresentationSource]::FromVisual($box)
    $preview = New-Object System.Windows.Input.KeyEventArgs([System.Windows.Input.Keyboard]::PrimaryDevice, $source, 0, $key)
    $preview.RoutedEvent = [System.Windows.Input.Keyboard]::PreviewKeyDownEvent
    $box.RaiseEvent($preview)
    if (-not $preview.Handled)
    {
        $down = New-Object System.Windows.Input.KeyEventArgs([System.Windows.Input.Keyboard]::PrimaryDevice, $source, 0, $key)
        $down.RoutedEvent = [System.Windows.Input.Keyboard]::KeyDownEvent
        $box.RaiseEvent($down)
    }
    Pump
}
function TypeText($box, [string]$text) { $box.Focus() | Out-Null; $box.Text = $text; $box.CaretIndex = $text.Length; [Pusula.Wpf.MentionAssist]::Update($box); Pump }
function SnapPopup($box, [string]$path)
{
    $el = [Pusula.Wpf.MentionAssist]::PopupOf($box).Child
    $el.Measure([System.Windows.Size]::new([double]::PositiveInfinity, [double]::PositiveInfinity)); $el.Arrange([System.Windows.Rect]::new($el.DesiredSize)); $el.UpdateLayout()
    $bmp = New-Object System.Windows.Media.Imaging.RenderTargetBitmap([int][Math]::Max(1, $el.ActualWidth), [int][Math]::Max(1, $el.ActualHeight), 96, 96, [System.Windows.Media.PixelFormats]::Pbgra32)
    $bmp.Render($el); $png = New-Object System.Windows.Media.Imaging.PngBitmapEncoder; $png.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($bmp))
    $fs = [IO.File]::Create($path); try { $png.Save($fs) } finally { $fs.Close() }
}
$MA = [Pusula.Wpf.MentionAssist]; $K = [System.Windows.Input.Key]

[System.Threading.SynchronizationContext]::SetSynchronizationContext((New-Object System.Windows.Threading.DispatcherSynchronizationContext))
$app = New-Object Pusula.Wpf.App; $app.InitializeComponent(); $app.ShutdownMode = 'OnExplicitShutdown'; [Pusula.Wpf.Themes]::Apply([Pusula.Wpf.Themes]::Find('FluentDark'))
$win = New-Object Pusula.Wpf.MainWindow
$win.WindowStartupLocation = 'Manual'; $win.Left = -20000; $win.Top = -20000; $win.ShowActivated = $false; $win.Width = 1360; $win.Height = 900
$win.Show(); Pump
$vm = $win.DataContext; $vm.RootPath = $Data; Wait

"--- 1. Stok kısa notu (düz numara)"
$stock = FindNode $vm.Nodes 'AB-7000-0003'; $vm.SelectedNode = $stock; Wait
$box = FindBox $win 'Stock'
Check 'kısa not kutusu bulundu, öneri takılı' ($box -ne $null -and [Pusula.Wpf.MentionAssist]::GetMode($box) -eq 'plain')
$box.Text = ''; Pump
Check 'boş kısa not kutusunda ipucu metni görünür' ([DesignKit.Controls.TextBoxHelper]::IsPlaceholderShown($box) -and [DesignKit.Controls.TextBoxHelper]::GetPlaceholderText($box) -match 'Kısa not yaz') ([DesignKit.Controls.TextBoxHelper]::GetPlaceholderText($box))
TypeText $box 'Ölçüm @JIG'
Check 'yazınca ipucu kaybolur' (-not [DesignKit.Controls.TextBoxHelper]::IsPlaceholderShown($box))
$items = $MA::Items($box)
Check '@JIG: liste açılır, jigler gelir' ($MA::IsOpen($box) -and $items.Count -ge 1 -and @($items | Where-Object { $_.Key -like 'JIG-*' }).Count -eq $items.Count) (($items | ForEach-Object { $_.Key }) -join ', ')
SnapPopup $box "$Out\mention-popup.png"
$second = if ($items.Count -ge 2) { $items[1].Key } else { $items[0].Key }
if ($items.Count -ge 2) { Key $box $K::Down }
$notesBefore = $vm.Detail.Timeline.Count
Key $box $K::Enter
Check 'Enter: seçili öneri eklenir (düz numara), not gönderilmez' ($box.Text -eq "Ölçüm $second " -and -not $MA::IsOpen($box) -and $vm.Detail.Timeline.Count -eq $notesBefore) $box.Text
$box.Undo() | Out-Null
Check 'Ctrl+Z ile geri alınır' ($box.Text -eq 'Ölçüm @JIG') $box.Text
TypeText $box 'Konuşuldu @'
$user = [Environment]::UserName
Check 'yalnız @: kişiler de listede' (@($MA::Items($box) | Where-Object { $_.Kind -eq 'Kişi' }).Count -ge 1 -or @($MA::Items($box)).Count -eq 12)
TypeText $box ('Konuşuldu @' + $user.Substring(0, [Math]::Min(3, $user.Length)))
Check 'kişi adıyla süzülür' (@($MA::Items($box) | Where-Object { $_.Key -eq $user }).Count -eq 1)
Key $box $K::Escape
Check 'Esc kapatır, metin kalır' (-not $MA::IsOpen($box) -and $box.Text.StartsWith('Konuşuldu @'))
TypeText $box 'e-posta a@b'
Check 'e-posta @ değil: liste açılmaz' (-not $MA::IsOpen($box))
TypeText $box 'yok @zzzzqq'
Check 'eşleşme yoksa açılmaz' (-not $MA::IsOpen($box))
TypeText $box 'Rapor @AB-7000-0002'; Key $box $K::Tab
Check 'Tab da ekler; stok önerisi' ($box.Text -eq 'Rapor AB-7000-0002 ') $box.Text
Key $box $K::Enter; Wait
Check 'liste kapalıyken Enter notu gönderir' ($vm.Detail.Timeline.Count -eq $notesBefore + 1 -and $box.Text -eq '') $vm.Detail.Timeline.Count
$tl = @($vm.Detail.Timeline | Where-Object { $_.IsQuickNote -and $_.Summary -match 'AB-7000-0002' })
Check 'not numarayla kaydedildi' ($tl.Count -ge 1)

"--- 2. md'ye giden kutu ([[bağlantı]])"
$host2 = New-Object System.Windows.Window; $host2.Left = -20000; $host2.Top = -20000; $host2.ShowActivated = $false; $host2.Width = 400; $host2.Height = 200
$MA::Provider = [Delegate]::CreateDelegate([Func[System.Collections.Generic.IEnumerable[Pusula.Wpf.ViewModels.NoteSuggestion]]], $vm, 'NoteSuggestions')
$tb = New-Object System.Windows.Controls.TextBox; $MA::SetMode($tb, 'link'); $host2.Content = $tb; $host2.Show(); Pump
TypeText $tb 'Bak @JIG'
Key $tb $K::Enter
Check 'link: [[NUMARA]] eklenir' ($tb.Text -match '^Bak \[\[JIG-\d{4}-\d{4}\]\] $') $tb.Text
TypeText $tb 'Stok [[AB-7000-000'
Key $tb $K::Enter
Check '[[ ile de açılır, bağlantı kapanır' ($tb.Text -match '^Stok \[\[AB-7000-000\d\]\] $') $tb.Text
$tb.Text = 'Stok [[AB-7000-000]]'; $tb.CaretIndex = 18; $MA::Update($tb); Pump; Key $tb $K::Enter
Check 'otomatik kapanmış ]] iki kez yazılmaz' ($tb.Text -match '^Stok \[\[AB-7000-000\d\]\] ?$' -and -not $tb.Text.Contains(']]]]')) $tb.Text
TypeText $tb ('Ali ile @' + $user.Substring(0, [Math]::Min(3, $user.Length))); Key $tb $K::Enter
Check 'link: kişi @Ad olarak' ($tb.Text -eq "Ali ile @$user ") $tb.Text
$host2.Close()

"--- 3. Diyalog kutuları"
$modes = New-Object System.Collections.Generic.List[string]
$stack = New-Object System.Collections.Stack; $stack.Push($win)
while ($stack.Count -gt 0) { $node = $stack.Pop(); if ($node -is [System.Windows.Controls.TextBox]) { $m = $MA::GetMode($node); if ($m) { $modes.Add($m) } }; if ($node -is [System.Windows.DependencyObject]) { foreach ($c in [System.Windows.LogicalTreeHelper]::GetChildren($node)) { if ($c -is [System.Windows.DependencyObject]) { $stack.Push($c) } } } }
Check 'öneri takılı kutular: 3 kısa not + not ekle + durum notu (düz), olay başlığı + açıklaması (bağlantı)' (@($modes | Where-Object { $_ -eq 'plain' }).Count -eq 5 -and @($modes | Where-Object { $_ -eq 'link' }).Count -eq 2) ($modes -join ',')
$win.Close()
"SONUÇ OK=$ok HATA=$fail"
