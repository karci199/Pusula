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

# Stok / proje kısa not kutusu kaldırıldı (15.11.2026, sayfa yeniden tasarlanıyor); o kutuya bağlı denemeler çıkarıldı.
$user = [Environment]::UserName
"--- 1. Günlük penceresinde # etiketi"
$stock = FindNode $vm.Nodes 'AB-7000-0003'; $vm.SelectedNode = $stock; Wait
$vm.OpenJournalHere() | Out-Null; Wait
$jt = $null; $st = New-Object System.Collections.Stack; $st.Push($win)
# Pencere ekran dışında açıldığından açılır pencere içeriği "görünür" sayılmaz; kutu bağlandığı alandan (JournalTags) bulunur.
while ($st.Count -gt 0) { $n = $st.Pop(); if ($n -is [Pusula.Wpf.TagBox]) { $bx = [System.Windows.Data.BindingOperations]::GetBinding($n, [Pusula.Wpf.TagBox]::TagsProperty); if ($bx -and $bx.Path.Path -eq 'JournalTags') { $jt = $n; break } }; foreach ($c in [System.Windows.LogicalTreeHelper]::GetChildren($n)) { if ($c -is [System.Windows.DependencyObject]) { $st.Push($c) } } }
Check 'Günlük penceresinde etiket kutusu rozetli' ($jt -ne $null)
if ($jt) { TypeText $jt.Input '#'; Check 'Günlük etiketinde # önerir' ($MA::IsOpen($jt.Input) -and $MA::Items($jt.Input).Count -ge 1) }
$vm.IsJournalOpen = $false; Wait
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

$win.Close()
"SONUÇ OK=$ok HATA=$fail"
